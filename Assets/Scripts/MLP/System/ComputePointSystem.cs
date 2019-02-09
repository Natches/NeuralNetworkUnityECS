using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine.Jobs;
using ECS.Race;
using ECS.Movement;

namespace ECS {
	namespace NeuralNetwork {

		[UpdateAfter(typeof(UpdatePositionFromVelocitySystem))]
		public class ComputePointSystem : JobComponentSystem {

			[BurstCompile]
			struct ComputePointJob : IJobParallelFor {

				[ReadOnly] public ComponentDataArray<Position> Positions;
				[ReadOnly] public ComponentDataArray<BonusMalusComponentData> BonusMalus;
				[ReadOnly] public ComponentDataArray<IDComponentData> NextCheckPointList;
				[ReadOnly] public NativeHashMap<int, float> CheckpointToPointValue;
				[ReadOnly] public NativeHashMap<int, float3> CollidersPosition;

				[NativeDisableParallelForRestriction] public ComponentDataArray<PointComponentData> Points;

				public void Execute(int index) {
					var checkPointToReach = NextCheckPointList[index].ID;
					var checkpointValue = CheckpointToPointValue[checkPointToReach];
					var bonusMalus = BonusMalus[index];
					var closestPointOnCheckPoint = CollidersPosition[checkPointToReach];
					var point = math.clamp(checkpointValue /
						(math.distance(Positions[index].Value, closestPointOnCheckPoint) * 0.10f) +
						bonusMalus.Bonus - bonusMalus.Malus,
						-bonusMalus.Malus,
						bonusMalus.Bonus + checkpointValue);
					var pointComponent = Points[index];
					pointComponent.PointValue = point;
					Points[index] = pointComponent;
				}
			}

			ComponentGroup neuralNetworkComponentGroup;
			ComponentGroup checkPointComponentGroup;

			protected override void OnCreateManager() {
				neuralNetworkComponentGroup = GetComponentGroup(ComponentType.ReadOnly<NeuralNetworkComponentData>(),
					ComponentType.ReadOnly<BonusMalusComponentData>(),
					ComponentType.ReadOnly<IDComponentData>(),
					ComponentType.ReadOnly<Position>(),
					ComponentType.Create<PointComponentData>(),
					ComponentType.Subtractive<ResetComponentData>());
				checkPointComponentGroup = GetComponentGroup(ComponentType.ReadOnly<CheckPointComponentData>(),
					ComponentType.ReadOnly<IDComponentData>(),
					ComponentType.ReadOnly<Position>(),
					ComponentType.ReadOnly<PointComponentData>(),
					ComponentType.ReadOnly<BoxCollider>());
			}

			private Dictionary<int, BoxCollider> idToCollider;
			private NativeHashMap<int, float> idToPointValue;
			private NativeHashMap<int, float3> idToPosition;

			protected override JobHandle OnUpdate(JobHandle inputDeps) {
				if (neuralNetworkComponentGroup.GetEntityArray().Length != 0) {
					BuildIDToCollider(out idToCollider);
					BuildIDToPointValue();
					BuildIDToPosition();
					var positions = neuralNetworkComponentGroup.GetComponentDataArray<Position>();
					var nextCheckPoints = neuralNetworkComponentGroup.GetComponentDataArray<IDComponentData>();

					var job = new ComputePointJob {
						BonusMalus = neuralNetworkComponentGroup.GetComponentDataArray<BonusMalusComponentData>(),
						CollidersPosition = idToPosition,
						Points = neuralNetworkComponentGroup.GetComponentDataArray<PointComponentData>(),
						NextCheckPointList = nextCheckPoints,
						Positions = positions,
						CheckpointToPointValue = idToPointValue
					};
					Utils.Utils.ScheduleBatchedJobAndComplete(job.Schedule(positions.Length, 32, inputDeps));
				}
				return inputDeps;
			}

			void BuildIDToCollider(out Dictionary<int, BoxCollider> idToColliderDict) {
				if (idToCollider != null) {
					idToColliderDict = idToCollider;
					return;
				}
				idToColliderDict = new Dictionary<int, BoxCollider>(checkPointComponentGroup.GetEntityArray().Length);
				var boxColliders = checkPointComponentGroup.GetComponentArray<BoxCollider>();
				var ids = checkPointComponentGroup.GetComponentDataArray<IDComponentData>();
				for (int i = 0, length = ids.Length; i < length; ++i)
					idToColliderDict[ids[i].ID] = boxColliders[i];

				idToCollider = idToColliderDict;
			}

			void BuildIDToPointValue() {
				if (idToPointValue.IsCreated && idToPointValue.Length != 0)
					return;
				var ids = checkPointComponentGroup.GetComponentDataArray<IDComponentData>();
				var pointValues = checkPointComponentGroup.GetComponentDataArray<PointComponentData>();
				Utils.Utils.ExpandOrCreateHashMap(ref idToPointValue, ids.Length, Allocator.Persistent);
				for (int i = 0, length = ids.Length; i < length; ++i) {
					idToPointValue.TryAdd(ids[i].ID, pointValues[i].PointValue);
				}
			}

			void BuildIDToPosition() {
				if (idToPosition.IsCreated && idToPosition.Length != 0)
					return;
				var ids = checkPointComponentGroup.GetComponentDataArray<IDComponentData>();
				var positions = checkPointComponentGroup.GetComponentDataArray<Position>();
				Utils.Utils.ExpandOrCreateHashMap(ref idToPosition, ids.Length, Allocator.Persistent);
				for (int i = 0, length = ids.Length; i < length; ++i) {
					idToPosition.TryAdd(ids[i].ID, positions[i].Value);
				}
			}

			protected override void OnDestroyManager() {
				if (idToPointValue.IsCreated)
					idToPointValue.Dispose();
				if (idToPosition.IsCreated)
					idToPosition.Dispose();
			}
		}
	}
}
