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

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Hardware;

using Android.Opengl;

using Com.Google.VRToolkit.CardBoard.Sensors.Internal;

namespace Com.Google.VRToolkit.CardBoard.Sensors
{
	public class HeadTracker
    {
		private const double NS2S = 1E-09D;
		private static SensorType[] INPUT_SENSORS = { SensorType.Accelerometer, SensorType.Gyroscope, SensorType.MagneticField };
		private Context mContext;
		private float[] mEkfToHeadTracker = new float[16];

		private float[] mTmpHeadView = new float[16];

		private float[] mTmpRotatedEvent = new float[3];
		private Looper mSensorLooper;
		private SensorEventListener mSensorEventListener;
		private bool mTracking;
		private OrientationEKF mTracker = new OrientationEKF();
		private long mLastGyroEventTimeNanos;

		private bool mAlignedToNorth;
		public void setAlignedToNorth(bool flag) { mAlignedToNorth = flag; }

		public HeadTracker(Context context)
		{
			mContext = context;
			Matrix.SetRotateEulerM(mEkfToHeadTracker, 0, -90.0F, 0.0F, 0.0F);
		}

		private class SensorEventListener : Java.Lang.Object, ISensorEventListener
		{
			private readonly HeadTracker Owner;

			public SensorEventListener(HeadTracker owner) { this.Owner = owner; }
			
			public void OnSensorChanged(SensorEvent e) { this.Owner.processSensorEvent(e); }

			public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy) { }
		}

		public void startTracking()
		{
			if (mTracking)
			{
				return;
			}
			mTracker.reset();

			mSensorEventListener = new SensorEventListener(this);

			Java.Lang.Thread sensorThread = new Java.Lang.Thread(() =>
			{
				Looper.Prepare();

				mSensorLooper = Looper.MyLooper();
				Handler handler = new Handler();

				SensorManager sensorManager = mContext.GetSystemService(Context.SensorService) as SensorManager;

				foreach (var item in INPUT_SENSORS)
                {
					var sensor = sensorManager.GetDefaultSensor(item);
					if (sensor != null) sensorManager.RegisterListener(mSensorEventListener, sensor, SensorDelay.Fastest, handler);
				}

				Looper.Loop();
			});

			sensorThread.Start();
			mTracking = true;
		}

		public void stopTracking()
		{
			if (!mTracking)
			{
				return;
			}

			SensorManager sensorManager = mContext.GetSystemService(Context.SensorService) as SensorManager;
			sensorManager.UnregisterListener(mSensorEventListener);
			mSensorEventListener = null;

			mSensorLooper.Quit();
			mSensorLooper = null;
			mTracking = false;
		}

		public void getLastHeadView(float[] headView, int offset)
		{
			if (offset + 16 > headView.Length)
			{
				throw new Java.Lang.IllegalArgumentException("Not enough space to write the result");
			}
			
			lock(mTracker) {
				double secondsSinceLastGyroEvent = (Java.Lang.JavaSystem.NanoTime() - mLastGyroEventTimeNanos) * NS2S;
				double secondsToPredictForward = secondsSinceLastGyroEvent + 0.03333333333333333D;
				double[] mat = mTracker.getPredictedGLMatrix(secondsToPredictForward);
				for (int i = 0; i < headView.Length; i++)
				{
					mTmpHeadView[i] = ((float)mat[i]);
				}
			}

			Matrix.MultiplyMM(headView, offset, mTmpHeadView, 0, mEkfToHeadTracker, 0);
		}

		private void processSensorEvent(SensorEvent e)
		{
			long timeNanos = Java.Lang.JavaSystem.NanoTime();

			mTmpRotatedEvent[0] = (-e.Values[1]);
			mTmpRotatedEvent[1] = e.Values[0];
			mTmpRotatedEvent[2] = e.Values[2];
			lock (mTracker)
			{
				if (e.Sensor.Type == SensorType.Accelerometer)
				{
					mTracker.processAcc(mTmpRotatedEvent, e.Timestamp);
				}
				else if (e.Sensor.Type == SensorType.Gyroscope)
				{
					mLastGyroEventTimeNanos = timeNanos;
					mTracker.processGyro(mTmpRotatedEvent, e.Timestamp);
				}
				else if (e.Sensor.Type == SensorType.MagneticField)
				{
					if (mAlignedToNorth)
                    {
						mTracker.processMag(mTmpRotatedEvent, e.Timestamp);
					}
				}
			}
		}
	}
}