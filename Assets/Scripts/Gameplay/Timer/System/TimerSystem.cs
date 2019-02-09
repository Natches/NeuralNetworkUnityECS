using System;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;


namespace ECS.Timer {

	[UpdateBefore(typeof(TimerSystem))]
	public class TimerUpdateJobs : JobComponentSystem {

		[BurstCompile]
		public struct UpdateTimeJob : IJobProcessComponentData<TimerData> {

			public float deltatime;

			public void Execute(ref TimerData aTimerData) {
				if (aTimerData.Stopped == 1) {
					aTimerData.Time = 0.0f;
					return;
				}
				aTimerData.Time += deltatime;
				if (aTimerData.Time >= aTimerData.Interval && aTimerData.Interval != -1)
					aTimerData.Stopped = 1;
			}
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps) {
			var job = new UpdateTimeJob() {
				deltatime = Time.deltaTime
			};
			Utils.Utils.ScheduleBatchedJobAndComplete(job.Schedule(this, inputDeps));
			return inputDeps;
		}
	}

	[UpdateAfter(typeof(TimerUpdateJobs))]
	public class TimerSystem : ComponentSystem {

#pragma warning disable 649
		public struct Data {
			[ReadOnly] public ComponentDataArray<TimerData> TimerDatas;
			public GameObjectArray Timers;
		}

		[Inject] private Data data;
#pragma warning restore 649

		protected override void OnUpdate() {
			int length = data.TimerDatas.Length;
			NativeArray<TimerData> timerdatas = new NativeArray<TimerData>(length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			data.TimerDatas.CopyTo(timerdatas);
			GameObject[] timers = data.Timers.ToArray();
			for (int i = 0; i < length; ++i) {
				Timer ut = timers[i].GetComponent<Timer>();
				TimerData utd = timerdatas[i];
				if (utd.Stopped == 1 && utd.Time >= utd.Interval) {
					ut.Invoke();
					if (utd.Loop == 0 && ut != null)
						ut.enabled = false;
				}
			}
			timerdatas.Dispose();
		}
	}
}

