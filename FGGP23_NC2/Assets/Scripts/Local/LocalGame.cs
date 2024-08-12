using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEditor;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;

namespace FGNetworkProgramming
{
    public enum EGameState
    {
        NONE,
        START,
        WAITING,
        RESTART,
        MULTIPLAYER_PLAY,
        WIN,
        LOSE,
    }

    public interface IOnGameStatePlay
    {
        void OnGameStatePlay(NetworkGame myNetworkGame, int localConnectionIndex);
    }

    public interface IOnGameStateStart
    {
        void OnGameStateStart(LocalGame myLocalGame);
    }

    public interface IOnGameStateWaiting
    {
        void OnGameStateWaiting(NetworkGame myNetworkGame, LocalGame myLocalGame);
    }

    public interface IOnGameStateWin
    {
        void OnGameStateWin(NetworkGame myNetworkGame, LocalGame myLocalGame);
    }

    public interface IOnGameStateLose
    {
        void OnGameStateLose(NetworkGame myNetworkGame, LocalGame myLocalGame);
    }

    public class LocalGame : MonoBehaviour
    {
        [SerializeField] private GameData gameData;
        private EGameState currentGameState = EGameState.NONE;                

        private NetworkManager networkManagerInstance;
        private LocalGameCamera mainCameraInstance;
        private GameView gameViewInstance;
        private GameObject backgroundInstance;
        private List<GameSpawnHitArea> hitAreas; 

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
        public EGameState CurrentState { get { return currentGameState; } }

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
            hitAreas = new List<GameSpawnHitArea>();

            networkGameInstances = new List<NetworkGame>();
            networkUnitInstances = new Dictionary<int, NetworkUnit>();
                        
            networkManagerInstance =  Instantiate(gameData.NetworkManager);
            gameViewInstance = Instantiate(gameData.GameView);            
            mainCameraInstance = FindObjectOfType<LocalGameCamera>(); // TODO: we might want to instantiate it later

            mainCameraInstance.Initialize(this);
            gameViewInstance.Initialize(this, mainCameraInstance.GameCamera);

            Input.Instance.Initialize();            
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
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.TextArea("Client ID: " + NetworkManager.Singleton.LocalClientId.ToString());
                    int localConnectionIndex = -1 ;
                    if (MyNetworkGameInstance != null) localConnectionIndex = MyNetworkGameInstance.ConnectionIndex.Value;
                    GUILayout.TextArea("Connection index: " + localConnectionIndex.ToString());
                }
                if (GUILayout.Button("Reset"))
                {
                    MyNetworkGameInstance.RestartRpc();
                }
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

            GUILayout.Label("Network Game Instances:");
            var nge = NetworkGameInstances.GetEnumerator();
            while(nge.MoveNext())
            {                
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.TextArea($"Connection Index: {nge.Current.ConnectionIndex.Value}");
                    GUILayout.TextArea($"Game HP: {nge.Current.PlayerHealth.Value}");
                }
            }            
            
            GUILayout.Label("Network Unit Instances");
            var e = networkUnitInstances.GetEnumerator();
            int unitIDToDestroy = 0;
            while (e.MoveNext())
            {
                var nu = e.Current.Value;
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.TextArea((nu.OwnerConnectionIndexPlusOne.Value-1).ToString());
                    GUILayout.TextArea(nu.UnitID.Value.ToString());
                    GUILayout.TextArea($"HP: {nu.Health.Value}");
                    GUILayout.TextArea($"State: {nu.CurrentState.Value}");
                    if (MyNetworkGameInstance.IsServer)
                    {
                        if (GUILayout.Button("Destroy"))
                        {
                            unitIDToDestroy = nu.UnitID.Value;                                                                                        
                        }
                    }
                }                
            }
            if (unitIDToDestroy != 0)
            {
                MyNetworkGameInstance.DespawnUnitRpc(unitIDToDestroy);
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
        
        void ClearNetworkUnits()
        {
            if (MyNetworkGameInstance != null && MyNetworkGameInstance.IsServer)
            {
                var unitsToRemove = new List<int>();
                var e = networkUnitInstances.GetEnumerator();                
                while (e.MoveNext())
                {
                    var nu = e.Current.Value;
                    if (nu != null)
                    {
                        unitsToRemove.Add(nu.UnitID.Value);    
                    }
                }

                foreach(int uID in unitsToRemove)
                {
                    if (MyNetworkGameInstance != null)
                    {
                        MyNetworkGameInstance.DespawnUnitRpc(uID);
                    }
                }
            }
            networkUnitInstances.Clear();
        }

        void ClearHitAreas()
        {
            foreach(var gsha in hitAreas)
            {
                Destroy(gsha.gameObject);
            }
            hitAreas.Clear();
        }

        public void ChangeState(EGameState newState)
        {
            if (newState == currentGameState) { Debug.LogWarning($"Game state already in {newState}\n skipping game state change..."); return; }

            switch(newState)
            {
                case EGameState.START:
                if (backgroundInstance) Destroy(backgroundInstance);
                
                ClearHitAreas();
                ClearNetworkUnits();

                NetworkGameInstances.Clear();
                MyNetworkGameInstance = null;

                var startStateInterfaces = FindObjectsOfType<MonoBehaviour>(true).OfType<IOnGameStateStart>();
                foreach(var ni in startStateInterfaces)
                {
                    ni.OnGameStateStart(this);
                }
                break;
                
                case EGameState.WAITING:
                if (backgroundInstance) Destroy(backgroundInstance);
                
                ClearHitAreas();                
                ClearNetworkUnits();

                var waitingStateInterfaces = FindObjectsOfType<MonoBehaviour>(true).OfType<IOnGameStateWaiting>();
                foreach(var ni in waitingStateInterfaces)
                {
                    ni.OnGameStateWaiting(myNetworkGameInstance, this);
                }
                break;

                case EGameState.MULTIPLAYER_PLAY:
                backgroundInstance = Instantiate(gameData.BackgroundPrefab);

                // spawn hit areas
                for (int i = 0; i<GameData.UnitSpawnPosition.Count; i++)
                {
                    GameSpawnHitArea a = Instantiate(GameData.HitAreaPrefab);
                    a.Initialize(i, GameData.UnitSpawnPosition[i], GameData.UnitSpawnRadius[i]);
                    hitAreas.Add(a);
                }                                
                
                var playStateInterfaces = FindObjectsOfType<MonoBehaviour>(true).OfType<IOnGameStatePlay>();
                foreach(var ni in playStateInterfaces)
                {
                    ni.OnGameStatePlay(myNetworkGameInstance, myNetworkGameInstance.ConnectionIndex.Value);
                }
                break;

                case EGameState.WIN:
                if (backgroundInstance) Destroy(backgroundInstance);
                
                ClearHitAreas();
                ClearNetworkUnits();

                var winStateInterfaces = FindObjectsOfType<MonoBehaviour>(true).OfType<IOnGameStateWin>();
                foreach(var ni in winStateInterfaces)
                {
                    ni.OnGameStateWin(myNetworkGameInstance, this);
                }
                break;

                case EGameState.LOSE:
                if (backgroundInstance) Destroy(backgroundInstance);
                
                ClearHitAreas();
                ClearNetworkUnits();

                var loseStateInterfaces = FindObjectsOfType<MonoBehaviour>(true).OfType<IOnGameStateLose>();
                foreach(var ni in loseStateInterfaces)
                {
                    ni.OnGameStateLose(myNetworkGameInstance, this);
                }
                break;
                case EGameState.RESTART:
                if (backgroundInstance) Destroy(backgroundInstance);
                
                ClearHitAreas();
                ClearNetworkUnits();
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
