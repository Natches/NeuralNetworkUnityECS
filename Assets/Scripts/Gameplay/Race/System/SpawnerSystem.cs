using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using System.Collections.Generic;


namespace ECS {
	namespace Spawner {

		[UpdateAfter(typeof(ECS.Timer.TimerSystem))]
		public class SpawnerSystem : ComponentSystem {

			struct SpawnInstance {
				public int spawnerIndex;
				public Entity sourceEntity;
			}

			ComponentGroup SpawnerGroup;

			protected override void OnUpdate() {
				List<SpawnerData> spawnerDatas = new List<SpawnerData>(10);
				EntityManager.GetAllUniqueSharedComponentData(spawnerDatas);
				int instanceCount = 0;
				int length = spawnerDatas.Count;
				for (int i = 0; i < length; ++i) {
					SpawnerData spawnerData = spawnerDatas[i];
					SpawnerGroup.SetFilter(spawnerData);
					EntityArray entities = SpawnerGroup.GetEntityArray();
					instanceCount += entities.Length;
				}
				NativeArray<SpawnInstance> spawnInstances = new NativeArray<SpawnInstance>(instanceCount, Allocator.Temp);
				{
					int spawnIndex = 0;
					for (int sharedIndex = 0; sharedIndex < length; ++sharedIndex) {
						SpawnerData spawnerData = spawnerDatas[sharedIndex];
						SpawnerGroup.SetFilter(spawnerData);
						EntityArray entities = SpawnerGroup.GetEntityArray();
						for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++) {
							SpawnInstance spawnInstance = new SpawnInstance();

							spawnInstance.sourceEntity = entities[entityIndex];
							spawnInstance.spawnerIndex = sharedIndex;

							spawnInstances[spawnIndex] = spawnInstance;
							spawnIndex++;
						}
					}
				}
				for (int spawnIndex = 0; spawnIndex < spawnInstances.Length; spawnIndex++) {
					int spawnerIndex = spawnInstances[spawnIndex].spawnerIndex;
					SpawnerData spawnerData = spawnerDatas[spawnerIndex];
					int count = spawnerData.Count;
					var entities = new NativeArray<Entity>(count, Allocator.Temp);
					GameObject prefab = spawnerData.Prefab;
					Entity sourceEntity = spawnInstances[spawnIndex].sourceEntity;
					float yOffset = spawnerData.yOffset;

					EntityManager.Instantiate(prefab, entities);
					float3 position = EntityManager.GetComponentData<Position>(sourceEntity).Value;

					ParentSpawnerData parentData = new ParentSpawnerData();
					parentData.Parent = sourceEntity;
					for (int i = 0; i < count; i++) {
						Position positionComponent = new Position {
							Value = position + new float3(0, yOffset, 0)
						};
						EntityManager.SetComponentData(entities[i], positionComponent);
						EntityManager.AddSharedComponentData(entities[i], parentData);
					}

					EntityManager.GetComponentObject<Transform>(sourceEntity).GetComponent<SpawnerComponent>().enabled = false;

					entities.Dispose();
				}
				spawnInstances.Dispose();
			}

			protected override void OnCreateManager() {
				SpawnerGroup = EntityManager.CreateComponentGroup(typeof(SpawnerData),
					typeof(Position), ComponentType.Subtractive<ECS.Timer.TimerData>(), typeof(Transform));
			}
		}
	}
}