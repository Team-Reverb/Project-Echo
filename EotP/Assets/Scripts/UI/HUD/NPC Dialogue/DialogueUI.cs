using UnityEngine;
using System.Collections;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("Dialogue Panel")]
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject continueIndicator;

    [Header("Normal HUD")]
    [SerializeField] private CanvasGroup hudGroup;

    [Header("Timing")]
    [SerializeField] private float charactersPerSecond = 60f;
    [SerializeField] private float fadeTime = 0.25f;

    private Coroutine typingRoutine;
    private Coroutine panelFade;
    private Coroutine hudFade;

    public bool IsTyping { get; private set; }

    void Awake()
    {
        panelGroup.alpha = 0f;
        panelGroup.blocksRaycasts = false;
        panelGroup.interactable = false;
        continueIndicator.SetActive(false);
    }

    public void Open(string npcName)
    {
        nameText.text = npcName;
        dialogueText.text = "";
        continueIndicator.SetActive(false);

        StartFade(ref panelFade, panelGroup, 1f);
        StartFade(ref hudFade, hudGroup, 0f);
    }

    public void Close()
    {
        StopTyping();

        StartFade(ref panelFade, panelGroup, 0f);
        StartFade(ref hudFade, hudGroup, 1f);
    }

    public void ShowLine(string line)
    {
        StopTyping();
        typingRoutine = StartCoroutine(TypeRoutine(line));
    }

    public void SkipTyping()
    {
        if (!IsTyping) return;

        StopTyping();
        dialogueText.maxVisibleCharacters = int.MaxValue;
        continueIndicator.SetActive(true);
    }

    void StopTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = null;
        IsTyping = false;
    }

    IEnumerator TypeRoutine(string line)
    {
        IsTyping = true;
        if (continueIndicator != null) continueIndicator.SetActive(false);

        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        int total = dialogueText.textInfo.characterCount;

        float visible = 0f;
        while (visible < total)
        {
            visible += charactersPerSecond * Time.deltaTime;
            dialogueText.maxVisibleCharacters = Mathf.Min(Mathf.FloorToInt(visible), total);
            yield return null;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        IsTyping = false;
        typingRoutine = null;
        continueIndicator.SetActive(true);
    }

    void StartFade(ref Coroutine slot, CanvasGroup group, float target)
    {
        if (slot != null) StopCoroutine(slot);
        slot = StartCoroutine(Fade(group, target));
    }

    IEnumerator Fade(CanvasGroup group, float target)
    {
        float start = group.alpha;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, elapsed / fadeTime);
            yield return null;
        }

        group.alpha = target;
    }
}
