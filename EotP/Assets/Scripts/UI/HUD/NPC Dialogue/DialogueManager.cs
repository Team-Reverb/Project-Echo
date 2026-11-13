using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DialogueUI ui;
    [SerializeField] private InteractionDetector detector;
    [SerializeField] private PlayerController player;
    [SerializeField] private Camera playerCamera;

    [Header("Input")]
    [SerializeField] private float inputGraceTime = 0.2f; // System ignores button presses immediately after conversation starts
    [SerializeField] private bool finishLineFirst = true; // True: first press finishes typing the line, second press skips

    public bool IsActive { get; private set; }

    private string[] lines;
    private int lineIndex;
    private NpcInteractionCamera npcCamera;
    private Coroutine cameraRoutine;
    private float acceptInputAt;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (ui == null) ui = FindAnyObjectByType<DialogueUI>();
        if (detector == null) detector = FindAnyObjectByType<InteractionDetector>();
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsActive || Time.time < acceptInputAt) return;
        if (!AnyButtonPressedThisFrame()) return;

        Advance();
    }

    public void StartDialogue(string npcName, string[] dialogueLines, NpcInteractionCamera cam)
    {
        if (IsActive || dialogueLines == null || dialogueLines.Length == 0) return;

        if (ui == null || detector == null || player == null)
        {
            Debug.LogWarning("DialogueManager: Missing a reference (UI, detector or player).");
            return;
        }

        IsActive = true;
        lines = dialogueLines;
        lineIndex = 0;
        npcCamera = cam;
        acceptInputAt = Time.time + inputGraceTime;

        detector.SetLocked(true);
        player.SetInputEnabled(false);

        if (npcCamera != null && playerCamera != null)
        {
            if (cameraRoutine != null) StopCoroutine(cameraRoutine);
            cameraRoutine = StartCoroutine(npcCamera.BeginRoutine(playerCamera));
        }

        ui.Open(npcName);
        ui.ShowLine(lines[lineIndex]);
    }

    void Advance()
    {
        if (finishLineFirst && ui.IsTyping)
        {
            ui.SkipTyping();
            return;
        }

        lineIndex++;
        if (lineIndex >= lines.Length)
        {
            EndDialogue();
            return;
        }

        ui.ShowLine(lines[lineIndex]);
    }

    void EndDialogue()
    {
        IsActive = false;
        ui.Close();

        if (cameraRoutine != null) StopCoroutine(cameraRoutine);
        cameraRoutine = StartCoroutine(EndRoutine());
    }

    IEnumerator EndRoutine()
    {
        if (npcCamera != null && playerCamera != null)
        {
            yield return npcCamera.EndRoutine(playerCamera);
        }
        else
        {
            yield return new WaitForSeconds(0.25f);
        }

        player.SetInputEnabled(true);
        detector.SetLocked(false);
        npcCamera = null;
        cameraRoutine = null;
    }

    static bool AnyButtonPressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) return true;

        Mouse mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)) return true;

        Gamepad pad = Gamepad.current;
        if (pad != null &&
            (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame ||
             pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
             pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame ||
             pad.startButton.wasPressedThisFrame))
        {
            return true;
        }

        return false;
    }
}
