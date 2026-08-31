using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Serializable]
    public class SoundEntry
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("Sound Library")]
    public List<SoundEntry> sfxLibrary = new List<SoundEntry>();
    public List<SoundEntry> loopLibrary = new List<SoundEntry>(); // key ซ้ำได้ = เล่นพร้อมกันเป็นเลเยอร์

    [Header("Audio Sources")]
    public AudioSource sfxSource;      // Play On Awake = false

    [Header("Loop Group Parents (สร้าง AudioSource ลูกอัตโนมัติใต้นี้)")]
    public Transform ambientGroupParent; // ถ้าไม่ตั้ง จะใช้ transform ของ AudioManager เอง
    public Transform alarmGroupParent;

    private Dictionary<string, SoundEntry> sfxDict;
    private Dictionary<string, List<SoundEntry>> loopDict; // key -> หลาย clip

    // เก็บ AudioSource ที่กำลังเล่นอยู่ของแต่ละกลุ่ม (ambient / alarm) เพื่อสั่งหยุดทีหลังได้
    private List<AudioSource> activeAmbientSources = new List<AudioSource>();
    private List<AudioSource> activeAlarmSources = new List<AudioSource>();
    private string currentAmbientKey = null;
    private string currentAlarmKey = null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxDict = new Dictionary<string, SoundEntry>();
        foreach (var entry in sfxLibrary)
        {
            if (!sfxDict.ContainsKey(entry.key))
                sfxDict.Add(entry.key, entry);
        }

        // รวม entry ที่ key ซ้ำกันเข้า list เดียวกัน แทนที่จะทิ้งตัวซ้ำ
        loopDict = new Dictionary<string, List<SoundEntry>>();
        foreach (var entry in loopLibrary)
        {
            if (!loopDict.ContainsKey(entry.key))
                loopDict[entry.key] = new List<SoundEntry>();
            loopDict[entry.key].Add(entry);
        }

        if (ambientGroupParent == null) ambientGroupParent = transform;
        if (alarmGroupParent == null) alarmGroupParent = transform;
    }

    public void PlaySFX(string key)
    {
        if (sfxSource == null) return;

        if (sfxDict.TryGetValue(key, out SoundEntry entry) && entry.clip != null)
        {
            sfxSource.PlayOneShot(entry.clip, entry.volume);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX key '{key}' not found or has no clip assigned.");
        }
    }

    public void PlayAmbient(string key)
    {
        PlayLoopGroup(key, ambientGroupParent, activeAmbientSources, ref currentAmbientKey);
    }

    public void StopAmbient()
    {
        StopLoopGroup(activeAmbientSources);
        currentAmbientKey = null;
    }

    public void PlayAlarm(string key)
    {
        PlayLoopGroup(key, alarmGroupParent, activeAlarmSources, ref currentAlarmKey);
    }

    public void StopAlarm()
    {
        StopLoopGroup(activeAlarmSources);
        currentAlarmKey = null;
    }

    // เล่นทุก clip ที่มี key ตรงกันพร้อมกัน โดยสร้าง AudioSource แยกให้แต่ละ clip
    private void PlayLoopGroup(string key, Transform parent, List<AudioSource> activeSources, ref string currentKey)
    {
        if (currentKey == key && activeSources.Count > 0) return; // เล่น key เดิมอยู่แล้ว ไม่ต้องเริ่มซ้ำ

        StopLoopGroup(activeSources); // เคลียร์ของเก่าก่อนเริ่มกลุ่มใหม่

        if (!loopDict.TryGetValue(key, out List<SoundEntry> entries) || entries.Count == 0)
        {
            Debug.LogWarning($"[AudioManager] Loop key '{key}' not found or has no entries.");
            currentKey = null;
            return;
        }

        foreach (var entry in entries)
        {
            if (entry.clip == null) continue;

            GameObject sourceObj = new GameObject($"LoopSource_{key}_{entry.clip.name}");
            sourceObj.transform.SetParent(parent, false);

            AudioSource src = sourceObj.AddComponent<AudioSource>();
            src.clip = entry.clip;
            src.volume = entry.volume;
            src.loop = true;
            src.playOnAwake = false;
            src.Play();

            activeSources.Add(src);
        }

        currentKey = key;
    }

    private void StopLoopGroup(List<AudioSource> activeSources)
    {
        foreach (var src in activeSources)
        {
            if (src == null) continue;
            src.Stop();
            Destroy(src.gameObject);
        }
        activeSources.Clear();
    }
}