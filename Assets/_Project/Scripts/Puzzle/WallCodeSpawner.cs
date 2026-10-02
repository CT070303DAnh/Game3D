using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// WallCodeSpawner: Dat ngau nhien 3 con so (1, 4, 2) len cac buc tuong khac nhau trong Man 2.
/// Mat khau dung de mo thang may: "2 1 4"
/// Vi tri thu 1: So 2 (Ky hieu ①)
/// Vi tri thu 2: So 1 (Ky hieu ②)
/// Vi tri thu 3: So 4 (Ky hieu ③)
/// </summary>
public class WallCodeSpawner : MonoBehaviour
{
    [Header("Wall Anchor Points (Cac vi tri tren tuong)")]
    [SerializeField] private Transform[] wallPoints;

    private void Start()
    {
        SpawnCluesOnWalls();
    }

    public void SpawnCluesOnWalls()
    {
        if (wallPoints == null || wallPoints.Length < 3)
        {
            Debug.LogWarning("[WallCodeSpawner] Can it nhat 3 diem tren tuong de dat so.");
            return;
        }

        // Shuffle danh sach vi tri tuong
        List<Transform> availablePoints = new List<Transform>(wallPoints);
        for (int i = 0; i < availablePoints.Count; i++)
        {
            int rnd = Random.Range(i, availablePoints.Count);
            var temp = availablePoints[i];
            availablePoints[i] = availablePoints[rnd];
            availablePoints[rnd] = temp;
        }

        // 3 con so de tao thanh mat ma 2 1 4:
        // So 2 (Vi tri thu 1: ①)
        CreateClueAtPoint(availablePoints[0], "2", 1, "①", new Color(0.1f, 0.85f, 1f));

        // So 1 (Vi tri thu 2: ②)
        CreateClueAtPoint(availablePoints[1], "1", 2, "②", new Color(1f, 0.85f, 0.1f));

        // So 4 (Vi tri thu 3: ③)
        CreateClueAtPoint(availablePoints[2], "4", 3, "③", new Color(0.2f, 1f, 0.4f));

        Debug.Log("[WallCodeSpawner] Da spawn ngau nhien 3 con so (2, 1, 4) len cac tuong phong.");
    }

    private void CreateClueAtPoint(Transform pt, string digit, int order, string label, Color color)
    {
        if (pt == null) return;

        // Xoa cac clue cu neu co
        for (int i = pt.childCount - 1; i >= 0; i--)
        {
            Destroy(pt.GetChild(i).gameObject);
        }

        GameObject clueGO = new GameObject($"WallClue_{digit}");
        clueGO.transform.SetParent(pt, false);
        clueGO.transform.localPosition = Vector3.zero;
        clueGO.transform.localRotation = Quaternion.identity;

        // 1. Bang kim loai nen toi de chu so noi bat
        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "BackingBoard";
        board.transform.SetParent(clueGO.transform, false);
        board.transform.localPosition = new Vector3(0, 0, 0.02f);
        board.transform.localScale = new Vector3(1.5f, 1.5f, 0.04f);
        var bMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bMat.color = new Color(0.08f, 0.08f, 0.10f);
        board.GetComponent<Renderer>().material = bMat;

        // Vien khung phan quang
        var border = GameObject.CreatePrimitive(PrimitiveType.Cube);
        border.name = "FrameBorder";
        border.transform.SetParent(clueGO.transform, false);
        border.transform.localPosition = new Vector3(0, 0, 0.01f);
        border.transform.localScale = new Vector3(1.6f, 1.6f, 0.02f);
        var fMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        fMat.color = color;
        fMat.EnableKeyword("_EMISSION");
        fMat.SetColor("_EmissionColor", color * 1.2f);
        border.GetComponent<Renderer>().material = fMat;

        // 2. Collider tuong tac
        BoxCollider box = clueGO.AddComponent<BoxCollider>();
        box.size = new Vector3(2.0f, 2.0f, 1.5f);
        box.center = new Vector3(0, 0, 0.6f);
        box.isTrigger = true;

        // 3. TextMesh chu so (Quay mat ra huong forward phong)
        GameObject textGO = new GameObject("DigitText");
        textGO.transform.SetParent(clueGO.transform, false);
        textGO.transform.localPosition = new Vector3(0, 0, 0.06f);
        textGO.transform.localRotation = Quaternion.Euler(0, 180f, 0); // Quay mat ra ngoai
        textGO.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);

        TextMesh tm = textGO.AddComponent<TextMesh>();
        tm.text = $"{label}\n{digit}";
        tm.fontSize = 72;
        tm.characterSize = 1f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = color;
        tm.fontStyle = FontStyle.Bold;

        // 4. Den Neon chieu sang so
        GameObject lightGO = new GameObject("NeonGlow");
        lightGO.transform.SetParent(clueGO.transform, false);
        lightGO.transform.localPosition = new Vector3(0, 0, 0.5f);
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = 4.0f;
        l.intensity = 2.0f;

        // 5. Gan script WallCodeClue
        WallCodeClue clueScript = clueGO.AddComponent<WallCodeClue>();
        clueScript.SetupClue(digit, order, label, color);
    }
}
