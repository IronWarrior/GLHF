using UnityEngine;
using UnityEditor;

namespace GLHF.Editor
{
    [CustomEditor(typeof(Bootstrap))]
    public class EditorBootstrap : UnityEditor.Editor
    {        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            Bootstrap bootstrap = (Bootstrap)target;

            if (Application.isPlaying)
            {
                GUILayout.Space(16);

                EditorGUI.BeginDisabledGroup(!bootstrap.CanAddClient());
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Add Client"))
                {
                    bootstrap.AddClient("localhost");
                }

                GUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}
