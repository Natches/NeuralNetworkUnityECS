using UnityEngine;
using Unity.Entities;
using ECS.NeuralNetwork;

namespace ECS {
	namespace Movement {
		[UpdateAfter(typeof(MovementSystem))]
		public class CopyVelocityToRigidBodySystem : ComponentSystem {

			ComponentGroup group;

			protected override void OnUpdate() {
				var velocityArray = group.GetComponentDataArray<VelocityComponentData>();
				var rigidBodyArray = group.GetComponentArray<Rigidbody>();
				for (int i = 0; i < velocityArray.Length; ++i) {
					rigidBodyArray[i].velocity = velocityArray[i].Velocity;
				}
			}

			protected override void OnCreateManager() {
				base.OnCreateManager();
				group = GetComponentGroup(typeof(VelocityComponentData),
					typeof(Transform),
					typeof(Rigidbody),
					ComponentType.Subtractive<DeadComponentData>());
			}
		}
	}
}
