using System;
using Unity.Entities;
using UnityEngine;

namespace ECS {
	namespace Spawner {

		[Serializable]
		public struct SpawnerData : ISharedComponentData {
			public GameObject Prefab;
			public int Count;
			public float yOffset;
		}

		public class SpawnerComponent : SharedComponentDataWrapper<SpawnerData> { }
	}
}