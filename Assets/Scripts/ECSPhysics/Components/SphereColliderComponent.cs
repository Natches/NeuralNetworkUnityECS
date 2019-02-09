using System;
using Unity.Entities;
using Unity.Mathematics;


namespace ECS {
	namespace Physics {

		[Serializable]
		public struct SphereColliderComponentData : IComponentData {
			internal float3 Center;
			internal float Radius;
			internal byte IsTrigger;
		}
	}
}