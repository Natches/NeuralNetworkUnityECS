using Unity.Burst;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using System.Collections.Generic;
using ECS.Utils;

namespace ECS {
	namespace NeuralNetwork {

		[UpdateBefore(typeof(PreUpdateNeuralNetworkSystem))]
		public class NeuralNetworkBootstrapSystem : JobComponentSystem {

			ComponentGroup groupToInit;
			ComponentGroup neuralNetworkGroup;

			protected override void OnCreateManager() {
				groupToInit = GetComponentGroup(ComponentType.ReadOnly<SharedComponentIndexToAddComponentData>(),
					ComponentType.ReadOnly<InitializeNeuralNetworkComponentData>(),
					ComponentType.Subtractive<NeuralNetworkComponentData>());
				neuralNetworkGroup = GetComponentGroup(typeof(NeuralNetworkComponentData),
					typeof(SharedDataInitializerComponentData));
			}

			[BurstCompile]
			struct InitConnection : IJobParallelForBatch {
				[ReadOnly] public int ConnectionPerNetwork;
				[ReadOnly] public NativeArray<int> PerceptronPerLayer;
				[ReadOnly] public NativeArray<float> RandomNumbers;

				[WriteOnly, NativeDisableParallelForRestriction] public NativeArray<float> Connections;

				public void Execute(int startIndex, int count) {
					int connectionNumber = ConnectionPerNetwork * startIndex / count;
					int perceptronNumber = 0;
					for (int layer = 0, layerCount = PerceptronPerLayer.Length - 1; layer < layerCount; ++layer) {
						int connectionCount = PerceptronPerLayer[layer];
						int perceptronCount = PerceptronPerLayer[layer + 1];
						for (int j = 0; j < perceptronCount; ++j, ++perceptronNumber) {
							for (int k = 0; k < connectionCount; ++k, ++connectionNumber) {
								Connections[connectionNumber] = RandomNumbers[connectionNumber];
							}
						}
					}
				}
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps) {
				if (groupToInit.GetEntityArray().Length == 0)
					return inputDeps;
				var sharedIdxs = new List<SharedComponentIndexToAddComponentData>(20);
				EntityManager.GetAllUniqueSharedComponentData(sharedIdxs);
				for (int i = 0, length = sharedIdxs.Count; i < length; ++i) {
					var sharedIdx = sharedIdxs[i];
					groupToInit.SetFilter(sharedIdx);
					if (groupToInit.GetEntityArray().Length == 0)
						continue;
					var entities = new NativeArray<Entity>(groupToInit.GetEntityArray().ToArray(), Allocator.TempJob);
					var neuralNetwork = neuralNetworkGroup.GetSharedComponentDataArray<NeuralNetworkComponentData>()[sharedIdx.Index];
					var inputCount = neuralNetwork.PerceptronPerLayerCount[0];


					neuralNetwork.Inputs = new NativeArray<float>(inputCount * entities.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
					int perceptronCount = inputCount, connectionCount = 0, previousCount = perceptronCount;
					for (int j = 1, length2 = neuralNetwork.PerceptronPerLayerCount.Length; j < length2; ++j) {
						int perceptronPerLayer = neuralNetwork.PerceptronPerLayerCount[j];
						connectionCount += previousCount * perceptronPerLayer;
						perceptronCount += perceptronPerLayer;
						previousCount = perceptronPerLayer;
					}
					neuralNetwork.ConnectionCountPerNetwork = connectionCount;
					neuralNetwork.PerceptronCountPerNetwork = perceptronCount;
					neuralNetwork.ResultCountPerNetwork = perceptronCount - inputCount;
					neuralNetwork.PerceptronCount = (perceptronCount * entities.Length);
					neuralNetwork.Results = new NativeArray<float>((perceptronCount * entities.Length) - (inputCount * entities.Length), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
					neuralNetwork.Connections = new NativeArray<float>(connectionCount * entities.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
					var randomNumbers = new NativeList<float>(connectionCount * entities.Length, Allocator.TempJob);
					for (int j = 0, count = connectionCount * entities.Length; j < count; ++j)
						randomNumbers.Add(UnityEngine.Random.Range(-neuralNetwork.WeightRange, neuralNetwork.WeightRange));

					var job = new InitConnection {
						PerceptronPerLayer = neuralNetwork.PerceptronPerLayerCount,
						Connections = neuralNetwork.Connections,
						RandomNumbers = randomNumbers,
						ConnectionPerNetwork = connectionCount
					};
					var handle = job.ScheduleBatch(perceptronCount * entities.Length, perceptronCount, inputDeps);

					JobHandle.ScheduleBatchedJobs();

					handle.Complete();

					for (int j = 0, entityCount = entities.Length; j < entityCount; ++j) {
						var entity = entities[j];
						EntityManager.AddSharedComponentData(entity, neuralNetwork);
						EntityManager.AddComponentData(entity, new InitializedComponentData());
						EntityManager.RemoveComponent<SharedComponentIndexToAddComponentData>(entity);
						EntityManager.RemoveComponent<InitializeNeuralNetworkComponentData>(entity);
					}

					randomNumbers.Dispose();
					entities.Dispose();
				}
				return inputDeps;
			}
		}
	}
}