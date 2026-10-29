using UnityEngine;
using TMPro;

public class TMPTypingSound : MonoBehaviour
{
    private TMP_Text tmpText;
    private AudioSource audioSource;
    private int lastTextLength;
    private int lastMaxVisible;

    [Header("Typing Sound Settings")]
    public AudioClip typeSound;
    [Range(0.8f, 1.2f)] public float minPitch = 0.9f;
    [Range(0.8f, 1.2f)] public float maxPitch = 1.1f;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        if (tmpText != null)
        {
            lastTextLength = tmpText.text.Length;
            lastMaxVisible = tmpText.maxVisibleCharacters;
        }
    }

    private void Update()
    {
        if (tmpText == null) return;

        int currentLength = tmpText.text.Length;

        // เล่นเสียงเมื่อมีตัวอักษรเพิ่มขึ้นมา
        int currentMaxVisible = tmpText.maxVisibleCharacters;

        bool textLengthIncreased = currentLength > lastTextLength;
        bool maxVisibleIncreased = currentMaxVisible > lastMaxVisible && currentMaxVisible <= currentLength;

        if (textLengthIncreased || maxVisibleIncreased)
        {
            char lastChar = ' ';
            if (maxVisibleIncreased && currentMaxVisible > 0 && currentMaxVisible <= currentLength)
            {
                lastChar = tmpText.text[currentMaxVisible - 1];
            }
            else if (textLengthIncreased && currentLength > 0)
            {
                lastChar = tmpText.text[currentLength - 1];
            }

            if (!char.IsWhiteSpace(lastChar))
            {
                PlayTypingSound();
            }
        }

        lastTextLength = currentLength;
        lastMaxVisible = currentMaxVisible;
    }

    private void PlayTypingSound()
    {
        if (audioSource == null) return;

        // สุ่ม Pitch เล็กน้อยเพื่อให้เสียงพิมพ์ดูเป็นธรรมชาติ
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        if (typeSound != null)
        {
            audioSource.PlayOneShot(typeSound);
        }
        else if (audioSource.clip != null)
        {
            audioSource.PlayOneShot(audioSource.clip);
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("clickSound");
        }
    }
}