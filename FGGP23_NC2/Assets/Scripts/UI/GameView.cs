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

public class GameView : MonoBehaviour, IOnGameStatePlay, IOnGameStateStart, IOnGameStateWaiting, IOnGameStateWin, IOnGameStateLose, IOnMessageReceived
{       
    [SerializeField] private Button spawnUnit;
    [SerializeField] private TMP_InputField customMessageInput;
    [SerializeField] private Button openMessageButton;

    private Canvas gameViewCanvasInstance;
    private Canvas messageViewCanvasInstance;
    private Dictionary<int, Canvas> replyViewCanvasInstances;
    private EventSystem eventSystem;

    private List<Button> messageButtons;
    public void Initialize(LocalGame localGame, Camera worldCamera)
    {
        // Initialize game view canvas
        gameViewCanvasInstance = GetComponent<Canvas>();
        gameViewCanvasInstance.renderMode = RenderMode.ScreenSpaceCamera;
        gameViewCanvasInstance.worldCamera = worldCamera;
        gameViewCanvasInstance.planeDistance = 1;

        var es = new GameObject("EventSystem");
        
        eventSystem = es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        // Initialize custom message Input field
        if (customMessageInput != null)
        {
            int limit = 128 / sizeof(char);
            customMessageInput.characterLimit = limit; 
            customMessageInput.gameObject.SetActive(false);                            
        }

        // Initialize message view
        messageViewCanvasInstance = Instantiate(localGame.GameData.MessageView);
        messageViewCanvasInstance.renderMode = RenderMode.WorldSpace;
        messageViewCanvasInstance.worldCamera = worldCamera;
        messageViewCanvasInstance.gameObject.SetActive(false);

        // Initialize buttons
        messageButtons = new List<Button>();

        for(int i=0; i < messageViewCanvasInstance.transform.childCount; i++)
        {
            var button = messageViewCanvasInstance.transform.GetChild(i).GetComponent<Button>();                        
            messageButtons.Add(button);
        }

        customMessageInput.onValueChanged.AddListener( (string text) => {
            var childTMP = messageButtons[messageButtons.Count - 1].GetComponentInChildren<TextMeshProUGUI>();
            childTMP.text = text;
        });

        openMessageButton.onClick.AddListener(() => {
            messageViewCanvasInstance.gameObject.SetActive(true);
            customMessageInput.gameObject.SetActive(true);                                    
            openMessageButton.gameObject.SetActive(false);
            FGNetworkProgramming.Input.Instance.OnHandleMouseInput.AddListener(HandleMouseInput);
        });                
    
        // Initialize reply view
        replyViewCanvasInstances = new Dictionary<int, Canvas>();
    }

    void HandleMouseInput(Vector2 position, EGameInput input, EInputState state)
    {
        if (input == EGameInput.LEFT_MOUSE_BUTTON && state == EInputState.PRESSED)
        {
            FGNetworkProgramming.Input.Instance.OnHandleMouseInput.RemoveListener(HandleMouseInput);
            messageViewCanvasInstance.gameObject.SetActive(false);
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
            networkGame.SpawnUnitRpc(LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value);
        });

        // reposition MessageViewCanvas to face the camera and also closer to camera view plane
        messageViewCanvasInstance.transform.position = LocalGame.Instance.GameData.UnitSpawnPosition[clientID];
        messageViewCanvasInstance.transform.rotation = LocalGame.Instance.GameData.CameraRotation[clientID];

        Vector3 d = LocalGame.Instance.GameData.CameraSpawnPosition[clientID] - LocalGame.Instance.GameData.UnitSpawnPosition[clientID];
        Vector3 projectedD = Vector3.Dot(d, -messageViewCanvasInstance.transform.forward) * -messageViewCanvasInstance.transform.forward;
                
        messageViewCanvasInstance.transform.position += (projectedD * 0.9f);

        // reposition ReplyViewCanvases to face the camera and also closer to camera view plane
        foreach(var ng in LocalGame.Instance.NetworkGameInstances)
        {
            var replyViewCanvasInstance = Instantiate(LocalGame.Instance.GameData.ReplyView);
            replyViewCanvasInstance.renderMode = RenderMode.WorldSpace;
            replyViewCanvasInstance.worldCamera = LocalGame.Instance.MainCamera.GameCamera;
            replyViewCanvasInstance.gameObject.SetActive(false);

            replyViewCanvasInstance.transform.position = LocalGame.Instance.GameData.UnitSpawnPosition[ng.ConnectionIndex.Value];
            replyViewCanvasInstance.transform.rotation = LocalGame.Instance.GameData.CameraRotation[LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value];
            
            d = LocalGame.Instance.GameData.CameraSpawnPosition[LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value] - LocalGame.Instance.GameData.UnitSpawnPosition[ng.ConnectionIndex.Value];
            projectedD = Vector3.Dot(d, -messageViewCanvasInstance.transform.forward) * -messageViewCanvasInstance.transform.forward;
                    
            replyViewCanvasInstance.transform.position += (projectedD * 0.9f);

            replyViewCanvasInstances.Add(ng.ConnectionIndex.Value, replyViewCanvasInstance);
        }        

        // initialize message buttons
        for (int i=0; i<messageButtons.Count; i++)
        {
            var button = messageButtons[i];
            if (i == messageViewCanvasInstance.transform.childCount - 1)
            {
                button.onClick.AddListener(() => {
                    if (!String.IsNullOrEmpty(customMessageInput.text))
                    {
                        networkGame.SendMessageRpc(customMessageInput.text, LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value);
                    }
                    messageViewCanvasInstance.gameObject.SetActive(false);
                    customMessageInput.gameObject.SetActive(false);                                    
                    openMessageButton.gameObject.SetActive(true);
                });
            }
            else
            {
                string m = LocalGame.Instance.GameData.Messages[i];
                button.onClick.AddListener(() => {                    
                    networkGame.SendMessageRpc(m, LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value);
                    messageViewCanvasInstance.gameObject.SetActive(false);
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
        messageViewCanvasInstance.gameObject.SetActive(false);
        for (int i=0; i<messageButtons.Count; i++)
        {
            messageButtons[i].onClick.RemoveAllListeners();
        }

        var rvce = replyViewCanvasInstances.GetEnumerator();
        while (rvce.MoveNext())
        {
            rvce.Current.Value.gameObject.SetActive(false);
        }
    }

    public void OnGameStateWaiting(NetworkGame myNetworkGame, LocalGame game)
    {
        gameObject.SetActive(false);
        messageViewCanvasInstance.gameObject.SetActive(false);
        for (int i=0; i<messageButtons.Count; i++)
        {
            messageButtons[i].onClick.RemoveAllListeners();
        }
    }

    public void OnGameStateWin(NetworkGame myNetworkGame, LocalGame myLocalGame)
    {
        gameObject.SetActive(false);
        messageViewCanvasInstance.gameObject.SetActive(false);
        for (int i=0; i<messageButtons.Count; i++)
        {
            messageButtons[i].onClick.RemoveAllListeners();
        }
        var rvce = replyViewCanvasInstances.GetEnumerator();
        while (rvce.MoveNext())
        {
            rvce.Current.Value.gameObject.SetActive(false);
        }
    }

    public void OnGameStateLose(NetworkGame myNetworkGame, LocalGame myLocalGame)
    {
        gameObject.SetActive(false);
        messageViewCanvasInstance.gameObject.SetActive(false);
        for (int i=0; i<messageButtons.Count; i++)
        {
            messageButtons[i].onClick.RemoveAllListeners();
        }

        var rvce = replyViewCanvasInstances.GetEnumerator();
        while (rvce.MoveNext())
        {
            rvce.Current.Value.gameObject.SetActive(false);
        }
    }

    public void OnMessageReceieved(string message, int ownerconnectionIndex)
    {
        // TODO: add to queue and buffer it instead
        replyViewCanvasInstances[ownerconnectionIndex].gameObject.SetActive(true);
        var tmp = replyViewCanvasInstances[ownerconnectionIndex].gameObject.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = message;
    }
}
