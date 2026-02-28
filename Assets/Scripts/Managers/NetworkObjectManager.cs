using UnityEngine;
using System.Collections.Generic;

public class NetworkObjectManager : MonoBehaviour // Not NetworkBehaviour
{
    public static NetworkObjectManager Instance { get; private set; }

    private Dictionary<ulong, Player> _players = new Dictionary<ulong, Player>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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