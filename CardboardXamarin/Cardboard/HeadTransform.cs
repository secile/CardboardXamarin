﻿/*
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

using Android.Opengl;

using Java.Lang;

namespace Com.Google.VRToolkit.CardBoard
{
    public class HeadTransform
    {
		private const float GIMBAL_LOCK_EPSILON = 0.01F;
		private const float PI = 3.141593F;
		private float[] mHeadView;

		public HeadTransform()
		{
			mHeadView = new float[16];
			Matrix.SetIdentityM(mHeadView, 0);
		}

		public float[] getHeadView()
		{
			return mHeadView;
		}

		public void getHeadView(float[] headView, int offset)
		{
			if (offset + 16 > headView.Length)
			{
				throw new IllegalArgumentException("Not enough space to write the result");
			}

			//Java.Lang.JavaSystem.Arraycopy(mHeadView, 0, headView, offset, 16);
			System.Array.Copy(mHeadView, 0, headView, offset, 16);
		}

		public void getTranslation(float[] translation, int offset)
		{
			if (offset + 3 > translation.Length)
			{
				throw new IllegalArgumentException("Not enough space to write the result");
			}

			for (int i = 0; i < 3; i++)
				translation[(i + offset)] = mHeadView[(12 + i)];
		}

		public void getForwardVector(float[] forward, int offset)
		{
			if (offset + 3 > forward.Length)
			{
				throw new IllegalArgumentException("Not enough space to write the result");
			}

			for (int i = 0; i < 3; i++)
				forward[i + offset] = -this.mHeadView[2 + (i << 2)]; // fix issue#2.
		}

		public void getUpVector(float[] up, int offset)
		{
			if (offset + 3 > up.Length)
			{
				throw new IllegalArgumentException("Not enough space to write the result");
			}

			for (int i = 0; i < 3; i++)
				up[i + offset] = this.mHeadView[1 + (i << 2)]; // fix issue#2.
		}

		public void getRightVector(float[] right, int offset)
		{
			if (offset + 3 > right.Length)
			{
				throw new IllegalArgumentException("Not enough space to write the result");
			}

			for (int i = 0; i < 3; i++)
				right[i + offset] = this.mHeadView[i << 2]; // fix issue#2.
		}

		public void getQuaternion(float[] quaternion, int offset)
		{
			if (offset + 4 > quaternion.Length)
			{
				throw new IllegalArgumentException("Not enough space to write the result");
			}

			float[] m = mHeadView;
			float t = m[0] + m[5] + m[10];
			float s, w, x, y, z;
			if (t >= 0.0F)
			{
				s = (float)Math.Sqrt(t + 1.0F);
				w = 0.5F * s;
				s = 0.5F / s;
				x = (m[9] - m[6]) * s;
				y = (m[2] - m[8]) * s;
				z = (m[4] - m[1]) * s;
			}
			else
			{
				if ((m[0] > m[5]) && (m[0] > m[10]))
				{
					s = (float)Math.Sqrt(1.0F + m[0] - m[5] - m[10]);
					x = s * 0.5F;
					s = 0.5F / s;
					y = (m[4] + m[1]) * s;
					z = (m[2] + m[8]) * s;
					w = (m[9] - m[6]) * s;
				}
				else
				{
					if (m[5] > m[10])
					{
						s = (float)Math.Sqrt(1.0F + m[5] - m[0] - m[10]);
						y = s * 0.5F;
						s = 0.5F / s;
						x = (m[4] + m[1]) * s;
						z = (m[9] + m[6]) * s;
						w = (m[2] - m[8]) * s;
					}
					else
					{
						s = (float)Math.Sqrt(1.0F + m[10] - m[0] - m[5]);
						z = s * 0.5F;
						s = 0.5F / s;
						x = (m[2] + m[8]) * s;
						y = (m[9] + m[6]) * s;
						w = (m[4] - m[1]) * s;
					}
				}
			}
			quaternion[(offset + 0)] = x;
			quaternion[(offset + 1)] = y;
			quaternion[(offset + 2)] = z;
			quaternion[(offset + 3)] = w;
		}

		public void getEulerAngles(float[] eulerAngles, int offset)
		{
			if (offset + 3 > eulerAngles.Length)
			{
				throw new IllegalArgumentException("Not enough space to write the result");
			}

			float yaw, roll, pitch = (float)Math.Asin(mHeadView[6]);
			if ((float)Math.Sqrt(1.0F - mHeadView[6] * mHeadView[6]) >= GIMBAL_LOCK_EPSILON)
			{
				yaw = (float)Math.Atan2(-mHeadView[2], mHeadView[10]);
				roll = (float)Math.Atan2(-mHeadView[4], mHeadView[5]);
			}
			else
			{
				yaw = 0.0F;
				roll = (float)Math.Atan2(mHeadView[1], mHeadView[0]);
			}

			eulerAngles[(offset + 0)] = (-pitch);
			eulerAngles[(offset + 1)] = (-yaw);
			eulerAngles[(offset + 2)] = (-roll);
		}
	}
}