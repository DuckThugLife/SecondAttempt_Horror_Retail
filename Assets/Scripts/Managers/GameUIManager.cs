using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Game UI")]
    [SerializeField] private GameObject gameRootGO;
    [SerializeField] private GameObject hoverIconGO;
    [SerializeField] private GameObject crosshairGO;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowGameUI()
    {
        if (gameRootGO != null)
            gameRootGO.SetActive(true);
    }

    public void HideGameUI()
    {
        if (gameRootGO != null)
            gameRootGO.SetActive(false);
    }

    public void HoverUI()
    {
        if (hoverIconGO != null) hoverIconGO.SetActive(true);
        if (crosshairGO != null) crosshairGO.SetActive(false);
    }

    public void UnHoverUI()
    {
        if (hoverIconGO != null) hoverIconGO.SetActive(false);
        if (crosshairGO != null) crosshairGO.SetActive(true);
    }
}