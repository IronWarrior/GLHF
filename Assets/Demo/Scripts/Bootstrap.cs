using GLHF.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

namespace GLHF
{
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField]
        Config config;

        [SerializeField]
        int clients = 1;

        [SerializeField]
        int port = 7777;

        [SerializeField]
        bool useLocalTransport;

        [SerializeField]
        bool useSimulatedLatency;

        [SerializeField]
        int minLatencyMilliseconds, maxLatencyMilliseconds;

        private Runner host;

        private List<Runner> runners;

        private IEnumerator Start()
        {
            if (FindObjectOfType<Runner>() == null)
            {
                DontDestroyOnLoad(gameObject);

                host = null;

                runners = new List<Runner>();

                if (clients > 0)
                {
                    for (int i = 0; i < clients; i++)
                    {
                        BuildRunner(out Runner runner, out ITransport transport);

                        if (i == 0)
                        {
                            runner.name = "Host";
                            runner.Host(port, config, transport);
                            host = runner;
                        }
                        else
                        {
                            runner.name = $"Client {i}";
                            runner.Join(port, "localhost", config, transport);
                        }

                        runners.Add(runner);
                    }

                    bool wait = true;

                    while (wait)
                    {
                        wait = false;

                        foreach (var runner in runners)
                        {
                            if (!runner.Connected)
                                wait = true;
                        }

                        yield return null;
                    }

                    host.StartGame();

                    StartCoroutine(UnloadBootstrapSceneWhenRunnerLoaded(host));
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void AddClient(string ip)
        {
            BuildRunner(out Runner runner, out ITransport transport);

            runner.name = $"Client {runners.Count - 1}";
            runner.Join(port, ip, config, transport);

            runners.Add(runner);

            if (runners.Count == 1)
            {
                StartCoroutine(UnloadBootstrapSceneWhenRunnerLoaded(runners[0]));
            }
        }

        public bool CanAddClient()
        {
            return !useLocalTransport || (host != null && host.Running);
        }

        private void BuildRunner(out Runner runner, out ITransport transport)
        {
            runner = new GameObject().AddComponent<Runner>();

            transport = useLocalTransport ? (ITransport)new TransportLocal() : new TransportLiteNetLib();

            if (useSimulatedLatency)
            {
                try
                {
                    transport.SetSimulatedLatency(new SimulatedLatency() { MaxDelay = maxLatencyMilliseconds, MinDelay = minLatencyMilliseconds });
                }
                catch (NotImplementedException)
                {
                    Debug.LogError($"Attempted to set simulated latency on ITransport implementation that does not support it ({transport.GetType()}).");
                }
            }
        }

        private IEnumerator UnloadBootstrapSceneWhenRunnerLoaded(Runner runner)
        {
            yield return new WaitUntil(() => runner.Scene.isLoaded);

            SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
        }
    }
}