using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SpriteImageLooper : MonoBehaviour
{
    [Header("Image & Sprites")]
    [Tooltip("Image ที่ต้องการเปลี่ยนรูป (ถ้าเว้นว่างไว้จะดึงจาก GameObject นี้อัตโนมัติ)")]
    public Image targetImage;

    [Tooltip("ใส่รูปภาพ Sprite ที่ต้องการให้เล่นวนลูป")]
    public Sprite[] spriteFrames;

    [Header("Settings")]
    [Tooltip("ระยะเวลาในการเปลี่ยนรูปแต่ละภาพ (วินาที)")]
    public float frameInterval = 0.2f;

    [Tooltip("เริ่มเล่นทันทีตอนเกมเริ่มหรือไม่")]
    public bool playOnAwake = true;

    [Tooltip("วนลูปซ้ำเมื่อจบภาพสุดท้ายหรือไม่")]
    public bool loop = true;

    [Tooltip("นับเวลาแม้ตอน Time.timeScale = 0 (เช่น ตอนกด Pause หรืออยู่ในช่วง HitPause)")]
    public bool useUnscaledTime = true;

    private int currentIndex = 0;
    private Coroutine loopCoroutine;
    private bool isPlaying = false;

    void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    void OnEnable()
    {
        if (playOnAwake)
        {
            Play();
        }
    }

    void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        if (spriteFrames == null || spriteFrames.Length == 0) return;

        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
        }

        isPlaying = true;
        loopCoroutine = StartCoroutine(LoopRoutine());
    }

    public void Stop()
    {
        isPlaying = false;
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }
    }

    private IEnumerator LoopRoutine()
    {
        while (isPlaying)
        {
            if (spriteFrames != null && spriteFrames.Length > 0 && targetImage != null)
            {
                targetImage.sprite = spriteFrames[currentIndex];
                currentIndex++;

                if (currentIndex >= spriteFrames.Length)
                {
                    if (loop)
                    {
                        currentIndex = 0;
                    }
                    else
                    {
                        isPlaying = false;
                        yield break;
                    }
                }
            }

            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(frameInterval);
            }
            else
            {
                yield return new WaitForSeconds(frameInterval);
            }
        }
    }
}