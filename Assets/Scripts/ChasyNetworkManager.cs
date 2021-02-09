using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public struct CreateCharacterMessage : NetworkMessage {
    public string name;
}

public class ChasyNetworkManager : NetworkManager
{
    public List<GameObject> playerSpawns;
    private int players = 0;

    public override void OnStartServer()
    {
        base.OnStartServer();

        NetworkServer.RegisterHandler<CreateCharacterMessage>(OnCreateCharacter);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        conn.Send(new CreateCharacterMessage { name = "halko" });
    }

    void OnCreateCharacter(NetworkConnection conn, CreateCharacterMessage createCharacterMessage)
    {
        GameObject characterObject = playerSpawns != null && playerSpawns.Capacity >= 3 
            ? Instantiate(playerPrefab, playerSpawns[players++ % 3].transform.position, playerSpawns[players++ % 3].transform.rotation)
            : Instantiate(playerPrefab);

        NetworkServer.AddPlayerForConnection(conn, characterObject);
    }
}
