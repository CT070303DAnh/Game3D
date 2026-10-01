using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// InventoryUI: Hien thi danh sach cac vat pham (items) da nhat duoc.
/// Bam TAB hoac nut tui do de mo/tat.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Text itemListText;
    [SerializeField] private Button toggleButton;

    private bool isOpen = false;

    private void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleInventory);

        UpdateInventoryText();
    }

    private void OnEnable()
    {
        GameState.OnSecurityCardCollected += UpdateInventoryText;
        GameState.OnFuseCollected += UpdateInventoryText;
        GameState.OnAccessCodeSolved += UpdateInventoryText; // Hoac bat cu event nhat do nao khac
    }

    private void OnDisable()
    {
        GameState.OnSecurityCardCollected -= UpdateInventoryText;
        GameState.OnFuseCollected -= UpdateInventoryText;
        GameState.OnAccessCodeSolved -= UpdateInventoryText;
    }

    private void Update()
    {
        // Phim I hoac TAB tren PC de mo tui do
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isOpen);
            if (isOpen)
            {
                UpdateInventoryText();
                
                // Hien con tro chuot neu dang FPS/PUBG de co the tat (neu can)
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }
    }

    public void UpdateInventoryText()
    {
        if (itemListText == null || GameState.Instance == null) return;

        string txt = "";
        
        if (GameState.Instance.SecurityCardCollected)
            txt += "• Thẻ Bảo Mật\n";
        
        if (GameState.Instance.FuseCollected)
            txt += "• Cầu Chì\n";
        
        if (GameState.Instance.AccessCodeFound)
            txt += "• Ghi Chú Mã Truy Cập\n";
            
        if (GameState.Instance.LaboratoryKeyCollected)
            txt += "• Chìa Khóa Phòng Thí Nghiệm\n";
            
        if (string.IsNullOrEmpty(txt))
            txt = "<i>Túi đồ trống</i>";

        itemListText.text = txt;
    }
}
