using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using FGNetworkProgramming;
using System.Linq;
using System;

public interface INetworkGameSpawned
{
    void OnNetworkGameSpawned(NetworkGame game, ulong clientID);
}

[Serializable]
public struct UnitData : INetworkSerializable, System.IEquatable<UnitData>
{

    public int UnitID;
    public ulong NetworkOwnerID;
    // public Vector3 Position;

    // Copy constructor
    public UnitData(UnitData data)
    {
        UnitID = data.UnitID;
        NetworkOwnerID = data.NetworkOwnerID;
        // Position = data.Position;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out UnitID);
            reader.ReadValueSafe(out NetworkOwnerID);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(UnitID);
            writer.WriteValueSafe(NetworkOwnerID);
        }
    }

    public bool Equals(UnitData other)
    {
        return other.Equals(this);
    }
}

/// <summary>
/// Network Game tracks GameStates and manages spawned network objects
/// </summary>
public class NetworkGame : NetworkBehaviour
{
    #region CLIENT-READ, SERVER-WRITE NETWORK VARIABLES
    private NetworkList<UnitData> unitData;

    public NetworkList<UnitData> UnitData
    {
        get { return unitData; }
    }
    #endregion

    void Awake()
    {
        unitData = new NetworkList<UnitData>(writePerm: NetworkVariableWritePermission.Server);        
    }

    void Start()
    {
        // TODO: register input events
        if (IsLocalPlayer)
        {
            FGNetworkProgramming.Input.Instance.OnHandleMouseInput.AddListener((Vector2 mousePos, EGameInput input, EInputState state) => {
                switch(input)
                {
                    case EGameInput.LEFT_MOUSE_BUTTON:
                    if (state == EInputState.PRESSED)
                    {
                        Ray ray = FGNetworkProgramming.LocalGame.Instance.MainCamera.GameCamera.ScreenPointToRay(mousePos);                        
                        RaycastHit hit;                    
                        bool isHit = Physics.Raycast(ray, out hit);
                        if (isHit)
                        {                            
                        }
                    }
                    break;
                }
            });
        }        
    }        
    
    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {            
            // we call all interfaces that should only run when network is spawned
            var networkInitializers = FindObjectsOfType<MonoBehaviour>(true).OfType<INetworkGameSpawned>();
            foreach(var ni in networkInitializers)
            {
                ni.OnNetworkGameSpawned(this, NetworkManager.Singleton.LocalClientId);
            }
            LocalGame.Instance.MyNetworkGameInstance = this;
        }

        unitData.OnListChanged += (NetworkListEvent<UnitData> changeEvent) => {                
            Debug.Log($"[S] The list changed and now has {unitData.Count} elements\n [{changeEvent.Type}]: {changeEvent.Value.UnitID},{changeEvent.Value.NetworkOwnerID}");
            
            switch (changeEvent.Type)
            {
                case NetworkListEvent<UnitData>.EventType.Add:                
                // Unit u = Instantiate(LocalGame.Instance.GameData.Unit);
                // u.Initialize(changeEvent.Value, NetworkManager.Singleton.LocalClientId);
                // LocalGame.Instance.UnitInstances.Add(u);
                break;
                case NetworkListEvent<UnitData>.EventType.Value:
                // UnitData ud = changeEvent.Value;
                // Unit unit = LocalGame.Instance.UnitInstances.Find(x => x.Data.ID == ud.ID);
                // unit.transform.position = ud.Position;
                break;                                                
            }                    
        };        
        // TODO: remove when player despawns
        LocalGame.Instance.NetworkGameInstances.Add(this);
    }

    #region SERVER RPCs
    [Rpc(SendTo.Server)]
    public void DespawnUnitRpc(int unitID)
    {
        int idxToRemove = -1;
        foreach(var u in unitData)
        {
            idxToRemove++;
            if (unitID == u.UnitID)
            {
                break; 
            }
        }
        if (idxToRemove >= 0)
        {
            unitData.RemoveAt(idxToRemove);
        }

        // destroy network unit
        LocalGame.Instance.NetworkUnitInstances[unitID].GetComponent<NetworkObject>().Despawn();        
    }

    [Rpc(SendTo.Server)]
    public void SpawnUnitRpc(ulong networkGameID)
    {
        // TODO: do some sanity check here to see if player can spawn unit
        var ud = new UnitData();
        ud.NetworkOwnerID = networkGameID;
        ud.UnitID = System.Guid.NewGuid().GetHashCode();
        unitData.Add(ud);

        NetworkUnit ob = Instantiate(LocalGame.Instance.GameData.NetworkUnit);
        ob.transform.position = new Vector3(99, 99, 99); // we set initial position somewhere super far so it does not randomly collide with things
        ob.GetComponent<NetworkObject>().Spawn();

        int ownID = (int)networkGameID;
        var targetID = UnityEngine.Random.Range(0, LocalGame.Instance.GameData.UnitSpawnPosition.Count);
        if (targetID == ownID)
        {
            targetID = (targetID+1) % LocalGame.Instance.GameData.UnitSpawnPosition.Count;
        }

        ob.InitializeRpc(networkGameID, ud.UnitID);                
        
        ob.MoveTarget.Value = LocalGame.Instance.GameData.UnitSpawnPosition[targetID];
        ob.Health.Value = LocalGame.Instance.GameData.UnitMaxHealth;
        ob.LastAttackTime.Value = Time.time;        
        ob.ChangeStateRpc(ENetworkUnitState.MOVE);
    }    
    #endregion    
}
