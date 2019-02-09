using System;
using Unity.Entities;
using Unity.Mathematics;


namespace ECS {
	namespace Physics {

		[Serializable]
		public struct CapsuleColliderComponentData : IComponentData {
			internal float3 Center;
			internal float Height;
			internal float Radius;
			internal int Direction;
			internal byte IsTrigger;
		}

	}
}