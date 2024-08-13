using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

namespace FGNetworkProgramming
{
    [CreateAssetMenu(fileName = "GameData", menuName = "Data/Game")]
    public class GameData : ScriptableObject
    {
        #region Game Settings
        public static int NUMBER_OF_PLAYERS = 2;

        [SerializeField] private float playerStartHealth = 20.0f;
        [SerializeField] private LayerMask playerAttackableLayer;
        
        [SerializeField] private float messageSendCooldownInSeconds = 1.5f;
        [SerializeField] private float messageDisplayCooldownInSeconds = 1.0f;
        [SerializeField] private List<string> messages;

        public float PlayerStartHealth { get { return playerStartHealth;} }

        public LayerMask PlayerAttackableLayer { get { return playerAttackableLayer; }}
        
        public float MessageSendCooldownInSeconds { get { return messageSendCooldownInSeconds; }}

        public float MessageDisplayCooldownInSeconds { get { return messageDisplayCooldownInSeconds; }}

        public List<string> Messages { get { return messages; } }
        #endregion

        #region Network Prefabs
        [Header("Network Prefabs")]
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private NetworkUnit networkUnit;
        
        public NetworkManager NetworkManager { get { return networkManager; } }
        public NetworkUnit NetworkUnit { get { return networkUnit; }}
        #endregion
        
        #region Game Assets / Prefabs
        [Header("Game Assets / Prefabs")]
        [SerializeField] private List<Material> gameMaterials = new List<Material>();        
        [SerializeField] private GameObject backgroundPrefab;
        [SerializeField] private GameSpawnHitArea hitAreaPrefab;        

        public List<Material> GameMaterials { get { return gameMaterials; }}
        public GameObject BackgroundPrefab { get { return backgroundPrefab; }}
        public GameSpawnHitArea HitAreaPrefab { get { return hitAreaPrefab; }}
        #endregion        

        #region UI Prefabs

        [Header("UI Prefabs")]
        [SerializeField] private GameView gameView;
        [SerializeField] private UnitStatView unitStatView;
        [SerializeField] private Canvas messageView;
        [SerializeField] private Canvas replyView;
        public GameView GameView { get { return gameView; }}
        public UnitStatView UnitStatView { get { return unitStatView; } }
        public Canvas MessageView { get { return messageView; } }
        public Canvas ReplyView { get { return replyView; } }
        #endregion

        #region Scene Settings

        [Header("Scene Settings")]
        [SerializeField] private Vector3 cameraNonNetworkSpawnPosition = Vector3.zero;
        [SerializeField] private Quaternion cameraNonNetworkRotation = Quaternion.identity;

        [SerializeField] private List<Vector3> unitSpawnPosition = new List<Vector3>(new Vector3[NUMBER_OF_PLAYERS]);
        [SerializeField] private List<float> unitSpawnRadius = new List<float>(new float[NUMBER_OF_PLAYERS]);
        [SerializeField] private List<Vector3> cameraSpawnPosition = new List<Vector3>(new Vector3[NUMBER_OF_PLAYERS]);
        [SerializeField] private List<Quaternion> cameraRotation = new List<Quaternion>(new Quaternion[2] { Quaternion.identity, Quaternion.identity } );

        public Vector3 CameraNonNetworkSpawnPosition { get { return cameraNonNetworkSpawnPosition; } set { cameraNonNetworkSpawnPosition = value; } }
        public Quaternion CameraNonNetworkRotation { get { return cameraNonNetworkRotation; } set { cameraNonNetworkRotation= value; } }


        public List<Vector3> UnitSpawnPosition { get { return unitSpawnPosition; }}         
        public List<float> UnitSpawnRadius { get { return unitSpawnRadius; }} 
        public List<Vector3> CameraSpawnPosition { get { return cameraSpawnPosition; }}         
        public List<Quaternion> CameraRotation { get { return cameraRotation; }}         
        #endregion
    
        #region Unit Settings
        
        [Header("Unit Settings")]
        [SerializeField] private float unitAttackRadius = 1;
        [SerializeField] private float unitAttackIntervalSeconds = 1;
        [SerializeField] private float unitAttackStrength = 1;
        [SerializeField] private float unitMaxHealth = 10;
        [SerializeField] private LayerMask unitAttackableLayer;
        
        public float UnitAttackRadius { get { return unitAttackRadius; }}
        public float UnitMaxHealth { get { return unitMaxHealth; }}
        public float UnitAttackIntervalSeconds { get { return unitAttackIntervalSeconds; }}
        public float UnitAttackStrength { get { return unitAttackStrength; }}
        public LayerMask UnitAttackableLayer { get { return unitAttackableLayer; }}
        #endregion
    }
}
