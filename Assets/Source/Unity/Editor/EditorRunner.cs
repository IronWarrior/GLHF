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
                GUILayout.Label($"Tick: {runner.Simulation.Tick}");

                Runner.Diagnostics diagnostics = runner.GetDiagnostics();

                if (runner.Role == Runner.RunnerRole.Client)
                {
                    if (runner.Running)
                    {                        
                        GUILayout.Label($"Ping: {diagnostics.RoundTripTime * 1000:#}ms");
                        GUILayout.Label($"Predicted Ticks: {diagnostics.PredictedTickCount}");
                    }
                    else
                    {
                        GUILayout.Label($"Client runner not running");
                    }
                }
                else
                {
                    GUILayout.Label($"Players: {runner.PlayerCount}");
                }
            }
        }
    }
}
