using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEditor;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;

namespace FGNetworkProgramming
{
    public enum EGameState
    {
        NONE,
        START,
        WAITING,
        PLAY,
        WIN,
        LOSE
    }

    public interface IOnGameStatePlay
    {
        void OnGameStatePlay(NetworkGame game, int clientID);
    }

    public interface IOnGameStateStart
    {
        void OnGameStateStart(LocalGame game);
    }

    public interface IOnGameStateWaiting
    {
        void OnGameStateWaiting(NetworkGame myNetworkGame, LocalGame game);
    }

    public class LocalGame : MonoBehaviour
    {
        [SerializeField] private GameData gameData;
        private EGameState currentGameState = EGameState.NONE;                

        private NetworkManager networkManagerInstance;
        private LocalGameCamera mainCameraInstance;
        private GameView gameViewInstance;
        private GameObject backgroundInstance;
        private int connectionIndex = -1;
        
        Dictionary<int, NetworkUnit> networkUnitInstances;
        List<NetworkGame> networkGameInstances;
        NetworkGame myNetworkGameInstance;        
        
        private static LocalGame instance;

        #region GUI variables
        private bool bIsShowLog = true;
        Queue logQueue = new Queue();
        #endregion

        public GameData GameData {
            get { return gameData; }
        }
        public LocalGameCamera MainCamera
        {
            get { return mainCameraInstance; }
        }
        public List<NetworkGame> NetworkGameInstances
        {
            get { return networkGameInstances; }
        }
        public NetworkGame MyNetworkGameInstance
        {
            get { return myNetworkGameInstance; }
            set { myNetworkGameInstance = value; }
        }
        public Dictionary<int, NetworkUnit> NetworkUnitInstances
        {
            get { return networkUnitInstances; }
        }    
        public int ConnectionIndex { get { return connectionIndex; } }

        public static LocalGame Instance 
        {
            get {
                if (instance == null) {
                    instance = FindObjectOfType<LocalGame>();
                }
                if (instance == null) {
                    var go = new GameObject("Game");
                    instance = go.AddComponent<LocalGame>();
                }
                return instance;
            }
        }            
        
        void Awake()
        {
            networkGameInstances = new List<NetworkGame>();
            networkUnitInstances = new Dictionary<int, NetworkUnit>();

            Input.Instance.Initialize();            
                        
            networkManagerInstance =  Instantiate(gameData.NetworkManager);
            gameViewInstance = Instantiate(gameData.GameView);            
            mainCameraInstance = FindObjectOfType<LocalGameCamera>(); // TODO: we might want to instantiate it later

            mainCameraInstance.Initialize(this);
            gameViewInstance.Initialize(this, mainCameraInstance.GameCamera);

            Input.Instance.OnHandleMouseInput.AddListener((Vector2 mousePos, EGameInput input, EInputState state) => {
                switch(input)
                {
                    case EGameInput.LEFT_MOUSE_BUTTON:
                    if (state == EInputState.PRESSED)
                    {
                        Ray ray = mainCameraInstance.GameCamera.ScreenPointToRay(mousePos);                        
                        RaycastHit hit;                    
                        bool isHit = Physics.Raycast(ray, out hit);
                        if (isHit)
                        {                            
                        }
                    }
                    break;
                }
            });

            networkManagerInstance.OnClientConnectedCallback += (ulong s) => {
                Debug.Log("Client connected: " + s);
            };
            networkManagerInstance.OnClientDisconnectCallback += (ulong s) => {
                Debug.Log("Client disconnected: " + s);
            };
            networkManagerInstance.OnClientStarted += () => {
                Debug.Log("Client started");
            };
            networkManagerInstance.OnClientStopped += (bool b) => {
                Debug.Log("Client stopped");
            };
            
            networkManagerInstance.OnConnectionEvent += HandleConnectionEvent;

            networkManagerInstance.OnServerStarted += () => {
                Debug.Log("server started!");
            };
            
            networkManagerInstance.OnServerStopped += HandleServerStop;

            networkManagerInstance.OnTransportFailure += () => {
                Debug.LogError("transport failure!");
            };

            ChangeState(EGameState.START);
        }
        
        void OnEnable() 
        {
            Application.logMessageReceived += HandleLog;
        }

        void OnDisable() 
        {
            Application.logMessageReceived -= HandleLog;
        }
        
        void OnGUI()
        {        
            if (!NetworkManager.Singleton.IsConnectedClient)
            {
                if (GUILayout.Button("Host"))
                {
                    NetworkManager.Singleton.StartHost();
                }

                if (GUILayout.Button("Join"))
                {
                    NetworkManager.Singleton.StartClient();
                }
            }
            else
            {
                GUILayout.TextArea("Client ID: " + NetworkManager.Singleton.LocalClientId.ToString());
                GUILayout.TextArea("Connection index: " + connectionIndex.ToString());
                if (GUILayout.Button("Leave"))
                {
                    NetworkManager.Singleton.Shutdown();                
                }
            }

            if (GUILayout.Button("Quit"))
            {
                Application.Quit();
            }

            GUILayout.TextArea($"Current Game State: {currentGameState}");

            if (NetworkManager.Singleton.IsServer)
            {
                string clientStrings = "Connected Clients:\n";
                foreach (var nc in NetworkManager.Singleton.ConnectedClientsList)
                {
                    clientStrings += nc.ClientId + "\n";
                }
                GUILayout.TextArea(clientStrings);
            }

            var e = networkUnitInstances.GetEnumerator();
            int unitIDToDestroy = 0;
            while (e.MoveNext())
            {
                var nu = e.Current.Value;
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.TextArea(nu.OwnerConnectionID.ToString());
                    GUILayout.TextArea(nu.UnitID.ToString());
                    GUILayout.TextArea($"HP: {nu.Health.Value}");
                    GUILayout.TextArea($"State: {nu.CurrentState.Value}");
                    if (MyNetworkGameInstance.IsServer)
                    {
                        if (GUILayout.Button("Destroy"))
                        {
                            unitIDToDestroy = nu.UnitID;                                                                                        
                        }
                    }
                }                
            }
            if (unitIDToDestroy != 0)
            {
                networkUnitInstances[unitIDToDestroy].GetComponent<NetworkObject>().Despawn();
            }

            var debugRect = new Rect(Screen.width/2, 0, Screen.width/2, Screen.height/4);
            if (bIsShowLog)
            {
                Texture2D texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, Color.black);
                texture.Apply();
                
                GUILayout.BeginArea(debugRect, texture);
                if (GUILayout.Button("Close Logs"))
                {
                    bIsShowLog = false;
                }
                GUI.Box(new Rect(0, 0, Screen.width/2, Screen.height/4), texture);
                GUILayout.Label(string.Join("\n", logQueue.ToArray()));                
                GUILayout.EndArea();
            }
            else
            {
                GUILayout.BeginArea(debugRect);
                if (GUILayout.Button("Open Logs"))
                {
                    bIsShowLog = true;
                }
                GUILayout.EndArea();
            }
        }

        void HandleLog(string logString, string stackTrace, LogType type) 
        {
            logQueue.Enqueue("[" + type + "] : " + logString);
            if (type == LogType.Exception) logQueue.Enqueue(stackTrace);            
            while (logQueue.Count > 5) logQueue.Dequeue();
        }         

        void HandleConnectionEvent(NetworkManager m, ConnectionEventData c)
        {
            string output = $"[{c.EventType}]: {c.ClientId}\nClient Ids:\n";
            foreach(var p in c.PeerClientIds)
            {
                output += $"{p}\n";
            }                
            Debug.Log(output);

            switch(c.EventType)
            {                 
                case ConnectionEvent.ClientConnected:
                if (c.ClientId == m.LocalClientId)
                {
                    connectionIndex = c.PeerClientIds.Length;                                                                
                }
                
                // NOTE: this state change is also done in a player's NetworkGame because we don't know which callback runs first
                
                var connectedClientCount = 0;
                if (NetworkManager.Singleton.IsServer)
                {
                    connectedClientCount = NetworkManager.Singleton.ConnectedClientsList.Count;                    
                }
                else
                {
                    connectedClientCount = c.PeerClientIds.Length + 1;
                }
                
                if (connectedClientCount >= GameData.NUMBER_OF_PLAYERS)
                {
                    ChangeState(EGameState.PLAY);
                }
                else
                {
                    ChangeState(EGameState.WAITING);                
                }   
                break;
                case ConnectionEvent.ClientDisconnected:
                if (c.ClientId == m.LocalClientId)
                {                                                                                    
                    connectionIndex = -1;
                    ChangeState(EGameState.START);
                }
                else
                {                    
                    // NOTE: this state change is also done in a player's NetworkGame because we don't know which callback runs first 
                    int connectedClientCount2 = 0;
                    if (NetworkManager.Singleton.IsServer)
                    {                                               
                        foreach(var cID in NetworkManager.Singleton.ConnectedClientsIds)
                        {
                            if (cID == c.ClientId) continue;
                            connectedClientCount2++;
                        }
                    }
                    else
                    {
                        connectedClientCount2 = c.PeerClientIds.Length + 1;
                    }

                    Debug.Log($"Client remaining count: {connectedClientCount2}");

                    if (connectedClientCount2 < GameData.NUMBER_OF_PLAYERS)
                    {
                        ChangeState(EGameState.WAITING);
                    }
                }
                break;
                case ConnectionEvent.PeerConnected:
                break;
                case ConnectionEvent.PeerDisconnected:
                break;
            }
        }

        void HandleServerStop(bool b)
        {
            Debug.Log($"server stopped: {b}");
            connectionIndex = -1;            
            ChangeState(EGameState.START);
        }

        public void ChangeState(EGameState newState)
        {
            if (newState == currentGameState) { Debug.LogWarning($"Game state already in {newState}\n skipping game state change..."); return; }

            switch(newState)
            {
                case EGameState.START:
                if (backgroundInstance) Destroy(backgroundInstance);
                
                NetworkGameInstances.Clear();
                networkUnitInstances.Clear();

                myNetworkGameInstance = null;

                var startStateInterfaces = FindObjectsOfType<MonoBehaviour>(true).OfType<IOnGameStateStart>();
                foreach(var ni in startStateInterfaces)
                {
                    ni.OnGameStateStart(this);
                }
                break;
                
                case EGameState.WAITING:
                if (backgroundInstance) Destroy(backgroundInstance);
                
                if (MyNetworkGameInstance.IsServer)
                {
                    var e = networkUnitInstances.GetEnumerator();                
                    while (e.MoveNext())
                    {
                        var nu = e.Current.Value;    
                        MyNetworkGameInstance.DespawnUnitRpc(nu.UnitID);
                    }
                }

                networkUnitInstances.Clear();

                var waitingStateInterfaces = FindObjectsOfType<MonoBehaviour>(true).OfType<IOnGameStateWaiting>();
                foreach(var ni in waitingStateInterfaces)
                {
                    ni.OnGameStateWaiting(myNetworkGameInstance, this);
                }
                break;

                case EGameState.PLAY:
                backgroundInstance = Instantiate(gameData.BackgroundPrefab);                
                
                var playStateInterfaces = FindObjectsOfType<MonoBehaviour>(true).OfType<IOnGameStatePlay>();
                foreach(var ni in playStateInterfaces)
                {
                    ni.OnGameStatePlay(myNetworkGameInstance, connectionIndex);
                }
                break;

                case EGameState.WIN:
                if (backgroundInstance) Destroy(backgroundInstance);
                break;

                case EGameState.LOSE:
                if (backgroundInstance) Destroy(backgroundInstance);
                break;
            }
            currentGameState = newState;
        }

        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Handles.color = Color.red;
            Gizmos.color = Color.red;

            for (int i=0; i< GameData.NUMBER_OF_PLAYERS; i++)
            {
                Handles.DrawSolidDisc(gameData.UnitSpawnPosition[i], Vector3.up, 0.25f);                                    
                Gizmos.DrawWireSphere(gameData.UnitSpawnPosition[i], gameData.UnitSpawnRadius[i]);
            }

            Gizmos.color = Color.white;
            for (int i=0; i<GameData.NUMBER_OF_PLAYERS; i++)
            {
                Gizmos.DrawSphere(gameData.CameraSpawnPosition[i], 0.25f);
            }

            if (Application.isPlaying)
            {
                if (NetworkGameInstances != null)
                {
                    foreach(var nu in networkUnitInstances)
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawWireSphere(nu.Value.MoveTarget.Value, 0.25f);
                        Gizmos.DrawLine(nu.Value.transform.position, nu.Value.MoveTarget.Value);                
                        
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(nu.Value.transform.position, gameData.UnitAttackRadius);
                    }
                }
            }
        }
        #endif
    }
}
