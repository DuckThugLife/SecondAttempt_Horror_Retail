using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
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

            // Fire the event for any already-subscribed listeners
            OnServicesInitialized?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Service Init Failed: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        // Clear the event to prevent memory leaks
        OnServicesInitialized = null;
    }
}