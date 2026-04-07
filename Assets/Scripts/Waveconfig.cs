using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top End War — Dalga Konfigurasyonu v1 (Claude)
///
/// Bir wave'in hangi dusmanlari, hangi sirada, hangi aralikla cikardigi.
/// Sahnede dusman dizmek yerine SpawnManager bu SO'yu okur.
///
/// SLICE DALGA IDS (Final Pack v1):
///   W1  = Trooper Line
///   W2  = Swarm Burst
///   W3  = Charger Pressure
///   W4  = Mixed Light
///   M1  = Tutorial Exam
///   W_BruteIntro / M_BrutePlusLine / W_EliteIntro / M2 / MB1
///
/// ASSETS: Create > TopEndWar > WaveConfig
/// </summary>
[CreateAssetMenu(fileName = "Wave_", menuName = "TopEndWar/WaveConfig")]
public class WaveConfig : ScriptableObject
{
    [Header("Kimlik")]
    public string waveId    = "W1";
    public string waveCode  = "W1_TrooperLine";
    public WaveRole waveRole = WaveRole.Normal;

    [Header("Spawn Gruplari")]
    [Tooltip("Her grup ayri bir burst. Gruplar arasi delay spawnGroupDelay saniye.")]
    public List<WaveGroup> groups = new List<WaveGroup>();

    [Header("Zamanlama")]
    [Tooltip("Gruplar arasi bekleme suresi")]
    public float spawnGroupDelay = 2.5f;
    [Tooltip("Ayni grup icinde dusman spawn araliginda saniye")]
    public float intraGroupDelay = 0.4f;
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
    Normal,     // Standart dalga
    Elite,      // Elite agirlikli
    Boss,       // Mini-boss veya boss
    MixedExam,  // Test dalgasi
}

public enum LaneBias
{
    Spread,     // Yola yayil
    Center,     // Merkez agirlikh
    Left,
    Right,
    Random,
}