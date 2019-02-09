using System;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace ECS {
	namespace NeuralNetwork {

		public enum E_InputType {
			RAYCAST,
			POSITION,
			TARGET_POSITION
		}

		public enum E_THRESHOLDFUNCTION {
			SIGMOID,
			TANH,
			ARCTAN,
			ALGEBRAIC,
			ALGEBRAICWITHPOISE,
			NONE
		}

		[Serializable]
		public struct NeuralNetworkComponentData : ISharedComponentData {
			internal NativeArray<int> PerceptronPerLayerCount;
			internal NativeArray<float> Inputs;
			internal NativeArray<float> Connections;
			internal NativeArray<float> Results;
			internal NativeArray<float3> RaycastDirections;
			internal int ConnectionCountPerNetwork;
			internal int PerceptronCountPerNetwork;
			internal int ResultCountPerNetwork;
			internal int PerceptronCount;

			public E_THRESHOLDFUNCTION HiddenLayerThresholdFunction;
			public E_THRESHOLDFUNCTION OutputThresholdFunction;
			public E_InputType InputType;

			public float WeightRange;
			public float Poise;
		}

		public class NeuralNetworkComponent : SharedComponentDataWrapper<NeuralNetworkComponentData> {
			public int[] PerceptronPerLayerCount = new int[5];
			public float3[] RaycastsDirections;

			protected override void ValidateSerializedData(ref NeuralNetworkComponentData serializedData) {
				if (serializedData.InputType == E_InputType.POSITION || serializedData.InputType == E_InputType.TARGET_POSITION) {
					PerceptronPerLayerCount[0] = 3;
				}
				if (RaycastsDirections != null && RaycastsDirections.Length != PerceptronPerLayerCount[0]) {
					var RaycastsDirectionsOld = RaycastsDirections;
					RaycastsDirections = new float3[PerceptronPerLayerCount[0]];
					for (int i = 0, length = RaycastsDirectionsOld.Length < PerceptronPerLayerCount[0] ? RaycastsDirectionsOld.Length : PerceptronPerLayerCount[0]; i < length; ++i) {
						RaycastsDirections[i] = RaycastsDirectionsOld[i];
					}
				}
				else {
					if (serializedData.RaycastDirections.IsCreated)
						serializedData.RaycastDirections.Dispose();
					for (int i = 0, length = PerceptronPerLayerCount[0]; i < length; ++i) {
						RaycastsDirections[i] = math.normalizesafe(RaycastsDirections[i]);
					}
					serializedData.RaycastDirections = new NativeArray<float3>(PerceptronPerLayerCount[0], Allocator.Persistent);
					serializedData.RaycastDirections.CopyFrom(RaycastsDirections);
				}
				if (serializedData.PerceptronPerLayerCount.IsCreated)
					serializedData.PerceptronPerLayerCount.Dispose();
				serializedData.PerceptronPerLayerCount = new NativeArray<int>(PerceptronPerLayerCount.Length, Allocator.Persistent);
				serializedData.PerceptronPerLayerCount.CopyFrom(PerceptronPerLayerCount);
			}

			protected override void OnDisable() {
				if (Value.RaycastDirections.IsCreated)
					Value.RaycastDirections.Dispose();
				if (Value.PerceptronPerLayerCount.IsCreated)
					Value.PerceptronPerLayerCount.Dispose();
				if (Value.Results.IsCreated)
					Value.Results.Dispose();
				if (Value.Inputs.IsCreated)
					Value.Inputs.Dispose();
				if (Value.Connections.IsCreated)
					Value.Connections.Dispose();
			}
		}
	}
}
