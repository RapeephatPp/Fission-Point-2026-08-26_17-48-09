using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SlidingTextEffect : MonoBehaviour
{
    [Header("Zone Bounds & Masking")]
    [Tooltip("กำหนดกรอบโซนด้วยตัวเอง (ถ้าเว้นว่าง ระบบจะใช้ขนาดดั้งเดิมของ Text สร้างกรอบตัดให้อัตโนมัติ)")]
    public RectTransform customZoneRect;

    [Tooltip("ความกว้างของโซนแสดงผล (หากตั้งเป็น 0 จะใช้ความกว้างของ RectTransform หรือ Zone อัตโนมัติ)")]
    public float overrideZoneWidth = 0f;

    [Tooltip("สร้างกรอบตัดขอบ (RectMask2D) อัตโนมัติ เพื่อไม่ให้ตัวหนังสือแลบออกนอกกล่อง")]
    public bool autoCreateMaskZone = true;

    [Header("Slide Settings")]
    [Tooltip("ความเร็วในการเลื่อนไปทางขวา (พิกเซลต่อวินาที)")]
    public float slideSpeed = 120f;

    [Tooltip("วนลูปข้อความซ้ำเรื่อยๆ จนกว่ามินิเกมจะจบหรือเปลี่ยนข้อความ")]
    public bool loop = true;

    [Tooltip("ระยะเวลารอก่อนเริ่มวนรอบใหม่ (วินาที)")]
    public float loopDelay = 0.6f;

    [Tooltip("นับเวลาแม้ Time.timeScale = 0 หรือไม่")]
    public bool useUnscaledTime = true;

    private TextMeshProUGUI tmpText;
    private RectTransform rectTransform;
    private RectTransform activeZoneRect;
    private string lastText = "";
    private Coroutine slideCoroutine;
    private Vector2 originalPos;
    private bool isInitialized = false;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        originalPos = rectTransform.anchoredPosition;

        // ป้องกันข้อความตัดขึ้นบรรทัดใหม่ขณะสไลด์
        tmpText.enableWordWrapping = false;
        tmpText.overflowMode = TextOverflowModes.Overflow;

        InitializeZone();
    }

    private void InitializeZone()
    {
        if (isInitialized) return;

        if (customZoneRect != null)
        {
            activeZoneRect = customZoneRect;
            EnsureMaskOnZone(activeZoneRect.gameObject);
        }
        else if (autoCreateMaskZone)
        {
            // ตรวจสอบว่า Parent มี Mask อยู่แล้วหรือไม่
            if (transform.parent != null && transform.parent.GetComponent<RectMask2D>() != null)
            {
                activeZoneRect = transform.parent.GetComponent<RectTransform>();
            }
            else
            {
                // สร้าง Viewport ขังตัวหนังสือไว้เฉพาะพื้นที่เดิมของมัน
                GameObject zoneObj = new GameObject(gameObject.name + "_Zone", typeof(RectTransform), typeof(RectMask2D));
                activeZoneRect = zoneObj.GetComponent<RectTransform>();

                // ตั้งพิกัดและขนาดให้ตรงกับ Text ดั้งเดิม
                activeZoneRect.SetParent(transform.parent, false);
                activeZoneRect.SetSiblingIndex(transform.GetSiblingIndex());
                activeZoneRect.anchorMin = rectTransform.anchorMin;
                activeZoneRect.anchorMax = rectTransform.anchorMax;
                activeZoneRect.pivot = rectTransform.pivot;
                activeZoneRect.anchoredPosition = rectTransform.anchoredPosition;

                float zoneW = (overrideZoneWidth > 0f) ? overrideZoneWidth : Mathf.Max(rectTransform.rect.width, 200f);
                float zoneH = Mathf.Max(rectTransform.rect.height, 35f);
                activeZoneRect.sizeDelta = new Vector2(zoneW, zoneH);

                // ย้าย Text เข้าไปอยู่ข้างในโซน
                transform.SetParent(activeZoneRect, false);
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                originalPos = Vector2.zero;
            }
        }
        else if (transform.parent != null)
        {
            activeZoneRect = transform.parent.GetComponent<RectTransform>();
        }

        isInitialized = true;
    }

    private void EnsureMaskOnZone(GameObject zoneObj)
    {
        if (zoneObj.GetComponent<RectMask2D>() == null && zoneObj.GetComponent<Mask>() == null)
        {
            zoneObj.AddComponent<RectMask2D>();
        }
    }

    void Update()
    {
        if (tmpText == null) return;

        // เริ่มแอนิเมชันใหม่เมื่อข้อความเปลี่ยน
        if (tmpText.text != lastText)
        {
            lastText = tmpText.text;

            if (slideCoroutine != null) StopCoroutine(slideCoroutine);

            if (!string.IsNullOrEmpty(tmpText.text))
            {
                slideCoroutine = StartCoroutine(SlideToRightRoutine());
            }
            else
            {
                rectTransform.anchoredPosition = originalPos;
            }
        }
    }

    private IEnumerator SlideToRightRoutine()
    {
        tmpText.ForceMeshUpdate();

        float textWidth = tmpText.preferredWidth;
        float zoneWidth = (overrideZoneWidth > 0f) ? overrideZoneWidth : 
                          ((activeZoneRect != null) ? activeZoneRect.rect.width : 250f);

        // จุดเริ่ม: หลบอยู่ชิดขอบซ้ายในโซน (จะถูก Mask ตัดมองไม่เห็น)
        float startX = -(zoneWidth / 2f) - (textWidth / 2f);
        // จุดจบ: หลบพ้นขอบขวาของโซน
        float endX = (zoneWidth / 2f) + (textWidth / 2f);

        do
        {
            rectTransform.anchoredPosition = new Vector2(startX, originalPos.y);

            while (rectTransform.anchoredPosition.x < endX)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                rectTransform.anchoredPosition += new Vector2(slideSpeed * dt, 0f);
                yield return null;
            }

            rectTransform.anchoredPosition = new Vector2(endX, originalPos.y);

            if (loop)
            {
                yield return new WaitForSecondsRealtime(loopDelay);
            }

        } while (loop && !string.IsNullOrEmpty(tmpText.text));
    }

    void OnDisable()
    {
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        if (rectTransform != null) rectTransform.anchoredPosition = originalPos;
        lastText = "";
    }
}