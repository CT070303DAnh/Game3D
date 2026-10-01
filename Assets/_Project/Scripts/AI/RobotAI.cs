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
    [SerializeField] private Color patrolColor = Color.green;
    [SerializeField] private Color detectColor = Color.yellow;
    [SerializeField] private Color chaseColor = Color.red;

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
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
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

        // Bat dau inactive
        agent.enabled = false;
        SetSensorColor(patrolColor);
    }

    private void Update()
    {
        if (currentState == RobotState.Inactive) return;
        if (playerTransform == null) return;

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

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            waypointTimer += Time.deltaTime;
            if (waypointTimer >= waypointWaitTime)
            {
                waypointTimer = 0;
                currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
                agent.SetDestination(waypoints[currentWaypoint].position);
            }
        }
    }

    private void UpdateDetect()
    {
        // Nhìn ve phia player trong luc phat hien
        LookAt(playerTransform.position);

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

        // Tan cong neu du gan
        float dist = Vector3.Distance(transform.position, playerTransform.position);
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
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
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

        agent.SetDestination(waypoints[currentWaypoint].position);
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
            ChangeState(RobotState.Patrol);
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
                agent.isStopped = false;
                if (waypoints != null && waypoints.Length > 0)
                    agent.SetDestination(waypoints[currentWaypoint].position);
                break;
            case RobotState.Detect:
                SetSensorColor(detectColor);
                agent.isStopped = true;
                detection.StartDetectionTimer();
                break;
            case RobotState.Chase:
                SetSensorColor(chaseColor);
                agent.isStopped = false;
                PlaySound(chaseSound);
                OnRobotAlert?.Invoke();
                break;
            case RobotState.Search:
                SetSensorColor(detectColor);
                searchTimer = 0;
                agent.isStopped = false;
                break;
            case RobotState.Return:
                SetSensorColor(patrolColor);
                agent.isStopped = false;
                break;
            case RobotState.Attack:
                agent.isStopped = true;
                attackTimer = attackCooldown; // Tan cong ngay
                break;
        }
        Debug.Log($"[RobotAI] State: {newState}");
    }

    private void ActivateRobot()
    {
        if (currentState != RobotState.Inactive) return;
        agent.enabled = true;
        PlaySound(alertSound);
        ChangeState(RobotState.Patrol);
        Debug.Log("[RobotAI] Robot activated!");
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
