using UnityEngine;

/// <summary>
/// PlayerHealth: Quan ly HP, nhan damage va xu ly death.
/// Phat event de UIManager va GameManager lang nghe.
///
/// Attach vao: Player GameObject
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // ───────────────────────────────────────────────
    // Inspector
    // ───────────────────────────────────────────────
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invincibleDuration = 0.6f; // Thoi gian bat tu ngan (0.6s) de khong can tro nhip danh tiep theo

    // ───────────────────────────────────────────────
    // Runtime State
    // ───────────────────────────────────────────────
    private int currentHealth;
    private bool isInvincible;
    private float invincibleTimer;
    private bool isDead;

    // Lưu lượng máu chuyển tiếp giữa các màn chơi
    private static int savedHealth = -1; // -1 = Chưa lưu / Khởi tạo game mới

    public static int SavedHealth => savedHealth;

    /// <summary>Lưu hoặc ép lượng máu (dùng khi chuyển màn hoặc khởi tạo).</summary>
    public static void SetSavedHealth(int health)
    {
        savedHealth = Mathf.Max(1, health);
        Debug.Log($"[PlayerHealth] Đã lưu lượng máu chuyển màn: {savedHealth} HP");
    }

    /// <summary>Reset lượng máu đã lưu về mặc định (dùng khi bắt đầu game mới hoặc chơi lại sau khi chết).</summary>
    public static void ResetSavedHealth()
    {
        savedHealth = -1;
        Debug.Log("[PlayerHealth] Đã reset lượng máu lưu trữ về mặc định (100 HP).");
    }

    // ───────────────────────────────────────────────
    // Events
    // ───────────────────────────────────────────────
    public static event System.Action<int, int> OnHealthChanged;  // (current, max)
    public static event System.Action OnPlayerDied;
    public static event System.Action OnPlayerDamaged;

    // ───────────────────────────────────────────────
    // Properties
    // ───────────────────────────────────────────────
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public float HealthPercent => (float)currentHealth / maxHealth;

    // ───────────────────────────────────────────────
    // Unity Lifecycle
    // ───────────────────────────────────────────────
    private void Start()
    {
        if (savedHealth > 0)
        {
            currentHealth = Mathf.Clamp(savedHealth, 1, maxHealth);
            Debug.Log($"[PlayerHealth] Tiếp tục lượng máu từ màn trước: {currentHealth}/{maxHealth} HP");
        }
        else
        {
            currentHealth = maxHealth;
        }

        savedHealth = currentHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void OnDestroy()
    {
        if (!isDead && currentHealth > 0)
        {
            savedHealth = currentHealth;
            Debug.Log($"[PlayerHealth] Lưu trữ máu trước khi đổi màn: {savedHealth}/{maxHealth} HP");
        }
    }

    private void Update()
    {
        // Dem thoi gian invincible
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f)
                isInvincible = false;
        }
    }

    // ───────────────────────────────────────────────
    // Public API
    // ───────────────────────────────────────────────

    /// <summary>Nhan damage tu Robot hoac trap.</summary>
    public void TakeDamage(int amount)
    {
        if (isDead || isInvincible) return;
        if (amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (!isDead && currentHealth > 0) savedHealth = currentHealth;
        Debug.Log($"[PlayerHealth] Took {amount} damage. HP: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnPlayerDamaged?.Invoke();
        GameAudioManager.PlayPlayerHurt();

        // Bat ky sau khi bi damage
        isInvincible = true;
        invincibleTimer = invincibleDuration;

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>Tru mau do ngat khi doc hoac hoi nuoc nong (bo qua invincibility de tru mau dinh ky dung nhip).</summary>
    public void TakeEnvironmentalDamage(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (!isDead && currentHealth > 0) savedHealth = currentHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnPlayerDamaged?.Invoke();

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>Bị điện giật từ Robot (giảm đúng 5 HP và kích hoạt phản hồi sát thương).</summary>
    public void TakeElectricShock(int amount = 5)
    {
        TakeEnvironmentalDamage(amount);
    }

    /// <summary>Hoi phuc HP (battery item).</summary>
    public void Heal(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (!isDead && currentHealth > 0) savedHealth = currentHealth;
        Debug.Log($"[PlayerHealth] Healed {amount}. HP: {currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // ───────────────────────────────────────────────
    // Death
    // ───────────────────────────────────────────────
    private void Die()
    {
        if (isDead) return;
        isDead = true;
        currentHealth = 0;
        savedHealth = -1; // Reset để khi chơi lại hồi đủ máu
        Debug.Log("[PlayerHealth] Player died.");

        // 1. Báo GameManager TRƯỚC (để các hệ thống khác biết đã Game Over)
        try
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver();
        }
        catch (System.Exception e) { Debug.LogWarning(e); }

        // 2. Khóa điều khiển người chơi ngay lập tức
        var pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        // 3. Phát event (bọc try/catch để 1 listener lỗi không chặn Game Over)
        try { OnPlayerDied?.Invoke(); }
        catch (System.Exception e) { Debug.LogWarning(e); }

        // 4. Luôn đảm bảo popup Game Over hiển thị và game dừng lại
        bool shown = false;
        try { shown = GameplayUI.ForceShowGameOver(); }
        catch (System.Exception e) { Debug.LogWarning(e); }

        if (!shown)
        {
            Debug.LogWarning("[PlayerHealth] Không tìm thấy GameplayUI để hiện Game Over -> dừng game.");
        }
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
