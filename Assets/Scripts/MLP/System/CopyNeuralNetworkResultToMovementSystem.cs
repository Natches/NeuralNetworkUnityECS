using Unity.Burst;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine.Jobs;
using ECS.Movement;

namespace ECS {
	namespace NeuralNetwork {

		[UpdateAfter(typeof(UpdateNeuralNetworkSystem)), UpdateBefore(typeof(SteeringSystem))]
		public class CopyNeuralNetworkResultToMovementSystem : JobComponentSystem {

			[BurstCompile]
			struct UpdateMovementTargetPos : IJobParallelFor {

				[ReadOnly] public NeuralNetworkComponentData NeuralNetwork;
				[ReadOnly] public ComponentDataArray<Position> Positions;
				[ReadOnly] public ComponentDataArray<NeuralNetworkIDComponentData> IDs;

				[NativeDisableParallelForRestriction] public ComponentDataArray<MovementData> Movements;

				public void Execute(int index) {
					int resultIndex = IDs[index].ID * NeuralNetwork.ResultCountPerNetwork + (NeuralNetwork.ResultCountPerNetwork - NeuralNetwork.PerceptronPerLayerCount[NeuralNetwork.PerceptronPerLayerCount.Length - 1]);
					var movement = Movements[index];
					movement.TargetPos = Positions[index].Value + math.normalizesafe(new float3(NeuralNetwork.Results[resultIndex], 0.0f, NeuralNetwork.Results[resultIndex + 1]));
					Movements[index] = movement;
				}
			}

			ComponentGroup neuralNetworkGroup;

			protected override void OnCreateManager() {
				neuralNetworkGroup = GetComponentGroup(ComponentType.ReadOnly<NeuralNetworkComponentData>(),
					ComponentType.ReadOnly<NeuralNetworkIDComponentData>(),
					ComponentType.ReadOnly<Position>(),
					ComponentType.ReadOnly<InitializedComponentData>(),
					ComponentType.Create<MovementData>(),
					ComponentType.Subtractive<DeadComponentData>(),
					ComponentType.Subtractive<ResetComponentData>(),
					ComponentType.Subtractive<FrozenComponentData>());
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps) {
				var neuralNetworks = new List<NeuralNetworkComponentData>(20);
				EntityManager.GetAllUniqueSharedComponentData(neuralNetworks);
				for (int i = 0, length = neuralNetworks.Count; i < length; ++i) {
					var neuralNetwork = neuralNetworks[i];
					neuralNetworkGroup.SetFilter(neuralNetwork);
					var movements = neuralNetworkGroup.GetComponentDataArray<MovementData>();
					if (movements.Length != 0) {
						var job = new UpdateMovementTargetPos {
							Movements = movements,
							Positions = neuralNetworkGroup.GetComponentDataArray<Position>(),
							NeuralNetwork = neuralNetwork,
							IDs = neuralNetworkGroup.GetComponentDataArray<NeuralNetworkIDComponentData>()
						};
						Utils.Utils.ScheduleBatchedJobAndComplete(job.Schedule(movements.Length, 32, inputDeps));
					}
				}
				return inputDeps;
			}
		}
	}
}