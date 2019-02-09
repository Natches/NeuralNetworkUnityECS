using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace NeuralNetwork {
		[Serializable]
		public struct BonusMalusComponentData : IComponentData {
			internal float Bonus;
			internal float Malus;
		}

		[DisallowMultipleComponent]
		public class BonusMalusComponent : ComponentDataWrapper<BonusMalusComponentData> { }
	}
}