using Unity.Burst;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine.Jobs;
using ECS.Movement;

namespace ECS {
	namespace NeuralNetwork {

		[UpdateBefore(typeof(MovementSystem))]
		public class UpdateNeuralNetworkSystem : JobComponentSystem {

			[BurstCompile]
			struct UpdateNeuralNetwork : IJobParallelFor {
				[ReadOnly] public int ResultCountPerNetwork;
				[ReadOnly] public int ConnectionPerNetwork;
				[ReadOnly] public float Poise;
				[ReadOnly] public ComponentDataArray<NeuralNetworkIDComponentData> NeuralNetworkIDs;
				[ReadOnly] public NativeArray<float> Inputs;
				[ReadOnly] public NativeArray<int> PerceptronPerLayer;
				[ReadOnly] public NativeArray<float> Connections;
				[ReadOnly] public E_THRESHOLDFUNCTION hiddenLayerFunction;
				[ReadOnly] public E_THRESHOLDFUNCTION outputLayerFunction;
				[NativeDisableParallelForRestriction] public NativeArray<float> Results;

				public void Execute(int index) {
					int neuralNetworkID = NeuralNetworkIDs[index].ID;

					int startInputIndex = PerceptronPerLayer[0] * neuralNetworkID;
					int startConnectionIndex = ConnectionPerNetwork * neuralNetworkID;
					int startResultIndex = ResultCountPerNetwork * neuralNetworkID;

					int currentConnectionIndex = startConnectionIndex;
					int currentResultIndex = startResultIndex;

					ComputeNextLayerResultFromConnections(PerceptronPerLayer[0], PerceptronPerLayer[1],
						startInputIndex, ref currentResultIndex,
						ref currentConnectionIndex, Inputs, Results);
					for (int i = 1, length = PerceptronPerLayer.Length - 1; i < length; ++i) {
						ComputeNextLayerResultFromConnections(PerceptronPerLayer[i], PerceptronPerLayer[i + 1],
							startResultIndex, ref currentResultIndex,
							ref currentConnectionIndex, Results, Results);
					}
					ComputeThresholdFunctions(startResultIndex);
				}

				void ComputeNextLayerResultFromConnections(int perceptronCountCurrentLayer, int perceptronCoutNextLayer,
					int startInputIndex, ref int startOutputIndex, ref int startConnectionIndex,
					NativeArray<float> inputs, NativeArray<float> output) {
					for (int i = 0; i < perceptronCoutNextLayer; ++i) {
						float sum = 0f;
						for (int j = 0, inputIndex = startInputIndex; j < perceptronCountCurrentLayer; ++j, ++inputIndex) {
							var input = inputs[inputIndex];
							sum += Connections[startConnectionIndex++] * input;
						}
						Results[startOutputIndex++] = sum;
					}
				}

				void ComputeThresholdFunctions(int startIndex) {
					for (int i = startIndex, length = startIndex + ResultCountPerNetwork,
						lengthMinusOutputCount = length - PerceptronPerLayer[PerceptronPerLayer.Length - 1]; i < length; ++i) {
						E_THRESHOLDFUNCTION function = i >= lengthMinusOutputCount ? outputLayerFunction : hiddenLayerFunction;
						Results[i] = ThresholdFunction(function, Results[i], Poise);
					}
				}

				float ThresholdFunction(E_THRESHOLDFUNCTION function, float input, float poise = 1) {
					switch (function) {
						case E_THRESHOLDFUNCTION.SIGMOID:
							return 1.0f / (1 + math.exp(-input));
						case E_THRESHOLDFUNCTION.TANH:
							return math.tanh(input);
						case E_THRESHOLDFUNCTION.ARCTAN:
							return math.atan(input);
						case E_THRESHOLDFUNCTION.ALGEBRAIC:
							return input / (1 + math.abs(input));
						case E_THRESHOLDFUNCTION.ALGEBRAICWITHPOISE:
							input = input * poise;
							var absInput = math.abs(input);
							return (math.atan(input) * absInput) / (1 + absInput);
						case E_THRESHOLDFUNCTION.NONE:
							return input;
						default:
							return input;
					}
				}
			}


			ComponentGroup NeuralNetworkGroup;

			protected override void OnCreateManager() {
				NeuralNetworkGroup = GetComponentGroup(ComponentType.Create<NeuralNetworkComponentData>(),
					ComponentType.ReadOnly<NeuralNetworkIDComponentData>(),
					ComponentType.ReadOnly<InitializedComponentData>(),
					ComponentType.Subtractive<DeadComponentData>(),
					ComponentType.Subtractive<ResetComponentData>(),
					ComponentType.Subtractive<FrozenComponentData>());
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps) {
				var neuralNetworks = new List<NeuralNetworkComponentData>(20);
				EntityManager.GetAllUniqueSharedComponentData(neuralNetworks);
				for (int i = 0, length = neuralNetworks.Count; i < length; ++i) {
					var neuralNetwork = neuralNetworks[i];
					NeuralNetworkGroup.SetFilter(neuralNetwork);
					if (NeuralNetworkGroup.GetEntityArray().Length != 0) {
						var job = new UpdateNeuralNetwork {
							ResultCountPerNetwork = neuralNetwork.ResultCountPerNetwork,
							Inputs = neuralNetwork.Inputs,
							Connections = neuralNetwork.Connections,
							Results = neuralNetwork.Results,
							Poise = neuralNetwork.Poise,
							hiddenLayerFunction = neuralNetwork.HiddenLayerThresholdFunction,
							outputLayerFunction = neuralNetwork.OutputThresholdFunction,
							PerceptronPerLayer = neuralNetwork.PerceptronPerLayerCount,
							ConnectionPerNetwork = neuralNetwork.ConnectionCountPerNetwork,
							NeuralNetworkIDs = NeuralNetworkGroup.GetComponentDataArray<NeuralNetworkIDComponentData>()
						};
						Utils.Utils.ScheduleBatchedJobAndComplete(job.Schedule(NeuralNetworkGroup.GetComponentDataArray<NeuralNetworkIDComponentData>().Length, 64, inputDeps));
					}
				}
				return inputDeps;
			}
		}
	}
}