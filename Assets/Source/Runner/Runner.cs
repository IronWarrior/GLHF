using GLHF.Network;
using GLHF.Transport;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GLHF
{
    /// <summary>
    /// Manages connections between players and ticks the simulation.
    /// Can probably split the simulation bits into Host and Client.
    /// </summary>
    public unsafe class Runner : MonoBehaviour
    {
        public bool PollInput { get; set; } = true;

        public float DeltaTime { get; private set; }

        public bool Connected { get; private set; }
        public bool Running { get; private set; }

        public int PlayerCount { get; private set; }

        public UnityEngine.SceneManagement.Scene Scene { get; private set; }

        public RunnerRole Role { get; private set; }

        public enum RunnerRole
        {
            None = 0,
            Server = 1,
            Host = 2,
            Client = 3
        }

        public Simulation Simulation { get; private set; }

        public event Action<Snapshot> OnSimulateTick;

        private List<StateInput> currentInputs = new List<StateInput>();
        private int playerJoinEvents = 0;

        #region Server
        private List<ClientInputBuffer> clientInputBuffers;
        private float deltaTimeAccumulated;
        #endregion

        // TODO: Things that are client or server specific should be encapsulated.
        #region Client
        public event Action<Snapshot> OnDesync;

        private NetworkSimulation clientSimulation;
        private OrderedMessageBuffer<ServerInputMessage> unconsumedServerStates;

        private Rollback rollback;

        private int localPlayerIndex;
        #endregion

        private Transporter transporter;
        private Config config;

        private IInputHandler inputHandler;

        private void Start()
        {
            inputHandler = GetComponent<IInputHandler>();
        }

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
                clientInputBuffers.Add(new ClientInputBuffer(Running ? Simulation.Tick : 0));
                currentInputs.Add(default);

                PlayerCount++;
                playerJoinEvents++;

                if (Running)
                {
                    byte[] data = Simulation.Snapshot.Allocator.ToByteArray(true);

                    // TODO: Make a message class for this.
                    ByteBuffer buffer = new ByteBuffer();
                    buffer.Put((byte)MessageType.Start);
                    buffer.Put(0);
                    buffer.Put(PlayerCount);
                    buffer.Put(peerId + 1);
                    buffer.Put(true);
                    buffer.Put(Simulation.Tick);      

                    buffer.Put(data.Length);
                    buffer.Put(data);

                    transporter.Send(peerId, buffer.Data, DeliveryMethod.Reliable);
                }
            }
            else if (Role == RunnerRole.Client)
            {
                Connected = true;
            }
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

                clientInputBuffers[id].Insert(message, Simulation.Tick, DeltaTime - deltaTimeAccumulated, DeltaTime);
            }
            else
            {
                if (msgType == MessageType.Start)
                {
                    int sceneIndex = buffer.Get<int>();
                    PlayerCount = buffer.Get<int>();
                    localPlayerIndex = buffer.Get<int>();

                    bool hasState = buffer.Get<bool>();

                    Action OnComplete = null;

                    if (hasState)
                    {
                        int tick = buffer.Get<int>();
                        int dataLength = buffer.Get<int>();
                        byte[] state = buffer.Get<byte>(dataLength);

                        OnComplete = () =>
                        {
                            Simulation.Tick = tick;
                            rollback.ForwardTick = tick;
                            clientSimulation.SetConfirmedTime(Simulation.Tick * DeltaTime);
                            Simulation.Snapshot.Allocator.CopyFrom(state);
                            Simulation.RebuildGameObjectWorld();
                            rollback.PushSnapshotToConfirmed();
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

            for (int i = 1; i < PlayerCount; i++)
            {
                ByteBuffer buffer = new ByteBuffer();
                buffer.Put((byte)MessageType.Start);
                buffer.Put(0);
                buffer.Put(PlayerCount);
                buffer.Put(i);

                transporter.Send(i - 1, buffer.Data, DeliveryMethod.ReliableOrdered);
            }

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
        
        private void StartRunning()
        {
            Running = true;

            var allocator = new Allocator(1024);
            var snapshot = new Snapshot(allocator);

            var gameObjectWorld = new GameObjectWorld(snapshot, Scene, config.PrefabTable);

            Simulation = new Simulation(snapshot, DeltaTime, Scene, gameObjectWorld, localPlayerIndex);
            gameObjectWorld.BuildFromStateObjects(Scene.FindObjectsOfType<StateObject>());

            Simulation.RebuildGameObjectWorld();

            if (Role == RunnerRole.Client)
                rollback = new Rollback(snapshot);
        }
        #endregion

        private void Update()
        {
            // Check for incoming messages, firing any applicable events.
            transporter.Poll();

            if (Running)
            {
                StateInput polledInput = default;

                if (PollInput)
                    polledInput = inputHandler.GetInput();

                if (Role == RunnerRole.Host)
                {
                    deltaTimeAccumulated += Time.deltaTime;

                    while (deltaTimeAccumulated > DeltaTime)
                    {
                        deltaTimeAccumulated -= DeltaTime;

                        currentInputs[0] = polledInput;

                        for (int i = 0; i < clientInputBuffers.Count; i++)
                        {
                            if (clientInputBuffers[i].TryPop(out ClientInputMessage inputMessage))
                            {
                                currentInputs[i + 1] = inputMessage.Input;
                            }
                        }

                        var inputs = new Simulation.Inputs()
                        {
                            PlayerJoinEvents = playerJoinEvents,
                            StateInputs = currentInputs.ToArray()
                        };

                        Simulation.Integrate(inputs, out long checksum);

                        OnSimulateTick?.Invoke(Simulation.Snapshot);

                        for (int i = 0; i < clientInputBuffers.Count; i++)
                        {
                            var byteBuffer = new ByteBuffer();
                            var serverInputMessage = new ServerInputMessage(currentInputs, Simulation.Tick - 1, checksum, playerJoinEvents, clientInputBuffers[i].Error);
                            serverInputMessage.Write(byteBuffer);

                            transporter.Send(i, byteBuffer.Data, DeliveryMethod.Reliable);
                        }

                        playerJoinEvents = 0;
                    }
                }
                else
                {
                    int nextConfirmed = rollback.Confirmed.Tick;

                    int nextTickToEnterBuffer = clientSimulation.NextTickToEnter(nextConfirmed);

                    while (unconsumedServerStates.TryDequeue(nextTickToEnterBuffer, out ServerInputMessage message))
                    {
                        clientSimulation.Insert(message, Time.time);

                        nextTickToEnterBuffer++;
                    }

                    clientSimulation.Integrate(Time.time, Time.deltaTime);

                    bool confirmedTickSimulated = false;
                    
                    while (clientSimulation.TryPop(nextConfirmed, Time.time, out ServerInputMessage serverInputMessage))
                    {
                        if (!confirmedTickSimulated)
                        {
                            rollback.PopConfirmedToSnapshot();
                            Simulation.RebuildGameObjectWorld();
                        }

                        Debug.Assert(serverInputMessage.Tick == Simulation.Tick, $"Attempting to use inputs from server tick {serverInputMessage.Tick} while client is on tick {Simulation.Tick}.");

                        currentInputs = serverInputMessage.Inputs;

                        playerJoinEvents = serverInputMessage.NewPlayersJoining;

                        var inputs = new Simulation.Inputs()
                        {
                            PlayerJoinEvents = playerJoinEvents,
                            StateInputs = currentInputs.ToArray()
                        };
                        
                        Simulation.Integrate(inputs, out long checksum);

                        if (checksum != serverInputMessage.Checksum)
                        {
                            OnDesync?.Invoke(Simulation.Snapshot);

                            Debug.LogError($"Checksums not equal.");
                        }

                        rollback.ConsumePredictedInput(Simulation.Tick - 1);

                        nextConfirmed = Simulation.Tick;
                        confirmedTickSimulated = true;
                    }
                    
                    if (confirmedTickSimulated)
                    {
                        rollback.PushSnapshotToConfirmed();
                        Simulation.RebuildGameObjectWorld();

                        int targetForwardTick = Simulation.Tick + clientSimulation.GetPredictedTickCount();

                        while (Simulation.Tick < rollback.ForwardTick)
                        {
                            StateInput input = rollback.GetPredictedInput(Simulation.Tick);

                            currentInputs[localPlayerIndex] = input;

                            var inputs = new Simulation.Inputs()
                            {
                                PlayerJoinEvents = playerJoinEvents,
                                StateInputs = currentInputs.ToArray()
                            };

                            Simulation.Integrate(inputs, out _);
                        }

                        while (Simulation.Tick <= targetForwardTick)
                        {
                            if (!PollInput)
                                polledInput = default;
                            
                            rollback.InsertPredictedInput(Simulation.Tick, polledInput);

                            currentInputs[localPlayerIndex] = polledInput;

                            var inputs = new Simulation.Inputs()
                            {
                                PlayerJoinEvents = playerJoinEvents,
                                StateInputs = currentInputs.ToArray()
                            };

                            Simulation.Integrate(inputs, out _);

                            var byteBuffer = new ByteBuffer();
                            var clientInputMessage = new ClientInputMessage(polledInput, Simulation.Tick - 1);
                            clientInputMessage.Write(byteBuffer);

                            transporter.SendToAll(byteBuffer.Data, DeliveryMethod.Reliable);
                        }

                        rollback.ForwardTick = Simulation.Tick;
                    }
                }

                Simulation.Render();
            }
        }
        
        #region Diagnostics
        public struct Diagnostics
        {
            public int PredictedTickCount;
        }

        public Diagnostics GetDiagnostics()
        {
            Diagnostics diagnostics = new();

            if (Role == RunnerRole.Client)
            {
                diagnostics.PredictedTickCount = clientSimulation.GetPredictedTickCount();
            }

            return diagnostics;
        }
        #endregion
    }
}
