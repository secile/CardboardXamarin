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

using System;

using Android.Views;
using Android.Util;

namespace Com.Google.VRToolkit.CardBoard
{
    public class ScreenParams
    {
		public const float METERS_PER_INCH = 0.0254F;
		private const float DEFAULT_BORDER_SIZE_METERS = 0.003F;
		private int mWidth;
		private int mHeight;
		private float mXMetersPerPixel;
		private float mYMetersPerPixel;
		private float mBorderSizeMeters;

		public ScreenParams(Display display)
		{
			DisplayMetrics metrics = new DisplayMetrics();
			try
			{
				display.GetRealMetrics(metrics);
			}
			catch (Java.Lang.NoSuchMethodError e)
			{
				display.GetMetrics(metrics);
			}

			mXMetersPerPixel = (0.0254F / metrics.Xdpi);
			mYMetersPerPixel = (0.0254F / metrics.Ydpi);
			mWidth = metrics.WidthPixels;
			mHeight = metrics.HeightPixels;
			mBorderSizeMeters = 0.003F;

			if (mHeight > mWidth)
			{
				int tempPx = mWidth;
				mWidth = mHeight;
				mHeight = tempPx;

				float tempMetersPerPixel = mXMetersPerPixel;
				mXMetersPerPixel = mYMetersPerPixel;
				mYMetersPerPixel = tempMetersPerPixel;
			}
		}

		public ScreenParams(ScreenParams param)
		{
			mWidth = param.mWidth;
			mHeight = param.mHeight;
			mXMetersPerPixel = param.mXMetersPerPixel;
			mYMetersPerPixel = param.mYMetersPerPixel;
			mBorderSizeMeters = param.mBorderSizeMeters;
		}

		public void setWidth(int width)
		{
			mWidth = width;
		}

		public int getWidth()
		{
			return mWidth;
		}

		public void setHeight(int height)
		{
			mHeight = height;
		}

		public int getHeight()
		{
			return mHeight;
		}

		public float getWidthMeters()
		{
			return mWidth * mXMetersPerPixel;
		}

		public float getHeightMeters()
		{
			return mHeight * mYMetersPerPixel;
		}

		public void setBorderSizeMeters(float screenBorderSize)
		{
			mBorderSizeMeters = screenBorderSize;
		}

		public float getBorderSizeMeters()
		{
			return mBorderSizeMeters;
		}

		public bool equals(Object other)
		{
			if (other == null)
			{
				return false;
			}

			if (other == this)
			{
				return true;
			}

			if (!(other is ScreenParams)) {
				return false;
			}

			ScreenParams o = (ScreenParams)other;

			return (mWidth == o.mWidth) && (mHeight == o.mHeight) && (mXMetersPerPixel == o.mXMetersPerPixel) && (mYMetersPerPixel == o.mYMetersPerPixel) && (mBorderSizeMeters == o.mBorderSizeMeters);
		}
	}
}