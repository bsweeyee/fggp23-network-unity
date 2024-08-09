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
        #endregion

        #region Network Prefabs
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private NetworkUnit networkUnit;
        
        public NetworkManager NetworkManager { get { return networkManager; } }
        public NetworkUnit NetworkUnit { get { return networkUnit; }}
        #endregion
        
        #region Game Assets
        [SerializeField] private List<Material> gameMaterials = new List<Material>();        
        [SerializeField] private Unit unitPrefab;

        public List<Material> GameMaterials { get { return gameMaterials; }}
        public Unit Unit { get { return unitPrefab; }}
        #endregion        

        #region UI Prefabs
        [SerializeField] private GameView gameView;
        public GameView GameView { get { return gameView; }}
        #endregion

        #region Game Editor
        [SerializeField] private List<Vector3> unitSpawnPosition = new List<Vector3>(new Vector3[NUMBER_OF_PLAYERS]);
        [SerializeField] private List<float> unitSpawnRadius = new List<float>(new float[NUMBER_OF_PLAYERS]);
        [SerializeField] private List<Vector3> cameraSpawnPosition = new List<Vector3>(new Vector3[NUMBER_OF_PLAYERS]);
        [SerializeField] private List<Quaternion> cameraRotation = new List<Quaternion>(new Quaternion[2] { Quaternion.identity, Quaternion.identity } );

        public List<Vector3> UnitSpawnPosition { get { return unitSpawnPosition; }}         
        public List<float> UnitSpawnRadius { get { return unitSpawnRadius; }} 
        public List<Vector3> CameraSpawnPosition { get { return cameraSpawnPosition; }}         
        public List<Quaternion> CameraRotation { get { return cameraRotation; }}         
        #endregion
    
        #region Unit Settings
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
