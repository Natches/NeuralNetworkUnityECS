using System;
using Unity.Entities;
using UnityEngine;

namespace ECS {
	namespace Spawner {

		[Serializable]
		public struct ParentSpawnerData : ISharedComponentData {
			public Entity Parent;
		}

		public class ParentSpawnerComponent : SharedComponentDataWrapper<SpawnerData> { }
	}
}