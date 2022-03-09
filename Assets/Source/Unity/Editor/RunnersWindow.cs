using UnityEngine;
using UnityEditor;
using System.Linq;

namespace GLHF.Editor
{
    public class RunnersWindow : EditorWindow
    {      
        [System.Serializable]
        private class CachedRunner
        {
            public Runner Runner;
            public bool Visible;
            public bool PollInput;

            public bool Valid => Runner != null;

            public void ApplyChanges()
            {
                Runner.PollInput = PollInput;

                if (Runner.Scene.IsValid())
                {
                    var roots = Runner.Scene.GetRootGameObjects();

                    if (Visible)
                        SceneVisibilityManager.instance.Show(roots, true);
                    else
                        SceneVisibilityManager.instance.Hide(roots, true);
                }
            }
        }

        private CachedRunner[] cachedRunners;

        [MenuItem("Window/GLHF/Runners")]
        private static void Open()
        {
            RunnersWindow window = (RunnersWindow)GetWindow(typeof(RunnersWindow), false, "Runners");
            window.Show();
        }

        private void ApplyChangesToAllRunners()
        {
            foreach (var cachedRunner in cachedRunners)
            {
                cachedRunner.ApplyChanges();
            }
        }

        private void RefreshRunners()
        {
            var runners = FindObjectsOfType<Runner>(true);
            runners = runners.Reverse().ToArray();

            var newCachedRunners = new CachedRunner[runners.Length];

            for (int i = 0; i < runners.Length; i++)
            {
                newCachedRunners[i] = new CachedRunner() { Runner = runners[i] };

                if (cachedRunners != null && i < cachedRunners.Length)
                {
                    newCachedRunners[i].Visible = cachedRunners[i].Visible;
                    newCachedRunners[i].PollInput = cachedRunners[i].PollInput;
                }
                else
                {
                    newCachedRunners[i].Visible = true;
                    newCachedRunners[i].PollInput = true;
                }
            }

            cachedRunners = newCachedRunners;

            ApplyChangesToAllRunners();
        }

        private void OnGUI()
        {
            if ((cachedRunners == null || cachedRunners.Length == 0 || !cachedRunners[0].Valid) && Application.isPlaying)
            {
                RefreshRunners();
            }

            if (cachedRunners == null || cachedRunners.Length == 0)
                return;

            EditorGUILayout.BeginVertical();

            var bootstrap = FindObjectOfType<Bootstrap>();

            if (bootstrap != null && bootstrap.CanAddClient())
            {
                if (GUILayout.Button("Add Client"))
                {
                    bootstrap.AddClient();

                    RefreshRunners();
                }
            }

            for (int i = 0; i < cachedRunners.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginDisabledGroup(!Application.isPlaying);
                string text = cachedRunners[i].Valid ? cachedRunners[i].Runner.Role.ToString() : $"Runner {i}";
                GUILayout.Label(text);
                EditorGUI.EndDisabledGroup();

                bool visible = GUILayout.Toggle(cachedRunners[i].Visible, "Visible");
                bool input = GUILayout.Toggle(cachedRunners[i].PollInput, "Input");

                if (visible != cachedRunners[i].Visible)
                {
                    cachedRunners[i].Visible = visible;

                    if (Application.isPlaying)
                        ApplyChangesToAllRunners();
                }

                if (input != cachedRunners[i].PollInput)
                {
                    cachedRunners[i].PollInput = input;

                    if (Application.isPlaying)
                        ApplyChangesToAllRunners();
                }

                EditorGUI.BeginDisabledGroup(!cachedRunners[i].Valid || !cachedRunners[i].Runner.Running);

                if (GUILayout.Button("Disconnect"))
                {
                    cachedRunners[i].Runner.Shutdown();
                }

                EditorGUI.EndDisabledGroup();                
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
