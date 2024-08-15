using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using FGNetworkProgramming;
using System.Linq;
using System;
using Unity.Collections;
using System.Net.Mail;

/// <summary>
/// THIS IS NOW OBSOLETE, DO NOT USE
/// Unit Data is a struct container that tracks a unit's variable states instead of storing it in a C# script
/// However, this turns out to not be useful and may cost more network bandwidth because updating data requires sending the entire struct
/// Shifted back to using NetworkVariables
/// </summary>
[Obsolete]
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

public struct TMessagePacket
{
    public int SenderConnectionIndex;
    public string Message;
}

public struct TSpawnPacket
{
    public int ConnectionIndex;
    public int SpawnIndex;

    public TSpawnPacket(int connectionIndex, int spawnIndex)
    {
        this.ConnectionIndex = connectionIndex;
        this.SpawnIndex = spawnIndex;
    }
}

public struct TProjectilePacket
{
    public int ConnectionIndex;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 InputVelocity;

    public TProjectilePacket(int connectionIndex, Vector3 position, Quaternion rotation, Vector3 inputVelocity)
    {
        this.ConnectionIndex = connectionIndex;
        this.Position = position;
        this.Rotation = rotation;
        this.InputVelocity = inputVelocity;
    }
}

public interface IOnMessageReceived {
    void OnMessageReceieved(string message, int ownerconnectionIndex);
}

public interface IOnGameHealthChange {
    void OnGameHealthChange(int ownerConnectionIndex, float oldValue, float newValue);
}

/// <summary>
/// Network Game tracks GameStates and manages spawned network objects
/// </summary>
public class NetworkGame : NetworkBehaviour, IOnGameStatePlay
{
    public NetworkVariable<float> PlayerHealth = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);    
    
    // This will control when the MULIPLAYER_PLAY state is triggered. 
    public NetworkVariable<int> ConnectionIndex = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);
    
    private bool hasAssignedConnectionIndex = false;

    private Queue<TMessagePacket> server_MessageQueue;
    private Queue<TSpawnPacket> server_spawnRequestQueue;
    private Queue<TProjectilePacket> server_projectileRequestQueue;
    
    private float server_LastMessageTime;
    private float server_LastSpawnCooldownTime;
    private float server_LastProjectileCooldownTime;

    void Start()
    {
        server_MessageQueue = new Queue<TMessagePacket>();
        server_spawnRequestQueue = new Queue<TSpawnPacket>();
        server_projectileRequestQueue = new Queue<TProjectilePacket>();

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

            var gameHealthChangeInterfaces = FindObjectsOfType<MonoBehaviour>(true).OfType<IOnGameHealthChange>(); 
            foreach(var ghc in gameHealthChangeInterfaces)
            {
                ghc.OnGameHealthChange(ConnectionIndex.Value, oldValue, newValue);
            }
        };        
        // NOTE: connection index tracks number of player information so doing the check for state change should work here                 
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

        // we need to remove the copy of HandleServerConnectionEvent delegate from the host / server side
        if (IsServer)
        {
            NetworkManager.Singleton.OnConnectionEvent -= HandleServerConnectionEvent;        
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
            case ConnectionEvent.ClientConnected:
            NetworkManager.Singleton.OnClientStopped -= LocalGame.Instance.HandleLocalClientStop;
            break;
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
        string output = $"[{ConnectionIndex.Value}: Handle Server Connection Event][{c.EventType}]: {c.ClientId}\nClient Ids:\n";
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
            // code here is to account for the client-side players                            
            if (c.ClientId == m.LocalClientId)
            {    
                NetworkManager.Singleton.OnConnectionEvent -= HandleServerConnectionEvent;
                LocalGame.Instance.ChangeState(EGameState.START);
            }
            else
            {       
                // code here is to account for other connected players         
                if (IsLocalPlayer)
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
            }
            break;
            case ConnectionEvent.PeerConnected:
            break;
            case ConnectionEvent.PeerDisconnected:
            break;
        }
    }

    void HandleServerStop(bool b)
    {
        Debug.Log($"server stopped: {b}");                    
        LocalGame.Instance.ChangeState(EGameState.START);
        
        NetworkManager.Singleton.OnServerStopped -= HandleServerStop;
        NetworkManager.Singleton.OnConnectionEvent -= HandleServerConnectionEvent;
        NetworkManager.Singleton.OnConnectionEvent -= HandleClientConnectionEvent;
    }

    void Update()
    {
        if (IsServer)
        {
            if (server_MessageQueue.Count > 0)
            {
                if (Time.time - server_LastMessageTime > LocalGame.Instance.GameData.MessageSendCooldownInSeconds)
                {
                    var mp = server_MessageQueue.Dequeue();
                    DistributeMessageRpc(mp.Message, mp.SenderConnectionIndex);                    
                }
            }

            if (server_spawnRequestQueue.Count > 0)
            {
                if (Time.time - server_LastSpawnCooldownTime > LocalGame.Instance.GameData.SpawnCooldownInSeconds)
                {
                    var spawnRequest = server_spawnRequestQueue.Dequeue();

                    // TODO: do some sanity check here to see if player can spawn unit
                    NetworkUnit ob = Instantiate(LocalGame.Instance.GameData.NetworkUnit);        
                    ob.GetComponent<NetworkObject>().Spawn();

                    int interval = Mathf.FloorToInt(LocalGame.Instance.GameData.UnitSpawnPosition.Count / GameData.NUMBER_OF_PLAYERS);        
                    // var offset = UnityEngine.Random.Range(0, interval);
                    var targetConnectionIndex = UnityEngine.Random.Range(0, GameData.NUMBER_OF_PLAYERS);
                    if (targetConnectionIndex == spawnRequest.ConnectionIndex)
                    {
                        targetConnectionIndex += 1;
                        targetConnectionIndex %= GameData.NUMBER_OF_PLAYERS;
                    }
                    
                    int toTargetIndex = interval * targetConnectionIndex + spawnRequest.SpawnIndex;
                    int toSpawnIndex = interval * spawnRequest.ConnectionIndex + spawnRequest.SpawnIndex;                

                    // initializing all variables on server side
                    ob.UnitID.Value = System.Guid.NewGuid().GetHashCode(); 
                    ob.OwnerConnectionIndexPlusOne.Value = spawnRequest.ConnectionIndex + 1;
                    ob.MoveTarget.Value = LocalGame.Instance.GameData.UnitSpawnPosition[toTargetIndex];
                    ob.Health.Value = LocalGame.Instance.GameData.UnitMaxHealth;                
                    
                    ob.transform.position = LocalGame.Instance.GameData.UnitSpawnPosition[toSpawnIndex];                                 
                    ob.ChangeState(ENetworkUnitState.MOVE); // TOOD: I think I made 2 network rpcs here because ChangeState is another network RPC. See if we can skip the other RPC call since this is already a serverRPC                                        

                    server_spawnRequestQueue.Clear();
                    server_LastSpawnCooldownTime = Time.time;
                }
            }

            if (server_projectileRequestQueue.Count > 0)
            {
                if (Time.time - server_LastProjectileCooldownTime > LocalGame.Instance.GameData.ProjectileCooldownInSeconds)
                {
                    var pr = server_projectileRequestQueue.Dequeue();
                    SpawnProjectileClientHostRpc(pr.ConnectionIndex, pr.Position, pr.Rotation, pr.InputVelocity);
                    server_projectileRequestQueue.Clear();
                    server_LastProjectileCooldownTime = Time.time;
                }
            }
        }
    }

    #region EVERYONE RPCS
    [Rpc(SendTo.Everyone)]
    public void RestartRpc()
    {
        LocalGame.Instance.ChangeState(EGameState.RESTART);
        LocalGame.Instance.ChangeState(EGameState.MULTIPLAYER_PLAY);
    }
    [Rpc(SendTo.Everyone)]
    private void DistributeMessageRpc(FixedString128Bytes message, int senderConnectionIndex)
    {                
        var receivedMessageInterfaces = FindObjectsOfType<MonoBehaviour>(true).OfType<IOnMessageReceived>().ToArray();
        foreach(var rmi in receivedMessageInterfaces)
        {
            rmi.OnMessageReceieved(message.ToString(), senderConnectionIndex);
        }
    }
    #endregion

    #region CLIENT_HOST RPCS
    [Rpc(SendTo.ClientsAndHost)]
    void SpawnProjectileClientHostRpc(int connectionIndex, Vector3 position, Quaternion rotation, Vector3 inputVelocity)
    {
        LocalGame.Instance.ProjectileHandlers[connectionIndex].CreateProjectile(position, rotation, inputVelocity);
    }
    #endregion 
    
    #region SERVER RPCs
    // NOTE: Seems dangerous to allow any NetworkGame to Despawn unit, even if it is a server rpc call
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
    public void SpawnUnitRpc(int connectionIndex, int spawnIndex)
    {
        server_spawnRequestQueue.Enqueue(new TSpawnPacket(connectionIndex, spawnIndex));       
    }

    [Rpc(SendTo.Server)]
    public void SpawnProjectileServerRpc(int connectionIndex, Vector3 position, Quaternion rotation, Vector3 inputVelocity)
    {
        server_projectileRequestQueue.Enqueue(new TProjectilePacket(connectionIndex, position, rotation, inputVelocity));
    }

    [Rpc(SendTo.Server)]
    public void SendMessageRpc(FixedString128Bytes message, int senderConnectionIndex)
    {
        TMessagePacket mp = new TMessagePacket();
        mp.Message = message.ToString();
        mp.SenderConnectionIndex = senderConnectionIndex;        
        server_MessageQueue.Enqueue(mp);        
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
