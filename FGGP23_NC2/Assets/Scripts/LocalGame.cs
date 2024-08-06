using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace FGNetworkProgramming
{

    public class LocalGame : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;

        [SerializeField] private List<Material> gameMaterials = new List<Material>();
        
        private Camera mainCamera;

        public Camera MainCamera
        {
            get { return mainCamera; }
        }    

        private static LocalGame instance;
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
        
        public List<Material> GameMaterials { get { return gameMaterials; } }
        void Awake()
        {            
            Input.Instance.Initialize();            
            Instantiate(networkManager);
            mainCamera = FindObjectOfType<Camera>(); // TODO: we might want to instantiate it later
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
        }
    }
}
