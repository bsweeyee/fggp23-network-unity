using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IMDraw
{
    public enum EPrimitive
    {
        LINE_SDF,
        LINE,
        LINE2D,
        DISC,
        DISC_SDF
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

        public TPrimitive(EPrimitive id, Vector3 start, Vector3 end, Vector3 normal, float radius, Color color)
        {
            this.PrimitiveID = id;
            this.Start = start;
            this.End = end;
            this.Color = color;
            this.Normal = normal;
            this.Radius = radius;
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
        public static Material DefaultLineSDFMaterial = new Material(Shader.Find("IMDraw/IMDrawCapsuleSDF"));
        public static Material DefaultTorusSDFMaterial = new Material(Shader.Find("IMDraw/IMDrawTorusSDF"));
        public static Material DefaultPrimitiveMaterial = new Material(Shader.Find("IMDraw/IMDrawDefault"));

        public PrimitiveScope()
        {            
            Camera.onPostRender += OnPostRenderCallback; //NOTE: onPostRender will add delegate to SceneCamera if the tab is also opened

            DefaultLineSDFMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            DefaultLineSDFMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            DefaultLineSDFMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            DefaultLineSDFMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            DefaultLineSDFMaterial.SetInt("_ZWrite", 0);

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

        void DrawSDFLine(TPrimitive primitiveData)
        {
            float offset = 1.0f;
            float radius = primitiveData.Radius;
            DefaultLineSDFMaterial.SetPass(0);
            DefaultLineSDFMaterial.SetInt("_Type", 0);
            DefaultLineSDFMaterial.SetVector("_LineStart", new Vector4(primitiveData.Start.x,primitiveData.Start.y,primitiveData.Start.z,0));
            DefaultLineSDFMaterial.SetVector("_LineEnd", new Vector4(primitiveData.End.x,primitiveData.End.y,primitiveData.End.z,0));
            DefaultLineSDFMaterial.SetFloat("_Radius", radius);

            // TODO: Generate 3D AABB of a line
            Vector3 start = primitiveData.Start;
            Vector3 end = primitiveData.End;
            Vector3 l = end - start;
            float length = l.magnitude;

            Vector3 L = Vector3.ProjectOnPlane(l, Vector3.up);
            float lDotRNormalized = Vector3.Dot(L.normalized, Vector3.right);
            float lDotFNormalized = Vector3.Dot(L.normalized, Vector3.forward);
            float lDotUNormalized = Vector3.Dot(l.normalized, Vector3.up);

            Vector3 Lr = Vector3.Dot(L, Vector3.right) * Vector3.right;
            Vector3 Lf = Vector3.Dot(L, Vector3.forward) * Vector3.forward;
            Vector3 Lh = l - L;

            Vector3 A = (lDotRNormalized > 0) ? start : start + Lr;
            Vector3 B = (lDotRNormalized > 0) ? start + Lr : start;
            Vector3 C = (lDotRNormalized > 0) ? start + Lr + Lh : start + Lh;
            Vector3 D = (lDotRNormalized > 0) ? start + Lh : start + Lr + Lh;
            Vector3 E = (lDotRNormalized > 0) ? start + Lf + Lr + Lh : start + Lf + Lh;
            Vector3 F = (lDotRNormalized > 0) ? start + Lf + Lh : start + Lf + Lr + Lh;
            Vector3 G = (lDotRNormalized > 0) ? start + Lf : start + Lf + Lr;
            Vector3 H = (lDotRNormalized > 0) ? start + Lf + Lr : start + Lf;

            A = (lDotFNormalized > 0) ? A : A + Lf;
            B = (lDotFNormalized > 0) ? B : B + Lf;
            C = (lDotFNormalized > 0) ? C : C + Lf;
            D = (lDotFNormalized > 0) ? D : D + Lf;

            E = (lDotFNormalized > 0) ? E : E - Lf;
            F = (lDotFNormalized > 0) ? F : F - Lf;
            G = (lDotFNormalized > 0) ? G : G - Lf;
            H = (lDotFNormalized > 0) ? H : H - Lf;

            A = (lDotUNormalized > 0) ? A : A + Lh;
            B = (lDotUNormalized > 0) ? B : B + Lh;
            C = (lDotUNormalized > 0) ? C : C - Lh;       
            D = (lDotUNormalized > 0) ? D : D - Lh;       
            
            E = (lDotUNormalized > 0) ? E : E - Lh;
            F = (lDotUNormalized > 0) ? F : F - Lh;
            G = (lDotUNormalized > 0) ? G : G + Lh;       
            H = (lDotUNormalized > 0) ? H : H + Lh;

            Vector3 center = start + Lr/2 + Lf/2 + Lh/2;
            Vector3 A_direction = (A - center).normalized;
            Vector3 B_direction = (B - center).normalized;
            Vector3 C_direction = (C - center).normalized;
            Vector3 D_direction = (D - center).normalized;
            Vector3 E_direction = (E - center).normalized;
            Vector3 F_direction = (F - center).normalized;
            Vector3 G_direction = (G - center).normalized;
            Vector3 H_direction = (H - center).normalized;

            A += A_direction * radius* offset;                
            B += B_direction * radius* offset;                
            C += C_direction * radius* offset;                
            D += D_direction * radius* offset;                
            E += E_direction * radius* offset;                
            F += F_direction * radius* offset;                
            G += G_direction * radius* offset;                
            H += H_direction * radius* offset;

            float rIV = (lDotRNormalized > 0) ? Vector3Extension.InverseLerp(Vector3.right * length, Vector3.zero, Lr) : Vector3Extension.InverseLerp(-Vector3.right * length, Vector3.zero, Lr);
            float fIV = (lDotFNormalized > 0) ? Vector3Extension.InverseLerp(Vector3.forward * length, Vector3.zero, Lf) : Vector3Extension.InverseLerp(-Vector3.forward * length, Vector3.zero, Lf);
            float uIV = (lDotUNormalized > 0) ? Vector3Extension.InverseLerp(Vector3.up * length, Vector3.zero, Lh) : Vector3Extension.InverseLerp(-Vector3.up * length, Vector3.zero, Lh);

            A = A - fIV * Vector3.forward * radius - Mathf.Clamp(rIV, 0, 1) * Vector3.right * radius - uIV * Vector3.up * radius;
            B = B - fIV * Vector3.forward * radius + Mathf.Clamp(rIV, 0, 1) * Vector3.right * radius - uIV * Vector3.up * radius;
            C = C - fIV * Vector3.forward * radius + Mathf.Clamp(rIV, 0, 1) * Vector3.right * radius + uIV * Vector3.up * radius;
            D = D - fIV * Vector3.forward * radius - Mathf.Clamp(rIV, 0, 1) * Vector3.right * radius + uIV * Vector3.up * radius;
            E = E + fIV * Vector3.forward * radius + Mathf.Clamp(rIV, 0, 1) * Vector3.right * radius + uIV * Vector3.up * radius;
            F = F + fIV * Vector3.forward * radius - Mathf.Clamp(rIV, 0, 1) * Vector3.right * radius + uIV * Vector3.up * radius;
            G = G + fIV * Vector3.forward * radius - Mathf.Clamp(rIV, 0, 1) * Vector3.right * radius - uIV * Vector3.up * radius;
            H = H + fIV * Vector3.forward * radius + Mathf.Clamp(rIV, 0, 1) * Vector3.right * radius - uIV * Vector3.up * radius;

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);                   
            
            // draw the bounding mesh
            GL.Begin(GL.TRIANGLES);                         
            GL.Color(primitiveData.Color);                                    

            // Front face
            GL.Vertex3(A.x, A.y, A.z);
            GL.Vertex3(C.x, C.y, C.z);           
            GL.Vertex3(B.x, B.y, B.z);

            GL.Vertex3(A.x, A.y, A.z);
            GL.Vertex3(D.x, D.y, D.z);           
            GL.Vertex3(C.x, C.y, C.z);

            // Left face
            GL.Vertex3(B.x, B.y, B.z);
            GL.Vertex3(C.x, C.y, C.z);           
            GL.Vertex3(H.x, H.y, H.z);
                       
            GL.Vertex3(C.x, C.y, C.z);           
            GL.Vertex3(E.x, E.y, E.z);
            GL.Vertex3(H.x, H.y, H.z);

            // Right face
            GL.Vertex3(E.x, E.y, E.z);
            GL.Vertex3(G.x, G.y, G.z);           
            GL.Vertex3(H.x, H.y, H.z);           
            
            GL.Vertex3(E.x, E.y, E.z);
            GL.Vertex3(F.x, F.y, F.z);           
            GL.Vertex3(G.x, G.y, G.z);

            // Back face
            GL.Vertex3(D.x, D.y, D.z);
            GL.Vertex3(G.x, G.y, G.z);           
            GL.Vertex3(F.x, F.y, F.z);

            GL.Vertex3(D.x, D.y, D.z);
            GL.Vertex3(A.x, A.y, A.z);           
            GL.Vertex3(G.x, G.y, G.z);           
            
            // Top Face
            GL.Vertex3(D.x, D.y, D.z);
            GL.Vertex3(F.x, F.y, F.z);
            GL.Vertex3(E.x, E.y, E.z);           

            GL.Vertex3(C.x, C.y, C.z);           
            GL.Vertex3(D.x, D.y, D.z);
            GL.Vertex3(E.x, E.y, E.z);

            // Bottom Face
            GL.Vertex3(B.x, B.y, B.z);
            GL.Vertex3(H.x, H.y, H.z);
            GL.Vertex3(G.x, G.y, G.z);           

            GL.Vertex3(A.x, A.y, A.z);           
            GL.Vertex3(B.x, B.y, B.z);
            GL.Vertex3(G.x, G.y, G.z);           
                       
            // GL.Vertex3(primitiveData.Start.x, primitiveData.Start.y, primitiveData.Start.z);            
            // GL.Vertex3(primitiveData.End.x, primitiveData.End.y, primitiveData.End.z);            

            GL.End();

            GL.PopMatrix();
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

        void DrawSDFDisc(TPrimitive primitiveData)
        {
            DefaultTorusSDFMaterial.SetPass(0);

            Matrix4x4 m = new Matrix4x4();

            Vector3 u = primitiveData.Normal.normalized;
            Vector3 f = Vector3.Cross(Vector3.right.normalized, u.normalized).normalized;
            Vector3 r = Vector3.Cross(u.normalized, f.normalized).normalized;                            
            
            m.SetColumn(0, new Vector4(r.x, r.y, r.z, 0));                
            m.SetColumn(1, new Vector4(f.x, f.y, f.z, 0));                
            m.SetColumn(2, new Vector4(u.x, u.y, u.z, 0));                
            m.SetColumn(3, new Vector4(0, 0, 0, 1));            
            DefaultTorusSDFMaterial.SetMatrix("_RotationMatrix", Matrix4x4.Inverse(m));

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINE_STRIP);
                       
            GL.Color(primitiveData.Color);

           
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
                    case EPrimitive.LINE_SDF:
                    DrawSDFLine(drawCommand);
                    break;
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
        public static void LineSDF(Vector3 start, Vector3 end, float width, string colorString = "#000000")
        {
            Color color;
            UnityEngine.ColorUtility.TryParseHtmlString(colorString, out color);
            PrimitiveScope.DrawCommands.Enqueue(new TPrimitive(EPrimitive.LINE_SDF, start, end, Vector3.zero, width, color));
        }

        public static void LineSDF(Vector3 start, Vector3 end, float width, Color color)
        {
            PrimitiveScope.DrawCommands.Enqueue(new TPrimitive(EPrimitive.LINE_SDF, start, end, Vector3.zero, width, color));
        }

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
