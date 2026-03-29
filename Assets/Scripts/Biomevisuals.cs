using UnityEngine;
using DG.Tweening;

/// <summary>
/// Top End War — Biyom Görsel Sistemi (Claude)
///
/// BiomeManager'ın OnBiomeChanged eventini dinler.
/// Her biyom geçişinde:
///   - Kamera arkaplan rengi değişir (DOTween ile yumuşak)
///   - Directional Light rengi değişir
///   - Fog rengi değişir (opsiyonel)
///
/// UNITY KURULUM:
///   Hierarchy → Create Empty → "BiomeVisuals" → bu scripti ekle
///   mainCamera   → Main Camera
///   mainLight    → Directional Light
///
/// BİYOM RENKLERİ:
///   Taş  (Sivas)  → gri/mavi soğuk
///   Orman(Tokat)  → yeşil/sıcak
///   Çöl  (Kayser) → turuncu/sarı kuru
///   Karlı(Erzrum) → beyaz/mavi buz
///   Tarım(Mlatya) → yeşil/sarı yumuşak
/// </summary>
public class BiomeVisuals : MonoBehaviour
{
    [Header("Referanslar")]
    public Camera    mainCamera;
    public Light     mainLight;

    [Header("Geçiş Süresi")]
    public float transitionDuration = 2.5f;

    // ── Biyom renk tanımları ──────────────────────────────────────────────
    static readonly System.Collections.Generic.Dictionary<string, BiomeColors> COLORS
        = new System.Collections.Generic.Dictionary<string, BiomeColors>
    {
        ["Tas"]   = new BiomeColors(
            sky:   new Color(0.28f, 0.33f, 0.42f),   // soğuk gri-mavi
            light: new Color(0.90f, 0.88f, 0.80f),   // soluk beyaz
            fog:   new Color(0.60f, 0.62f, 0.68f),
            fogDensity: 0.008f
        ),
        ["Orman"] = new BiomeColors(
            sky:   new Color(0.18f, 0.28f, 0.20f),   // koyu yeşil
            light: new Color(1.00f, 0.95f, 0.75f),   // sıcak sarı
            fog:   new Color(0.45f, 0.55f, 0.40f),
            fogDensity: 0.012f
        ),
        ["Cul"]   = new BiomeColors(
            sky:   new Color(0.55f, 0.40f, 0.20f),   // turuncu çöl
            light: new Color(1.00f, 0.88f, 0.60f),   // sıcak altın
            fog:   new Color(0.70f, 0.58f, 0.35f),
            fogDensity: 0.015f
        ),
        ["Karli"] = new BiomeColors(
            sky:   new Color(0.70f, 0.78f, 0.90f),   // açık buz mavisi
            light: new Color(0.85f, 0.92f, 1.00f),   // soğuk beyaz-mavi
            fog:   new Color(0.80f, 0.85f, 0.95f),
            fogDensity: 0.018f
        ),
        ["Tarim"] = new BiomeColors(
            sky:   new Color(0.35f, 0.45f, 0.25f),   // tarım yeşili
            light: new Color(1.00f, 0.96f, 0.78f),   // güneşli
            fog:   new Color(0.55f, 0.60f, 0.42f),
            fogDensity: 0.006f
        ),
    };

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainLight  == null) mainLight  = FindFirstObjectByType<Light>();

        GameEvents.OnBiomeChanged += OnBiomeChanged;

        // Başlangıç biyomunu uygula (animasyonsuz)
        string startBiome = BiomeManager.Instance?.currentBiome ?? "Tas";
        ApplyImmediate(startBiome);
    }

    void OnDestroy() => GameEvents.OnBiomeChanged -= OnBiomeChanged;

    void OnBiomeChanged(string biome) => ApplyTransition(biome);

    void ApplyImmediate(string biome)
    {
        if (!COLORS.TryGetValue(biome, out var c)) return;
        if (mainCamera) { mainCamera.backgroundColor = c.sky; mainCamera.clearFlags = CameraClearFlags.SolidColor; }
        if (mainLight)  mainLight.color = c.light;
        RenderSettings.fogColor   = c.fog;
        RenderSettings.fogDensity = c.fogDensity;
        RenderSettings.fog        = true;
    }

    void ApplyTransition(string biome)
    {
        if (!COLORS.TryGetValue(biome, out var c)) return;

        // Kamera arkaplan
        if (mainCamera)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            DOTween.To(
                () => mainCamera.backgroundColor,
                x  => mainCamera.backgroundColor = x,
                c.sky, transitionDuration
            ).SetEase(Ease.InOutSine);
        }

        // Işık rengi
        if (mainLight)
        {
            DOTween.To(
                () => mainLight.color,
                x  => mainLight.color = x,
                c.light, transitionDuration
            ).SetEase(Ease.InOutSine);
        }

        // Fog
        DOTween.To(
            () => RenderSettings.fogColor,
            x  => RenderSettings.fogColor = x,
            c.fog, transitionDuration
        ).SetEase(Ease.InOutSine);

        RenderSettings.fog = true;
        DOTween.To(
            () => RenderSettings.fogDensity,
            x  => RenderSettings.fogDensity = x,
            c.fogDensity, transitionDuration
        );
    }

    // ── İç tip ─────────────────────────────────────────────────────────────
    struct BiomeColors
    {
        public Color sky, light, fog;
        public float fogDensity;
        public BiomeColors(Color sky, Color light, Color fog, float fogDensity)
        { this.sky=sky; this.light=light; this.fog=fog; this.fogDensity=fogDensity; }
    }
}