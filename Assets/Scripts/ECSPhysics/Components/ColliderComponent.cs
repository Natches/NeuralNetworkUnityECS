using System;
using UnityEngine;
using Unity.Entities;


namespace ECS {
	namespace Physics {

		public enum E_CollisionVolume : byte {
			BOX = 1,
			SPHERE = 2,
			CAPSULE = 4
		}

		[Serializable]
		public struct ColliderComponentData : IComponentData {
			internal E_CollisionVolume Volume;
		}

		[DisallowMultipleComponent]
		public class ColliderComponent : ComponentDataWrapper<ColliderComponentData> { }

	}
}