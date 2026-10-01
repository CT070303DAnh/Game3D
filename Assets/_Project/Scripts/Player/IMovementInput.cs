using UnityEngine;

/// <summary>
/// IMovementInput: Interface truu tuong hoa nguon input.
/// PlayerController chi phu thuoc vao interface nay, khong phu thuoc vao UI cu the.
/// De dang swap giua Mobile, Keyboard, Gamepad.
/// </summary>
public interface IMovementInput
{
    /// <summary>Vector di chuyen, magnitude 0..1, relative to world/camera.</summary>
    Vector2 Move { get; }

    /// <summary>True mot frame khi player nhan nut jump.</summary>
    bool JumpPressed { get; }

    /// <summary>True khi player dang giu nut run.</summary>
    bool RunPressed { get; }

    /// <summary>True mot frame khi player nhan nut interact.</summary>
    bool InteractPressed { get; }
}
