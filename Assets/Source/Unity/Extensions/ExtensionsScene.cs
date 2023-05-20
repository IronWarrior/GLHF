using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExtensionsScene
{
    private static readonly List<GameObject> roots = new();

    public static T FindObjectOfType<T>(this Scene scene, bool includeInactive = false) where T : class
    {
        scene.GetRootGameObjects(roots);

        foreach (var root in roots)
        {
            if (!root.activeSelf)
                continue;

            var result = root.GetComponentInChildren<T>(includeInactive);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public static T[] FindObjectsOfType<T>(this Scene scene, bool includeInactive = false) where T : class
    {
        List<T> results = new();

        scene.GetRootGameObjects(roots);

        foreach (var root in roots)
        {
            if (!root.activeSelf)
                continue;

            results.AddRange(root.GetComponentsInChildren<T>(includeInactive));
        }

        return results.ToArray();
    }
}
