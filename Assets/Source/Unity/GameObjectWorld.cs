using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GLHF
{
    public unsafe class GameObjectWorld
    {
        public LinkedList<StateObject> StateObjects { get; private set; }

        private readonly Dictionary<int, StateObject> prefabTable;

        private Snapshot snapshot;

        public GameObjectWorld(Snapshot snapshot, Dictionary<int, StateObject> prefabTable)
        {
            this.prefabTable = prefabTable;

            StateObjects = new LinkedList<StateObject>();
            
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
        public void BuildFromSnapshot(Snapshot snapshot, Scene scene)
        {
            this.snapshot = snapshot;

            foreach (var so in StateObjects)
            {
                if (!so.IsSceneObject)
                    Object.Destroy(so.gameObject);
            }

            StateObjects.Clear();

            var scenePrefabTable = RetrieveSceneObjects(scene);

            Allocator.Block* current = null;
            
            while (snapshot.NextStateObject(current, out var next, out byte* ptr, out int prefabId))
            {
                if (prefabId >= 0)
                {
                    Instantiate(prefabId, ptr, scene);
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

        private Dictionary<int, StateObject> RetrieveSceneObjects(Scene scene)
        {
            var results = new Dictionary<int, StateObject>();

            GameObject[] roots = scene.GetRootGameObjects();

            foreach (var root in roots)
            {
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

        public StateObject Instantiate(int id, byte* ptr, Scene scene)
        {
            var prefab = prefabTable[id];
            var spawned = Object.Instantiate(prefab);
            
            SceneManager.MoveGameObjectToScene(spawned.gameObject, scene);

            spawned.SetPointer(ptr);

            StateObjects.AddLast(spawned);

            return spawned;
        }

        public T Spawn<T>(T prefab, Scene scene) where T : TickBehaviour
        {
            var spawned = Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(spawned.gameObject, scene);

            Allocate(spawned.Object);

            StateObjects.AddLast(spawned.Object);

            return spawned;
        }

        private void Allocate(StateObject so)
        {
            var ptr = snapshot.Allocator.Allocate(so.Size);
            so.SetPointer(ptr);
        }

        public void Despawn(StateObject so)
        {
            snapshot.Allocator.Release(so.Ptr);

            StateObjects.Remove(so);
            Object.Destroy(so.gameObject);
        }
    }
}
