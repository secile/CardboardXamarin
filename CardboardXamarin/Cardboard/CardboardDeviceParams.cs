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

using Android.Util;
using Android.Nfc;

namespace Com.Google.VRToolkit.CardBoard
{
    public class CardboardDeviceParams
    {
		private const String TAG = "CardboardDeviceParams";
		private const String DEFAULT_VENDOR = "com.google";
		private const String DEFAULT_MODEL = "cardboard";
		private const String DEFAULT_VERSION = "1.0";
		private const float DEFAULT_INTERPUPILLARY_DISTANCE = 0.06F;
		private const float DEFAULT_VERTICAL_DISTANCE_TO_LENS_CENTER = 0.035F;
		private const float DEFAULT_LENS_DIAMETER = 0.025F;
		private const float DEFAULT_SCREEN_TO_LENS_DISTANCE = 0.037F;
		private const float DEFAULT_EYE_TO_LENS_DISTANCE = 0.011F;
		private const float DEFAULT_VISIBLE_VIEWPORT_MAX_SIZE = 0.06F;
		private const float DEFAULT_FOV_Y = 65.0F;
		private NdefMessage mNfcTagContents;
		private String mVendor;
		private String mModel;
		private String mVersion;
		private float mInterpupillaryDistance;
		private float mVerticalDistanceToLensCenter;
		private float mLensDiameter;
		private float mScreenToLensDistance;
		private float mEyeToLensDistance;
		private float mVisibleViewportSize;
		private float mFovY;
		private Distortion mDistortion;

		public CardboardDeviceParams()
		{
			mVendor = DEFAULT_VENDOR;
			mModel = DEFAULT_MODEL;
			mVersion = DEFAULT_VERSION;

			mInterpupillaryDistance = DEFAULT_INTERPUPILLARY_DISTANCE;
			mVerticalDistanceToLensCenter = DEFAULT_VERTICAL_DISTANCE_TO_LENS_CENTER;
			mLensDiameter = DEFAULT_LENS_DIAMETER;
			mScreenToLensDistance = DEFAULT_SCREEN_TO_LENS_DISTANCE;
			mEyeToLensDistance = DEFAULT_EYE_TO_LENS_DISTANCE;

			mVisibleViewportSize = DEFAULT_VISIBLE_VIEWPORT_MAX_SIZE;
			mFovY = DEFAULT_FOV_Y;

			mDistortion = new Distortion();
		}

		public CardboardDeviceParams(CardboardDeviceParams @params)
		{
			mNfcTagContents = @params.mNfcTagContents;

			mVendor = @params.mVendor;
			mModel = @params.mModel;
			mVersion = @params.mVersion;

			mInterpupillaryDistance = @params.mInterpupillaryDistance;
			mVerticalDistanceToLensCenter = @params.mVerticalDistanceToLensCenter;
			mLensDiameter = @params.mLensDiameter;
			mScreenToLensDistance = @params.mScreenToLensDistance;
			mEyeToLensDistance = @params.mEyeToLensDistance;

			mVisibleViewportSize = @params.mVisibleViewportSize;
			mFovY = @params.mFovY;

			mDistortion = new Distortion(@params.mDistortion);
		}

		public static CardboardDeviceParams createFromNfcContents(NdefMessage tagContents)
		{
			if (tagContents == null)
			{
				Log.Warn("CardboardDeviceParams", "Could not get contents from NFC tag.");
				return null;
			}

			CardboardDeviceParams deviceParams = new CardboardDeviceParams();

			foreach (NdefRecord record in tagContents.GetRecords())
			{
				if (deviceParams.parseNfcUri(record))
				{
					break;
				}
			}

			return deviceParams;
		}

		public NdefMessage getNfcTagContents()
		{
			return mNfcTagContents;
		}

		public void setVendor(String vendor)
		{
			mVendor = vendor;
		}

		public String getVendor()
		{
			return mVendor;
		}

		public void setModel(String model)
		{
			mModel = model;
		}

		public String getModel()
		{
			return mModel;
		}

		public void setVersion(String version)
		{
			mVersion = version;
		}

		public String getVersion()
		{
			return mVersion;
		}

		public void setInterpupillaryDistance(float interpupillaryDistance)
		{
			mInterpupillaryDistance = interpupillaryDistance;
		}

		public float getInterpupillaryDistance()
		{
			return mInterpupillaryDistance;
		}

		public void setVerticalDistanceToLensCenter(float verticalDistanceToLensCenter)
		{
			mVerticalDistanceToLensCenter = verticalDistanceToLensCenter;
		}

		public float getVerticalDistanceToLensCenter()
		{
			return mVerticalDistanceToLensCenter;
		}

		public void setVisibleViewportSize(float visibleViewportSize)
		{
			mVisibleViewportSize = visibleViewportSize;
		}

		public float getVisibleViewportSize()
		{
			return mVisibleViewportSize;
		}

		public void setFovY(float fovY)
		{
			mFovY = fovY;
		}

		public float getFovY()
		{
			return mFovY;
		}

		public void setLensDiameter(float lensDiameter)
		{
			mLensDiameter = lensDiameter;
		}

		public float getLensDiameter()
		{
			return mLensDiameter;
		}

		public void setScreenToLensDistance(float screenToLensDistance)
		{
			mScreenToLensDistance = screenToLensDistance;
		}

		public float getScreenToLensDistance()
		{
			return mScreenToLensDistance;
		}

		public void setEyeToLensDistance(float eyeToLensDistance)
		{
			mEyeToLensDistance = eyeToLensDistance;
		}

		public float getEyeToLensDistance()
		{
			return mEyeToLensDistance;
		}

		public Distortion getDistortion()
		{
			return mDistortion;
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

			if (!(other is CardboardDeviceParams)) {
				return false;
			}

			CardboardDeviceParams o = (CardboardDeviceParams)other;

			return (mVendor == o.mVendor) && (mModel == o.mModel) && (mVersion == o.mVersion) && (mInterpupillaryDistance == o.mInterpupillaryDistance) && (mVerticalDistanceToLensCenter == o.mVerticalDistanceToLensCenter) && (mLensDiameter == o.mLensDiameter) && (mScreenToLensDistance == o.mScreenToLensDistance) && (mEyeToLensDistance == o.mEyeToLensDistance) && (mVisibleViewportSize == o.mVisibleViewportSize) && (mFovY == o.mFovY) && (mDistortion.equals(o.mDistortion));
		}

		private bool parseNfcUri(NdefRecord record)
		{
			var uri = record.ToUri();
			if (uri == null)
			{
				return false;
			}

			if (uri.Host.Equals("v1.0.0"))
			{
				mVendor = DEFAULT_VENDOR;
				mModel = DEFAULT_MODEL;
				mVersion = DEFAULT_VERSION;
				return true;
			}

			var segments = uri.PathSegments;
			if (segments.Count != 2)
			{
				return false;
			}

			mVendor = uri.Host;
			mModel = ((String)segments[0]);
			mVersion = ((String)segments[1]);

			return true;
		}
	}
}