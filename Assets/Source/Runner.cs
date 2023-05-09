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
        private int playerJoinEvents = 0;

        #region Server
        private List<ClientInputBuffer> clientInputBuffers;
        #endregion

        // TODO: Things that are client or server specific should be encapsulated.
        #region Client
        private OrderedMessageBuffer<ServerInputMessage> unconsumedServerStates;
        private MessageBuffer<ServerInputMessage> pendingServerStates;

        private float playbackTime;
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
            pendingServerStates = new MessageBuffer<ServerInputMessage>(DeltaTime);

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

                    byte[] data = snapshot.Allocator.ToByteArray();

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

                clientInputBuffers[id].Insert(message);
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
                            playbackTime = Tick * DeltaTime;
                            snapshot.Allocator.CopyFrom(state);
                            RebuildWorld();
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

            ByteBuffer buffer = new ByteBuffer(1024);
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

        #region Simulation Public Methods
        private StateObject Spawn(int id)
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
                return default;
        }

        public void SetState(Allocator source)
        {
            snapshot.Allocator.CopyFrom(source);

            RebuildWorld();
        }

        public new T FindObjectOfType<T>() where T : class
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

        public new T[] FindObjectsOfType<T>() where T : class
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

                        currentInputs[0] = polledInput;

                        for (int i = 0; i < clientInputBuffers.Count; i++)
                        {
                            while (clientInputBuffers[i].TryPop(out ClientInputMessage inputMessage))
                            {
                                currentInputs[i + 1] = inputMessage.Input;
                            }
                        }

                        TickEvents();

                        TickUpdate();

                        long checksum = snapshot.Allocator.Checksum();

                        ByteBuffer byteBuffer = new ByteBuffer();
                        ServerInputMessage serverInputMessage = new ServerInputMessage(currentInputs, Tick, checksum, playerJoinEvents);
                        serverInputMessage.Write(byteBuffer);

                        transporter.SendToAll(byteBuffer.Data, DeliveryMethod.Reliable);

                        playerJoinEvents = 0;

                        Tick++;
                    }
                }
                else
                {
                    int nextTickToEnterBuffer = pendingServerStates.NewestTick != -1 ? pendingServerStates.NewestTick + 1 : Tick;

                    while (unconsumedServerStates.TryDequeue(nextTickToEnterBuffer, out ServerInputMessage message))
                    {
                        pendingServerStates.Insert(message, UnityEngine.Time.time);

                        nextTickToEnterBuffer++;
                    }

                    float error = pendingServerStates.CalculateError(DeltaTime, UnityEngine.Time.time, playbackTime);
                    float timescale = config.JitterTimescale.CalculateTimescale(error);

                    playbackTime += UnityEngine.Time.deltaTime * timescale;

                    while (pendingServerStates.TryPop(Tick, playbackTime, DeltaTime, out ServerInputMessage serverInputMessage))
                    {
                        Debug.Assert(serverInputMessage.Tick == Tick, $"Attempting to use inputs from server tick {serverInputMessage.Tick} while client is on tick {Tick}.");

                        currentInputs = serverInputMessage.Inputs;

                        playerJoinEvents = serverInputMessage.NewPlayersJoining;

                        TickEvents();

                        TickUpdate();

                        long checksum = snapshot.Allocator.Checksum();

                        Debug.Assert(checksum == serverInputMessage.Checksum, "Checksums not equal.");

                        if (PollInput)
                        {
                            ByteBuffer byteBuffer = new ByteBuffer();
                            ClientInputMessage clientInputMessage = new ClientInputMessage(polledInput, Tick);
                            clientInputMessage.Write(byteBuffer);

                            transporter.SendToAll(byteBuffer.Data, DeliveryMethod.Reliable);
                        }

                        Tick++;
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

            return pendingServerStates.Size;
        }

        public float MessageBufferDelay()
        {
            Debug.Assert(Role == RunnerRole.Client);

            return pendingServerStates.CurrentDelay(DeltaTime, UnityEngine.Time.time, playbackTime);
        }

        public float TargetBufferDelay()
        {
            Debug.Assert(Role == RunnerRole.Client);

            return pendingServerStates.TargetDelay();
        }

        public float CurrentBufferError()
        {
            Debug.Assert(Role == RunnerRole.Client);

            return pendingServerStates.CalculateError(DeltaTime, UnityEngine.Time.time, playbackTime);
        }

        public int NextTick()
        {
            Debug.Assert(Role == RunnerRole.Client);

            return pendingServerStates.OldestTick;
        }

        public float Ping()
        {
            Debug.Assert(Role == RunnerRole.Client);

            return 0;
        }

        public float Timescale()
        {
            return config.JitterTimescale.CalculateTimescale(pendingServerStates.CalculateError(DeltaTime, UnityEngine.Time.time, playbackTime));
        }

        public int ClientInputBufferCount()
        {
            Debug.Assert(Role == RunnerRole.Host);

            return clientInputBuffers[0].Size;
        }
        #endregion
    }
}
