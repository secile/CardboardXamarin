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

namespace Com.Google.VRToolkit.CardBoard
{
	public class EyeParams
    {
		private int mEye;
		private Viewport mViewport;
		private FieldOfView mFov;
		private EyeTransform mEyeTransform;

		public EyeParams(int eye)
		{
			mEye = eye;
			mViewport = new Viewport();
			mFov = new FieldOfView();
			mEyeTransform = new EyeTransform(this);
		}

		public int getEye()
		{
			return mEye;
		}

		public Viewport getViewport()
		{
			return mViewport;
		}

		public FieldOfView getFov()
		{
			return mFov;
		}

		public EyeTransform getTransform()
		{
			return mEyeTransform;
		}

		/** Defines the constants identifying the current eye. */
		public static class Eye
		{
			public const int MONOCULAR = 0;
			public const int LEFT = 1;
			public const int RIGHT = 2;
		}
	}
}