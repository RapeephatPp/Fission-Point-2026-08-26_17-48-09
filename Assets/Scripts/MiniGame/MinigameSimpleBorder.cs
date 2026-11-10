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

    [Header("Animation Settings")]
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
        if (Application.isPlaying)
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

    private void OnValidate()
    {
        UpdateShaderProperties();
    }

    /// <summary>
    /// สั่งแสดงกรอบไฮไลต์ตามมินิเกมที่ระบุ
    /// </summary>
    public void ShowBorder(GameObject minigameObj)
    {
        if (minigameObj == null) return;

        EnsureBorderUI();
        gameObject.SetActive(true);

        int profileIdx = FindProfileIndex(minigameObj);
        ApplyProfile(profileIdx);

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(1f, 0.25f));
    }

    /// <summary>
    /// สั่งซ่อนกรอบไฮไลต์เมื่อเล่นจบ
    /// </summary>
    public void HideBorder()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(0f, 0.2f, () =>
        {
            activeProfileIndex = -1;
            gameObject.SetActive(false);
        }));
    }

    /// <summary>
    /// ใช้งาน Profile ตามลำดับ index โดยไม่มีการ Overwrite ค่าพิกัด
    /// </summary>
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
    }

    public void SetBorderColor(Color col)
    {
        currentColor = col;
        UpdateShaderProperties();
    }

    /// <summary>
    /// บันทึกค่าพิกัดและขนาดของ RectTransform ปัจจุบันลงใน Active Profile (Two-Way Sync)
    /// </summary>
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

    /// <summary>
    /// Snap ตำแหน่งและขนาดของ Profile จาก referenceTransform (เรียกต่อเมื่อผู้ใช้กดสั่งเท่านั้น)
    /// </summary>
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
        targetMat.SetFloat(PropPulseSpeed, enablePulse ? pulseSpeed : 0f);
        targetMat.SetFloat(PropPulseMinAlpha, minPulseAlpha);
        targetMat.SetFloat(PropPulseMaxAlpha, maxPulseAlpha);
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
            canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
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
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

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
