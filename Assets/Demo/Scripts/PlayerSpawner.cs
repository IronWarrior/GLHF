using GLHF;
using UnityEngine;

public class PlayerSpawner : TickBehaviour, IPlayerJoined
{
    [SerializeField]
    PlayerPhysics playerPrefab;

    public void PlayerJoined()
    {
        int index = Runner.FindObjectsOfType<PlayerPhysics>().Length;

        var player = Runner.Spawn(playerPrefab, new Vector3(index * 2, 0, 0));
        player.SetPlayerIndex(index);
    }
}
