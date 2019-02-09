using UnityEngine;
using Unity.Entities;
using ECS.NeuralNetwork;
using ECS.Timer;
using ECS.Utils;
using ECS.Movement;
using ECS.Race;
using System.Collections.Generic;
using Unity.Transforms;
using Unity.Mathematics;
using System;

namespace ECS {
	namespace Physics {

		class CarCollisionResponse : CollisionResponseComponent {

			public CarCollisionResponse(Entity entity, EntityManager entityManager,
				int checkPointNumber, ComponentDataArray<Position> checkPointPositions,
				ComponentDataArray<PointComponentData> checkPointValues,
				ComponentDataArray<IDComponentData> checkPointIDs) {
				this.entityManager = entityManager;
				this.entity = entity;
				this.checkPointNumber = checkPointNumber;
				collisionState = entityManager.GetComponentObject<CollisionStateComponent>(entity);

				onCollisionEnter += CollisionEnter;
				onCollisionExit += CollisionExit;
				onCollisionStay += CollisionStay;

				idToCheckPointPosition = new float3[checkPointNumber];
				idToCheckPointValue = new float[checkPointNumber];
				for (int i = 0; i < checkPointNumber; ++i) {
					var id = checkPointIDs[i].ID;
					idToCheckPointPosition[id] = checkPointPositions[i].Value;
					idToCheckPointValue[id] = checkPointValues[i].PointValue;
				}
			}

			EntityManager entityManager;
			Entity entity;
			CollisionStateComponent collisionState;
			float3[] idToCheckPointPosition;
			float[] idToCheckPointValue;


			int checkPointNumber;

			protected override void OnEnable() {
				var gameObjectEntity = GetComponent<GameObjectEntity>();
				entityManager = gameObjectEntity.EntityManager;
				entity = gameObjectEntity.Entity;
			}

			protected override void OnDisable() {
				onCollisionEnter -= CollisionEnter;
				onCollisionExit -= CollisionExit;
				onCollisionStay -= CollisionStay;
			}

			protected override void CollisionEnter(RaycastHit hit) {
				var nextCheckPoint = entityManager.GetComponentData<IDComponentData>(entity);
				var bonusMalus = entityManager.GetComponentData<BonusMalusComponentData>(entity);
				if (hit.collider.tag == "Wall") {
					if (!entityManager.HasComponent<DeadComponentData>(entity))
						entityManager.AddComponentData(entity, new DeadComponentData());
					var checkPointPosition = idToCheckPointPosition[nextCheckPoint.ID];
					var position = entityManager.GetComponentData<Position>(entity).Value;
					if (math.dot(hit.normal, math.normalizesafe(checkPointPosition - position)) <= 0)
						bonusMalus.Malus += idToCheckPointValue[nextCheckPoint.ID] * 2;
					collisionState.colliders.Clear();
					collisionState.presentColliders.Clear();
				}
				else if (hit.collider.tag == "Bonus") {
					var movement = entityManager.GetComponentData<MovementData>(entity);
					var bonus = entityManager.GetSharedComponentData<BonusComponentData>(hit.collider.gameObject.GetComponent<GameObjectEntity>().Entity);
					movement.BoostValue = bonus.BoostValue;
					entityManager.SetComponentData(entity, movement);

					Timer.Timer timer = new Timer.Timer();
					var data = timer.Value;
					data.Interval = bonus.BoostDuration;
					data.Loop = 0;
					timer.Value = data;
					timer.onElapsed += ResetBoost;

					var entityTemp = entityManager.Instantiate(new Entity());
					entityManager.AddComponent(entityTemp, ComponentType.Create<Timer.Timer>());
					entityManager.SetComponentObject(entityTemp, ComponentType.Create<Timer.Timer>(), timer);
				}
				else if (hit.collider.tag == "Checkpoint") {
					var checkPointEntity = hit.collider.GetComponent<GameObjectEntity>().Entity;
					var timer = entityManager.GetComponentData<TimerData>(entity);
					if (entityManager.GetComponentData<IDComponentData>(checkPointEntity).ID == nextCheckPoint.ID) {
						bonusMalus.Bonus += entityManager.GetComponentData<PointComponentData>(checkPointEntity).PointValue / (timer.Time == 0 ? float.Epsilon : timer.Time);
						nextCheckPoint.ID = (nextCheckPoint.ID + 1) % checkPointNumber;
						entityManager.SetComponentData(entity, nextCheckPoint);
					}
					else {
						bonusMalus.Malus = entityManager.GetComponentData<PointComponentData>(checkPointEntity).PointValue;
						if(!entityManager.HasComponent<DeadComponentData>(entity))
							entityManager.AddComponentData(entity, new DeadComponentData());
						collisionState.colliders.Clear();
						collisionState.presentColliders.Clear();
					}
					timer.Time = 0;
					entityManager.SetComponentData(entity, timer);
				}
				entityManager.SetComponentData(entity, bonusMalus);
			}

			void ResetBoost() {
				var movement = entityManager.GetComponentData<MovementData>(entity);
				movement.BoostValue = 1;
				entityManager.SetComponentData(entity, movement);
			}

			protected override void CollisionStay(RaycastHit hit) {
				return;
			}

			protected override void CollisionExit(Collider other) {
				return;
			}
		}
	}
}