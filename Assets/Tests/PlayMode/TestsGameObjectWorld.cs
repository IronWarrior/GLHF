using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor;

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

            var world = new GameObjectWorld(snapshot, prefabTable);

            world.BuildFromStateObjects(sos);

            var positions = new List<Vector3>();

            foreach (var so in sos)
            {
                Vector3 position = Random.insideUnitSphere;

                so.GetComponent<StateTransform>().Position = position;

                positions.Add(position);
            }

            long initialChecksum = allocator.Checksum();

            world.BuildFromSnapshot(snapshot, scene);

            long checksum = allocator.Checksum();

            Assert.That(initialChecksum == checksum, $"Allocator checksums differ before and after rebuild. ({initialChecksum} vs {checksum})");
            Assert.That(world.StateObjects.Count == 10);
            
            int count = 0;

            foreach (var so in world.StateObjects)
            {
                Assert.That(so.GetComponent<StateTransform>().Position == positions[count]);

                count++;
            }
        }
    }
}
