using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;

namespace CardboardXamarin
{
    /// <summary>図形の抽象クラス。</summary>
    public abstract class Shape
    {
        protected float[] Vertexs;
        protected float[] Normals;
        protected float[] Texture;

        protected BeginMode PrimitiveType = BeginMode.Points;

        /// <summary>頂点数。三角形は「3」。四角形は「4」。</summary>
        protected int VertexCount;

        public virtual void Draw(Shader shader)
        {
            // 頂点座標をシェーダーに
            if (Vertexs != null) shader.SetVertex(Vertexs);
            //if (Normals != null) shader.SetNormal(Normals);
            //if (Texture != null) shader.SetTexture(Texture);

            //描画をシェーダに指示
            shader.UpdateMatrix();
            GL.DrawArrays(PrimitiveType, 0, VertexCount);
        }
    }

    /// <summary>三角形をXY平面上に描画する。</summary>
    public class Triangle : Shape
    {
        public Triangle(float size)
        {
            Init(size);

            this.VertexCount = 3;
            this.PrimitiveType = BeginMode.Triangles;
        }

        private void Init(float size)
        {
            var s = size / 2;
            Vertexs = new float[]
            {
                +0, +s, +0,
                -s, -s, +0,
                +s, -s, +0
            };

            Normals = new float[]
            {
                +0.0f, +0.0f, +1.0f,
                +0.0f, +0.0f, +1.0f,
                +0.0f, +0.0f, +1.0f
            };

            Texture = new float[]
            {
                0.0f, 0.5f,
                0.0f, 0.0f,
                1.0f, 0.0f
            };
        }
    }

    /// <summary>四角形をXY平面上に描画する。</summary>
    public class Rectangle : Shape
    {
        public Rectangle(float size)
        {
            Init(size);

            this.VertexCount = 4;
            this.PrimitiveType = BeginMode.TriangleStrip;
        }

        private void Init(float size)
        {
            var s = size / 2;
            Vertexs = new float[]
            {
                -s, +s, +0,
                -s, -s, +0,
                +s, +s, +0,
                +s, -s, +0
            };

            Normals = new float[]
            {
                +0.0f, +0.0f, +1.0f,
                +0.0f, +0.0f, +1.0f,
                +0.0f, +0.0f, +1.0f,
                +0.0f, +0.0f, +1.0f
            };

            Texture = new float[]
            {
                0.0f, 1.0f,
                0.0f, 0.0f,
                1.0f, 1.0f,
                1.0f, 0.0f
            };
        }
    }
}
