using System;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;


namespace ECS {
	namespace Physics {

		[Serializable]
		public struct CollisionResponseComponentData : IComponentData { }

		[DisallowMultipleComponent]
		public abstract class CollisionResponseComponent : ComponentDataWrapper<CollisionResponseComponentData> {

			protected delegate void OnCollisionEnter(RaycastHit hit);
			protected event OnCollisionEnter onCollisionEnter;

			public void InvokeOnCollisionEnter(RaycastHit hit) {
				if (onCollisionEnter.GetInvocationList().Length != 0)
					onCollisionEnter.Invoke(hit);
			}

			protected delegate void OnCollisionStay(RaycastHit hit);
			protected event OnCollisionStay onCollisionStay;

			public void InvokeOnCollisionStay(RaycastHit hit) {
				if (onCollisionStay.GetInvocationList().Length != 0)
					onCollisionStay.Invoke(hit);
			}

			protected delegate void OnCollisionExit(Collider hit);
			protected event OnCollisionExit onCollisionExit;

			public void InvokeOnCollisionExit(Collider other) {
				if (onCollisionExit.GetInvocationList().Length != 0)
					onCollisionExit.Invoke(other);
			}

			protected abstract override void OnEnable();
			protected abstract override void OnDisable();

			protected abstract void CollisionEnter(RaycastHit hit);
			protected abstract void CollisionStay(RaycastHit hit);
			protected abstract void CollisionExit(Collider other);
		}
	}
}