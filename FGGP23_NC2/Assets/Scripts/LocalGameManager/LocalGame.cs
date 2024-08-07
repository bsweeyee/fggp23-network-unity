using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEditor;

namespace FGNetworkProgramming
{    
    public class LocalGame : MonoBehaviour
    {
        [SerializeField] private GameData gameData;                

        private NetworkManager networkManagerInstance;
        private Camera mainCameraInstance;
        private GameView gameViewInstance;
        private static LocalGame instance;

        public GameData GameData {
            get { return gameData; }
        }
        public Camera MainCamera
        {
            get { return mainCameraInstance; }
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
            Input.Instance.Initialize();            
            networkManagerInstance =  Instantiate(gameData.NetworkManager);
            gameViewInstance = Instantiate(gameData.GameView);
            mainCameraInstance = FindObjectOfType<Camera>(); // TODO: we might want to instantiate it later

            gameViewInstance.Initialize(this);
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

            var debugRect = new Rect(Screen.width/2, Screen.height/2, Screen.width/2, Screen.height/2);
            GUILayout.BeginArea(debugRect);
            GUI.Box(debugRect, "a");
            GUILayout.EndArea();
        }         

        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Handles.color = Color.red;            
            for (int i=0; i< gameData.UnitSpawnPosition.Count; i++)
            {
                Handles.DrawSolidDisc(gameData.UnitSpawnPosition[i], Vector3.up, 0.25f);                                    
            }
        }
        #endif
    }
}
