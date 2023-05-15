using GLHF.Dev;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GLHF
{
    public class DesyncAnalyzer
    {
        private readonly Queue<Snapshot> hostSnapshots = new Queue<Snapshot>();

        private Runner host;

        private const int maxHostSnapshots = 60;

        public void SetHost(Runner runner)
        {
            host = runner;

            host.OnSimulateTick += OnHostSimulateTick;
        }

        private void OnHostSimulateTick(Snapshot snapshot)
        {
            Snapshot clone = new Snapshot(snapshot);

            hostSnapshots.Enqueue(clone);

            if (hostSnapshots.Count > maxHostSnapshots)
            {
                hostSnapshots.Dequeue();
            }
        }

        public void AddClient(Runner runner)
        {
            runner.OnDesync += (snapshot) => OnDesync(runner, snapshot);
        }

        private unsafe void OnDesync(Runner runner, Snapshot snapshot)
        {
            Debug.Break();

            long tick = snapshot.Tick;

            foreach (var hostSnapshot in hostSnapshots)
            {
                if (hostSnapshot.Tick == snapshot.Tick)
                {
                    var window = EditorWindow.GetWindow<SnapshotComparer>();
                    window.Initialize(hostSnapshot, snapshot, runner);
                    return;
                }
            }

            Debug.LogError($"Unable to find host snapshot for tick {tick}.");            
        }
    }
}
