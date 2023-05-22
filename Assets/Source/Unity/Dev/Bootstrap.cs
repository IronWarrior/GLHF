using GLHF.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace GLHF.Editor
{
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField]
        Config config;

        [SerializeField]
        Runner runnerPrefab;

        [SerializeField]
        int clients = 1;

        [SerializeField]
        int port = 7777;

        [SerializeField]
        bool useLocalTransport;

        [SerializeField]
        bool useSimulatedLatency;

        [SerializeField]
        bool useDesyncAnalyzer;

        [SerializeField]
        int minLatencyMilliseconds, maxLatencyMilliseconds;

        private Runner host;

        private List<Runner> runners;

        private DesyncAnalyzer desyncAnalyzer;

        private IEnumerator Start()
        {
            if (FindObjectOfType<Runner>() == null)
            {
                DontDestroyOnLoad(gameObject);

                if (useDesyncAnalyzer)
                   desyncAnalyzer = new DesyncAnalyzer();

                host = null;

                runners = new List<Runner>();

                if (clients > 0)
                {
                    for (int i = 0; i < clients; i++)
                    {
                        BuildRunner(out Runner runner, out Transporter transporter);

                        if (i == 0)
                        {
                            runner.name = "Host";
                            runner.Host(port, config, transporter);
                            host = runner;

                            desyncAnalyzer?.SetHost(runner);
                        }
                        else
                        {
                            runner.name = $"Client {i}";
                            runner.Join(port, "localhost", config, transporter);

                            desyncAnalyzer?.AddClient(runner);
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
            BuildRunner(out Runner runner, out Transporter transporter);

            runner.name = $"Client {runners.Count - 1}";
            runner.Join(port, ip, config, transporter);

            runners.Add(runner);

            desyncAnalyzer?.AddClient(runner);

            if (runners.Count == 1)
            {
                StartCoroutine(UnloadBootstrapSceneWhenRunnerLoaded(runners[0]));
            }
        }

        public bool CanAddClient()
        {
            return !useLocalTransport || (host != null && host.Running);
        }

        private void BuildRunner(out Runner runner, out Transporter transporter)
        {
            runner = Instantiate(runnerPrefab);
            
            ITransport transport = useLocalTransport ? new TransportLocal() : new TransportLiteNetLib();
            transporter = new Transporter(transport);

            if (useSimulatedLatency)
            {
                transporter.SetSimulatedLatency(new Transporter.SimulatedLatency() { MaxDelay = maxLatencyMilliseconds, MinDelay = minLatencyMilliseconds });
            }
        }

        private IEnumerator UnloadBootstrapSceneWhenRunnerLoaded(Runner runner)
        {
            yield return new WaitUntil(() => runner.Scene.isLoaded);

            SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
        }
    }
}