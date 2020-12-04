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
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;

using Android.Nfc;
using Android.Nfc.Tech;
using Android.Util;

using Java.Util;

namespace Com.Google.VRToolkit.CardBoard.Sensors
{
    public class NfcSensor
    {
        public const String NFC_DATA_SCHEME = "cardboard";
		public const String FIRST_TAG_VERSION = "v1.0.0";
		private const String TAG = "NfcSensor";
		private const int MAX_CONNECTION_FAILURES = 1;
		private const long NFC_POLLING_INTERVAL_MS = 250L;
		private static NfcSensor sInstance;
		private Context mContext;
		private NfcAdapter mNfcAdapter;
		private Object mTagLock;
		private List<ListenerHelper> mListeners;
		private IntentFilter[] mNfcIntentFilters;
		private volatile Ndef mCurrentTag;
		private Timer mNfcDisconnectTimer;
		private int mTagConnectionFailures;

		public static NfcSensor getInstance(Context context)
		{
			if (sInstance == null)
			{
				sInstance = new NfcSensor(context);
			}

			return sInstance;
		}

		private NfcSensor(Context context)
		{
			mContext = context.ApplicationContext;
			mNfcAdapter = NfcAdapter.GetDefaultAdapter(mContext);
			mListeners = new List<ListenerHelper>();
			mTagLock = new Object();

			if (mNfcAdapter == null)
			{
				return;
			}

			IntentFilter ndefIntentFilter = new IntentFilter("android.nfc.action.NDEF_DISCOVERED");
			ndefIntentFilter.AddDataScheme(NFC_DATA_SCHEME);
			mNfcIntentFilters = new IntentFilter[] { ndefIntentFilter };

			mContext.RegisterReceiver(new MyBroadcastReciever(this), ndefIntentFilter);
		}

		public void addOnCardboardNfcListener(OnCardboardNfcListener listener)
		{
			if (listener == null)
			{
				return;
			}

			lock(mListeners) {
				foreach (ListenerHelper helper in mListeners)
				{
					if (helper.getListener() == listener)
					{
						return;
					}
				}

				mListeners.Add(new ListenerHelper(listener, new Handler()));
			}
		}

		public void removeOnCardboardNfcListener(OnCardboardNfcListener listener)
		{
			if (listener == null)
			{
				return;
			}

			lock(mListeners) {
				foreach (ListenerHelper helper in mListeners)
					if (helper.getListener() == listener)
					{
						mListeners.Remove(helper);
						return;
					}
			}
		}

		public bool isNfcSupported()
		{
			return mNfcAdapter != null;
		}

		public bool isNfcEnabled()
		{
			return (isNfcSupported()) && (mNfcAdapter.IsEnabled);
		}

		public bool isDeviceInCardboard()
		{
			return mCurrentTag != null;
		}

		public CardboardDeviceParams getCardboardDeviceParams()
		{
			NdefMessage tagContents = null;
			lock(mTagLock) {
				try
				{
					tagContents = mCurrentTag.CachedNdefMessage;
				}
				catch (Exception e)
				{
					return null;
				}
			}

			if (tagContents == null)
			{
				return null;
			}

			return CardboardDeviceParams.createFromNfcContents(tagContents);
		}

		public void onResume(Activity activity)
		{
			if (!isNfcEnabled())
			{
				return;
			}

			Intent intent = new Intent("android.nfc.action.NDEF_DISCOVERED");
			intent.SetPackage(activity.PackageName);

			PendingIntent pendingIntent = PendingIntent.GetBroadcast(mContext, 0, intent, 0);
			mNfcAdapter.EnableForegroundDispatch(activity, pendingIntent, mNfcIntentFilters, null);
		}

		public void onPause(Activity activity)
		{
			if (!isNfcEnabled())
			{
				return;
			}

			mNfcAdapter.DisableForegroundDispatch(activity);
		}


		private void closeCurrentNfcTag()
		{
			if (mNfcDisconnectTimer != null)
			{
				mNfcDisconnectTimer.Cancel();
			}

			try
			{
				mCurrentTag.Close();
			}
			catch (Java.IO.IOException e)
			{
				Log.Warn(TAG, e.ToString());
			}

			mCurrentTag = null;
		}

		private void sendDisconnectionEvent()
		{
			lock(mListeners) {
				foreach (ListenerHelper listener in mListeners)
					listener.OnRemovedFromCardboard();
			}
		}

		public void onNfcIntent(Intent intent)
		{
			if ((!isNfcEnabled()) || (intent == null) || (!"android.nfc.action.NDEF_DISCOVERED".Equals(intent.Action)))
			{
				return;
			}

			Android.Net.Uri uri = intent.Data;
			Tag nfcTag = (Tag)intent.GetParcelableExtra("android.nfc.extra.TAG");
			if ((uri == null) || (nfcTag == null))
			{
				return;
			}

			Ndef ndef = Ndef.Get(nfcTag);
			if ((ndef == null) || (!uri.Scheme.Equals(NFC_DATA_SCHEME)) || ((!uri.Host.Equals(FIRST_TAG_VERSION)) && (uri.PathSegments.Count == 2)))
			{
				return;
			}

			lock (mTagLock)
			{
				bool isSameTag = false;

				if (mCurrentTag != null)
				{
					byte[] tagId1 = nfcTag.GetId();
					byte[] tagId2 = mCurrentTag.Tag.GetId();
					isSameTag = (tagId1 != null) && (tagId2 != null) && (Arrays.Equals(tagId1, tagId2));

					closeCurrentNfcTag();
					if (!isSameTag)
					{
						sendDisconnectionEvent();
					}
				}

				NdefMessage nfcTagContents;
				try
				{
					ndef.Connect();
					nfcTagContents = ndef.CachedNdefMessage;
				}
				catch (Exception e)
				{
					Log.Error(TAG, "Error reading NFC tag: " + e.ToString());

					if (isSameTag)
					{
						sendDisconnectionEvent();
					}

					return;
				}

				mCurrentTag = ndef;

				if (!isSameTag)
				{
					lock (mListeners)
					{
						foreach (ListenerHelper listener in mListeners)
						{
							listener.OnInsertedIntoCardboard(CardboardDeviceParams.createFromNfcContents(nfcTagContents));
						}

					}

				}

				mTagConnectionFailures = 0;
				mNfcDisconnectTimer = new Timer("NFC disconnect timer");
				mNfcDisconnectTimer.Schedule(new MyTimerTask(this), NFC_POLLING_INTERVAL_MS, NFC_POLLING_INTERVAL_MS);
			}
		}


        private class MyTimerTask : TimerTask
        {
			private NfcSensor mSensor;
			public MyTimerTask(NfcSensor sensor)
			{
				mSensor = sensor;
			}

            public override void Run()
            {
				lock (mSensor.mTagLock)
				{
					if (!mSensor.mCurrentTag.IsConnected)
					{
						// NfcSensor.access$204(NfcSensor.this);

						if (mSensor.mTagConnectionFailures > MAX_CONNECTION_FAILURES)
						{
							mSensor.closeCurrentNfcTag();
							mSensor.sendDisconnectionEvent();
						}
					}
				}
            }
        }

        private class MyBroadcastReciever : BroadcastReceiver
        {
			private NfcSensor mSensor;
			public MyBroadcastReciever(NfcSensor sensor)
			{
				mSensor = sensor;
			}

            public override void OnReceive(Context context, Intent intent)
            {
				mSensor.onNfcIntent(intent);
			}
        }

        private class ListenerHelper : NfcSensor.OnCardboardNfcListener
		{
			private NfcSensor.OnCardboardNfcListener mListener;
			private Handler mHandler;

			public ListenerHelper(NfcSensor.OnCardboardNfcListener listener, Handler handler)
			{
				mListener = listener;
				mHandler = handler;
			}

			public NfcSensor.OnCardboardNfcListener getListener()
			{
				return mListener;
			}

			public void OnInsertedIntoCardboard(CardboardDeviceParams deviceParams)
			{
				mHandler.Post(() =>
				{
					mListener.OnInsertedIntoCardboard(deviceParams);
				});
			}

			public void OnRemovedFromCardboard()
			{
				mHandler.Post(() =>
				{
					mListener.OnRemovedFromCardboard();
				});
			}
		}

		/** Interface for listeners of Cardboard NFC events. */
		public interface OnCardboardNfcListener
		{
			void OnInsertedIntoCardboard(CardboardDeviceParams paramCardboardDeviceParams);

			void OnRemovedFromCardboard();
		}

	}
}
