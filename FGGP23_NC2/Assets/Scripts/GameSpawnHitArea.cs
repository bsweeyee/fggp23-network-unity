using System.Collections;
using System.Collections.Generic;
using FGNetworkProgramming;
using UnityEngine;

public class GameSpawnHitArea : MonoBehaviour
{
    private int ownerConnectionIndex;
    private int unitSpawnIndex;
    public int UnitSpawnIndex { get { return unitSpawnIndex; } }
    public int OwnerConnectionIndex { get { return ownerConnectionIndex; }}
    
    private SphereCollider sphereCollider;

    public void Initialize(int usi, Vector3 position, float radius)
    {
        this.unitSpawnIndex = usi;
        this.sphereCollider = gameObject.GetComponent<SphereCollider>();        
        sphereCollider.radius = radius;
        transform.position = position;

        int interval = Mathf.FloorToInt(LocalGame.Instance.GameData.UnitSpawnPosition.Count / GameData.NUMBER_OF_PLAYERS);        
        int offset = Mathf.FloorToInt(usi / interval);
        ownerConnectionIndex = offset;

        Debug.Log($"Spawn area: {ownerConnectionIndex}, {usi}");
    }          
}
