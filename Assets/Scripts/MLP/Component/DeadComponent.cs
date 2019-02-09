using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace NeuralNetwork {

		[Serializable]
		public struct DeadComponentData : IComponentData { }

		[DisallowMultipleComponent]
		public class DeadComponent : ComponentDataWrapper<DeadComponentData> { }
	}
}