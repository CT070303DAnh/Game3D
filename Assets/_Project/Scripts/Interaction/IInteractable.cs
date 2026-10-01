/// <summary>
/// IInteractable: Interface cho moi object co the tuong tac.
/// Door, Terminal, Generator, PickupItem, KeycardReader deu implement interface nay.
/// PlayerInteraction chi can biet IInteractable, khong can biet loai object cu the.
/// </summary>
public interface IInteractable
{
    /// <summary>Duoc goi khi player nhan nut Interact.</summary>
    void Interact();

    /// <summary>Text hien thi tren interact prompt (vi du: "Open Door", "Pick Up Fuse").</summary>
    string InteractPromptText { get; }

    /// <summary>Player co the tuong tac luc nay khong? (vi du: da unlock chua).</summary>
    bool CanInteract { get; }
}
