using System;
using Unity.Entities;
using UnityEngine;


namespace ECS {
	namespace Movement {

		[Serializable]
		public struct SteeringSharedData : ISharedComponentData {
			public float MaxAcceleration;
			internal float MaxAccelerationSqr;
		}

		public class SteeringShared : SharedComponentDataWrapper<SteeringSharedData> {
			protected override void ValidateSerializedData(ref SteeringSharedData serializedData) {
				serializedData.MaxAccelerationSqr = serializedData.MaxAcceleration * serializedData.MaxAcceleration;
			}
		}
	}
}
