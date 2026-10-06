using UnityEngine;

/// <summary>
/// RobotDetection: Xu ly phat hien Player.
/// Logic: Distance Check -> FOV Check -> Raycast (Line of Sight) -> Timer
/// Attach vao: Robot GameObject (cung voi RobotAI)
/// </summary>
public class RobotDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float fieldOfView = 90f;        // Tong goc (45 moi phia)
    [SerializeField] private float detectionTime = 0.8f;     // Thoi gian de full detect
    [SerializeField] private Transform eyePoint;             // Vi tri mat robot

    [Header("Layer Masks")]
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask obstacleMask;

    // Runtime
    private float detectionTimer;
    private bool isDetecting;

    public bool IsFullyDetected => detectionTimer >= detectionTime;
    public float DetectionProgress => Mathf.Clamp01(detectionTimer / detectionTime);

    public void StartDetectionTimer() => isDetecting = true;

    private void Update()
    {
        if (!isDetecting) return;
        detectionTimer = Mathf.Min(detectionTimer + Time.deltaTime, detectionTime);
    }

    /// <summary>
    /// Kiem tra co the phat hien player khong.
    /// Phai thoa man: Distance + FOV + LineOfSight.
    /// </summary>
    public bool CanDetectPlayer(Transform player)
    {
        if (player == null) return false;

        // 1. Distance check
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRange)
        {
            ResetTimer();
            return false;
        }

        // 2. FOV check
        Vector3 eye = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 dirToPlayer = (player.position - eye).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > fieldOfView * 0.5f)
        {
            ResetTimer();
            return false;
        }

        // 3. Line of Sight (Raycast) - nhan dien player theo Tag/Hierarchy (khong phu thuoc Layer)
        Vector3 target = player.position + Vector3.up * 1.0f;
        Vector3 losDir = (target - eye).normalized;
        float losDist = Vector3.Distance(eye, target);
        int mask = (obstacleMask.value | playerMask.value | (1 << player.gameObject.layer));
        if (mask == 0) mask = ~0;
        RaycastHit[] hits = Physics.RaycastAll(eye, losDir, losDist, mask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            Transform t = hit.collider.transform;
            if (t == transform || t.IsChildOf(transform)) continue; // bo qua chinh robot
            if (t == player || t.IsChildOf(player) || hit.collider.CompareTag("Player")) break; // thay player
            // Bi chan boi vat can
            ResetTimer();
            return false;
        }

        // Thay player
        isDetecting = true;
        return true;
    }

    private void ResetTimer()
    {
        isDetecting = false;
        detectionTimer = 0f;
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // FOV lines
        Vector3 fovLeft  = Quaternion.Euler(0, -fieldOfView * 0.5f, 0) * transform.forward;
        Vector3 fovRight = Quaternion.Euler(0,  fieldOfView * 0.5f, 0) * transform.forward;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, fovLeft * detectionRange);
        Gizmos.DrawRay(transform.position, fovRight * detectionRange);
    }
}
