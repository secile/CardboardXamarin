using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES20;

namespace CardboardXamarin
{
    public class Shader
    {
        private int myProgram;

        public void Activate()
        {
            GL.UseProgram(myProgram);
        }

        public Shader()
        {
            CompileShadere(vert_phong, frag_phong);
            LinkHandle();
            Activate();
        }

        #region シェーダーの作成

        // OpenGLのレンダリングパイプライン
        // [バーテックスシェーダー] → [ビューポート変換] → [ラスタイズ] → [フラグメントシェーダー]
        
        // ①バーテックスシェーダー
        // 3Dの位置情報(x, y, z)を、最終的にGPUで出力する2Dの位置情報に変換する。
        // 頂点ごとに呼び出される。
        // 変換後の位置情報を「gl_Position」に格納する。

        // ②フラグメントシェーダー
        // 各ピクセルの色情報を計算する。ピクセルごとなので大量に呼び出される。
        // バーテックスシェーダーによって計算された、最終的な頂点座標で構成される図形のピクセルごとに呼び出される。
        // 色情報は「gl_FragColor」に格納する。

        // このシェーダーでは頂点座標のみ利用し、法線・テクスチャは使用していない。
        private const string vert_phong = @"
precision mediump float;

// Projection * ModelView Matrix
uniform mat4 u_mvpMatrix;

// Vertexs
attribute vec4 a_Position;
attribute vec3 a_Normal;
attribute vec2 a_Texture;

void main(){
    gl_Position = u_mvpMatrix * a_Position;
}
";

        private const string frag_phong = @"
precision mediump float;

// Material
uniform vec4 u_MaterialDiffuse;

void main(){
    gl_FragColor = u_MaterialDiffuse;
}
";

        private bool CompileShadere(string vertex, string fragment)
        {
            //シェーダーオブジェクトの生成
            int vertexShader = LoadShader(ShaderType.VertexShader, vertex);
            if (vertexShader == -1) return false;

            int fragmentShader = LoadShader(ShaderType.FragmentShader, fragment);
            if (fragmentShader == -1) return false;

            //シェーダプログラムのオブジェクトを生成
            myProgram = GL.CreateProgram();

            //各シェーダオブジェクトをシェーダプログラムへ登録
            GL.AttachShader(myProgram, vertexShader);
            GL.AttachShader(myProgram, fragmentShader);

            //不要になった各シェーダオブジェクトを削除
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            //シェーダプログラムのリンク
            GL.LinkProgram(myProgram);

            int status;
            GL.GetProgram(myProgram, ProgramParameter.LinkStatus, out status);
            //シェーダプログラムのリンクのチェック
            if (status == 0)
            {
                throw new ApplicationException(GL.GetProgramInfoLog(myProgram));
            }

            return true;
        }

        /// <summary>シェーダー言語をStreamから読み出してコンパイル。</summary>
        private static int LoadShader(ShaderType type, System.IO.Stream stream)
        {
            using (var sr = new System.IO.StreamReader(stream))
            {
                return LoadShader(type, sr.ReadToEnd());
            }
        }

        /// <summary>シェーダー言語を文字列で読み出してコンパイル。</summary>
        private static int LoadShader(ShaderType type, string code)
        {
            //シェーダオブジェクト(バーテックス)を生成
            var vertexShader = GL.CreateShader(type);

            //バーテックスシェーダのコードを指定
            GL.ShaderSource(vertexShader, code);

            //バーテックスシェーダをコンパイル
            GL.CompileShader(vertexShader);

            int status;
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out status);
            //コンパイル結果をチェック
            if (status == 0)
            {
                throw new ApplicationException(GL.GetShaderInfoLog(vertexShader));
            }

            return vertexShader;
        }

        // 頂点座標のハンドル
        private int positionHandle;
        private int normalHandle;
        private int textureHandle;

        //マテリアルのハンドル
        private int materialDiffuseHandle;   //マテリアルの拡散光色ハンドル

        //行列のハンドル
        private int mvpMatrixHandle; //(射影行列×モデルビュー行列)ハンドル

        // 行列
        private Matrix4 mMatrix;    //モデルマトリックス
        private Matrix4 vMatrix;    //ビューマトリクス
        private Matrix4 pMatrix;    //プロジェクション行列（射影行列）

        private void LinkHandle()
        {
            //マテリアルのハンドルの取得
            materialDiffuseHandle = GL.GetUniformLocation(myProgram, "u_MaterialDiffuse");

            //行列のハンドルの取得
            mvpMatrixHandle = GL.GetUniformLocation(myProgram, "u_mvpMatrix");

            //頂点とその法線ベクトルのハンドルの取得
            positionHandle = GL.GetAttribLocation(myProgram, "a_Position");
            normalHandle = GL.GetAttribLocation(myProgram, "a_Normal");
            textureHandle = GL.GetAttribLocation(myProgram, "a_Texture");
        }

        #endregion


        #region Shaderが利用

        /// <summary>頂点配列をシェーダに指定。</summary>
        public void SetVertex(float[] data)
        {
            GL.EnableVertexAttribArray(positionHandle);
            GL.VertexAttribPointer(positionHandle, 3, VertexAttribPointerType.Float, true, 0, data);
        }

        /// <summary>法線配列をシェーダに指定。</summary>
        public void SetNormal(float[] data)
        {
            GL.EnableVertexAttribArray(normalHandle);
            GL.VertexAttribPointer(normalHandle, 3, VertexAttribPointerType.Float, true, 0, data);
        }

        /// <summary>テクスチャ座標配列をシェーダに指定。</summary>
        public void SetTexture(float[] data)
        {
            GL.EnableVertexAttribArray(textureHandle);
            GL.VertexAttribPointer(textureHandle, 2, VertexAttribPointerType.Float, true, 0, data);
        }

        /// <summary>モデル、ビュー、プロジェクション行列をシェーダに指定。描画前に必ず実行すること。</summary>
        public void UpdateMatrix()
        {
            var mv = mMatrix * vMatrix;  // カメラ前の座標系
            var mvp = mv * pMatrix;      // カメラ前の見え方

            //プロジェクション行列（射影行列）×モデルビュー行列をシェーダに指定
            GL.UniformMatrix4(mvpMatrixHandle, false, ref mvp);
        }

        #endregion


        #region モデル行列操作

        /// <summary>モデル行列を初期化する。</summary>
        public void IdentityMatrix()
        {
            mMatrix = Matrix4.Identity;
        }

        private Stack<Matrix4> Matrixs = new Stack<Matrix4>();

        public void PushMatrix()
        {
            Matrixs.Push(mMatrix);
        }

        public void PopMatrix()
        {
            mMatrix = Matrixs.Pop();
        }

        #endregion


        #region モデル行列(移動・回転・拡大)

        public void Rotate(float degree, Vector3 axis)
        {
            var rad = degree * Math.PI / 180;
            mMatrix = Matrix4.CreateFromAxisAngle(axis, (float)rad) * mMatrix;
        }

        public void Translate(float x, float y, float z)
        {
            mMatrix = Matrix4.CreateTranslation(x, y, z) * mMatrix;
        }

        public void Scale(float x, float y, float z)
        {
            mMatrix = Matrix4CreateScale(x, y, z) * mMatrix;
        }

        private static Matrix4 Matrix4CreateScale(float x, float y, float z)
        {
            var result = Matrix4.Identity;
            result.Row0.X = x;
            result.Row1.Y = y;
            result.Row2.Z = z;
            return result;
        }

        #endregion


        public void SetLookAt(Matrix4 mat)
        {
            vMatrix = mat;
        }

        public void SetProjection(Matrix4 mat)
        {
            pMatrix = mat;
        }

        public void SetMaterial(Color4 color)
        {
            GL.Uniform4(materialDiffuseHandle, color.R, color.G, color.B, color.A); //拡散光
        }
    }
}