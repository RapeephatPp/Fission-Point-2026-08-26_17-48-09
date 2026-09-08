using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TerminalTextAnimation : MonoBehaviour // 🟢 ปรับชื่อคลาสให้ตรงกับ TerminalTextAnimation.cs
{
    public enum TextAnimMode
    {
        DecryptGlitch,      // สุ่มสัญลักษณ์ขยะแล้วค่อยๆ ถอดรหัสเป็นคำจริง
        TerminalCursor,     // พิมพ์ทีละตัวพร้อมแท่งเคอร์เซอร์กะพริบต่อท้าย
        AlarmFlash,         // กะพริบเตือนภัยสลับสี
        PunchScale          // เด้งขยายกระแทกสายตา
    }

    [Header("Animation Mode")]
    [Tooltip("เลือกรูปแบบแอนิเมชันที่ต้องการ")]
    public TextAnimMode animationMode = TextAnimMode.DecryptGlitch;

    [Header("General Settings")]
    [Tooltip("นับเวลาแม้ Time.timeScale = 0 หรือไม่")]
    public bool useUnscaledTime = true;

    [Header("1. Decrypt Glitch Settings")]
    public float decryptDuration = 0.6f;
    public float glitchInterval = 0.04f;
    private const string GlitchChars = "$#%@&0123456789!?/\\*+=-";

    [Header("2. Terminal Cursor Settings")]
    public float typeSpeed = 0.035f;
    public string cursorChar = "█";
    public float cursorBlinkRate = 0.35f;

    [Header("3. Alarm Flash Settings")]
    public Color flashColor = Color.red;
    public float flashSpeed = 5.0f;
    public int flashCount = 4;

    [Header("4. Punch Scale Settings")]
    public float punchScale = 1.35f;
    public float punchDuration = 0.25f;

    private TextMeshProUGUI tmpText;
    private RectTransform rectTransform;
    private string targetText = "";
    private string currentDisplayedText = "";
    private Color defaultColor;
    private Vector3 originalScale;
    private Coroutine activeAnimCoroutine;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        defaultColor = tmpText.color;
        originalScale = rectTransform.localScale;
    }

    void Update()
    {
        if (tmpText == null) return;

        if (tmpText.text != targetText && tmpText.text != currentDisplayedText)
        {
            targetText = tmpText.text;

            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);

            if (!string.IsNullOrEmpty(targetText))
            {
                activeAnimCoroutine = StartCoroutine(PlaySelectedAnimation(targetText));
            }
        }
    }

    private IEnumerator PlaySelectedAnimation(string fullText)
    {
        rectTransform.localScale = originalScale;
        tmpText.color = defaultColor;

        switch (animationMode)
        {
            case TextAnimMode.DecryptGlitch:
                yield return StartCoroutine(DecryptGlitchRoutine(fullText));
                break;

            case TextAnimMode.TerminalCursor:
                yield return StartCoroutine(TerminalCursorRoutine(fullText));
                break;

            case TextAnimMode.AlarmFlash:
                yield return StartCoroutine(AlarmFlashRoutine(fullText));
                break;

            case TextAnimMode.PunchScale:
                yield return StartCoroutine(PunchScaleRoutine(fullText));
                break;
        }
    }

    private IEnumerator DecryptGlitchRoutine(string fullText)
    {
        int length = fullText.Length;
        char[] displayChars = new char[length];
        float elapsed = 0f;

        while (elapsed < decryptDuration)
        {
            float progress = elapsed / decryptDuration;
            int lockedCount = Mathf.FloorToInt(progress * length);

            for (int i = 0; i < length; i++)
            {
                if (i < lockedCount || char.IsWhiteSpace(fullText[i]))
                {
                    displayChars[i] = fullText[i];
                }
                else
                {
                    displayChars[i] = GlitchChars[Random.Range(0, GlitchChars.Length)];
                }
            }

            currentDisplayedText = new string(displayChars);
            tmpText.text = currentDisplayedText;

            float wait = glitchInterval;
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(wait);
            else yield return new WaitForSeconds(wait);

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        currentDisplayedText = fullText;
        tmpText.text = currentDisplayedText;
    }

    private IEnumerator TerminalCursorRoutine(string fullText)
    {
        tmpText.text = cursorChar;
        currentDisplayedText = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            currentDisplayedText += fullText[i];
            tmpText.text = currentDisplayedText + cursorChar;

            if (useUnscaledTime) yield return new WaitForSecondsRealtime(typeSpeed);
            else yield return new WaitForSeconds(typeSpeed);
        }

        float timer = 0f;
        bool cursorVisible = true;
        while (true)
        {
            timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (timer >= cursorBlinkRate)
            {
                timer = 0f;
                cursorVisible = !cursorVisible;
                tmpText.text = cursorVisible ? currentDisplayedText + cursorChar : currentDisplayedText;
            }
            yield return null;
        }
    }

    private IEnumerator AlarmFlashRoutine(string fullText)
    {
        currentDisplayedText = fullText;
        tmpText.text = fullText;

        float timer = 0f;
        int completedFlashes = 0;

        while (flashCount == 0 || completedFlashes < flashCount)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timer += dt * flashSpeed;

            float t = (Mathf.Sin(timer * Mathf.PI * 2f) + 1f) * 0.5f;
            tmpText.color = Color.Lerp(defaultColor, flashColor, t);

            if (flashCount > 0 && timer >= 1f)
            {
                timer -= 1f;
                completedFlashes++;
            }

            yield return null;
        }

        tmpText.color = defaultColor;
    }

    private IEnumerator PunchScaleRoutine(string fullText)
    {
        currentDisplayedText = fullText;
        tmpText.text = fullText;

        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = elapsed / punchDuration;

            float scale = Mathf.LerpUnclamped(punchScale, 1f, t * t * (2.5f * t - 1.5f) + 1f);
            rectTransform.localScale = originalScale * scale;
            yield return null;
        }

        rectTransform.localScale = originalScale;
    }

    void OnDisable()
    {
        if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
        if (rectTransform != null) rectTransform.localScale = originalScale;
        if (tmpText != null) tmpText.color = defaultColor;
        targetText = "";
        currentDisplayedText = "";
    }
}