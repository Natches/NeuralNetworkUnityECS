using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace NeuralNetwork {
		[Serializable]
		public struct FrozenComponentData : IComponentData { }

		[DisallowMultipleComponent]
		public class FrozenComponent : ComponentDataWrapper<FrozenComponentData> { }
	}
}
