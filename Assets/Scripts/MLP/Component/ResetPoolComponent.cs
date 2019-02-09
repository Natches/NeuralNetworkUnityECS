using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace NeuralNetwork {

		[Serializable]
		public struct ResetPoolFromPresetComponentData : IComponentData { }

		[DisallowMultipleComponent]
		public class ResetPoolFromPresetComponent : ComponentDataWrapper<ResetPoolFromPresetComponentData> { }
	}
}
