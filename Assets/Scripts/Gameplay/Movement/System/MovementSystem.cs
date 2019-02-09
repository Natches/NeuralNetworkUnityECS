using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine.Jobs;
using ECS.NeuralNetwork;


namespace ECS {
	namespace Movement {
		[UpdateAfter(typeof(SteeringSystem)), UpdateBefore(typeof(CopyTransformToGameObjectSystem))]
		public class MovementSystem : JobComponentSystem {

			[BurstCompile]
			public struct MovementJob : IJobParallelFor {

				[ReadOnly] public ComponentDataArray<Position> Positions;
				[ReadOnly] public ComponentDataArray<SteeringData> Steerings;
				[ReadOnly] public MovementSharedData MovementShared;
				public ComponentDataArray<MovementData> Movements;
				public ComponentDataArray<Rotation> Rotations;

				[BurstCompile]
				public void Execute(int index) {
					SteeringData steering = Steerings[index];
					MovementData movement = Movements[index];
					float3 position = Positions[index].Value;

					float3 steeringVelocity = steering.Velocity;
					float distance = math.distance(movement.TargetPos, position);
					ref float3 velocity = ref movement.Velocity;


					if (distance <= MovementShared.SlowingDistance) {
						if (distance < MovementShared.MinDistToStop || movement.HasArrived != 0) {
							velocity = Vector3.zero;
							steeringVelocity = Vector3.zero;
							movement.HasArrived = 1;
						}
						else {
							float3 brakingVelocity = math.normalizesafe(steeringVelocity) * movement.MaxSpeed * (distance / MovementShared.SlowingDistance);
							steeringVelocity = brakingVelocity - velocity;
						}
					}
					else {
						movement.HasArrived = 0;
					}

					velocity += steeringVelocity;

					if (distance < MovementShared.MinDistToStop) {
						velocity = float3.zero;
					}
					else {
						if (math.lengthsq(velocity) > 0)
							movement.Rotation = math.atan2(velocity.x, velocity.z);
					}

					// truncate to max speed
					if (math.lengthsq(movement.Velocity) > math.pow(movement.MaxSpeed, 2))
						velocity = math.normalizesafe(velocity) * movement.MaxSpeed;

					velocity.y = 0.0f;

					Rotation rot = Rotations[index];
					rot.Value = quaternion.AxisAngle(new float3(0, 1, 0), movement.Rotation);
					Rotations[index] = rot;
					Movements[index] = movement;
				}
			}

			ComponentGroup group;

			protected override JobHandle OnUpdate(JobHandle inputDeps) {
				var job = new MovementJob() {
					Positions = group.GetComponentDataArray<Position>(),
					Rotations = group.GetComponentDataArray<Rotation>(),
					Steerings = group.GetComponentDataArray<SteeringData>(),
					MovementShared = group.GetSharedComponentDataArray<MovementSharedData>()[0],
					Movements = group.GetComponentDataArray<MovementData>()
				};
				return job.Schedule(group.GetComponentDataArray<MovementData>().Length, 16, inputDeps);
			}

			protected override void OnCreateManager() {
				group = GetComponentGroup(ComponentType.ReadOnly<Position>(),
					ComponentType.ReadOnly<SteeringData>(),
					ComponentType.ReadOnly<MovementSharedData>(),
					ComponentType.Create<Rotation>(),
					ComponentType.Create<MovementData>(),
					ComponentType.Subtractive<DeadComponentData>(),
					ComponentType.Subtractive<ResetComponentData>(),
					ComponentType.Subtractive<FrozenComponentData>());
			}
		}
	}
}