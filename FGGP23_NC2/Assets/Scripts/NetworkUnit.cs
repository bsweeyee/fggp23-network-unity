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
    #region UNIT DATA    
    private int unitID;
    private ulong ownerID;
    #endregion

    #region SERVER_AND_CLIENT-READ, SERVER-WRITE NETWORK VARIABLES    
    public NetworkVariable<Vector3> MoveTarget = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Server);    
    public NetworkVariable<ENetworkUnitState> CurrentState = new NetworkVariable<ENetworkUnitState>(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> Health = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> LastAttackTime = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);
    #endregion

    MeshRenderer currentRenderer;

    public int UnitID
    {
        get { return unitID; }
    }
    public ulong OwnerID
    {
        get { return ownerID; }
    }

    ulong targetAttackOwnerID = 99;    

    private void OnEnable()
    {
        currentRenderer = GetComponentInChildren<MeshRenderer>();        
    }

    private void Update()
    {
        LocalUpdate(Time.deltaTime);
        ServerUpdate(Time.deltaTime);
    }      

    private void FixedUpdate()
    {
        FixedServerUpdate(Time.fixedDeltaTime);
    }

    private void LocalUpdate(float dt)
    {
        switch(CurrentState.Value)
        {
            case ENetworkUnitState.MOVE:
            if (NetworkManager.Singleton.LocalClientId == ownerID) 
            {                
                // we skip check if it is not the owner
                targetAttackOwnerID = SelectAttackOwnerID();
                if (targetAttackOwnerID < 99) ChangeStateRpc(ENetworkUnitState.ATTACK);
            }

            break;
            case ENetworkUnitState.ATTACK:
            if (NetworkManager.Singleton.LocalClientId == ownerID) 
            {                
                if (Time.time - LastAttackTime.Value > LocalGame.Instance.GameData.UnitAttackIntervalSeconds)
                {
                    ExecuteAttackRpc(targetAttackOwnerID);
                    targetAttackOwnerID = SelectAttackOwnerID();
                    if (targetAttackOwnerID >= 99) ChangeStateRpc(ENetworkUnitState.MOVE);                                    
                }
            }            
            break;
        }
    }

    private void ServerUpdate(float dt)
    {
        if (IsServer)
        {
            switch(CurrentState.Value)
            {
                case ENetworkUnitState.ATTACK:
                
                break;
                case ENetworkUnitState.MOVE:
                // TODO: move to use collision sphere when things get complicated
                var distanceFromTarget = (transform.position - MoveTarget.Value).magnitude;
                var spawnRadius =LocalGame.Instance.GameData.UnitSpawnRadius[(int)NetworkManager.Singleton.LocalClientId];                
                if (distanceFromTarget <= spawnRadius)
                {
                    ChangeStateRpc(ENetworkUnitState.IDLE);
                }                
                break;
            }                        
        }
    }   

    private void FixedServerUpdate(float dt)
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
        
    private ulong SelectAttackOwnerID()
    {
        Collider[] hs = Physics.OverlapSphere(transform.position, LocalGame.Instance.GameData.UnitAttackRadius, LocalGame.Instance.GameData.UnitAttackableLayer);
        Collider[] notOwnerHS = hs.Where( x=> x.GetComponent<NetworkUnit>().OwnerID != NetworkManager.Singleton.LocalClientId).ToArray();
        
        if (notOwnerHS.Length > 0)
        {
            int idx = UnityEngine.Random.Range(0, notOwnerHS.Length);
            return notOwnerHS[idx].GetComponent<NetworkUnit>().OwnerID;                
        }                
        return 99;
    }
    
    #region Network Callbacks
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();        
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        LocalGame.Instance.NetworkUnitInstances.Remove(unitID);
    }
    #endregion

    #region SERVER RPCS
    [Rpc(SendTo.Server)]
    public void ExecuteAttackRpc(ulong ownerID)
    {
        if (Time.time - LastAttackTime.Value > LocalGame.Instance.GameData.UnitAttackIntervalSeconds)
        {
            NetworkUnit owner = LocalGame.Instance.NetworkUnitInstances[unitID];                
            owner.GetComponent<NetworkUnit>().Health.Value -= LocalGame.Instance.GameData.UnitAttackStrength;                        
            if (owner.GetComponent<NetworkUnit>().Health.Value <= 0)
            {                            
                owner.GetComponent<NetworkUnit>().ChangeStateRpc(ENetworkUnitState.DEAD);
            }                
            LastAttackTime.Value = Time.time;            
        }
    }

    [Rpc(SendTo.Server)]
    public void ChangeStateRpc(ENetworkUnitState newState)
    {
        switch(newState)
        {
            case ENetworkUnitState.IDLE:
            break;
            case ENetworkUnitState.MOVE:
            break;
            case ENetworkUnitState.ATTACK:
            LastAttackTime.Value = Time.time;
            break;
            case ENetworkUnitState.DEAD:            
            Debug.Log($"[{unitID}]: DEAD");
            var owner = LocalGame.Instance.MyNetworkGameInstance;            
            owner.DespawnUnitRpc(unitID);
            break;
        }
        CurrentState.Value = newState;
    }
    #endregion 

    #region CLIENT_HOST RPCS
    // NOTE: i'm not sure if I should set position in Client and Host. This should be done in Server
    [Rpc(SendTo.ClientsAndHost)]
    public void InitializeRpc(ulong id, int unitID)
    {       
        this.unitID = unitID; 
        this.ownerID = id;
        LocalGame.Instance.NetworkUnitInstances.Add(unitID, this);
        if (NetworkManager.Singleton.LocalClientId == id)
        {
            transform.position = LocalGame.Instance.GameData.UnitSpawnPosition[0];
            currentRenderer.SetMaterials(new List<Material>{ FGNetworkProgramming.LocalGame.Instance.GameData.GameMaterials[0] } );
        }
        else
        {
            transform.position = LocalGame.Instance.GameData.UnitSpawnPosition[1];
            currentRenderer.SetMaterials(new List<Material>{ FGNetworkProgramming.LocalGame.Instance.GameData.GameMaterials[1] } );
        }
    }
    #endregion    
}
