using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Draw
{
    public enum EPrimitive
    {
        LINE,
        LINE2D,
        DISC
    }

    public struct TPrimitive
    {
        public EPrimitive PrimitiveID;
        public Vector3 Start;
        public Vector3 End;
        public Vector3 Normal;
        public float Radius;
        public Color Color;

        public TPrimitive(EPrimitive id, Vector3 start, Vector3 end, Color color)
        {
            this.PrimitiveID = id;
            this.Start = start;
            this.End = end;
            this.Color = color;
            this.Normal = Vector3.zero;
            this.Radius = 0;
        }
        public TPrimitive(EPrimitive id, Vector3 start, Vector3 normal, float radius, Color color)
        {
            this.PrimitiveID = id;
            this.Start = start;
            this.End = Vector3.zero;
            this.Color = color;
            this.Normal = normal;
            this.Radius = radius;
        }        
    }

    public class PrimitiveScope: System.IDisposable
    {                
        public static Queue<TPrimitive> DrawCommands = new Queue<TPrimitive>();         
        public static Material DefaultPrimitiveMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        
        public PrimitiveScope()
        {            
            Camera.onPostRender += OnPostRenderCallback; //NOTE: onPostRender will add delegate to SceneCamera if the tab is also opened

            DefaultPrimitiveMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            DefaultPrimitiveMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            DefaultPrimitiveMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            DefaultPrimitiveMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            DefaultPrimitiveMaterial.SetInt("_ZWrite", 0);                    
        }

        public void Dispose()
        {
            
        }

        void DrawLine(TPrimitive primitiveData)
        {
            DefaultPrimitiveMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);            

            GL.Color(primitiveData.Color);            
            GL.Vertex3(primitiveData.Start.x, primitiveData.Start.y, primitiveData.Start.z);            
            GL.Vertex3(primitiveData.End.x, primitiveData.End.y, primitiveData.End.z);            

            GL.End();
            GL.PopMatrix();
        }

        void DrawLine2D(TPrimitive primitiveData)
        {
            DefaultPrimitiveMaterial.SetPass(0);
            
            GL.PushMatrix();
            GL.LoadOrtho();

            GL.Begin(GL.LINES);
            GL.Color(primitiveData.Color);
            GL.Vertex(new Vector3(primitiveData.Start.x / Screen.width, primitiveData.Start.y / Screen.height, 0));
            GL.Vertex(new Vector3(primitiveData.End.x / Screen.width, primitiveData.End.y / Screen.height, 0));
            GL.End();

            GL.PopMatrix(); 
        }

        void DrawDisc(TPrimitive primitiveData)
        {
            DefaultPrimitiveMaterial.SetPass(0);
            int iterations = 100;
            
            Matrix4x4 m = new Matrix4x4();

            Vector3 r = Vector3.Cross(primitiveData.Normal.normalized, Vector3.up.normalized);
            Vector3 f = Vector3.Cross(primitiveData.Normal.normalized, r.normalized);
            r = r.normalized;
            f = f.normalized;

            m.SetColumn(0, new Vector4(r.x, r.y, r.z, 0));                
            m.SetColumn(1, new Vector4(primitiveData.Normal.x, primitiveData.Normal.y, primitiveData.Normal.z, 0));                
            m.SetColumn(2, new Vector4(f.x, f.y, f.z, 0));                
            m.SetColumn(3, new Vector4(0, 0, 0, 1));

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);
            
            for(int i=1; i<iterations; ++i)
            {
                float a0 = (i-1) / (float)iterations;
                float a1 = i / (float)iterations;
                float angle0 = a0 * Mathf.PI * 2;
                float angle1 = a1 * Mathf.PI * 2;                                                

                Vector3 p0 = m * new Vector3(Mathf.Cos(angle0) * primitiveData.Radius, Mathf.Sin(angle0) * primitiveData.Radius, 0);
                Vector3 p1 = m * new Vector3(Mathf.Cos(angle1) * primitiveData.Radius, Mathf.Sin(angle1) * primitiveData.Radius, 0);                                                

                Primitive.Line(primitiveData.Start + p0, primitiveData.Start + p1); 
            }

            GL.End();
            GL.PopMatrix();
        }        

        void OnPostRenderCallback(Camera camera)
        {
            if (camera == Camera.main)
            {
                while (DrawCommands.Count > 0)
                {
                    TPrimitive drawCommand = DrawCommands.Dequeue();
                    switch(drawCommand.PrimitiveID)
                    {
                        case EPrimitive.LINE:
                        DrawLine(drawCommand);
                        break;
                        case EPrimitive.LINE2D:
                        DrawLine2D(drawCommand);
                        break;
                        case EPrimitive.DISC:
                        DrawDisc(drawCommand);
                        break;
                    }
                }
            }
            DrawCommands.Clear();
            Camera.onPostRender -= OnPostRenderCallback;            
        }
    }

    public class Primitive
    {        
        public static void Line(Vector3 start, Vector3 end, string colorString = "#000000")
        {
            Color color;
            UnityEngine.ColorUtility.TryParseHtmlString(colorString, out color);           
            PrimitiveScope.DrawCommands.Enqueue(new TPrimitive(EPrimitive.LINE, start, end, color));
        }

        public static void Line(Vector3 start, Vector3 end, Color color)
        {            
            PrimitiveScope.DrawCommands.Enqueue(new TPrimitive(EPrimitive.LINE, start, end, color));
        }

        public static void Line2D(Vector3 screenSpaceStart, Vector3 screenSpaceEnd, string colorString = "#000000")
        {
            Color color;
            UnityEngine.ColorUtility.TryParseHtmlString(colorString, out color);
            PrimitiveScope.DrawCommands.Enqueue(new TPrimitive(EPrimitive.LINE2D, screenSpaceStart, screenSpaceEnd, color));                             
        }
        
        public static void Line2D(Vector3 screenSpaceStart, Vector3 screenSpaceEnd, Color color)
        {            
            PrimitiveScope.DrawCommands.Enqueue(new TPrimitive(EPrimitive.LINE2D, screenSpaceStart, screenSpaceEnd, color));                             
        }

        public static void Disc(Vector3 center, Vector3 normal, float radius, string colorString = "#000000")
        {
            Color color;
            UnityEngine.ColorUtility.TryParseHtmlString(colorString, out color);
            PrimitiveScope.DrawCommands.Enqueue(new TPrimitive(EPrimitive.DISC, center, normal, radius, color));
        }
    }
}
