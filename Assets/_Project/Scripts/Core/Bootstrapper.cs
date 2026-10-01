using UnityEngine;

/// <summary>
/// Bootstrapper: Dam bao GameManager va GameState ton tai truoc bat ky scene nao.
/// Them GameObject nay vao dau tien trong MainMenu scene.
/// Su dung [DefaultExecutionOrder(-100)] de chay truoc cac script khac.
/// </summary>
[DefaultExecutionOrder(-100)]
public class Bootstrapper : MonoBehaviour
{
    [Header("Core Prefabs")]
    [SerializeField] private GameManager gameManagerPrefab;
    [SerializeField] private GameState gameStatePrefab;

    private void Awake()
    {
        // Neu chua co GameManager, tao moi
        if (GameManager.Instance == null)
        {
            if (gameManagerPrefab != null)
                Instantiate(gameManagerPrefab);
            else
                Debug.LogError("[Bootstrapper] GameManager prefab not assigned!");
        }

        // Neu chua co GameState, tao moi
        if (GameState.Instance == null)
        {
            if (gameStatePrefab != null)
                Instantiate(gameStatePrefab);
            else
                Debug.LogError("[Bootstrapper] GameState prefab not assigned!");
        }

        // Bootstrapper tu huy sau khi khoi tao xong
        Destroy(gameObject);
    }
}
