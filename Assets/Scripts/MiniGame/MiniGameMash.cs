using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MiniGameMash : MonoBehaviour
{
    [Header("References")]
    public ControlRoomManager gameManager;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI timerText;

    [Header("UI Elements")]
    public Image progressBar;                

    [Header("Button Visual & Sprites")]
    public RectTransform buttonVisual; 
    public Image buttonImage;            // ช่องสำหรับใส่ Image ของปุ่ม
    public Sprite defaultSprite;         // รูปปุ่มสีแดง (ตอนรัว)
    public Sprite successSprite;         // รูปปุ่มสีเขียว (ตอนชนะ)

    public float pressedScale = 0.85f;       
    public float buttonRecoverSpeed = 20f;   

    [Header("Game Settings")]
    public float timeLimit;             
    public float fillPerPress = 0.08f;       
    public float drainRate;

    [Header("Overheat Hazard Twists (Day 4+)")]
    public bool enableOverheat = true;
    public int overheatStartDay = 4;
    [Tooltip("ระยะเวลาแจ้งเตือนล่วงหน้าก่อนปุ่ม Overheat")]
    public float overheatWarningDuration = 0.6f;
    [Tooltip("ระยะเวลาที่ต้องหยุดรัวรอปุ่มหายร้อน")]
    public float overheatDuration = 0.95f;
    [Tooltip("ค่า Progress ที่ถูกลดลงหากฝืนกดตอน Overheat")]
    public float overheatPenalty = 0.12f;
    public Color warningBarColor = new Color(1f, 0.85f, 0.1f, 1f);
    public Color overheatBarColor = Color.white;

    private float currentProgress = 0f;
    private float timer = 0f;
    private bool isGameActive = false;
    private bool isOverheatWarning = false;
    private bool isOverheated = false;
    private Coroutine overheatCoroutine;

    private Vector3 originalButtonScale;
    private Vector2 originalButtonPos;
    private Color originalProgressColor = Color.white;
    private Color originalInstructionColor = Color.white;

    private void Awake()
    {
        if (buttonVisual != null)
        {
            originalButtonScale = buttonVisual.localScale;
            originalButtonPos = buttonVisual.anchoredPosition;
            
            // ดึงคอมโพเนนต์ Image มาให้อัตโนมัติ เผื่อลืมลากใส่
            if (buttonImage == null) 
            {
                buttonImage = buttonVisual.GetComponent<Image>();
            }
        }

        if (progressBar != null)
        {
            originalProgressColor = progressBar.color;
        }

        if (instructionText != null)
        {
            originalInstructionColor = instructionText.color;
        }
    }

    private void OnEnable()
    {
        if (buttonVisual != null)
        {
            buttonVisual.localScale = originalButtonScale;
            buttonVisual.anchoredPosition = originalButtonPos;
        }
        IdleMinigame();
    }
    
    public void IdleMinigame()
    {
        isGameActive = false;
        isOverheatWarning = false;
        isOverheated = false;
        overheatCoroutine = null;
        StopAllCoroutines();

        if (instructionText != null)
        {
            instructionText.text = "";
            instructionText.color = originalInstructionColor;
        }
        if (timerText != null) timerText.text = "";
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
            progressBar.color = originalProgressColor; // คืนสีหลอดเพราะใช้ร่วมกับ timerBar ของ Press
        }
        if (buttonVisual != null)
        {
            buttonVisual.localScale = originalButtonScale;
            buttonVisual.anchoredPosition = originalButtonPos;
        }
    }

    public void StartMinigame()
    {
        if (gameManager == null)
        {
            Debug.LogError("ยังไม่ได้ใส่ Game Manager ในหน้าต่าง Inspector ของมินิเกม กด! ไปลากมาใส่ซะดีๆ");
            return; 
        }

        if (overheatCoroutine != null)
        {
            StopCoroutine(overheatCoroutine);
            overheatCoroutine = null;
        }

        int day = gameManager.currentDay;

        if (day <= 2)
        {
            timeLimit = 6f;
            drainRate = 0.15f;
        }
        else if (day >= 3 && day <= 5)
        {
            timeLimit = (enableOverheat && day >= overheatStartDay) ? 6.2f : 5f;
            drainRate = 0.2f;
        }
        else if (day >= 6)
        {
            timeLimit = enableOverheat ? 5.5f : 4f;
            drainRate = 0.29f;
        }

        currentProgress = 0f;
        timer = timeLimit;
        isOverheatWarning = false;
        isOverheated = false;

        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
            progressBar.color = originalProgressColor;
        }
        if (instructionText != null)
        {
            instructionText.text = "Mash the button to fill the gauge..";
            instructionText.color = originalInstructionColor;
        }

        // รีเซ็ตปุ่มกลับเป็นสีแดงทุกครั้งที่เริ่มเกมใหม่
        if (buttonImage != null && defaultSprite != null)
        {
            buttonImage.sprite = defaultSprite;
        }
        if (buttonVisual != null)
        {
            buttonVisual.anchoredPosition = originalButtonPos;
        }

        isGameActive = true;

        if (enableOverheat && day >= overheatStartDay)
        {
            overheatCoroutine = StartCoroutine(OverheatRoutine());
        }
    }

    private IEnumerator OverheatRoutine()
    {
        // รอให้ผู้เล่นรัวไปแล้วเล็กน้อยก่อนจะเตือน
        yield return new WaitForSeconds(Random.Range(1.3f, 1.9f));

        while (isGameActive && timer > 2.0f)
        {
            // 1. ช่วงแจ้งเตือนล่วงหน้า (Pre-warning)
            isOverheatWarning = true;
            if (instructionText != null)
            {
                instructionText.text = "WARNING: OVERHEATING...";
                instructionText.color = warningBarColor;
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("clickSound");

            yield return new WaitForSeconds(overheatWarningDuration);
            if (!isGameActive) yield break;

            // 2. ช่วง Overheat ต้องหยุดรัว
            isOverheatWarning = false;
            isOverheated = true;
            if (instructionText != null)
            {
                instructionText.text = "OVERHEAT! STOP PRESSING!";
                instructionText.color = new Color(1f, 0.3f, 0.3f, 1f);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("missSound");

            yield return new WaitForSeconds(overheatDuration);
            if (!isGameActive) yield break;

            // 3. หายร้อน กลับมารัวต่อได้
            isOverheated = false;
            if (progressBar != null) progressBar.color = originalProgressColor;
            if (buttonVisual != null) buttonVisual.anchoredPosition = originalButtonPos;
            if (instructionText != null)
            {
                instructionText.text = "Mash the button to fill the gauge..";
                instructionText.color = originalInstructionColor;
            }

            yield return new WaitForSeconds(Random.Range(1.8f, 2.5f));
        }
    }

    private void Update()
    {
        if (!isGameActive) return;

        timer -= Time.deltaTime;
        if (timerText != null) timerText.text = Mathf.Ceil(timer).ToString() + "s";

        // หยุดการไหลลดลงของหลอดชั่วคราวตอน Overheat เพื่อความยุติธรรมขณะผู้เล่นหยุดมือ
        if (!isOverheated)
        {
            currentProgress -= drainRate * Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (isOverheated)
            {
                currentProgress -= overheatPenalty;
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("missSound");
            }
            else
            {
                currentProgress += fillPerPress;
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("tileClick");
            }

            if (buttonVisual != null)
            {
                buttonVisual.localScale = originalButtonScale * pressedScale;
            }
        }

        // Visual feedback สำหรับ Warning / Overheat บนหลอด progressBar และแรงสั่นปุ่ม
        if (isOverheatWarning || isOverheated)
        {
            if (progressBar != null)
            {
                Color targetCol = isOverheated ? overheatBarColor : warningBarColor;
                float flash = Mathf.PingPong(Time.time * (isOverheated ? 16f : 10f), 1f);
                progressBar.color = Color.Lerp(originalProgressColor, targetCol, flash);
            }
            if (buttonVisual != null)
            {
                float shakeStrength = isOverheated ? 4f : 1.5f;
                buttonVisual.anchoredPosition = originalButtonPos + Random.insideUnitCircle * shakeStrength;
            }
        }

        if (buttonVisual != null)
        {
            buttonVisual.localScale = Vector3.Lerp(buttonVisual.localScale, originalButtonScale, Time.deltaTime * buttonRecoverSpeed);
        }

        currentProgress = Mathf.Clamp01(currentProgress);
        if (progressBar != null) progressBar.fillAmount = currentProgress;

        if (currentProgress >= 1f)
        {
            WinGame();
        }
        else if (timer <= 0)
        {
            LoseGame();
        }
    }

    private void WinGame()
    {
        isGameActive = false;
        isOverheatWarning = false;
        isOverheated = false;
        if (overheatCoroutine != null)
        {
            StopCoroutine(overheatCoroutine);
            overheatCoroutine = null;
        }

        if (instructionText != null)
        {
            instructionText.text = "Success";
            instructionText.color = originalInstructionColor;
        }
        if (progressBar != null) progressBar.color = originalProgressColor;
        if (buttonVisual != null)
        {
            buttonVisual.localScale = originalButtonScale;
            buttonVisual.anchoredPosition = originalButtonPos;
        }

        // เปลี่ยนปุ่มเป็นสีเขียวเมื่อรัวเกจจนเต็ม
        if (buttonImage != null && successSprite != null)
        {
            buttonImage.sprite = successSprite;
        }

        StartCoroutine(EndMinigameRoutine(true));
    }

    private void LoseGame()
    {
        isGameActive = false;
        isOverheatWarning = false;
        isOverheated = false;
        if (overheatCoroutine != null)
        {
            StopCoroutine(overheatCoroutine);
            overheatCoroutine = null;
        }

        if (timerText != null) timerText.text = "0s";
        if (instructionText != null)
        {
            instructionText.text = "You Fail..";
            instructionText.color = originalInstructionColor;
        }
        if (progressBar != null) progressBar.color = originalProgressColor;
        if (buttonVisual != null)
        {
            buttonVisual.localScale = originalButtonScale;
            buttonVisual.anchoredPosition = originalButtonPos;
        }

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
