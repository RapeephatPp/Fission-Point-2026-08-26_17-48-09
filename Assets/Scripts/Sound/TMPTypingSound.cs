using UnityEngine;
using TMPro;

public class TMPTypingSound : MonoBehaviour
{
    private TMP_Text tmpText;
    private AudioSource audioSource;
    private int lastTextLength;

    [Header("Typing Sound Settings")]
    public AudioClip typeSound;
    [Range(0.8f, 1.2f)] public float minPitch = 0.9f;
    [Range(0.8f, 1.2f)] public float maxPitch = 1.1f;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (tmpText != null)
        {
            lastTextLength = tmpText.text.Length;
        }
    }

    private void Update()
    {
        if (tmpText == null) return;

        int currentLength = tmpText.text.Length;

        // เล่นเสียงเมื่อมีตัวอักษรเพิ่มขึ้นมา
        if (currentLength > lastTextLength)
        {
            PlayTypingSound();
        }

        lastTextLength = currentLength;
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
    }
}