using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using ECS.Timer;
using ECS.Spawner;

namespace ECS {
	namespace Race {

		[UpdateAfter(typeof(SpawnerSystem))]
		public class BonusPickedUpSystem : ComponentSystem {

			ComponentGroup group;
			ComponentGroup spawners;

			protected override void OnUpdate() {
				if (group.GetEntityArray().Length > 0) {
					List<ParentSpawnerData> parentSpawnerDatas = new List<ParentSpawnerData>(10);
					EntityManager.GetAllUniqueSharedComponentData(parentSpawnerDatas);
					int lengthParentSpawner = parentSpawnerDatas.Count;
					for (int ParentSpawnerIndex = 1; ParentSpawnerIndex < lengthParentSpawner; ++ParentSpawnerIndex) {
						Entity parent = parentSpawnerDatas[ParentSpawnerIndex].Parent;
						float respawnTime = EntityManager.GetSharedComponentData<BonusSpawnerComponentData>(parent).bonusRespawnDelay;
						TimerData timer = new TimerData();
						timer.Interval = respawnTime;
						EntityManager.AddComponentData(parent, timer);
					}
					GameObjectArray gameObjects = spawners.GetGameObjectArray();
					int length = gameObjects.Length;
					for (int i = 0; i < length; ++i) {
						gameObjects[i].GetComponent<SpawnerComponent>().enabled = true;
					}
				}
			}

			protected override void OnCreateManager() {
				base.OnCreateManager();
				group = GetComponentGroup(typeof(ParentSpawnerData),
					typeof(BonusComponentData),
					typeof(PickedUpComponentData));
				spawners = GetComponentGroup(typeof(Transform),
					typeof(SpawnerIDData));
			}
		}
	}
}