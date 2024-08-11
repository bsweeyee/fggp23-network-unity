using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using FGNetworkProgramming;
using System.Linq;
using System;
using Unity.Collections;

[Serializable]
public struct UnitData : INetworkSerializable, System.IEquatable<UnitData>
{

    public int UnitID;
    public int NetworkOwnerID;
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
public class NetworkGame : NetworkBehaviour, IOnGameStatePlay
{
    public NetworkVariable<float> PlayerHealth = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);    
    public NetworkVariable<int> ConnectionIndex = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);
    void Start()
    {
        PlayerHealth.OnValueChanged += (float oldValue, float newValue) => {
            if (newValue <= 0)
            {
                if (IsLocalPlayer)
                {                
                    LocalGame.Instance.ChangeState(EGameState.LOSE);
                }
                else
                {
                    LocalGame.Instance.ChangeState(EGameState.WIN);
                }
            }
        };
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[{NetworkObjectId}] Network Game Spawned!");
        
        if (IsLocalPlayer)
        {                        
            LocalGame.Instance.MyNetworkGameInstance = this;
        }
          
        // TODO: remove when player despawns
        LocalGame.Instance.NetworkGameInstances.Add(this);                                  
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (LocalGame.Instance.NetworkGameInstances.Contains(this))
        {
            LocalGame.Instance.NetworkGameInstances.Remove(this);
        }        

        Debug.Log($"[{NetworkObjectId}] Network Game Despawned!");
    }

    #region EVERYONE RPCS
    [Rpc(SendTo.Everyone)]
    public void RestartRpc()
    {
        LocalGame.Instance.ChangeState(EGameState.RESTART);
        LocalGame.Instance.ChangeState(EGameState.PLAY);
    }
    [Rpc(SendTo.Everyone)]
    private void DistributeMessageRpc(FixedString128Bytes message)
    {
        Debug.Log($"message received: {message}");
    }
    #endregion 
    
    #region SERVER RPCs
    [Rpc(SendTo.Server)]
    public void SetConnectionIndexRpc(int connectionIndex)
    {
        ConnectionIndex.Value = connectionIndex;
    }
    
    // NOTE: Dangerous to allow any NetworkGame to Despawn unit, even if it is done on server side?
    [Rpc(SendTo.Server)]
    public void DespawnUnitRpc(int unitID)
    {
        // destroy network unit
        if (LocalGame.Instance.NetworkUnitInstances.ContainsKey(unitID))
        {
            if (LocalGame.Instance.NetworkUnitInstances[unitID] != null)
                LocalGame.Instance.NetworkUnitInstances[unitID].GetComponent<NetworkObject>().Despawn();        
            else
                LocalGame.Instance.NetworkUnitInstances.Remove(unitID);
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnUnitRpc(int connectionIndex)
    {
        // TODO: do some sanity check here to see if player can spawn unit
        NetworkUnit ob = Instantiate(LocalGame.Instance.GameData.NetworkUnit);        
        ob.GetComponent<NetworkObject>().Spawn();

        var targetID = UnityEngine.Random.Range(0, LocalGame.Instance.GameData.UnitSpawnPosition.Count);
        if (targetID == connectionIndex)
        {
            targetID = (targetID+1) % LocalGame.Instance.GameData.UnitSpawnPosition.Count;
        }

        ob.UnitID.Value = System.Guid.NewGuid().GetHashCode(); 
        ob.OwnerConnectionIndexPlusOne.Value = connectionIndex + 1;
        ob.transform.position = LocalGame.Instance.GameData.UnitSpawnPosition[connectionIndex];                         
        
        ob.MoveTarget.Value = LocalGame.Instance.GameData.UnitSpawnPosition[targetID];
        ob.Health.Value = LocalGame.Instance.GameData.UnitMaxHealth;                
        ob.ChangeState(ENetworkUnitState.MOVE);
    }

    [Rpc(SendTo.Server)]
    public void SendMessageRpc(FixedString128Bytes message)
    {
        // TODO: validate if message is safe
        DistributeMessageRpc(message);
    }
    #endregion
    
    public void OnGameStatePlay(NetworkGame myNetworkGame, int localConnectionIndex)
    {
        if (IsServer)
        {
            PlayerHealth.Value = LocalGame.Instance.GameData.PlayerStartHealth;
        }
    }
}
