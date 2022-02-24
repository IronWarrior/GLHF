using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GLHF
{
    [CreateAssetMenu(menuName = "Galaxy/Create Config Asset")]
    public class Config : ScriptableObject
    {
        public int TickRate = 60;
        public int JitterBufferSize = 1;

        public StateObject[] Prefabs;

        public Dictionary<int, StateObject> PrefabTable
        {
            get
            {
                if (prefabTable == null)
                    GeneratePrefabTable();

                return prefabTable;
            }
        }

        private Dictionary<int, StateObject> prefabTable;

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < Prefabs.Length; i++)
            {
                if (Prefabs[i] != null)
                    Prefabs[i].BakedPrefabId = i;
            }

            AssetDatabase.SaveAssets();
        }
#endif

        private void GeneratePrefabTable()
        {
            prefabTable = new Dictionary<int, StateObject>();

            foreach (var prefab in Prefabs)
            {
                prefabTable.Add(prefab.BakedPrefabId, prefab);
            }
        }
    }
}
