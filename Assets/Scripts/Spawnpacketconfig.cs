using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Spawn Packet Konfigurasyonu v1
///
/// Bir "packet", WaveConfig'e benzer ama sahneye gömülü waveSequence'a
/// bagimli degil. SpawnRhythmTable uzerinden stage araligina gore
/// agirlikli olarak secilir ve SpawnManager'in prosedural fallback
/// katmani yerine gecer.
///
/// PacketType kodlari:
///   Baseline       — duz tek hat, Stage 1-3 zemini
///   DenseSwarm     — kalabalik, siki, merkez agirlikli
///   DelayedCharger — once destek, ardindan leadGapZ bosluk, sonra tehdit
///   ArmorCheck     — zirhli on hat + normal destek
///   Relief         — seyrek, yavas, nefes momenti
///
/// ASSETS: Create > TopEndWar > SpawnPacketConfig
/// </summary>
[CreateAssetMenu(fileName = "Packet_", menuName = "TopEndWar/SpawnPacketConfig")]
public class SpawnPacketConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string    packetId   = "baseline_01";
    public PacketType packetType = PacketType.Baseline;

    [Header("Gruplar")]
    [Tooltip("Her grup bir burst; gruplar arasi groupZStep kadar Z boslugu birakilir.\n" +
             "WaveGroup yapisi dogrudan kullanilir (archetype + count + laneBias).")]
    public List<WaveGroup> groups = new List<WaveGroup>();

    [Header("Z Zamanlama")]
    [Tooltip("Gruplar arasi Z mesafesi (metre).")]
    public float groupZStep = 6f;

    [Tooltip("Grup icindeki her dusman icin rastgele Z kayması — ± bu degerin yarisi.")]
    public float jitterZ = 1.2f;

    [Tooltip("Grup icindeki her dusman icin rastgele X kayması — ± bu degerin yarisi.")]
    public float jitterX = 0.4f;

    [Tooltip("Grup icindeki dusman Z adimi (sira adimi). Arti deger grubu dagitir, 0 uust uste.")]
    public float intraZStep = 1.6f;

    [Header("Lead Gap  (DelayedCharger icin)")]
    [Tooltip("Ilk gruptan once bosluk birakilsin mi? Bir oncu destek grubundan sonra\n" +
             "tehdit grubunun geciktirmeli gelmesi icin kullanilir.")]
    public bool  hasLeadGap = false;

    [Tooltip("Bas boslugu Z miktari (metre). hasLeadGap = true ise uygulanir.")]
    public float leadGapZ   = 8f;

#if UNITY_EDITOR
    void OnValidate()
    {
        groupZStep  = Mathf.Max(2f, groupZStep);
        jitterZ     = Mathf.Max(0f, jitterZ);
        jitterX     = Mathf.Max(0f, jitterX);
        intraZStep  = Mathf.Max(0f, intraZStep);
        leadGapZ    = Mathf.Max(0f, leadGapZ);

        if (!string.IsNullOrEmpty(packetId))
            name = $"Packet_{packetType}_{packetId}";
    }
#endif
}

/// <summary>
/// Packet'in davranis sinifini tanimlar.
/// SpawnManager bu enum'u filtreleme veya debug icin kullanabilir;
/// gercek spawn davranisi WaveGroup + timing alanlari ile belirlenir.
/// </summary>
public enum PacketType
{
    Baseline,        // Sabit hat — tahmin edilebilir, Stage 1–3 zemini
    DenseSwarm,      // Kalabalik cluster — Swarm arketiplerini bekler
    DelayedCharger,  // Destek + bosluk + tehdit — hasLeadGap flag'i ile calisir
    ArmorCheck,      // Zirhli on hat + normal destek — ArmorPen degerini test eder
    Relief,          // Seyrek, yavas, nefes — skor araligi kapama icin
    GuardedCore,
    EliteSpike,
    BossPrep
}