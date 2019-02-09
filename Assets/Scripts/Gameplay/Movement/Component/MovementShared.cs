using System;

using Unity.Entities;
using UnityEngine;


namespace ECS {
	namespace Movement {

		[Serializable]
		public struct MovementSharedData : ISharedComponentData {
			public float SlowingDistance;
			public float MinDistToStop;
			public float PosOffsetY;
			public float BoostDecreaseRate;
		}

		public class MovementShared : SharedComponentDataWrapper<MovementSharedData> { }
	}
}