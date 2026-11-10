using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InteractionDetector detector;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;

    private PlayerControls controls;
    private IInteractable current;

    void Awake()
    {
        controls = new PlayerControls();

        if (detector == null) detector = FindAnyObjectByType<InteractionDetector>();
        if (detector == null) Debug.LogWarning("InteractionPromptUI: No InteractionDetector found.");

        Hide();
    }

    void OnEnable()
    {
        if (detector != null) detector.TargetChanged += OnTargetChanged;
        InputDeviceTracker.SchemeChanged += OnSchemeChanged;
    }

    void OnDisable()
    {
        if (detector != null) detector.TargetChanged -= OnTargetChanged;
        InputDeviceTracker.SchemeChanged -= OnSchemeChanged;
    }

    void OnDestroy()
    {
        controls.Dispose();
    }

    void OnTargetChanged(IInteractable target)
    {
        current = target;
        Refresh();
    }

    void OnSchemeChanged(InputScheme scheme)
    {
        Refresh();
    }

    void Refresh()
    {
        if (current == null)
        {
            Hide();
            return;
        }

        string binding = GetBindingLabel(InputDeviceTracker.Current);
        promptText.text = $"Press [{binding}] to {current.Prompt}";
        promptRoot.SetActive(true);
    }

    void Hide()
    {
        if (promptRoot != null) promptRoot.SetActive(false);
    }

    // Binding Labels

    string GetBindingLabel(InputScheme scheme)
    {
        InputAction action = controls.Player.Interact;
        bool wantKeyboard = scheme == InputScheme.KeyboardMouse;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isComposite || binding.isPartOfComposite) continue;

            string path = binding.effectivePath;
            bool isKeyboard = path.StartsWith("<Keyboard>") || path.StartsWith("<Mouse>");
            if (isKeyboard != wantKeyboard) continue;

            if (wantKeyboard)
            {
                return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
            }
            return GamepadLabel(path, scheme);
        }

        return "Interact";
    }

    string GamepadLabel(string path, InputScheme scheme)
    {
        string control = path.Substring(path.LastIndexOf('/') + 1);
        bool ps = scheme == InputScheme.PlayStation;

        switch (control)
        {
            case "buttonSouth": return ps ? "Cross" : "A";
            case "buttonEast": return ps ? "Circle" : "B";
            case "buttonWest": return ps ? "Square" : "X";
            case "buttonNorth": return ps ? "Triangle" : "Y";
            case "leftShoulder": return ps ? "L1" : "LB";
            case "rightShoulder": return ps ? "R1" : "RB";
            case "leftTrigger": return ps ? "L2" : "LT";
            case "rightTrigger": return ps ? "R2" : "RT";
            case "start": return ps ? "Options" : "Menu";
            case "select": return ps ? "Share" : "View";
            default:
                return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
    }

}
