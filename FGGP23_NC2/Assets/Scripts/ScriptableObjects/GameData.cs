using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

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

        [SerializeField] private float spawnCooldownInSeconds = 1.0f;

        public float PlayerStartHealth { get { return playerStartHealth;} }

        public LayerMask PlayerAttackableLayer { get { return playerAttackableLayer; }}
        
        public float MessageSendCooldownInSeconds { get { return messageSendCooldownInSeconds; }}

        public float MessageDisplayCooldownInSeconds { get { return messageDisplayCooldownInSeconds; }}

        public List<string> Messages { get { return messages; } }

        public float SpawnCooldownInSeconds { get { return spawnCooldownInSeconds; } }
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
        [SerializeField] private ProjectileHandler projectileHandlerPrefab;   
        [SerializeField] private Projectile projectilePrefab;     

        public List<Material> GameMaterials { get { return gameMaterials; }}
        public GameObject BackgroundPrefab { get { return backgroundPrefab; }}
        public GameSpawnHitArea HitAreaPrefab { get { return hitAreaPrefab; }}
        public ProjectileHandler ProjectileHandlerPrefab { get { return projectileHandlerPrefab; } }
        public Projectile ProjectilePrefab { get { return projectilePrefab; } }
        #endregion        

        #region UI Prefabs

        [Header("UI Prefabs")]
        [SerializeField] private GameView gameView;
        [SerializeField] private UnitStatView unitStatView;
        [SerializeField] private PlayerStatView playerStatView;
        [SerializeField] private Canvas messageView;
        [SerializeField] private Canvas replyView;
        [SerializeField] private Canvas damageView;
        
        public GameView GameView { get { return gameView; }}
        public UnitStatView UnitStatView { get { return unitStatView; } }
        public PlayerStatView PlayerStatView { get { return playerStatView; }}
        public Canvas MessageView { get { return messageView; } }
        public Canvas ReplyView { get { return replyView; } }
        public Canvas DamageView { get { return damageView; } }
        #endregion

        #region Scene Settings

        [Header("Scene Settings")]
        [SerializeField] private List<Vector3> playerSpawnPosition = new List<Vector3>(new Vector3[NUMBER_OF_PLAYERS]);
        [SerializeField] private List<Quaternion> playerSpawnRotaion = new List<Quaternion>(new Quaternion[2] { Quaternion.identity, Quaternion.identity });

        [SerializeField] private Vector3 cameraNonNetworkSpawnPosition = Vector3.zero;
        [SerializeField] private Quaternion cameraNonNetworkRotation = Quaternion.identity;

        [SerializeField] private List<Vector3> unitSpawnPosition = new List<Vector3>(new Vector3[NUMBER_OF_PLAYERS]);
        [SerializeField] private List<float> unitSpawnRadius = new List<float>(new float[NUMBER_OF_PLAYERS]);
        [SerializeField] private List<Vector3> cameraSpawnPosition = new List<Vector3>(new Vector3[NUMBER_OF_PLAYERS]);
        [SerializeField] private List<Quaternion> cameraRotation = new List<Quaternion>(new Quaternion[2] { Quaternion.identity, Quaternion.identity } );

        public List<Vector3> PlayerSpawnPosition { get { return playerSpawnPosition; } }
        public List<Quaternion> PlayerSpawnRotation { get { return playerSpawnRotaion; } }

        public Vector3 CameraNonNetworkSpawnPosition { get { return cameraNonNetworkSpawnPosition; } set { cameraNonNetworkSpawnPosition = value; } }
        public Quaternion CameraNonNetworkRotation { get { return cameraNonNetworkRotation; } set { cameraNonNetworkRotation= value; } }

        public List<Vector3> UnitSpawnPosition { get { return unitSpawnPosition; }}         
        public List<float> UnitSpawnRadius { get { return unitSpawnRadius; }} 
        public List<Vector3> CameraSpawnPosition { get { return cameraSpawnPosition; }}         
        public List<Quaternion> CameraRotation { get { return cameraRotation; }}         
        #endregion
    
        #region Unit Settings
        
        [Header("Unit Settings")]
        [SerializeField] private List<Color> unitColors = new List<Color>(new Color[NUMBER_OF_PLAYERS]);
        [SerializeField] private float unitAttackRadius = 1;
        [SerializeField] private float unitAttackIntervalSeconds = 1;
        [SerializeField] private float unitAttackStrength = 1;
        [SerializeField] private float unitMaxHealth = 10;
        [SerializeField] private LayerMask unitAttackableLayer;
        
        public List<Color> UnitColors { get { return unitColors; } }
        public float UnitAttackRadius { get { return unitAttackRadius; }}
        public float UnitMaxHealth { get { return unitMaxHealth; }}
        public float UnitAttackIntervalSeconds { get { return unitAttackIntervalSeconds; }}
        public float UnitAttackStrength { get { return unitAttackStrength; }}
        public LayerMask UnitAttackableLayer { get { return unitAttackableLayer; }}
        #endregion
    
        #region Projectile Settings
        
        [SerializeField] private float minForwardStrength = 0.1f;
        [SerializeField] private float maxForwardStrength = 0.2f;

        [SerializeField] private float minNormalizedDirection = 0.35f;
        [SerializeField] private float maxNormalizedDirection = 0.65f;

        [SerializeField] private float projectileUpStrength = 0.2f;
        [SerializeField] private Vector3 projectileGravity = new Vector3(0.0f, -1.0f, 0.0f);
        
        
        public float MinForwardStrength { get { return minForwardStrength; } }
        public float MaxForwardStrength { get { return maxForwardStrength; } 
        }
        public float MinNormalizedDirection { get { return minNormalizedDirection; } }
        public float MaxNormalizedDirection { get { return maxNormalizedDirection; } }
        
        public float ProjectileUpStrength { get { return projectileUpStrength; } }
        public Vector3 ProjectileGravity { get { return projectileGravity; } }
        #endregion
    }
}
