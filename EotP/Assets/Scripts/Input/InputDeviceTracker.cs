using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

public enum InputScheme
{
    KeyboardMouse,
    Xbox,
    PlayStation
}

public static class InputDeviceTracker
{
    public static InputScheme Current { get; private set; } = InputScheme.KeyboardMouse;
    public static event Action<InputScheme> SchemeChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        Current = InputScheme.KeyboardMouse;
        InputSystem.onActionChange -= OnActionChange;
        InputSystem.onActionChange += OnActionChange;
    }

    static void OnActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed) return;
        if (obj is not InputAction action) return;

        InputDevice device = action.activeControl?.device;
        if (device == null) return;

        InputScheme scheme = Classify(device);
        if (scheme == Current) return;

        Current = scheme;
        SchemeChanged?.Invoke(scheme);
    }

    static InputScheme Classify(InputDevice device)
    {
        if (device is Keyboard || device is Mouse) return InputScheme.KeyboardMouse;
        if (device is DualShockGamepad) return InputScheme.PlayStation;
        if (device is Gamepad) return InputScheme.Xbox;
        return Current;
    }
}