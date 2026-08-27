using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    [Header("UI Reference")] public CanvasGroup fadeImageGroup;

    [Header("Settings")] public float fadeDuration = 0.5f;

    void Awake()
    {
        // ทำให้ระบบนี้อยู่ข้ามฉากได้ตลอดรอดฝั่ง
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // ถอด Event ออกเมื่อตัวมันโดนทำลาย กัน Error
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // 🟢 ฟังก์ชันนี้จะทำงานอัตโนมัติ ทันทีที่ฉากใหม่โหลดเสร็จ 100%
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (fadeImageGroup != null)
        {
            // บังคับปลดล็อคเมาส์ทันที! (แก้บั๊กคลิกไม่ได้)
            fadeImageGroup.blocksRaycasts = false;

            // สั่งเฟดจอจากมืดเป็นสว่าง
            StartCoroutine(FadeRoutine(0f));
        }
    }

    // 🟢 ใช้ฟังก์ชันนี้ฟังก์ชันเดียวเลย ตอนจะเปลี่ยนฉาก
    public void FadeToScene(int sceneIndex)
    {
        StartCoroutine(FadeAndLoadRoutine(sceneIndex));
    }

    private IEnumerator FadeAndLoadRoutine(int sceneIndex)
    {
        // 1. ล็อคเมาส์ไม่ให้กดปุ่มอื่นมั่วซั่ว และเฟดให้จอดำ
        if (fadeImageGroup != null) fadeImageGroup.blocksRaycasts = true;
        yield return StartCoroutine(FadeRoutine(1f));

        // 2. คืนค่าเวลา (เผื่อหยุดเกมมาจากหน้า Pause)
        Time.timeScale = 1f;

        // 3. 🟢 โหลดฉากใหม่แบบ Async (โหลดอยู่เบื้องหลัง จะไม่ทำให้เกมค้างหรือกระตุก)
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);

        // รอจนกว่าจะโหลดด่านเสร็จ
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // พอโหลดเสร็จ ฟังก์ชัน OnSceneLoaded ด้านบนจะรับช่วงต่อเองครับ!
    }

    public IEnumerator FadeRoutine(float targetAlpha)
    {
        if (fadeImageGroup == null) yield break;

        float startAlpha = fadeImageGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            // ใช้ unscaledDeltaTime เพื่อให้เฟดได้แม้เกมถูก Pause ไว้
            elapsed += Time.unscaledDeltaTime;
            fadeImageGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        fadeImageGroup.alpha = targetAlpha;
    }
}
   