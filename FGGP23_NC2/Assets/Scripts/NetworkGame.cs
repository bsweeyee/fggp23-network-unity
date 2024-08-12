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
    
    private bool hasAssignedConnectionIndex = false;
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
        // NOTE: connection index relays number of player information so it should work
        ConnectionIndex.OnValueChanged += (int oldValue, int newValue) => {            
            // NOTE: the host does not receive this callback because first value is initialized as 0. ( so OnValueChanged is not called. We work around the state change by handling it OnServerStarted )        
            if (newValue + 1 >= GameData.NUMBER_OF_PLAYERS)
            {
                LocalGame.Instance.ChangeState(EGameState.MULTIPLAYER_PLAY);
            }
            else
            {
                LocalGame.Instance.ChangeState(EGameState.WAITING);                
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
            NetworkManager.Singleton.OnConnectionEvent += HandleClientConnectionEvent;
            if (IsServer)
            {
                NetworkManager.Singleton.OnServerStarted += HandleServerStart;
                NetworkManager.Singleton.OnServerStopped += HandleServerStop;
            }
        }

        if (IsServer)
        {                                            
            NetworkManager.Singleton.OnConnectionEvent += HandleServerConnectionEvent;            
        }
          
        // TODO: remove when player despawns
        LocalGame.Instance.NetworkGameInstances.Add(this);

        // NetworkManager.Singleton.OnClientConnectedCallback += (ulong s) => {
        //     Debug.Log("Client connected: " + s);
        // };
        // NetworkManager.Singleton.OnClientDisconnectCallback += (ulong s) => {
        //     Debug.Log("Client disconnected: " + s);
        // };
        // NetworkManager.Singleton.OnClientStarted += () => {
        //     Debug.Log("Client started");
        // };
        // NetworkManager.Singleton.OnClientStopped += (bool b) => {
        //     Debug.Log("Client stopped");
        // };
        
        // NetworkManager.Singleton.OnServerStarted += () => {
        //     Debug.Log("server started!");
        // };
        
        // NetworkManager.Singleton.OnTransportFailure += () => {
        //     Debug.LogError("transport failure!");
        // };                                  
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

    void HandleClientConnectionEvent(NetworkManager m, ConnectionEventData c)
    {
        string output = $"[Handle Client Connection Event][{c.EventType}]: {c.ClientId}\nClient Ids:\n";
        foreach(var p in c.PeerClientIds)
        {
            output += $"{p}\n";
        }                
        Debug.Log(output);

        switch(c.EventType)
        {
            case ConnectionEvent.ClientDisconnected:
            if (c.ClientId == m.LocalClientId)
            {                                
                NetworkManager.Singleton.OnConnectionEvent -= HandleClientConnectionEvent;
                LocalGame.Instance.ChangeState(EGameState.START);
            }
            else
            {
                if (IsServer) break;

                int connectedClientCount2 = 0;                
                connectedClientCount2 = c.PeerClientIds.Length + 1;

                Debug.Log($"Client remaining count: {connectedClientCount2}");

                if (connectedClientCount2 < GameData.NUMBER_OF_PLAYERS)
                {
                    LocalGame.Instance.ChangeState(EGameState.WAITING);
                }                                
            } 
            break;
        }
    }

    void HandleServerConnectionEvent(NetworkManager m, ConnectionEventData c)
    {
        string output = $"[Handle Server Connection Event][{c.EventType}]: {c.ClientId}\nClient Ids:\n";
        foreach(var p in c.PeerClientIds)
        {
            output += $"{p}\n";
        }                
        Debug.Log(output);        

        switch(c.EventType)
        {                 
            case ConnectionEvent.ClientConnected:                                        
            if (NetworkManager.Singleton.IsServer)
            {                
                if (!hasAssignedConnectionIndex)
                {                                                            
                    ConnectionIndex.Value = LocalGame.Instance.NetworkGameInstances.Count - 1;
                    hasAssignedConnectionIndex = true;
                }
            }                       
            break;
            case ConnectionEvent.ClientDisconnected:            
            if (c.ClientId == m.LocalClientId)
            {                                
                NetworkManager.Singleton.OnConnectionEvent -= HandleServerConnectionEvent;
                LocalGame.Instance.ChangeState(EGameState.START);
            }
            else
            {                    
                int connectedClientCount2 = 0;
                foreach(var cID in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (cID == c.ClientId) continue;
                    connectedClientCount2++;
                }

                Debug.Log($"Client remaining count: {connectedClientCount2}");

                if (connectedClientCount2 < GameData.NUMBER_OF_PLAYERS)
                {
                    LocalGame.Instance.ChangeState(EGameState.WAITING);
                }
            }
            break;
            case ConnectionEvent.PeerConnected:
            break;
            case ConnectionEvent.PeerDisconnected:
            break;
        }
    }

    void HandleServerStart()
    {
        Debug.Log($"server started");
        LocalGame.Instance.ChangeState(EGameState.WAITING);
    }

    void HandleServerStop(bool b)
    {
        Debug.Log($"server stopped: {b}");                    
        LocalGame.Instance.ChangeState(EGameState.START);
        
        NetworkManager.Singleton.OnServerStarted -= HandleServerStart;
        NetworkManager.Singleton.OnServerStopped -= HandleServerStop;
        NetworkManager.Singleton.OnConnectionEvent -= HandleServerConnectionEvent;
    }

    #region EVERYONE RPCS
    [Rpc(SendTo.Everyone)]
    public void RestartRpc()
    {
        LocalGame.Instance.ChangeState(EGameState.RESTART);
        LocalGame.Instance.ChangeState(EGameState.MULTIPLAYER_PLAY);
    }
    [Rpc(SendTo.Everyone)]
    private void DistributeMessageRpc(FixedString128Bytes message)
    {
        Debug.Log($"message received: {message}");
    }
    #endregion 
    
    #region SERVER RPCs
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
