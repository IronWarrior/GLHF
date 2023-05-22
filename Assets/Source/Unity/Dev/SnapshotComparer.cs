using UnityEngine;
using UnityEditor;

namespace GLHF.Dev
{
#if UNITY_EDITOR
    public unsafe class SnapshotComparer : EditorWindow
    {
        private Snapshot truth, target;
        private Runner targetRunner;

        private Allocator.Block*[] truthBlocks, targetBlocks;

        public void Initialize(Snapshot truth, Snapshot target, Runner targetRunner)
        {
            this.truth = new Snapshot(truth);
            this.target = new Snapshot(target);

            this.targetRunner = targetRunner;

            truthBlocks = truth.RetrieveStateObjects();
            targetBlocks = target.RetrieveStateObjects();
        }

        private unsafe StateObject LocateStateObjectForPtr(Runner runner, byte* ptr)
        {
            foreach (var go in runner.Scene.GetRootGameObjects())
            {
                foreach (var so in go.GetComponentsInChildren<StateObject>(true))
                {
                    if (so.Ptr == ptr)
                        return so;
                }
            }

            return null;
        }

        private Vector2 scrollPosition;

        private void OnGUI()
        {
            if (truth == null || truth.Allocator.Head == null)
            {
                GUILayout.Label("Snapshots not initialized.");

                return;
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();

            GUILayout.Label($"Truth: {truthBlocks.Length} objects");

            for (int i = 0; i < truthBlocks.Length; i++)
            {
                var truthBlock = truthBlocks[i];

                DrawBlock(truthBlock, null, null);
            }

            GUILayout.EndVertical();
            GUILayout.BeginVertical();

            GUILayout.Label($"Target: {targetBlocks.Length} objects");

            for (int i = 0; i < targetBlocks.Length; i++)
            {
                var targetBlock = targetBlocks[i];
                var truthBlock = i < truthBlocks.Length ? truthBlocks[i] : null;

                DrawBlock(targetBlock, truthBlock, targetRunner);
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        private GUIStyle defaultStyle = new GUIStyle("Label");

        private void DrawBlock(Allocator.Block* block, Allocator.Block* compare, Runner sourceRunner)
        {
            GUILayout.Space(5);

            byte* ptr = Allocator.Block.Data(block);

            StateObject stateObject = null;

            if (sourceRunner != null)
                stateObject = LocateStateObjectForPtr(sourceRunner, ptr);

            GUILayout.Label(stateObject != null ? stateObject.name : "");

            if (compare != null)
                SetTextColor(block->Size == compare->Size);

            GUILayout.Label($"Size: {block->Size}");

            byte* comparePtr = Allocator.Block.Data(compare);

            StateBehaviour currentBehaviour = null;

            for (int offset = 0; offset < block->Size; offset++)
            {
                string additionalText = "";

                byte* offsetPtr = (ptr + offset);

                if (stateObject != null)
                {
                    var behaviour = stateObject.StateBehaviourContainingPointer(offsetPtr);

                    if (currentBehaviour != behaviour)
                    {
                        currentBehaviour = behaviour;
                        additionalText = currentBehaviour.GetType().Name;
                    }
                }

                byte data = *offsetPtr;
                bool equal = true;

                if (compare != null && compare->Size >= block->Size)
                {
                    equal = data == *(comparePtr + offset);
                }

                SetTextColor(equal);

                string message = data.ToString();

                if (!string.IsNullOrEmpty(additionalText))
                    message += $"\t{additionalText}";

                GUILayout.Label(message);
            }

            GUILayout.Space(5);

            GUI.contentColor = defaultStyle.normal.textColor;
        }

        private void SetTextColor(bool equal)
        {
            GUI.contentColor = equal ? defaultStyle.normal.textColor : Color.red;
        }
    }
#endif
}
