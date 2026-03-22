# Top End War — MASTER PROMPT v12
**Repo:** https://github.com/LpeC0/Top-End-War  
**Motor:** Unity 6.3 LTS (URP, 3D) | **Platform:** Android / iOS

---

## VERSİYON GEÇMİŞİ

| Versiyon | Değişiklik |
|----------|-----------|
| v1-v3 | Grok+Gemini+Claude: temel runner sistemi |
| v4-v6 | Claude: drag, pool, morph, spawn |
| v7 | Claude: DDA, RiskReward, pity timer, 3 dalga |
| v8 | Claude: HP bar, GameOver, GateFeedback |
| v9 | Claude: MorphController crash fix (PrewarmModels) |
| v10 | Claude: GPT tahribatı temizlendi, namespace kaldırıldı |
| v11 | Claude: ArmyManager, BiomeManager, CommanderHP, EquipmentData v1, PetController, BossManager v5 (41k HP) |
| **v12** | **Claude: EquipmentData v2 (6 slot, silah tipleri, DR, HP bonus, cpMultiplier), SpawnManager v12 (Olay Kapısı: Tekli/Duel/Üçlü), PlayerController v4 (FindTarget anchor fix), PlayerStats v5 (TotalDamageReduction/TotalEquipmentHPBonus), SaveManager, GameOverUI v2, GameStartup** |

---

## OYUN TANIMI

Runner/auto-shooter. Player otomatik koşar, sürükleme ile serbest hareket.  
Yolda matematiksel kapılar (sol/sağ + olay kapıları). Düşmanlar dalga halinde, auto-shoot.  
CP = savaş gücü, tier atlarken model morph.  
1200m boss, sonra Türkiye haritasında yeni şehir.

---

## DEĞİŞTİRİLEMEZ KURALLAR

```
xLimit = 8              PlayerController + Enemy + SpawnManager.ROAD_HALF_WIDTH AYNI
Player Rigidbody: YOK   transform.position hareketi
Cinemachine: YOK        SimpleCameraFollow
Input: Old/Legacy
Namespace: YOK
GameEvents: Action<>    Raise...() metod YOK
PlayerStats.CP          Property (public field değil) — _baseCP + equipment bonusları
Gate shader: Sprites/Default
Gate Panel: QUAD (Cube değil)
SetActive(false)        pool için (Destroy değil)
Unicode sembol KULLANMA
Player'a Enemy.cs/Bullet.cs EKLEME
DOTween kurulu
Bullet: OverlapSphere   OnTriggerEnter değil
Enemy tag: "Enemy"      zorunlu, büyük E
"Soldier" tag: YOK
```

---

## HIERARCHY

```
SampleScene
  GameStartup          — FPS + Quality + Sleep ayarları
  PoolManager          ObjectPooler (Bullet:30, Enemy:20)
  SaveManager          — PlayerPrefs yüksek skor
  DifficultyManager    DifficultyManager + ProgressionConfig
  GameOverManager      GameOverUI v2
  BossManager          bossMaxHP=41000
  ArmyManager          maxSoldiers=20
  BiomeManager         currentBiome="Tas"
  Player               PlayerController + PlayerStats + MorphController
                       + GateFeedback + PetController [Tag:Player]
      FirePoint
  Main Camera          SimpleCameraFollow
  SpawnManager         SpawnManager v12 (eventGateEvery=5)
  ChunkManager         ChunkManager (RoadChunk X scale=1.6)
  Canvas → HUDPanel    GameHUD v8
    commanderHPSlider  ← Inspector'da bağlanmalı!
    soldierCountText   ← Inspector'da bağlanmalı!
  EventSystem
```

---

## TÜM SCRIPT TABLOSU

| Script | Versiyon | Durum | Özet |
|--------|---------|-------|------|
| ArmyManager.cs | v1 | ✅ | Max 20 asker, V formasyon, merge |
| BiomeManager.cs | v1 | ✅ | Biyom × path hasar matrisi |
| BossManager.cs | v5 | ✅ | HP=41k, biyom zayıflığı, dinamik boss adı |
| Bullet.cs | v4 | ✅ | OverlapSphere, hızlı gizle |
| ChunkManager.cs | v1 | ✅ | Sonsuz yol |
| DifficultyManager.cs | v2 | ✅ | DDA, SmoothedPowerRatio |
| Enemy.cs | v4 | ✅ | Separation cache, EnemyHealthBar, SaveManager.RegisterKill |
| EnemyHealthBar.cs | v1 | ✅ | WorldSpace HP bar |
| EquipmentData.cs | **v2** | ✅ | 6 slot, WeaponType, ArmorType, damageMultiplier, DR, cpMultiplier |
| GameEvents.cs | v4 | ✅ | Commander HP + Asker + Biyom eventleri |
| GameHUD.cs | v8 | ✅ | Slider fill rect düzeltildi, komutan HP bar, asker sayısı |
| GameOverUI.cs | **v2** | ✅ | Skor + mesafe + kill + en iyi skor, yeni rekor vurgusu |
| GameStartup.cs | v1 | ✅ | FPS=60, shadows=off, mobile quality |
| Gate.cs | v7 | ✅ | Sprites/Default shader |
| GateData.cs | v3 | ✅ | 14 gate tipi |
| GateFeedback.cs | v1 | ✅ | DOTween scale pop + kamera shake |
| MorphController.cs | v2 | ✅ | OnBiomeChanged shader wiring |
| ObjectPooler.cs | v1 | ✅ | Bullet:30, Enemy:20 |
| PetController.cs | v1 | ✅ | Pet takip + anchor DR aura |
| PetData.cs | v1 | ✅ | cpBonus, anchorDamageReduction |
| PlayerController.cs | **v4** | ✅ | FindTarget() — anchor'da OverlapSphere, normal'de BoxCast |
| PlayerStats.cs | **v5** | ✅ | 6 ekipman slotu, TotalDamageReduction(), TotalEquipmentHPBonus() |
| ProgressionConfig.cs | v1 | ✅ | ScriptableObject |
| SaveManager.cs | **v1** | ✅ | PlayerPrefs: highCP, highDist, totalRuns, totalKills |
| SimpleCameraFollow.cs | v1 | ✅ | X sabit |
| SoldierUnit.cs | v2 | ✅ | path-based shooting, BiomeManager multiplier |
| SpawnManager.cs | **v12** | ✅ | Olay Kapısı: Tekli/Duel/Üçlü, Z offset, ölçek |

---

## SIRADAKI GÖREVLER (öncelik sırası)

### Hemen Yapılacak
1. **Inspector: commanderHPSlider bağla** — Canvas → GameHUD → commanderHPSlider alanına sahne slider'ını sürükle
2. **Inspector: SaveManager ekle** — Hierarchy → Create Empty → "SaveManager" → script bağla
3. **Inspector: GameStartup ekle** — Hierarchy → Create Empty → "GameStartup" → script bağla

### Sonraki Kod Turları
4. **Risk Kapısı v2** — AddSoldier/Merge/Heal kapılarına da etki etmeli (plan: DESIGN_BIBLE §5)
5. **DamagePopup.cs** — Floating damage text, renk kodlu (plan: DESIGN_BIBLE §6)
6. **Equipment UI** — In-game overlay, 6 slot gösterir
7. **Mob zorluk düzeltmesi** — `difficultyExponent: 1.3 → 1.1` veya `playerCPScalingFactor: 0.9 → 0.7`

---

## EKİPMAN DEĞERLERİ REFERANSI

### Silah Oluşturma (EquipmentData ScriptableObject)
```
Assets → Create → TopEndWar → Equipment
slot = Weapon
weaponType = [Pistol/Rifle/Automatic/Sniper/Shotgun]

TABANCA (Pistol):
  fireRateMultiplier = 1.5
  damageMultiplier = 0.7
  baseCPBonus = 50
  rarity = 1

OTOMATIK (Automatic):
  fireRateMultiplier = 2.2
  damageMultiplier = 0.6
  baseCPBonus = 150
  rarity = 3

KESKİN NİŞANCI (Sniper):
  fireRateMultiplier = 0.35
  damageMultiplier = 3.5
  baseCPBonus = 200
  rarity = 4
```

### Zırh Oluşturma
```
ORTA ZIRH:
  slot = Armor
  armorType = Medium
  commanderHPBonus = 200
  damageReduction = 0.12
  baseCPBonus = 100
  rarity = 2

KALKAN:
  slot = Armor
  armorType = Shield
  commanderHPBonus = 150
  damageReduction = 0.30
  rarity = 3
```

---

## BOSS KALİBRASYONU (10k simülasyon)

```
Boss HP = 41.000 | Biyom: Taş | Zayıflık: Teknoloji ×1.25

T3 + 10 Teknoloji Lv1 + Taş:  ~75s  ✓
T3 + 10 Piyade Lv1 + Taş:     ~108s ✓ (yanlış path = daha zor)
T4 + 15 Teknoloji Lv2:        ~39s  ✓
T5 + 5 Teknoloji Lv4:         ~19s  (max güç)
```

---

## KULLANIM ŞABLONU

```
Projem Unity 6.3 LTS URP 3D mobil runner: Top End War
GitHub: https://github.com/LpeC0/Top-End-War
MASTER PROMPT v12: [bu dosya]
DESIGN BIBLE v4: [DESIGN_BIBLE_v4.md]

Mevcut scriptler: [SCRIPT TABLOSUNA BAK]
Son değişiklikler: [VERSİYON GEÇMİŞİNE BAK]

Kural:
- Namespace yok
- Unity 6.3 LTS URP
- Input Legacy
- DOTween kurulu
- PlayerStats.CP property (_baseCP + equipment)
- GameEvents Action<> pattern, Raise() yok
```

---

## DEĞİŞİKLİK GEÇMİŞİ (detay)

```
v12 (Mar 2026 — bu tur):
  + EquipmentData v2: 6 slot (Weapon/Armor/Shoulder/Knee/Necklace/Ring)
    WeaponType enum (Pistol/Rifle/Automatic/Sniper/Shotgun)
    ArmorType enum (Light/Medium/Heavy/Shield)
    damageMultiplier, damageReduction, commanderHPBonus, cpMultiplier, spreadBonus
  + PlayerStats v5: TotalDamageReduction(), TotalEquipmentHPBonus()
    6 equippedXxx alanı, kolye CP çarpanı, max %60 DR
  + PlayerController v4: FindTarget() 
    anchor mod → OverlapSphere 70 birim (boss kesin yakalanır)
    normal mod → BoxCast (mevcut)
    damageMultiplier silah hasarına uygulanır
  + SpawnManager v12: Olay Kapısı sistemi
    EventType: Single / Duel / Triple
    İlerlemeye göre ağırlık değişir
    Triple: Z+4 offset, 0.75 scale (iç içe geçmez)
  + SaveManager v1: PlayerPrefs highCP/highDist/totalRuns/totalKills
  + GameOverUI v2: skor + mesafe + kill + rekor vurgusu
  + GameStartup v1: FPS=60, shadows=off, mobile quality
  + Enemy.cs: Die()→ SaveManager.RegisterKill()

  PLAN (sonraki tur):
  - Risk Kapısı v2 (tüm gate tiplerine etki)
  - DamagePopup.cs (floating damage text)
  - Equipment UI (in-game 6 slot overlay)
  - Mob zorluk düzeltmesi (exponent 1.3→1.1)
```
