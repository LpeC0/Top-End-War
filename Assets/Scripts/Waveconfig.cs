using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Dalga Konfigurasyonu v2
///
/// DEĞİŞİKLİK:
///   - OnValidate eklendi
///   - Slice icin guvenli clamp'ler eklendi
/// </summary>
[CreateAssetMenu(fileName = "Wave_", menuName = "TopEndWar/WaveConfig")]
public class WaveConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string waveId = "W1";
    public string waveCode = "W1_TrooperLine";
    public WaveRole waveRole = WaveRole.Normal;

    [Header("Spawn Gruplari")]
    [Tooltip("Her grup ayri bir burst. Gruplar arasi delay spawnGroupDelay saniye.")]
    public List<WaveGroup> groups = new List<WaveGroup>();

    [Header("Zamanlama")]
    [Tooltip("Gruplar arasi bekleme suresi")]
    public float spawnGroupDelay = 2.5f;

    [Tooltip("Ayni grup icinde dusman spawn araliginda saniye")]
    public float intraGroupDelay = 0.4f;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (groups == null)
            groups = new List<WaveGroup>();

        spawnGroupDelay = Mathf.Max(0f, spawnGroupDelay);
        intraGroupDelay = Mathf.Max(0f, intraGroupDelay);

        if (!string.IsNullOrEmpty(waveId))
            name = $"Wave_{waveId}";
    }
#endif
}

[System.Serializable]
public class WaveGroup
{
    [Tooltip("Bu gruptan hangi dusman arketipi?")]
    public EnemyArchetypeConfig archetype;

    [Tooltip("Kac adet ciksin?")]
    [Range(1, 15)]
    public int count = 4;

    [Tooltip("Bu grup icinde tekrar kullanilan lane dagilimi")]
    public LaneBias laneBias = LaneBias.Spread;
}

public enum WaveRole
{
    Normal,
    Elite,
    Boss,
    MixedExam,
}

public enum LaneBias
{
    Spread,
    Center,
    Left,
    Right,
    Random,
}