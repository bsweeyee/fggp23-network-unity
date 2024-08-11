using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FGNetworkProgramming;
using Unity.Netcode;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.ComponentModel;
using System;

public class GameView : MonoBehaviour, IOnGameStatePlay, IOnGameStateStart, IOnGameStateWaiting, IOnGameStateWin, IOnGameStateLose
{       
    [SerializeField] private Button spawnUnit;
    [SerializeField] private TMP_InputField customMessageInput;
    [SerializeField] private Button openMessageButton;

    private Canvas canvas;
    private Canvas messageViewCanvas;
    private EventSystem eventSystem;

    private List<Button> messageButtons;
    public void Initialize(LocalGame localGame, Camera worldCamera)
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = worldCamera;
        canvas.planeDistance = 1;

        var es = new GameObject("EventSystem");
        
        eventSystem = es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        if (customMessageInput != null)
        {
            int limit = 128 / sizeof(char);
            customMessageInput.characterLimit = limit; 
            customMessageInput.gameObject.SetActive(false);                            
        }

        messageViewCanvas = Instantiate(localGame.GameData.MessageView);
        messageViewCanvas.renderMode = RenderMode.WorldSpace;
        messageViewCanvas.worldCamera = worldCamera;
        messageViewCanvas.gameObject.SetActive(false);

        messageButtons = new List<Button>();

        for(int i=0; i < messageViewCanvas.transform.childCount; i++)
        {
            var button = messageViewCanvas.transform.GetChild(i).GetComponent<Button>();                        
            messageButtons.Add(button);
        }

        customMessageInput.onValueChanged.AddListener( (string text) => {
            var childTMP = messageButtons[messageButtons.Count - 1].GetComponentInChildren<TextMeshProUGUI>();
            childTMP.text = text;
        });

        openMessageButton.onClick.AddListener(() => {
            messageViewCanvas.gameObject.SetActive(true);
            customMessageInput.gameObject.SetActive(true);                                    
            openMessageButton.gameObject.SetActive(false);
            FGNetworkProgramming.Input.Instance.OnHandleMouseInput.AddListener(HandleMouseInput);
        });                
    }

    void HandleMouseInput(Vector2 position, EGameInput input, EInputState state)
    {
        if (input == EGameInput.LEFT_MOUSE_BUTTON && state == EInputState.PRESSED)
        {
            FGNetworkProgramming.Input.Instance.OnHandleMouseInput.RemoveListener(HandleMouseInput);
            messageViewCanvas.gameObject.SetActive(false);
            customMessageInput.gameObject.SetActive(false);                                    
            openMessageButton.gameObject.SetActive(true);

            customMessageInput.text = "";
            var tmp = messageButtons[messageButtons.Count - 1].GetComponentInChildren<TextMeshProUGUI>();            
            tmp.text = LocalGame.Instance.GameData.Messages[messageButtons.Count - 1];
        }
    }

    public void OnGameStatePlay(NetworkGame networkGame, int clientID)
    {
        gameObject.SetActive(true);
        spawnUnit.onClick.RemoveAllListeners();
        spawnUnit.onClick.AddListener(() => {
            // TODO: Figure out a way to abstract the need to reference networkGame from View
            networkGame.SpawnUnitRpc(LocalGame.Instance.LocalConnectionIndex);
        });

        messageViewCanvas.transform.position = LocalGame.Instance.GameData.UnitSpawnPosition[clientID];
        messageViewCanvas.transform.rotation = LocalGame.Instance.GameData.CameraRotation[clientID];

        Vector3 d = LocalGame.Instance.GameData.CameraSpawnPosition[clientID] - LocalGame.Instance.GameData.UnitSpawnPosition[clientID];
        Vector3 projectedD = Vector3.Dot(d, -messageViewCanvas.transform.forward) * -messageViewCanvas.transform.forward;
                
        messageViewCanvas.transform.position += (projectedD * 0.9f);

        for (int i=0; i<messageButtons.Count; i++)
        {
            var button = messageButtons[i];
            if (i == messageViewCanvas.transform.childCount - 1)
            {
                button.onClick.AddListener(() => {
                    if (!String.IsNullOrEmpty(customMessageInput.text))
                    {
                        networkGame.SendMessageRpc(customMessageInput.text);
                    }
                    messageViewCanvas.gameObject.SetActive(false);
                    customMessageInput.gameObject.SetActive(false);                                    
                    openMessageButton.gameObject.SetActive(true);
                });
            }
            else
            {
                string m = LocalGame.Instance.GameData.Messages[i];
                button.onClick.AddListener(() => {                    
                    networkGame.SendMessageRpc(m);
                    messageViewCanvas.gameObject.SetActive(false);
                    customMessageInput.gameObject.SetActive(false);                                    
                    openMessageButton.gameObject.SetActive(true);                                        
                });
            }
            
            var childTMP = button.GetComponentInChildren<TextMeshProUGUI>();
            childTMP.text = LocalGame.Instance.GameData.Messages[i];
        }
    }

    public void OnGameStateStart(LocalGame game)
    {
        gameObject.SetActive(false);
        messageViewCanvas.gameObject.SetActive(false);
        for (int i=0; i<messageButtons.Count; i++)
        {
            messageButtons[i].onClick.RemoveAllListeners();
        }
    }

    public void OnGameStateWaiting(NetworkGame myNetworkGame, LocalGame game)
    {
        gameObject.SetActive(false);
        messageViewCanvas.gameObject.SetActive(false);
        for (int i=0; i<messageButtons.Count; i++)
        {
            messageButtons[i].onClick.RemoveAllListeners();
        }
    }

    public void OnGameStateWin(NetworkGame myNetworkGame, LocalGame myLocalGame)
    {
        gameObject.SetActive(false);
        messageViewCanvas.gameObject.SetActive(false);
        for (int i=0; i<messageButtons.Count; i++)
        {
            messageButtons[i].onClick.RemoveAllListeners();
        }
    }

    public void OnGameStateLose(NetworkGame myNetworkGame, LocalGame myLocalGame)
    {
        gameObject.SetActive(false);
        messageViewCanvas.gameObject.SetActive(false);
        for (int i=0; i<messageButtons.Count; i++)
        {
            messageButtons[i].onClick.RemoveAllListeners();
        }
    }
}
