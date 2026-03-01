using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class Bootstrapper : MonoBehaviour
{
    public static event System.Action OnServicesInitialized;
    public static bool ServicesInitialized { get; private set; }

    private async void Awake()
    {
        DontDestroyOnLoad(gameObject);
        await InitializeServices();

        // Set local player name BEFORE loading scene
        if (AuthenticationService.Instance.IsSignedIn)
        {
            string playerName = $"Player{AuthenticationService.Instance.PlayerId}";
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
            Debug.Log($"Bootstrapper: Saved player name {playerName}");
        }

        Debug.Log("Services ready, loading LobbyScene...");
        SceneManager.LoadScene("LobbyScene");
    }

    private async Task InitializeServices()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            ServicesInitialized = true;
            OnServicesInitialized?.Invoke();
            Debug.Log("Services initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Service Init Failed: {e.Message}");
        }
    }
}