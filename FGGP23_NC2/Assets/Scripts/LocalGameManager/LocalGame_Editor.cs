using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FGNetworkProgramming 
{
    #if UNITY_EDITOR
    [CustomEditor(typeof(LocalGame))]
    public class LocalGame_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            LocalGame lg = (LocalGame)target;            

            EditorGUILayout.LabelField("Game Settings", EditorStyles.boldLabel);
            for (int i=0; i< lg.GameData.UnitSpawnPosition.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.TextArea(lg.GameData.UnitSpawnPosition[i].ToString());                    
                    if (GUILayout.Button("Reset"))
                    {
                        lg.GameData.UnitSpawnPosition[i] = Vector3.zero;
                    }
                }
            }
        }
        protected virtual void OnSceneGUI()        
        {
            LocalGame lg = (LocalGame)target;
            EditorGUI.BeginChangeCheck();

            Handles.color = Color.red;
            Gizmos.color = Color.red;

            if (!Application.isPlaying)
            {                
                for (int i=0; i< lg.GameData.UnitSpawnPosition.Count; i++)
                {
                    Vector3 newPos = Handles.PositionHandle(lg.GameData.UnitSpawnPosition[i], Quaternion.identity);
                    if (newPos != lg.GameData.UnitSpawnPosition[i])
                    {
                        lg.GameData.UnitSpawnPosition[i] = newPos;
                        Repaint();
                    }                    
                }
            }
        }
    }
    #endif
}
