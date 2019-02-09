using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace NeuralNetwork {

		[Serializable]
		public struct PointComponentData : IComponentData {
			public float PointValue;
		}

		[DisallowMultipleComponent]
		public class PointComponent : ComponentDataWrapper<PointComponentData> { }
	}
}
