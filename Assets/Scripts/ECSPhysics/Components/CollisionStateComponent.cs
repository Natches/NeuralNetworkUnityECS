using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;


namespace ECS {
	namespace Physics {

		[Serializable]
		public struct CollisionStateComponentData : IComponentData { }

		[DisallowMultipleComponent]
		public class CollisionStateComponent : ComponentDataWrapper<CollisionStateComponentData> {


			internal List<Collider> colliders;
			internal HashSet<Collider> presentColliders;

			public CollisionStateComponent() {
				colliders = new List<Collider>();
				presentColliders = new HashSet<Collider>();
			}
		}
	}
}