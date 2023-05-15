using GLHF.Network;
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

        private GameObjectWorld gameObjectWorld;

        public Snapshot snapshot;
        public event Action<Snapshot> OnSimulateTick;

        private List<StateInput> currentInputs = new List<StateInput>();
        private int playerJoinEvents = 0;

        #region Server
        private List<ClientInputBuffer> clientInputBuffers;
        #endregion

        // TODO: Things that are client or server specific should be encapsulated.
        #region Client
        public event Action<Snapshot> OnDesync;

        private NetworkSimulation clientSimulation;
        private OrderedMessageBuffer<ServerInputMessage> unconsumedServerStates;

        private Rollback rollback;
        private int forwardTick = -1;
        #endregion

        private Transporter transporter;
        private Config config;

        #region Connection Methods
        public void Host(int port, Config config, Transporter transporter)
        {
            this.config = config;

            DeltaTime = 1f / config.TickRate;
            Role = RunnerRole.Host;
            DontDestroyOnLoad(gameObject);

            this.transporter = transporter;

            transporter.OnReceive += Transport_OnReceive;
            transporter.OnPeerConnected += Transport_OnPeerConnected;
            transporter.Listen(port);

            clientInputBuffers = new List<ClientInputBuffer>();

            currentInputs.Add(default);
            PlayerCount++;

            playerJoinEvents++;

            Connected = true;

            this.transporter = transporter;
        }

        public void Join(int port, string ip, Config config, Transporter transporter)
        {
            this.config = config;

            DeltaTime = 1f / config.TickRate;

            Role = RunnerRole.Client;
            DontDestroyOnLoad(gameObject);

            transporter.OnReceive += Transport_OnReceive;
            transporter.OnPeerConnected += Transport_OnPeerConnected;
            transporter.OnPeerDisconnected += Transport_OnPeerDisconnected;
            transporter.Connect(ip, port);

            unconsumedServerStates = new OrderedMessageBuffer<ServerInputMessage>();
            clientSimulation = new NetworkSimulation(DeltaTime, config.JitterTimescale);

            this.transporter = transporter;
        }

        public void Shutdown()
        {
            transporter.Shutdown();

            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(Scene);

            Running = false;
        }
        #endregion

        private void Transport_OnPeerConnected(int peerId)
        {
            if (Role == RunnerRole.Host)
            {
                clientInputBuffers.Add(new ClientInputBuffer(Running ? Tick : 0));
                currentInputs.Add(default);
                PlayerCount++;

                playerJoinEvents++;

                if (Running)
                {
                    // TODO: Make a message class for this.
                    ByteBuffer buffer = new ByteBuffer();
                    buffer.Put((byte)MessageType.Start);
                    buffer.Put(0);
                    buffer.Put(PlayerCount);
                    buffer.Put(true);
                    buffer.Put(Tick);

                    byte[] data = snapshot.Allocator.ToByteArray(true);

                    buffer.Put(data.Length);
                    buffer.Put(data);

                    transporter.Send(peerId, buffer.Data, DeliveryMethod.Reliable);
                }
            }

            if (Role == RunnerRole.Client)
                Connected = true;
        }

        private void Transport_OnPeerDisconnected(int obj)
        {
            Shutdown();
        }

        private void Transport_OnReceive(int id, float rtt, byte[] data)
        {
            ByteBuffer buffer = new ByteBuffer(data);
            MessageType msgType = (MessageType)buffer.Get<byte>();

            if (Role == RunnerRole.Host)
            {
                Debug.Assert(msgType == MessageType.Input);

                ClientInputMessage message = new ClientInputMessage(buffer);

                clientInputBuffers[id].Insert(message, Tick, DeltaTime - deltaTimeAccumulated, DeltaTime);
            }
            else
            {
                if (msgType == MessageType.Start)
                {
                    int sceneIndex = buffer.Get<int>();
                    PlayerCount = buffer.Get<int>();

                    bool hasState = buffer.Get<bool>();

                    Action OnComplete = null;

                    if (hasState)
                    {
                        int tick = buffer.Get<int>();
                        int dataLength = buffer.Get<int>();
                        byte[] state = buffer.Get<byte>(dataLength);

                        OnComplete = () =>
                        {
                            Tick = tick;
                            forwardTick = tick;
                            clientSimulation.SetConfirmedTime(Tick * DeltaTime);
                            snapshot.Allocator.CopyFrom(state);
                            gameObjectWorld.BuildFromSnapshot(snapshot, Scene);
                        };
                    }

                    LoadSceneAndStartGame(sceneIndex, OnComplete);
                }
                else if (msgType == MessageType.Input)
                {
                    ServerInputMessage message = new ServerInputMessage(buffer);
                    unconsumedServerStates.Insert(message);
                }
            }
        }

        #region Game and Scene Managment
        public void StartGame()
        {
            Debug.Assert(Role == RunnerRole.Host, "Clients are not permitted to initiate game start.");

            ByteBuffer buffer = new ByteBuffer();
            buffer.Put((byte)MessageType.Start);
            buffer.Put(0);
            buffer.Put(PlayerCount);

            transporter.SendToAll(buffer.Data, DeliveryMethod.ReliableOrdered);

            playerJoinEvents = PlayerCount;

            LoadSceneAndStartGame(0, null);
        }

        private void LoadSceneAndStartGame(int index, Action OnComplete)
        {
            StartCoroutine(LoadSceneAndStartGameRoutine(index, true, OnComplete));
        }

        private IEnumerator LoadSceneAndStartGameRoutine(int buildIndex, bool additive = false, Action OnComplete = null)
        {
            var async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(buildIndex, additive ? UnityEngine.SceneManagement.LoadSceneMode.Additive : UnityEngine.SceneManagement.LoadSceneMode.Single);
            int index = UnityEngine.SceneManagement.SceneManager.sceneCount - 1;
            Scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(index);

            yield return new WaitUntil(() => async.isDone);

            StartRunning();

            OnComplete?.Invoke();
        }
        #endregion

        private void Update()
        {
            // Check for incoming messages, firing any applicable events.
            transporter.Poll();

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

                        // currentInputs[0] = polledInput;

                        for (int i = 0; i < clientInputBuffers.Count; i++)
                        {
                            if (clientInputBuffers[i].TryPop(out ClientInputMessage inputMessage))
                            {
                                currentInputs[i + 1] = inputMessage.Input;
                            }
                        }

                        TickEvents();

                        TickUpdate();

                        OnSimulateTick?.Invoke(snapshot);

                        long checksum = snapshot.Allocator.Checksum();

                        for (int i = 0; i < clientInputBuffers.Count; i++)
                        {
                            ByteBuffer byteBuffer = new ByteBuffer();
                            ServerInputMessage serverInputMessage = new ServerInputMessage(currentInputs, Tick, checksum, playerJoinEvents, clientInputBuffers[i].Error);
                            serverInputMessage.Write(byteBuffer);

                            transporter.Send(i, byteBuffer.Data, DeliveryMethod.Reliable);
                        }

                        playerJoinEvents = 0;

                        Tick++;
                    }
                }
                else
                {
                    int nextTickToEnterBuffer = clientSimulation.NextTickToEnter(Tick);

                    while (unconsumedServerStates.TryDequeue(nextTickToEnterBuffer, out ServerInputMessage message))
                    {
                        clientSimulation.Insert(message, UnityEngine.Time.time);

                        nextTickToEnterBuffer++;
                    }

                    clientSimulation.Integrate(UnityEngine.Time.time, UnityEngine.Time.deltaTime);

                    while (clientSimulation.TryPop(Tick, UnityEngine.Time.time, out ServerInputMessage serverInputMessage))
                    {
                        // snapshot = rollback.Confirmed;

                        Debug.Assert(serverInputMessage.Tick == Tick, $"Attempting to use inputs from server tick {serverInputMessage.Tick} while client is on tick {Tick}.");

                        currentInputs = serverInputMessage.Inputs;

                        playerJoinEvents = serverInputMessage.NewPlayersJoining;

                        TickEvents();

                        TickUpdate();

                        gameObjectWorld.BuildFromSnapshot(snapshot, Scene);

                        foreach (var so in gameObjectWorld.StateObjects)
                        {
                            so.SetRunner(this);
                        }

                        foreach (var so in gameObjectWorld.StateObjects)
                        {
                            so.RenderStart();
                        }

                        long checksum = snapshot.Allocator.Checksum();

                        if (checksum != serverInputMessage.Checksum)
                        {
                            OnDesync?.Invoke(snapshot);

                            Debug.LogError($"Checksums not equal.");
                        }

                        Tick++;

                        if (PollInput)
                        {
                            int targetForwardTick = Tick + clientSimulation.GetPredictedTickCount();

                            //rollback.CopyToPredicted();
                            //snapshot = rollback.Predicted;

                            //gameObjectWorld.BuildFromSnapshot(snapshot, Scene);
                            //RebuildGameObjectWorld();

                            while (forwardTick <= targetForwardTick)
                            {
                                ByteBuffer byteBuffer = new ByteBuffer();
                                ClientInputMessage clientInputMessage = new ClientInputMessage(polledInput, forwardTick);
                                clientInputMessage.Write(byteBuffer);

                                transporter.SendToAll(byteBuffer.Data, DeliveryMethod.Reliable);

                                forwardTick++;
                            }
                        }
                    }
                }

                foreach (var so in gameObjectWorld.StateObjects)
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

            gameObjectWorld = new GameObjectWorld(snapshot, config.PrefabTable);
            gameObjectWorld.BuildFromStateObjects(FindObjectsOfType<StateObject>());

            if (Role == RunnerRole.Client)
            {
                rollback = new Rollback(snapshot);
            }

            foreach (var so in gameObjectWorld.StateObjects)
            {
                so.SetRunner(this);
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
            if (playerJoinEvents > 0)
            {
                var joineds = FindObjectsOfType<IPlayerJoined>();

                for (int i = 0; i < playerJoinEvents; i++)
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

        #region Simulation Public Methods
        public T Spawn<T>(T prefab, Vector3 position) where T : TickBehaviour
        {
            var spawned = gameObjectWorld.Spawn(prefab, Scene);

            spawned.Object.SetRunner(this);

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

        public StateInput GetInput(int index)
        {
            if (index < currentInputs.Count)
                return currentInputs[index];
            else
                return default;
        }

        public void SetState(Allocator source)
        {
            snapshot.Allocator.CopyFrom(source);

            RebuildGameObjectWorld();
        }

        private void RebuildGameObjectWorld()
        {
            gameObjectWorld.BuildFromSnapshot(snapshot, Scene);

            foreach (var so in gameObjectWorld.StateObjects)
            {
                so.SetRunner(this);
            }

            foreach (var so in gameObjectWorld.StateObjects)
            {
                so.RenderStart();
            }
        }

        public new T FindObjectOfType<T>() where T : class
        {
            GameObject[] roots = Scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                if (root.activeSelf != true)
                    continue;

                var result = root.GetComponentInChildren<T>();

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public new T[] FindObjectsOfType<T>() where T : class
        {
            List<T> results = new List<T>();

            GameObject[] roots = Scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                if (root.activeSelf != true)
                    continue;

                results.AddRange(root.GetComponentsInChildren<T>());
            }

            return results.ToArray();
        }
        #endregion

        #region Debug
        public int MessageBufferCount()
        {
            return 0;

            Debug.Assert(Role == RunnerRole.Client);

            //return pendingServerStates.Size;
        }

        public float MessageBufferDelay()
        {
            return 0;

            Debug.Assert(Role == RunnerRole.Client);

            //return pendingServerStates.CurrentDelay(DeltaTime, UnityEngine.Time.time, playbackTime);
        }

        public float TargetBufferDelay()
        {
            return 0;

            Debug.Assert(Role == RunnerRole.Client);

            //return pendingServerStates.TargetDelay();
        }

        public float CurrentBufferError()
        {
            return 0;

            Debug.Assert(Role == RunnerRole.Client);

            //return pendingServerStates.CalculateError(DeltaTime, UnityEngine.Time.time, playbackTime);
        }

        public int NextTick()
        {
            Debug.Assert(Role == RunnerRole.Client);

            return 0;

            //return pendingServerStates.OldestTick;
        }

        public float Ping()
        {
            Debug.Assert(Role == RunnerRole.Client);

            return 0;
        }

        public float Timescale()
        {
            return 0;

            //return config.JitterTimescale.CalculateTimescale(pendingServerStates.CalculateError(DeltaTime, UnityEngine.Time.time, playbackTime));
        }

        public int ClientInputBufferCount()
        {
            Debug.Assert(Role == RunnerRole.Host);

            return clientInputBuffers[0].Size;
        }
        #endregion
    }
}
