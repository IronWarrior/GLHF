using GLHF;
using UnityEngine;
using UnityEngine.UI;

public class DevSnapshotTool : MonoBehaviour
{
    [SerializeField]
    Button capture, load;

    private Allocator snapshottedState;

    private void Awake()
    {
        capture.onClick.AddListener(() => Capture());
        load.onClick.AddListener(() => Load());

        capture.interactable = true;
        load.interactable = false;
    }

    private void Capture()
    {
        Runner runner = FindObjectOfType<Runner>();
        var allocator = runner.snapshot.Allocator;
        snapshottedState = new Allocator(allocator);

        Debug.Assert(allocator.Checksum() == snapshottedState.Checksum());

        load.GetComponentInChildren<Text>().text = $"Load Snapshot Tick {runner.snapshot.Tick}";

        load.interactable = true;
    }

    private void Load()
    {
        Debug.Assert(snapshottedState != null);

        Runner runner = FindObjectOfType<Runner>();
        runner.SetState(snapshottedState);
    }
}
