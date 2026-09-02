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
    public Image buttonImage;            // 🟢 เพิ่มช่องสำหรับใส่ Image ของปุ่ม
    public Sprite defaultSprite;         // 🟢 รูปปุ่มสีแดง (ตอนรัว)
    public Sprite successSprite;         // 🟢 รูปปุ่มสีเขียว (ตอนชนะ)

    public float pressedScale = 0.85f;       
    public float buttonRecoverSpeed = 20f;   

    [Header("Game Settings")]
    public float timeLimit;             
    public float fillPerPress = 0.08f;       
    public float drainRate;         

    private float currentProgress = 0f;
    private float timer = 0f;
    private bool isGameActive = false;

    private Vector3 originalButtonScale;

    private void Awake()
    {
        if (buttonVisual != null)
        {
            originalButtonScale = buttonVisual.localScale;
            
            // ดึงคอมโพเนนต์ Image มาให้กริมอัตโนมัติ เผื่อลืมลากใส่
            if (buttonImage == null) 
            {
                buttonImage = buttonVisual.GetComponent<Image>();
            }
        }
    }

    private void OnEnable()
    {
        if (buttonVisual != null) buttonVisual.localScale = originalButtonScale;
        IdleMinigame(); // เปลี่ยนจาก StartMinigame() เป็นแบบนี้
    }
    
    public void IdleMinigame()
    {
        isGameActive = false;
        if (instructionText != null) instructionText.text = "";
        if (timerText != null) timerText.text = "";
        // เคลียร์หลอดต่างๆ ให้เป็น 0
    }

    public void StartMinigame()
    {
        if (gameManager == null)
        {
            Debug.LogError("ยังไม่ได้ใส่ Game Manager ในหน้าต่าง Inspector ของมินิเกม กด! ไปลากมาใส่ซะดีๆ");
            return; 
        }

        int day = gameManager.currentDay;

        if (day <= 2)
        {
            timeLimit = 6f;
            drainRate = 0.15f;
        }
        else if (day >= 3 && day <= 5)
        {
            timeLimit = 5f;
            drainRate = 0.2f;
        }
        else if (day >= 6)
        {
            timeLimit = 4f;
            drainRate = 0.29f;
        }

        currentProgress = 0f;
        timer = timeLimit;

        if (progressBar != null) progressBar.fillAmount = 0f;
        if (instructionText != null) instructionText.text = "Mash the button to fill the gauge..";

        // 🟢 รีเซ็ตปุ่มกลับเป็นสีแดงทุกครั้งที่เริ่มเกมใหม่[cite: 11]
        if (buttonImage != null && defaultSprite != null)
        {
            buttonImage.sprite = defaultSprite;
        }

        isGameActive = true;
    }

    private void Update()
    {
        if (!isGameActive) return;

        timer -= Time.deltaTime;
        if (timerText != null) timerText.text = Mathf.Ceil(timer).ToString() + "s";

        currentProgress -= drainRate * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            currentProgress += fillPerPress;

            if (buttonVisual != null)
            {
                buttonVisual.localScale = originalButtonScale * pressedScale;
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
        if (instructionText != null) instructionText.text = "Success";
        if (buttonVisual != null) buttonVisual.localScale = originalButtonScale; 

        // 🟢 เปลี่ยนปุ่มเป็นสีเขียวเมื่อรัวเกจจนเต็ม[cite: 11]
        if (buttonImage != null && successSprite != null)
        {
            buttonImage.sprite = successSprite;
        }

        StartCoroutine(EndMinigameRoutine(true));
    }

    private void LoseGame()
    {
        isGameActive = false;
        if (timerText != null) timerText.text = "0s";
        if (instructionText != null) instructionText.text = "You Fail..";
        if (buttonVisual != null) buttonVisual.localScale = originalButtonScale;

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