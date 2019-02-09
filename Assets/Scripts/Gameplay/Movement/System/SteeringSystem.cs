using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using UnityEngine.Jobs;
using Unity.Transforms;
using ECS.NeuralNetwork;

namespace ECS {
	namespace Movement {

		[UpdateAfter(typeof(CopyTransformFromGameObjectSystem)), UpdateBefore(typeof(MovementSystem))]
		public class SteeringSystem : JobComponentSystem {

			[BurstCompile]
			private struct SteeringJob : IJobParallelFor {

				[ReadOnly] public ComponentDataArray<Position> Positions;
				[ReadOnly] public ComponentDataArray<MovementData> Movements;
				[ReadOnly] public SteeringSharedData SharedSteering;
				public ComponentDataArray<SteeringData> Steerings;

				public void Execute(int index) {
					SteeringData steering = Steerings[index];
					SteeringSharedData steeringShared = SharedSteering;
					MovementData movement = Movements[index];
					float3 position = Positions[index].Value;
					steering.Velocity = movement.TargetPos - position;
					if (math.lengthsq(steering.Velocity) > steeringShared.MaxAccelerationSqr)
						steering.Velocity = math.normalizesafe(steering.Velocity) * steeringShared.MaxAcceleration;
					steering.Velocity.y = 0.0f;
					Steerings[index] = steering;
				}
			}

			ComponentGroup group;

			protected override void OnCreateManager() {
				group = GetComponentGroup(ComponentType.ReadOnly<Position>(),
					ComponentType.ReadOnly<MovementData>(),
					ComponentType.Create<SteeringData>(),
					ComponentType.ReadOnly<SteeringSharedData>(),
					ComponentType.Subtractive<DeadComponentData>(),
					ComponentType.Subtractive<ResetComponentData>(),
					ComponentType.Subtractive<FrozenComponentData>());
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps) {
				var job = new SteeringJob() {
					Positions = group.GetComponentDataArray<Position>(),
					Movements = group.GetComponentDataArray<MovementData>(),
					SharedSteering = group.GetSharedComponentDataArray<SteeringSharedData>()[0],
					Steerings = group.GetComponentDataArray<SteeringData>()
				};
				return job.Schedule(group.GetComponentDataArray<SteeringData>().Length, 16, inputDeps);
			}
		}
	}
}