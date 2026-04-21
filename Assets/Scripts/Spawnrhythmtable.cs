using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Spawn Rhythm Tablosu v1
///
/// SpawnManager, aktif stage ID'sine gore bu tablodan agirlikli random
/// bir SpawnPacketConfig secer. Tasarimci her stage bandi icin
/// packet agirliklarini Inspector'dan dugumleyebilir.
///
/// Secim onceligi:
///   StageConfig.waveSequence dolu → o kullanilir, bu tablo devreye GIRMEZ.
///   waveSequence bos              → bu tablodan packet secilir.
///   rhythmTable de atanmamissa    → prosedural fallback (SpawnManager eski kodu).
///
/// ASSETS: Create > TopEndWar > SpawnRhythmTable
/// </summary>
[CreateAssetMenu(fileName = "RhythmTable_", menuName = "TopEndWar/SpawnRhythmTable")]
public class SpawnRhythmTable : ScriptableObject
{
    [Header("Packet Havuzu")]
    [Tooltip("Stage araligina gore agirlikli packet secimi.\n" +
             "Ayni stage icin birden fazla entry eklenebilir; agirlikla orani belirlenir.")]
    public List<RhythmEntry> entries = new List<RhythmEntry>();

    /// <summary>
    /// Mevcut stage'e uygun kayitlar arasinda agirlikli random secim yapar.
    /// exclude: son secilen packet — ayni packet ust uste gelmemesi icin
    ///          agirliginin %25'ine dusurilur (tamamen elenmez, sonuz dongu olmaz).
    /// </summary>
    public SpawnPacketConfig Pick(int currentWorld,int currentStage, SpawnPacketConfig exclude = null)
    {
        // Aktif stage araligina uyan kayitlari topla
        var pool  = new List<(SpawnPacketConfig packet, float weight)>();
        float total = 0f;

        foreach (RhythmEntry e in entries)
        {
            if (e.packet == null) continue;
            if (currentStage < e.minStage || currentStage > e.maxStage) continue;

            // Son secilen packet'i tamamen eleme; agirligini yarisla (cesitlilik saglanir)
            float w = (e.packet == exclude) ? e.weight * 0.25f : e.weight;
            pool.Add((e.packet, w));
            total += w;
        }

        if (pool.Count == 0) return null;

        float roll = Random.value * total;
        float acc  = 0f;
        foreach (var (p, w) in pool)
        {
            acc += w;
            if (roll <= acc) return p;
        }

        return pool[pool.Count - 1].packet;   // float tolerans kapama
    }
}

/// <summary>
/// Rhythm tablosundaki tek bir kayit: hangi packet, ne agirligi, hangi stage araliginda.
/// </summary>
[System.Serializable]
public class RhythmEntry
{
    [Tooltip("Secilecek packet konfigurasyonu.")]
    public SpawnPacketConfig packet;

    [Tooltip("Agirlik. Buyuk deger = daha sik secilir. Diger entry'lerle oranli calisir.")]
    [Range(0.1f, 10f)]
    public float weight = 1f;

    [Tooltip("Bu entry hangi stage'den itibaren aktif (dahil).")]
    public int minStage = 1;

    [Tooltip("Bu entry hangi stage'e kadar aktif (dahil).")]
    public int maxStage = 5;

    public int worldID = 1;
}