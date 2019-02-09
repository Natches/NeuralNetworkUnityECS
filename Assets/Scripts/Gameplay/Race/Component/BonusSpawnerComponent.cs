using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace Spawner {

		[Serializable]
		public struct BonusSpawnerComponentData : ISharedComponentData {
			[Range(1.0f, 30.0f)]
			public float bonusRespawnDelay;
		}

		public class BonusSpawnerComponent : SharedComponentDataWrapper<BonusSpawnerComponentData> {
			private float DefaultValue = 1.0f;

			protected override void OnEnable() {
				base.OnEnable();
				BonusSpawnerComponentData data = Value;
				data.bonusRespawnDelay = DefaultValue;
				Value = data;
			}

			protected override void OnDisable() {
				base.OnEnable();
				DefaultValue = Value.bonusRespawnDelay;
			}
		}
	}
}