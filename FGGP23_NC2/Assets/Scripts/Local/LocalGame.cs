using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEditor;
using UnityEngine.UI;
using Unity.VisualScripting;

namespace FGNetworkProgramming
{    
    public class LocalGame : MonoBehaviour
    {
        [SerializeField] private GameData gameData;                

        private NetworkManager networkManagerInstance;
        private LocalGameCamera mainCameraInstance;
        private GameView gameViewInstance;
        
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
            gameViewInstance.Initialize(this);
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
            }

            if (GUILayout.Button("Quit"))
            {
                Application.Quit();
            }

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
                    GUILayout.TextArea(nu.OwnerID.ToString());
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
                if (networkGameInstances != null)
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
