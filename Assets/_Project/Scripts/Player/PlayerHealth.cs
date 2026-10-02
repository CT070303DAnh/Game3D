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
    [SerializeField] private float invincibleDuration = 1.5f; // Thoi gian bat tu sau khi bi damage

    // ───────────────────────────────────────────────
    // Runtime State
    // ───────────────────────────────────────────────
    private int currentHealth;
    private bool isInvincible;
    private float invincibleTimer;
    private bool isDead;

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
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
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
        Debug.Log($"[PlayerHealth] Took {amount} damage. HP: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnPlayerDamaged?.Invoke();

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
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnPlayerDamaged?.Invoke();

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>Hoi phuc HP (battery item).</summary>
    public void Heal(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
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
        Debug.Log("[PlayerHealth] Player died.");
        OnPlayerDied?.Invoke();

        // Bao GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver();
    }
}
