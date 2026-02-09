using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{

    [SerializeField] private GameObject playerPrefab;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        NetBootstrap.OnServicesInitialized += HandleServicesReady;
    }

    private void OnDisable()
    {
        NetBootstrap.OnServicesInitialized -= HandleServicesReady;
    }

    private void HandleServicesReady()
    {
        // Initial entry point of the game
        SceneManager.LoadScene("LobbyScene");

        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("In lobby scene  and your host instantiate player prefab!");
            var playerInstance = Instantiate(playerPrefab);
            playerInstance.GetComponent<NetworkObject>().Spawn();
        }
    }

    // Explicit API — nothing implicit
    public void GoToLobby()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    public void GoToGame()
    {
        SceneManager.LoadScene("GameScene");
    }
}
