using System;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;
using ECS.Utils;

namespace ECS {
	namespace NeuralNetwork {

		[UpdateAfter(typeof(NeuralNetworkFusionBootstrap))]
		public class NeuralNetworkFusionSystem : JobComponentSystem {

			[BurstCompile]
			struct FusionBestNeuralNetwork : IJobParallelForBatch {
				[ReadOnly] public float Mutation;
				[ReadOnly] public float PercentA;
				[ReadOnly] public float PercentB;
				[ReadOnly] public ValueTuple<float, NativeArray<float>> ScoreConnectionA;
				[ReadOnly] public ValueTuple<float, NativeArray<float>> ScoreConnectionB;
				[ReadOnly, DeallocateOnJobCompletion] public NativeArray<float> RandomNumbers;
				[ReadOnly, DeallocateOnJobCompletion] public NativeArray<float> RandomRanges;

				[NativeDisableParallelForRestriction] public NativeArray<float> Connections;

				public void Execute(int startIndex, int count) {
					count += startIndex;
					for (int i = startIndex, j = 0; i < count; ++i, ++j) {
						var data = Connections[i];
						if (RandomNumbers[i] < Mutation)
							data = RandomRanges[i];
						else
							data = (ScoreConnectionA.Item2[j] * PercentA + ScoreConnectionB.Item2[j] * PercentB);
						Connections[i] = data;
					}
				}
			}

			ComponentGroup geneticTrainerGroup;
			ComponentGroup neuralNetworkToResetGroup;

			protected override void OnCreateManager() {
				geneticTrainerGroup = GetComponentGroup(ComponentType.ReadOnly<GeneticTrainerComponentData>(),
					ComponentType.ReadOnly<Transform>(),
					ComponentType.Create<InitializedComponentData>(),
					ComponentType.Subtractive<ResetPoolFromPresetComponentData>());
				neuralNetworkToResetGroup = GetComponentGroup(ComponentType.Create<NeuralNetworkComponentData>(),
					ComponentType.ReadOnly<ResetComponentData>(),
					ComponentType.ReadOnly<GeneticTrainerIDComponentData>(),
					ComponentType.Create<InitializedComponentData>(),
					ComponentType.Subtractive<DeadComponentData>());
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps) {
				var geneticTrainerID = new List<GeneticTrainerIDComponentData>(10);
				EntityManager.GetAllUniqueSharedComponentData(geneticTrainerID);

				var transforms = geneticTrainerGroup.GetComponentArray<Transform>();
				for (int i = 0, length = transforms.Length; i < length; ++i) {
					var geneticTrainer = transforms[i].GetComponent<GeneticTrainerComponent>();
					neuralNetworkToResetGroup.SetFilter(geneticTrainerID[i]);//may need to change if more than one
					if (neuralNetworkToResetGroup.GetEntityArray().Length != 0) {
						if (neuralNetworkToResetGroup.GetEntityArray().Length == geneticTrainer.pool.Length) {
							var neuralNetwork = EntityManager.GetSharedComponentData<NeuralNetworkComponentData>(neuralNetworkToResetGroup.GetEntityArray()[0]);
							ComputeBestNetwork(geneticTrainer, neuralNetwork);
							var randomNumbers = new NativeArray<float>(neuralNetwork.Connections.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
							var randomRanges = new NativeArray<float>(neuralNetwork.Connections.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
							var rangeByGain = (geneticTrainer.Value.maxPoint / geneticTrainer.bestOverall[0].Item1) * neuralNetwork.WeightRange;
							System.Random random = new System.Random();
							for (int j = 0, numberCount = neuralNetwork.Connections.Length; j < numberCount; ++j) {
								var weightA = geneticTrainer.bestOverall[0].Item2[j % neuralNetwork.ConnectionCountPerNetwork];
								var weightB = geneticTrainer.bestOverall[1].Item2[j % neuralNetwork.ConnectionCountPerNetwork];
								randomNumbers[j] = (float)random.NextDouble();
								randomRanges[j] = UnityEngine.Random.Range(math.min(weightA - rangeByGain, weightB - rangeByGain),
									math.max(weightA + rangeByGain, weightB + rangeByGain));
							}
							var percentA = geneticTrainer.bestOverall[0].Item1 / (geneticTrainer.bestOverall[0].Item1 + geneticTrainer.bestOverall[1].Item1);
							var percentB = 1.0f - percentA;
							var job = new FusionBestNeuralNetwork {
								Connections = neuralNetwork.Connections,
								Mutation = geneticTrainer.Value.Mutation,
								PercentA = percentA,
								PercentB = percentB,
								RandomNumbers = randomNumbers,
								RandomRanges = randomRanges,
								ScoreConnectionA = geneticTrainer.bestOverall[0],
								ScoreConnectionB = geneticTrainer.bestOverall[1]
							};

							Utils.Utils.ScheduleBatchedJobAndComplete(job.ScheduleBatch(neuralNetwork.Connections.Length, neuralNetwork.ConnectionCountPerNetwork, inputDeps));

							geneticTrainer.scores.Clear();
							EntityManager.AddComponentData(geneticTrainer.GetComponent<GameObjectEntity>().Entity, new ResetPoolFromPresetComponentData());
							var data = geneticTrainer.Value;
							data.Generation += 1;
							geneticTrainer.Value = data;
							geneticTrainer.generationCount.text = (data.Generation).ToString();
						}
					}
				}
				return inputDeps;
			}

			void ComputeBestNetwork(GeneticTrainerComponent GeneticTrainer, NeuralNetworkComponentData neuralNetwork) {
				float key = GeneticTrainer.scores.Keys[0];
				if (GeneticTrainer.bestOverall[0].Item1 < key) {
					GeneticTrainer.bestOverall[1].Item1 = GeneticTrainer.bestOverall[0].Item1;
					GeneticTrainer.bestOverall[0].Item2.CopyNativeArrayTo(GeneticTrainer.bestOverall[1].Item2, GeneticTrainer.bestOverall[0].Item2.Length);
					GeneticTrainer.bestOverall[0].Item1 = key;
					int id = EntityManager.GetComponentData<NeuralNetworkIDComponentData>(GeneticTrainer.scores[key]).ID;
					Utils.Utils.ExpandOrCreateArray(ref GeneticTrainer.bestOverall[0].Item2, neuralNetwork.ConnectionCountPerNetwork, Allocator.Persistent);
					neuralNetwork.Connections.CopyNativeArrayTo(GeneticTrainer.bestOverall[0].Item2, neuralNetwork.ConnectionCountPerNetwork, id * neuralNetwork.ConnectionCountPerNetwork);
				}
				else if (GeneticTrainer.bestOverall[1].Item1 < key) {
					GeneticTrainer.bestOverall[1].Item1 = key;
					int id = EntityManager.GetComponentData<NeuralNetworkIDComponentData>(GeneticTrainer.scores[key]).ID;
					Utils.Utils.ExpandOrCreateArray(ref GeneticTrainer.bestOverall[1].Item2, neuralNetwork.ConnectionCountPerNetwork, Allocator.Persistent);
					neuralNetwork.Connections.CopyNativeArrayTo(GeneticTrainer.bestOverall[1].Item2, neuralNetwork.ConnectionCountPerNetwork, id * neuralNetwork.ConnectionCountPerNetwork);
				}
				if (GeneticTrainer.scores.Keys.Count < 2) {
					GeneticTrainer.bestOverall[1].Item1 = GeneticTrainer.bestOverall[0].Item1;
					GeneticTrainer.bestOverall[0].Item2.CopyNativeArrayTo(GeneticTrainer.bestOverall[1].Item2, GeneticTrainer.bestOverall[0].Item2.Length);
				}
				else {
					key = GeneticTrainer.scores.Keys[1];
					if (GeneticTrainer.bestOverall[1].Item1 < key) {
						GeneticTrainer.bestOverall[1].Item1 = key;
						int id = EntityManager.GetComponentData<NeuralNetworkIDComponentData>(GeneticTrainer.scores[key]).ID;
						Utils.Utils.ExpandOrCreateArray(ref GeneticTrainer.bestOverall[1].Item2, neuralNetwork.ConnectionCountPerNetwork, Allocator.Persistent);
						neuralNetwork.Connections.CopyNativeArrayTo(GeneticTrainer.bestOverall[1].Item2, neuralNetwork.ConnectionCountPerNetwork, id * neuralNetwork.ConnectionCountPerNetwork);
					}
				}
				Debug.Log(GeneticTrainer.bestOverall[0].Item1);
				Debug.Log(GeneticTrainer.bestOverall[1].Item1);
			}
		}
	}
}