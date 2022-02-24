using GLHF;
using UnityEngine;

public class PlayerSpawner : StateBehaviour
{
    [SerializeField]
    PlayerPhysics playerPrefab;

    public override int Size => 0;

    public override void TickStart()
    {
        for (int i = 0; i < Runner.PlayerCount; i++)
        {
            var player = Runner.Spawn(playerPrefab, new Vector3(i * 2, 0, 0));
            player.SetPlayerIndex(i);
        }
    }
}
