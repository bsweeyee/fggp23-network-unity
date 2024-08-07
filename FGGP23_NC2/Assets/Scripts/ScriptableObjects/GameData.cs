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
        #endregion

        #region Network Prefabs
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private NetworkUnit networkUnit;
        
        public NetworkManager NetworkManager { get { return networkManager; } }
        public NetworkUnit NetworkUnit { get { return networkUnit; }}
        #endregion
        
        #region Game Assets
        [SerializeField] private List<Material> gameMaterials = new List<Material>();        
        
        public List<Material> GameMaterials { get { return gameMaterials; }}
        #endregion        

        #region UI Prefabs
        [SerializeField] private GameView gameView;
        public GameView GameView { get { return gameView; }}
        #endregion

        #region Game Editor
        [SerializeField] private List<Vector3> unitSpawnPosition = new List<Vector3>(new Vector3[NUMBER_OF_PLAYERS]);
        public List<Vector3> UnitSpawnPosition { get { return unitSpawnPosition; }} 
        #endregion
    }
}
