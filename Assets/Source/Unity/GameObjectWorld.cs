using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GLHF
{
    public unsafe class GameObjectWorld
    {
        public IReadOnlyCollection<StateObject> StateObjects => stateObjects;

        private readonly LinkedList<StateObject> stateObjects;
        private readonly Dictionary<int, StateObject> prefabTable;

        private Snapshot snapshot;

        public GameObjectWorld(Snapshot snapshot, Dictionary<int, StateObject> prefabTable)
        {
            this.prefabTable = prefabTable;

            stateObjects = new LinkedList<StateObject>();
            
            this.snapshot = snapshot;
        }

        public void BuildFromStateObjects(IEnumerable<StateObject> stateObjects)
        {
            foreach (var so in stateObjects)
            {
                Allocate(so);

                this.stateObjects.AddFirst(so);
            }
        }

        /// <summary>
        /// Rebuilds the game object and monobehaviour representing of the world in the
        /// current snapshot.
        /// </summary>
        public void BuildFromSnapshot(Snapshot snapshot, Scene scene)
        {
            this.snapshot = snapshot;

            LinkedListNode<StateObject> node = stateObjects.First;

            while (node != null)
            {
                var next = node.Next;
                
                if (!node.Value.IsSceneObject)
                {
                    stateObjects.Remove(node);

                    Object.Destroy(node.Value.gameObject);
                }

                node = next;
            }

            Allocator.Block* current = null;
            
            while (snapshot.NextStateObject(current, out var next, out byte* ptr, out int prefabId))
            {
                if (prefabId != -1)
                {
                    Spawn(prefabId, scene);
                }

                current = next;
            }
        }

        public StateObject Spawn(int id, Scene scene)
        {
            var prefab = prefabTable[id];
            var spawned = Object.Instantiate(prefab);
            
            SceneManager.MoveGameObjectToScene(spawned.gameObject, scene);

            stateObjects.AddFirst(spawned);

            return spawned;
        }

        public T Spawn<T>(T prefab, Scene scene) where T : TickBehaviour
        {
            var spawned = Object.Instantiate(prefab);
            SceneManager.MoveGameObjectToScene(spawned.gameObject, scene);

            Allocate(spawned.Object);

            stateObjects.AddFirst(spawned.Object);

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

            stateObjects.Remove(so);
            Object.Destroy(so.gameObject);
        }
    }
}
