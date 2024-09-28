using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RVOTest : MonoBehaviour
{    
    GameObject[] agents;
    void Start()
    {
        agents = new GameObject[2];
        Vector3 camPos = Camera.main.transform.position;
        Vector3 agentPos = new Vector3(camPos.x, camPos.y, 0);
        for (int i=0; i<agents.Length; i++)
        {
            agents[i] = new GameObject($"{i}");
            var rvo = agents[i].AddComponent<RVOAgent>();
            var sc = agents[i].AddComponent<SphereCollider>();
            sc.radius = rvo.Radius;                    

            int spawnYDirection = (i%2 == 0) ? 1 : -1;
            int spawnXDirection = (i%2 == 0) ? 1 : -1;
            agents[i].transform.position = agentPos + new Vector3(spawnXDirection * 2f * Mathf.CeilToInt(i/2), spawnYDirection * 2.5f, 0);
        }

        for (int i=0; i<agents.Length; i++)
        {
            var rvo = agents[i].GetComponent<RVOAgent>();
            rvo.Goal = agents[(i+1)%agents.Length].transform.position;
        }
    }

    void OnGUI()
    {
        foreach(var a in agents)
        {
            var rvo = a.GetComponent<RVOAgent>();
            GUILayout.Label($"{rvo.name}: {rvo.iVelocity.magnitude}");
        }
    }        
}
