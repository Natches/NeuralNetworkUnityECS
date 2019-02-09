using System;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS {
	namespace Physics {

		[Serializable]
		public struct BoxColliderComponentData : IComponentData {
			internal float3 Center;
			internal float3 Size;
			internal byte IsTrigger;
		}
	}
}

