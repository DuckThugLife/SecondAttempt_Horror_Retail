using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Managers")]
    [field: SerializeField] public SessionUIManager SessionUIManager { get; private set; }
    [field: SerializeField] public GameUIManager GameUIManager { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}