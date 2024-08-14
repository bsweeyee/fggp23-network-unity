using System.Collections;
using System.Collections.Generic;
using FGNetworkProgramming;
using TMPro;
using UnityEngine;

public class GameSpawnHitArea : MonoBehaviour
{
    private int ownerConnectionIndex;
    private int unitSpawnIndex;
    public int UnitSpawnIndex { get { return unitSpawnIndex; } }
    public int OwnerConnectionIndex { get { return ownerConnectionIndex; }}
    
    private SphereCollider sphereCollider;

    // TODO: we might want to move this out into GameView for consistency
    private LinkedList<Canvas> damageTextInstances;        

    public void Initialize(int usi, Vector3 position, float radius)
    {
        this.unitSpawnIndex = usi;
        this.sphereCollider = gameObject.GetComponent<SphereCollider>();        
        sphereCollider.radius = radius;
        transform.position = position;

        int interval = Mathf.FloorToInt(LocalGame.Instance.GameData.UnitSpawnPosition.Count / GameData.NUMBER_OF_PLAYERS);        
        int offset = Mathf.FloorToInt(usi / interval);
        ownerConnectionIndex = offset;

        damageTextInstances = new LinkedList<Canvas>();

        Debug.Log($"Spawn area: {ownerConnectionIndex}, {usi}");
    }    

    void Update()
    {
        var dcf = damageTextInstances.First;
        while (dcf != null)
        {
            var next = dcf.Next;
            dcf.Value.GetComponentInChildren<TextMeshProUGUI>().GetComponent<RectTransform>().anchoredPosition += Vector2.up * 10.0f * Time.deltaTime;
            if (dcf.Value.GetComponentInChildren<TextMeshProUGUI>().GetComponent<RectTransform>().anchoredPosition.y > 0) 
            {                
                Destroy(dcf.Value.gameObject);
                damageTextInstances.Remove(dcf);                
            }
            dcf = next;
        }
    }
}
