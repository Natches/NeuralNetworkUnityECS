using Unity.Entities;
using System.Collections.Generic;

namespace ECS {
	namespace NeuralNetwork {

		public class NeuralNetworkCleanerSystem : ComponentSystem {
			protected override void OnDestroyManager() {
				var neuralNetworks = new List<NeuralNetworkComponentData>(20);
				EntityManager.GetAllUniqueSharedComponentData(neuralNetworks);
				for (int i = 0, length = neuralNetworks.Count; i < length; ++i) {
					var neuralNetwork = neuralNetworks[i];
					if (neuralNetwork.Results.IsCreated)
						neuralNetwork.Results.Dispose();
					if (neuralNetwork.Inputs.IsCreated)
						neuralNetwork.Inputs.Dispose();
					if (neuralNetwork.Connections.IsCreated)
						neuralNetwork.Connections.Dispose();
				}
			}

			protected override void OnUpdate() {
				return;
			}
		}
	}
}
