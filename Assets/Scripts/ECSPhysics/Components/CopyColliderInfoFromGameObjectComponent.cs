using System;
using UnityEngine;
using Unity.Entities;


namespace ECS {
	namespace Physics {

		[Serializable]
		public struct CopyColliderInfoFromGameObjectComponentData : IComponentData { }

		[DisallowMultipleComponent]
		public class CopyColliderInfoFromGameObjectComponent :
			ComponentDataWrapper<CopyColliderInfoFromGameObjectComponentData> {
			protected override void OnEnable() {
				if (gameObject.GetComponent<DirtyColliderComponent>() == null)
					gameObject.AddComponent<DirtyColliderComponent>();
			}
		}
	}
}

