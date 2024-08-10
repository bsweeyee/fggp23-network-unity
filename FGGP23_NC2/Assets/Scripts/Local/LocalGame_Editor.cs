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
        private bool bIsDisplaySpawnPosition;
        private bool bDisplayNonNetworkCameraPosition;
        private bool bIsDisplayCameraPosition;
        private bool bIsDisplayCameraRotation;
        private bool bDisplayNonNetworkCameraRotation;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            LocalGame lg = (LocalGame)target;            

            EditorGUILayout.LabelField("Game Settings", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                bIsDisplaySpawnPosition = EditorGUILayout.Toggle(bIsDisplaySpawnPosition, GUILayout.Width(15));
                EditorGUILayout.LabelField("Spawn Positions and Radius", EditorStyles.miniBoldLabel);
            }
            if (bIsDisplaySpawnPosition)
            {
                for (int i=0; i< GameData.NUMBER_OF_PLAYERS; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {                                        
                        EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(10));
                        
                        var newPos = EditorGUILayout.Vector3Field("", lg.GameData.UnitSpawnPosition[i]);
                        if (newPos != lg.GameData.UnitSpawnPosition[i])
                        {
                            lg.GameData.UnitSpawnPosition[i] = newPos;
                            SerializeGameData(lg.GameData);
                        }

                        if (GUILayout.Button("Reset"))
                        {
                            lg.GameData.UnitSpawnPosition[i] = Vector3.zero;
                        }

                        var newRadius = EditorGUILayout.FloatField("", lg.GameData.UnitSpawnRadius[i]);
                        if (newRadius != lg.GameData.UnitSpawnRadius[i])
                        {
                            lg.GameData.UnitSpawnRadius[i] = newRadius;
                            SerializeGameData(lg.GameData);
                        }
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                bDisplayNonNetworkCameraPosition = EditorGUILayout.Toggle(bDisplayNonNetworkCameraPosition, GUILayout.Width(15));                
                EditorGUILayout.LabelField("Non-network Camera Position", EditorStyles.miniBoldLabel);
            }

            if (bDisplayNonNetworkCameraPosition)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var newPos = EditorGUILayout.Vector3Field("", lg.GameData.CameraNonNetworkSpawnPosition);
                    if (newPos != lg.GameData.CameraNonNetworkSpawnPosition)
                    {
                        lg.GameData.CameraNonNetworkSpawnPosition = newPos;
                        SerializeGameData(lg.GameData);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                bDisplayNonNetworkCameraRotation = EditorGUILayout.Toggle(bDisplayNonNetworkCameraRotation, GUILayout.Width(15));                
                EditorGUILayout.LabelField("Non-network Camera Rotation", EditorStyles.miniBoldLabel);
            }

            if (bDisplayNonNetworkCameraRotation)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var newEuler = EditorGUILayout.Vector3Field("", lg.GameData.CameraNonNetworkRotation.eulerAngles);
                    if (newEuler != lg.GameData.CameraNonNetworkRotation.eulerAngles)
                    {
                        lg.GameData.CameraNonNetworkRotation = Quaternion.Euler(newEuler);
                        SerializeGameData(lg.GameData);
                    }
                }
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                bIsDisplayCameraPosition = EditorGUILayout.Toggle(bIsDisplayCameraPosition, GUILayout.Width(15));                
                EditorGUILayout.LabelField("Camera Positions", EditorStyles.miniBoldLabel);
            }

            if (bIsDisplayCameraPosition)
            {
                for (int i=0; i< GameData.NUMBER_OF_PLAYERS; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {                                        
                        EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(10));
                        
                        var newPos = EditorGUILayout.Vector3Field("", lg.GameData.CameraSpawnPosition[i]);
                        if (newPos != lg.GameData.CameraSpawnPosition[i])
                        {
                            lg.GameData.CameraSpawnPosition[i] = newPos;
                            SerializeGameData(lg.GameData);
                        }
                        
                        if (GUILayout.Button("Reset"))
                        {
                            lg.GameData.CameraSpawnPosition[i] = Vector3.zero;
                        }
                        if (GUILayout.Button("Set To Position (Temp)"))
                        {
                            var camera = FindObjectOfType<Camera>();
                            camera.transform.position = lg.GameData.CameraSpawnPosition[i];
                        }
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                bIsDisplayCameraRotation = EditorGUILayout.Toggle(bIsDisplayCameraRotation, GUILayout.Width(15));                
                EditorGUILayout.LabelField("Camera Rotation", EditorStyles.miniBoldLabel);
            }
            
            if (bIsDisplayCameraRotation)
            {
                for (int i=0; i< GameData.NUMBER_OF_PLAYERS; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {                                        
                        EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(10));
                        
                        var newEuler = EditorGUILayout.Vector3Field("", lg.GameData.CameraRotation[i].eulerAngles);
                        if (newEuler != lg.GameData.CameraRotation[i].eulerAngles)
                        {
                            lg.GameData.CameraRotation[i] = Quaternion.Euler(newEuler);
                            SerializeGameData(lg.GameData);
                        }
                                            
                        if (GUILayout.Button("Reset"))
                        {
                            lg.GameData.CameraRotation[i] = Quaternion.identity;
                        }
                        if (GUILayout.Button("Set To Rotation (Temp)"))
                        {
                            var camera = FindObjectOfType<Camera>();
                            camera.transform.rotation = lg.GameData.CameraRotation[i];
                        }
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
                if (bIsDisplaySpawnPosition)
                {
                    for (int i=0; i<GameData.NUMBER_OF_PLAYERS; i++)
                    {
                        Vector3 newPos = Handles.PositionHandle(lg.GameData.UnitSpawnPosition[i], Quaternion.identity);
                        if (newPos != lg.GameData.UnitSpawnPosition[i])
                        {
                            lg.GameData.UnitSpawnPosition[i] = newPos;
                            Repaint();
                        }                    
                    }
                }

                if (bIsDisplayCameraPosition)
                {

                    for (int i=0; i<GameData.NUMBER_OF_PLAYERS; i++)
                    {
                        Vector3 newPos = Handles.PositionHandle(lg.GameData.CameraSpawnPosition[i], lg.GameData.CameraRotation[i]);                        

                        if (newPos != lg.GameData.CameraSpawnPosition[i])
                        {
                            lg.GameData.CameraSpawnPosition[i] = newPos;
                            Repaint();
                        }                        
                    }
                }

                if (bIsDisplayCameraRotation)
                {
                    for (int i=0; i<GameData.NUMBER_OF_PLAYERS; i++)
                    {
                        Quaternion newQuat = Handles.RotationHandle(lg.GameData.CameraRotation[i], lg.GameData.CameraSpawnPosition[i]);
                        Vector3 forward = newQuat * Vector3.forward;                        
                        Handles.color = Color.blue;
                        Handles.DrawLine(lg.GameData.CameraSpawnPosition[i], lg.GameData.CameraSpawnPosition[i] + forward.normalized * 5);                        

                        if (newQuat != lg.GameData.CameraRotation[i])
                        {
                            lg.GameData.CameraRotation[i] = newQuat;
                            Repaint();
                        }
                    }
                }                
            }

            if (EditorGUI.EndChangeCheck())
            {
                if(!Application.isPlaying) {
                    Undo.RecordObject(lg, "Change Spawn Position"); 
                    SerializeGameData(lg.GameData);                   
                }
                // generator.SpawnPoints = sp;
            }            
        }

        protected void SerializeGameData(GameData gd)
        {
            var so = new SerializedObject(gd);
            for (var i=0; i<gd.UnitSpawnPosition.Count; i++) {
                var element = so.FindProperty("unitSpawnPosition").GetArrayElementAtIndex(i);                        
                element.vector3Value = gd.UnitSpawnPosition[i];
            }

            var e = so.FindProperty("cameraNonNetworkSpawnPosition");
            e.vector3Value = gd.CameraNonNetworkSpawnPosition;

            e = so.FindProperty("cameraNonNetworkRotation");
            e.quaternionValue = gd.CameraNonNetworkRotation;

            for (var i=0; i<gd.CameraSpawnPosition.Count; i++) {
                var element = so.FindProperty("cameraSpawnPosition").GetArrayElementAtIndex(i);                        
                element.vector3Value = gd.CameraSpawnPosition[i];
            }

            for (var i=0; i<gd.CameraRotation.Count; i++) {
                var element = so.FindProperty("cameraRotation").GetArrayElementAtIndex(i);                        
                element.quaternionValue = gd.CameraRotation[i];
            }

            for (var i=0; i<gd.UnitSpawnRadius.Count; i++) {
                var element = so.FindProperty("unitSpawnRadius").GetArrayElementAtIndex(i);                        
                element.floatValue = gd.UnitSpawnRadius[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(so.targetObject);
        }
    }
    #endif
}
