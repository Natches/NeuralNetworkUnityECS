using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using System.Collections.Generic;
using ECS.Timer;
using ECS.Physics;
using ECS.Movement;

namespace ECS {
	namespace NeuralNetwork {

		[UpdateAfter(typeof(NeuralNetworkFusionSystem))]
		public class ResetGeneticTrainerPoolFromPresetSystem : ComponentSystem {

			ComponentGroup geneticTrainerGroup;
			ComponentGroup neuralNetworkToResetGroup;

			protected override void OnCreateManager() {
				geneticTrainerGroup = GetComponentGroup(ComponentType.ReadOnly<GeneticTrainerComponentData>(),
					ComponentType.Create<Transform>(),
					ComponentType.Create<GeneticTrainerComponent>(),
					ComponentType.Create<InitializedComponentData>(),
					ComponentType.Create<ResetPoolFromPresetComponentData>());
				neuralNetworkToResetGroup = GetComponentGroup(ComponentType.ReadOnly<NeuralNetworkComponentData>(),
					ComponentType.ReadOnly<ResetComponentData>(),
					ComponentType.ReadOnly<GeneticTrainerIDComponentData>(),
					ComponentType.Create<PointComponentData>(),
					ComponentType.Create<BonusMalusComponentData>(),
					ComponentType.Create<CollisionStateComponent>(),
					ComponentType.Create<Position>(),
					ComponentType.Create<Rotation>(),
					ComponentType.Create<MovementData>(),
					ComponentType.Create<SteeringData>(),
					ComponentType.Create<IDComponentData>(),
					ComponentType.Create<TimerData>(),
					ComponentType.Subtractive<DeadComponentData>());
			}

			protected override void OnUpdate() {
				var geneticTrainerID = new List<GeneticTrainerIDComponentData>(10);
				EntityManager.GetAllUniqueSharedComponentData(geneticTrainerID);

				var transforms = geneticTrainerGroup.GetComponentArray<Transform>();
				for (int i = 0, length = transforms.Length; i < length; ++i) {
					var geneticTrainer = transforms[i].GetComponent<GeneticTrainerComponent>();
					var entities = neuralNetworkToResetGroup.GetEntityArray().ToArray();
					var pointComponentArray = neuralNetworkToResetGroup.GetComponentDataArray<PointComponentData>();
					var bonusMalusComponentArray = neuralNetworkToResetGroup.GetComponentDataArray<BonusMalusComponentData>();
					var positionComponentArray = neuralNetworkToResetGroup.GetComponentDataArray<Position>();
					var rotationComponentArray = neuralNetworkToResetGroup.GetComponentDataArray<Rotation>();
					var nextCheckpointComponentArray = neuralNetworkToResetGroup.GetComponentDataArray<IDComponentData>();
					var movementArray = neuralNetworkToResetGroup.GetComponentDataArray<MovementData>();
					var steeringArray = neuralNetworkToResetGroup.GetComponentDataArray<SteeringData>();
					var timerArray = neuralNetworkToResetGroup.GetComponentDataArray<TimerData>();

					var presetRotation = geneticTrainer.preset.GetComponent<RotationComponent>();
					var presetPosition = geneticTrainer.preset.GetComponent<PositionComponent>();
					var presetMovement = geneticTrainer.preset.GetComponent<Movement.Movement>();
					var presetSteering = geneticTrainer.preset.GetComponent<Steering>();
					var collisionStateArray = neuralNetworkToResetGroup.GetComponentArray<CollisionStateComponent>();
					for (int j = 0, entityCount = entities.Length; j < entityCount; ++j) {
						var pointComponent = pointComponentArray[j];
						pointComponent.PointValue = 0;
						pointComponentArray[j] = pointComponent;

						var bonusMalus = bonusMalusComponentArray[j];
						bonusMalus.Bonus = 0;
						bonusMalus.Malus = 0;
						bonusMalusComponentArray[j] = bonusMalus;

						positionComponentArray[j] = presetPosition.Value;
						rotationComponentArray[j] = presetRotation.Value;

						var nextCheckpoint = nextCheckpointComponentArray[j];
						nextCheckpoint.ID = 0;
						nextCheckpointComponentArray[j] = nextCheckpoint;

						var collisionState = collisionStateArray[j];
						collisionState.colliders.Clear();
						collisionState.presentColliders.Clear();

						var movement = movementArray[j];
						movement.HasArrived = 0;
						movement.Rotation = presetMovement.Value.Rotation;
						movement.TargetPos = presetMovement.Value.TargetPos;
						movement.Velocity = presetMovement.Value.Velocity;
						movementArray[j] = movement;

						var steering = steeringArray[j];
						steering.Velocity = presetSteering.Value.Velocity;
						steeringArray[j] = steering;

						var time = timerArray[j];
						time.Time = 0.0f;
						timerArray[j] = time;
					}
					for (int j = 0, entityCount = entities.Length; j < entityCount; ++j) {
						EntityManager.RemoveComponent(entities[j], ComponentType.Create<ResetComponentData>());
					}

					Timer.Timer timer = geneticTrainer.GetComponent<Timer.Timer>();
					var data = timer.Value;
					data.Time = 0;
					data.Stopped = 0;
					timer.Value = data;
					timer.enabled = true;

					EntityManager.RemoveComponent<ResetPoolFromPresetComponentData>(geneticTrainer.GetComponent<GameObjectEntity>().Entity);

					World.GetExistingManager<PreCollisionSystem>().HasChanged = true;
				}
			}
		}
	}
}