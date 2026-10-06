using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// RobotAI: Finite State Machine dieu khien Robot.
/// States: Patrol -> Detect -> Chase -> Search -> Return -> Attack
/// Require: NavMeshAgent, Animator, RobotDetection
/// Attach vao: Robot GameObject (tren NavMesh)
/// Robot chi ACTIVE sau khi PowerRestored.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(RobotDetection))]
public class RobotAI : MonoBehaviour
{
    public enum RobotState { Inactive, Patrol, Detect, Chase, Search, Return, Attack }

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointWaitTime = 1.5f;
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 4.5f;

    [Header("Search")]
    [SerializeField] private float searchDuration = 5f;
    [SerializeField] private float searchRadius = 4f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip alertSound;
    [SerializeField] private AudioClip chaseSound;
    [SerializeField] private AudioClip attackSound;

    [Header("VFX")]
    [SerializeField] private Light sensorLight;
    [SerializeField] private RobotVisionCone visionCone;
    [SerializeField] private Color patrolColor = Color.green;
    [SerializeField] private Color detectColor = Color.yellow;
    [SerializeField] private Color chaseColor = Color.red;

    [Header("Ranged Shooting")]
    [SerializeField] private bool canShoot = false;
    [SerializeField] private float shootRange = 18f;
    [SerializeField] private float shootCooldown = 1.3f;
    [SerializeField] private int shootDamage = 15;
    [SerializeField] private Transform gunMuzzle;
    [SerializeField] private LineRenderer laserBeamLine;
    [SerializeField] private Light muzzleLight;
    [SerializeField] private LayerMask shootObstacleMask;
    private float shootTimer = 0f;

    // Components
    private NavMeshAgent agent;
    private RobotDetection detection;
    private Animator animator;
    private AudioSource audioSource;

    // State
    private RobotState currentState = RobotState.Inactive;
    private int currentWaypoint = 0;
    private float waypointTimer;
    private float searchTimer;
    private float attackTimer;
    private Vector3 lastSeenPlayerPos;
    private Transform playerTransform;

    // Animator hashes
    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashAttack = Animator.StringToHash("Attack");
    private bool hasAnimator;

    public RobotState CurrentState => currentState;
    public static event System.Action OnRobotAlert;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        detection = GetComponent<RobotDetection>();
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (visionCone == null) visionCone = GetComponentInChildren<RobotVisionCone>(true);
        if (sensorLight == null) sensorLight = GetComponentInChildren<Light>(true);
        hasAnimator = animator != null && animator.runtimeAnimatorController != null;
    }

    private void OnEnable()
    {
        GameState.OnPowerRestored += ActivateRobot;
    }

    private void OnDisable()
    {
        GameState.OnPowerRestored -= ActivateRobot;
    }

    private void Start()
    {
        // Tim player
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) playerTransform = playerGO.transform;

        // Dam bao robot nam tren NavMesh
        if (agent != null && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 4.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isOutdoorLevel3 = sceneName.Contains("Level3") || sceneName.Contains("Helipad");

        // Ở Màn 1 (Lab) hoặc các màn giải đố cần cấp điện: Robot BẮT BUỘC bất động hoàn toàn, CHỈ thức tỉnh khi lắp cầu chì & bật máy phát điện (PowerRestored)!
        if (!isOutdoorLevel3)
        {
            if (GameState.Instance != null && GameState.Instance.PowerRestored)
            {
                ActivateRobot();
            }
            else
            {
                DeactivateRobot();
            }
        }
        else
        {
            // Riêng Màn 3 (Sân đỗ trực thăng): Robot tuần tra kích hoạt ngay từ đầu
            ActivateRobot();
        }
    }

    private void Update()
    {
        if (currentState == RobotState.Inactive) return;
        if (playerTransform == null)
        {
            var pGO = GameObject.FindGameObjectWithTag("Player");
            if (pGO != null) playerTransform = pGO.transform;
            else return;
        }

        switch (currentState)
        {
            case RobotState.Patrol:  UpdatePatrol();  break;
            case RobotState.Detect:  UpdateDetect();  break;
            case RobotState.Chase:   UpdateChase();   break;
            case RobotState.Search:  UpdateSearch();  break;
            case RobotState.Return:  UpdateReturn();  break;
            case RobotState.Attack:  UpdateAttack();  break;
        }

        // Cap nhat animator speed
        if (hasAnimator)
            animator.SetFloat(HashSpeed, agent.velocity.magnitude / chaseSpeed);
    }

    // ── STATES ──────────────────────────────────────

    private void UpdatePatrol()
    {
        // Kiem tra phat hien player truoc
        if (detection.CanDetectPlayer(playerTransform))
        { ChangeState(RobotState.Detect); return; }

        if (waypoints == null || waypoints.Length == 0) return;
        if (!agent.isOnNavMesh) return;

        // Kiem tra da toi waypoint chua (tinh toan theo stoppingDistance de khong bao gio bi ket)
        bool reached = false;
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance + 0.3f, 0.8f))
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.05f || agent.remainingDistance < 0.5f)
                    reached = true;
            }
        }

        if (reached)
        {
            PlayAnimState("Idle_Guard_AR", 0.25f);
            waypointTimer += Time.deltaTime;
            if (waypointTimer >= waypointWaitTime)
            {
                waypointTimer = 0f;
                currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
                if (waypoints[currentWaypoint] != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(waypoints[currentWaypoint].position);
                    PlayAnimState("WalkFront_Shoot_AR", 0.25f);
                }
            }
        }
        else
        {
            // Dam bao agent khong bi stop va dang di chuyen ve waypoint hien tai
            if (agent.isStopped) agent.isStopped = false;
            if (!agent.hasPath && !agent.pathPending && waypoints[currentWaypoint] != null)
            {
                agent.SetDestination(waypoints[currentWaypoint].position);
            }
            PlayAnimState("WalkFront_Shoot_AR", 0.25f);
        }
    }

    private void UpdateDetect()
    {
        // Nhìn ve phia player trong luc phat hien
        LookAt(playerTransform.position);

        // Bắn súng cảnh cáo khi phát hiện người chơi
        if (canShoot)
        {
            shootTimer += Time.deltaTime;
            if (shootTimer >= shootCooldown)
            {
                shootTimer = 0f;
                ShootAtPlayer();
            }
        }

        if (detection.IsFullyDetected)
        {
            ChangeState(RobotState.Chase);
        }
        else if (!detection.CanDetectPlayer(playerTransform))
        {
            ChangeState(RobotState.Patrol);
        }
    }

    private void UpdateChase()
    {
        if (playerTransform == null) { ChangeState(RobotState.Search); return; }

        lastSeenPlayerPos = playerTransform.position;
        agent.SetDestination(lastSeenPlayerPos);

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // Bắn súng vào người chơi từ xa
        if (canShoot && dist <= shootRange)
        {
            LookAt(playerTransform.position);
            shootTimer += Time.deltaTime;
            if (shootTimer >= shootCooldown)
            {
                shootTimer = 0f;
                ShootAtPlayer();
            }
        }

        // Tan cong can chien neu du gan
        if (dist <= attackRange)
        { ChangeState(RobotState.Attack); return; }

        // Mat player -> Search
        if (!detection.CanDetectPlayer(playerTransform))
        {
            ChangeState(RobotState.Search);
        }
    }

    private void UpdateSearch()
    {
        searchTimer += Time.deltaTime;

        // Neu gap player trong luc search
        if (detection.CanDetectPlayer(playerTransform))
        { ChangeState(RobotState.Chase); return; }

        // Di chuyen ngau nhien trong vung search
        if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance + 0.3f, 0.8f))
        {
            Vector3 randomPoint = lastSeenPlayerPos + Random.insideUnitSphere * searchRadius;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        // Het thoi gian search -> Return
        if (searchTimer >= searchDuration)
            ChangeState(RobotState.Return);
    }

    private void UpdateReturn()
    {
        // Neu thay player tren duong ve
        if (detection.CanDetectPlayer(playerTransform))
        { ChangeState(RobotState.Chase); return; }

        if (waypoints == null || waypoints.Length == 0) { ChangeState(RobotState.Patrol); return; }

        if (agent.isOnNavMesh)
        {
            agent.SetDestination(waypoints[currentWaypoint].position);
            if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance + 0.3f, 0.8f))
                ChangeState(RobotState.Patrol);
        }
    }

    private void UpdateAttack()
    {
        if (playerTransform == null) { ChangeState(RobotState.Chase); return; }

        LookAt(playerTransform.position);
        attackTimer += Time.deltaTime;

        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0;
            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (dist <= attackRange)
            {
                PerformAttack();
            }
            else
            {
                ChangeState(RobotState.Chase);
            }
        }
    }

    private void PerformAttack()
    {
        if (hasAnimator) animator.SetTrigger(HashAttack);
        PlaySound(attackSound);

        PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(attackDamage);
        Debug.Log("[RobotAI] Attack! Damage: " + attackDamage);
    }

    public void SetupGun(Transform muzzle, LineRenderer beam, Light flash)
    {
        gunMuzzle = muzzle;
        laserBeamLine = beam;
        muzzleLight = flash;
    }

    private void ShootAtPlayer()
    {
        if (playerTransform == null) return;

        LookAt(playerTransform.position);

        Vector3 muzzlePos = gunMuzzle != null ? gunMuzzle.position : transform.position + Vector3.up * 1.4f + transform.forward * 0.5f;
        Vector3 targetPos = playerTransform.position + Vector3.up * 1.0f; // Target player's torso
        Vector3 dir = (targetPos - muzzlePos).normalized;
        float dist = Vector3.Distance(muzzlePos, targetPos);

        bool hitPlayer = true;
        // Bắn Raycast kiểm tra vật cản (bỏ qua trigger & chính robot, nhận diện player theo hierarchy)
        int mask = shootObstacleMask.value != 0 ? shootObstacleMask.value : ~0;
        RaycastHit[] hits = Physics.RaycastAll(muzzlePos, dir, dist, mask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            Transform t = hit.collider.transform;
            if (t == transform || t.IsChildOf(transform)) continue;
            if (t == playerTransform || t.IsChildOf(playerTransform) || hit.collider.CompareTag("Player")) break;
            hitPlayer = false;
            targetPos = hit.point;
            break;
        }

        // Bắn tia đạn laser đỏ rực
        StartCoroutine(LaserTracerRoutine(muzzlePos, targetPos));
        PlayAnimState("Shoot_Autoshot_AR", 0.1f);

        // Âm thanh bắn súng
        if (attackSound != null && audioSource != null)
            audioSource.PlayOneShot(attackSound);
        else if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();

        // Gây sát thương nếu trúng người chơi
        if (hitPlayer)
        {
            PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
            if (ph != null && !ph.IsDead)
            {
                ph.TakeDamage(shootDamage);
                NotificationUI.ShowMessage($"⚠️ CẢNH BÁO: BỊ ROBOT BẮN TRÚNG! (-{shootDamage} HP)");
            }
        }
    }

    private System.Collections.IEnumerator LaserTracerRoutine(Vector3 start, Vector3 end)
    {
        if (laserBeamLine != null)
        {
            laserBeamLine.enabled = true;
            laserBeamLine.SetPosition(0, start);
            laserBeamLine.SetPosition(1, end);
        }
        if (muzzleLight != null)
        {
            muzzleLight.enabled = true;
        }

        yield return new WaitForSeconds(0.12f);

        if (laserBeamLine != null) laserBeamLine.enabled = false;
        if (muzzleLight != null) muzzleLight.enabled = false;
    }

    // ── STATE MACHINE ────────────────────────────────

    private void ChangeState(RobotState newState)
    {
        currentState = newState;
        agent.speed = newState == RobotState.Chase || newState == RobotState.Attack
            ? chaseSpeed : patrolSpeed;

        switch (newState)
        {
            case RobotState.Patrol:
                SetSensorColor(patrolColor);
                PlayAnimState("WalkFront_Shoot_AR", 0.25f);
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    if (waypoints != null && waypoints.Length > 0 && waypoints[currentWaypoint] != null)
                        agent.SetDestination(waypoints[currentWaypoint].position);
                }
                break;
            case RobotState.Detect:
                SetSensorColor(detectColor);
                PlayAnimState("Idle_Guard_AR", 0.2f);
                agent.isStopped = true;
                shootTimer = shootCooldown * 0.65f; // ban phat dau tien nhanh hon
                detection.StartDetectionTimer();
                break;
            case RobotState.Chase:
                SetSensorColor(chaseColor);
                PlayAnimState("Run_guard_AR", 0.25f);
                agent.isStopped = false;
                PlaySound(chaseSound);
                OnRobotAlert?.Invoke();
                break;
            case RobotState.Search:
                SetSensorColor(detectColor);
                PlayAnimState("Idle_Guard_AR", 0.2f);
                searchTimer = 0;
                agent.isStopped = false;
                break;
            case RobotState.Return:
                SetSensorColor(patrolColor);
                PlayAnimState("WalkFront_Shoot_AR", 0.25f);
                agent.isStopped = false;
                break;
            case RobotState.Attack:
                agent.isStopped = true;
                attackTimer = attackCooldown; // Tan cong ngay
                PlayAnimState("Shoot_Autoshot_AR", 0.15f);
                break;
        }
        Debug.Log($"[RobotAI] State: {newState}");
    }

    public void DeactivateRobot()
    {
        currentState = RobotState.Inactive;

        if (agent != null)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            agent.enabled = false;
        }

        // Tắt đèn sensor hoặc chỉnh mờ tối hẳn khi robot chưa có điện
        if (sensorLight != null)
        {
            sensorLight.color = new Color(0.1f, 0.15f, 0.25f);
            sensorLight.intensity = 0.2f;
        }

        // Ẩn vùng nón radar khi robot chưa thức tỉnh
        if (visionCone != null)
        {
            visionCone.gameObject.SetActive(false);
        }

        PlayAnimState("Idle_Guard_AR", 0.1f);
        if (animator != null)
        {
            animator.speed = 0f;
        }

        Debug.Log("[RobotAI] Robot is INACTIVE (Chờ lắp Cầu Chì vào máy phát điện mới thức tỉnh).");
    }

    public void ActivateRobot()
    {
        if (currentState != RobotState.Inactive) return;

        if (agent != null)
        {
            agent.enabled = true;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 4.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            agent.isStopped = false;
        }

        if (visionCone != null)
        {
            visionCone.gameObject.SetActive(true);
        }

        if (sensorLight != null)
        {
            sensorLight.color = patrolColor;
            sensorLight.intensity = 4.0f;
        }

        if (animator != null)
        {
            animator.speed = 1f;
        }

        PlaySound(alertSound);
        ChangeState(RobotState.Patrol);
        Debug.Log("[RobotAI] ⚡ Robot ACTIVATED! Bắt đầu tuần tra...");
    }

    public void PlayAnimState(string stateName, float transitionDuration = 0.2f)
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            hasAnimator = animator != null && animator.runtimeAnimatorController != null;
        }
        if (!hasAnimator || animator == null) return;

        int hash = Animator.StringToHash(stateName);
        if (animator.HasState(0, hash))
        {
            animator.CrossFadeInFixedTime(stateName, transitionDuration);
        }
    }

    private void LookAt(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    private void SetSensorColor(Color color)
    {
        if (sensorLight != null) sensorLight.color = color;
        if (visionCone != null) visionCone.SetConeColor(color);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
