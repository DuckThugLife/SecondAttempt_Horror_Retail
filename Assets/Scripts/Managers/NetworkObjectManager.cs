using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class NetworkObjectManager : NetworkBehaviour
{
    public static NetworkObjectManager Instance { get; private set; }

    private Dictionary<ulong, Player> _players = new Dictionary<ulong, Player>();

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("NetworkObjectManager instance already exists, destroying duplicate");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("NetworkObjectManager initialized");
    }

    public void RegisterPlayer(ulong clientId, Player player)
    {
        if (!_players.ContainsKey(clientId))
        {
            _players.Add(clientId, player);
            Debug.Log($"Registered player {clientId}");
        }
    }

    public void UnregisterPlayer(ulong clientId)
    {
        if (_players.ContainsKey(clientId))
        {
            _players.Remove(clientId);
            Debug.Log($"Unregistered player {clientId}");
        }
    }

    public Player GetPlayer(ulong clientId)
    {
        _players.TryGetValue(clientId, out var player);
        return player;
    }

    public List<Player> GetAllPlayers()
    {
        return new List<Player>(_players.Values);
    }
}