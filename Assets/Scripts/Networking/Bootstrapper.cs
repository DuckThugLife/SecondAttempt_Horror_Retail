using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox; 

public class Bootstrapper : MonoBehaviour
{
    public static event System.Action OnServicesInitialized;
    public static bool ServicesInitialized { get; private set; }
    private static bool _vivoxInitialized = false;

    [Header("Player Names")]
    [SerializeField]
    private string[] randomNames = new string[]
    {
        "GhostHunter", "SpookyPlayer", "PhasmFears", "Ectoplasm",
        "Paranormal", "SpecterSeeker", "Sokka", "Aang",
        "Toph", "Katara", "Zuko"
    };

    private async void Awake()
    {
        DontDestroyOnLoad(gameObject);
        await InitializeServices();

        if (!PlayerPrefs.HasKey("PlayerName") && randomNames.Length > 0)
        {
            string randomName = randomNames[Random.Range(0, randomNames.Length)] + " " +
                                randomNames[Random.Range(0, randomNames.Length)];
            PlayerPrefs.SetString("PlayerName", randomName);
            PlayerPrefs.Save();
            Debug.Log($"Generated random name: {randomName}");
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