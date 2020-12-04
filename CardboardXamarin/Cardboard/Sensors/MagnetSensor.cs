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
using System.Collections.Generic;
using System.Linq;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Hardware;

namespace Com.Google.VRToolkit.CardBoard.Sensors
{
    public class MagnetSensor
    {
		private TriggerDetector mDetector;
		private Java.Lang.Thread mDetectorThread;

		public MagnetSensor(Context context)
		{
			mDetector = new TriggerDetector(context);
		}

		public void start()
		{
			mDetectorThread = new Java.Lang.Thread(mDetector);
			mDetectorThread.Start();
		}

		public void stop()
		{
			if (mDetectorThread != null)
			{
				mDetectorThread.Interrupt();
				mDetector.stop();
			}
		}

		public void setOnCardboardTriggerListener(OnCardboardTriggerListener listener)
		{
			mDetector.setOnCardboardTriggerListener(listener, new Handler());
		}

		public void fakeTrigger()
		{
			mDetector.handleButtonPressed();
		}

		private class TriggerDetector : Java.Lang.Object, Java.Lang.IRunnable, ISensorEventListener
		{
			private const String TAG = "TriggerDetector";
			private const int SEGMENT_SIZE = 20;
			private const int NUM_SEGMENTS = 2;
			private const int WINDOW_SIZE = 40;
			private const int T1 = 30;
			private const int T2 = 130;
			private SensorManager mSensorManager;
			private Sensor mMagnetometer;
			private List<float[]> mSensorData;
			private float[] mOffsets = new float[SEGMENT_SIZE];
			private MagnetSensor.OnCardboardTriggerListener mListener;
			private Handler mHandler;

			public TriggerDetector(Context context)
			{
				mSensorData = new List<float[]>();
				mSensorManager = ((SensorManager)context.GetSystemService(Context.SensorService));
				mMagnetometer = mSensorManager.GetDefaultSensor(SensorType.MagneticField);
			}

			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
			public /*synchronized*/ void setOnCardboardTriggerListener(MagnetSensor.OnCardboardTriggerListener listener, Handler handler)
			{
				mListener = listener;
				mHandler = handler;
			}

			private void addData(float[] values, long time)
			{
				if (mSensorData.Count > WINDOW_SIZE)
				{
					mSensorData.RemoveAt(0);
				}
				mSensorData.Add(values);

				evaluateModel();
			}

			private void evaluateModel()
			{
				if (mSensorData.Count < WINDOW_SIZE)
				{
					return;
				}

				float[] means = new float[NUM_SEGMENTS];
				float[] maximums = new float[NUM_SEGMENTS];
				float[] minimums = new float[NUM_SEGMENTS];

				float[] baseline = (float[])mSensorData[mSensorData.Count - 1];

				for (int i = 0; i < NUM_SEGMENTS; i++)
				{
					int segmentStart = SEGMENT_SIZE * i;

					float[] mOffsets = computeOffsets(segmentStart, baseline);

					means[i] = computeMean(mOffsets);
					maximums[i] = computeMaximum(mOffsets);
					minimums[i] = computeMinimum(mOffsets);
				}

				float min1 = minimums[0];
				float max2 = maximums[1];

				if ((min1 < T1) && (max2 > T2))
					handleButtonPressed();
			}

			/** turned public to fake this event **/
			public void handleButtonPressed()
			{
				mSensorData.Clear();

				lock (this)
				{
					if (mListener != null)
					{
						mHandler.Post(() =>
						{
							mListener.OnCardboardTrigger();
						});
					}
				}
			}
		

			private float[] computeOffsets(int start, float[] baseline)
			{
				for (int i = 0; i < SEGMENT_SIZE; i++)
				{
					float[] point = (float[])mSensorData[start + i];
					float[] o = { point[0] - baseline[0], point[1] - baseline[1], point[2] - baseline[2] };
					float magnitude = (float)Math.Sqrt(o[0] * o[0] + o[1] * o[1] + o[2] * o[2]);
					mOffsets[i] = magnitude;
				}
				return mOffsets;
			}

			private float computeMean(float[] offsets)
			{
				float sum = 0.0F;
				foreach (var o in offsets)
				{
					sum += o;
				}
				return sum / offsets.Length;
			}

			private float computeMaximum(float[] offsets)
			{
				float max = (1.0F / -1.0F);
				foreach (var o in offsets)
				{
					max = Math.Max(o, max);
				}
				return max;
			}

			private float computeMinimum(float[] offsets)
			{
				float min = (1.0F / 1.0F);
				foreach (var o in offsets)
				{
					min = Math.Min(o, min);
				}
				return min;
			}

			public void Run()
			{
				Process.SetThreadPriority(ThreadPriority.Lowest);
				Looper.Prepare();
				mSensorManager.RegisterListener(this, mMagnetometer, 0);
				Looper.Loop();
			}

			public void stop()
			{
				mSensorManager.UnregisterListener(this);
			}

			public void OnSensorChanged(SensorEvent e)
			{
				if (e.Sensor.Equals(mMagnetometer))
				{
					float[] values = e.Values.ToArray();

					if ((values[0] == 0.0F) && (values[1] == 0.0F) && (values[2] == 0.0F))
					{
						return;
					}
					addData(values, e.Timestamp);
				}
			}

			public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
			{
			}
		}


		/** Interface for listeners of Cardboard trigger events. */
		public interface OnCardboardTriggerListener
        {
            void OnCardboardTrigger();
        }
    }
}