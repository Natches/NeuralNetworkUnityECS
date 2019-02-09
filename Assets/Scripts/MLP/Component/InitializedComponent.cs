using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace NeuralNetwork {

		[Serializable]
		public struct InitializedComponentData : IComponentData { }

		[DisallowMultipleComponent]
		public class InitializedComponent : ComponentDataWrapper<InitializedComponentData> { }
	}
}