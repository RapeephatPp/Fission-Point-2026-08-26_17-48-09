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

    [Header("Game Settings")]
    public float timeLimit ;        
    public int requiredHits ;        

    [Header("Radar Settings")]
    public float startScale = 3f;        
    public float targetScale = 1f;       
    public float tolerance = 0.2f;       

    public float minShrinkSpeed = 1.5f;  
    public float maxShrinkSpeed = 3.5f;  

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

    private void OnEnable()
    {        
        IdleMinigame(); 
    }

    public void IdleMinigame()
    {
        isGameActive = false; 
        StopAllCoroutines();
        if (shrinkingRing != null) shrinkingRing.localScale = Vector3.zero;
        if (instructionText != null) instructionText.text = "";
        if (timerText != null) timerText.text = "";
        if (shrinkingRingImage != null) shrinkingRingImage.color = Color.clear;
    }

    public void StartMinigame()
    {
        Debug.Log("Radar StartMinigame Triggered!");
        if (gameManager == null)
        {
            Debug.LogError("ยังไม่ได้ใส่ Game Manager ในหน้าต่าง Inspector ของมินิเกม กด! ไปลากมาใส่ซะดีๆ");
            return;
        }

        int day = gameManager.currentDay;

        if (day <= 2)
        {
            timeLimit = 15f;
            requiredHits = 5;
            minShrinkSpeed = 1.5f;
            maxShrinkSpeed = 3.5f;
         }
        else if (day >= 3 && day <= 5)
        {
            timeLimit = 12f;
            requiredHits = 6;
            minShrinkSpeed = 2f;
            maxShrinkSpeed = 4f;
        }
        else if (day >= 6)
        {
            timeLimit = 10f;
            requiredHits = 5;
            minShrinkSpeed = 1.5f;
            maxShrinkSpeed = 5f;
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
        // รีเซ็ตขนาดและสุ่มความเร็วของวงแหวนใหม่
        currentRingScale = startScale;
        currentShrinkSpeed = Random.Range(minShrinkSpeed, maxShrinkSpeed);

        if (shrinkingRingImage != null) shrinkingRingImage.color = normalColor;
        UpdateRingScale();

        isWaitingForNext = false;
    }

    private void Update()
    {
        if (!isGameActive) return;

        
        timer -= Time.deltaTime;
        if (timerText != null) timerText.text = Mathf.Ceil(timer).ToString() + "s";

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
            shrinkingRing.localScale = new Vector3(currentRingScale, currentRingScale, 1f);
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
        
        yield return new WaitForSeconds(0.3f);

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
        yield return new WaitForSeconds(1.5f);

        if (gameManager != null)
        {
            gameManager.FinishMinigame(isSuccess);
        }

        
    }
}