using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace NeuralNetwork {
		[Serializable]
		public struct GeneticTrainerIDComponentData : ISharedComponentData {
			public int ID;
		}

		public class GeneticTrainerIDComponent : SharedComponentDataWrapper<GeneticTrainerIDComponentData> { }
	}
}