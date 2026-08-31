using UnityEngine;

/// <summary>
/// สคริปต์ทดสอบ AudioManager อย่างเดียว ไม่เกี่ยวกับ ControlRoomManager
/// เอาไปแปะกับ GameObject ว่างๆ ในซีนทดสอบ (test2) แล้วกด Play
/// กดปุ่มตามที่ระบุใน Console เพื่อเช็คว่าแต่ละเสียงเล่นได้จริง
/// </summary>
public class AudioTestTrigger : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== Audio Test Trigger พร้อมแล้ว ===");
        Debug.Log("1 = clickSound | 2 = hitSound | 3 = missSound");
        Debug.Log("4 = explosionSound | 5 = gameOverSound");
        Debug.Log("A = ambientLoop (play) | S = stop ambient");
        Debug.Log("L = alarmLoop (play) | K = stop alarm");
    }

    void Update()
    {
        if (AudioManager.Instance == null)
        {
            return; // จะไม่มี log ซ้ำทุกเฟรม เดี๋ยว Console ล้น
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) AudioManager.Instance.PlaySFX("clickSound");
        if (Input.GetKeyDown(KeyCode.Alpha2)) AudioManager.Instance.PlaySFX("hitSound");
        if (Input.GetKeyDown(KeyCode.Alpha3)) AudioManager.Instance.PlaySFX("missSound");
        if (Input.GetKeyDown(KeyCode.Alpha4)) AudioManager.Instance.PlaySFX("explosionSound");
        if (Input.GetKeyDown(KeyCode.Alpha5)) AudioManager.Instance.PlaySFX("gameOverSound");

        if (Input.GetKeyDown(KeyCode.A)) AudioManager.Instance.PlayAmbient("ambientLoop");
        if (Input.GetKeyDown(KeyCode.S)) AudioManager.Instance.StopAmbient();

        if (Input.GetKeyDown(KeyCode.L)) AudioManager.Instance.PlayAlarm("alarmLoop");
        if (Input.GetKeyDown(KeyCode.K)) AudioManager.Instance.StopAlarm();
    }
}