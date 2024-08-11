using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using FGNetworkProgramming;
using System.Runtime.CompilerServices;
using System.Data.Common;
using System.Linq;

public enum ENetworkUnitState
{
    NONE,
    IDLE,
    MOVE,
    ATTACK,
    DEAD
}

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
public class NetworkUnit : NetworkBehaviour
{
    #region SERVER_AND_CLIENT-READ, SERVER-WRITE NETWORK VARIABLES    
    public NetworkVariable<int> UnitID = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);
    // NOTE: We take connection index plus 1 because we want to trigger OnValueChanged callback when it is first set ( by default is 0 ). If OnValueChanged does not trigger, we cannot assign materials correctly
    public NetworkVariable<int> OwnerConnectionIndexPlusOne = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<Vector3> MoveTarget = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Server);    
    public NetworkVariable<ENetworkUnitState> CurrentState = new NetworkVariable<ENetworkUnitState>(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> Health = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);
    #endregion
    
    Queue<int> server_unitIDAttackRequestBuffer; // we have to store a buffer of when a unit request for an attack because client trigger time does not sync up with server trigger time. So there may be situations where client's request to attack is dropped if server time does not match
    float server_lastAttackTime = 0; // we store a server-side last attack time to ensure that when client triggers an attack, server also agrees that the cooldown for last attack has been cleared
    float client_lastAttackTime = 0; // we store a client-side last attack time to do a client side overlap sphere check to determine who to attack

    MeshRenderer currentRenderer;
    Collider currentCollider;

    int targetUnitID = int.MaxValue;


    private void OnEnable()
    {
        currentRenderer = GetComponentInChildren<MeshRenderer>();
        currentCollider = GetComponent<SphereCollider>();

        currentCollider.enabled = false;
        currentRenderer.enabled = false;

        server_unitIDAttackRequestBuffer = new Queue<int>();
                
        UnitID.OnValueChanged += (int previousValue, int newValue) => {
            LocalGame.Instance.NetworkUnitInstances.Add(newValue, this);            
            currentCollider.enabled = true;
            currentRenderer.enabled = true;        
        };
        OwnerConnectionIndexPlusOne.OnValueChanged += (int previousValue, int newValue) => {
            if (LocalGame.Instance.LocalConnectionIndex == newValue - 1)
            {
                currentRenderer.SetMaterials(new List<Material>{ LocalGame.Instance.GameData.GameMaterials[0] } );            
            }
            else
            {
                currentRenderer.SetMaterials(new List<Material>{ LocalGame.Instance.GameData.GameMaterials[1] } );
            }
        };        
    }

    private void Update()
    {
        switch(LocalGame.Instance.CurrentState)
        {
            case EGameState.PLAY:
            OnPlayStateLocalUpdate(Time.deltaTime);
            OnPlayServerUpdate(Time.deltaTime);
            break;
        }
    }      

    private void FixedUpdate()
    {
        switch(LocalGame.Instance.CurrentState)
        {
            case EGameState.PLAY:
            OnPlayFixedServerUpdate(Time.fixedDeltaTime);
            break;
        }
    }

    private void OnPlayStateLocalUpdate(float dt)
    {
        switch(CurrentState.Value)
        {
            case ENetworkUnitState.MOVE:
            if (LocalGame.Instance.LocalConnectionIndex == (OwnerConnectionIndexPlusOne.Value - 1)) 
            {                
                // we skip check if it is not the owner
                targetUnitID = SelectAttackTarget();
                if (targetUnitID < int.MaxValue) 
                {
                    ChangeState(ENetworkUnitState.ATTACK);
                }
            }

            break;
            case ENetworkUnitState.ATTACK:
            if (LocalGame.Instance.LocalConnectionIndex == (OwnerConnectionIndexPlusOne.Value - 1)) 
            {
                if (Time.time - client_lastAttackTime > LocalGame.Instance.GameData.UnitAttackIntervalSeconds)
                {
                    ExecuteAttackRpc(targetUnitID);
                    targetUnitID = SelectAttackTarget();
                    if (targetUnitID >= int.MaxValue) ChangeState(ENetworkUnitState.MOVE);                                    
                    client_lastAttackTime = Time.time;
                }
            }            
            break;
        }
    }

    private void OnPlayServerUpdate(float dt)
    {
        if (IsServer)
        {
            switch(CurrentState.Value)
            {
                case ENetworkUnitState.ATTACK:
                if (server_unitIDAttackRequestBuffer.Count > 0)
                {
                    if (Time.time - server_lastAttackTime > LocalGame.Instance.GameData.UnitAttackIntervalSeconds)
                    {
                        var tUnitID = server_unitIDAttackRequestBuffer.Dequeue();
                        if (LocalGame.Instance.NetworkUnitInstances.ContainsKey(tUnitID))
                        {
                            NetworkUnit unit = LocalGame.Instance.NetworkUnitInstances[tUnitID];                
                            unit.GetComponent<NetworkUnit>().Health.Value -= LocalGame.Instance.GameData.UnitAttackStrength;                        
                            if (unit.GetComponent<NetworkUnit>().Health.Value <= 0)
                            {                            
                                unit.GetComponent<NetworkUnit>().ChangeState(ENetworkUnitState.DEAD);
                            }                
                        } else if (tUnitID < int.MaxValue && tUnitID >= int.MaxValue - GameData.NUMBER_OF_PLAYERS)
                        {
                            var id = int.MaxValue - tUnitID - 1;                        
                            var ng = LocalGame.Instance.NetworkGameInstances.Find( x => x.ConnectionIndex.Value == id);
                            ng.PlayerHealth.Value -= LocalGame.Instance.GameData.UnitAttackStrength;                    
                        }
                        server_unitIDAttackRequestBuffer.Clear();
                        server_lastAttackTime = Time.time;                       
                    } 
                }                
                break;
                case ENetworkUnitState.MOVE:
                // TODO: move to use collision sphere when things get complicated
                var distanceFromTarget = (transform.position - MoveTarget.Value).magnitude;
                var spawnRadius =LocalGame.Instance.GameData.UnitSpawnRadius[(int)NetworkManager.Singleton.LocalClientId];                
                if (distanceFromTarget <= spawnRadius)
                {
                    ChangeState(ENetworkUnitState.IDLE);
                }                
                break;
            }
        }
    }   

    private void OnPlayFixedServerUpdate(float dt)
    {
        if (IsServer)
        {
            switch(CurrentState.Value)
            {
                case ENetworkUnitState.IDLE:
                break;
                case ENetworkUnitState.MOVE:
                Vector3 ownPosition = transform.position;
                var moveDirection = (MoveTarget.Value - ownPosition).normalized;
                transform.position += moveDirection * dt;                
                break;
            }
        }
    }
        
    private int SelectAttackTarget()
    {
        Collider[] ps = Physics.OverlapSphere(transform.position, LocalGame.Instance.GameData.UnitAttackRadius, LocalGame.Instance.GameData.PlayerAttackableLayer);        
        Collider[] hs = Physics.OverlapSphere(transform.position, LocalGame.Instance.GameData.UnitAttackRadius, LocalGame.Instance.GameData.UnitAttackableLayer);
        
        Collider[] notOwnerHS = hs.Where( x=> (x.GetComponent<NetworkUnit>().OwnerConnectionIndexPlusOne.Value - 1) != LocalGame.Instance.LocalConnectionIndex).ToArray();                   
        Collider[] notFriendlyPS = ps.Where(x => x.GetComponent<GameSpawnHitArea>().OwnerConnectionIndex != (OwnerConnectionIndexPlusOne.Value - 1)).ToArray();

        
        if (notOwnerHS.Length > 0)
        {
            int idx = UnityEngine.Random.Range(0, notOwnerHS.Length);
            int uID = notOwnerHS[idx].GetComponent<NetworkUnit>().UnitID.Value;
            return notOwnerHS[idx].GetComponent<NetworkUnit>().UnitID.Value;                
        } else if (notFriendlyPS.Length > 0)
        {
            int idx = UnityEngine.Random.Range(0, notFriendlyPS.Length);
            int uID = notFriendlyPS[idx].GetComponent<GameSpawnHitArea>().OwnerConnectionIndex + 1;                                     
            return int.MaxValue - uID; // we return the ownerConnectionIndex instead of Unit index if we are hitting the player
        }                
        return int.MaxValue;
    }

    public void ChangeState(ENetworkUnitState newState)
    {
        switch(newState)
        {
            case ENetworkUnitState.MOVE:
            client_lastAttackTime = Time.time;
            break;
            case ENetworkUnitState.ATTACK:
            break;
            case ENetworkUnitState.DEAD:
            break;
        }
        ChangeStateRpc(newState);
    }
    
    #region Network Callbacks
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();        
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        LocalGame.Instance.NetworkUnitInstances.Remove(UnitID.Value);
    }
    #endregion

    #region SERVER RPCS
    [Rpc(SendTo.Server)]
    public void ExecuteAttackRpc(int tUnitID)
    {
        server_unitIDAttackRequestBuffer.Enqueue(tUnitID);               
    }

    [Rpc(SendTo.Server)]
    private void ChangeStateRpc(ENetworkUnitState newState)
    {
        switch(newState)
        {
            case ENetworkUnitState.IDLE:
            break;
            case ENetworkUnitState.MOVE:
            server_lastAttackTime = Time.time;
            break;
            case ENetworkUnitState.ATTACK:
            break;
            case ENetworkUnitState.DEAD:            
            var owner = LocalGame.Instance.MyNetworkGameInstance;            
            owner.DespawnUnitRpc(UnitID.Value);
            break;
        }
        CurrentState.Value = newState;
    }
    #endregion    
}
