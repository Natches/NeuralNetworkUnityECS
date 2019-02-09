using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine.Jobs;
using ECS.NeuralNetwork;

namespace ECS {
	namespace Movement {

		[UpdateAfter(typeof(MovementSystem))]
		public class UpdatePositionFromVelocitySystem : JobComponentSystem {

			[BurstCompile]
			struct UpdateMovementTargetPos : IJobParallelFor {

				[ReadOnly] public ComponentDataArray<MovementData> Movements;

				[NativeDisableParallelForRestriction] public ComponentDataArray<Position> Positions;

				[ReadOnly] public float deltaTime;

				public void Execute(int index) {
					var position = Positions[index];
					position.Value += Movements[index].Velocity * deltaTime;
					Positions[index] = position;
				}
			}

			ComponentGroup movementGroup;

			protected override void OnCreateManager() {
				movementGroup = GetComponentGroup(ComponentType.ReadOnly<MovementData>(),
					ComponentType.Create<Position>(),
					ComponentType.Subtractive<DeadComponentData>(),
					ComponentType.Subtractive<ResetComponentData>(),
					ComponentType.Subtractive<FrozenComponentData>());
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps) {
				var job = new UpdateMovementTargetPos {
					Movements = movementGroup.GetComponentDataArray<MovementData>(),
					Positions = movementGroup.GetComponentDataArray<Position>(),
					deltaTime = Time.deltaTime
				};
				Utils.Utils.ScheduleBatchedJobAndComplete(job.Schedule(movementGroup.GetComponentDataArray<Position>().Length, 32, inputDeps));
				return inputDeps;
			}
		}
	}
}