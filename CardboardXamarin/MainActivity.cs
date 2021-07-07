using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.OS;

using OpenTK;
using OpenTK.Graphics.ES20;

using Com.Google.VRToolkit.CardBoard;

namespace CardboardXamarin
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape)]
    public class MainActivity : CardboardActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // create CardboardView and set.
            // CardboarViewの作成。
            var glview = new CardboardView(this);
            glview.SetAlignedToNorth(true); // 実際の北に合わせる場合はtrue
            SetCardboardView(glview);

            // dont work on my AQUOS sense4 lite without below.
            // これがないと私のAQUOS sense4 liteで下記ログが表示され動作しない。
            // Surface size 2064x1008 does not match the expected screen size 2280x1080. Rendering is disabled.
            var screen = glview.GetScreenParams();
            glview.Holder.SetFixedSize(screen.getWidth(), screen.getHeight());

            // create Renderer and set.
            // Rendrerの作成。
            var render = new VrRenderer();
            glview.SetRenderer(render);

            SetContentView(glview);
        }
    }

    class VrRenderer : Java.Lang.Object, CardboardView.StereoRenderer
    {
        private Shader Shader;
        private Shape Shape;

        public void OnSurfaceCreated(Javax.Microedition.Khronos.Egl.EGLConfig config)
        {
            GL.ClearColor(0.1f, 0.1f, 0.4f, 1.0f); // 背景色の設定。
            GL.Enable(EnableCap.DepthTest); // Depthバッファの有効化(Z座標で手前に表示)

            Shader = new Shader();
            Shape = new Triangle(0.5f);
        }

        public void OnSurfaceChanged(int width, int height)
        {
            Android.Util.Log.Debug("OnSurfaceChanged", $"w:{width}, h:{height}");
            GL.Viewport(0, 0, width, height);
        }

        // OnNewFrame→OnDrawEye(Left Eye)→OnDrawEye(Right Eye)→OnNewFrame→…の繰り返し(repeat)

        private float Rotate = 0;

        public void OnNewFrame(HeadTransform transform)
        {
            Shader.Activate();

            // update rotate angle.
            // 回転角度を更新する。
            Rotate++;
        }

        public void OnDrawEye(EyeTransform transform)
        {
            // clear buffer.
            // 画面をクリアする。
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            float[] prj = transform.GetPerspective();
            OpenTK.Matrix4 prjMat = MatrixFromArray(ref prj);
            Shader.SetProjection(prjMat);

            // apply left/right eye matrix.
            float[] eye = transform.GetEyeView();
            OpenTK.Matrix4 eyeMat = MatrixFromArray(ref eye);
            OpenTK.Matrix4 lookat = Matrix4.LookAt(-Vector3.UnitZ, Vector3.Zero, Vector3.UnitY); // 手前から、Y軸を上にして、原点を見る視点。
            Shader.SetLookAt(lookat * eyeMat);                                                   // 左目または右目の視点にする。

            // draw shape.
            // 図形を描画する。
            Shader.IdentityMatrix();
            Shader.Rotate(Rotate, Vector3.UnitY);
            Shader.SetMaterial(OpenTK.Graphics.Color4.Red);
            Shape.Draw(Shader);
        }

        public void OnFinishFrame(Viewport viewport)
        {

        }

        public void OnRendererShutdown()
        {

        }

        private static Matrix4 MatrixFromArray(ref float[] m)
        {
            return new Matrix4(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);
        }
    }
}