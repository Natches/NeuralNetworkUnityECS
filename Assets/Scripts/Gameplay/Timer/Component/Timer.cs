using System;
using Unity.Entities;
using UnityEngine;

namespace ECS.Timer {

	[Serializable]
	public struct TimerData : IComponentData {
		internal float Time;
		public float Interval;
		public byte Loop;
		internal byte Stopped;
	}

	[DisallowMultipleComponent]
	public class Timer : ComponentDataWrapper<TimerData> {
		public delegate void OnElapsed();
		public event OnElapsed onElapsed;

		public void Invoke() {
			if (onElapsed.GetInvocationList().Length > 0)
				onElapsed.Invoke();
			else
				Destroy(this);
		}
	}
}
