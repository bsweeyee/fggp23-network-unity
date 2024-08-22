using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace IMDraw
{
    public enum EPrimitive
    {
        LINE,
        LINE2D,
        DISC
    }

    // TODO: at some point, look into how to separate different data struct for different types of primitive draw
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
        public static Material DefaultPrimitiveMaterial = new Material(Shader.Find("Hidden/IMDrawLine"));
        
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
            float radius = 0.1f;
            DefaultPrimitiveMaterial.SetPass(0);
            DefaultPrimitiveMaterial.SetVector("_LineStart", new Vector4(primitiveData.Start.x,primitiveData.Start.y,primitiveData.Start.z,0));
            DefaultPrimitiveMaterial.SetVector("_LineEnd", new Vector4(primitiveData.End.x,primitiveData.End.y,primitiveData.End.z,0));
            DefaultPrimitiveMaterial.SetFloat("_Radius", radius);

            // TODO: Generate 3D AABB of a line
            Vector3 l = primitiveData.End - primitiveData.Start;
            Vector3 L = Vector3.ProjectOnPlane(l, Vector3.up);
            Vector3 h = l - L;

            Vector3 A = primitiveData.Start + L + Vector3.Cross(l, L).normalized * radius;
            Vector3 B = primitiveData.Start + L + Vector3.Cross(L, l).normalized * radius;
            Vector3 C = primitiveData.Start + L + h + Vector3.Cross(L, l).normalized * radius;
            Vector3 D = primitiveData.Start + L + h + Vector3.Cross(l, L).normalized * radius;
            Vector3 E = primitiveData.Start + h + Vector3.Cross(L, l).normalized * radius;
            Vector3 F = primitiveData.Start + h + Vector3.Cross(l, L).normalized * radius;
            Vector3 G = primitiveData.Start + Vector3.Cross(l, L).normalized * radius;
            Vector3 H = primitiveData.Start + Vector3.Cross(L, l).normalized * radius;
            
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.TRIANGLES);            
            // GL.Begin(GL.LINES);            

            GL.Color(primitiveData.Color);                                    
                       
            // GL.Vertex3(primitiveData.Start.x, primitiveData.Start.y, primitiveData.Start.z);            
            // GL.Vertex3(primitiveData.End.x, primitiveData.End.y, primitiveData.End.z);            

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

            Vector3 u = primitiveData.Normal.normalized;            
            Vector3 f = Vector3.Cross(Vector3.right.normalized, u.normalized).normalized;
            Vector3 r = Vector3.Cross(u.normalized, f.normalized).normalized;           
                            
            
            m.SetColumn(0, new Vector4(r.x, r.y, r.z, 0));                
            m.SetColumn(1, new Vector4(f.x, f.y, f.z, 0));                
            m.SetColumn(2, new Vector4(u.x, u.y, u.z, 0));                
            m.SetColumn(3, new Vector4(0, 0, 0, 1));

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINE_STRIP);
            
            for(int i=1; i<iterations+1; ++i)
            {
                float a0 = (i-1) / (float)iterations+1;
                float a1 = i / (float)iterations+1;
                float angle0 = a0 * Mathf.PI * 2;
                float angle1 = a1 * Mathf.PI * 2;

                Vector3 p0 = m * new Vector3(Mathf.Cos(angle0) * primitiveData.Radius, Mathf.Sin(angle0) * primitiveData.Radius, 0);
                Vector3 p1 = m * new Vector3(Mathf.Cos(angle1) * primitiveData.Radius, Mathf.Sin(angle1) * primitiveData.Radius, 0);                                                
                
                GL.Color(primitiveData.Color);            
                GL.Vertex3(primitiveData.Start.x + p0.x, primitiveData.Start.y + p0.y, primitiveData.Start.z + p0.z);            
                GL.Vertex3(primitiveData.Start.x + p1.x, primitiveData.Start.y + p1.y, primitiveData.Start.z + p1.z);                
            }

            GL.End();
            GL.PopMatrix();
        }        

        void OnPostRenderCallback(Camera camera)
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

        public static void Disc(Vector3 center, Vector3 normal, float radius, Color color)
        {            
            PrimitiveScope.DrawCommands.Enqueue(new TPrimitive(EPrimitive.DISC, center, normal, radius, color));
        }
    }
}
