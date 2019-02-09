using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using UnityEngine.Jobs;
using Unity.Transforms;
using Unity.Mathematics;
using ECS.NeuralNetwork;
using ECS.Movement;

namespace ECS {
	namespace Physics {

		//PrecollisionSystem which prepare the data for the CollisionSystem
		[UpdateBefore(typeof(UpdatePositionFromVelocitySystem)), UpdateBefore(typeof(CollisionSystem))]
		public class PreCollisionSystem : JobComponentSystem {

			ComponentGroup BoxPreCollisionGroup;
			ComponentGroup SpherePreCollisionGroup;
			ComponentGroup CapsulePreCollisionGroup;
			ComponentGroup PreColliderGroup;

			protected override void OnCreateManager() {
				BoxPreCollisionGroup = GetComponentGroup(new EntityArchetypeQuery() {
					All = new ComponentType[] {
				ComponentType.ReadOnly<Position>(),
				ComponentType.ReadOnly<Rotation>(),
				ComponentType.ReadOnly<Scale>(),
				ComponentType.ReadOnly<BoxColliderComponentData>()
			},
					None = new ComponentType[] {
				ComponentType.Create<DeadComponentData>(),
				ComponentType.Create<ResetComponentData>(),
				ComponentType.Create<FrozenComponentData>()
			}
				});
				SpherePreCollisionGroup = GetComponentGroup(new EntityArchetypeQuery() {
					All = new ComponentType[] {
				ComponentType.ReadOnly<Position>(),
				ComponentType.ReadOnly<Scale>(),
				ComponentType.ReadOnly<SphereColliderComponentData>()
			},
					None = new ComponentType[] {
				ComponentType.Create<DeadComponentData>(),
				ComponentType.Create<ResetComponentData>(),
				ComponentType.Create<FrozenComponentData>()
			}
				});
				CapsulePreCollisionGroup = GetComponentGroup(new EntityArchetypeQuery() {
					All = new ComponentType[] {
				ComponentType.ReadOnly<Position>(),
				ComponentType.ReadOnly<Rotation>(),
				ComponentType.ReadOnly<Scale>(),
				ComponentType.ReadOnly<CapsuleColliderComponentData>()
			},
					None = new ComponentType[] {
				ComponentType.Create<DeadComponentData>(),
				ComponentType.Create<ResetComponentData>(),
				ComponentType.Create<FrozenComponentData>()
			}
				});
				PreColliderGroup = EntityManager.CreateComponentGroup(ComponentType.ReadOnly<ColliderComponentData>(),
					ComponentType.Subtractive<DeadComponentData>(),
					ComponentType.Subtractive<ResetComponentData>(),
					ComponentType.Subtractive<FrozenComponentData>());

				EntityBoxCastMap = new NativeHashMap<Entity, BoxcastCommand>(0, Allocator.Persistent);
				EntitySphereCastMap = new NativeHashMap<Entity, SpherecastCommand>(0, Allocator.Persistent);
				EntityCapsuleCastMap = new NativeHashMap<Entity, CapsulecastCommand>(0, Allocator.Persistent);
				Boxcasts = new NativeArray<BoxcastCommand>(0, Allocator.Persistent);
				Spherecasts = new NativeArray<SpherecastCommand>(0, Allocator.Persistent);
				Capsulecasts = new NativeArray<CapsulecastCommand>(0, Allocator.Persistent);
			}

			[BurstCompile, RequireComponentTag(typeof(ColliderComponentData))]
			struct BoxPreCollisionJob : IJobChunk {

				[ReadOnly] public ArchetypeChunkComponentType<Position> PositionType;
				[ReadOnly] public ArchetypeChunkComponentType<Rotation> RotationType;
				[ReadOnly] public ArchetypeChunkComponentType<Scale> ScaleType;
				[ReadOnly] public ArchetypeChunkComponentType<BoxColliderComponentData> BoxColliderType;
				[ReadOnly] public ArchetypeChunkEntityType EntityType;

				[WriteOnly] public NativeHashMap<Entity, BoxcastCommand>.Concurrent BoxCasts;

				public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex) {
					NativeArray<BoxColliderComponentData> boxColliders = chunk.GetNativeArray(BoxColliderType);
					NativeArray<Rotation> rotations = chunk.GetNativeArray(RotationType);
					NativeArray<Position> positions = chunk.GetNativeArray(PositionType);
					NativeArray<Scale> scales = chunk.GetNativeArray(ScaleType);

					NativeArray<Entity> entities = chunk.GetNativeArray(EntityType);

					float3 zero = float3.zero;
					for (int i = 0, length = boxColliders.Length; i < length; ++i) {
						BoxColliderComponentData box = boxColliders[i];
						Position position = positions[i];
						Rotation rotation = rotations[i];
						Scale scale = scales[i];

						BoxcastCommand boxCast = new BoxcastCommand(position.Value + box.Center,
							box.Size * scale.Value * 0.5f,
							quaternion.identity,
							float3.zero, 0,
							0);
						BoxCasts.TryAdd(entities[i], boxCast);
					}
				}
			}

			[BurstCompile, RequireComponentTag(typeof(ColliderComponentData))]
			struct SpherePreCollisionJob : IJobChunk {

				[ReadOnly] public ArchetypeChunkComponentType<Position> PositionType;
				[ReadOnly] public ArchetypeChunkComponentType<Scale> ScaleType;
				[ReadOnly] public ArchetypeChunkComponentType<SphereColliderComponentData> SphereColliderType;
				[ReadOnly] public ArchetypeChunkEntityType EntityType;

				[WriteOnly] public NativeHashMap<Entity, SpherecastCommand>.Concurrent SphereCasts;

				public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex) {
					NativeArray<SphereColliderComponentData> sphereColliders = chunk.GetNativeArray(SphereColliderType);
					NativeArray<Position> positions = chunk.GetNativeArray(PositionType);
					NativeArray<Scale> scales = chunk.GetNativeArray(ScaleType);

					NativeArray<Entity> entities = chunk.GetNativeArray(EntityType);

					float3 zero = float3.zero;
					for (int i = 0, length = sphereColliders.Length; i < length; ++i) {
						SphereColliderComponentData sphere = sphereColliders[i];
						Position position = positions[i];
						Scale scale = scales[i];

						SpherecastCommand sphereCast = new SpherecastCommand(position.Value + sphere.Center,
							sphere.Radius * math.max(scale.Value.z, math.max(scale.Value.y, scale.Value.x)),
							zero, 0,
							0);
						SphereCasts.TryAdd(entities[i], sphereCast);
					}
				}
			}

			[BurstCompile, RequireComponentTag(typeof(ColliderComponentData))]
			struct CapsulePreCollisionJob : IJobChunk {

				[ReadOnly] public ArchetypeChunkComponentType<Position> PositionType;
				[ReadOnly] public ArchetypeChunkComponentType<Rotation> RotationType;
				[ReadOnly] public ArchetypeChunkComponentType<Scale> ScaleType;
				[ReadOnly] public ArchetypeChunkComponentType<CapsuleColliderComponentData> CapsuleColliderType;
				[ReadOnly] public ArchetypeChunkEntityType EntityType;

				[WriteOnly] public NativeHashMap<Entity, CapsulecastCommand>.Concurrent CapsuleCasts;

				public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex) {
					NativeArray<CapsuleColliderComponentData> capsuleColliders = chunk.GetNativeArray(CapsuleColliderType);
					NativeArray<Position> positions = chunk.GetNativeArray(PositionType);
					NativeArray<Rotation> rotations = chunk.GetNativeArray(RotationType);
					NativeArray<Scale> scales = chunk.GetNativeArray(ScaleType);

					NativeArray<Entity> entities = chunk.GetNativeArray(EntityType);

					float3 zero = float3.zero;
					for (int i = 0, length = capsuleColliders.Length; i < length; ++i) {
						CapsuleColliderComponentData capsule = capsuleColliders[i];
						Position position = positions[i];
						Rotation rotation = rotations[i];
						Scale scale = scales[i];

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

						float3 point1 = position.Value + capsule.Center + offset;
						float3 point2 = position.Value + capsule.Center - offset;

						CapsulecastCommand capsuleCast = new CapsulecastCommand(point1, point2,
							radius, zero, 0, 0);
						CapsuleCasts.TryAdd(entities[i], capsuleCast);
					}
				}
			}

			[BurstCompile]
			struct FinalizePreCollision : IJobParallelFor {

				[ReadOnly] public int BoxCount;
				[ReadOnly] public int SphereCount;
				[ReadOnly] public int CapsuleCount;

				[ReadOnly] public NativeArray<Entity> BoxEntities;
				[ReadOnly] public NativeArray<Entity> SphereEntities;
				[ReadOnly] public NativeArray<Entity> CapsuleEntities;

				[ReadOnly] public NativeHashMap<Entity, BoxcastCommand> EntityBoxCastMap;
				[ReadOnly] public NativeHashMap<Entity, SpherecastCommand> EntitySphereCastMap;
				[ReadOnly] public NativeHashMap<Entity, CapsulecastCommand> EntityCapsuleCastMap;

				[WriteOnly, NativeDisableParallelForRestriction] public NativeArray<Entity> Entities;
				[WriteOnly, NativeDisableParallelForRestriction] public NativeArray<BoxcastCommand> BoxCasts;
				[WriteOnly, NativeDisableParallelForRestriction] public NativeArray<SpherecastCommand> SphereCasts;
				[WriteOnly, NativeDisableParallelForRestriction] public NativeArray<CapsulecastCommand> CapsuleCasts;

				public void Execute(int index) {
					Entity entity;
					if (BoxCount != 0 && index < BoxCount) {
						entity = BoxEntities[index];
						BoxCasts[index * 3] = EntityBoxCastMap[entity];
					}
					else if (SphereCount != 0 && index < BoxCount + SphereCount) {
						entity = SphereEntities[index - BoxCount];
						SphereCasts[(index - BoxCount) * 3] = EntitySphereCastMap[entity];
					}
					else {
						entity = CapsuleEntities[index - (BoxCount + SphereCount)];
						CapsuleCasts[(index - (BoxCount + SphereCount)) * 3] = EntityCapsuleCastMap[entity];
					}
					Entities[index] = entity;
				}
			}

			public NativeArray<Entity> Entities;
			public NativeArray<BoxcastCommand> Boxcasts;
			public NativeArray<SpherecastCommand> Spherecasts;
			public NativeArray<CapsulecastCommand> Capsulecasts;

			private NativeHashMap<Entity, BoxcastCommand> EntityBoxCastMap;
			private NativeHashMap<Entity, SpherecastCommand> EntitySphereCastMap;
			private NativeHashMap<Entity, CapsulecastCommand> EntityCapsuleCastMap;

			internal bool HasChanged = true;

			protected override JobHandle OnUpdate(JobHandle inputDeps) {
				if (HasChanged) {
					int sphereCount = 0, capsuleCount = 0, boxCount = 0;
					var colliders = PreColliderGroup.GetComponentDataArray<ColliderComponentData>();
					for (int i = 0, length = colliders.Length; i < length; ++i) {
						ColliderComponentData colliderComponent = colliders[i];
						if (Utils.Utils.HasBoxCollider(colliderComponent))
							++boxCount;
						if (Utils.Utils.HasSphereCollider(colliderComponent))
							++sphereCount;
						if (Utils.Utils.HasCapsuleCollider(colliderComponent))
							++capsuleCount;
					}

					var entityCount = PreColliderGroup.GetEntityArray().Length;
					Utils.Utils.ExpandOrCreateArray(ref Entities, entityCount, Allocator.Persistent);

					JobHandle boxHandle = new JobHandle(), sphereHandle = new JobHandle(), capsuleHandle = new JobHandle();

					if (boxCount != 0) {
						Utils.Utils.ExpandOrCreateArray(ref Boxcasts, boxCount * 3, Allocator.Persistent);
						Utils.Utils.ExpandOrCreateHashMap(ref EntityBoxCastMap, boxCount, Allocator.Persistent);

						BoxPreCollisionJob boxPreCollision = new BoxPreCollisionJob {
							BoxColliderType = GetArchetypeChunkComponentType<BoxColliderComponentData>(true),
							PositionType = GetArchetypeChunkComponentType<Position>(true),
							RotationType = GetArchetypeChunkComponentType<Rotation>(true),
							ScaleType = GetArchetypeChunkComponentType<Scale>(true),
							BoxCasts = EntityBoxCastMap.ToConcurrent(),
							EntityType = GetArchetypeChunkEntityType()
						};

						boxHandle = boxPreCollision.Schedule(BoxPreCollisionGroup, inputDeps);
					}

					if (sphereCount != 0) {
						Utils.Utils.ExpandOrCreateArray(ref Spherecasts, sphereCount * 3, Allocator.Persistent);
						Utils.Utils.ExpandOrCreateHashMap(ref EntitySphereCastMap, sphereCount, Allocator.Persistent);

						SpherePreCollisionJob spherePreCollision = new SpherePreCollisionJob {
							SphereColliderType = GetArchetypeChunkComponentType<SphereColliderComponentData>(true),
							PositionType = GetArchetypeChunkComponentType<Position>(true),
							ScaleType = GetArchetypeChunkComponentType<Scale>(true),
							SphereCasts = EntitySphereCastMap.ToConcurrent(),
							EntityType = GetArchetypeChunkEntityType()
						};

						sphereHandle = spherePreCollision.Schedule(SpherePreCollisionGroup, inputDeps);
					}

					if (capsuleCount != 0) {
						Utils.Utils.ExpandOrCreateArray(ref Capsulecasts, capsuleCount * 3, Allocator.Persistent);
						Utils.Utils.ExpandOrCreateHashMap(ref EntityCapsuleCastMap, capsuleCount, Allocator.Persistent);

						CapsulePreCollisionJob capsulePreCollision = new CapsulePreCollisionJob {
							CapsuleColliderType = GetArchetypeChunkComponentType<CapsuleColliderComponentData>(true),
							PositionType = GetArchetypeChunkComponentType<Position>(true),
							RotationType = GetArchetypeChunkComponentType<Rotation>(true),
							ScaleType = GetArchetypeChunkComponentType<Scale>(true),
							CapsuleCasts = EntityCapsuleCastMap.ToConcurrent(),
							EntityType = GetArchetypeChunkEntityType()
						};

						capsuleHandle = capsulePreCollision.Schedule(CapsulePreCollisionGroup, inputDeps);
					}

					JobHandle.ScheduleBatchedJobs();

					NativeArray<Entity> boxEntities = new NativeArray<Entity>(0, Allocator.Persistent),
						sphereEntities = new NativeArray<Entity>(0, Allocator.Persistent),
						capsuleEntities = new NativeArray<Entity>(0, Allocator.Persistent);

					if (boxCount != 0) {
						boxHandle.Complete();
						boxEntities.Dispose();
						boxEntities = EntityBoxCastMap.GetKeyArray(Allocator.TempJob);
					}
					if (sphereCount != 0) {
						sphereHandle.Complete();
						sphereEntities.Dispose();
						sphereEntities = EntitySphereCastMap.GetKeyArray(Allocator.TempJob);
					}
					if (capsuleCount != 0) {
						capsuleHandle.Complete();
						capsuleEntities.Dispose();
						capsuleEntities = EntityCapsuleCastMap.GetKeyArray(Allocator.TempJob);
					}

					var job = new FinalizePreCollision {
						BoxCount = boxCount,
						SphereCount = sphereCount,
						CapsuleCount = capsuleCount,
						EntityBoxCastMap = EntityBoxCastMap,
						EntitySphereCastMap = EntitySphereCastMap,
						EntityCapsuleCastMap = EntityCapsuleCastMap,
						BoxEntities = boxEntities,
						SphereEntities = sphereEntities,
						CapsuleEntities = capsuleEntities,
						Entities = Entities,
						BoxCasts = Boxcasts,
						CapsuleCasts = Capsulecasts,
						SphereCasts = Spherecasts,
					};

					var handle = job.Schedule(entityCount, 32, inputDeps);

					Utils.Utils.ScheduleBatchedJobAndComplete(handle);

					boxEntities.Dispose();
					sphereEntities.Dispose();
					capsuleEntities.Dispose();
					HasChanged = false;
				}
				return inputDeps;
			}

			protected override void OnDestroyManager() {
				if (EntityBoxCastMap.IsCreated)
					EntityBoxCastMap.Dispose();
				if (EntitySphereCastMap.IsCreated)
					EntitySphereCastMap.Dispose();
				if (EntityCapsuleCastMap.IsCreated)
					EntityCapsuleCastMap.Dispose();
				if (Boxcasts.IsCreated)
					Boxcasts.Dispose();
				if (Spherecasts.IsCreated)
					Spherecasts.Dispose();
				if (Capsulecasts.IsCreated)
					Capsulecasts.Dispose();
				if (Entities.IsCreated)
					Entities.Dispose();
			}
		}
	}
}