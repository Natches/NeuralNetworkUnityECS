using System;
using UnityEngine;
using Unity.Entities;


namespace ECS {
	namespace Physics {

		[Serializable]
		public struct DirtyColliderComponentData : IComponentData { }

		[DisallowMultipleComponent]
		public class DirtyColliderComponent :
			ComponentDataWrapper<DirtyColliderComponentData> { }
	}
}
