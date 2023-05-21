using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GLHF
{
    public class Simulation
    {
        public Snapshot Snapshot { get; private set; }

        public int Tick
        {
            get => Snapshot.Tick;
            set => Snapshot.Tick = value;
        }

        public float Time => Tick * DeltaTime;

        public float DeltaTime { get; private set; }
        public Scene Scene { get; private set; }
        public int LocalPlayerIndex { get; private set; }

        private readonly GameObjectWorld gameObjectWorld;

        public class Inputs
        {
            public int PlayerJoinEvents;
            public StateInput[] StateInputs;
        }

        public Simulation(Snapshot snapshot, float deltaTime, Scene scene, GameObjectWorld world, int localPlayerIndex)
        {
            Snapshot = snapshot;
            DeltaTime = deltaTime;
            gameObjectWorld = world;
            Scene = scene;
            LocalPlayerIndex = localPlayerIndex;
        }

        private Inputs currentInputs;

        internal void Integrate(Inputs inputs, out long checksum)
        {
            currentInputs = inputs;

            TickEvents();

            TickUpdate();

            currentInputs = null;

            checksum = Snapshot.Allocator.Checksum();

            Tick++;
        }

        internal void Render()
        {
            foreach (var so in gameObjectWorld.StateObjects)
            {
                so.Render();
            }
        }

        internal void SetState(Allocator source)
        {
            Snapshot.Allocator.CopyFrom(source);

            RebuildGameObjectWorld();
        }

        internal void RebuildGameObjectWorld()
        {
            gameObjectWorld.BuildFromSnapshot(Snapshot);

            foreach (var so in gameObjectWorld.StateObjects)
            {
                so.SetSimulation(this);
            }

            foreach (var so in gameObjectWorld.StateObjects)
            {
                so.RenderStart();
            }
        }

        // TODO: Right now, the only event is new players joining. Extend to
        // allow for generic gameplay events.
        private void TickEvents()
        {
            if (currentInputs.PlayerJoinEvents > 0)
            {
                // TODO: Will need to loop through a deterministic ordering of the scene objects.
                var joineds = Scene.FindObjectsOfType<IPlayerJoined>();

                for (int i = 0; i < currentInputs.PlayerJoinEvents; i++)
                {
                    foreach (var joined in joineds)
                    {
                        joined.PlayerJoined();
                    }
                }
            }
        }

        private void TickUpdate()
        {
            // Gather the current state objects into an iterator, as they can
            // be added and removed during tick callbacks.
            var stateObjectIterator = gameObjectWorld.StateObjectIterator();

            if (Tick == 0)
            {
                foreach (var so in stateObjectIterator)
                {
                    if (so == null || !so.Spawned)
                        continue;

                    if (so.IsSceneObject)
                    {
                        so.TickStart();
                    }
                }
            }

            foreach (var so in stateObjectIterator)
            {
                if (so == null || !so.Spawned)
                    continue;

                so.TickUpdate();
            }
        }

        public T Spawn<T>(T prefab, Vector3 position) where T : TickBehaviour
        {
            var spawned = gameObjectWorld.Spawn(prefab);

            spawned.Object.SetSimulation(this);

            if (spawned.TryGetComponent<StateTransform>(out var t))
            {
                t.Position = position;
            }

            spawned.Object.TickStart();
            spawned.Object.RenderStart();

            return spawned;
        }

        public void Despawn(StateObject so)
        {
            so.TickDestroy();

            gameObjectWorld.Despawn(so);
        }

        public StateInput GetInput(int playerIndex)
        {
            if (playerIndex < currentInputs.StateInputs.Length)
                return currentInputs.StateInputs[playerIndex];
            else
                return default;
        }

        public T FindComponentOnStateObject<T>() where T : Component
        {
            return gameObjectWorld.FindComponentOnStateObject<T>();
        }

        public List<T> FindComponentsOnStateObjects<T>() where T : Component
        {
            return gameObjectWorld.FindComponentsOnStateObjects<T>();
        }
    }
}
