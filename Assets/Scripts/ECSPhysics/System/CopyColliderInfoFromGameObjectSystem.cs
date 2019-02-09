using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

namespace ECS {
	namespace Physics {

		[UpdateBefore(typeof(PreCollisionSystem))]
		public class CopyColliderInfoFromGameObjectSystem : ComponentSystem {

			ComponentGroup boxGroup;
			ComponentGroup sphereGroup;
			ComponentGroup capsuleGroup;
			ComponentGroup colliderGroup;

			protected override void OnCreateManager() {
				base.OnCreateManager();
				boxGroup = GetComponentGroup(new EntityArchetypeQuery {
					All = new ComponentType[] { ComponentType.ReadOnly(typeof(CopyColliderInfoFromGameObjectComponentData)),
					ComponentType.ReadOnly(typeof(BoxCollider)),
					ComponentType.ReadOnly(typeof(DirtyColliderComponentData))
					},
				});

				sphereGroup = GetComponentGroup(new EntityArchetypeQuery {
					All = new ComponentType[] { ComponentType.ReadOnly(typeof(CopyColliderInfoFromGameObjectComponentData)),
					ComponentType.ReadOnly(typeof(SphereCollider)),
					ComponentType.ReadOnly(typeof(DirtyColliderComponentData))
					},
				});

				capsuleGroup = GetComponentGroup(new EntityArchetypeQuery {
					All = new ComponentType[] { ComponentType.ReadOnly(typeof(CopyColliderInfoFromGameObjectComponentData)),
					ComponentType.ReadOnly(typeof(CapsuleCollider)),
					ComponentType.ReadOnly(typeof(DirtyColliderComponentData))
					},
				});

				colliderGroup = GetComponentGroup(new EntityArchetypeQuery {
					All = new ComponentType[] { ComponentType.ReadOnly(typeof(CopyColliderInfoFromGameObjectComponentData)),
					ComponentType.ReadOnly(typeof(DirtyColliderComponentData))
					},
					Any = new ComponentType[] { ComponentType.ReadOnly(typeof(CapsuleColliderComponentData)),
				ComponentType.ReadOnly(typeof(BoxColliderComponentData)),
				ComponentType.ReadOnly(typeof(SphereColliderComponentData))
					},
					None = new ComponentType[] { typeof(ColliderComponentData) }
				});
			}

			protected override void OnUpdate() {
				var boxChunks = boxGroup.CreateArchetypeChunkArray(Allocator.TempJob);
				var sphereChuncks = sphereGroup.CreateArchetypeChunkArray(Allocator.TempJob);
				var capsuleChunks = capsuleGroup.CreateArchetypeChunkArray(Allocator.TempJob);

				for (int i = 0, length = boxChunks.Length; i < length; ++i) {
					ArchetypeChunk boxChunk = boxChunks[i];
					var tempEntity = boxChunk.GetNativeArray(GetArchetypeChunkEntityType());
					NativeArray<Entity> entities = new NativeArray<Entity>(tempEntity.Length, Allocator.TempJob);
					tempEntity.CopyTo(entities);
					UpdateBoxColliders(entities,
						boxChunk.GetComponentObjects(GetArchetypeChunkComponentType<BoxCollider>(true), EntityManager));
					entities.Dispose();
				}

				for (int i = 0, length = sphereChuncks.Length; i < length; ++i) {
					ArchetypeChunk sphereChunk = sphereChuncks[i];
					var tempEntity = sphereChunk.GetNativeArray(GetArchetypeChunkEntityType());
					NativeArray<Entity> entities = new NativeArray<Entity>(tempEntity.Length, Allocator.TempJob);
					tempEntity.CopyTo(entities);
					UpdateSphereColliders(entities,
						sphereChunk.GetComponentObjects(GetArchetypeChunkComponentType<SphereCollider>(true), EntityManager));
					entities.Dispose();
				}

				for (int i = 0, length = capsuleChunks.Length; i < length; ++i) {
					ArchetypeChunk capsuleChunk = capsuleChunks[i];
					var tempEntity = capsuleChunk.GetNativeArray(GetArchetypeChunkEntityType());
					NativeArray<Entity> entities = new NativeArray<Entity>(tempEntity.Length, Allocator.TempJob);
					tempEntity.CopyTo(entities);
					UpdateCapsuleColliders(entities,
						capsuleChunk.GetComponentObjects(GetArchetypeChunkComponentType<CapsuleCollider>(true), EntityManager));
					entities.Dispose();
				}

				boxChunks.Dispose();
				sphereChuncks.Dispose();
				capsuleChunks.Dispose();

				var colliderChunks = colliderGroup.CreateArchetypeChunkArray(Allocator.TempJob);

				for (int i = 0, length = colliderChunks.Length; i < length; ++i) {
					ArchetypeChunk colliderChunk = colliderChunks[i];
					var tempEntity = colliderChunk.GetNativeArray(GetArchetypeChunkEntityType());
					NativeArray<Entity> entities = new NativeArray<Entity>(tempEntity.Length, Allocator.TempJob);
					tempEntity.CopyTo(entities);
					for (int j = 0, entityCount = entities.Length; j < entityCount; ++j) {
						ColliderComponentData colliderInfo = new ColliderComponentData();
						Entity entity = entities[j];
						if (GetComponentDataFromEntity<BoxColliderComponentData>(true).Exists(entity))
							colliderInfo.Volume |= E_CollisionVolume.BOX;
						if (GetComponentDataFromEntity<SphereColliderComponentData>(true).Exists(entity))
							colliderInfo.Volume |= E_CollisionVolume.SPHERE;
						if (GetComponentDataFromEntity<CapsuleColliderComponentData>(true).Exists(entity))
							colliderInfo.Volume |= E_CollisionVolume.CAPSULE;
						Utils.Utils.AddOrSetComponent(entity, colliderInfo, GetComponentDataFromEntity<ColliderComponentData>(true), EntityManager);
						EntityManager.RemoveComponent<DirtyColliderComponentData>(entity);
					}
					entities.Dispose();
				}
				if (World.GetExistingManager<PreCollisionSystem>() != null)
					World.GetExistingManager<PreCollisionSystem>().HasChanged = true;
				if (colliderChunks.IsCreated)
					colliderChunks.Dispose();
			}

			void UpdateBoxColliders(NativeArray<Entity> entities,
				[ReadOnly] ArchetypeChunkComponentObjects<BoxCollider> boxColliders) {
				for (int i = 0, length = boxColliders.Length; i < length; ++i) {
					BoxCollider box = boxColliders[i];
					if (box != null) {
						BoxColliderComponentData boxInfo = new BoxColliderComponentData();
						boxInfo.Center = box.center;
						boxInfo.Size = box.size;
						boxInfo.IsTrigger = box.isTrigger ? (byte)1 : (byte)0;
						Utils.Utils.AddOrSetComponent(entities[i], boxInfo, GetComponentDataFromEntity<BoxColliderComponentData>(true), EntityManager);
					}
				}
			}

			void UpdateSphereColliders(NativeArray<Entity> entities,
				[ReadOnly] ArchetypeChunkComponentObjects<SphereCollider> sphereColliders) {
				for (int i = 0, length = sphereColliders.Length; i < length; ++i) {
					SphereCollider sphere = sphereColliders[i];
					if (sphere != null) {
						SphereColliderComponentData sphereInfo = new SphereColliderComponentData();
						sphereInfo.Center = sphere.center;
						sphereInfo.Radius = sphere.radius;
						sphereInfo.IsTrigger = sphere.isTrigger ? (byte)1 : (byte)0;
						Utils.Utils.AddOrSetComponent(entities[i], sphereInfo, GetComponentDataFromEntity<SphereColliderComponentData>(true), EntityManager);
					}
				}
			}

			void UpdateCapsuleColliders(NativeArray<Entity> entities,
				[ReadOnly] ArchetypeChunkComponentObjects<CapsuleCollider> capsuleColliders) {
				for (int i = 0, length = capsuleColliders.Length; i < length; ++i) {
					CapsuleCollider capsule = capsuleColliders[i];
					if (capsule != null) {
						CapsuleColliderComponentData capsuleInfo = new CapsuleColliderComponentData();
						capsuleInfo.Center = capsule.center;
						capsuleInfo.Direction = capsule.direction;
						capsuleInfo.Height = capsule.height;
						capsuleInfo.Radius = capsule.radius;
						capsuleInfo.IsTrigger = capsule.isTrigger ? (byte)1 : (byte)0;
						Utils.Utils.AddOrSetComponent(entities[i], capsuleInfo, GetComponentDataFromEntity<CapsuleColliderComponentData>(true), EntityManager);
					}
				}
			}
		}
	}
}