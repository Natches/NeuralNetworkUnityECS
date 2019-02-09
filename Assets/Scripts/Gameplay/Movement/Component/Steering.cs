using System;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;


namespace ECS {
	namespace Movement {

		[Serializable]
		public struct SteeringData : IComponentData {
			internal float3 Velocity;
		}

		[DisallowMultipleComponent]
		public class Steering : ComponentDataWrapper<SteeringData> { }
	}
}