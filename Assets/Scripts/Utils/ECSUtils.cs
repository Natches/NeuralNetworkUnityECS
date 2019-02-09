using System;
using System.Reflection;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using ECS.Physics;

namespace ECS {
	namespace Utils {
		public static class Utils {
			public static void AddOrSetComponent<T>(Entity entity, T component, ComponentDataFromEntity<T> cdfe, EntityManager entityManager) where T : struct, IComponentData {
				if (cdfe.Exists(entity)) {
					entityManager.SetComponentData(entity, component);
				}
				else {
					entityManager.AddComponentData(entity, component);
				}
			}

			public static bool HasSphereCollider(ColliderComponentData data) {
				return (data.Volume & E_CollisionVolume.SPHERE) == E_CollisionVolume.SPHERE;
			}

			public static bool HasCapsuleCollider(ColliderComponentData data) {
				return (data.Volume & E_CollisionVolume.CAPSULE) == E_CollisionVolume.CAPSULE;
			}

			public static bool HasBoxCollider(ColliderComponentData data) {
				return (data.Volume & E_CollisionVolume.BOX) == E_CollisionVolume.BOX;
			}

			public static void ScheduleBatchedJobAndComplete(JobHandle handle) {
				JobHandle.ScheduleBatchedJobs();
				handle.Complete();
			}

			public static void ExpandOrCreateArray<T>(ref NativeArray<T> array, int length, Allocator allocator) where T : struct {
				if (array.IsCreated && array.Length != length) {
					array.Dispose();
					array = new NativeArray<T>(length, allocator, NativeArrayOptions.UninitializedMemory);
				}
				else if (!array.IsCreated)
					array = new NativeArray<T>(length, allocator, NativeArrayOptions.UninitializedMemory);
			}

			public static void ExpandOrCreateHashMap<T, U>(ref NativeHashMap<T, U> hashMap, int length, Allocator allocator)
				where T : struct, IEquatable<T>
				where U : struct {
				if (hashMap.IsCreated && hashMap.Length != length) {
					hashMap.Dispose();
					hashMap = new NativeHashMap<T, U>(length, allocator);
				}
				else if (!hashMap.IsCreated)
					hashMap = new NativeHashMap<T, U>(length, allocator);
			}

			public static void ExpandOrCreateMultiHashMap<T, U>(ref NativeMultiHashMap<T, U> hashMap, int length, Allocator allocator)
				where T : struct, IEquatable<T>
				where U : struct {
				if (hashMap.IsCreated && hashMap.Length != length) {
					hashMap.Dispose();
					hashMap = new NativeMultiHashMap<T, U>(length, allocator);
				}
				else if (!hashMap.IsCreated)
					hashMap = new NativeMultiHashMap<T, U>(length, allocator);
			}

			[BurstCompile]
			public static void CopyNativeArrayTo<T>(this NativeArray<T> arraySrc, NativeArray<T> arrayDest, int length) where T : struct {
				if (arrayDest.Length >= length && arraySrc.Length >= length) {
					for (int i = 0; i < length; ++i) {
						arrayDest[i] = arraySrc[i];
					}
				}
			}

			[BurstCompile]
			public static void CopyNativeArrayTo<T>(this NativeArray<T> arraySrc, NativeArray<T> arrayDest, int length, int startIndex) where T : struct {
				if (arrayDest.Length >= length && arraySrc.Length >= length + startIndex) {
					length += startIndex;
					for (int i = startIndex, j = 0; i < length; ++i, ++j) {
						arrayDest[j] = arraySrc[i];
					}
				}
			}
		}
		class DescendingComparer<T> : IComparer<T> {
			public int Compare(T x, T y) {
				return Comparer<T>.Default.Compare(y, x);
			}
		}

		// <copyright>
		//     Copyright (c) Timothy Raines. All rights reserved.
		// </copyright>
		public static class EntityManagerExtensions {
			private static Action<EntityManager, Entity, ComponentType, object> setComponentObject;
			private static bool setup;

			public static void SetComponentObject(this EntityManager entityManager, Entity entity, ComponentType componentType, object componentObject) {
				if (!setup) {
					Setup();
				}

				setComponentObject.Invoke(entityManager, entity, componentType, componentObject);
			}


			private static void Setup() {
				setComponentObject = CreateSetComponentObject();

				setup = true;
			}

			private static Action<EntityManager, Entity, ComponentType, object> CreateSetComponentObject() {
				var addTypeLookupMethodInfo = typeof(EntityManager).GetMethod(
					"SetComponentObject",
					BindingFlags.NonPublic | BindingFlags.Instance);

				if (addTypeLookupMethodInfo == null) {
					throw new NullReferenceException("SetComponentObject changed");
				}

				return (Action<EntityManager, Entity, ComponentType, object>)Delegate.CreateDelegate(typeof(Action<EntityManager, Entity, ComponentType, object>), null, addTypeLookupMethodInfo);
			}
		}
	}
}
