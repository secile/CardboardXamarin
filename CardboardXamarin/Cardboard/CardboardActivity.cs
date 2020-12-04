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

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;

using Java.Lang;

using Com.Google.VRToolkit.CardBoard.Sensors;

namespace Com.Google.VRToolkit.CardBoard
{
    public class CardboardActivity : Activity, MagnetSensor.OnCardboardTriggerListener, NfcSensor.OnCardboardNfcListener
    {
        private const int NAVIGATION_BAR_TIMEOUT_MS = 2000;
        private CardboardView mCardboardView;
        private MagnetSensor mMagnetSensor;
        private NfcSensor mNfcSensor;
        private int mVolumeKeysMode;

		public void SetCardboardView(CardboardView cardboardView)
		{
			mCardboardView = cardboardView;

			if (cardboardView != null)
			{
				CardboardDeviceParams cardboardDeviceParams = mNfcSensor.getCardboardDeviceParams();
				if (cardboardDeviceParams == null)
				{
					cardboardDeviceParams = new CardboardDeviceParams();
				}

				cardboardView.UpdateCardboardDeviceParams(cardboardDeviceParams);
			}
		}

		public CardboardView GetCardboardView()
		{
			return mCardboardView;
		}

		public void SetVolumeKeysMode(int mode)
		{
			mVolumeKeysMode = mode;
		}

		public int GetVolumeKeysMode()
		{
			return mVolumeKeysMode;
		}

		public bool AreVolumeKeysDisabled()
		{
			switch (mVolumeKeysMode)
			{
				case VolumeKeys.NOT_DISABLED:
					return false;
				case VolumeKeys.DISABLED_WHILE_IN_CARDBOARD:
					return IsDeviceInCardboard();
				case VolumeKeys.DISABLED:
					return true;
			}

			throw new IllegalStateException("Invalid volume keys mode " + mVolumeKeysMode);
		}

		public bool IsDeviceInCardboard()
		{
			return mNfcSensor.isDeviceInCardboard();
		}

		public void OnInsertedIntoCardboard(CardboardDeviceParams deviceParams)
		{
			if (mCardboardView != null)
				mCardboardView.UpdateCardboardDeviceParams(deviceParams);
		}

		public void OnRemovedFromCardboard()
		{
		}

		public void OnCardboardTrigger()
		{
		}

		protected void OnNfcIntent(Intent intent)
		{
			mNfcSensor.onNfcIntent(intent);
		}

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			RequestWindowFeature( WindowFeatures.NoTitle);

			Window.AddFlags( WindowManagerFlags.KeepScreenOn);

			mMagnetSensor = new MagnetSensor(this);
			mMagnetSensor.setOnCardboardTriggerListener(this);

			mNfcSensor = NfcSensor.getInstance(this);
			mNfcSensor.addOnCardboardNfcListener(this);

			OnNfcIntent(Intent);

			SetVolumeKeysMode(VolumeKeys.DISABLED_WHILE_IN_CARDBOARD);

			if (Build.VERSION.SdkInt < BuildVersionCodes.Kitkat)
			{
				var listener = new OnSystemUiVisibilityChangeListener(this);
				Window.DecorView.SetOnSystemUiVisibilityChangeListener(listener);
			}
		}

		private class OnSystemUiVisibilityChangeListener : Java.Lang.Object, View.IOnSystemUiVisibilityChangeListener
		{
			private CardboardActivity mActivity;
			private Handler mHandler;

			public OnSystemUiVisibilityChangeListener(CardboardActivity activity)
			{
				mActivity = activity;
				mHandler = new Handler();
			}

			public void OnSystemUiVisibilityChange([GeneratedEnum] StatusBarVisibility visibility)
			{
				if (((int)visibility & 0x2) == 0)
				{
					mHandler.PostDelayed(() =>
					{
						mActivity.SetFullscreenMode();
					}, NAVIGATION_BAR_TIMEOUT_MS);
				}

			}
		}

		protected override void OnResume()
		{
			base.OnResume();

			if (mCardboardView != null)
			{
				mCardboardView.OnResume();
			}

			mMagnetSensor.start();
			mNfcSensor.onResume(this);
		}

		protected override void OnPause()
		{
			base.OnPause();

			if (mCardboardView != null)
			{
				mCardboardView.OnPause();
			}

			mMagnetSensor.stop();
			mNfcSensor.onPause(this);
		}

		protected override void OnDestroy()
		{
			mNfcSensor.removeOnCardboardNfcListener(this);
			base.OnDestroy();
		}

		public override void SetContentView(View view)
		{
			if ((view is CardboardView)) {
				SetCardboardView((CardboardView)view);
			}

			base.SetContentView(view);
		}

		public override void SetContentView(View view, ViewGroup.LayoutParams param)
		{
			if ((view is CardboardView)) {
				SetCardboardView((CardboardView)view);
			}

			base.SetContentView(view, param);
		}

		public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
		{
			if (keyCode == Keycode.VolumeUp || keyCode == Keycode.VolumeDown)
			{
				mMagnetSensor.fakeTrigger();
				return true;
			}

			if (((keyCode == Keycode.VolumeUp) || (keyCode == Keycode.VolumeDown)) && (AreVolumeKeysDisabled()))
			{
				return true;
			}

			return base.OnKeyDown(keyCode, e);
		}

		public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
		{
			if (((keyCode == Keycode.VolumeUp) || (keyCode == Keycode.VolumeDown)) && (AreVolumeKeysDisabled()))
			{
				return true;
			}

			return base.OnKeyUp(keyCode, e);
		}

		public override void OnWindowFocusChanged(bool hasFocus)
		{
			base.OnWindowFocusChanged(hasFocus);

			if (hasFocus)
				SetFullscreenMode();
		}

		private void SetFullscreenMode()
		{
			// 5894 = 4096(ImmersiveSticky) + 1536(LayoutFlags) + 256(LayoutStable) + 4(Fullscreen) + 2(HideNavigation)
			Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(5894);
		}

		/** Defines the constants with options for managing the volume keys.  */
		public static class VolumeKeys
		{
			public const int NOT_DISABLED = 0;
			public const int DISABLED = 1;
			public const int DISABLED_WHILE_IN_CARDBOARD = 2;
		}
	}
}