using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace NeuralNetwork {

		[Serializable]
		public struct InitializeNeuralNetworkComponentData : IComponentData { }

		[DisallowMultipleComponent]
		public class InitializeNeuralNetworkComponent : ComponentDataWrapper<InitializeNeuralNetworkComponentData> { }
	}
}
