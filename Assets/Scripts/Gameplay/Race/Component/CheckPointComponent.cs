using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace Race {

		[Serializable]
		public struct CheckPointComponentData : IComponentData { }

		[DisallowMultipleComponent]
		public class CheckPointComponent : ComponentDataWrapper<CheckPointComponentData> { }

	}
}