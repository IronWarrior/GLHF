using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GLHF
{
    public unsafe class GameObjectWorld
    {
        public LinkedList<StateObject> StateObjects { get; private set; } = new LinkedList<StateObject>();

        private readonly Dictionary<int, StateObject> prefabTable;
        private readonly Scene scene;

        private Snapshot snapshot;

        public GameObjectWorld(Snapshot snapshot, Scene scene, Dictionary<int, StateObject> prefabTable)
        {
            this.prefabTable = prefabTable;
            this.scene = scene;
            this.snapshot = snapshot;         
        }

        public void BuildFromStateObjects(IEnumerable<StateObject> stateObjects)
        {
            foreach (var so in stateObjects)
            {
                Allocate(so);

                StateObjects.AddLast(so);
            }
        }

        /// <summary>
        /// Rebuilds the game object and monobehaviour representing of the world in the
        /// current snapshot.
        /// </summary>
        public void BuildFromSnapshot(Snapshot snapshot)
        {
            this.snapshot = snapshot;

            foreach (var so in StateObjects)
            {
                if (!so.IsSceneObject)
                    Destroy(so);
            }

            StateObjects.Clear();

            var scenePrefabTable = RetrieveSceneObjects();

            Allocator.Block* current = null;
            
            while (snapshot.NextStateObject(current, out var next, out byte* ptr, out int prefabId))
            {
                if (prefabId >= 0)
                {
                    Instantiate(prefabId, ptr);
                }
                else
                {
                    var sceneObject = scenePrefabTable[prefabId];
                    sceneObject.SetPointer(ptr);

                    StateObjects.AddLast(sceneObject);
                }

                current = next;
            }
        }

        public T Spawn<T>(T prefab) where T : TickBehaviour
        {
            var spawned = Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(spawned.gameObject, scene);

            Allocate(spawned.Object);

            StateObjects.AddLast(spawned.Object);

            return spawned;
        }

        public void Despawn(StateObject so)
        {
            snapshot.Allocator.Release(so.Ptr);

            StateObjects.Remove(so);
            Destroy(so);
        }

        public IEnumerable<StateObject> StateObjectIterator()
        {
            var stateObjects = new List<StateObject>(StateObjects);

            return stateObjects;
        }

        private Dictionary<int, StateObject> RetrieveSceneObjects()
        {
            var results = new Dictionary<int, StateObject>();

            GameObject[] roots = scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                if (root.activeSelf != true)
                    continue;

                foreach (var so in root.GetComponentsInChildren<StateObject>(true))
                {
                    if (so.IsSceneObject)
                    {
                        results.Add(so.PrefabId, so);
                    }
                }
            }

            return results;
        }

        private StateObject Instantiate(int id, byte* ptr)
        {
            var prefab = prefabTable[id];
            var spawned = Object.Instantiate(prefab);

            SceneManager.MoveGameObjectToScene(spawned.gameObject, scene);

            spawned.SetPointer(ptr);

            StateObjects.AddLast(spawned);

            return spawned;
        }

        private void Destroy(StateObject so)
        {
            so.ClearPointer();
            Object.Destroy(so.gameObject);
        }

        private void Allocate(StateObject so)
        {
            var ptr = snapshot.Allocator.Allocate(so.Size);
            so.SetPointer(ptr);
        }
    }
}
