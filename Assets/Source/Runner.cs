using GLHF.Transport;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GLHF
{
    /// <summary>
    /// Manages connections between players, the tick based simulation,
    /// and is interacted with by game object behaviours to spawn and despawn objects.
    /// TODO: Should decouple all three of the above into separate modules.
    /// Can probably also split the simulation bits into Host and Client.
    /// </summary>
    public unsafe class Runner : MonoBehaviour
    {
        public bool PollInput { get; set; } = true;

        public int Tick
        {
            get => snapshot.Tick;
            set => snapshot.Tick = value;
        }

        public float DeltaTime { get; private set; }
        public float Time => Tick * DeltaTime;

        public bool Connected { get; private set; }
        public bool Running { get; private set; }

        public int PlayerCount { get; private set; }

        public UnityEngine.SceneManagement.Scene Scene { get; private set; }

        public RunnerRole Role { get; private set; }

        public enum RunnerRole { None, Host, Client }

        public Func<StateInput> OnPollInput;

        private float deltaTimeAccumulated;

        private LinkedList<StateObject> stateObjects;

        public Snapshot snapshot;
        public Allocator confirmedState;

        private List<StateInput> currentInputs = new List<StateInput>();

        private MessageBuffer pendingInputsClientSide;

        private ITransport transport;
        private Config config;

        #region Connection Methods
        public void Host(int port, Config config, ITransport transport = null)
        {
            this.config = config;

            DeltaTime = 1f / config.TickRate;
            Role = RunnerRole.Host;
            DontDestroyOnLoad(gameObject);

            name = "Host";

            if (transport == null)
                transport = new TransportLocal();

            transport.OnReceive += Transport_OnReceive;
            transport.OnPeerConnected += Transport_OnPeerConnected;
            transport.Listen(port);

            this.transport = transport;

            currentInputs.Add(new StateInput());
            PlayerCount++;

            Connected = true;
        }

        public void Join(int port, Config config, Runner runner, ITransport transport = null)
        {
            this.config = config;

            DeltaTime = 1f / config.TickRate;

            Role = RunnerRole.Client;
            DontDestroyOnLoad(gameObject);

            Debug.Assert(runner.Role == RunnerRole.Host);

            name = "Client";

            if (transport == null)
                transport = new TransportLocal();

            transport.OnReceive += Transport_OnReceive;
            transport.OnPeerConnected += Transport_OnPeerConnected;
            transport.Connect("localhost", port);

            pendingInputsClientSide = new MessageBuffer();

            this.transport = transport;
        }
        #endregion

        #region Game and Scene Managment
        public void StartGame()
        {
            Debug.Assert(Role == RunnerRole.Host, "Clients are not permitted to initiate game start.");

            ByteBuffer buffer = new ByteBuffer();
            buffer.Put((byte)MessageType.Start);
            buffer.Put(0);
            buffer.Put(PlayerCount);

            transport.SendToAll(buffer.Data, DeliveryMethod.ReliableOrdered);

            LoadSceneAndStartGame(0);
        }

        private void LoadSceneAndStartGame(int index)
        {
            StartCoroutine(LoadSceneAndStartGameRoutine(index, true));
        }

        private IEnumerator LoadSceneAndStartGameRoutine(int buildIndex, bool additive = false)
        {
            var async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIndex, additive ? UnityEngine.SceneManagement.LoadSceneMode.Additive : UnityEngine.SceneManagement.LoadSceneMode.Single);
            int index = UnityEngine.SceneManagement.SceneManager.sceneCount - 1;
            Scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(index);

            yield return new WaitUntil(() => async.isDone);

            StartRunning();
        }
        #endregion

        #region Simulation Public Methods
        public StateObject Spawn(int id)
        {
            var prefab = config.PrefabTable[id];
            var spawned = Instantiate(prefab);

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(spawned.gameObject, Scene);

            stateObjects.AddFirst(spawned);

            return spawned;
        }

        public T Spawn<T>(T prefab, Vector3 position) where T : TickBehaviour
        {
            var spawned = Instantiate(prefab);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(spawned.gameObject, Scene);

            var ptr = snapshot.Allocator.Allocate(spawned.Object.Size);
            spawned.Object.Initialize(this, ptr);

            stateObjects.AddFirst(spawned.Object);

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
            snapshot.Allocator.Release(so.Ptr);

            stateObjects.Remove(so);
            Destroy(so.gameObject);
        }

        public StateInput GetInput(int index)
        {
            if (index < currentInputs.Count)
                return currentInputs[index];
            else
                return new StateInput();
        }

        public void SetState(Allocator source)
        {
            snapshot.Allocator.CopyFrom(source);

            RebuildWorld();
        }

        public new T FindObjectOfType<T>() where T : Component
        {
            GameObject[] roots = Scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                var result = root.GetComponentInChildren<T>();

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public new T[] FindObjectsOfType<T>() where T : Component
        {
            List<T> results = new List<T>();

            GameObject[] roots = Scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                results.AddRange(root.GetComponentsInChildren<T>());
            }

            return results.ToArray();
        }
        #endregion

        /// <summary>
        /// Rebuilds the game object and monobehaviour representing of the world in the
        /// current snapshot.
        /// </summary>
        private void RebuildWorld()
        {
            LinkedListNode<StateObject> node = stateObjects.First;

            while (node != null)
            {
                var next = node.Next;

                if (!node.Value.IsSceneObject)
                {
                    stateObjects.Remove(node);

                    Destroy(node.Value.gameObject);
                }

                node = next;
            }

            Allocator.Block* current = null;

            while (snapshot.NextStateObject(current, out var next, out byte* ptr, out int prefabId))
            {
                if (prefabId != -1)
                {
                    var spawned = Spawn(prefabId);
                    spawned.Initialize(this, ptr);
                    spawned.RenderStart();
                }

                current = next;
            }
        }

        private void Transport_OnPeerConnected()
        {
            if (Role == RunnerRole.Host)
            {
                currentInputs.Add(new StateInput());
                PlayerCount++;
            }

            if (Role == RunnerRole.Client)
                Connected = true;
        }

        private void Transport_OnReceive(int id, float rtt, byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);
            MessageType msgType = (MessageType)buffer.Get<byte>();

            if (Role == RunnerRole.Host)
            {
                Debug.Assert(msgType == MessageType.Input);

                ClientInputMessage message = new ClientInputMessage(buffer);
                currentInputs[id + 1] = message.Input;
            }
            else
            {
                if (msgType == MessageType.Start)
                {
                    int sceneIndex = buffer.Get<int>();
                    PlayerCount = buffer.Get<int>();

                    LoadSceneAndStartGame(sceneIndex);
                }
                else if (msgType == MessageType.Input)
                {
                    ServerInputMessage message = new ServerInputMessage(buffer);
                    pendingInputsClientSide.Insert(message, rtt);
                }
            }
        }

        private void Update()
        {
            // Check for incoming messages, firing any applicable events.
            transport.Update();

            if (Running)
            {
                StateInput polledInput = default;

                if (PollInput && OnPollInput != null)
                    polledInput = OnPollInput();

                if (Role == RunnerRole.Host)
                {
                    deltaTimeAccumulated += UnityEngine.Time.deltaTime;

                    while (deltaTimeAccumulated > DeltaTime)
                    {
                        deltaTimeAccumulated -= DeltaTime;

                        currentInputs[0] = polledInput;

                        TickUpdate();

                        long checksum = snapshot.Allocator.Checksum();

                        ByteBuffer byteBuffer = new ByteBuffer();
                        ServerInputMessage serverInputMessage = new ServerInputMessage(currentInputs, Tick, checksum);
                        serverInputMessage.Write(byteBuffer);

                        transport.SendToAll(byteBuffer.Data, DeliveryMethod.Reliable);

                        Tick++;
                    }
                }
                else
                {
                    deltaTimeAccumulated += UnityEngine.Time.deltaTime * config.JitterTimescale.CalculateTimescale(DeltaTime, pendingInputsClientSide.CurrentSize, pendingInputsClientSide.RttStandardDeviation);

                    while (deltaTimeAccumulated > DeltaTime && pendingInputsClientSide.TryPop(Tick, out ServerInputMessage networkInput))
                    {
                        deltaTimeAccumulated -= DeltaTime;

                        Debug.Assert(networkInput.Tick == Tick, $"Attempting to use inputs from server tick {networkInput.Tick} while client is on tick {Tick}.");

                        currentInputs = networkInput.Inputs;

                        TickUpdate();

                        long checksum = snapshot.Allocator.Checksum();

                        Debug.Assert(checksum == networkInput.Checksum, "Checksums not equal.");

                        Tick++;

                        if (PollInput)
                        {
                            ByteBuffer byteBuffer = new ByteBuffer();
                            ClientInputMessage clientInputMessage = new ClientInputMessage(polledInput);
                            clientInputMessage.Write(byteBuffer);

                            transport.SendToAll(byteBuffer.Data, DeliveryMethod.ReliableOrdered);
                        }
                    }
                }

                foreach (var so in stateObjects)
                {
                    so.Render();
                }
            }
        }

        private void StartRunning()
        {
            Running = true;

            var allocator = new Allocator(1024);
            snapshot = new Snapshot(allocator);

            stateObjects = new LinkedList<StateObject>(FindObjectsOfType<StateObject>());

            foreach (var stateObject in stateObjects)
            {
                var ptr = snapshot.Allocator.Allocate(stateObject.Size);
                stateObject.Initialize(this, ptr);
            }

            if (Role == RunnerRole.Client)
            {
                confirmedState = new Allocator(snapshot.Allocator);
            }

            foreach (var so in stateObjects)
            {
                so.RenderStart();
            }
        }

        private void TickUpdate()
        {
            if (Tick == 0)
            {
                LinkedListNode<StateObject> startNode = stateObjects.First;

                while (startNode != null)
                {
                    if (startNode.Value.IsSceneObject)
                    {
                        startNode.Value.TickStart();
                    }

                    startNode = startNode.Next;
                }
            }

            LinkedListNode<StateObject> node = stateObjects.First;

            while (node != null)
            {
                node.Value.TickUpdate();

                node = node.Next;
            }
        }

        #region Debug
        public int MessageBufferCount()
        {
            Debug.Assert(Role == RunnerRole.Client);

            return pendingInputsClientSide.CurrentSize;
        }

        public float PingStandardDeviation()
        {
            Debug.Assert(Role == RunnerRole.Client);

            return pendingInputsClientSide.RttStandardDeviation;
        }

        public float TargetMessageBufferSize()
        {
            Debug.Assert(Role == RunnerRole.Client);

            return config.JitterTimescale.TargetBufferSize(DeltaTime, pendingInputsClientSide.RttStandardDeviation);
        }
        #endregion
    }
}
