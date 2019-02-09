using System;
using Unity.Entities;
using UnityEngine;


namespace ECS {
	namespace Spawner {

		[Serializable]
		public struct SpawnerIDData : IComponentData { }

		[DisallowMultipleComponent]
		public class SpawnerIDComponent : ComponentDataWrapper<SpawnerIDData> { }
	}
}