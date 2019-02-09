using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace NeuralNetwork {

		[Serializable]
		public struct ResetComponentData : IComponentData { }

		[DisallowMultipleComponent]
		public class ResetComponent : ComponentDataWrapper<ResetComponentData> { }
	}
}
