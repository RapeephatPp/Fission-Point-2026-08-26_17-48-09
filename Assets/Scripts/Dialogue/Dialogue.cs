using UnityEngine;
using System.Collections;
using TMPro;

public class Dialogue : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject dialogPanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogText;

    [Header("Audio")]
    public AudioClip talkSound;
    [Range(0.8f, 1.2f)] public float minPitch = 0.95f;
    [Range(0.8f, 1.2f)] public float maxPitch = 1.05f;

    [Header("Settings")]
    public float typeSpeed = 0.04f;           
    public float autoAdvanceDelay = 2.0f;     

    private string[] currentLines;
    private int lineIndex;
    private Coroutine dialogCoroutine;
    private AudioSource audioSource;

    public bool IsDialogActive { get; private set; } = false;

    public System.Action OnDialogFinished;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        if (dialogPanel != null) dialogPanel.SetActive(false);
    }

    public void StartDialog(string speakerName, string[] lines)
    {
        IsDialogActive = true;
        dialogPanel.SetActive(true);
        if (speakerText != null) speakerText.text = speakerName;
        currentLines = lines;
        lineIndex = 0;

       

        if (dialogCoroutine != null) StopCoroutine(dialogCoroutine);
        dialogCoroutine = StartCoroutine(PlayDialogRoutine());
    }

    private IEnumerator PlayDialogRoutine()
    {
        
        while (lineIndex < currentLines.Length)
        {
            dialogText.text = "";

            
            foreach (char c in currentLines[lineIndex].ToCharArray())
            {
                dialogText.text += c;
                if (!char.IsWhiteSpace(c) && talkSound != null && audioSource != null)
                {
                    audioSource.pitch = Random.Range(minPitch, maxPitch);
                    audioSource.PlayOneShot(talkSound);
                }
                yield return new WaitForSecondsRealtime(typeSpeed);
            }

            
            yield return new WaitForSecondsRealtime(autoAdvanceDelay);

            
            lineIndex++;
        }

        // เมื่อครบทุกบรรทัด จะปิด UI และคืนค่าเวลาให้เกมเดินต่อ
        IsDialogActive = false;
        if (dialogPanel != null) dialogPanel.SetActive(false);
        Time.timeScale = 1f;

        OnDialogFinished?.Invoke(); 
    }

    private void OnDisable()
    {
        IsDialogActive = false;
        if (dialogCoroutine != null)
        {
            StopCoroutine(dialogCoroutine);
            dialogCoroutine = null;
        }
    }
}