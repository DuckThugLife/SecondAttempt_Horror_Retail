using UnityEngine;

public class PersistentCanvas : MonoBehaviour
{
    private void Awake()
    {
        if (this != null)
        {
            Destroy(gameObject);
            return;
        }
        Debug.Log($"Dont destroy this GAMEOBJECT!: {gameObject}");
        DontDestroyOnLoad(gameObject);
    }
}