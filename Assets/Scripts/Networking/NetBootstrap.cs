using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement; // Required for swapping scenes
using Unity.Services.Core;
using Unity.Services.Authentication;

public class NetBootstrap : MonoBehaviour
{
    public static bool IsInitialized { get; private set; }

    [SerializeField] private string lobbySceneName = "LobbyScene";

    private async void Awake()
    {
        // Singleton pattern: ensure only one 'brain' exists
        if (IsInitialized)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        await InitServices();
    }

    private async Task InitServices()
    {
        try
        {
            // Initialize the UGS (Unity Gaming Services) Core
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            // Sign in anonymously so we are ready for Relay/Multiplayer
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            IsInitialized = true;
            Debug.Log("Services Initialized & Player Signed In");

            // Automatically move to the Lobby Scene once everything is green
            SceneManager.LoadScene(lobbySceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Service Init Failed: {e.Message}");
            // Optional: Show an error message to the user here
        }
    }
}