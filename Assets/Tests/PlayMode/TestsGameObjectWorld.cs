using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor;
using Unity.Mathematics;

namespace GLHF.Tests
{
    public class TestsGameObjectWorld
    {
        [Test]
        public void BuildFromSnapshot_RebuildWorld()
        {
            Scene scene = SceneManager.CreateScene("Test Scene");

            GameObject dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets\\Tests\\PlayMode\\DummyStateObjectPrefab.prefab");
            var stateObjectPrefab = dummyPrefab.GetComponent<StateObject>();

            var sos = new List<StateObject>();            

            for (int i = 0; i < 10; i++)
            {
                var clone = Object.Instantiate(stateObjectPrefab);
                sos.Add(clone);
            }

            var allocator = new Allocator(1024);
            var snapshot = new Snapshot(allocator);
            var prefabTable = new Dictionary<int, StateObject>() { { stateObjectPrefab.BakedPrefabId, stateObjectPrefab } };

            var world = new GameObjectWorld(snapshot, scene, prefabTable);

            world.BuildFromStateObjects(sos);

            var positions = new List<float3>();
            
            foreach (var so in sos)
            {
                float3 position = UnityEngine.Random.insideUnitSphere;

                so.GetComponent<StateTransform>().Position = position;

                positions.Add(position);
            }

            long initialChecksum = allocator.Checksum();

            world.BuildFromSnapshot(snapshot);

            long checksum = allocator.Checksum();

            Assert.That(initialChecksum == checksum, $"Allocator checksums differ before and after rebuild. ({initialChecksum} vs {checksum})");
            Assert.That(world.StateObjects.Count == 10);
            
            int count = 0;

            foreach (var so in world.StateObjects)
            {
                Assert.That(so.GetComponent<StateTransform>().Position.Equals(positions[count]));

                count++;
            }
        }
    }
}
