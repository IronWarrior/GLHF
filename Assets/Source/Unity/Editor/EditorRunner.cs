using UnityEngine;
using UnityEditor;

namespace GLHF.Editor
{
    [CustomEditor(typeof(Runner))]
    public class EditorRunner : UnityEditor.Editor
    {
        private void OnEnable() { EditorApplication.update += Update; }
        private void OnDisable() { EditorApplication.update -= Update; }

        private float lastUpdateTime;
        private const float updateRate = 0.25f;

        private void Update()
        {
            Runner runner = (Runner)target;

            if (Application.isPlaying && runner.Running)
            {
                if (Time.unscaledTime > lastUpdateTime + updateRate)
                {
                    Repaint();
                    lastUpdateTime = Time.unscaledTime;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            Runner runner = (Runner)target;

            if (Application.isPlaying)
            {
                if (runner.Role == Runner.RunnerRole.Client)
                {
                    if (runner.Running)
                    {
                        GUILayout.Label($"Tick: {runner.Tick}");
                        GUILayout.Label($"Message Next Tick: {runner.NextTick()}");
                        GUILayout.Label($"Message Buffer Count: {runner.MessageBufferCount()}");
                        GUILayout.Label($"Current Delay: {runner.MessageBufferDelay()}");
                        GUILayout.Label($"Current Error: {runner.CurrentBufferError()}");
                        GUILayout.Label($"Target Delay: {runner.TargetBufferDelay()}");
                        GUILayout.Label($"Rtt: {runner.Ping() * 1000:F2}");
                        GUILayout.Label($"Timescale: {runner.Timescale():F2}");
                    }
                    else
                    {
                        GUILayout.Label($"Client runner not running");
                    }
                }
                else
                {
                    GUILayout.Label($"Players: {runner.PlayerCount}");

                    if (runner.Running)
                    {
                        GUILayout.Label($"Client Input Buffer Count: {runner.ClientInputBufferCount()}");
                    }
                }
            }
        }
    }
}
