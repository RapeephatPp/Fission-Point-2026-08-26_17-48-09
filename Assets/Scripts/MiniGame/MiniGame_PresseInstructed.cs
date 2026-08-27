using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class MiniGame_PresseInstructed : MonoBehaviour
{
    [Header("References")]
    public ControlRoomManager gameManager;    
    public TextMeshProUGUI instructionText;   
    public TextMeshProUGUI timerText;
    public Image timerBar;

    [Header("UI Visuals")]
    public RectTransform buttonVisual; // ลาก UI รูปปุ่มบนหน้าจอมาใส่ช่องนี้
    public float pressedScale = 0.9f;  // ขนาดตอนที่ปุ่มถูกกด (0.9 คือยุบลง 10%)
    public float pressDuration = 0.1f; // ระยะเวลาที่ปุ่มยุบลง (วินาที)

    [Header("Minigame Settings")]
    public int minRounds = 1;                 
    public int maxRounds = 5;                 
    public int minPresses = 1;                
    public int maxPresses = 9;                
    public float timePerRound ;           // เวลาให้กดในแต่ละรอบ (วินาที)
    public float delayBetweenRounds = 2f;     // เวลาพักก่อนเริ่มรอบต่อไป

    [Range(0f, 1f)]
    public float sameAsLastChance = 0.3f;     

    private int totalRounds;
    private int currentRound;
    private int targetPresses;
    private int currentPresses;
    private int previousTargetPresses;

    private float timer;
    private bool isRoundActive = false;

    private Vector3 originalButtonScale;
    private Coroutine pressCoroutine;

    private void Awake()
    {
        // เก็บขนาดดั้งเดิมของปุ่มไว้ตั้งแต่เริ่ม
        if (buttonVisual != null)
        {
            originalButtonScale = buttonVisual.localScale;
        }
    }

    private void OnEnable()
    {
        // เผื่อเปิดมินิเกมมาแล้วปุ่มค้างสถานะยุบ ให้รีเซ็ตกลับเป็นปกติ
        if (buttonVisual != null)
        {
            buttonVisual.localScale = originalButtonScale;
        }
        StartMinigame();
    }

    
    public void StartMinigame()
    {
        int day = gameManager.currentDay;
        // สุ่มจำนวนรอบทั้งหมดที่จะต้องเล่นในครั้งนี้
        if (day == 1)
        {
            totalRounds = 1;
            currentRound = 1;
            previousTargetPresses = 0;

            timePerRound = 10f;
        }
        else if (day >= 2 && day <= 5)
        {
            totalRounds = Random.Range(minRounds, maxRounds + 1);
            currentRound = 1;
            previousTargetPresses = 0;
            timePerRound = 8f;
        }
        else if (day >= 6)
        {
            totalRounds = 4;
            currentRound = 1;
            previousTargetPresses = 0;
            timePerRound = 5f;
        }

        StartRound();
    }

    private void StartRound()
    {
        currentPresses = 0; // รีเซ็ตจำนวนที่ผู้เล่นกด
        timer = timePerRound;

        if (timerBar != null)
        {
            timerBar.fillAmount = 1f;
        }

        // เช็กว่าถ้ารอบนี้เป็นรอบที่ 2 ขึ้นไป และสุ่มติดโอกาสที่ตั้งไว้
        if (currentRound > 1 && Random.value <= sameAsLastChance)
        {
            targetPresses = previousTargetPresses;
            instructionText.text = "Press the same number as last time..";
        }
        else
        {
            // สุ่มจำนวนครั้งปกติ
            targetPresses = Random.Range(minPresses, maxPresses + 1);
            instructionText.text = "Press " + targetPresses + " Times..";
        }

        isRoundActive = true;
    }

    private void Update()
    {
        if (!isRoundActive) return;

        // นับเวลาถอยหลัง
        timer -= Time.deltaTime;

        // อัปเดต UI หน้าจอให้เห็นเวลา (ปัดเศษขึ้น)
        timerText.text = Mathf.Ceil(timer).ToString() + "s";

        if (timerBar != null)
        {
            // คำนวณสัดส่วนเวลา (จะได้ค่าระหว่าง 0 ถึง 1)
            timerBar.fillAmount = timer / timePerRound;
        }

        // รับค่าการกดปุ่มของผู้เล่น (คลิกซ้าย หรือ สเปซบาร์)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            currentPresses++;
            // เล่นเอฟเฟกต์ปุ่มถูกกด
            if (buttonVisual != null)
            {
                // ถ้ากดรัวๆ ให้หยุดแอนิเมชันเดิมก่อนเริ่มใหม่ เพื่อไม่ให้บั๊ก
                if (pressCoroutine != null) StopCoroutine(pressCoroutine);
                pressCoroutine = StartCoroutine(AnimateButtonPress());
            }

        }

        // เมื่อหมดเวลา
        if (timer <= 0)
        {
            CheckRoundResult();
        }
    }

    private IEnumerator AnimateButtonPress()
    {
        // ย่อขนาดปุ่มลง
        buttonVisual.localScale = originalButtonScale * pressedScale;

        // รอเวลาตามที่ตั้งไว้
        yield return new WaitForSeconds(pressDuration);

        // คืนขนาดปุ่มกลับเป็นปกติ
        buttonVisual.localScale = originalButtonScale;
    }
    private void CheckRoundResult()
    {
        isRoundActive = false;
        timerText.text = "0s";
        if (timerBar != null) timerBar.fillAmount = 0f;

        // ตรวจสอบว่าจำนวนที่กด ตรงกับที่สั่งหรือไม่
        if (currentPresses == targetPresses)
        {
            // หากผ่าน
            if (currentRound >= totalRounds)
            {
                
                instructionText.text = "Pass";
                StartCoroutine(EndMinigameRoutine(true));
            }
            else
            {
                
                instructionText.text = "....";
                previousTargetPresses = targetPresses; 
                currentRound++;
                StartCoroutine(WaitAndStartNextRound());
            }
        }
        else
        {
            // หากพลาด (กดขาด หรือ กดเกิน)
            instructionText.text = "Fail you press " + currentPresses + " Time ";
            StartCoroutine(EndMinigameRoutine(false));
        }
    }

    
    private IEnumerator WaitAndStartNextRound()
    {
        yield return new WaitForSeconds(delayBetweenRounds);
        StartRound();
    }

    // ดีเลย์เล็กน้อยให้ผู้เล่นเห็นผลลัพธ์ ก่อนปิดมินิเกมแล้วส่งค่ากลับไปที่ ControlRoomManager
    private IEnumerator EndMinigameRoutine(bool isSuccess)
    {
        yield return new WaitForSeconds(1.5f);

        if (gameManager != null)
        {
            gameManager.FinishMinigame(isSuccess);
        }

        gameObject.SetActive(false); // ปิดหน้าต่างมินิเกม
    }
}
