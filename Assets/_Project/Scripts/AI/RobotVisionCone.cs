using UnityEngine;

/// <summary>
/// RobotVisionCone: Hiển thị Vùng Radar Quét của Robot AI dưới dạng cánh quạt phát sáng (Vision Cone / Radar Sweep).
/// - Tạo Mesh hình cánh quạt (Sector) chiếu xuống sàn nhà (FOV ~75-80°, Bán kính ~13m).
/// - Có tia quét Radar (Sweep Line) quét qua lại liên tục như Radar quét thực sự.
/// - Tự động đổi màu theo trạng thái RobotAI:
///     • Xanh Neon (Tuần tra bình thường)
///     • Vàng Cam (Phát hiện người chơi / Cảnh báo)
///     • Đỏ Rực (Truy đuổi / Tấn công)
/// - Tự động cắt gọt theo vật cản (Raycast Occlusion) khi gặp tường hoặc thùng container!
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RobotVisionCone : MonoBehaviour
{
    [Header("Radar Cone Dimensions")]
    [SerializeField] private float fov = 75f;
    [SerializeField] private float viewDistance = 13f;
    [SerializeField] private int rayCount = 36;
    [SerializeField] private float groundOffset = 0.08f; // Cao hơn mặt sàn một chút để tránh Z-fighting
    [SerializeField] private LayerMask obstacleMask;

    [Header("Radar Animation")]
    [SerializeField] private bool enableSweepAnimation = true;
    [SerializeField] private float sweepSpeed = 2.4f;

    [Header("Colors & Transparency")]
    [SerializeField] private Color patrolColor = new Color(0.1f, 1.0f, 0.35f, 0.28f);
    [SerializeField] private Color detectColor = new Color(1.0f, 0.85f, 0.1f, 0.45f);
    [SerializeField] private Color chaseColor  = new Color(1.0f, 0.15f, 0.1f, 0.55f);

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material coneMaterial;
    private RobotAI robotAI;
    private Color currentColor;

    // Visual Sweep Line & Outer Arc
    private LineRenderer sweepLine;
    private LineRenderer borderArc;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        robotAI = GetComponentInParent<RobotAI>();

        mesh = new Mesh { name = "VisionConeMesh" };
        meshFilter.mesh = mesh;

        SetupMaterial();
        SetupLines();

        currentColor = patrolColor;
        UpdateMaterialColor(currentColor);

        // Mặc định layer obstacle là Environment / Ground nếu chưa gán
        if (obstacleMask == 0)
        {
            int envLayer = LayerMask.NameToLayer("Environment");
            int groundLayer = LayerMask.NameToLayer("Ground");
            int defLayer = LayerMask.NameToLayer("Default");
            int mask = 0;
            if (envLayer >= 0) mask |= (1 << envLayer);
            if (groundLayer >= 0) mask |= (1 << groundLayer);
            if (defLayer >= 0) mask |= (1 << defLayer);
            obstacleMask = mask;
        }
    }

    private void SetupMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");

        coneMaterial = new Material(shader);
        
        // Thiết lập chế độ Transparent Additive/Alpha trong URP
        if (coneMaterial.HasProperty("_Surface"))
            coneMaterial.SetFloat("_Surface", 1); // Transparent
        if (coneMaterial.HasProperty("_Blend"))
            coneMaterial.SetFloat("_Blend", 2); // Additive Glow
        
        coneMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        coneMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive Glow rực rỡ
        coneMaterial.SetInt("_ZWrite", 0);
        coneMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;
        coneMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        meshRenderer.material = coneMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void SetupLines()
    {
        // 1. Sweep Line: Tia quét radar quét qua quét lại
        var sweepGO = new GameObject("RadarSweepLine");
        sweepGO.transform.SetParent(transform, false);
        sweepLine = sweepGO.AddComponent<LineRenderer>();
        sweepLine.useWorldSpace = true;
        sweepLine.startWidth = 0.12f;
        sweepLine.endWidth = 0.25f;
        sweepLine.positionCount = 2;
        sweepLine.material = new Material(coneMaterial.shader);
        sweepLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sweepLine.receiveShadows = false;

        // 2. Border Arc: Viền cánh quạt ngoài cùng
        var borderGO = new GameObject("RadarBorderArc");
        borderGO.transform.SetParent(transform, false);
        borderArc = borderGO.AddComponent<LineRenderer>();
        borderArc.useWorldSpace = true;
        borderArc.startWidth = 0.08f;
        borderArc.endWidth = 0.08f;
        borderArc.positionCount = rayCount + 1;
        borderArc.material = new Material(coneMaterial.shader);
        borderArc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        borderArc.receiveShadows = false;
    }

    private void LateUpdate()
    {
        UpdateColorFromAI();
        BuildConeMesh();
    }

    private void UpdateColorFromAI()
    {
        if (robotAI == null) return;

        Color targetColor = patrolColor;
        switch (robotAI.CurrentState)
        {
            case RobotAI.RobotState.Patrol:
            case RobotAI.RobotState.Inactive:
            case RobotAI.RobotState.Return:
                targetColor = patrolColor;
                break;
            case RobotAI.RobotState.Detect:
            case RobotAI.RobotState.Search:
                targetColor = detectColor;
                break;
            case RobotAI.RobotState.Chase:
            case RobotAI.RobotState.Attack:
                targetColor = chaseColor;
                break;
        }

        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * 8f);
        UpdateMaterialColor(currentColor);
    }

    public void SetConeColor(Color color)
    {
        currentColor = color;
        UpdateMaterialColor(color);
    }

    private void UpdateMaterialColor(Color color)
    {
        if (coneMaterial != null) coneMaterial.color = color;
        if (sweepLine != null && sweepLine.material != null)
        {
            Color brightCol = color; brightCol.a = 0.9f;
            sweepLine.material.color = brightCol;
        }
        if (borderArc != null && borderArc.material != null)
        {
            Color borderCol = color; borderCol.a = 0.75f;
            borderArc.material.color = borderCol;
        }
    }

    private void BuildConeMesh()
    {
        Vector3 origin = transform.position;
        // Bắt đầu từ vị trí mặt đất có offset nhẹ
        origin.y = groundOffset;

        int numVertices = rayCount + 2;
        Vector3[] vertices = new Vector3[numVertices];
        int[] triangles = new int[rayCount * 3];
        Vector2[] uv = new Vector2[numVertices];
        Vector3[] arcPoints = new Vector3[rayCount + 1];

        // Đỉnh trung tâm ở gốc robot
        vertices[0] = transform.InverseTransformPoint(origin);
        uv[0] = new Vector2(0.5f, 0f);

        float currentAngle = -fov * 0.5f;
        float angleStep = fov / rayCount;

        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 dir = Quaternion.Euler(0, currentAngle, 0) * transform.forward;
            Vector3 worldTarget = origin + dir * viewDistance;

            // Raycast kiểm tra va chạm tường/chướng ngại vật
            Ray ray = new Ray(origin + Vector3.up * 0.5f, dir);
            if (Physics.Raycast(ray, out RaycastHit hit, viewDistance, obstacleMask))
            {
                worldTarget = hit.point;
                worldTarget.y = groundOffset;
            }

            arcPoints[i] = worldTarget;
            vertices[i + 1] = transform.InverseTransformPoint(worldTarget);
            uv[i + 1] = new Vector2((float)i / rayCount, 1f);

            if (i < rayCount)
            {
                triangles[i * 3 + 0] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            currentAngle += angleStep;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        // Cập nhật viền cánh quạt
        if (borderArc != null)
        {
            borderArc.positionCount = arcPoints.Length;
            borderArc.SetPositions(arcPoints);
        }

        // Cập nhật tia quét radar chuyển động qua lại
        if (sweepLine != null && enableSweepAnimation)
        {
            float sweepAngle = Mathf.Sin(Time.time * sweepSpeed) * (fov * 0.5f);
            Vector3 sweepDir = Quaternion.Euler(0, sweepAngle, 0) * transform.forward;
            Vector3 sweepTarget = origin + sweepDir * viewDistance;

            Ray ray = new Ray(origin + Vector3.up * 0.5f, sweepDir);
            if (Physics.Raycast(ray, out RaycastHit hit, viewDistance, obstacleMask))
            {
                sweepTarget = hit.point;
                sweepTarget.y = groundOffset;
            }

            sweepLine.SetPosition(0, origin);
            sweepLine.SetPosition(1, sweepTarget);
        }
    }
}
