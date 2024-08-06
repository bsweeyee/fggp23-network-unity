using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FGNetworkProgramming
{
    public class Game : MonoBehaviour
    {
        void Awake()
        {
            Input.Instance.Initialize();            
        }

        void OnGUI()
        {
            if (GUILayout.Button("Host"))
            {

            }

            if (GUILayout.Button("Join"))
            {
                
            }

            if (GUILayout.Button("Quit"))
            {
                Application.Quit();
            }
        }
    }
}
