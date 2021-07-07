/*
 * Copyright 2014 Google Inc. All Rights Reserved.

 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Math = Java.Lang.Math;

using System;

using Android.Content;
using Android.Views;
using Android.Util;
using Android.Runtime;
using Android.Opengl;

using Javax.Microedition.Khronos.Opengles;
using Javax.Microedition.Khronos.Egl;

using Com.Google.VRToolkit.CardBoard.Sensors;

namespace Com.Google.VRToolkit.CardBoard
{
    public class CardboardView : GLSurfaceView
    {
		private const String TAG = "CardboardView";
		private const float DEFAULT_Z_NEAR = 0.1F;
		private const float DEFAULT_Z_FAR = 100.0F;
		private RendererHelper mRendererHelper;
		private HeadTracker mHeadTracker;
		private HeadMountedDisplay mHmd;
		private DistortionRenderer mDistortionRenderer;
		private CardboardDeviceParamsObserver mCardboardDeviceParamsObserver;
		private bool mVRMode = true;
		private volatile bool mDistortionCorrectionEnabled = true;
		private volatile float mDistortionCorrectionScale = 1.0F;
		private float mZNear = DEFAULT_Z_NEAR;
		private float mZFar = DEFAULT_Z_FAR;

		public CardboardView(Context context) : base(context)
		{
			Init(context);
		}

		public CardboardView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			Init(context);
		}

		public void SetRenderer(Renderer renderer)
		{
			mRendererHelper = (renderer != null ? new RendererHelper(this, renderer) : null);
			base.SetRenderer(mRendererHelper);
		}

		public void SetRenderer(StereoRenderer renderer)
		{
			SetRenderer(renderer != null ? new StereoRendererHelper(this, renderer) : null);
		}

		public void SetVRModeEnabled(bool enabled)
		{
			mVRMode = enabled;

			if (mRendererHelper != null)
				mRendererHelper.SetVRModeEnabled(enabled);
		}

		public bool GetVRMode()
		{
			return mVRMode;
		}

		public void SetAlignedToNorth(bool flag)
		{
			mHeadTracker.setAlignedToNorth(flag);
		}

		public HeadMountedDisplay GetHeadMountedDisplay()
		{
			return mHmd;
		}

		public void UpdateCardboardDeviceParams(CardboardDeviceParams cardboardDeviceParams)
		{
			if ((cardboardDeviceParams == null) || (cardboardDeviceParams.equals(mHmd.getCardboard())))
			{
				return;
			}

			if (mCardboardDeviceParamsObserver != null)
			{
				mCardboardDeviceParamsObserver.OnCardboardDeviceParamsUpdate(cardboardDeviceParams);
			}

			mHmd.setCardboard(cardboardDeviceParams);

			if (mRendererHelper != null)
				mRendererHelper.SetCardboardDeviceParams(cardboardDeviceParams);
		}

		public void SetCardboardDeviceParamsObserver(CardboardDeviceParamsObserver observer)
		{
			mCardboardDeviceParamsObserver = observer;
		}

		public CardboardDeviceParams GetCardboardDeviceParams()
		{
			return mHmd.getCardboard();
		}

		public void UpdateScreenParams(ScreenParams screenParams)
		{
			if ((screenParams == null) || (screenParams.equals(mHmd.getScreen())))
			{
				return;
			}

			mHmd.setScreen(screenParams);

			if (mRendererHelper != null)
				mRendererHelper.SetScreenParams(screenParams);
		}

		public ScreenParams GetScreenParams()
		{
			return mHmd.getScreen();
		}

		public void SetInterpupillaryDistance(float distance)
		{
			mHmd.getCardboard().setInterpupillaryDistance(distance);

			if (mRendererHelper != null)
				mRendererHelper.SetInterpupillaryDistance(distance);
		}

		public float GetInterpupillaryDistance()
		{
			return mHmd.getCardboard().getInterpupillaryDistance();
		}

		public void SetFovY(float fovY)
		{
			mHmd.getCardboard().setFovY(fovY);

			if (mRendererHelper != null)
				mRendererHelper.SetFOV(fovY);
		}

		public float GetFovY()
		{
			return mHmd.getCardboard().getFovY();
		}

		public void SetZPlanes(float zNear, float zFar)
		{
			mZNear = zNear;
			mZFar = zFar;

			if (mRendererHelper != null)
				mRendererHelper.SetZPlanes(zNear, zFar);
		}

		public float GetZNear()
		{
			return mZNear;
		}

		public float GetZFar()
		{
			return mZFar;
		}

		public void SetDistortionCorrectionEnabled(bool enabled)
		{
			mDistortionCorrectionEnabled = enabled;

			if (mRendererHelper != null)
				mRendererHelper.SetDistortionCorrectionEnabled(enabled);
		}

		public bool GetDistortionCorrectionEnabled()
		{
			return mDistortionCorrectionEnabled;
		}

		public void SetDistortionCorrectionScale(float scale)
		{
			mDistortionCorrectionScale = scale;

			if (mRendererHelper != null)
				mRendererHelper.SetDistortionCorrectionScale(scale);
		}

		public float GetDistortionCorrectionScale()
		{
			return mDistortionCorrectionScale;
		}

		public override void OnResume()
		{
			if (mRendererHelper == null)
			{
				return;
			}

			base.OnResume();
			mHeadTracker.startTracking();
		}

		public override void OnPause()
		{
			if (mRendererHelper == null)
			{
				return;
			}

			base.OnPause();
			mHeadTracker.stopTracking();
		}

		public override void SetRenderer(GLSurfaceView.IRenderer renderer)
		{
			throw new Java.Lang.RuntimeException("Please use the CardboardView renderer interfaces");
		}

		protected override void OnDetachedFromWindow()
		{
			if (mRendererHelper != null)
			{
				// Fix Java.Lang.IllegalMonitorStateException: 'object not locked by thread before wait()'
				// replace lock and wait() to CountDownLatch.

				/*lock (mRendererHelper)
				{
					mRendererHelper.Shutdown();
					try
					{
						mRendererHelper.Wait();
					}
					catch (Java.Lang.InterruptedException e)
					{
						Android.Util.Log.Error("CardboardView", "Interrupted during shutdown: " + e.ToString());
					}
				}*/

				var latch = new Java.Util.Concurrent.CountDownLatch(1);
				mRendererHelper.Shutdown(latch);
				try
				{
					latch.Await();
				}
				catch (Java.Lang.InterruptedException e)
				{
					Android.Util.Log.Error("CardboardView", "Interrupted during shutdown: " + e.ToString());
				}
			}

			base.OnDetachedFromWindow();
		}

		private void Init(Context context)
		{
			SetEGLContextClientVersion(2);
			PreserveEGLContextOnPause = true;

			IWindowManager windowManager = (IWindowManager)context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();

			mHeadTracker = new HeadTracker(context);
			mHmd = new HeadMountedDisplay(windowManager.DefaultDisplay);
		}

		private class StereoRendererHelper : Java.Lang.Object, CardboardView.Renderer
		{
			private CardboardView mView;
			private CardboardView.StereoRenderer mStereoRenderer;
			private bool mVRMode;

			public StereoRendererHelper(CardboardView view, CardboardView.StereoRenderer stereoRenderer)
			{
				mView = view;
				mStereoRenderer = stereoRenderer;
				mVRMode = mView.mVRMode;
			}

			public void SetVRModeEnabled(bool enabled)
			{
				// fix issue #1.
				//mView.QueueEvent(() =>
				//{
					mVRMode = enabled;
				//});
			}

			public void OnDrawFrame(HeadTransform head, EyeParams leftEye, EyeParams rightEye)
			{
				mStereoRenderer.OnNewFrame(head);
				GLES20.GlEnable(GLES20.GlScissorTest);

				leftEye.getViewport().setGLViewport();
				leftEye.getViewport().setGLScissor();
				mStereoRenderer.OnDrawEye(leftEye.getTransform());

				if (rightEye == null)
				{
					return;
				}

				rightEye.getViewport().setGLViewport();
				rightEye.getViewport().setGLScissor();
				mStereoRenderer.OnDrawEye(rightEye.getTransform());
			}

			public void OnFinishFrame(Viewport viewport)
			{
				viewport.setGLViewport();
				viewport.setGLScissor();
				mStereoRenderer.OnFinishFrame(viewport);
			}

			public void OnSurfaceChanged(int width, int height)
			{
				if (mVRMode)
				{
					mStereoRenderer.OnSurfaceChanged(width / 2, height);
				}
				else mStereoRenderer.OnSurfaceChanged(width, height);
			}

			public void OnSurfaceCreated(Javax.Microedition.Khronos.Egl.EGLConfig paramEGLConfig)
			{
				mStereoRenderer.OnSurfaceCreated(paramEGLConfig);
			}

			public void OnRendererShutdown()
			{
				mStereoRenderer.OnRendererShutdown();
			}
        }

	private class RendererHelper : Java.Lang.Object, GLSurfaceView.IRenderer
		{
			private CardboardView mView;
			private HeadTransform mHeadTransform;
			private EyeParams mMonocular;
			private EyeParams mLeftEye;
			private EyeParams mRightEye;
			private float[] mLeftEyeTranslate;
			private float[] mRightEyeTranslate;
			private CardboardView.Renderer mRenderer;
			private bool mShuttingDown;
			private HeadMountedDisplay mHmd;
			private bool mVRMode;
			private bool mDistortionCorrectionEnabled;
			private float mDistortionCorrectionScale;
			private float mZNear;
			private float mZFar;
			private bool mProjectionChanged;
			private bool mInvalidSurfaceSize;

			public RendererHelper(CardboardView view, CardboardView.Renderer renderer)
			{
				mView = view;
				mRenderer = renderer;
				mHmd = new HeadMountedDisplay(mView.mHmd);
				mHeadTransform = new HeadTransform();
				mMonocular = new EyeParams(EyeParams.Eye.MONOCULAR);
				mLeftEye = new EyeParams(EyeParams.Eye.LEFT);
				mRightEye = new EyeParams(EyeParams.Eye.RIGHT);
				UpdateFieldOfView(mLeftEye.getFov(), mRightEye.getFov());
				mView.mDistortionRenderer = new DistortionRenderer();

				mLeftEyeTranslate = new float[16];
				mRightEyeTranslate = new float[16];

				mVRMode = mView.mVRMode;
				mDistortionCorrectionEnabled = mView.mDistortionCorrectionEnabled;
				mDistortionCorrectionScale = mView.mDistortionCorrectionScale;
				mZNear = mView.mZNear;
				mZFar = mView.mZFar;

				mProjectionChanged = true;
			}

			public void Shutdown(Java.Util.Concurrent.CountDownLatch latch)
			{
				mView.QueueEvent(() =>
				{
					mShuttingDown = true;
					mRenderer.OnRendererShutdown();
					latch.CountDown();
				});
			}

			public void SetCardboardDeviceParams(CardboardDeviceParams newParams)
			{
				CardboardDeviceParams deviceParams = new CardboardDeviceParams(newParams);
				mView.QueueEvent(() =>
				{
					mHmd.setCardboard(deviceParams);
					mProjectionChanged = true;
				});
			}


			public void SetScreenParams(ScreenParams newParams)
			{
				ScreenParams screenParams = new ScreenParams(newParams);
				mView.QueueEvent(() =>
				{
					mHmd.setScreen(screenParams);
					mProjectionChanged = true;
				});
			}

			public void SetInterpupillaryDistance(float interpupillaryDistance)
			{
				mView.QueueEvent(() =>
				{
					mHmd.getCardboard().setInterpupillaryDistance(interpupillaryDistance);
					mProjectionChanged = true;
				});
			}

			public void SetFOV(float fovY)
			{
				mView.QueueEvent(() =>
				{
					mHmd.getCardboard().setFovY(fovY);
					mProjectionChanged = true;
				});
			}


			public void SetZPlanes(float zNear, float zFar)
			{
				mView.QueueEvent(() =>
				{
					mZNear = zNear;
					mZFar = zFar;
					mProjectionChanged = true;
				});	
			}

			public void SetDistortionCorrectionEnabled(bool enabled)
			{
				mView.QueueEvent(() =>
				{
					mDistortionCorrectionEnabled = enabled;
					mProjectionChanged = true;
				});
			}

			public void SetDistortionCorrectionScale(float scale)
			{
				mView.QueueEvent(() =>
				{
					mDistortionCorrectionScale = scale;
					mView.mDistortionRenderer.setResolutionScale(scale);
				});
			}

			public void SetVRModeEnabled(bool enabled)
			{
				mView.QueueEvent(() =>
				{
					if (mVRMode == enabled)
					{
						return;
					}

					mVRMode = enabled;

					if ((mRenderer is CardboardView.StereoRendererHelper)) {
						CardboardView.StereoRendererHelper stereoHelper = (CardboardView.StereoRendererHelper)mRenderer;
						stereoHelper.SetVRModeEnabled(enabled);
					}
					
					mProjectionChanged = true;
					OnSurfaceChanged((IGL10)null, mHmd.getScreen().getWidth(), mHmd.getScreen().getHeight());
				});
			}

			public void OnDrawFrame(IGL10 gl)
			{
				if ((mShuttingDown) || (mInvalidSurfaceSize))
				{
					return;
				}

				ScreenParams screen = mHmd.getScreen();
				CardboardDeviceParams cdp = mHmd.getCardboard();
				
				mView.mHeadTracker.getLastHeadView(mHeadTransform.getHeadView(), 0);

				float halfInterpupillaryDistance = cdp.getInterpupillaryDistance() * 0.5F;

				if (mVRMode)
				{
					Android.Opengl.Matrix.SetIdentityM(mLeftEyeTranslate, 0);
					Android.Opengl.Matrix.SetIdentityM(mRightEyeTranslate, 0);

					Android.Opengl.Matrix.TranslateM(mLeftEyeTranslate, 0, halfInterpupillaryDistance, 0.0F, 0.0F);

					Android.Opengl.Matrix.TranslateM(mRightEyeTranslate, 0, -halfInterpupillaryDistance, 0.0F, 0.0F);

					Android.Opengl.Matrix.MultiplyMM(mLeftEye.getTransform().GetEyeView(), 0, mLeftEyeTranslate, 0, mHeadTransform.getHeadView(), 0);

					Android.Opengl.Matrix.MultiplyMM(mRightEye.getTransform().GetEyeView(), 0, mRightEyeTranslate, 0, mHeadTransform.getHeadView(), 0);
				}
				else
				{
					//Java.Lang.JavaSystem.Arraycopy(mHeadTransform.getHeadView(), 0, mMonocular.getTransform().GetEyeView(), 0, mHeadTransform.getHeadView().Length);
					Array.Copy(mHeadTransform.getHeadView(), 0, mMonocular.getTransform().GetEyeView(), 0, mHeadTransform.getHeadView().Length);
				}

				if (mProjectionChanged)
				{
					mMonocular.getViewport().setViewport(0, 0, screen.getWidth(), screen.getHeight());

					if (!mVRMode)
					{
						float aspectRatio = screen.getWidth() / screen.getHeight();
						Android.Opengl.Matrix.PerspectiveM(mMonocular.getTransform().GetPerspective(), 0, cdp.getFovY(), aspectRatio, mZNear, mZFar);
					}
					else if (mDistortionCorrectionEnabled)
					{
						UpdateFieldOfView(mLeftEye.getFov(), mRightEye.getFov());
						mView.mDistortionRenderer.onProjectionChanged(mHmd, mLeftEye, mRightEye, mZNear, mZFar);
					}
					else
					{
						float distEyeToScreen = cdp.getVisibleViewportSize() / 2.0F / (float)Math.Tan(Math.ToRadians(cdp.getFovY()) / 2.0D);

						float left = screen.getWidthMeters() / 2.0F - halfInterpupillaryDistance;
						float right = halfInterpupillaryDistance;
						float bottom = cdp.getVerticalDistanceToLensCenter() - screen.getBorderSizeMeters();

						float top = screen.getBorderSizeMeters() + screen.getHeightMeters() - cdp.getVerticalDistanceToLensCenter();

						FieldOfView leftEyeFov = mLeftEye.getFov();
						leftEyeFov.setLeft((float)Math.ToDegrees(Math.Atan2(left, distEyeToScreen)));

						leftEyeFov.setRight((float)Math.ToDegrees(Math.Atan2(right, distEyeToScreen)));

						leftEyeFov.setBottom((float)Math.ToDegrees(Math.Atan2(bottom, distEyeToScreen)));

						leftEyeFov.setTop((float)Math.ToDegrees(Math.Atan2(top, distEyeToScreen)));

						FieldOfView rightEyeFov = mRightEye.getFov();
						rightEyeFov.setLeft(leftEyeFov.getRight());
						rightEyeFov.setRight(leftEyeFov.getLeft());
						rightEyeFov.setBottom(leftEyeFov.getBottom());
						rightEyeFov.setTop(leftEyeFov.getTop());

						leftEyeFov.toPerspectiveMatrix(mZNear, mZFar, mLeftEye.getTransform().GetPerspective(), 0);

						rightEyeFov.toPerspectiveMatrix(mZNear, mZFar, mRightEye.getTransform().GetPerspective(), 0);

						mLeftEye.getViewport().setViewport(0, 0, screen.getWidth() / 2, screen.getHeight());

						mRightEye.getViewport().setViewport(screen.getWidth() / 2, 0, screen.getWidth() / 2, screen.getHeight());
					}

					mProjectionChanged = false;
				}

				if (mVRMode)
				{
					if (mDistortionCorrectionEnabled)
					{
						mView.mDistortionRenderer.beforeDrawFrame();

						if (mDistortionCorrectionScale == 1.0F)
						{
							mRenderer.OnDrawFrame(mHeadTransform, mLeftEye, mRightEye);
						}
						else
						{
							int leftX = mLeftEye.getViewport().x;
							int leftY = mLeftEye.getViewport().y;
							int leftWidth = mLeftEye.getViewport().width;
							int leftHeight = mLeftEye.getViewport().height;
							int rightX = mRightEye.getViewport().x;
							int rightY = mRightEye.getViewport().y;
							int rightWidth = mRightEye.getViewport().width;
							int rightHeight = mRightEye.getViewport().height;

							mLeftEye.getViewport().setViewport((int)(leftX * mDistortionCorrectionScale), (int)(leftY * mDistortionCorrectionScale), (int)(leftWidth * mDistortionCorrectionScale), (int)(leftHeight * mDistortionCorrectionScale));

							mRightEye.getViewport().setViewport((int)(rightX * mDistortionCorrectionScale), (int)(rightY * mDistortionCorrectionScale), (int)(rightWidth * mDistortionCorrectionScale), (int)(rightHeight * mDistortionCorrectionScale));

							mRenderer.OnDrawFrame(mHeadTransform, mLeftEye, mRightEye);

							mLeftEye.getViewport().setViewport(leftX, leftY, leftWidth, leftHeight);

							mRightEye.getViewport().setViewport(rightX, rightY, rightWidth, rightHeight);
						}

						mView.mDistortionRenderer.afterDrawFrame();
					}
					else
					{
						mRenderer.OnDrawFrame(mHeadTransform, mLeftEye, mRightEye);
					}
				}
				else mRenderer.OnDrawFrame(mHeadTransform, mMonocular, null);

				mRenderer.OnFinishFrame(mMonocular.getViewport());
			}

			public void OnSurfaceChanged(IGL10 gl, int width, int height)
			{
				if (mShuttingDown)
				{
					return;
				}

				ScreenParams screen = mHmd.getScreen();
				if ((width != screen.getWidth()) || (height != screen.getHeight()))
				{
					if (!mInvalidSurfaceSize)
					{
						GLES20.GlClear(GLES20.GlColorBufferBit);
						Android.Util.Log.Warn("CardboardView", "Surface size " + width + "x" + height + " does not match the expected screen size " + screen.getWidth() + "x" + screen.getHeight() + ". Rendering is disabled.");
					}

					mInvalidSurfaceSize = true;
				}
				else
				{
					mInvalidSurfaceSize = false;
				}

				mRenderer.OnSurfaceChanged(width, height);
			}

			public void OnSurfaceCreated(IGL10 gl, Javax.Microedition.Khronos.Egl.EGLConfig config)
			{
				if (mShuttingDown)
				{
					return;
				}

				mRenderer.OnSurfaceCreated(config);
			}

			private void UpdateFieldOfView(FieldOfView leftEyeFov, FieldOfView rightEyeFov)
			{
				CardboardDeviceParams cdp = mHmd.getCardboard();
				ScreenParams screen = mHmd.getScreen();
				Distortion distortion = cdp.getDistortion();

				float idealFovAngle = (float)Math.ToDegrees(Math.Atan2(cdp.getLensDiameter() / 2.0F, cdp.getEyeToLensDistance()));

				float eyeToScreenDist = cdp.getEyeToLensDistance() + cdp.getScreenToLensDistance();

				float outerDist = (screen.getWidthMeters() - cdp.getInterpupillaryDistance()) / 2.0F;

				float innerDist = cdp.getInterpupillaryDistance() / 2.0F;
				float bottomDist = cdp.getVerticalDistanceToLensCenter() - screen.getBorderSizeMeters();

				float topDist = screen.getHeightMeters() + screen.getBorderSizeMeters() - cdp.getVerticalDistanceToLensCenter();

				float outerAngle = (float)Math.ToDegrees(Math.Atan2(distortion.distort(outerDist), eyeToScreenDist));

				float innerAngle = (float)Math.ToDegrees(Math.Atan2(distortion.distort(innerDist), eyeToScreenDist));

				float bottomAngle = (float)Math.ToDegrees(Math.Atan2(distortion.distort(bottomDist), eyeToScreenDist));

				float topAngle = (float)Math.ToDegrees(Math.Atan2(distortion.distort(topDist), eyeToScreenDist));

				leftEyeFov.setLeft(Math.Min(outerAngle, idealFovAngle));
				leftEyeFov.setRight(Math.Min(innerAngle, idealFovAngle));
				leftEyeFov.setBottom(Math.Min(bottomAngle, idealFovAngle));
				leftEyeFov.setTop(Math.Min(topAngle, idealFovAngle));

				rightEyeFov.setLeft(Math.Min(innerAngle, idealFovAngle));
				rightEyeFov.setRight(Math.Min(outerAngle, idealFovAngle));
				rightEyeFov.setBottom(Math.Min(bottomAngle, idealFovAngle));
				rightEyeFov.setTop(Math.Min(topAngle, idealFovAngle));
			}
        }

		/** Intercepts changes in the current Cardboard device parameters. */
		public interface CardboardDeviceParamsObserver
		{
			void OnCardboardDeviceParamsUpdate(CardboardDeviceParams paramCardboardDeviceParams);
		}

		/** Interface for renderers that delegate all stereoscopic rendering details to the view. */
		public interface StereoRenderer
		{
			void OnNewFrame(HeadTransform paramHeadTransform);

			void OnDrawEye(EyeTransform paramEyeTransform);

			void OnFinishFrame(Viewport paramViewport);

			void OnSurfaceChanged(int paramInt1, int paramInt2);

			void OnSurfaceCreated(Javax.Microedition.Khronos.Egl.EGLConfig paramEGLConfig);

			void OnRendererShutdown();
		}

		/** Interface for renderers who need to handle all the stereo rendering details by themselves. */
		public interface Renderer
		{
			void OnDrawFrame(HeadTransform paramHeadTransform, EyeParams paramEyeParams1, EyeParams paramEyeParams2);

			void OnFinishFrame(Viewport paramViewport);

			void OnSurfaceChanged(int paramInt1, int paramInt2);

			void OnSurfaceCreated(Javax.Microedition.Khronos.Egl.EGLConfig paramEGLConfig);

			void OnRendererShutdown();
		}
	}	
}
