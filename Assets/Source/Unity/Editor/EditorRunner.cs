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
                if (runner.Running)
                {
                    if (runner.Role == Runner.RunnerRole.Client)
                    {
                        GUILayout.Label($"Message Buffer Count: {runner.MessageBufferCount()}");
                        GUILayout.Label($"Stable Buffer Size: {runner.TargetMessageBufferSize()}");
                        GUILayout.Label($"Rtt Standard Dev: {runner.PingStandardDeviation() * 1000:F2}");
                        GUILayout.Label($"Timescale: {runner.Timescale():F2}");
                    }
                }
                else
                {
                    GUILayout.Label($"Runner not running");
                }
            }
        }
    }
}
