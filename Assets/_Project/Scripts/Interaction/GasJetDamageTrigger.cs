using UnityEngine;

/// <summary>
/// GasJetDamageTrigger: Vung luong hoi nuoc / khi doc phut ra tu van xa.
/// Khi nguoi choi buoc truc tiep vao luong hoi se bi bong / nhiem doc nang va bi tru mau.
/// </summary>
public class GasJetDamageTrigger : MonoBehaviour
{
    [SerializeField] private int damagePerTick = 3;
    [SerializeField] private float tickInterval = 0.7f;

    private float timer = 0f;
    private bool playerInside = false;
    private PlayerHealth playerHealth;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerHealth>() != null)
        {
            playerInside = true;
            playerHealth = other.GetComponent<PlayerHealth>();
            DealDamage();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (playerInside && playerHealth != null)
        {
            timer += Time.deltaTime;
            if (timer >= tickInterval)
            {
                timer = 0f;
                DealDamage();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerHealth>() != null)
        {
            playerInside = false;
            timer = 0f;
        }
    }

    private void DealDamage()
    {
        if (playerHealth != null && !playerHealth.IsDead)
        {
            playerHealth.TakeEnvironmentalDamage(damagePerTick);
            NotificationUI.ShowMessage("CẢNH BÁO: BỊ BỎNG HƠI KHÍ ĐỘC! Hãy tránh xa luồng khí đang xả.");
        }
    }
}
