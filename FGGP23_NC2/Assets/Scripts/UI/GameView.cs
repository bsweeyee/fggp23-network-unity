using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FGNetworkProgramming;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System;

public class GameView : MonoBehaviour, 
                        IOnGameStatePlay, IOnGameStateStart, IOnGameStateWaiting, IOnGameStateWin, IOnGameStateLose, 
                        IOnMessageReceived, INetworkUnitSpawn, INetworkUnitDespawn,
                        IOnGameHealthChange
{       
    /// <summary>
    /// Assigned GameObjects in Editor
    /// </summary>
    [SerializeField] private List<Button> spawnUnitButtons;
    [SerializeField] private Button fireButton;
    [SerializeField] private TMP_InputField customMessageInput;
    [SerializeField] private Button openMessageButton;
    [SerializeField] private Image healthAmountFill;

    /// <summary>
    /// Spawned game instances
    /// </summary>
    private Canvas gameViewCanvasInstance;
    private Canvas messageViewCanvasInstance;
    private Dictionary<int, Canvas> replyViewCanvasInstances;
    private Dictionary<int, UnitStatView> unitStatViewInstances;
    private Dictionary<int, PlayerStatView> playerStatViewInstances;
    private EventSystem eventSystem;

    private List<Button> messageButtons; // NOTE: Last button is always the custom message button

    /// <summary>
    /// Temporary time states
    /// </summary>
    private Dictionary<int, float> lastReplyCanvasDisplayTime;
    private float lastMessageSendTime = -1;
    private float lastSpawnRequestSendTime = -1;
    private float lastFireRequestSendTime = -1;
    
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
            if (String.IsNullOrEmpty(text))
            {
                messageButtons[messageButtons.Count - 1].interactable = false;                
                childTMP.text = LocalGame.Instance.GameData.Messages[messageButtons.Count - 1];
            }
            else
            {
                if (lastMessageSendTime < 0)
                {
                    messageButtons[messageButtons.Count - 1].interactable = true;
                }
                else
                {
                    messageButtons[messageButtons.Count - 1].interactable = false;
                }
            }
        });

        openMessageButton.onClick.AddListener(() => {
            messageViewCanvasInstance.gameObject.SetActive(true);
            customMessageInput.gameObject.SetActive(true);                                    
            openMessageButton.gameObject.SetActive(false);
            FGNetworkProgramming.Input.Instance.OnHandleMouseInput.AddListener(HandleMouseInput);
        });                
    
        // Initialize reply view
        replyViewCanvasInstances = new Dictionary<int, Canvas>();
        lastReplyCanvasDisplayTime = new Dictionary<int, float>();

        for(int i=0; i<GameData.NUMBER_OF_PLAYERS; i++)
        {
            var replyViewCanvasInstance = Instantiate(LocalGame.Instance.GameData.ReplyView);
            replyViewCanvasInstance.renderMode = RenderMode.WorldSpace;
            replyViewCanvasInstance.worldCamera = LocalGame.Instance.MainCamera.GameCamera;
            replyViewCanvasInstance.gameObject.SetActive(false);

            replyViewCanvasInstances.Add(i, replyViewCanvasInstance);
        }

        // Initialize Unit State view
        unitStatViewInstances = new Dictionary<int, UnitStatView>();

        // Initialize player state view
        playerStatViewInstances = new Dictionary<int, PlayerStatView>();
        for(int i=0; i<GameData.NUMBER_OF_PLAYERS; i++)
        {
            var playerStatViewInstance = Instantiate(LocalGame.Instance.GameData.PlayerStatView);
            playerStatViewInstance.Initialize(this);
            playerStatViewInstances.Add(i, playerStatViewInstance);
        }                
    }
    
    private void Update()
    {
        switch (LocalGame.Instance.CurrentState)
        {
            case EGameState.MULTIPLAYER_PLAY:
            var e = lastReplyCanvasDisplayTime.GetEnumerator();                
            var toRemove = new List<int>();
            while(e.MoveNext())
            {            
                var lastDisplayTime = e.Current.Value;
                if (Time.time - lastDisplayTime > LocalGame.Instance.GameData.MessageDisplayCooldownInSeconds)
                {
                    replyViewCanvasInstances[e.Current.Key].gameObject.SetActive(false);                
                    toRemove.Add(e.Current.Key);
                }
            }

            foreach(var k in toRemove)
            {
                lastReplyCanvasDisplayTime.Remove(k);
            }

            if (lastMessageSendTime >= 0 && Time.time - lastMessageSendTime > LocalGame.Instance.GameData.MessageSendCooldownInSeconds)
            {
                for(int i=0; i<messageButtons.Count; i++)
                {
                    if (i == messageButtons.Count - 1)
                    {
                        if (String.IsNullOrEmpty(customMessageInput.text))
                        {
                            messageButtons[i].interactable = false;                        
                        }
                        else
                        {
                            messageButtons[i].interactable = true;
                        }
                    }
                    else
                    {
                        messageButtons[i].interactable = true;
                    }
                }
                lastMessageSendTime = -1;
            }
            
            // we reset all the spawn buttons when cooldown is done
            if (lastSpawnRequestSendTime >= 0 && Time.time - lastSpawnRequestSendTime > LocalGame.Instance.GameData.SpawnCooldownInSeconds)
            {
                foreach(var b in spawnUnitButtons)
                {
                    b.interactable = true;
                } 
                lastSpawnRequestSendTime = -1;
            }

            // we reset fire button when cooldown is done
            if (lastFireRequestSendTime >= 0 && Time.time - lastFireRequestSendTime > LocalGame.Instance.GameData.ProjectileCooldownInSeconds)
            {
                fireButton.interactable = true;
                lastFireRequestSendTime = -1;
            }            
            break;
        }
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

            customMessageInput.SetTextWithoutNotify("");            
        }
    }

    void OnSpawnUnit(NetworkGame networkGame, int spawnIndex)
    {
        networkGame.SpawnUnitRpc(LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value, spawnIndex);
    }

    public void OnGameStatePlay(NetworkGame networkGame, int myConnectionIndex)
    {              
        gameObject.SetActive(true);        
        int[] indices = new int[spawnUnitButtons.Count];
        for(int i=0; i<indices.Length; i++) indices[i] = i;

        // initializing spawn unit button states
        for(int i=0; i<spawnUnitButtons.Count; i++)
        {   
            int index = indices[i];         
            spawnUnitButtons[i].onClick.RemoveAllListeners();
            spawnUnitButtons[i].onClick.AddListener(() => {
                var x = index;
                if (myConnectionIndex%2 != 0) x = indices.Length - index - 1;
                lastSpawnRequestSendTime = Time.time;
                OnSpawnUnit(networkGame, x);
                foreach(var b in spawnUnitButtons)
                {
                    b.interactable = false;
                }                 
            });
            spawnUnitButtons[i].interactable = true;
        }

        // reposition MessageViewCanvas to face the camera and also closer to camera view plane
        int interval = Mathf.FloorToInt(LocalGame.Instance.GameData.UnitSpawnPosition.Count / GameData.NUMBER_OF_PLAYERS);
        int middleIndex = interval * myConnectionIndex + (interval / 2);

        messageViewCanvasInstance.transform.position = LocalGame.Instance.GameData.UnitSpawnPosition[middleIndex];
        messageViewCanvasInstance.transform.rotation = LocalGame.Instance.GameData.CameraRotation[myConnectionIndex];

        Vector3 d = LocalGame.Instance.GameData.CameraSpawnPosition[myConnectionIndex] - LocalGame.Instance.GameData.UnitSpawnPosition[middleIndex];
        Vector3 projectedD = Vector3.Dot(d, -messageViewCanvasInstance.transform.forward) * -messageViewCanvasInstance.transform.forward;
                
        messageViewCanvasInstance.transform.position += (projectedD * 0.9f);

        // reposition ReplyViewCanvases to face the camera and also closer to camera view plane
        for(int i=0; i<LocalGame.Instance.NetworkGameInstances.Count; i++)
        {
            var ng = LocalGame.Instance.NetworkGameInstances[i];
            
            interval = Mathf.FloorToInt(LocalGame.Instance.GameData.UnitSpawnPosition.Count / GameData.NUMBER_OF_PLAYERS);
            middleIndex = interval * ng.ConnectionIndex.Value + (interval / 2);
            
            var replyViewCanvasInstance = replyViewCanvasInstances[i];            
            replyViewCanvasInstance.transform.position = LocalGame.Instance.GameData.UnitSpawnPosition[middleIndex];
            replyViewCanvasInstance.transform.rotation = LocalGame.Instance.GameData.CameraRotation[LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value];
            
            d = LocalGame.Instance.GameData.CameraSpawnPosition[LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value] - LocalGame.Instance.GameData.UnitSpawnPosition[middleIndex];
            projectedD = Vector3.Dot(d, -messageViewCanvasInstance.transform.forward) * -messageViewCanvasInstance.transform.forward;
                    
            replyViewCanvasInstance.transform.position += (projectedD * 0.9f);
        }   

        // reposition player stat view to their respective position
        for (int i=0; i<LocalGame.Instance.NetworkGameInstances.Count; i++)
        {
            var ng = LocalGame.Instance.NetworkGameInstances[i];
            
            var playerStatCanvasInstance = playerStatViewInstances[i];            
            playerStatCanvasInstance.transform.position = LocalGame.Instance.GameData.PlayerSpawnPosition[ng.ConnectionIndex.Value];
            playerStatCanvasInstance.transform.rotation = LocalGame.Instance.GameData.CameraRotation[LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value];
            
            d = LocalGame.Instance.GameData.CameraSpawnPosition[LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value] - LocalGame.Instance.GameData.PlayerSpawnPosition[ng.ConnectionIndex.Value];
            projectedD = Vector3.Dot(d, -messageViewCanvasInstance.transform.forward) * -messageViewCanvasInstance.transform.forward;
                    
            playerStatCanvasInstance.transform.position += (projectedD * 0.9f);

            playerStatCanvasInstance.EnterGamePlayState(ng.ConnectionIndex.Value);
        }     

        // initialize message buttons
        for (int i=0; i<messageButtons.Count; i++)
        {
            var button = messageButtons[i];
            if (i == messageViewCanvasInstance.transform.childCount - 1)
            {
                button.interactable = false;                                
                button.onClick.AddListener(() => {
                    if (!String.IsNullOrEmpty(customMessageInput.text))
                    {
                        networkGame.SendMessageRpc(customMessageInput.text, LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value);
                    }
                    messageViewCanvasInstance.gameObject.SetActive(false);
                    customMessageInput.gameObject.SetActive(false);                                    
                    openMessageButton.gameObject.SetActive(true);
                    
                    foreach(var b in messageButtons)
                    {
                        b.interactable = false;
                    }
                    lastMessageSendTime = Time.time;
                    customMessageInput.text = "";
                });
            }
            else
            {
                button.interactable = true;
                string m = LocalGame.Instance.GameData.Messages[i];
                button.onClick.AddListener(() => {                    
                    networkGame.SendMessageRpc(m, LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value);
                    messageViewCanvasInstance.gameObject.SetActive(false);
                    customMessageInput.gameObject.SetActive(false);                                    
                    openMessageButton.gameObject.SetActive(true);
                    button.interactable = false;

                    foreach(var b in messageButtons)
                    {
                        b.interactable = false;
                    }
                    lastMessageSendTime = Time.time;
                    customMessageInput.text = "";                                        
                });
            }
            
            var childTMP = button.GetComponentInChildren<TextMeshProUGUI>();
            childTMP.text = LocalGame.Instance.GameData.Messages[i];
        }        

        // initialize fire button
        fireButton.interactable = true;
        fireButton.onClick.RemoveAllListeners();
        fireButton.onClick.AddListener(() => {
            var ph = LocalGame.Instance.ProjectileHandlers[myConnectionIndex];
            ph.Spawn(myConnectionIndex);
            lastFireRequestSendTime = Time.time;
            fireButton.interactable = false;
        });

        lastMessageSendTime = -1;
        lastFireRequestSendTime = -1;
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

        lastMessageSendTime = -1;
        lastReplyCanvasDisplayTime.Clear();

        foreach(var playerHealthInstances in playerStatViewInstances)
        {
            playerHealthInstances.Value.ExitGamePlayState();
        }

        fireButton.onClick.RemoveAllListeners();
        fireButton.interactable = true;
    }

    public void OnGameStateWaiting(NetworkGame myNetworkGame, LocalGame game)
    {
        gameObject.SetActive(false);
        messageViewCanvasInstance.gameObject.SetActive(false);
        for (int i=0; i<messageButtons.Count; i++)
        {
            messageButtons[i].onClick.RemoveAllListeners();
        }

        lastMessageSendTime = -1;
        lastReplyCanvasDisplayTime.Clear();

        foreach(var playerHealthInstances in playerStatViewInstances)
        {
            playerHealthInstances.Value.ExitGamePlayState();
        }

        fireButton.onClick.RemoveAllListeners();
        fireButton.interactable = true;
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

        lastMessageSendTime = -1;
        lastReplyCanvasDisplayTime.Clear();

        foreach(var playerHealthInstances in playerStatViewInstances)
        {
            playerHealthInstances.Value.ExitGamePlayState();
        }

        fireButton.onClick.RemoveAllListeners();
        fireButton.interactable = true;
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

        lastMessageSendTime = -1;
        lastReplyCanvasDisplayTime.Clear();

        foreach(var playerHealthInstances in playerStatViewInstances)
        {
            playerHealthInstances.Value.ExitGamePlayState();
        }

        fireButton.onClick.RemoveAllListeners();
        fireButton.interactable = true;
    }

    public void OnMessageReceieved(string message, int ownerconnectionIndex)
    {        
        replyViewCanvasInstances[ownerconnectionIndex].gameObject.SetActive(true);
        var tmp = replyViewCanvasInstances[ownerconnectionIndex].gameObject.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = message;
        if (lastReplyCanvasDisplayTime.ContainsKey(ownerconnectionIndex))
        {
            lastReplyCanvasDisplayTime[ownerconnectionIndex] = Time.time;
        }
        else
        {
            lastReplyCanvasDisplayTime.Add(ownerconnectionIndex, Time.time);
        }
    }

    public void OnNetworkUnitIDUpdate(NetworkUnit unit, int unitID)
    {
        var nuStateView = Instantiate(LocalGame.Instance.GameData.UnitStatView, unit.transform);
        unitStatViewInstances.Add(unitID, nuStateView);
        
        nuStateView.transform.localPosition = Vector3.zero;
        var offset = nuStateView.GetComponent<RectTransform>().rect.height * nuStateView.transform.localScale.y;
        nuStateView.transform.position += Vector3.up * (unit.CurrentMeshRenderer.bounds.extents.y + offset);        

        nuStateView.transform.rotation = LocalGame.Instance.GameData.CameraRotation[LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value];        

        if (unit.OwnerConnectionIndexPlusOne.Value - 1 >= 0)
        {            
            nuStateView.Initialize(this, unitID, unit.OwnerConnectionIndexPlusOne.Value - 1);
        }
    }

    public void OnNetworkUnitOwnerConnectionIDUpdate(NetworkUnit unit, int ownerConnectionIndexPlusOne)
    {
        if (unitStatViewInstances.ContainsKey(unit.UnitID.Value))
        {
            var nuStateView = unitStatViewInstances[unit.UnitID.Value];
            nuStateView.Initialize(this, unit.UnitID.Value, ownerConnectionIndexPlusOne - 1);
        }
    }    

    public void OnNetworkUnitDespawn(NetworkUnit unit)
    {        
        var nuStateView = unitStatViewInstances[unit.UnitID.Value];
        Destroy(nuStateView.gameObject);
        unitStatViewInstances.Remove(unit.UnitID.Value);                
    }

    public void OnGameHealthChange(int ownerConnectionIndex, float oldValue, float newValue)
    {
        if (LocalGame.Instance.MyNetworkGameInstance.ConnectionIndex.Value == ownerConnectionIndex)
        {
            float v = newValue / LocalGame.Instance.GameData.PlayerStartHealth;
            healthAmountFill.fillAmount = v;
        }
        else
        {
            playerStatViewInstances[ownerConnectionIndex].SetHealthValue(ownerConnectionIndex, oldValue, newValue);
        }
    }
}
