using GGEZ.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

namespace GGEZ
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

        private IEnumerator Start()
        {
            if (FindObjectOfType<Runner>() == null)
            {
                var scene = SceneManager.GetActiveScene();

                Runner host = null;

                List<Runner> runners = new List<Runner>();

                for (int i = 0; i < clients; i++)
                {
                    var runner = new GameObject().AddComponent<Runner>();                    

                    ITransport transport = useLocalTransport ? (ITransport)new TransportLocal() : new TransportLiteNetLib();

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

                    if (i == 0)
                    {
                        runner.Host(port, config, transport);
                        host = runner;
                    }
                    else
                    {
                        runner.Join(port, config, host, transport);
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

                yield return new WaitUntil(() => host.Running);

                SceneManager.SetActiveScene(host.Scene);
                SceneManager.UnloadSceneAsync(scene);
            }

            Destroy(gameObject);
        }
    }
}