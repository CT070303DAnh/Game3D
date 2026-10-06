using UnityEngine;

/// <summary>
/// RooftopStairsTrigger: Kích hoạt khi người chơi leo lên đỉnh cầu thang nóc nhà sân đỗ.
/// Hoàn thành các nhiệm vụ né tránh và tiếp cận trực thăng.
/// </summary>
public class RooftopStairsTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;
        ObjectiveManager.Instance?.CompleteObjective("avoid_robots");
        ObjectiveManager.Instance?.CompleteObjective("climb_stairs");

        bool hasFuel = GameState.Instance != null && GameState.Instance.JetFuelCollected;
        bool hasRadar = GameState.Instance != null && GameState.Instance.RadarActivated;
        if (hasFuel && hasRadar)
        {
            NotificationUI.ShowMessage("✓ BẠN ĐÃ LÊN ĐẾN NÓC NHÀ! Tiếp cận Trực thăng và bấm [E] để Nạp Nhiên Liệu & Tẩu Thoát!");
        }
        else
        {
            NotificationUI.ShowMessage("✓ BẠN ĐÃ LÊN ĐẾN NÓC NHÀ! Lưu ý: Trực thăng cần Bình Nhiên Liệu (Kho Tiếp Liệu) & Tín hiệu Radar (Tháp Điều Khiển) để bay!");
        }
    }
}
