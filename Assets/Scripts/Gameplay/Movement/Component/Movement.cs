using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace ECS {
	namespace Movement {

		[Serializable]
		public struct MovementData : IComponentData {
			public float MaxSpeed;
			internal byte HasArrived;
			internal float3 TargetPos;
			internal float3 Velocity;
			internal float Rotation;
			internal float BoostValue;
		}

		[DisallowMultipleComponent]
		public class Movement : ComponentDataWrapper<MovementData> { }
	}
}