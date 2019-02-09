using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using ECS.Utils;
using ECS.Physics;
using ECS.Race;
using Unity.Transforms;
using ECS.Timer;

namespace ECS {
	namespace NeuralNetwork {

		[UpdateBefore(typeof(NeuralNetworkBootstrapSystem)), UpdateBefore(typeof(CopyColliderInfoFromGameObjectSystem))]
		public class GeneticTrainerInitializationSystem : ComponentSystem {

			ComponentGroup geneticTrainerGroup;
			ComponentGroup checkPointComponentGroup;

			protected override void OnCreateManager() {
				geneticTrainerGroup = GetComponentGroup(ComponentType.ReadOnly<GeneticTrainerComponentData>(),
					ComponentType.Create<GeneticTrainerComponent>(),
					ComponentType.Create<Transform>(),
					ComponentType.Create<TimerData>(),
					ComponentType.Subtractive<InitializedComponentData>());
				checkPointComponentGroup = GetComponentGroup(ComponentType.ReadOnly<CheckPointComponentData>(),
					ComponentType.ReadOnly<Position>(),
					ComponentType.ReadOnly<IDComponentData>(),
					ComponentType.ReadOnly<PointComponentData>());
			}

			protected override void OnUpdate() {
				if (geneticTrainerGroup.GetEntityArray().Length != 0) {
					float maxPoint = 0;
					for (int i = 0, length = checkPointComponentGroup.GetEntityArray().Length; i < length; ++i) {
						maxPoint += checkPointComponentGroup.GetComponentDataArray<PointComponentData>()[i].PointValue;
					}
					var chunks = geneticTrainerGroup.CreateArchetypeChunkArray(Allocator.TempJob);
					for (int i = 0, length = chunks.Length; i < length; ++i) {
						var chunk = chunks[i];
						var entities = chunk.GetNativeArray(GetArchetypeChunkEntityType());
						for (int j = 0, trainerCount = entities.Length; j < length; ++j) {
							var entity = entities[j];
							var geneticTrainer = EntityManager.GetComponentObject<Transform>(entity).GetComponent<GeneticTrainerComponent>();
							var timer = EntityManager.GetComponentData<TimerData>(entity);
							timer.Stopped = 1;
							EntityManager.SetComponentData(entity, timer);
							var data = geneticTrainer.Value;
							data.maxPoint = maxPoint;
							geneticTrainer.Value = data;
							if (geneticTrainer.preset == null) {
								geneticTrainer.preset = geneticTrainer.InstantiateGameObject(geneticTrainer.unInitialiazedPreset);
								EntityManager.Instantiate(geneticTrainer.preset, geneticTrainer.pool);
								EntityManager.AddComponent(geneticTrainer.pool, ComponentType.Create<CollisionStateComponent>());
								EntityManager.AddComponent(geneticTrainer.pool, ComponentType.Create<CollisionResponseComponent>());
								var geneticTrainerID = new GeneticTrainerIDComponentData { ID = geneticTrainer.Value.TrainerID };
								for (int k = 0, poolLength = geneticTrainer.pool.Length; k < poolLength; ++k) {
									var poolEntity = geneticTrainer.pool[k];
									EntityManager.AddComponentData(poolEntity, new FrozenComponentData());
									EntityManager.AddComponentData(poolEntity, new NeuralNetworkIDComponentData { ID = k });
									EntityManager.AddSharedComponentData(poolEntity, geneticTrainerID);
									EntityManager.SetComponentObject(poolEntity, ComponentType.Create<CollisionStateComponent>(), new CollisionStateComponent());
									EntityManager.SetComponentObject(poolEntity, ComponentType.Create<CollisionResponseComponent>(),
										new CarCollisionResponse(poolEntity, EntityManager,
										checkPointComponentGroup.GetEntityArray().Length, checkPointComponentGroup.GetComponentDataArray<Position>(),
										checkPointComponentGroup.GetComponentDataArray<PointComponentData>(), checkPointComponentGroup.GetComponentDataArray<IDComponentData>()));
								}
								EntityManager.AddComponentData(entity, new InitializedComponentData());
							}
						}
					}
					chunks.Dispose();
				}
			}
		}

		[UpdateBefore(typeof(NeuralNetworkBootstrapSystem)), UpdateBefore(typeof(PreCollisionSystem)), UpdateAfter(typeof(CopyColliderInfoFromGameObjectSystem))]
		public class GeneticTrainerAddColliderFromPresetSystem : ComponentSystem {

			ComponentGroup geneticTrainerGroup;

			protected override void OnCreateManager() {
				geneticTrainerGroup = GetComponentGroup(ComponentType.ReadOnly<GeneticTrainerComponentData>(),
					ComponentType.Create<GeneticTrainerComponent>(),
					ComponentType.Create<Transform>(),
					ComponentType.Create<InitializedComponentData>(),
					ComponentType.Subtractive<ColliderAddedData>());
			}

			protected override void OnUpdate() {
				var chunks = geneticTrainerGroup.CreateArchetypeChunkArray(Allocator.TempJob);
				for (int i = 0, length = chunks.Length; i < length; ++i) {
					var chunk = chunks[i];
					var entities = chunk.GetNativeArray(GetArchetypeChunkEntityType());
					for (int j = 0, trainerCount = entities.Length; j < length; ++j) {
						var entity = entities[j];
						var geneticTrainer = EntityManager.GetComponentObject<Transform>(entity).GetComponent<GeneticTrainerComponent>();
						var presetEntity = geneticTrainer.preset.GetComponent<GameObjectEntity>().Entity;
						var colliderComponent = EntityManager.GetComponentData<ColliderComponentData>(presetEntity);
						for (int k = 0, poolLength = geneticTrainer.pool.Length; k < poolLength; ++k) {
							var poolEntity = geneticTrainer.pool[k];
							EntityManager.AddComponentData(poolEntity, colliderComponent);
							switch (colliderComponent.Volume) {
								case E_CollisionVolume.BOX:
									EntityManager.AddComponentData(poolEntity, EntityManager.GetComponentData<BoxColliderComponentData>(presetEntity));
									break;
								case E_CollisionVolume.SPHERE:
									EntityManager.AddComponentData(poolEntity, EntityManager.GetComponentData<SphereColliderComponentData>(presetEntity));
									break;
								case E_CollisionVolume.CAPSULE:
									EntityManager.AddComponentData(poolEntity, EntityManager.GetComponentData<CapsuleColliderComponentData>(presetEntity));
									break;
							}
						}
						EntityManager.AddComponentData(entity, new ColliderAddedData());
						geneticTrainer.preset.SetActive(false);
					}
				}
				chunks.Dispose();
			}
		}
	}
}