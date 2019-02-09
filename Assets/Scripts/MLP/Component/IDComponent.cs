using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace NeuralNetwork {

		[Serializable]
		public struct IDComponentData : IComponentData {
			public int ID;
		}

		[DisallowMultipleComponent]
		public class IDComponent : ComponentDataWrapper<IDComponentData> { }
	}
}