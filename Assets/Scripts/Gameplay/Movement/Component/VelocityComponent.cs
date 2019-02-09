using System;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;


namespace ECS {
	namespace Movement {

		[Serializable]
		public struct VelocityComponentData : IComponentData {
			internal float3 Velocity;
		}

		[DisallowMultipleComponent]
		public class VelocityComponent : ComponentDataWrapper<VelocityComponentData> { }

	}
}
