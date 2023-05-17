using GLHF;
using UnityEngine;

public class PlayerSpawner : TickBehaviour, IPlayerJoined
{
    [SerializeField]
    PlayerPhysics playerPrefab;

    public void PlayerJoined()
    {
        int index = Simulation.FindObjectsOfType<PlayerPhysics>().Length;

        var player = Simulation.Spawn(playerPrefab, new Vector3(index * 2, 0, 0));
        player.SetPlayerIndex(index);
    }
}
