# Top End War — MASTER PROMPT v13
**Repo:** https://github.com/LpeC0/Top-End-War  
**Motor:** Unity 6.3 LTS (URP, 3D) | **Platform:** Android / iOS

---

## VERSİYON GEÇMİŞİ

| Versiyon | Değişiklik |
|----------|-----------|
| v1-v11 | Temel sistem, runner, boss, equipment altyapısı |
| v12 | EquipmentData v2, SpawnManager v12, PlayerStats v5, SaveManager, GameOverUI v2 |
| **v13** | **DPS formülü düzeltildi (CP/10 kaldırıldı). Multi-World yapısı. Rarity world'e kilitlendi. CommanderData sistemi. SlotLevelMult + RarityMult ayrımı. Başlangıç komutanı zorunlu.** |

---

## OYUN TANIMI

Runner/auto-shooter. Hybrid-Casual / Mid-Core.  
Oyuncu otomatik koşar, sürükleme ile serbest hareket.  
**Multi-World yapısı:** Her dünya = farklı bölge + farklı biyom + farklı rarity eşiği.  
**Çoklu komutan:** Her dünya boss sonrası yeni komutan açılır.  
**CP = Gear Score (meta-hub). DPS = statlardan hesaplanır (CP'den değil).**

---

## DEĞİŞTİRİLEMEZ KURALLAR

```
xLimit = 8
Player Rigidbody: YOK
Cinemachine: YOK
Input: Old/Legacy
Namespace: YOK
GameEvents: Action<> — Raise...() YOK
PlayerStats.CP: Property (_baseCP + equipment bonusları)
DPS FORMÜLÜ: BaseDMG[tier] × WeaponDmgMult × SlotLevelMult × RarityMult × GlobalMult
             CP/10 KULLANILMAZ — magic number hatası
startCP: KALDIRILDI — starter equipment + commander zorunlu
Gate shader: Sprites/Default / Gate Panel: QUAD
Bullet: OverlapSphere / Enemy tag: "Enemy" (büyük E)
Fixed Difficulty: Düşman HP stage'e göre sabit, oyuncuya göre değil
Rarity: World'e kilitli — Altın World 5+'tan önce drop etmez
```

---

## TEMEL FORMÜLLER

```csharp
// ── DPS ─────────────────────────────────────────────────────────────────
float[] BASE_DMG        = { 60, 95, 145, 210, 300 };      // tier 1-5
float[] BASE_FIRE_RATES = { 1.5f, 2.5f, 4.0f, 6.0f, 8.5f }; // tier 1-5

// Slot çarpanı (max level 50 → +%150)
float SlotLevelMult = 1f + (slotLevel * 0.03f);

// Rarity çarpanı (her zaman dominant)
// Gri=1.0 / Yeşil=1.3 / Mavi=1.7 / Mor=2.2 / Altın=3.0

public float GetTotalDPS()
{
    int   idx        = Mathf.Clamp(CurrentTier - 1, 0, 4);
    float baseDMG    = BASE_DMG[idx];
    float weaponMult = equippedWeapon != null ? equippedWeapon.damageMultiplier : 1f;
    float slotMult   = InventoryManager.Instance.GetSlotMult(EquipmentSlot.Weapon);
    float rarityMult = equippedWeapon != null ? GetRarityMult(equippedWeapon.rarity) : 1f;
    float globalMult = equippedRing   != null ? equippedRing.globalDmgMultiplier   : 1f;
    return baseDMG * weaponMult * slotMult * rarityMult * globalMult;
}

public float GetBaseFireRate()
{
    int   idx       = Mathf.Clamp(CurrentTier - 1, 0, 4);
    float equipMult = equippedWeapon != null ? equippedWeapon.fireRateMultiplier : 1f;
    return BASE_FIRE_RATES[idx] * equipMult;
}

// PlayerController AutoShoot:
// bulletDamage = Mathf.RoundToInt(GetTotalDPS() / (GetBaseFireRate() * BulletCount))

// ── Gelen Hasar ──────────────────────────────────────────────────────────
// FinalIncoming = EnemyDMG × (1 - TotalDamageReduction) [DR max 0.60]

// ── Sabit Düşman HP (stage'e göre, oyuncuya göre değil) ─────────────────
// MobHP  = StageConfig.mobHP   (SO'dan gelir)
// BossHP = StageConfig.bossHP  (SO'dan gelir)

// ── Boss Phase Shield ────────────────────────────────────────────────────
// %60 HP → 2sn invuln + Faz 2
// %30 HP → 2sn invuln + Enrage (hız ×2.2)

// ── Rarity Çarpanı ───────────────────────────────────────────────────────
float GetRarityMult(int rarity) => rarity switch {
    1 => 1.0f,  // Gri
    2 => 1.3f,  // Yeşil
    3 => 1.7f,  // Mavi
    4 => 2.2f,  // Mor
    5 => 3.0f,  // Altın
    _ => 1.0f
};
```

---

## MULTI-WORLD YAPISI

```
Dünya 1 (Sivas — Taş)     : Stage 1-1 → 1-15  | Rarity: Gri, Yeşil
Dünya 2 (Tokat — Orman)   : Stage 2-1 → 2-20  | Rarity: Yeşil, Mavi
Dünya 3 (Kayseri — Çöl)   : Stage 3-1 → 3-25  | Rarity: Mavi, Mor
Dünya 4 (Erzurum — Karlı) : Stage 4-1 → 4-30  | Rarity: Mor
Dünya 5+ (Malatya+)       : Devam               | Rarity: Mor, Altın

Her dünya boss sonrası:
  → Haritada bölge yeşillenir
  → Yeni komutan açılır
  → Bir sonraki dünya açılır
  → Dünya Sandığı düşer
```

---

## KOMUTAN SİSTEMİ

```csharp
// CommanderData.cs — ScriptableObject
public class CommanderData : ScriptableObject
{
    public string commanderName;
    public float[] baseDMG       = { 60, 95, 145, 210, 300 };
    public float[] baseFireRate  = { 1.5f, 2.5f, 4.0f, 6.0f, 8.5f };
    public int[]   baseHP        = { 500, 700, 950, 1200, 1500 };
    public CommanderSpecialty specialty;  // Assault/Sniper/Support/Swarm
    public ArmySynergy armySynergy;       // Piyade/Mekanik/Teknoloji/Hybrid
    public string unlockCondition;        // "World2BossDefeated"
    public GameObject[] tierModels;       // Tier 1-5 prefab
    public ParticleSystem[] tierAuras;
}

// PlayerStats içinde:
// public CommanderData activeCommander;
// GetTotalDPS() → activeCommander.baseDMG[tier] kullanır
```

**Komutan unlock tablosu:**
```
Gonullu Er  → Başlangıç (Assault, Hybrid)
Kesifci     → Dünya 2 Boss (Sniper, Teknoloji)
Zirhli      → Dünya 3 Boss (Support, Mekanik)
Drone Kom.  → Dünya 4 Boss (Swarm, Teknoloji)
...         → Dünya 5+
```

---

## HIERARCHY

```
SampleScene
  GameStartup
  PoolManager          Bullet:30, Enemy:20
  SaveManager
  EconomyManager       Altın / TechCore / Kristal
  StageManager         World/Stage yükleme (WorldConfig + StageConfig SO)
  DifficultyManager    v3 (exponent→1.1, cpScalingFactor→0.5)
  GameOverManager      GameOverUI v3 (Revive + Retreat)
  BossManager          Phase Shield @%60 ve %30
  ArmyManager          maxSoldiers=20
  BiomeManager         currentBiome=WorldConfig'den gelir
  Player               PlayerController v5 + PlayerStats v7 + MorphController
                       + GateFeedback + PetController [Tag:Player]
      FirePoint
  Main Camera          SimpleCameraFollow
  SpawnManager         v13 (Soft Cap)
  ChunkManager
  Canvas → HUDPanel    GameHUD v8
  EventSystem
```

---

## TÜM SCRIPT TABLOSU

| Script | Ver | Durum | Özet |
|--------|-----|-------|------|
| ArmyManager.cs | v1 | ✅ | Max 20 asker |
| BiomeManager.cs | v1 | ✅ | Biyom × path matrisi |
| BossManager.cs | **v6** | 📋 | Phase Shield @%60 ve %30 |
| Bullet.cs | v4 | ✅ | OverlapSphere |
| ChunkManager.cs | v1 | ✅ | Sonsuz yol |
| **CommanderData.cs** | **v1** | 🔲 YENİ | Komutan SO |
| DifficultyManager.cs | **v3** | 📋 | exponent→1.1 |
| EconomyManager.cs | **v1** | 📋 | Altın/TechCore/Kristal |
| Enemy.cs | v4 | ✅ | |
| EnemyHealthBar.cs | v1 | ✅ | |
| EquipmentData.cs | **v3** | 📋 | globalDmgMultiplier eklendi (cpMultiplier yerine DPS için) |
| GameEvents.cs | v4 | ✅ | |
| GameHUD.cs | v8 | ✅ | |
| GameOverUI.cs | **v3** | 📋 | Revive + Retreat |
| GameStartup.cs | v1 | ✅ | |
| Gate.cs | v7 | ✅ | |
| GateData.cs | v3 | ✅ | 14 tip |
| GateFeedback.cs | v1 | ✅ | |
| **InventoryManager.cs** | **v1** | 🔲 YENİ | Slot leveling, merge, itemID bazlı karşılaştırma |
| LocalizationManager.cs | **v1** | 🔲 YENİ | JSON tabanlı, hardcoded yok |
| MapManager.cs | **v1** | 📋 | Corrupt→Temiz geçiş |
| MorphController.cs | v2 | ✅ | |
| ObjectPooler.cs | v1 | ✅ | |
| PetController.cs | v1 | ✅ | |
| PetData.cs | v1 | ✅ | |
| PlayerController.cs | **v5** | 📋 | AutoShoot DPS formülü |
| PlayerStats.cs | **v7** | 📋 | activeCommander, GetTotalDPS() slot+rarity dahil |
| ProgressionConfig.cs | v1 | ✅ | |
| SaveManager.cs | v1 | ✅ | |
| SimpleCameraFollow.cs | v1 | ✅ | |
| SoldierUnit.cs | v2 | ✅ | |
| SpawnManager.cs | **v13** | 📋 | Soft Cap |
| **StageConfig.cs** | **v1** | 🔲 YENİ | World ID, stage HP cetveli SO |
| **StageManager.cs** | **v1** | 🔲 YENİ | World/Stage yükleme runtime |
| TierVisualizer.cs | **v1** | 📋 | Aura + model |
| UIManager.cs | **v1** | 📋 | Dashboard geçişleri |
| **WorldConfig.cs** | **v1** | 🔲 YENİ | Dünya SO: biome, rarity cap, stage sayısı |

---

## SIRADAKI GÖREVLER

### Hemen (Savaş matematiği)
1. `PlayerStats v7` — GetTotalDPS() slot+rarity çarpanları dahil, activeCommander referansı
2. `PlayerController v5` — BulletDamage = DPS / (rate × BulletCount)
3. `EquipmentData v3` — `cpMultiplier` → `globalDmgMultiplier` yeniden adlandır
4. `BossManager v6` — Phase Shield
5. `DifficultyManager v3` — exponent → 1.1

### Sonraki Tur (World sistemi)
6. `CommanderData.cs` — Komutan ScriptableObject
7. `WorldConfig.cs` — Dünya yapılandırma SO
8. `StageConfig.cs` — Stage HP cetveli SO
9. `StageManager.cs` — Runtime world/stage yükleme
10. `InventoryManager.cs` — itemID bazlı merge, slot leveling

---

## EKİPMAN REFERANSI

### EquipmentData v3'te Değişiklik
```
// ESKİ (kaldırıldı)
public float cpMultiplier = 1f;  // DPS için yanıltıcı isim

// YENİ
public float globalDmgMultiplier = 1f;  // Yüzük/Kolye için global hasar çarpanı
```

### Starter Equipment (Tutorial)
```
İlk çalıştırmada otomatik:
  Gri (rarity=1) Tüfek    → damageMult=1.0, fireRateMult=1.0, CP=75
  Gri (rarity=1) Hafif Zırh → HP+100, DR=0.05, CP=40
  Başlangıç Komutanı: "Gonullu Er" CommanderData SO
```

### Rarity Çarpanları
```
Gri   (rarity 1): ×1.0
Yeşil (rarity 2): ×1.3
Mavi  (rarity 3): ×1.7
Mor   (rarity 4): ×2.2
Altın (rarity 5): ×3.0  ← sadece World 5+'tan drop eder
```

---

## KULLANIM ŞABLONU (Başka AI'ya)

```
Projem Unity 6.3 LTS URP 3D mobil runner: Top End War
MASTER PROMPT v13 + DESIGN BIBLE v5.2

Kurallar:
- Namespace yok, Input Legacy, DOTween kurulu
- Multi-World: Her dünya farklı biome + rarity eşiği
- Çoklu komutan: CommanderData SO, PlayerStats.activeCommander
- DPS = BaseDMG[tier] × WeaponMult × SlotLevelMult × RarityMult × GlobalMult
  (CP/10 YAPILMAZ — magic number)
- BulletDamage = DPS / (fireRate × BulletCount)
- Fixed Difficulty: HP stage'e göre sabit
- Boss Phase Shield: %60 ve %30'da 2sn invuln
- startCP kaldırıldı, starter equipment + commander zorunlu
- globalDmgMultiplier kullan (cpMultiplier değil DPS için)
- itemID ile merge karşılaştır (string itemName değil)
- Localization: hardcoded string değil, key tabanlı
```
