using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Biyom Yoneticisi (Claude)
///
/// UNITY KURULUM:
///   Hierarchy -> Create Empty -> "BiomeManager" -> bu scripti ekle.
///   Inspector'da currentBiome'u sahneye gore ayarla:
///     Sivas=Tas  Tokat=Orman  Kayseri=Cul  Erzurum=Karli  Malatya=Tarim
///
/// Kullanim:
///   float mult = BiomeManager.Instance.GetMultiplier(SoldierPath.Teknoloji);
/// </summary>
public class BiomeManager : MonoBehaviour
{
    public static BiomeManager Instance { get; private set; }

    [Header("Bu Sahnenin Biyomu")]
    [Tooltip("Tas / Orman / Cul / Karli / Tarim")]
    public string currentBiome = "Tas";

    // Biyom x Path hasar matrisi — dogru path x1.25, yanlis x0.85 ceza
    static readonly Dictionary<string, Dictionary<string, float>> _matrix
        = new Dictionary<string, Dictionary<string, float>>
    {
        ["Tas"]   = new Dictionary<string, float> { ["Piyade"]=0.90f, ["Mekanik"]=1.10f, ["Teknoloji"]=1.25f },
        ["Orman"] = new Dictionary<string, float> { ["Piyade"]=1.20f, ["Mekanik"]=1.00f, ["Teknoloji"]=0.85f },
        ["Cul"]   = new Dictionary<string, float> { ["Piyade"]=1.10f, ["Mekanik"]=1.20f, ["Teknoloji"]=1.00f },
        ["Karli"] = new Dictionary<string, float> { ["Piyade"]=1.15f, ["Mekanik"]=0.85f, ["Teknoloji"]=1.15f },
        ["Tarim"] = new Dictionary<string, float> { ["Piyade"]=1.25f, ["Mekanik"]=1.10f, ["Teknoloji"]=0.80f },
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => GameEvents.OnBiomeChanged?.Invoke(currentBiome);

    /// <summary>Asker pathine gore biyom hasar carpanini dondurur.</summary>
    public float GetMultiplier(SoldierPath path)
    {
        string key = path.ToString();
        if (_matrix.TryGetValue(currentBiome, out var row) &&
            row.TryGetValue(key, out float mult))
            return mult;
        Debug.LogWarning($"[BiomeManager] Bilinmeyen biome/path: {currentBiome}/{key}");
        return 1f;
    }

    /// <summary>Runtime biyom degistir (yeni bolum gecislerinde).</summary>
    public void SetBiome(string biome)
    {
        if (!_matrix.ContainsKey(biome))
        { Debug.LogWarning($"[BiomeManager] Bilinmeyen biome: {biome}"); return; }
        currentBiome = biome;
        GameEvents.OnBiomeChanged?.Invoke(currentBiome);
        Debug.Log($"[Biome] -> {biome}");
    }

    public string GetBossName() => currentBiome switch
    {
        "Tas"   => "Gokmedrese Muhafizi",
        "Orman" => "Orman Canavarı",
        "Cul"   => "Kum Devigi",
        "Karli" => "Buz Muhafizi",
        "Tarim" => "Tarla Ruhu",
        _       => "Bilinmeyen Boss"
    };

    void OnDestroy() { if (Instance == this) Instance = null; }
}