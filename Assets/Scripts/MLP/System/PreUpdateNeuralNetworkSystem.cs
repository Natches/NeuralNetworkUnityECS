using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine.Jobs;
using ECS.Movement;

namespace ECS {
	namespace NeuralNetwork {

		[UpdateBefore(typeof(UpdateNeuralNetworkSystem))]
		public class PreUpdateNeuralNetworkSystem : JobComponentSystem {

			[BurstCompile]
			struct PreUpdateNeuralNetworkBootstrapRaycast : IJobParallelFor {
				[WriteOnly, NativeDisableParallelForRestriction] public NativeArray<RaycastCommand> RaycastCommands;

				public void Execute(int index) {
					RaycastCommands[index] = new RaycastCommand(float3.zero, float3.zero, 0, 0, 1);
				}
			}

			[BurstCompile]
			struct UpdateNeuralNetworkBootstrapRaycast : IJobParallelFor {
				[ReadOnly] public ComponentDataArray<NeuralNetworkIDComponentData> NeuralNetworkIDs;
				[ReadOnly] public int mask;
				[ReadOnly] public ComponentDataArray<Position> Positions;
				[ReadOnly] public ComponentDataArray<Rotation> Rotations;
				[ReadOnly] public int InputCountPerNetwork;
				[ReadOnly] public NativeArray<float3> Directions;
				[WriteOnly, NativeDisableParallelForRestriction] public NativeArray<RaycastCommand> RaycastCommands;

				public void Execute(int index) {
					int ID = NeuralNetworkIDs[index].ID;
					var position = Positions[index];
					var rotation = Rotations[index];
					ID *= InputCountPerNetwork;
					RaycastCommand raycast = new RaycastCommand(position.Value, float3.zero, 50, mask, 1);
					for (int i = 0; i < InputCountPerNetwork; ++i) {
						raycast.direction = math.rotate(rotation.Value, Directions[i]);
						RaycastCommands[ID++] = raycast;
					}
				}
			}

			[BurstCompile]
			struct UpdateNeuralNetworkBootstrapTargetPosition : IJobParallelFor {
				[ReadOnly] public int InputCountPerNetwork;
				[ReadOnly] public ComponentDataArray<NeuralNetworkIDComponentData> NeuralNetworkIDs;
				[ReadOnly] public ComponentDataArray<MovementData> Movements;
				[WriteOnly, NativeDisableParallelForRestriction] public NativeArray<float> Inputs;

				public void Execute(int index) {
					int ID = NeuralNetworkIDs[index].ID;
					var movement = Movements[index];
					ID *= InputCountPerNetwork;

					Inputs[ID++] = movement.TargetPos.x;
					Inputs[ID++] = movement.TargetPos.y;
					Inputs[ID++] = movement.TargetPos.z;
				}
			}

			[BurstCompile]
			struct UpdateNeuralNetworkBootstrapPosition : IJobParallelFor {
				[ReadOnly] public int InputCountPerNetwork;
				[ReadOnly] public ComponentDataArray<NeuralNetworkIDComponentData> NeuralNetworkIDs;
				[ReadOnly] public ComponentDataArray<Position> Positions;
				[WriteOnly, NativeDisableParallelForRestriction] public NativeArray<float> Inputs;

				public void Execute(int index) {
					int ID = NeuralNetworkIDs[index].ID;
					var position = Positions[index];
					ID *= InputCountPerNetwork;

					Inputs[ID++] = position.Value.x;
					Inputs[ID++] = position.Value.y;
					Inputs[ID++] = position.Value.z;
				}
			}

			ComponentGroup neuralNetworkGroup;

			protected override void OnCreateManager() {
				neuralNetworkGroup = GetComponentGroup(ComponentType.Create<NeuralNetworkComponentData>(),
					ComponentType.ReadOnly<NeuralNetworkIDComponentData>(),
					ComponentType.ReadOnly<InitializedComponentData>(),
					ComponentType.ReadOnly<Position>(),
					ComponentType.ReadOnly<Rotation>(),
					ComponentType.ReadOnly<MovementData>(),
					ComponentType.Subtractive<DeadComponentData>(),
					ComponentType.Subtractive<ResetComponentData>(),
					ComponentType.Subtractive<FrozenComponentData>());
			}

			NativeArray<RaycastCommand> RaycastCommands;
			NativeArray<RaycastHit> Hits;
			NativeArray<float> Obstacles;

			protected override JobHandle OnUpdate(JobHandle inputDeps) {
				int mask = LayerMask.GetMask("Wall");
				var neuralNetworks = new List<NeuralNetworkComponentData>(20);
				EntityManager.GetAllUniqueSharedComponentData(neuralNetworks);
				for (int i = 0, length = neuralNetworks.Count; i < length; ++i) {
					var neuralNetwork = neuralNetworks[i];
					neuralNetworkGroup.SetFilter(neuralNetwork);
					var positions = neuralNetworkGroup.GetComponentDataArray<Position>();
					if (positions.Length != 0) {
						if (neuralNetwork.InputType == E_InputType.RAYCAST) {
							Utils.Utils.ExpandOrCreateArray(ref RaycastCommands, neuralNetwork.Inputs.Length, Allocator.Persistent);

							var preBootstrap = new PreUpdateNeuralNetworkBootstrapRaycast {
								RaycastCommands = RaycastCommands
							};

							Utils.Utils.ScheduleBatchedJobAndComplete(preBootstrap.Schedule(neuralNetwork.Inputs.Length, 32, default));

							var bootstrap = new UpdateNeuralNetworkBootstrapRaycast {
								Directions = neuralNetwork.RaycastDirections,
								InputCountPerNetwork = neuralNetwork.PerceptronPerLayerCount[0],
								Rotations = neuralNetworkGroup.GetComponentDataArray<Rotation>(),
								Positions = positions,
								mask = mask,
								RaycastCommands = RaycastCommands,
								NeuralNetworkIDs = neuralNetworkGroup.GetComponentDataArray<NeuralNetworkIDComponentData>()
							};

							Utils.Utils.ScheduleBatchedJobAndComplete(bootstrap.Schedule(positions.Length, 32, inputDeps));

							Utils.Utils.ExpandOrCreateArray(ref Hits, RaycastCommands.Length, Allocator.Persistent);

							Utils.Utils.ScheduleBatchedJobAndComplete(RaycastCommand.ScheduleBatch(RaycastCommands, Hits, 2, default));

							Utils.Utils.ExpandOrCreateArray(ref Obstacles, RaycastCommands.Length, Allocator.Persistent);

							for (int j = 0, hitCount = RaycastCommands.Length; j < hitCount; ++j) {
								Obstacles[j] = Hits[j].distance;
							}

							neuralNetwork.Inputs.CopyFrom(Obstacles);
						}
						else if (neuralNetwork.InputType == E_InputType.TARGET_POSITION) {
							var bootstrap = new UpdateNeuralNetworkBootstrapTargetPosition {
								InputCountPerNetwork = neuralNetwork.PerceptronPerLayerCount[0],
								Inputs = neuralNetwork.Inputs,
								Movements = neuralNetworkGroup.GetComponentDataArray<MovementData>(),
								NeuralNetworkIDs = neuralNetworkGroup.GetComponentDataArray<NeuralNetworkIDComponentData>()
							};

							Utils.Utils.ScheduleBatchedJobAndComplete(bootstrap.Schedule(neuralNetworkGroup.GetComponentDataArray<NeuralNetworkIDComponentData>().Length, 32, inputDeps));
						}
						else if (neuralNetwork.InputType == E_InputType.POSITION) {
							var bootstrap = new UpdateNeuralNetworkBootstrapPosition {
								InputCountPerNetwork = neuralNetwork.PerceptronPerLayerCount[0],
								Inputs = neuralNetwork.Inputs,
								Positions = neuralNetworkGroup.GetComponentDataArray<Position>(),
								NeuralNetworkIDs = neuralNetworkGroup.GetComponentDataArray<NeuralNetworkIDComponentData>()
							};

							Utils.Utils.ScheduleBatchedJobAndComplete(bootstrap.Schedule(neuralNetworkGroup.GetComponentDataArray<NeuralNetworkIDComponentData>().Length, 32, inputDeps));
						}
					}
				}
				return inputDeps;
			}

			protected override void OnDestroyManager() {
				if (RaycastCommands.IsCreated)
					RaycastCommands.Dispose();
				if (Hits.IsCreated)
					Hits.Dispose();
				if (Obstacles.IsCreated)
					Obstacles.Dispose();
			}
		}
	}
}