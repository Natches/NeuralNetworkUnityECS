using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace Race {

		[Serializable]
		public struct PickedUpComponentData : IComponentData { }

		[DisallowMultipleComponent]
		public class PickedUpComponent : ComponentDataWrapper<PickedUpComponentData> { }

	}
}