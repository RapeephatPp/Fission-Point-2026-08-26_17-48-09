using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class WarningImageBlinker : MonoBehaviour
{
    [Header("Target Image")]
    [Tooltip("Image ที่ต้องการให้กะพริบ (ถ้าเว้นว่างจะดึงจาก GameObject นี้อัตโนมัติ)")]
    public Image targetImage;

    [Header("Blink Settings")]
    [Tooltip("สีที่ต้องการให้กะพริบเตือน (เช่น สีแดง หรือสีส้ม)")]
    public Color warningColor = new Color(1f, 0f, 0f, 1f);

    [Tooltip("ความเร็วในการกะพริบ (ค่ายิ่งมากยิ่งกะพริบถี่)")]
    public float blinkSpeed = 6.0f;

    [Tooltip("ค่าความโปร่งใสต่ำสุด (0 = หายไปเลย)")]
    [Range(0f, 1f)]
    public float minAlpha = 0.0f;

    [Tooltip("ค่าความโปร่งใสสูงสุด")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.85f;

    [Tooltip("นับเวลาแม้ Time.timeScale = 0 หรือไม่")]
    public bool useUnscaledTime = true;

    [Header("Auto Trigger (Broadcast Message)")]
    [Tooltip("เริ่มกะพริบทันทีเมื่อมินิเกมส่งข้อความ StartMinigame")]
    public bool triggerOnStartMinigame = true;

    [Tooltip("หยุดกะพริบและซ่อนทันทีเมื่อส่งข้อความ IdleMinigame")]
    public bool stopOnIdleMinigame = true;

    private Coroutine blinkCoroutine;
    private Color defaultColor;
    private bool isBlinking = false;

    void Awake()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
        defaultColor = targetImage.color;
    }

    void OnDisable()
    {
        StopBlink();
    }

    // รองรับ BroadcastMessage จาก ControlRoomManager โดยตรง
    public void StartMinigame()
    {
        if (triggerOnStartMinigame) StartBlink();
    }

    public void IdleMinigame()
    {
        if (stopOnIdleMinigame) StopBlink();
    }

    public void StartBlink()
    {
        if (targetImage == null) return;
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);

        isBlinking = true;
        targetImage.enabled = true;
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    public void StopBlink()
    {
        isBlinking = false;
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        if (targetImage != null)
        {
            Color c = warningColor;
            c.a = 0f;
            targetImage.color = c;
            targetImage.enabled = false;
        }
    }

    private IEnumerator BlinkRoutine()
    {
        Color c = warningColor;
        float timer = 0f;

        while (isBlinking)
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timer += deltaTime * blinkSpeed;

            // คำนวณคลื่น Sine เพื่อให้แสงวาบเข้า-ออกอย่างนุ่มนวล
            float wave = (Mathf.Sin(timer) + 1f) * 0.5f;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, wave);
            targetImage.color = c;

            yield return null;
        }
    }
}