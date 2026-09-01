using UnityEngine;

public class MiniGameClickSound : MonoBehaviour
{
    [SerializeField] private string soundKey = "tileClick";

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            AudioManager.Instance?.PlaySFX(soundKey);
        }
    }
}