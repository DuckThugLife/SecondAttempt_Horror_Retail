using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement; // Required for swapping scenes
using Unity.Services.Core;
using Unity.Services.Authentication;

public class NetBootstrap : MonoBehaviour
{
    public static bool IsInitialized { get; private set; }
    public static event System.Action OnServicesInitialized;

    private async void Awake()
    {
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
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            IsInitialized = true;
            OnServicesInitialized?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Service Init Failed: {e.Message}");
        }
    }
}