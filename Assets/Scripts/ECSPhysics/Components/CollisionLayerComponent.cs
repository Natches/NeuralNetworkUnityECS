using System;
using UnityEngine;
using Unity.Entities;


namespace ECS {
	namespace Physics {

		[Serializable]
		public struct CollisionLayerComponentData : ISharedComponentData { //WorkAround until boxcast, spherecast and capsulecast ECS support multihit
			[Tooltip("Put one unique layer")]
			public LayerMask Mask1;
			[Tooltip("Put one unique layer")]
			public LayerMask Mask2;
			[Tooltip("Put one unique layer")]
			public LayerMask Mask3;
		}

		public class CollisionLayerComponent : SharedComponentDataWrapper<CollisionLayerComponentData> {}
	}
}