using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using ECS.Physics;

namespace ECS {
	namespace NeuralNetwork {

		[UpdateAfter(typeof(CollisionSystem)), UpdateAfter(typeof(ECS.Timer.TimerSystem))]
		public class NeuralNetworkFusionBootstrap : ComponentSystem {

			ComponentGroup geneticTrainerGroup;
			ComponentGroup deadNetworkGroup;

			protected override void OnCreateManager() {
				geneticTrainerGroup = GetComponentGroup(ComponentType.ReadOnly<GeneticTrainerComponentData>(),
					ComponentType.ReadOnly<Transform>(),
					ComponentType.Create<InitializedComponentData>());
				deadNetworkGroup = GetComponentGroup(ComponentType.ReadOnly<NeuralNetworkComponentData>(),
					ComponentType.Create<DeadComponentData>(),
					ComponentType.ReadOnly<PointComponentData>(),
					ComponentType.ReadOnly<GeneticTrainerIDComponentData>(),
					ComponentType.Create<InitializedComponentData>(),
					ComponentType.Subtractive<ResetComponentData>());
			}

			protected override void OnUpdate() {
				var geneticTrainerID = new List<GeneticTrainerIDComponentData>(10);
				EntityManager.GetAllUniqueSharedComponentData(geneticTrainerID);

				var transforms = geneticTrainerGroup.GetComponentArray<Transform>();
				for (int i = 0, length = transforms.Length; i < length; ++i) {
					var geneticTrainer = transforms[i].GetComponent<GeneticTrainerComponent>();
					deadNetworkGroup.SetFilter(geneticTrainerID[i]);//may need to change if more than one
					if (deadNetworkGroup.GetEntityArray().Length != 0) {
						if (deadNetworkGroup.GetEntityArray().Length == geneticTrainer.pool.Length) {
							for (int j = 0, entityCount = deadNetworkGroup.GetEntityArray().Length; deadNetworkGroup.GetEntityArray().Length != 0;) {
								var entity = deadNetworkGroup.GetEntityArray()[j];
								geneticTrainer.scores[deadNetworkGroup.GetComponentDataArray<PointComponentData>()[j].PointValue] = entity;
								EntityManager.AddComponentData(entity, new ResetComponentData());
								EntityManager.RemoveComponent(entity, ComponentType.Create<DeadComponentData>());
							}
						}
					}
				}
			}
		}
	}
}