using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using UnityEngine.Jobs;
using Unity.Transforms;
using Unity.Mathematics;
using System.Collections.Generic;
using ECS.NeuralNetwork;
using ECS.Movement;

namespace ECS {
	namespace Physics {

		//CollisionSystem which work only with moving object
		[UpdateAfter(typeof(UpdatePositionFromVelocitySystem)), UpdateAfter(typeof(PreCollisionSystem))]
		public class CollisionSystem : JobComponentSystem {

			struct UpdateBoxDistDir : IJobParallelFor {
				[ReadOnly] public NativeArray<Entity> Entities;
				[NativeDisableParallelForRestriction] public ExclusiveEntityTransaction entityTransaction;

				[NativeDisableParallelForRestriction] public NativeArray<BoxcastCommand> Boxcasts;


				[BurstDiscard]
				public void Execute(int index) {
					var entity = Entities[index];
					var position = entityTransaction.GetComponentData<Position>(entity);
					var rotation = entityTransaction.GetComponentData<Rotation>(entity);
					var box = entityTransaction.GetComponentData<BoxColliderComponentData>(entity);
					var collisionLayer = entityTransaction.GetSharedComponentData<CollisionLayerComponentData>(entity);

					BoxcastCommand boxcast = Boxcasts[index * 3];
					var dir = ((position.Value + box.Center) - (float3)boxcast.center);
					boxcast.orientation = rotation.Value;
					boxcast.direction = math.normalizesafe(dir);
					boxcast.distance = math.length(dir);
					boxcast.layerMask = collisionLayer.Mask1;
					Boxcasts[index * 3] = boxcast;
					boxcast.layerMask = collisionLayer.Mask2;
					Boxcasts[index * 3 + 1] = boxcast;
					boxcast.layerMask = collisionLayer.Mask3;
					Boxcasts[index * 3 + 2] = boxcast;
				}
			}

			struct UpdateSphereDistDir : IJobParallelFor {

				[ReadOnly] public int StartIndex;
				[ReadOnly] public NativeArray<Entity> Entities;
				[NativeDisableParallelForRestriction] public ExclusiveEntityTransaction entityTransaction;

				[NativeDisableParallelForRestriction] public NativeArray<SpherecastCommand> Spherecasts;


				[BurstDiscard]
				public void Execute(int index) {
					var entity = Entities[StartIndex + index];
					var position = entityTransaction.GetComponentData<Position>(entity);
					var rotation = entityTransaction.GetComponentData<Rotation>(entity);
					var sphere = entityTransaction.GetComponentData<SphereColliderComponentData>(entity);
					var collisionLayer = entityTransaction.GetSharedComponentData<CollisionLayerComponentData>(entity);

					SpherecastCommand spherecast = Spherecasts[StartIndex + index * 3];
					var dir = ((position.Value + sphere.Center) - (float3)spherecast.origin);
					spherecast.direction = math.normalizesafe(dir);
					spherecast.distance = math.length(dir);
					spherecast.layerMask = collisionLayer.Mask1;
					Spherecasts[StartIndex + index * 3] = spherecast;
					spherecast.layerMask = collisionLayer.Mask2;
					Spherecasts[StartIndex + index * 3 + 1] = spherecast;
					spherecast.layerMask = collisionLayer.Mask3;
					Spherecasts[StartIndex + index * 3 + 2] = spherecast;
				}
			}

			struct UpdateCapsuleDistDir : IJobParallelFor {

				[ReadOnly] public int StartIndex;
				[ReadOnly] public NativeArray<Entity> Entities;
				[NativeDisableParallelForRestriction] public ExclusiveEntityTransaction entityTransaction;

				[NativeDisableParallelForRestriction] public NativeArray<CapsulecastCommand> Capsulecasts;


				[BurstDiscard]
				public void Execute(int index) {
					var entity = Entities[StartIndex + index];
					var position = entityTransaction.GetComponentData<Position>(entity);
					var rotation = entityTransaction.GetComponentData<Rotation>(entity);
					var scale = entityTransaction.GetComponentData<Scale>(entity);
					var capsule = entityTransaction.GetComponentData<CapsuleColliderComponentData>(entity);
					var collisionLayer = entityTransaction.GetSharedComponentData<CollisionLayerComponentData>(entity);

					var capsulecast = Capsulecasts[StartIndex + index * 3];

					float3 dir = new float3();
					float radius = 0.0f;

					switch (capsule.Direction) {
						case 0:
							dir.x = 1;
							radius = capsule.Radius * math.max(scale.Value[1], scale.Value[2]);
							break;
						case 1:
							dir.y = 1;
							radius = capsule.Radius * math.max(scale.Value[0], scale.Value[2]);
							break;
						case 2:
							dir.z = 1;
							radius = capsule.Radius * math.max(scale.Value[0], scale.Value[1]);
							break;
					}

					float height = capsule.Height * scale.Value[capsule.Direction];
					dir = math.rotate(rotation.Value, dir);

					float3 offset = (dir * (height * 0.5f) - (dir * radius));

					dir = ((position.Value + capsule.Center + offset) - (float3)capsulecast.point1);
					capsulecast.direction = math.normalizesafe(dir);
					capsulecast.distance = math.length(dir);
					capsulecast.layerMask = collisionLayer.Mask1;
					Capsulecasts[StartIndex + index * 3] = capsulecast;
					capsulecast.layerMask = collisionLayer.Mask2;
					Capsulecasts[StartIndex + index * 3 + 1] = capsulecast;
					capsulecast.layerMask = collisionLayer.Mask3;
					Capsulecasts[StartIndex + index * 3 + 2] = capsulecast;
				}
			}


			ComponentGroup BoxCollisionGroup;
			ComponentGroup SphereCollisionGroup;
			ComponentGroup CapsuleCollisionGroup;

			protected override void OnCreateManager() {
				BoxCollisionGroup = GetComponentGroup(ComponentType.ReadOnly<Position>(),
						ComponentType.ReadOnly<Rotation>(),
						ComponentType.ReadOnly<BoxColliderComponentData>(),
						ComponentType.Create<CollisionStateComponent>(),
						ComponentType.Create<CollisionResponseComponent>(),
						ComponentType.Subtractive<DeadComponentData>(),
						ComponentType.Subtractive<ResetComponentData>(),
						ComponentType.Subtractive<FrozenComponentData>());
				SphereCollisionGroup = GetComponentGroup(ComponentType.ReadOnly<Position>(),
						ComponentType.ReadOnly<SphereColliderComponentData>(),
						ComponentType.Create<CollisionStateComponent>(),
						ComponentType.Create<CollisionResponseComponent>(),
						ComponentType.Subtractive<DeadComponentData>(),
						ComponentType.Subtractive<ResetComponentData>(),
						ComponentType.Subtractive<FrozenComponentData>());
				CapsuleCollisionGroup = GetComponentGroup(ComponentType.ReadOnly<Position>(),
						ComponentType.ReadOnly<Rotation>(),
						ComponentType.ReadOnly<Scale>(),
						ComponentType.ReadOnly<CapsuleColliderComponentData>(),
						ComponentType.Create<CollisionStateComponent>(),
						ComponentType.Create<CollisionResponseComponent>(),
						ComponentType.Subtractive<DeadComponentData>(),
						ComponentType.Subtractive<ResetComponentData>(),
						ComponentType.Subtractive<FrozenComponentData>());
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps) {
				PreCollisionSystem preCollisionSystem = World.Active.GetExistingManager<PreCollisionSystem>();
				if (preCollisionSystem != null) {
					var entities = preCollisionSystem.Entities;
					var boxcasts = preCollisionSystem.Boxcasts;
					var spherecasts = preCollisionSystem.Spherecasts;
					var capsulecasts = preCollisionSystem.Capsulecasts;

					EntityManager.CompleteAllJobs();

					if (boxcasts.IsCreated && boxcasts.Length != 0) {

						var hitBoxCollider = new NativeArray<RaycastHit>(boxcasts.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

						var transaction = EntityManager.BeginExclusiveEntityTransaction();

						var job = new UpdateBoxDistDir {
							Boxcasts = boxcasts,
							Entities = entities,
							entityTransaction = transaction
						};


						var handle = job.Schedule(boxcasts.Length / 3, 32, inputDeps);

						EntityManager.ExclusiveEntityTransactionDependency = handle;

						Utils.Utils.ScheduleBatchedJobAndComplete(handle);

						EntityManager.EndExclusiveEntityTransaction();

						handle = BoxcastCommand.ScheduleBatch(boxcasts, hitBoxCollider, 1, default);

						Utils.Utils.ScheduleBatchedJobAndComplete(handle);

						InvokeCollisionResponse(entities, hitBoxCollider, 0, boxcasts.Length);

						hitBoxCollider.Dispose();
					}

					if (spherecasts.IsCreated && spherecasts.Length != 0) {

						var hitSphereCollider = new NativeArray<RaycastHit>(spherecasts.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

						var transaction = EntityManager.BeginExclusiveEntityTransaction();

						var job = new UpdateSphereDistDir {
							Spherecasts = spherecasts,
							Entities = entities,
							entityTransaction = transaction,
							StartIndex = boxcasts.Length / 3
						};

						var handle = job.Schedule(spherecasts.Length / 3, 32, inputDeps);

						EntityManager.ExclusiveEntityTransactionDependency = handle;

						Utils.Utils.ScheduleBatchedJobAndComplete(handle);

						EntityManager.EndExclusiveEntityTransaction();

						handle = SpherecastCommand.ScheduleBatch(spherecasts, hitSphereCollider, 1, default);

						Utils.Utils.ScheduleBatchedJobAndComplete(handle);

						InvokeCollisionResponse(entities, hitSphereCollider, boxcasts.Length, spherecasts.Length);

						hitSphereCollider.Dispose();
					}

					if (capsulecasts.IsCreated && capsulecasts.Length != 0) {
						var hitCapsuleCollider = new NativeArray<RaycastHit>(capsulecasts.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

						var transaction = EntityManager.BeginExclusiveEntityTransaction();

						var job = new UpdateCapsuleDistDir {
							Capsulecasts = capsulecasts,
							Entities = entities,
							entityTransaction = transaction,
							StartIndex = (boxcasts.Length + spherecasts.Length) / 3
						};

						var handle = job.Schedule(capsulecasts.Length / 3, 32, inputDeps);

						EntityManager.ExclusiveEntityTransactionDependency = handle;

						Utils.Utils.ScheduleBatchedJobAndComplete(handle);

						EntityManager.EndExclusiveEntityTransaction();

						handle = CapsulecastCommand.ScheduleBatch(capsulecasts, hitCapsuleCollider, 1, default);

						Utils.Utils.ScheduleBatchedJobAndComplete(handle);

						InvokeCollisionResponse(entities, hitCapsuleCollider, spherecasts.Length, capsulecasts.Length);

						hitCapsuleCollider.Dispose();
					}
				}
				return inputDeps;
			}

			[BurstDiscard]
			void InvokeCollisionResponse([ReadOnly] NativeArray<Entity> Entities,
				[ReadOnly] NativeArray<RaycastHit> RaycastHits,
				int startIndex, int count) {
				var precollisionSystem = World.GetExistingManager<PreCollisionSystem>();

				HashSet<Collider> presentCollider = new HashSet<Collider>();

				count += startIndex;
				for (int i = startIndex, j = startIndex; i < count; ++j) {
					var entity = Entities[j];
					var collisionState = EntityManager.GetComponentObject<CollisionStateComponent>(entity);
					var collisionResponse = EntityManager.GetComponentObject<CollisionResponseComponent>(entity);
					for (int k = 0; k < 3; ++k, ++i) {
						RaycastHit hit = RaycastHits[i];
						if (hit.collider != null) {
							presentCollider.Add(hit.collider);
							if (collisionState.presentColliders.Contains(hit.collider))
								collisionResponse.InvokeOnCollisionStay(hit);
							else {
								collisionResponse.InvokeOnCollisionEnter(hit);
								collisionState.colliders.Add(hit.collider);
								collisionState.presentColliders.Add(hit.collider);
							}
						}
					}
					foreach (Collider col in collisionState.colliders.ToArray()) {
						if (!presentCollider.Contains(col)) {
							collisionResponse.InvokeOnCollisionExit(col);
							collisionState.colliders.Remove(col);
							collisionState.presentColliders.Remove(col);
						}
					}
					presentCollider.Clear();
				}
				precollisionSystem.HasChanged = true;
			}
		}
	}
}