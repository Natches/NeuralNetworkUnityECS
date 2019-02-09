using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace Utils {

		[Serializable]
		public struct SharedComponentIndexToAddComponentData : ISharedComponentData {
			public int Index;
		}

		public class SharedComponentIndexToAddComponent : SharedComponentDataWrapper<SharedComponentIndexToAddComponentData> { }
	}
}
