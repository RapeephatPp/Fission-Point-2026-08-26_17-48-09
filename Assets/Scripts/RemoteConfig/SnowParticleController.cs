using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SnowParticleController : MonoBehaviour
{
    [Header("Particle References")]
    [SerializeField] private ParticleSystem snowParticleSystem;

    [Header("Snow Settings")]
    [Range(0f, 60f)]
    public float defaultEmissionRate = 15f;
    public float fadeInDuration = 1.5f;
    public float fadeOutDuration = 2.0f;

    [Header("Particle Size & Density Tuning")]
    [Range(0.01f, 0.15f)] public float minParticleSize = 0.025f;
    [Range(0.02f, 0.25f)] public float maxParticleSize = 0.07f;
    public int maxParticlesLimit = 120;

    private float targetEmissionRate = 0f;
    private Coroutine transitionCoroutine;
    private static Texture2D cachedCircleTexture;

    private void Awake()
    {
        if (snowParticleSystem == null)
        {
            snowParticleSystem = GetComponent<ParticleSystem>();
        }

        if (snowParticleSystem == null)
        {
            SetupProceduralSnowParticle();
        }

        ApplyParticleAesthetics();

        // เริ่มต้นด้วยการหยุด Particle ทั้งหมดให้สนิท ไม่ปล่อยให้มีหิมะตกค้าง
        if (snowParticleSystem != null)
        {
            var emission = snowParticleSystem.emission;
            emission.rateOverTime = 0f;
            snowParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void Start()
    {
        ApplyParticleAesthetics();
    }

    private void OnEnable()
    {
        ApplyParticleAesthetics();
    }

    /// <summary>
    /// ปรับสีของอนุภาคให้ตรงกับธีมเทศกาล (เช่น หิมะขาว หรือประกายไฟฮาโลวีน)
    /// </summary>
    public void SetParticleTheme(Color minColor, Color maxColor)
    {
        ApplyParticleAesthetics();
        if (snowParticleSystem != null)
        {
            var main = snowParticleSystem.main;
            main.startColor = new ParticleSystem.MinMaxGradient(minColor, maxColor);
        }
    }

    /// <summary>
    /// กำหนดการเปิดหรือปิดเอฟเฟกต์หิมะ พร้อมระบุความหนาแน่น
    /// </summary>
    public void SetSnowActive(bool active, float rate = 15f)
    {
        ApplyParticleAesthetics();
        targetEmissionRate = active ? Mathf.Max(rate, 2f) : 0f;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(TransitionEmissionRoutine(targetEmissionRate, active ? fadeInDuration : fadeOutDuration));
    }

    /// <summary>
    /// ปรับความหนาแน่นของหิมะทันที
    /// </summary>
    public void SetEmissionRate(float rate)
    {
        ApplyParticleAesthetics();
        targetEmissionRate = Mathf.Max(0f, rate);
        if (snowParticleSystem != null)
        {
            var emission = snowParticleSystem.emission;
            emission.rateOverTime = targetEmissionRate;

            if (targetEmissionRate > 0f && !snowParticleSystem.isPlaying)
            {
                snowParticleSystem.Play();
            }
            else if (targetEmissionRate <= 0f && snowParticleSystem.isPlaying)
            {
                snowParticleSystem.Stop();
            }
        }
    }

    private IEnumerator TransitionEmissionRoutine(float targetRate, float duration)
    {
        if (snowParticleSystem == null) yield break;

        var emission = snowParticleSystem.emission;
        float startRate = emission.rateOverTime.constant;

        if (targetRate > 0f && !snowParticleSystem.isPlaying)
        {
            snowParticleSystem.Play();
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float currentRate = Mathf.Lerp(startRate, targetRate, elapsed / duration);
            emission.rateOverTime = currentRate;
            yield return null;
        }

        emission.rateOverTime = targetRate;

        if (targetRate <= 0f && snowParticleSystem.isPlaying)
        {
            snowParticleSystem.Stop();
        }
    }

    /// <summary>
    /// สร้างระบบอนุภาคหิมะอัตโนมัติหากยังไม่มี Particle System ติดตั้งไว้
    /// </summary>
    private void SetupProceduralSnowParticle()
    {
        snowParticleSystem = gameObject.AddComponent<ParticleSystem>();

        var main = snowParticleSystem.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = true;

        var emission = snowParticleSystem.emission;
        emission.rateOverTime = 0f;

        var shape = snowParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f, 1f, 1f);
        shape.position = new Vector3(0f, 7f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        // Velocity Over Lifetime เพื่อให้หิมะปลิวเอียงตามสายลมเบาๆ
        var velocity = snowParticleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.6f, -0.8f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Noise module ให้หิมะร่อนแกว่งไหวเป็นธรรมชาติอย่างนุ่มนวล
        var noise = snowParticleSystem.noise;
        noise.enabled = true;
        noise.strength = 0.2f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.4f;

        ApplyParticleAesthetics();
    }

    /// <summary>
    /// อัปเดตขนาดและความเร็วให้ละอองหิมะมีขนาดเล็ก ละเอียดนุ่มนวล และไม่บดบังจอเกม
    /// </summary>
    public void ApplyParticleAesthetics()
    {
        if (snowParticleSystem == null) return;

        var main = snowParticleSystem.main;
        main.startSize = new ParticleSystem.MinMaxCurve(minParticleSize, maxParticleSize);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(5.0f, 8.0f);
        main.maxParticles = maxParticlesLimit;

        EnsureValidMaterial();
    }

    /// <summary>
    /// กำหนด Material ที่รองรับ URP 2D เพื่อป้องกันปัญหา Particle เป็นสีม่วง (Missing Shader)
    /// </summary>
    public void EnsureValidMaterial()
    {
        var renderer = GetComponent<ParticleSystemRenderer>();
        if (renderer == null) return;

        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 50;

        bool needsMaterial = false;
        if (renderer.sharedMaterial == null)
        {
            needsMaterial = true;
        }
        else if (renderer.sharedMaterial.shader == null || renderer.sharedMaterial.shader.name.Contains("InternalErrorShader"))
        {
            needsMaterial = true;
        }

        if (needsMaterial)
        {
            Shader targetShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
            if (targetShader == null) targetShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (targetShader == null) targetShader = Shader.Find("Sprites/Default");

            Material snowMaterial = new Material(targetShader);
            snowMaterial.name = "SnowParticle_URP_Mat";

            if (cachedCircleTexture == null)
            {
                cachedCircleTexture = CreateSoftCircleTexture();
            }

            snowMaterial.mainTexture = cachedCircleTexture;
            renderer.material = snowMaterial;
        }
    }

    private static Texture2D CreateSoftCircleTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "SoftCircle_Snow";
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.45f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (dist / radius));
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    private void OnValidate()
    {
        ApplyParticleAesthetics();
    }
}
