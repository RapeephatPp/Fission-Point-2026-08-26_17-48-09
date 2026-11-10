using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[ExecuteAlways]
public class MinigameSimpleBorder : MonoBehaviour
{
    [System.Serializable]
    public class BorderProfile
    {
        public string minigameName = "Minigame";
        [Tooltip("GameObject หรือ Panel ของมินิเกม")]
        public GameObject minigamePanel;
        [Tooltip("Transform ที่ใช้อ้างอิงตำแหน่งและขนาดเมื่อกดปุ่ม Snap (ถ้ามี)")]
        public RectTransform referenceTransform;

        [Header("Transform (พิกัดและขนาดของกรอบ)")]
        public Vector2 anchoredPosition = Vector2.zero;
        public Vector2 size = new Vector2(300f, 200f);

        [Header("Appearance")]
        public Color borderColor = new Color(1.0f, 0.85f, 0.1f, 1.0f); // Warning Yellow
    }

    [Header("Blink & Auto Fade-Out (กะพริบนำสายตาแล้วดับ)")]
    [Tooltip("เปิดใช้งานโหมดกะพริบเตือนแล้วค่อยๆ ดับหายไปเอง")]
    public bool autoFadeOut = true;
    [Tooltip("จำนวนครั้งที่ให้กะพริบเตือน (ค่าแนะนำ: 2 - 3 ครั้ง)")]
    [Range(1, 5)] public int blinkCount = 3;

    [Header("Smooth Blink Timing (จังหวะเวลานุ่มนวล)")]
    [Tooltip("หน่วงเวลาก่อนเริ่มกะพริบ เพื่อให้กล้องซูมห้องควบคุมเข้าที่ก่อน (วินาที)")]
    [Range(0f, 0.5f)] public float startDelay = 0.15f;
    [Tooltip("ระยะเวลาสว่างขึ้นแบบ Ease-In-Out ในแต่ละครั้ง (วินาที)")]
    [Range(0.08f, 0.4f)] public float flashInDuration = 0.18f;
    [Tooltip("ระยะเวลาค้างสว่างที่จุดสูงสุด (วินาที)")]
    [Range(0f, 0.25f)] public float flashHoldDuration = 0.06f;
    [Tooltip("ระยะเวลาหรี่ลงแบบ Ease-In-Out ในแต่ละครั้ง (วินาที)")]
    [Range(0.08f, 0.4f)] public float flashOutDuration = 0.18f;
    [Tooltip("ระยะเวลาค่อยๆ ละลายจางหายไปครั้งสุดท้าย (วินาที)")]
    [Range(0.15f, 1.0f)] public float finalFadeOutDuration = 0.40f;
    [Tooltip("ความสว่างต่ำสุดระหว่างแต่ละจังหวะกะพริบ")]
    [Range(0.0f, 0.5f)] public float minBlinkAlpha = 0.20f;

    [Header("Shader Material")]
    public Material borderMaterial;

    [Header("Box SDF Border Settings (ขนาดและขอบ)")]
    [Tooltip("ความหนาของเส้นกรอบหลัก (พิกเซล)")]
    [Range(1f, 20f)] public float borderThickness = 4.0f;
    [Tooltip("รัศมีความโค้งมนของมุมกรอบ (พิกเซล) 0 = เหลี่ยมคม")]
    [Range(0f, 30f)] public float cornerRadius = 6.0f;
    [Tooltip("ระยะเผื่อขอบ เพื่อให้แสงเรืองไม่ถูกตัดขอบสี่เหลี่ยม (พิกเซล)")]
    [Range(0f, 30f)] public float glowPadding = 14.0f;
    [Tooltip("ระยะการฟุ้งของแสงเรือง (Neon Glow)")]
    [Range(1f, 40f)] public float glowSpread = 14.0f;
    [Tooltip("ความสว่างของแสงเรือง")]
    [Range(0f, 2f)] public float glowIntensity = 0.9f;

    [Header("Continuous Pulse (ใช้เมื่อปิด Auto Fade-Out)")]
    public bool enablePulse = true;
    [Range(0f, 10f)] public float pulseSpeed = 4.0f;
    [Range(0.2f, 1f)] public float minPulseAlpha = 0.65f;
    [Range(0.5f, 1f)] public float maxPulseAlpha = 1.0f;

    [Tooltip("ลำแสงวิ่งรอบกรอบ (Traveling Dash)")]
    [Range(0f, 1f)] public float animatedDashIntensity = 0.4f;
    [Range(0f, 10f)] public float dashSpeed = 1.5f;

    [Header("Default Color")]
    public Color defaultBorderColor = new Color(1.0f, 0.85f, 0.1f, 1.0f);

    [Header("Minigame Profiles (แยกตาม 4 มินิเกม)")]
    public BorderProfile[] profiles = new BorderProfile[0];

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private int activeProfileIndex = -1;

    public int ActiveProfileIndex => activeProfileIndex;

    // UI references
    private RectTransform rootRect;
    private CanvasGroup canvasGroup;
    private Image borderImage;
    private Coroutine fadeCoroutine;
    private Color currentColor;
    private bool isShowing = false;

    // Shader Property IDs
    private static readonly int PropRectSize = Shader.PropertyToID("_RectSize");
    private static readonly int PropBorderColor = Shader.PropertyToID("_BorderColor");
    private static readonly int PropThickness = Shader.PropertyToID("_BorderThickness");
    private static readonly int PropCornerRadius = Shader.PropertyToID("_CornerRadius");
    private static readonly int PropPadding = Shader.PropertyToID("_Padding");
    private static readonly int PropGlowSpread = Shader.PropertyToID("_GlowSpread");
    private static readonly int PropGlowIntensity = Shader.PropertyToID("_GlowIntensity");
    private static readonly int PropPulseSpeed = Shader.PropertyToID("_PulseSpeed");
    private static readonly int PropPulseMinAlpha = Shader.PropertyToID("_PulseMinAlpha");
    private static readonly int PropPulseMaxAlpha = Shader.PropertyToID("_PulseMaxAlpha");
    private static readonly int PropAnimatedDash = Shader.PropertyToID("_AnimatedDash");
    private static readonly int PropDashSpeed = Shader.PropertyToID("_DashSpeed");

    private void Awake()
    {
        EnsureBorderUI();
        if (Application.isPlaying && !isShowing)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        EnsureBorderUI();
        UpdateShaderProperties();
    }

    private void OnDisable()
    {
        isShowing = false;
        fadeCoroutine = null;
    }

    private void OnRectTransformDimensionsChange()
    {
        if (rootRect != null && borderImage != null && gameObject.activeInHierarchy)
        {
            Material targetMat = borderImage.material != null ? borderImage.material : borderMaterial;
            if (targetMat != null)
            {
                targetMat.SetVector(PropRectSize, new Vector4(rootRect.rect.width, rootRect.rect.height, 0, 0));
            }
        }
    }

    private void OnValidate()
    {
        UpdateShaderProperties();
    }

    private void ActivateBorderObject()
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (!parent.gameObject.activeSelf) parent.gameObject.SetActive(true);
            parent = parent.parent;
        }

        enabled = true;
        isShowing = true;
        EnsureBorderUI();
        gameObject.SetActive(true);

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    /// <summary>
    /// สั่งแสดงกรอบไฮไลต์ตามมินิเกมที่ระบุ (กะพริบนำสายตาอย่างนุ่มนวลแล้วดับ)
    /// </summary>
    public void ShowBorder(GameObject minigameObj)
    {
        if (minigameObj == null) return;

        ActivateBorderObject();

        int profileIdx = FindProfileIndex(minigameObj);
        ApplyProfile(profileIdx);
        if (borderImage != null)
        {
            borderImage.SetVerticesDirty();
            borderImage.SetMaterialDirty();
        }

        if (gameObject.activeInHierarchy)
        {
            if (autoFadeOut)
            {
                fadeCoroutine = StartCoroutine(BlinkSequenceRoutine());
            }
            else
            {
                fadeCoroutine = StartCoroutine(FadeRoutine(1f, 0.25f));
            }
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    /// <summary>
    /// สั่งเริ่มแอนิเมชันกะพริบสำหรับ Preview
    /// </summary>
    public void PlayBlinkPreview()
    {
        ActivateBorderObject();
        UpdateShaderProperties();
        if (borderImage != null)
        {
            borderImage.SetVerticesDirty();
            borderImage.SetMaterialDirty();
        }
        if (gameObject.activeInHierarchy)
        {
            fadeCoroutine = StartCoroutine(BlinkSequenceRoutine());
        }
    }

    /// <summary>
    /// สั่งซ่อนกรอบไฮไลต์อย่างนุ่มนวลเมื่อเล่นจบ
    /// </summary>
    public void HideBorder()
    {
        isShowing = false;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (!gameObject.activeInHierarchy)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            activeProfileIndex = -1;
            return;
        }

        fadeCoroutine = StartCoroutine(FadeRoutine(0f, 0.25f, () =>
        {
            activeProfileIndex = -1;
            fadeCoroutine = null;
            gameObject.SetActive(false);
        }));
    }

    /// <summary>
    /// ฟังก์ชัน Cosine Ease-In-Out ให้ความโค้งมนของแสงนุ่มนวลไร้รอยหักมุม
    /// </summary>
    public static float SmoothEase(float t)
    {
        t = Mathf.Clamp01(t);
        return 0.5f * (1f - Mathf.Cos(t * Mathf.PI));
    }

    /// <summary>
    /// แอนิเมชันกะพริบเตือน N ครั้ง แบบ Smooth Sinusoidal Curve แล้วค่อยๆ ละลายดับ
    /// </summary>
    private IEnumerator BlinkSequenceRoutine()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = 0f;
        UpdateShaderProperties();

        // 1. หน่วงเวลาก่อนเริ่มเล็กน้อย (รอกล้องซูมเข้ามินิเกมให้นิ่ง)
        if (startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(startDelay);
        }
        UpdateShaderProperties();

        for (int i = 0; i < blinkCount; i++)
        {
            // 2. ค่อยๆ สว่างขึ้นด้วย Cosine Ease-In-Out
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            while (elapsed < flashInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = flashInDuration > 0f ? (elapsed / flashInDuration) : 1f;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, SmoothEase(t));
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // 3. ค้างสว่างเต็มที่ชั่วครู่ให้สายตาจับโฟกัสชัดเจน
            if (flashHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(flashHoldDuration);
            }

            // 4. ค่อยๆ หรี่ลง (หรือค่อยๆ ละลายดับหายไปสำหรับรอบสุดท้าย)
            bool isLast = (i == blinkCount - 1);
            float targetAlpha = isLast ? 0f : minBlinkAlpha;
            float duration = isLast ? finalFadeOutDuration : flashOutDuration;

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration > 0f ? (elapsed / duration) : 1f;
                float eased = isLast ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)) : SmoothEase(t);
                canvasGroup.alpha = Mathf.Lerp(1f, targetAlpha, eased);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
        }

        // จบการทำงานและซ่อน GameObject อย่างนุ่มนวล
        canvasGroup.alpha = 0f;
        activeProfileIndex = -1;
        isShowing = false;
        fadeCoroutine = null;
        gameObject.SetActive(false);
    }

    public void ApplyProfile(int index)
    {
        activeProfileIndex = index;
        if (profiles == null || index < 0 || index >= profiles.Length) return;

        BorderProfile p = profiles[index];
        if (p == null) return;

        SetPositionAndSize(p.anchoredPosition, p.size);
        SetBorderColor(p.borderColor);
    }

    public void SetPositionAndSize(Vector2 pos, Vector2 size)
    {
        EnsureBorderUI();
        if (rootRect == null) return;

        rootRect.anchoredPosition = pos;
        rootRect.sizeDelta = size;

        UpdateShaderProperties();
        if (borderImage != null)
        {
            borderImage.SetVerticesDirty();
            borderImage.SetMaterialDirty();
        }
    }

    public void SetBorderColor(Color col)
    {
        currentColor = col;
        UpdateShaderProperties();
    }

    public void SaveCurrentRectToActiveProfile()
    {
        if (rootRect == null) rootRect = GetComponent<RectTransform>();
        if (activeProfileIndex >= 0 && profiles != null && activeProfileIndex < profiles.Length)
        {
            BorderProfile p = profiles[activeProfileIndex];
            if (p != null)
            {
                p.anchoredPosition = rootRect.anchoredPosition;
                p.size = rootRect.sizeDelta;
            }
        }
    }

    public void SnapProfileFromReference(int index)
    {
        if (profiles == null || index < 0 || index >= profiles.Length) return;
        BorderProfile p = profiles[index];
        if (p == null || p.referenceTransform == null) return;

        EnsureBorderUI();
        if (rootRect == null || rootRect.parent == null) return;

        Vector3 worldPos = p.referenceTransform.position;
        Vector2 localPos = rootRect.parent.InverseTransformPoint(worldPos);
        p.anchoredPosition = localPos;
        p.size = p.referenceTransform.rect.size + new Vector2(glowPadding * 2f, glowPadding * 2f);

        if (activeProfileIndex == index)
        {
            ApplyProfile(index);
        }
    }

    public void UpdateShaderProperties()
    {
        EnsureBorderUI();
        if (borderImage == null || rootRect == null) return;

        Material targetMat = borderImage.material != null ? borderImage.material : borderMaterial;
        if (targetMat == null) return;

        targetMat.SetVector(PropRectSize, new Vector4(rootRect.rect.width, rootRect.rect.height, 0, 0));
        targetMat.SetColor(PropBorderColor, currentColor);
        targetMat.SetFloat(PropThickness, borderThickness);
        targetMat.SetFloat(PropCornerRadius, cornerRadius);
        targetMat.SetFloat(PropPadding, glowPadding);
        targetMat.SetFloat(PropGlowSpread, glowSpread);
        targetMat.SetFloat(PropGlowIntensity, glowIntensity);

        // ปิดการกะพริบใน Shader ขณะใช้โหมด Auto Fade-Out เพื่อไม่ให้คลื่นแสง 2 ตัวตีกันจนกระตุก
        bool useShaderPulse = enablePulse && !autoFadeOut;
        targetMat.SetFloat(PropPulseSpeed, useShaderPulse ? pulseSpeed : 0f);
        targetMat.SetFloat(PropPulseMinAlpha, useShaderPulse ? minPulseAlpha : 1.0f);
        targetMat.SetFloat(PropPulseMaxAlpha, useShaderPulse ? maxPulseAlpha : 1.0f);

        targetMat.SetFloat(PropAnimatedDash, animatedDashIntensity);
        targetMat.SetFloat(PropDashSpeed, dashSpeed);
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, System.Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float start = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        fadeCoroutine = null;
        onComplete?.Invoke();
    }

    public int FindProfileIndex(GameObject minigameObj)
    {
        if (profiles == null || minigameObj == null) return -1;

        for (int i = 0; i < profiles.Length; i++)
        {
            if (profiles[i] == null) continue;
            if (profiles[i].minigamePanel == minigameObj ||
                profiles[i].minigameName.Equals(minigameObj.name, System.StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    public void EnsureBorderUI()
    {
        if (rootRect == null) rootRect = GetComponent<RectTransform>();
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.enabled = true;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // ลบ GameObject ขอบ 4 ด้านเก่าออก (ถ้ามี)
        string[] oldLines = new string[] { "Border_Top", "Border_Bottom", "Border_Left", "Border_Right" };
        foreach (string line in oldLines)
        {
            Transform t = transform.Find(line);
            if (t != null)
            {
                if (Application.isPlaying) Destroy(t.gameObject);
                else DestroyImmediate(t.gameObject);
            }
        }

        if (borderMaterial == null)
        {
#if UNITY_EDITOR
            borderMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/MinigameBorderMat.mat");
#endif
            if (borderMaterial == null)
            {
                Shader s = Shader.Find("UI/MinigameBorder");
                if (s != null) borderMaterial = new Material(s);
            }
        }

        if (borderImage == null)
        {
            borderImage = GetComponent<Image>();
            if (borderImage == null) borderImage = gameObject.AddComponent<Image>();
        }

        borderImage.enabled = true;
        borderImage.raycastTarget = false;
        if (borderMaterial != null && borderImage.material != borderMaterial)
        {
            borderImage.material = borderMaterial;
        }

        if (currentColor == default(Color) || currentColor.a <= 0f)
        {
            currentColor = defaultBorderColor;
        }
    }
}
