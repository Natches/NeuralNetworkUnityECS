using System;
using Unity.Entities;
using UnityEngine;

namespace ECS {
	namespace Utils {

		[Serializable]
		public struct SharedDataInitializerComponentData : IComponentData { }

		[DisallowMultipleComponent]
		public class SharedDataInitializerComponent : ComponentDataWrapper<SharedDataInitializerComponentData> { }
	}
}
