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

using Android.Util;
using Android.Opengl;

using Java.Nio;

namespace Com.Google.VRToolkit.CardBoard
{
    class DistortionRenderer
    {
		private const String TAG = "DistortionRenderer";
		private int mTextureId;
		private int mRenderbufferId;
		private int mFramebufferId;
		private IntBuffer mOriginalFramebufferId;
		private IntBuffer mCullFaceEnabled;
		private IntBuffer mScissorTestEnabled;
		private IntBuffer mViewport;
		private float mResolutionScale;
		private DistortionMesh mLeftEyeDistortionMesh;
		private DistortionMesh mRightEyeDistortionMesh;
		private HeadMountedDisplay mHmd;
		private FieldOfView mLeftEyeFov;
		private FieldOfView mRightEyeFov;
		private ProgramHolder mProgramHolder;
		private const String VERTEX_SHADER = "attribute vec2 aPosition;\nattribute float aVignette;\nattribute vec2 aTextureCoord;\nvarying vec2 vTextureCoord;\nvarying float vVignette;\nuniform float uTextureCoordScale;\nvoid main() {\n    gl_Position = vec4(aPosition, 0.0, 1.0);\n    vTextureCoord = aTextureCoord.xy * uTextureCoordScale;\n    vVignette = aVignette;\n}\n";
		private const String FRAGMENT_SHADER = "precision mediump float;\nvarying vec2 vTextureCoord;\nvarying float vVignette;\nuniform sampler2D uTextureSampler;\nvoid main() {\n    gl_FragColor = vVignette * texture2D(uTextureSampler, vTextureCoord);\n}\n";

	public DistortionRenderer()
		{
			mTextureId = -1;
			mRenderbufferId = -1;
			mFramebufferId = -1;
			mOriginalFramebufferId = IntBuffer.Allocate(1);
			mCullFaceEnabled = IntBuffer.Allocate(1);
			mScissorTestEnabled = IntBuffer.Allocate(1);
			mViewport = IntBuffer.Allocate(4);

			mResolutionScale = 1.0F;
		}

		public void beforeDrawFrame()
		{
			GLES20.GlGetIntegerv(36006, mOriginalFramebufferId);
			GLES20.GlBindFramebuffer(36160, mFramebufferId);
		}

		public void afterDrawFrame()
		{
			GLES20.GlBindFramebuffer(36160, mOriginalFramebufferId/*.array()[0]*/.Get(0));
			GLES20.GlViewport(0, 0, mHmd.getScreen().getWidth(), mHmd.getScreen().getHeight());

			GLES20.GlGetIntegerv(2978, mViewport);
			GLES20.GlGetIntegerv(2884, mCullFaceEnabled);
			GLES20.GlGetIntegerv(3089, mScissorTestEnabled);
			GLES20.GlDisable(3089);
			GLES20.GlDisable(2884);

			GLES20.GlClearColor(0.0F, 0.0F, 0.0F, 1.0F);
			GLES20.GlClear(16640);

			GLES20.GlUseProgram(mProgramHolder.program);

			GLES20.GlEnable(3089);
			GLES20.GlScissor(0, 0, mHmd.getScreen().getWidth() / 2, mHmd.getScreen().getHeight());

			renderDistortionMesh(mLeftEyeDistortionMesh);

			GLES20.GlScissor(mHmd.getScreen().getWidth() / 2, 0, mHmd.getScreen().getWidth() / 2, mHmd.getScreen().getHeight());

			renderDistortionMesh(mRightEyeDistortionMesh);

			GLES20.GlDisableVertexAttribArray(mProgramHolder.aPosition);
			GLES20.GlDisableVertexAttribArray(mProgramHolder.aVignette);
			GLES20.GlDisableVertexAttribArray(mProgramHolder.aTextureCoord);
			GLES20.GlUseProgram(0);
			GLES20.GlBindBuffer(34962, 0);
			GLES20.GlBindBuffer(34963, 0);
			GLES20.GlDisable(3089);
			if (mCullFaceEnabled/*.array()[0]*/.Get(0) == 1)
			{
				GLES20.GlEnable(2884);
			}
			if (mScissorTestEnabled/*.array()[0]*/.Get(0) == 1)
			{
				GLES20.GlEnable(3089);
			}
			GLES20.GlViewport(mViewport/*.array()[0]*/.Get(0), mViewport/*.array()[1]*/.Get(1), mViewport/*.array()[2]*/.Get(2), mViewport/*.array()[3]*/.Get(3));
		}

		public void setResolutionScale(float scale)
		{
			mResolutionScale = scale;
		}

		public void onProjectionChanged(HeadMountedDisplay hmd, EyeParams leftEye, EyeParams rightEye, float zNear, float zFar)
		{
			mHmd = new HeadMountedDisplay(hmd);
			mLeftEyeFov = new FieldOfView(leftEye.getFov());
			mRightEyeFov = new FieldOfView(rightEye.getFov());

			ScreenParams screen = mHmd.getScreen();
			CardboardDeviceParams cdp = mHmd.getCardboard();

			if (mProgramHolder == null)
			{
				mProgramHolder = createProgramHolder();
			}

			EyeViewport leftEyeViewport = initViewportForEye(leftEye, 0.0F);
			EyeViewport rightEyeViewport = initViewportForEye(rightEye, leftEyeViewport.width);

			leftEye.getFov().toPerspectiveMatrix(zNear, zFar, leftEye.getTransform().GetPerspective(), 0);

			rightEye.getFov().toPerspectiveMatrix(zNear, zFar, rightEye.getTransform().GetPerspective(), 0);

			float textureWidthM = leftEyeViewport.width + rightEyeViewport.width;
			float textureHeightM = Math.Max(leftEyeViewport.height, rightEyeViewport.height);
			float xPxPerM = screen.getWidth() / screen.getWidthMeters();
			float yPxPerM = screen.getHeight() / screen.getHeightMeters();
			int textureWidthPx = Math.Round(textureWidthM * xPxPerM);
			int textureHeightPx = Math.Round(textureHeightM * yPxPerM);

			float xEyeOffsetMScreen = screen.getWidthMeters() / 2.0F - cdp.getInterpupillaryDistance() / 2.0F;
			float yEyeOffsetMScreen = cdp.getVerticalDistanceToLensCenter() - screen.getBorderSizeMeters();

			mLeftEyeDistortionMesh = createDistortionMesh(leftEye, leftEyeViewport, textureWidthM, textureHeightM, xEyeOffsetMScreen, yEyeOffsetMScreen);

			xEyeOffsetMScreen = screen.getWidthMeters() - xEyeOffsetMScreen;
			mRightEyeDistortionMesh = createDistortionMesh(rightEye, rightEyeViewport, textureWidthM, textureHeightM, xEyeOffsetMScreen, yEyeOffsetMScreen);

			setupRenderTextureAndRenderbuffer(textureWidthPx, textureHeightPx);
		}

		private EyeViewport initViewportForEye(EyeParams eye, float xOffsetM)
		{
			ScreenParams screen = mHmd.getScreen();
			CardboardDeviceParams cdp = mHmd.getCardboard();

			float eyeToScreenDistanceM = cdp.getEyeToLensDistance() + cdp.getScreenToLensDistance();
			float leftM = (float)Math.Tan(Math.ToRadians(eye.getFov().getLeft())) * eyeToScreenDistanceM;

			float rightM = (float)Math.Tan(Math.ToRadians(eye.getFov().getRight())) * eyeToScreenDistanceM;

			float bottomM = (float)Math.Tan(Math.ToRadians(eye.getFov().getBottom())) * eyeToScreenDistanceM;

			float topM = (float)Math.Tan(Math.ToRadians(eye.getFov().getTop())) * eyeToScreenDistanceM;

			EyeViewport vp = new EyeViewport();
			vp.x = xOffsetM;
			vp.y = 0.0F;
			vp.width = (leftM + rightM);
			vp.height = (bottomM + topM);
			vp.eyeX = (leftM + xOffsetM);
			vp.eyeY = bottomM;

			float xPxPerM = screen.getWidth() / screen.getWidthMeters();
			float yPxPerM = screen.getHeight() / screen.getHeightMeters();
			eye.getViewport().x = Math.Round(vp.x * xPxPerM);
			eye.getViewport().y = Math.Round(vp.y * xPxPerM);
			eye.getViewport().width = Math.Round(vp.width * xPxPerM);
			eye.getViewport().height = Math.Round(vp.height * xPxPerM);

			return vp;
		}

		private DistortionMesh createDistortionMesh(EyeParams eye, EyeViewport eyeViewport, float textureWidthM, float textureHeightM, float xEyeOffsetMScreen, float yEyeOffsetMScreen)
		{
			return new DistortionMesh(eye, mHmd.getCardboard().getDistortion(), mHmd.getScreen().getWidthMeters(), mHmd.getScreen().getHeightMeters(), xEyeOffsetMScreen, yEyeOffsetMScreen, textureWidthM, textureHeightM, eyeViewport.eyeX, eyeViewport.eyeY, eyeViewport.x, eyeViewport.y, eyeViewport.width, eyeViewport.height);
		}

		private void renderDistortionMesh(DistortionMesh mesh)
		{
			GLES20.GlBindBuffer(34962, mesh.mArrayBufferId);
			/*mesh.getClass(); mesh.getClass();*/ GLES20.GlVertexAttribPointer(mProgramHolder.aPosition, 3, 5126, false, 20, 0 * 4);

			GLES20.GlEnableVertexAttribArray(mProgramHolder.aPosition);

			/*mesh.getClass(); mesh.getClass();*/ GLES20.GlVertexAttribPointer(mProgramHolder.aVignette, 1, 5126, false, 20, 2 * 4);

			GLES20.GlEnableVertexAttribArray(mProgramHolder.aVignette);

			/*mesh.getClass(); mesh.getClass();*/ GLES20.GlVertexAttribPointer(mProgramHolder.aTextureCoord, 2, 5126, false, 20, 3 * 4);

			GLES20.GlEnableVertexAttribArray(mProgramHolder.aTextureCoord);

			GLES20.GlActiveTexture(33984);
			GLES20.GlBindTexture(3553, mTextureId);
			GLES20.GlUniform1i(mProgramHolder.uTextureSampler, 0);
			GLES20.GlUniform1f(mProgramHolder.uTextureCoordScale, mResolutionScale);

			GLES20.GlBindBuffer(34963, mesh.mElementBufferId);
			GLES20.GlDrawElements(5, mesh.nIndices, 5125, 0);
		}

		private float computeDistortionScale(Distortion distortion, float screenWidthM, float interpupillaryDistanceM)
		{
			return distortion.distortionFactor((screenWidthM / 2.0F - interpupillaryDistanceM / 2.0F) / (screenWidthM / 4.0F));
		}

		private int createTexture(int width, int height)
		{
			int[] textureIds = new int[1];
			GLES20.GlGenTextures(1, textureIds, 0);

			GLES20.GlBindTexture(3553, textureIds[0]);
			GLES20.GlTexParameteri(3553, 10242, 33071);

			GLES20.GlTexParameteri(3553, 10243, 33071);

			GLES20.GlTexParameteri(3553, 10240, 9729);

			GLES20.GlTexParameteri(3553, 10241, 9729);

			GLES20.GlTexImage2D(3553, 0, 6407, width, height, 0, 6407, 33635, null);

			return textureIds[0];
		}

		private int setupRenderTextureAndRenderbuffer(int width, int height)
		{
			if (mTextureId != -1)
			{
				GLES20.GlDeleteTextures(1, new int[] { mTextureId }, 0);
			}
			if (mRenderbufferId != -1)
			{
				GLES20.GlDeleteRenderbuffers(1, new int[] { mRenderbufferId }, 0);
			}
			if (mFramebufferId != -1)
			{
				GLES20.GlDeleteFramebuffers(1, new int[] { mFramebufferId }, 0);
			}

			mTextureId = createTexture(width, height);
			checkGlError("setupRenderTextureAndRenderbuffer: create texture");

			int[] renderbufferIds = new int[1];
			GLES20.GlGenRenderbuffers(1, renderbufferIds, 0);
			GLES20.GlBindRenderbuffer(36161, renderbufferIds[0]);
			GLES20.GlRenderbufferStorage(36161, 33189, width, height);

			mRenderbufferId = renderbufferIds[0];
			checkGlError("setupRenderTextureAndRenderbuffer: create renderbuffer");

			int[] framebufferIds = new int[1];
			GLES20.GlGenFramebuffers(1, framebufferIds, 0);
			GLES20.GlBindFramebuffer(36160, framebufferIds[0]);
			mFramebufferId = framebufferIds[0];

			GLES20.GlFramebufferTexture2D(36160, 36064, 3553, mTextureId, 0);

			GLES20.GlFramebufferRenderbuffer(36160, 36096, 36161, renderbufferIds[0]);

			int status = GLES20.GlCheckFramebufferStatus(36160);

			if (status != 36053)
			{
				throw new Java.Lang.RuntimeException("Framebuffer is not complete: " + Java.Lang.Integer.ToHexString(status));
			}

			GLES20.GlBindFramebuffer(36160, 0);

			return framebufferIds[0];
		}

		private int loadShader(int shaderType, String source)
		{
			int shader = GLES20.GlCreateShader(shaderType);
			if (shader != 0)
			{
				GLES20.GlShaderSource(shader, source);
				GLES20.GlCompileShader(shader);
				int[] compiled = new int[1];
				GLES20.GlGetShaderiv(shader, 35713, compiled, 0);
				if (compiled[0] == 0)
				{
					Log.Error("DistortionRenderer", "Could not compile shader " + shaderType + ":");
					Log.Error("DistortionRenderer", GLES20.GlGetShaderInfoLog(shader));
					GLES20.GlDeleteShader(shader);
					shader = 0;
				}
			}
			return shader;
		}

		private int createProgram(String vertexSource, String fragmentSource)
		{
			int vertexShader = loadShader(35633, vertexSource);
			if (vertexShader == 0)
			{
				return 0;
			}
			int pixelShader = loadShader(35632, fragmentSource);
			if (pixelShader == 0)
			{
				return 0;
			}

			int program = GLES20.GlCreateProgram();
			if (program != 0)
			{
				GLES20.GlAttachShader(program, vertexShader);
				checkGlError("glAttachShader");
				GLES20.GlAttachShader(program, pixelShader);
				checkGlError("glAttachShader");
				GLES20.GlLinkProgram(program);
				int[] linkStatus = new int[1];
				GLES20.GlGetProgramiv(program, 35714, linkStatus, 0);
				if (linkStatus[0] != 1)
				{
					Log.Error("DistortionRenderer", "Could not link program: ");
					Log.Error("DistortionRenderer", GLES20.GlGetProgramInfoLog(program));
					GLES20.GlDeleteProgram(program);
					program = 0;
				}
			}
			return program;
		}

		private ProgramHolder createProgramHolder()
		{
			ProgramHolder holder = new ProgramHolder();
			holder.program = createProgram("attribute vec2 aPosition;\nattribute float aVignette;\nattribute vec2 aTextureCoord;\nvarying vec2 vTextureCoord;\nvarying float vVignette;\nuniform float uTextureCoordScale;\nvoid main() {\n    gl_Position = vec4(aPosition, 0.0, 1.0);\n    vTextureCoord = aTextureCoord.xy * uTextureCoordScale;\n    vVignette = aVignette;\n}\n", "precision mediump float;\nvarying vec2 vTextureCoord;\nvarying float vVignette;\nuniform sampler2D uTextureSampler;\nvoid main() {\n    gl_FragColor = vVignette * texture2D(uTextureSampler, vTextureCoord);\n}\n");
			if (holder.program == 0)
			{
				throw new Java.Lang.RuntimeException("Could not create program");
			}

			holder.aPosition = GLES20.GlGetAttribLocation(holder.program, "aPosition");
			checkGlError("glGetAttribLocation aPosition");
			if (holder.aPosition == -1)
			{
				throw new Java.Lang.RuntimeException("Could not get attrib location for aPosition");
			}
			holder.aVignette = GLES20.GlGetAttribLocation(holder.program, "aVignette");
			checkGlError("glGetAttribLocation aVignette");
			if (holder.aVignette == -1)
			{
				throw new Java.Lang.RuntimeException("Could not get attrib location for aVignette");
			}
			holder.aTextureCoord = GLES20.GlGetAttribLocation(holder.program, "aTextureCoord");

			checkGlError("glGetAttribLocation aTextureCoord");
			if (holder.aTextureCoord == -1)
			{
				throw new Java.Lang.RuntimeException("Could not get attrib location for aTextureCoord");
			}
			holder.uTextureCoordScale = GLES20.GlGetUniformLocation(holder.program, "uTextureCoordScale");

			checkGlError("glGetUniformLocation uTextureCoordScale");
			if (holder.uTextureCoordScale == -1)
			{
				throw new Java.Lang.RuntimeException("Could not get attrib location for uTextureCoordScale");
			}
			holder.uTextureSampler = GLES20.GlGetUniformLocation(holder.program, "uTextureSampler");

			checkGlError("glGetUniformLocation uTextureSampler");
			if (holder.uTextureSampler == -1)
			{
				throw new Java.Lang.RuntimeException("Could not get attrib location for uTextureSampler");
			}

			return holder;
		}

		private void checkGlError(String op)
		{
			int error;
			if ((error = GLES20.GlGetError()) != 0)
			{
				Log.Error("DistortionRenderer", op + ": glError " + error);
				throw new Java.Lang.RuntimeException(op + ": glError " + error);
			}
		}

		private static float clamp(float val, float min, float max)
		{
			return Math.Max(min, Math.Min(max, val));
		}

		private class DistortionMesh
		{
			private const String TAG = "DistortionMesh";
			public const int BYTES_PER_FLOAT = 4;
			public const int BYTES_PER_INT = 4;
			public const int COMPONENTS_PER_VERT = 5;
			public const int DATA_STRIDE_BYTES = 20;

			public const int DATA_POS_OFFSET = 0;
			public const int DATA_VIGNETTE_OFFSET = 2;
			public const int DATA_UV_OFFSET = 3;
			public const int ROWS = 40;
			public const int COLS = 40;

			public const float VIGNETTE_SIZE_M_SCREEN = 0.002F;
			public int nIndices;
			public int mArrayBufferId = -1;
			public int mElementBufferId = -1;

			public DistortionMesh(EyeParams eye, Distortion distortion, float screenWidthM, float screenHeightM, float xEyeOffsetMScreen, float yEyeOffsetMScreen, float textureWidthM, float textureHeightM, float xEyeOffsetMTexture, float yEyeOffsetMTexture, float viewportXMTexture, float viewportYMTexture, float viewportWidthMTexture, float viewportHeightMTexture)
			{
				float mPerUScreen = screenWidthM;
				float mPerVScreen = screenHeightM;
				float mPerUTexture = textureWidthM;
				float mPerVTexture = textureHeightM;

				float[] vertexData = new float[8000];
				int vertexOffset = 0;

				for (int row = 0; row < 40; row++)
				{
					for (int col = 0; col < 40; col++)
					{
						float uTexture = col / 39.0F * (viewportWidthMTexture / textureWidthM) + viewportXMTexture / textureWidthM;

						float vTexture = row / 39.0F * (viewportHeightMTexture / textureHeightM) + viewportYMTexture / textureHeightM;

						float xTexture = uTexture * mPerUTexture;
						float yTexture = vTexture * mPerVTexture;
						float xTextureEye = xTexture - xEyeOffsetMTexture;
						float yTextureEye = yTexture - yEyeOffsetMTexture;
						float rTexture = (float)Math.Sqrt(xTextureEye * xTextureEye + yTextureEye * yTextureEye);

						float textureToScreen = rTexture > 0.0F ? distortion.distortInverse(rTexture) / rTexture : 1.0F;

						float xScreen = xTextureEye * textureToScreen + xEyeOffsetMScreen;
						float yScreen = yTextureEye * textureToScreen + yEyeOffsetMScreen;
						float uScreen = xScreen / mPerUScreen;
						float vScreen = yScreen / mPerVScreen;
						float vignetteSizeMTexture = 0.002F / textureToScreen;

						float dxTexture = xTexture - DistortionRenderer.clamp(xTexture, viewportXMTexture + vignetteSizeMTexture, viewportXMTexture + viewportWidthMTexture - vignetteSizeMTexture);

						float dyTexture = yTexture - DistortionRenderer.clamp(yTexture, viewportYMTexture + vignetteSizeMTexture, viewportYMTexture + viewportHeightMTexture - vignetteSizeMTexture);

						float drTexture = (float)Math.Sqrt(dxTexture * dxTexture + dyTexture * dyTexture);

						float vignette = 1.0F - DistortionRenderer.clamp(drTexture / vignetteSizeMTexture, 0.0F, 1.0F);

						vertexData[(vertexOffset + 0)] = (2.0F * uScreen - 1.0F);
						vertexData[(vertexOffset + 1)] = (2.0F * vScreen - 1.0F);
						vertexData[(vertexOffset + 2)] = vignette;
						vertexData[(vertexOffset + 3)] = uTexture;
						vertexData[(vertexOffset + 4)] = vTexture;

						vertexOffset += 5;
					}

				}

				nIndices = 3158;
				int[] indexData = new int[nIndices];
				int indexOffset = 0;
				vertexOffset = 0;
				for (int row = 0; row < 39; row++)
				{
					if (row > 0)
					{
						indexData[indexOffset] = indexData[(indexOffset - 1)];
						indexOffset++;
					}
					for (int col = 0; col < 40; col++)
					{
						if (col > 0)
						{
							if (row % 2 == 0)
							{
								vertexOffset++;
							}
							else
							{
								vertexOffset--;
							}
						}
						indexData[(indexOffset++)] = vertexOffset;
						indexData[(indexOffset++)] = (vertexOffset + 40);
					}
					vertexOffset += 40;
				}

				FloatBuffer vertexBuffer = ByteBuffer.AllocateDirect(vertexData.Length * 4).Order(ByteOrder.NativeOrder()).AsFloatBuffer();

				vertexBuffer.Put(vertexData).Position(0);

				IntBuffer indexBuffer = ByteBuffer.AllocateDirect(indexData.Length * 4).Order(ByteOrder.NativeOrder()).AsIntBuffer();

				indexBuffer.Put(indexData).Position(0);

				int[] bufferIds = new int[2];
				GLES20.GlGenBuffers(2, bufferIds, 0);
				mArrayBufferId = bufferIds[0];
				mElementBufferId = bufferIds[1];

				GLES20.GlBindBuffer(34962, mArrayBufferId);
				GLES20.GlBufferData(34962, vertexData.Length * 4, vertexBuffer, 35044);

				GLES20.GlBindBuffer(34963, mElementBufferId);
				GLES20.GlBufferData(34963, indexData.Length * 4, indexBuffer, 35044);

				GLES20.GlBindBuffer(34962, 0);
				GLES20.GlBindBuffer(34963, 0);
			}
		}

		private class EyeViewport
		{
			public float x;
			public float y;
			public float width;
			public float height;
			public float eyeX;
			public float eyeY;

			public EyeViewport()
			{
			}

			public String toString()
			{
				return "EyeViewport {x:" + x + " y:" + y + " width:" + width + " height:" + height + " eyeX: " + eyeX + " eyeY: " + eyeY + "}";
			}
		}

		private class ProgramHolder
		{
			public int program;
			public int aPosition;
			public int aVignette;
			public int aTextureCoord;
			public int uTextureCoordScale;
			public int uTextureSampler;

			public ProgramHolder()
			{
			}
		}
	}
}
 