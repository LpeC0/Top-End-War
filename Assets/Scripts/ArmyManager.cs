using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Top End War — Ordu Yoneticisi (Claude)
///
/// UNITY KURULUM:
///   Hierarchy -> Create Empty -> "ArmyManager" -> bu scripti ekle.
///   soldierPrefab: bos birakilabilir (fallback kapsul olusturur).
///   Opsiyonel: ObjectPooler'da "Soldier" poolu ekleyin.
///
/// ONEMLI:
///   - Soldier tag'i otomatik set edilir ("Soldier")
///   - xLimit = 8 (PlayerController ile ayni olmalı)
///   - Formasyon: 4 sira x 5 sutun, oyuncunun arkasindan
///
/// Disaridan kullanim:
///   ArmyManager.Instance.AddSoldier(SoldierPath.Teknoloji, "Tas");
///   ArmyManager.Instance.TryMerge();   // Merge kapisi gecilince
///   ArmyManager.Instance.HealAll(0.5f); // HealSoldiers kapisi
/// </summary>
public class ArmyManager : MonoBehaviour
{
    public static ArmyManager Instance { get; private set; }

    [Header("Asker Prefab (bos birakabilirsin)")]
    public GameObject soldierPrefab;

    [Header("Sinirlar")]
    public int maxSoldiers = 20;

    // DEĞİŞİKLİK
[Header("Gorsel")]
[Range(0.8f, 1f)] public float soldierVisualScale = 0.88f;
    // ── Formasyon (20 slot) ───────────────────────────────────────────────
    // Oyuncu +Z yonune kosar; askerler arkasindan gelir (negatif Z offset).
    // Y=0 (LateUpdate'de 1.2f set edilir), xLimit dahilinde.
   // DEĞİŞİKLİK
static readonly Vector3[] FORMATION = new Vector3[20]
{
    new Vector3(-2.4f, 0f, -1.4f), new Vector3(-1.2f, 0f, -1.4f), new Vector3(0f, 0f, -1.4f), new Vector3(1.2f, 0f, -1.4f), new Vector3(2.4f, 0f, -1.4f),
    new Vector3(-3.0f, 0f, -2.6f), new Vector3(-1.5f, 0f, -2.6f), new Vector3(0f, 0f, -2.6f), new Vector3(1.5f, 0f, -2.6f), new Vector3(3.0f, 0f, -2.6f),
    new Vector3(-2.4f, 0f, -3.8f), new Vector3(-1.2f, 0f, -3.8f), new Vector3(0f, 0f, -3.8f), new Vector3(1.2f, 0f, -3.8f), new Vector3(2.4f, 0f, -3.8f),
    new Vector3(-3.0f, 0f, -5.0f), new Vector3(-1.5f, 0f, -5.0f), new Vector3(0f, 0f, -5.0f), new Vector3(1.5f, 0f, -5.0f), new Vector3(3.0f, 0f, -5.0f),
};

    // ── Asker listesi ─────────────────────────────────────────────────────
    readonly List<SoldierUnit> _soldiers = new List<SoldierUnit>(20);

    // ── Base stat tablosu (path bazlı) ───────────────────────────────────
    static readonly Dictionary<SoldierPath, (int hp, float atk, float spd)> SOLDIER_BASE
        = new Dictionary<SoldierPath, (int, float, float)>
    {
        [SoldierPath.Piyade]    = (80,  15f, 1.5f),
        [SoldierPath.Mekanik]   = (120,  8f, 4.0f),
        [SoldierPath.Teknoloji] = (50,  30f, 0.8f),
    };

    // ── Merge level stat carpanlari ───────────────────────────────────────
    static readonly float[] MERGE_MULT = { 1f, 1.8f, 3.5f, 7.0f }; // Lv1-Lv4

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Asker Ekle ────────────────────────────────────────────────────────
    /// <summary>
    /// Formasyona yeni asker ekler. Dolu ise false döner.
    /// biome: "Tas", "Orman" vb. — null ise BiomeManager'dan alinir.
    /// count: kac asker eklenecek (genellikle 1-2).
    /// </summary>
    public bool AddSoldier(SoldierPath path, string biome = null, int mergeLevel = 1, int count = 1)
    {
        bool added = false;
        for (int i = 0; i < count; i++)
        {
            if (_soldiers.Count >= maxSoldiers) break;

            string actualBiome = biome ?? BiomeManager.Instance?.currentBiome ?? "Tas";
            SoldierUnit unit = SpawnSoldierUnit(path, actualBiome, mergeLevel);
            if (unit == null) continue;

            _soldiers.Add(unit);
            AssignFormationOffsets();
            added = true;
        }

        if (added)
        {
            GameEvents.OnSoldierAdded?.Invoke(_soldiers.Count);
            Debug.Log($"[Army] +{count} {path} asker | Toplam: {_soldiers.Count}");
        }
        return added;
    }

    // ── Asker Kaldir (olgum) ─────────────────────────────────────────────
    public void RemoveSoldier(SoldierUnit unit)
    {
        if (!_soldiers.Contains(unit)) return;
        _soldiers.Remove(unit);
        AssignFormationOffsets();
        GameEvents.OnSoldierRemoved?.Invoke(_soldiers.Count);
        Debug.Log($"[Army] Asker dust | Kalan: {_soldiers.Count}");
    }

    // ── Merge ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Mevcut formasyonda 3x ayni-path+ayni-level bulursa birlestirir.
    /// Merge kapisi gecilince cagirilir. Birden fazla grup varsa hepsini birlestirir.
    /// Merge oldu mu? True döner.
    /// </summary>
    public bool TryMerge()
    {
        bool anyMerge = false;

        // Tekrar tekrar dene — zincir merge mumkun (Lv1→Lv2→Lv3)
        bool found;
        int  safetyLimit = 10;
        do
        {
            found = false;
            if (safetyLimit-- <= 0) break;

            foreach (SoldierPath path in System.Enum.GetValues(typeof(SoldierPath)))
            {
                for (int lv = 1; lv <= 3; lv++) // Lv4 max, daha fazla merge yok
                {
                    List<SoldierUnit> group = FindGroup(path, lv);
                    if (group.Count < 3) continue;

                    // 3 askeri kaldir, 1 lv+1 ekle
                    SoldierUnit first = group[0];
                    string biome = first.biome;

                    for (int i = 0; i < 3; i++)
                    {
                        SoldierUnit u = group[i];
                        _soldiers.Remove(u);
                        u.gameObject.SetActive(false);
                    }

                    // Yeni seviyeli asker olustur (aynı noktaya dogup buyuyecek)
                    SoldierUnit merged = SpawnSoldierUnit(path, biome, lv + 1);
                    if (merged != null) _soldiers.Add(merged);

                    AssignFormationOffsets();
                    GameEvents.OnSoldierMerged?.Invoke(path.ToString(), lv + 1);
                    Debug.Log($"[Army] MERGE: {path} Lv{lv} x3 → Lv{lv + 1}");

                    found     = true;
                    anyMerge  = true;
                    break;
                }
                if (found) break;
            }
        } while (found);

        return anyMerge;
    }

    // ── Tum Askerleri İyilesir ───────────────────────────────────────────
    /// <summary>pct = 0.5f → %50 max HP geri yükle.</summary>
    public void HealAll(float pct)
    {
        int totalHealed = 0;
        foreach (SoldierUnit u in _soldiers)
        {
            int before = u.currentHP;
            u.HealPercent(pct);
            totalHealed += u.currentHP - before;
        }
        GameEvents.OnSoldierHPRestored?.Invoke(totalHealed);
        Debug.Log($"[Army] HealAll %{pct*100:.0f} | Toplam iyilestirme: {totalHealed}");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Yardimci: formasyon offset atama
    void AssignFormationOffsets()
    {
        for (int i = 0; i < _soldiers.Count && i < FORMATION.Length; i++)
            _soldiers[i].formationOffset = FORMATION[i];
    }

    // Yardimci: path+level grubu bul
    List<SoldierUnit> FindGroup(SoldierPath path, int level)
    {
        var list = new List<SoldierUnit>();
        foreach (SoldierUnit u in _soldiers)
            if (u.path == path && u.mergeLevel == level) list.Add(u);
        return list;
    }

    // Yardimci: SoldierUnit olustur (prefab veya fallback kapsul)
    SoldierUnit SpawnSoldierUnit(SoldierPath path, string biome, int mergeLevel)
    {
        GameObject go;
        if (soldierPrefab != null)
        {
            go = Instantiate(soldierPrefab);
        }
        else
        {
            // Fallback: renkli kapsul
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.localScale *= soldierVisualScale;
            Destroy(go.GetComponent<CapsuleCollider>());
            var cc = go.AddComponent<CapsuleCollider>();
            cc.radius = 0.4f; cc.height = 1.1f; cc.isTrigger = true;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        // Konum: player konumu + ufak random offset (gecislerde capraz)
        if (PlayerStats.Instance != null)
            go.transform.position = PlayerStats.Instance.transform.position
                                    + new Vector3(Random.Range(-1f, 1f), 1.2f, -2f);

        // SoldierUnit bileşeni ekle (veya al)
        SoldierUnit unit = go.GetComponent<SoldierUnit>() ?? go.AddComponent<SoldierUnit>();

        // Statları ayarla
        var (hp, atk, spd) = SOLDIER_BASE[path];
        float mm = MERGE_MULT[Mathf.Clamp(mergeLevel - 1, 0, MERGE_MULT.Length - 1)];

        unit.path       = path;
        unit.biome      = biome;
        unit.mergeLevel = mergeLevel;
        unit.maxHP      = Mathf.RoundToInt(hp * mm);
        unit.currentHP  = unit.maxHP;
        unit.baseAtk    = atk;
        unit.atkSpeed   = spd;

        // Renk uygula
        Renderer rend = go.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Color c = unit.GetPathColor();
            if (rend.material.HasProperty("_BaseColor"))
                rend.material.SetColor("_BaseColor", c);
            else
                rend.material.color = c;
        }

        go.name = $"Soldier_{path}_Lv{mergeLevel}";
        go.SetActive(true);
        return unit;
    }

    // ── Getter'lar (HUD vb.) ─────────────────────────────────────────────
    public int  SoldierCount       => _soldiers.Count;
    public bool IsFull             => _soldiers.Count >= maxSoldiers;
    public int  GetCountByPath(SoldierPath path)
    {
        int n = 0;
        foreach (SoldierUnit u in _soldiers) if (u.path == path) n++;
        return n;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}