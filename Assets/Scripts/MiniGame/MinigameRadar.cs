using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinigameRadar : MonoBehaviour
{
    [Header("References")]
    public ControlRoomManager gameManager;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI hitCountText; 

    [Header("UI Elements")]
    public RectTransform targetRing;     
    public RectTransform shrinkingRing;  
    public Image shrinkingRingImage;     

    // 🟢 ระบบซ่อน/แสดงเรดาร์
    [Header("Visibility / Hide Settings (New!)")]
    [Tooltip("ลาก GameObject ลูกที่เป็นคอนเทนต์รวม/หน้าจอเรดาร์มาใส่ตรงนี้ เพื่อซ่อนทั้งจอตอนไม่ได้เล่น (ถ้าไม่มี ปล่อยว่างได้)")]
    public GameObject radarContent;
    [Tooltip("หรือจะใช้ CanvasGroup คุมการแสดงผลทั้งจอ (ถ้ามี CanvasGroup ใน Object นี้จะดึงให้อัตโนมัติ)")]
    public CanvasGroup radarCanvasGroup;

    [Header("Scaling Fix (สเกล 0.26)")]
    [Tooltip("ติ๊กถูกไว้เพื่อให้อิงสเกลตาม targetRing (0.26) อัตโนมัติ")]
    public bool matchTargetRingScale = true;
    public Vector3 baseScale = Vector3.one;

    [Header("Game Settings")]
    public float timeLimit;        
    public int requiredHits;        

    [Header("Radar Settings")]
    public float startScale = 2.6f;        
    public float targetScale = 1.0f;       
    public float tolerance = 0.22f;       

    public float minShrinkSpeed = 1.4f;  
    public float maxShrinkSpeed = 3.2f;  

    [Header("Delay Settings")]
    public float endMinigameDelay = 0.8f;

    [Header("Colors (Feedback)")]
    public Color normalColor = Color.white;
    public Color hitColor = Color.green;
    public Color missColor = Color.red;

    private int currentHits = 0;
    private float timer = 0f;
    private float currentRingScale = 0f;
    private float currentShrinkSpeed = 0f;

    private bool isGameActive = false;
    private bool isWaitingForNext = false;

    private void Awake()
    {
        CacheBaseScale();

        if (radarCanvasGroup == null)
        {
            radarCanvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void CacheBaseScale()
    {
        if (matchTargetRingScale && targetRing != null)
        {
            baseScale = targetRing.localScale;
        }
        else if (shrinkingRing != null && shrinkingRing.localScale != Vector3.zero)
        {
            baseScale = shrinkingRing.localScale;
        }
    }

    private void OnEnable()
    {        
        IdleMinigame(); 
    }

    // 🟢 ซ่อนทุกชิ้นส่วนเมื่อไม่ได้ใช้งานมินิเกม
    public void IdleMinigame()
    {
        isGameActive = false; 
        StopAllCoroutines();

        // 1. ซ่อนผ่าน Container หรือ CanvasGroup (ถ้ามี)
        if (radarContent != null)
        {
            radarContent.SetActive(false);
        }

        if (radarCanvasGroup != null)
        {
            radarCanvasGroup.alpha = 0f;
            radarCanvasGroup.blocksRaycasts = false;
            radarCanvasGroup.interactable = false;
        }

        // 2. ปิดและซ่อนชิ้นส่วนเป้าหมายและวงแหวน
        if (targetRing != null)
        {
            targetRing.gameObject.SetActive(false);
        }

        if (shrinkingRing != null)
        {
            shrinkingRing.localScale = Vector3.zero;
            shrinkingRing.gameObject.SetActive(false);
        }

        if (shrinkingRingImage != null)
        {
            shrinkingRingImage.color = Color.clear;
        }

        // 3. ล้างข้อความทั้งหมดให้จอว่างเปล่า
        if (instructionText != null) instructionText.text = "";
        if (timerText != null) timerText.text = "";
        if (hitCountText != null) hitCountText.text = "";
    }

    // 🟢 เปิดและเริ่มแสดงองค์ประกอบเมื่อเข้ามินิเกม
    public void StartMinigame()
    {
        if (gameManager == null)
        {
            Debug.LogError("ยังไม่ได้ใส่ Game Manager ในหน้าต่าง Inspector ของมินิเกม Radar!");
            return;
        }

        if (baseScale == Vector3.one || baseScale == Vector3.zero)
        {
            CacheBaseScale();
        }

        // 1. เปิดการมองเห็นของหน้าจอ
        if (radarContent != null)
        {
            radarContent.SetActive(true);
        }

        if (radarCanvasGroup != null)
        {
            radarCanvasGroup.alpha = 1f;
            radarCanvasGroup.blocksRaycasts = true;
            radarCanvasGroup.interactable = true;
        }

        if (targetRing != null)
        {
            targetRing.gameObject.SetActive(true);
        }

        if (shrinkingRing != null)
        {
            shrinkingRing.gameObject.SetActive(true);
        }

        // 2. ตั้งค่าความยากตามวัน
        int day = gameManager.currentDay;

        if (day <= 2)
        {
            timeLimit = 16f;
            requiredHits = 4;
            minShrinkSpeed = 1.3f;
            maxShrinkSpeed = 2.8f;
        }
        else if (day >= 3 && day <= 5)
        {
            timeLimit = 14f;
            requiredHits = 5;
            minShrinkSpeed = 1.6f;
            maxShrinkSpeed = 3.2f;
        }
        else if (day >= 6)
        {
            timeLimit = 12f;
            requiredHits = 5;
            minShrinkSpeed = 1.8f;
            maxShrinkSpeed = 3.8f;
        }

        currentHits = 0;
        timer = timeLimit;
        isWaitingForNext = false;

        UpdateHitText();
        if (instructionText != null) instructionText.text = "Press at the right time..";

        isGameActive = true;
        SpawnNewRing();
    }

    private void SpawnNewRing()
    {
        currentRingScale = startScale;
        currentShrinkSpeed = Random.Range(minShrinkSpeed, maxShrinkSpeed);

        if (targetRing != null && shrinkingRing != null)
        {
            shrinkingRing.anchoredPosition = targetRing.anchoredPosition;
        }

        if (shrinkingRingImage != null) shrinkingRingImage.color = normalColor;
        UpdateRingScale();

        isWaitingForNext = false;
    }

    private void Update()
    {
        if (!isGameActive) return;

        timer -= Time.deltaTime;
        if (timerText != null) timerText.text = Mathf.Ceil(Mathf.Max(timer, 0f)).ToString() + "s";

        if (timer <= 0)
        {
            LoseGame("You Fail..");
            return;
        }

        if (!isWaitingForNext)
        {
            currentRingScale -= currentShrinkSpeed * Time.deltaTime;
            UpdateRingScale();

            if (currentRingScale < targetScale - tolerance)
            {
                StartCoroutine(ShowFeedbackAndReset(false));
            }
            else if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                CheckHit();
            }
        }
    }

    private void CheckHit()
    {
        float difference = Mathf.Abs(currentRingScale - targetScale);

        if (difference <= tolerance)
        {
            currentHits++;
            UpdateHitText();

            if (currentHits >= requiredHits)
            {
                WinGame();
            }
            else
            {
                StartCoroutine(ShowFeedbackAndReset(true));
            }
        }
        else
        {            
            StartCoroutine(ShowFeedbackAndReset(false));
        }
    }

    private void UpdateRingScale()
    {
        if (shrinkingRing != null)
        {
            float zScale = baseScale.z != 0 ? baseScale.z : 1f;
            shrinkingRing.localScale = new Vector3(
                baseScale.x * currentRingScale, 
                baseScale.y * currentRingScale, 
                zScale
            );
        }
    }

    private void UpdateHitText()
    {
        if (hitCountText != null)
        {
            hitCountText.text = $"Accuracy: {currentHits}/{requiredHits}";
        }
    }

    private IEnumerator ShowFeedbackAndReset(bool isHit)
    {
        isWaitingForNext = true; 

        if (shrinkingRingImage != null)
        {
            shrinkingRingImage.color = isHit ? hitColor : missColor;
        }
        
        yield return new WaitForSeconds(0.25f);

        if (isGameActive) 
        {
            SpawnNewRing();
        }
    }

    private void WinGame()
    {
        isGameActive = false;
        if (instructionText != null) instructionText.text = "Success";
        if (shrinkingRingImage != null) shrinkingRingImage.color = hitColor;

        currentRingScale = targetScale;
        UpdateRingScale();

        StartCoroutine(EndMinigameRoutine(true));
    }

    private void LoseGame(string reason)
    {
        isGameActive = false;
        if (timerText != null) timerText.text = "0s";
        if (instructionText != null) instructionText.text = reason;

        StartCoroutine(EndMinigameRoutine(false));
    }

    private IEnumerator EndMinigameRoutine(bool isSuccess)
    {
        yield return new WaitForSeconds(endMinigameDelay);

        // 🟢 ซ่อนหน้าจอทันทีเมื่อมินิเกมจบลง
        IdleMinigame();

        if (gameManager != null)
        {
            gameManager.FinishMinigame(isSuccess);
        }
    }
}