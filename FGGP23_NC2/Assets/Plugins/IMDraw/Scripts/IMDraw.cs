using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

namespace IMDraw
{
    public enum EPrimitive
    {
        NONE,
        LINE_SDF,
        LINE,
        LINE2D,
        DISC,
        DISC_SDF,
        SPHERE_SDF,
        CONE_SDF
    }

    // TODO: at some point, look into how to separate different data struct for different types of primitive draw
    public class TPrimitive
    {
        public EPrimitive PrimitiveID;
        public Vector3 Start;
        public Vector3 End;
        public Vector3 Normal;
        public float R1;
        public float R2;
        public Color Color;

        private static IObjectPool<TPrimitive> pool = new ObjectPool<TPrimitive>(OnCreatePooledItem, OnTakeFromPool, OnReturnedPool);

        private TPrimitive()
        {
            this.PrimitiveID = EPrimitive.NONE;
            this.Start = Vector3.zero;
            this.End = Vector3.zero;
            this.Color = Color.black;
            this.Normal = Vector3.zero;
            this.R1 = 0;
            this.R2 = 0;
        }

        public static void Release(TPrimitive item)
        {
            pool.Release(item);
        }

        public static TPrimitive New(EPrimitive id, Vector3 start, Vector3 end, Color color)
        {
            TPrimitive p = pool.Get();
            p.PrimitiveID = id;
            p.Start = start;
            p.End = end;
            p.Color = color;
            p.Normal = Vector3.zero;
            p.R1 = 0;
            p.R2 = 0;

            return p;
        }

        public static TPrimitive New(EPrimitive id, Vector3 start, Vector3 end, Vector3 normal, float r1, Color color)
        {
            TPrimitive p = pool.Get();
            p.PrimitiveID = id;
            p.Start = start;
            p.End = end;
            p.Color = color;
            p.Normal = normal;
            p.R1 = r1;
            p.R2 = 0;

            return p;
        }

        public static TPrimitive New(EPrimitive id, Vector3 start, Vector3 normal, float r1, float r2, Color color)
        {
            TPrimitive p = pool.Get();
            p.PrimitiveID = id;
            p.Start = start;
            p.End = Vector3.zero;
            p.Color = color;
            p.Normal = normal;
            p.R1 = r1;
            p.R2 = r2;

            return p;
        }

        public static TPrimitive New(EPrimitive id, Vector3 start, float r1, Color color)
        {
            TPrimitive p = pool.Get();
            p.PrimitiveID = id;
            p.Start = start;
            p.End = Vector3.zero;
            p.Color = color;
            p.Normal = Vector3.zero;
            p.R1 = r1;
            p.R2 = 0;

            return p;
        }

        static TPrimitive OnCreatePooledItem()
        {
            TPrimitive item = new TPrimitive();

            return item;
        }

        static void OnReturnedPool(TPrimitive item)
        {
        }

        static void OnTakeFromPool(TPrimitive item)
        {

        }            
    }

    public static class PrimitiveScope
    {                
        public static Queue<TPrimitive> DrawCommands = new Queue<TPrimitive>();         
        public static Material DefaultLineSDFMaterial = new Material(Shader.Find("IMDraw/IMDrawCapsuleSDF"));
        public static Material DefaultTorusSDFMaterial = new Material(Shader.Find("IMDraw/IMDrawTorusSDF"));
        public static Material DefaultSphereSDFMaterial = new Material(Shader.Find("IMDraw/IMDrawSphereSDF"));
        public static Material DefaultConeSDFMaterial = new Material(Shader.Find("IMDraw/IMDrawConeSDF"));
        public static Material DefaultPrimitiveMaterial = new Material(Shader.Find("IMDraw/IMDrawDefault"));

        public static void Initialize()
        {
            Camera.onPostRender -= OnPostRenderCallback;
            Camera.onPostRender += OnPostRenderCallback; //NOTE: onPostRender will add delegate to SceneCamera if the tab is also opened
        }
        
        public static void BeginScope()
        {                                    
            // SDF Capsule material setting
            DefaultLineSDFMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            DefaultLineSDFMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            DefaultLineSDFMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            DefaultLineSDFMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            DefaultLineSDFMaterial.SetInt("_ZWrite", 0);
            
            // SDF Torus material setting
            DefaultTorusSDFMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            DefaultTorusSDFMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            DefaultTorusSDFMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            DefaultTorusSDFMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            DefaultTorusSDFMaterial.SetInt("_ZWrite", 0);

             // SDF Capsule material setting
            DefaultSphereSDFMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            DefaultSphereSDFMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            DefaultSphereSDFMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            DefaultSphereSDFMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            DefaultSphereSDFMaterial.SetInt("_ZWrite", 0);

            // Default material setting
            DefaultPrimitiveMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            DefaultPrimitiveMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            DefaultPrimitiveMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            DefaultPrimitiveMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            DefaultPrimitiveMaterial.SetInt("_ZWrite", 0);            
        }

        public static void EndScope()
        {
            
        }        

        static void DrawSDFLine(TPrimitive primitiveData)
        {
            float offset = 1.0f;
            float radius = primitiveData.R1;
           
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

            DefaultLineSDFMaterial.SetVector("_Start", new Vector4(primitiveData.Start.x,primitiveData.Start.y,primitiveData.Start.z,0));
            DefaultLineSDFMaterial.SetVector("_End", new Vector4(primitiveData.End.x,primitiveData.End.y,primitiveData.End.z,0));
            DefaultLineSDFMaterial.SetFloat("_Radius", radius);              
            
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity); 

            DefaultLineSDFMaterial.SetPass(0);
            
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

        static void DrawLine(TPrimitive primitiveData)
        {
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            DefaultPrimitiveMaterial.SetPass(0);

            GL.Begin(GL.LINES);
            GL.Color(primitiveData.Color);

            GL.Vertex3(primitiveData.Start.x, primitiveData.Start.y, primitiveData.Start.z);
            GL.Vertex3(primitiveData.End.x, primitiveData.End.y, primitiveData.End.z);                        
            
            GL.End();
            GL.PopMatrix();
        }

        static void DrawLine2D(TPrimitive primitiveData)
        {
            
            GL.PushMatrix();
            GL.LoadOrtho();

            DefaultPrimitiveMaterial.SetPass(0);

            GL.Begin(GL.LINES);
            GL.Color(primitiveData.Color);
            GL.Vertex(new Vector3(primitiveData.Start.x / Screen.width, primitiveData.Start.y / Screen.height, 0));
            GL.Vertex(new Vector3(primitiveData.End.x / Screen.width, primitiveData.End.y / Screen.height, 0));
            GL.End();

            GL.PopMatrix(); 
        }

        static void DrawDisc(TPrimitive primitiveData)
        {
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

            DefaultPrimitiveMaterial.SetPass(0);
            
            GL.Begin(GL.LINE_STRIP);
            
            for(int i=1; i<iterations+1; ++i)
            {
                float a0 = (i-1) / (float)iterations+1;
                float a1 = i / (float)iterations+1;
                float angle0 = a0 * Mathf.PI * 2;
                float angle1 = a1 * Mathf.PI * 2;

                Vector3 p0 = m * new Vector3(Mathf.Cos(angle0) * primitiveData.R1, Mathf.Sin(angle0) * primitiveData.R1, 0);
                Vector3 p1 = m * new Vector3(Mathf.Cos(angle1) * primitiveData.R1, Mathf.Sin(angle1) * primitiveData.R1, 0);                                                
                
                GL.Color(primitiveData.Color);            
                GL.Vertex3(primitiveData.Start.x + p0.x, primitiveData.Start.y + p0.y, primitiveData.Start.z + p0.z);            
                GL.Vertex3(primitiveData.Start.x + p1.x, primitiveData.Start.y + p1.y, primitiveData.Start.z + p1.z);                
            }

            GL.End();
            GL.PopMatrix();
        }        

        static void DrawSDFDisc(TPrimitive primitiveData)
        {
            Matrix4x4 m = new Matrix4x4();
            Matrix4x4 mt = new Matrix4x4();

            Vector3 u = primitiveData.Normal.normalized;
            Vector3 f = Vector3.Cross(Vector3.right.normalized, u.normalized).normalized;
            f =  (Mathf.Abs(Vector3.Dot(u.normalized, Vector3.right.normalized)) < 0.99f) ? f : Vector3.forward;
            Vector3 r = Vector3.Cross(u.normalized, f.normalized).normalized;                            
            
            m.SetColumn(0, new Vector4(r.x, r.y, r.z, 0));                
            m.SetColumn(1, new Vector4(u.x, u.y, u.z, 0));                
            m.SetColumn(2, new Vector4(f.x, f.y, f.z, 0));                
            m.SetColumn(3, new Vector4(0, 0, 0, 1));

            mt.SetColumn(0, new Vector4(1, 0, 0, 0));
            mt.SetColumn(1, new Vector4(0, 1, 0, 0));
            mt.SetColumn(2, new Vector4(0, 0, 1, 0));
            mt.SetColumn(3, new Vector4(-primitiveData.Start.x, -primitiveData.Start.y, -primitiveData.Start.z, 1));

            Vector3 start = primitiveData.Start;
            float radius = primitiveData.R1;
            float minorRadius = primitiveData.R2;
            float offset = (radius + minorRadius) * 1.0f;

            Vector3 A = start + Vector3.right * offset + Vector3.up * offset + Vector3.forward * offset;
            Vector3 B = start + Vector3.right * offset + Vector3.up * offset - Vector3.forward * offset;
            Vector3 C = start + Vector3.right * offset - Vector3.up * offset - Vector3.forward * offset;
            Vector3 D = start + Vector3.right * offset - Vector3.up * offset + Vector3.forward * offset;
            
            Vector3 E = start - Vector3.right * offset - Vector3.up * offset - Vector3.forward * offset;
            Vector3 F = start - Vector3.right * offset - Vector3.up * offset + Vector3.forward * offset;
            Vector3 G = start - Vector3.right * offset + Vector3.up * offset + Vector3.forward * offset;
            Vector3 H = start - Vector3.right * offset + Vector3.up * offset - Vector3.forward * offset;

            // m.SetColumn(0, new Vector4(1, 0, 0, 0));                
            // m.SetColumn(1, new Vector4(0, 1, 0, 0));                
            // m.SetColumn(2, new Vector4(0, 0, 1, 0));                
            // m.SetColumn(3, new Vector4(0, 0, 0, 1));

            // m = Matrix4x4.identity;
            DefaultTorusSDFMaterial.SetFloat("_MajorRadius", radius);            
            DefaultTorusSDFMaterial.SetFloat("_MinorRadius", minorRadius);            
            DefaultTorusSDFMaterial.SetMatrix("_InverseTransformMatrix", Matrix4x4.Inverse(m));
            DefaultTorusSDFMaterial.SetMatrix("_TranslationMatrix", mt);

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);  

            DefaultTorusSDFMaterial.SetPass(0);                 
            
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

        static void DrawSDFSphere(TPrimitive primitiveData)
        {
            Vector3 start = primitiveData.Start;
            float radius = primitiveData.R1;

            float offset = radius * 1.0f;

            Vector3 A = start + Vector3.right * offset + Vector3.up * offset + Vector3.forward * offset;
            Vector3 B = start + Vector3.right * offset + Vector3.up * offset - Vector3.forward * offset;
            Vector3 C = start + Vector3.right * offset - Vector3.up * offset - Vector3.forward * offset;
            Vector3 D = start + Vector3.right * offset - Vector3.up * offset + Vector3.forward * offset;
            
            Vector3 E = start - Vector3.right * offset - Vector3.up * offset - Vector3.forward * offset;
            Vector3 F = start - Vector3.right * offset - Vector3.up * offset + Vector3.forward * offset;
            Vector3 G = start - Vector3.right * offset + Vector3.up * offset + Vector3.forward * offset;
            Vector3 H = start - Vector3.right * offset + Vector3.up * offset - Vector3.forward * offset;                    

            // Matrix4x4 mt = new Matrix4x4();
            // mt.SetColumn(0, new Vector4(1, 0, 0, 0));
            // mt.SetColumn(1, new Vector4(0, 1, 0, 0));
            // mt.SetColumn(2, new Vector4(0, 0, 1, 0));
            // mt.SetColumn(3, new Vector4(-start.x, -start.y, -start.z, 1));
            // DefaultSphereSDFMaterial.SetMatrix("_TranslationMatrix", mt); 

            DefaultSphereSDFMaterial.SetFloat("_Radius", radius); 
            DefaultSphereSDFMaterial.SetVector("_Origin", start); 

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);  

            DefaultSphereSDFMaterial.SetPass(0);                 
            
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

        static void DrawSDFCone(TPrimitive primitiveData)
        {
            Vector3 start = primitiveData.Start;
            Vector3 normal = primitiveData.Normal;

            float a = primitiveData.R1;
            float h = primitiveData.R2;

            float maxH = h/2;

            float offset = 1.2f;

            Matrix4x4 m = new Matrix4x4();
            Matrix4x4 mt = new Matrix4x4();

            Vector3 u = normal;
            Vector3 f = Vector3.Cross(Vector3.right.normalized, u.normalized).normalized;
            f =  (Mathf.Abs(Vector3.Dot(u.normalized, Vector3.right.normalized)) < 0.99f) ? f : Vector3.forward;
            Vector3 r = Vector3.Cross(u.normalized, f.normalized).normalized;                            
            
            m.SetColumn(0, new Vector4(r.x, r.y, r.z, 0));                
            m.SetColumn(1, new Vector4(u.x, u.y, u.z, 0));                
            m.SetColumn(2, new Vector4(f.x, f.y, f.z, 0));                
            m.SetColumn(3, new Vector4(0, 0, 0, 1));

            mt.SetColumn(0, new Vector4(1, 0, 0, 0));
            mt.SetColumn(1, new Vector4(0, 1, 0, 0));
            mt.SetColumn(2, new Vector4(0, 0, 1, 0));
            mt.SetColumn(3, new Vector4(-start.x, -start.y, -start.z, 1));

            Vector3 A = start + Vector3.right * maxH * offset + Vector3.up * maxH * offset + Vector3.forward * maxH * offset;
            Vector3 B = start + Vector3.right * maxH * offset + Vector3.up * maxH * offset - Vector3.forward * maxH * offset;
            Vector3 C = start + Vector3.right * maxH * offset - Vector3.up * maxH * offset - Vector3.forward * maxH * offset;
            Vector3 D = start + Vector3.right * maxH * offset - Vector3.up * maxH * offset + Vector3.forward * maxH * offset;
            
            Vector3 E = start - Vector3.right * maxH * offset - Vector3.up * maxH * offset - Vector3.forward * maxH * offset;
            Vector3 F = start - Vector3.right * maxH * offset - Vector3.up * maxH * offset + Vector3.forward * maxH * offset;
            Vector3 G = start - Vector3.right * maxH * offset + Vector3.up * maxH * offset + Vector3.forward * maxH * offset;
            Vector3 H = start - Vector3.right * maxH * offset + Vector3.up * maxH * offset - Vector3.forward * maxH * offset;                    

            DefaultConeSDFMaterial.SetFloat("_Angle", a); 
            DefaultConeSDFMaterial.SetFloat("_H", h);
            DefaultConeSDFMaterial.SetMatrix("_InverseTransformMatrix", Matrix4x4.Inverse(m)); 
            DefaultConeSDFMaterial.SetMatrix("_TranslationMatrix", mt); 

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);  

            DefaultConeSDFMaterial.SetPass(0);                 
            
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

        static void OnPostRenderCallback(Camera camera)
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
                    case EPrimitive.DISC_SDF:
                    DrawSDFDisc(drawCommand);
                    break;
                    case EPrimitive.SPHERE_SDF:
                    DrawSDFSphere(drawCommand);
                    break;
                    case EPrimitive.CONE_SDF:
                    DrawSDFCone(drawCommand);
                    break;
                }
                TPrimitive.Release(drawCommand);
            }
            DrawCommands.Clear();
        }        
    }

    public class Primitive
    {        
        public static void LineSDF(Vector3 start, Vector3 end, float width, string colorString = "#000000")
        {
            Color color;
            UnityEngine.ColorUtility.TryParseHtmlString(colorString, out color);                                    
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.LINE_SDF, start, end, Vector3.zero, width, color));
        }

        public static void LineSDF(Vector3 start, Vector3 end, float width, Color color)
        {
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.LINE_SDF, start, end, Vector3.zero, width, color));
        }

        public static void Line(Vector3 start, Vector3 end, string colorString = "#000000")
        {
            Color color;
            UnityEngine.ColorUtility.TryParseHtmlString(colorString, out color);
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.LINE, start, end, color));
        }

        public static void Line(Vector3 start, Vector3 end, Color color)
        {
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.LINE, start, end, color));
        }

        public static void Line2D(Vector3 screenSpaceStart, Vector3 screenSpaceEnd, string colorString = "#000000")
        {
            Color color;
            UnityEngine.ColorUtility.TryParseHtmlString(colorString, out color);
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.LINE2D, screenSpaceStart, screenSpaceEnd, color));                             
        }
        
        public static void Line2D(Vector3 screenSpaceStart, Vector3 screenSpaceEnd, Color color)
        {            
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.LINE2D, screenSpaceStart, screenSpaceEnd, color));                             
        }

        public static void Disc(Vector3 center, Vector3 normal, float radius, string colorString = "#000000")
        {
            Color color;
            UnityEngine.ColorUtility.TryParseHtmlString(colorString, out color);
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.DISC, center, normal, radius, 0, color));
        }

        public static void Disc(Vector3 center, Vector3 normal, float radius, Color color)
        {            
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.DISC, center, normal, radius, 0, color));
        }

        public static void DiscSDF(Vector3 center, Vector3 normal, float radius, float minorRadius, string colorString = "#000000")
        {
            Color color;
            UnityEngine.ColorUtility.TryParseHtmlString(colorString, out color);
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.DISC_SDF, center, normal, radius, minorRadius, color));
        }

        public static void DiscSDF(Vector3 center, Vector3 normal, float radius, float minorRadius, Color color)
        {            
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.DISC_SDF, center, normal, radius, minorRadius, color));
        }

        public static void SphereSDF(Vector3 center, float radius, string colorString = "#000000")
        {
            Color color;
            UnityEngine.ColorUtility.TryParseHtmlString(colorString, out color);
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.SPHERE_SDF, center, radius, color));
        }

        public static void SphereSDF(Vector3 center, float radius, Color color)
        {
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.SPHERE_SDF, center, radius, color));
        }

        public static void ConeSDF(Vector3 center, Vector3 normal, float angle, float height, string colorString = "#000000")
        {
            if (normal.magnitude < 0.01f) return;
            Color color;
            UnityEngine.ColorUtility.TryParseHtmlString(colorString, out color);
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.CONE_SDF, center, normal, angle, height, color));
        }

        public static void ConeSDF(Vector3 center, Vector3 normal, float angle, float height, Color color)
        {
            if (normal.magnitude < 0.01f) return;
            PrimitiveScope.DrawCommands.Enqueue(TPrimitive.New(EPrimitive.CONE_SDF, center, normal, angle, height, color));
        }
    }
}
