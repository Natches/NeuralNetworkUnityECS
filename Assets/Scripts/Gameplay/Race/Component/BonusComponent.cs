using System;
using UnityEngine;
using Unity.Entities;

namespace ECS {
	namespace Race {

		[Serializable]
		public struct BonusComponentData : ISharedComponentData {
			public float BoostValue;
			public float BoostDuration;
		}

		public class BonusComponent : SharedComponentDataWrapper<BonusComponentData> {
			private float DefaultValueBoost = 2.0f;
			private float DefaultValueDuration = 1.0f;

			protected override void OnEnable() {
				BonusComponentData data = Value;
				data.BoostValue = DefaultValueBoost;
				data.BoostDuration = DefaultValueDuration;
				Value = data;
			}

			protected override void OnDisable() {
				DefaultValueBoost = Value.BoostValue;
				DefaultValueDuration = Value.BoostDuration;
			}
		}
	}
}