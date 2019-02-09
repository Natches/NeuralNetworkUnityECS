using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using ECS.Timer;

namespace ECS {
	namespace NeuralNetwork {

		[Serializable]
		public struct GeneticTrainerComponentData : IComponentData {
			[Range(0, 1)]
			[Tooltip("In Percent")]
			public float Mutation;
			public int AIPerGen;
			[Tooltip("This is needed to put a Shared Neural Network Component on AI")]
			public int NeuralNetworkIndex;
			public int TrainerID;
			internal uint Generation;
			internal float maxPoint;
		}

		[DisallowMultipleComponent]
		public class GeneticTrainerComponent : ComponentDataWrapper<GeneticTrainerComponentData> {
			[SerializeField]
			internal string SaveFile = "NeuralNetwork.save";
			[SerializeField]
			internal bool SaveBest = false;
			[SerializeField]
			internal bool LoadFromFile = false;
			[SerializeField]
			internal UnityEngine.UI.Text generationCount;
			[SerializeField]
			internal GameObject unInitialiazedPreset;
			internal SortedList<float, Entity> scores;
			internal ValueTuple<float, NativeArray<float>>[] bestOverall;
			internal GameObject preset;
			internal NativeArray<Entity> pool;

			protected override void OnEnable() {
				GameObjectEntity gameObjectEntity = gameObject.GetComponent<GameObjectEntity>();
				EntityManager manager = gameObjectEntity.EntityManager;
				if (gameObjectEntity != null && manager != null) {
					manager.AddComponent(gameObjectEntity.Entity, ComponentType.Create<GeneticTrainerComponent>());
					var self = manager.GetComponentObject<GeneticTrainerComponent>(gameObjectEntity.Entity);
					self = this;
				}
				bestOverall = new ValueTuple<float, NativeArray<float>>[2];
				pool = new NativeArray<Entity>(Value.AIPerGen, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
				scores = new SortedList<float, Entity>(Value.AIPerGen, new Utils.DescendingComparer<float>());
				var temp = Value;
				temp.Generation = 0;
				Value = temp;
				generationCount.text = "0";

				FindObjectOfType<RaceMgr>().OnRaceInitialized += OnStartRace;
			}

			private void OnElapsed() {
				GameObjectEntity gameObjectEntity = gameObject.GetComponent<GameObjectEntity>();
				EntityManager manager = gameObjectEntity.EntityManager;
				for (int i = 0, length = pool.Length; i < length; ++i) {
					var entity = pool[i];
					if (!manager.HasComponent<DeadComponentData>(entity))
						manager.AddComponentData(entity, new DeadComponentData());
				}
			}

			private void OnStartRace() {
				GameObjectEntity gameObjectEntity = gameObject.GetComponent<GameObjectEntity>();
				EntityManager manager = gameObjectEntity.EntityManager;
				for (int i = 0, length = pool.Length; i < length; ++i) {
					var entity = pool[i];
					manager.RemoveComponent<FrozenComponentData>(entity);
					var timerData = manager.GetComponentData<TimerData>(entity);
					timerData.Time = 0.0f;
					manager.SetComponentData(entity, timerData);
				}
				Timer.Timer timer = GetComponent<Timer.Timer>();
				timer.onElapsed += OnElapsed;
				var data = timer.Value;
				data.Time = 0;
				data.Stopped = 0;
				timer.Value = data;
				timer.enabled = true;
			}

			internal GameObject InstantiateGameObject(GameObject gameObject) {
				return Instantiate(gameObject);
			}

			protected override void OnDisable() {
				if (pool.IsCreated)
					pool.Dispose();
				if (bestOverall[0].Item2.IsCreated)
					bestOverall[0].Item2.Dispose();
				if (bestOverall[1].Item2.IsCreated)
					bestOverall[1].Item2.Dispose();
			}
		}
	}
}
