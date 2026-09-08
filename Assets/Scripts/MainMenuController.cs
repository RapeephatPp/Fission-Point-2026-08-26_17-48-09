using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Loading (ScreenFader)")]
    [Tooltip("ชื่อ Scene เกมหลักที่จะโหลด")]
    public string gameSceneName = "GameScene";
    [Tooltip("Index ของ Scene ใน Build Settings")]
    public int gameSceneIndex = 1;
    [Tooltip("โหลดด้วย Scene Index หรือไม่ (ถ้าไม่ติ๊กจะโหลดด้วยชื่อ Scene)")]
    public bool loadByIndex = false;
    [Tooltip("ระยะเวลาการ Fade ดำตอนเริ่มเกม (วินาที)")]
    public float fadeOutDuration = 0.8f;

    [Header("Title UI & Flickering")]
    public TextMeshProUGUI titleText;
    public bool enableTitleFlicker = true;
    public float minFlickerInterval = 3.0f;
    public float maxFlickerInterval = 8.0f;
    [Range(0f, 0.8f)] public float flickerMinAlpha = 0.15f;

    [Header("Line & Cursor Movement")]
    public RectTransform cursor;
    public float cursorSpeed = 450f;
    public float startPointX = -350f;
    public float endPointX = 350f;
    public bool pingPongMovement = false;

    // 🟢 โซนเป้าหมายสำหรับกะจังหวะกดเริ่มเกมเหมือนในห้องควบคุม
    [Header("Interactive Start Zone (Gameplay Mechanic)")]
    [Tooltip("แถบโซนสีเขียวบนเส้น")]
    public RectTransform startZone;
    [Tooltip("Image ของแถบสีเขียว (ไว้เปลี่ยนสีตอนกดโดน/ไฮไลต์)")]
    public Image startZoneImage;
    [Tooltip("ต้องกะจังหวะกดให้โดนโซนสีเขียวเท่านั้นถึงจะเริ่มเกมได้")]
    public bool mustHitZoneToStart = true;
    [Tooltip("ระยะความเผื่อในการกดโดนโซน")]
    public float zoneHitboxMultiplier = 1.25f;
    public Color zoneNormalColor = new Color(0.2f, 0.85f, 0.35f, 0.8f);
    public Color zoneHitColor = Color.white;

    [Header("Start Prompt UI")]
    public TextMeshProUGUI startPromptText;
    public string promptMessage = "PRESS [SPACE] OR [CLICK] ON THE GREEN ZONE";
    public float promptBlinkSpeed = 3.5f;

    [Header("Juice & Feedback")]
    public float cursorBumpScale = 1.4f;
    public float bumpDuration = 0.12f;
    public Transform shakeTarget;
    public float shakeMagnitude = 8f;

    private bool isStarting = false;
    private Vector3 originalCursorScale;
    private Vector3 originalShakePos;
    private float moveDirection = 1f;
    private Color originalTitleColor;
    private Coroutine titleFlickerCoroutine;
    private Coroutine zoneFlashCoroutine;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (cursor != null)
        {
            originalCursorScale = cursor.localScale;
            cursor.anchoredPosition = new Vector2(startPointX, cursor.anchoredPosition.y);
        }

        if (shakeTarget != null)
        {
            originalShakePos = shakeTarget.localPosition;
        }

        if (startPromptText != null)
        {
            startPromptText.text = promptMessage;
        }

        if (startZoneImage != null)
        {
            startZoneImage.color = zoneNormalColor;
        }

        if (titleText != null)
        {
            originalTitleColor = titleText.color;
            if (enableTitleFlicker)
            {
                titleFlickerCoroutine = StartCoroutine(TitleFlickerRoutine());
            }
        }

        // เล่นเสียงเพลงบรรยากาศพื้นหลัง
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAmbient("ambientLoop");
        }

        // ค่อยๆ สว่างขึ้นเมื่อเข้าหน้าจอเมนูผ่าน ScreenFader
        if (ScreenFader.Instance != null)
        {
            StartCoroutine(ScreenFader.Instance.FadeRoutine(0f));
        }
    }

    void Update()
    {
        MoveCursor();
        AnimatePromptText();

        if (isStarting) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            HandleInputAttempt();
        }
    }

    private void HandleInputAttempt()
    {
        float cursorX = (cursor != null) ? cursor.anchoredPosition.x : 0f;
        bool isHit = IsInsideZone(cursorX, startZone, zoneHitboxMultiplier);

        // ถ้าเปิด mustHitZoneToStart จะต้องกดให้โดนแถบสีเขียวเท่านั้น
        if (mustHitZoneToStart)
        {
            if (isHit)
            {
                StartGameSequence();
            }
            else
            {
                // กดพลาด: เล่นเสียง Miss และหน้าจอสั่นเตือนเบาๆ
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX("missSound");
                }
                StartCoroutine(ShakeRoutine(shakeMagnitude * 0.5f, 0.15f));
            }
        }
        else
        {
            // หากไม่บังคับโซน กดตรงไหนก็เริ่มเกมได้
            StartGameSequence();
        }
    }

    private bool IsInsideZone(float xPos, RectTransform zone, float hitboxMultiplier)
    {
        if (zone == null || zone.rect.width <= 0) return true;
        float halfWidth = (zone.rect.width * hitboxMultiplier) / 2f;
        return xPos >= (zone.anchoredPosition.x - halfWidth) && xPos <= (zone.anchoredPosition.x + halfWidth);
    }

    private void MoveCursor()
    {
        if (cursor == null) return;

        if (!pingPongMovement)
        {
            cursor.anchoredPosition += new Vector2(cursorSpeed * Time.deltaTime, 0f);
            if (cursor.anchoredPosition.x >= endPointX)
            {
                cursor.anchoredPosition = new Vector2(startPointX, cursor.anchoredPosition.y);
            }
        }
        else
        {
            cursor.anchoredPosition += new Vector2(cursorSpeed * moveDirection * Time.deltaTime, 0f);
            if (cursor.anchoredPosition.x >= endPointX)
            {
                cursor.anchoredPosition = new Vector2(endPointX, cursor.anchoredPosition.y);
                moveDirection = -1f;
            }
            else if (cursor.anchoredPosition.x <= startPointX)
            {
                cursor.anchoredPosition = new Vector2(startPointX, cursor.anchoredPosition.y);
                moveDirection = 1f;
            }
        }
    }

    private void AnimatePromptText()
    {
        if (startPromptText == null || isStarting) return;

        float alpha = (Mathf.Sin(Time.unscaledTime * promptBlinkSpeed) + 1f) * 0.5f;
        Color c = startPromptText.color;
        c.a = Mathf.Lerp(0.25f, 1f, alpha);
        startPromptText.color = c;
    }

    private IEnumerator TitleFlickerRoutine()
    {
        while (enableTitleFlicker && titleText != null && !isStarting)
        {
            float waitTime = Random.Range(minFlickerInterval, maxFlickerInterval);
            yield return new WaitForSecondsRealtime(waitTime);

            if (isStarting) yield break;

            int flickerBursts = Random.Range(2, 5);
            for (int i = 0; i < flickerBursts; i++)
            {
                Color dimColor = originalTitleColor;
                dimColor.a = Random.Range(flickerMinAlpha, 0.45f);
                titleText.color = dimColor;

                yield return new WaitForSecondsRealtime(Random.Range(0.03f, 0.07f));

                titleText.color = originalTitleColor;
                yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.1f));
            }

            titleText.color = originalTitleColor;
        }
    }

    private void StartGameSequence()
    {
        isStarting = true;

        if (titleFlickerCoroutine != null) StopCoroutine(titleFlickerCoroutine);
        if (titleText != null) titleText.color = originalTitleColor;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("hitSound");
            AudioManager.Instance.PlaySFX("clickSound");
        }

        if (startZoneImage != null)
        {
            if (zoneFlashCoroutine != null) StopCoroutine(zoneFlashCoroutine);
            zoneFlashCoroutine = StartCoroutine(FlashZoneRoutine());
        }

        StartCoroutine(TriggerCursorBump());
        StartCoroutine(ShakeRoutine(shakeMagnitude, 0.3f));
        StartCoroutine(TransitionToGameRoutine());
    }

    private IEnumerator FlashZoneRoutine()
    {
        startZoneImage.color = zoneHitColor;
        float elapsed = 0f;
        float dur = 0.25f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            startZoneImage.color = Color.Lerp(zoneHitColor, zoneNormalColor, elapsed / dur);
            yield return null;
        }
        startZoneImage.color = zoneNormalColor;
    }

    private IEnumerator TriggerCursorBump()
    {
        if (cursor == null) yield break;

        float elapsed = 0f;
        while (elapsed < bumpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / bumpDuration;
            cursor.localScale = Vector3.Lerp(originalCursorScale * cursorBumpScale, originalCursorScale, t);
            yield return null;
        }
        cursor.localScale = originalCursorScale;
    }

    private IEnumerator ShakeRoutine(float magnitude, float duration)
    {
        if (shakeTarget == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            shakeTarget.localPosition = originalShakePos + new Vector3(x, y, 0);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        shakeTarget.localPosition = originalShakePos;
    }

    // 🟢 ตัดฉากเข้าเกมโดยใช้ ScreenFader
    private IEnumerator TransitionToGameRoutine()
    {
        if (startPromptText != null)
        {
            Color c = startPromptText.color;
            c.a = 1f;
            startPromptText.color = c;
            startPromptText.text = "STABILIZATION SEQUENCE INITIALIZED...";
        }

        yield return new WaitForSecondsRealtime(0.4f);

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.fadeDuration = fadeOutDuration;

            if (loadByIndex)
            {
                // ใช้ฟังก์ชัน FadeToScene ของ ScreenFader โดยตรง
                ScreenFader.Instance.FadeToScene(gameSceneIndex);
            }
            else
            {
                // Fade จอดำแล้วโหลดผ่านชื่อ Scene
                yield return StartCoroutine(ScreenFader.Instance.FadeRoutine(1.0f));
                SceneManager.LoadScene(gameSceneName);
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.5f);
            if (loadByIndex) SceneManager.LoadScene(gameSceneIndex);
            else SceneManager.LoadScene(gameSceneName);
        }
    }
}