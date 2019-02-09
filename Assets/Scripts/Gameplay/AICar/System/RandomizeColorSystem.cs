using UnityEngine;
using Unity.Entities;
using Unity.Rendering;

public class RandomizeColorSystem : ComponentSystem {
	ComponentGroup group;

	protected override void OnCreateManager() {
		group = GetComponentGroup(ComponentType.ReadOnly<RandomizeColorData>(),
			ComponentType.Create<RenderMesh>());
	}

	protected override void OnUpdate() {
		var defaultComponent = EntityManager.GetSharedComponentData<RenderMesh>(group.GetEntityArray()[0]);
		int length = group.GetEntityArray().Length;
		var entities = group.GetEntityArray().ToArray();
		for (int i = 0; i < length; ++i) {
			var newComponent = new RenderMesh();
			newComponent.layer = defaultComponent.layer;
			newComponent.mesh = defaultComponent.mesh;
			newComponent.material = new Material(defaultComponent.material);
			newComponent.subMesh = defaultComponent.subMesh;
			var entity = entities[i];
			newComponent.material.color = Random.ColorHSV();
			if(newComponent.material.color.r < 0.2f &&
				newComponent.material.color.g < 0.2f &&
				newComponent.material.color.b < 0.2f)
				newComponent.material.color += Random.ColorHSV();
			EntityManager.SetSharedComponentData(entity, newComponent);
			EntityManager.RemoveComponent<RandomizeColorData>(entity);
		}
	}
}
