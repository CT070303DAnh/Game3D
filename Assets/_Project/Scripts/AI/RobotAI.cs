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

    [Header("Electric Shock On Touch (Màn 1 & Màn 2)")]
    [Tooltip("Bật cơ chế giật điện khi robot di chuyển chạm vào người chơi")]
    [SerializeField] private bool enableShockOnTouch = true;
    [Tooltip("Lượng máu bị mất khi bị giật điện (-5 máu theo yêu cầu)")]
    [SerializeField] private int shockDamage = 5;
    [Tooltip("Khoảng cách tiếp xúc kích hoạt điện giật (mét)")]
    [SerializeField] private float shockTouchDistance = 1.35f;
    [Tooltip("Thời gian giãn cách giữa các lần giật điện (giây)")]
    [SerializeField] private float shockCooldown = 1.0f;
    [SerializeField] private AudioClip shockAudioClip;

    private float lastShockTime = -999f;
    private Light shockFlashLight;
    private LineRenderer shockArcLine;
    private ParticleSystem shockSparkVFX;
    private static AudioClip proceduralZapClip;

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
        EnsureShockComponents();
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

        // Phân biệt màn chơi:
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isLabLevel1 = sceneName.Equals("Lab", System.StringComparison.OrdinalIgnoreCase)
            || (sceneName.IndexOf("Lab", System.StringComparison.OrdinalIgnoreCase) >= 0
                && sceneName.IndexOf("Reactor", System.StringComparison.OrdinalIgnoreCase) < 0
                && sceneName.IndexOf("Helipad", System.StringComparison.OrdinalIgnoreCase) < 0);

        if (isLabLevel1)
        {
            // Màn 1 (Lab): Robot ngủ đông lúc đầu (tắt di chuyển, tắt nón quét radar, đèn cảm biến mờ tối).
            // Robot CHỈ thức tỉnh khi người chơi bật Cầu Dao / Máy Phát Điện (GameState.PowerRestored).
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
            // Màn 2 (Reactor) và Màn 3 (Helipad): Robot luôn kích hoạt tuần tra ngay từ đầu
            ActivateRobot();
        }

        EnsureShockComponents();
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

        // Kiểm tra tiếp xúc chạm giật điện liên tục khi robot đang di chuyển / hoạt động
        if (enableShockOnTouch && currentState != RobotState.Inactive && playerTransform != null)
        {
            CheckTouchShockProximity();
        }

        // Giữ cho Robot luôn đứng thẳng, không bao giờ bị lật hoặc nằm sàn
        Vector3 curEuler = transform.eulerAngles;
        if (Mathf.Abs(curEuler.x) > 3f || Mathf.Abs(curEuler.z) > 3f)
        {
            transform.eulerAngles = new Vector3(0f, curEuler.y, 0f);
        }

        // Chống kẹt animation: Nếu Animator lỡ rơi vào Die hoặc Jump, lập tức thoát ra hành vi hiện tại
        if (hasAnimator && animator != null)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            if (st.IsName("Die") || st.IsName("Jump"))
            {
                string resumeAnim = currentState == RobotState.Chase ? "Run_guard_AR" :
                    (agent != null && agent.velocity.sqrMagnitude > 0.05f) ? "WalkFront_Shoot_AR" : "Idle_Guard_AR";
                PlayAnimState(resumeAnim, 0.05f);
            }
        }
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
        PlayAnimState("Shoot_Autoshot_AR", 0.1f);
        PlaySound(attackSound);

        PlayerHealth ph = playerTransform != null 
            ? (playerTransform.GetComponent<PlayerHealth>() ?? playerTransform.GetComponentInParent<PlayerHealth>())
            : null;
        if (ph == null)
        {
            var pGO = GameObject.FindGameObjectWithTag("Player");
            if (pGO != null) ph = pGO.GetComponent<PlayerHealth>() ?? pGO.GetComponentInParent<PlayerHealth>();
            if (ph == null) ph = FindFirstObjectByType<PlayerHealth>();
        }

        if (ph != null && !ph.IsDead)
        {
            int dmg = attackDamage > 0 ? attackDamage : 20;
            ph.TakeDamage(dmg);
            NotificationUI.ShowMessage($"⚠️ CẢNH BÁO: BỊ ROBOT TẤN CÔNG! (-{dmg} HP)");
            Debug.Log($"[RobotAI] Attack hit player! -{dmg} HP. HP remaining: {ph.CurrentHealth}/{ph.MaxHealth}");

            // Hiệu ứng giật nảy & hồ quang khi trúng đòn
            StartCoroutine(ElectricShockVFXRoutine(ph.transform));
            ApplyShockKnockback(ph.transform);
            PlayShockAudio();
        }
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

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            hasAnimator = animator != null && animator.runtimeAnimatorController != null;
        }

        if (animator != null)
        {
            int idleHash = Animator.StringToHash("Idle_Guard_AR");
            if (animator.HasState(0, idleHash))
            {
                animator.Play(idleHash, 0, 0f);
                animator.Update(0f);
            }
            animator.speed = 0f;
        }

        Debug.Log("[RobotAI] 💤 Robot đang NGỦ ĐÔNG (Chờ người chơi bật Cầu Dao / Máy Phát Điện mới thức tỉnh).");
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
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
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

        AudioClip wakeClip = alertSound != null ? alertSound : GetProceduralWakeClip();
        PlaySound(wakeClip);
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
        if (enableShockOnTouch)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, shockTouchDistance);
        }
    }

    // ── ELECTRIC SHOCK ON TOUCH (MÀN 1 & MÀN 2) ──────

    /// <summary>
    /// Kiểm tra khoảng cách vật lý giữa Robot và Player.
    /// Kích hoạt giật điện nếu robot di chuyển chạm sát vào người chơi.
    /// </summary>
    private void CheckTouchShockProximity()
    {
        if (playerTransform == null) return;

        float horizontalDist = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(playerTransform.position.x, playerTransform.position.z)
        );
        float heightDiff = Mathf.Abs(transform.position.y - playerTransform.position.y);

        if (horizontalDist <= shockTouchDistance && heightDiff <= 1.8f)
        {
            TriggerElectricShock(playerTransform);
        }
    }

    /// <summary>
    /// Kích hoạt giật điện khi robot chạm vào người chơi:
    /// - Trừ đúng 5 HP máu.
    /// - Chớp sáng tia sét xanh điện URP.
    /// - Âm thanh giật điện BZZZT!
    /// - Thông báo cảnh báo trên HUD.
    /// - Giật nảy đẩy lùi nhẹ người chơi.
    /// </summary>
    public void TriggerElectricShock(Transform target)
    {
        if (!enableShockOnTouch || currentState == RobotState.Inactive) return;
        if (Time.time < lastShockTime + shockCooldown) return;

        if (target == null) target = playerTransform;
        if (target == null)
        {
            var pGO = GameObject.FindGameObjectWithTag("Player");
            if (pGO != null) target = pGO.transform;
            else return;
        }

        PlayerHealth ph = target.GetComponent<PlayerHealth>() ?? target.GetComponentInParent<PlayerHealth>();
        if (ph == null || ph.IsDead) return;

        lastShockTime = Time.time;

        // 1. Trừ 5 HP máu của Player
        ph.TakeElectricShock(shockDamage);
        Debug.Log($"<color=cyan>[RobotAI] ⚡ ĐIỆN GIẬT! Chạm vào Robot: -{shockDamage} HP! Còn {ph.CurrentHealth}/{ph.MaxHealth}</color>");

        // 2. Thông báo trên HUD
        NotificationUI.ShowMessage($"⚡ CẢNH BÁO: BỊ ROBOT GIẬT ĐIỆN! (-{shockDamage} HP)");

        // 3. Âm thanh giật điện BZZZT!
        PlayShockAudio();

        // 4. Hiệu ứng hồ quang sét và chớp sáng
        StartCoroutine(ElectricShockVFXRoutine(target));

        // 5. Lực giật điện đẩy văng nhẹ người chơi lùi ra
        ApplyShockKnockback(target);
    }

    private void ApplyShockKnockback(Transform target)
    {
        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc != null)
        {
            Vector3 pushDir = (target.position - transform.position);
            pushDir.y = 0f;
            if (pushDir.sqrMagnitude < 0.01f) pushDir = -transform.forward;
            pushDir.Normalize();

            // Đẩy lùi nhẹ 0.85m để người chơi phản xạ giật nảy ra khỏi thân robot
            cc.Move(pushDir * 0.85f);
        }
    }

    private System.Collections.IEnumerator ElectricShockVFXRoutine(Transform target)
    {
        Vector3 startPos = transform.position + Vector3.up * 1.1f;
        Vector3 endPos = target != null ? target.position + Vector3.up * 1.0f : startPos + transform.forward * 1.0f;
        Vector3 midPos = (startPos + endPos) * 0.5f;

        if (shockFlashLight != null)
        {
            shockFlashLight.transform.position = midPos;
            shockFlashLight.enabled = true;
        }

        if (shockSparkVFX != null)
        {
            shockSparkVFX.transform.position = midPos;
            shockSparkVFX.Play();
        }

        if (shockArcLine != null)
        {
            shockArcLine.enabled = true;
            int segments = 6;
            shockArcLine.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / (segments - 1);
                Vector3 p = Vector3.Lerp(startPos, endPos, t);
                if (i > 0 && i < segments - 1)
                    p += Random.insideUnitSphere * 0.25f;
                shockArcLine.SetPosition(i, p);
            }
        }

        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (shockArcLine != null && shockArcLine.enabled)
            {
                int segments = shockArcLine.positionCount;
                for (int i = 1; i < segments - 1; i++)
                {
                    float t = (float)i / (segments - 1);
                    Vector3 p = Vector3.Lerp(startPos, endPos, t) + Random.insideUnitSphere * 0.3f;
                    shockArcLine.SetPosition(i, p);
                }
            }
            yield return null;
        }

        if (shockArcLine != null) shockArcLine.enabled = false;
        if (shockFlashLight != null) shockFlashLight.enabled = false;
    }

    private void PlayShockAudio()
    {
        AudioClip clipToPlay = shockAudioClip != null ? shockAudioClip : GetProceduralZapClip();
        if (clipToPlay != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.15f);
            audioSource.PlayOneShot(clipToPlay, 1.0f);
        }
    }

    public static AudioClip GetProceduralZapClip()
    {
        if (proceduralZapClip != null) return proceduralZapClip;

        int sampleRate = 44100;
        float duration = 0.28f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        System.Random rand = new System.Random(42);
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 14f);
            float buzz = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.45f;
            float buzz2 = Mathf.Sin(2f * Mathf.PI * 480f * t) * 0.3f;
            float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.5f;
            if (rand.NextDouble() > 0.82) noise *= 1.8f;

            samples[i] = Mathf.Clamp((buzz + buzz2 + noise) * envelope * 0.9f, -1f, 1f);
        }

        proceduralZapClip = AudioClip.Create("ElectricZap_Procedural", sampleCount, 1, sampleRate, false);
        proceduralZapClip.SetData(samples, 0);
        return proceduralZapClip;
    }

    private static AudioClip proceduralWakeClip;
    public static AudioClip GetProceduralWakeClip()
    {
        if (proceduralWakeClip != null) return proceduralWakeClip;

        int sampleRate = 44100;
        float duration = 0.85f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(260f, 950f, t / duration);
            float envelope = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.45f;
            float tone2 = Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t) * 0.25f;
            float pulse = Mathf.Sin(2f * Mathf.PI * 16f * t) > 0 ? 1f : 0.6f;
            samples[i] = Mathf.Clamp((tone + tone2) * pulse * envelope, -1f, 1f);
        }

        proceduralWakeClip = AudioClip.Create("RobotWake_Procedural", sampleCount, 1, sampleRate, false);
        proceduralWakeClip.SetData(samples, 0);
        return proceduralWakeClip;
    }

    private void EnsureShockComponents()
    {
        // 1. Trigger Collider để bắt va chạm tiếp xúc
        var colliders = GetComponents<Collider>();
        bool hasTrigger = false;
        foreach (var col in colliders)
        {
            if (col.isTrigger) { hasTrigger = true; break; }
        }
        if (!hasTrigger)
        {
            var sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.95f;
            sc.center = new Vector3(0f, 0.9f, 0f);
        }

        // 2. Rigidbody Kinematic để Unity Physics gửi sự kiện Trigger/Collision (Khoá toàn bộ xoay)
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        // 3. Shock Flash Light
        Transform flashT = transform.Find("ShockFlashLight");
        if (flashT == null)
        {
            var fGO = new GameObject("ShockFlashLight");
            fGO.transform.SetParent(transform, false);
            fGO.transform.localPosition = new Vector3(0f, 1.1f, 0.4f);
            shockFlashLight = fGO.AddComponent<Light>();
            shockFlashLight.type = LightType.Point;
            shockFlashLight.color = new Color(0.2f, 0.85f, 1f);
            shockFlashLight.range = 5f;
            shockFlashLight.intensity = 6f;
            shockFlashLight.enabled = false;
        }
        else
        {
            shockFlashLight = flashT.GetComponent<Light>();
        }

        // 4. Shock Arc LineRenderer
        Transform arcT = transform.Find("ShockArcLine");
        if (arcT == null)
        {
            var aGO = new GameObject("ShockArcLine");
            aGO.transform.SetParent(transform, false);
            shockArcLine = aGO.AddComponent<LineRenderer>();
            shockArcLine.startWidth = 0.08f;
            shockArcLine.endWidth = 0.04f;
            shockArcLine.useWorldSpace = true;
            shockArcLine.positionCount = 6;

            Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (s != null)
            {
                var mat = new Material(s);
                mat.color = new Color(0.3f, 0.95f, 1f, 1f);
                shockArcLine.material = mat;
            }
            shockArcLine.startColor = new Color(0.4f, 0.95f, 1f, 1f);
            shockArcLine.endColor = new Color(0.8f, 1f, 1f, 0.7f);
            shockArcLine.enabled = false;
        }
        else
        {
            shockArcLine = arcT.GetComponent<LineRenderer>();
        }

        // 5. Shock Spark Particle System
        Transform sparkT = transform.Find("ShockSparks");
        if (sparkT == null)
        {
            var spGO = new GameObject("ShockSparks");
            spGO.transform.SetParent(transform, false);
            spGO.transform.localPosition = new Vector3(0f, 1.1f, 0.4f);
            shockSparkVFX = spGO.AddComponent<ParticleSystem>();
            var main = shockSparkVFX.main;
            main.startLifetime = 0.22f;
            main.startSpeed = 4f;
            main.startSize = 0.08f;
            main.startColor = new Color(0.3f, 0.9f, 1f);
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = 30;

            var emit = shockSparkVFX.emission;
            emit.rateOverTime = 0;
            emit.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

            var shape = shockSparkVFX.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            shockSparkVFX.Stop();
        }
        else
        {
            shockSparkVFX = sparkT.GetComponent<ParticleSystem>();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!enableShockOnTouch || currentState == RobotState.Inactive) return;
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerHealth>() != null)
        {
            Transform t = other.transform;
            var ph = other.GetComponentInParent<PlayerHealth>();
            if (ph != null) t = ph.transform;
            TriggerElectricShock(t);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        OnTriggerStay(other);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!enableShockOnTouch || currentState == RobotState.Inactive) return;
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponentInParent<PlayerHealth>() != null)
        {
            Transform t = collision.transform;
            var ph = collision.gameObject.GetComponentInParent<PlayerHealth>();
            if (ph != null) t = ph.transform;
            TriggerElectricShock(t);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnCollisionStay(collision);
    }
}
