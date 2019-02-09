using System;
using UnityEngine;
using Unity.Entities;


namespace ECS {
	namespace NeuralNetwork {

		[Serializable]
		public struct NeuralNetworkIDComponentData : IComponentData {
			public int ID;
		}

		[DisallowMultipleComponent]
		public class NeuralNetworkIDComponent : ComponentDataWrapper<NeuralNetworkIDComponentData> { }
	}
}