# Top End War — DESIGN BIBLE v2
**Repo:** https://github.com/LpeC0/Top-End-War
**Motor:** Unity 6.3 LTS (URP, 3D) | **Platform:** Android / iOS
**Son Güncelleme:** Mart 2026 | **Durum:** Sivas bölümü playable

---

## DEĞİŞİKLİK LOGU (v1 → v2)

| Tarih | Değişiklik | Sebep |
|-------|-----------|-------|
| v2 | MultiplyCP x2 kaldırıldı | 5 kapıda T5'e ulaşılıyordu |
| v2 | AddCP_huge (+200) kaldırıldı | Erken T5 |
| v2 | Kapı scale: dist/800 → dist/2400 | Çok hızlı değer artışı |
| v2 | Merge x1.8 → x1.5 | Tek kapıda tier atlama |
| v2 | Tier eşikleri: {0,300,800,2000,5000} → {0,500,1500,4000,9000} | Geç T4-T5 |
| v2 | AddBullet kapısı eklendi (%12) | Mermi = "asker hissi" |
| v2 | Boss HP: 3000 → 150000 | 3 saniyede ölüyordu |
| v2 | Bullet: isTrigger çakışması → OverlapSphere | Düşman hasarı yoktu |
| v2 | Anchor modu: OnBossEncountered → player durur | Boss savaşı |
| v2 | Lead targeting kaldırıldı | Görsel saçmalık |
| v2 | PathBoost: float += 20f → int sayaç | Sınırsız birikme |
| v2 | Enemy Tag eksikti → Unity ayarı notu eklendi | Mermi çarpmıyordu |

---

## 1. VİZYON VE OYUN MODLARI

### Şu An (Faz 1 — Runner)
Tek bir komutan koşar. BulletCount = "ordu gücü" temsili.
Kapılardan geçerek güçlenir, boss'u yen, haritada ilerle.

### Sonra (Faz 2 — Komutan + Asker) ← B seçeneği
Oyuncu 1 Komutan + yanında/arkasında askerler yönetir.
CP = birim savaş gücü. +Asker kapısı hem asker hem CP ekler.
Askerler düşmandan hasar alabilir, kaybedilebilir.
Path yönüne göre asker tipi değişir (Piyade/Mekanize/Teknoloji).
Bu mod için ArmyManager.cs yazılacak (ileride).

---

## 2. MATEMATİK ALTYAPI

### CP → Tier → DPS Zinciri

```
Tier Eşikleri: {0, 500, 1500, 4000, 9000}

Tier | CP    | İsim          | Hasar | Ateş/sn | DPS(1m)
-----|-------|---------------|-------|---------|--------
 1   |  0    | Gonullu Er    | 60    | 1.5     | 90
 2   | 500   | Elit Komando  | 95    | 2.5     | 237
 3   | 1500  | Gatling Timi  | 145   | 4.0     | 580
 4   | 4000  | Hava Indirme  | 210   | 6.0     | 1260
 5   | 9000  | Suru Drone    | 300   | 8.5     | 2550

DPS(N mermi) = DPS(1m) × N
```

### Kapı Değer Scale

```
scale = 1 + (distance / 2400)
0m   → 1.0x
400m → 1.17x
800m → 1.33x
1200m → 1.5x

Örnek: AddCP(+80) kapısı
  0m'de  → +80 CP
  800m'de → +106 CP
  1200m'de → +120 CP
```

### Boss Dengesi (simülasyon onaylı)

```
Boss HP = 150000
Faz mekanikleri ≈2x uzama

Tier+Mermi | Raw DPS | Süre(raw) | Efektif
-----------|---------|-----------|--------
T3 + 2m   | 1160    | 129s      | ~260s (çok zor)
T3 + 3m   | 1740    |  86s      | ~172s (zor)
T4 + 2m   | 2520    |  60s      | ~120s (normal)
T4 + 3m   | 3780    |  40s      | ~80s  (kolay)
T4 + 4m   | 5040    |  30s      | ~60s  (çok kolay)
T5 + 2m   | 5100    |  29s      | ~58s  (çok kolay)

Simülasyon dağılımı: %69 T3, %25 T4, %2 T5, %5 T1-T2
→ Çoğu oyuncu T3+2-3m ile boss görür = 80-170s savaş ✓
```

### Kapı Sistemi

```
Ağırlık tablosu (TOPLAM = 1.00):
  AddCP +80     0.28 | %28  ← en sık
  AddCP +45     0.22 | %22
  PathBoost×3   0.06 | %6 her biri = %18 toplam
  AddBullet     0.12 | %12
  Merge x1.5    0.07 | %7
  RiskReward    0.04 | %4
  MultiplyCP×1.2 0.04 | %4 (sürpriz, nadir)
  NegativeCP    0.05 | %5

Pity Zone (son 200m): Sadece AddCP, PathBoost, AddBullet
```

### Kapı Tipi Detayları

**AddCP:** Flat artış × scale. Temel büyüme motoru.

**PathBoost (Piyade/Mekanize/Teknoloji):**
- CP artışı + path sayacı +1
- Path sayaçları normalize edilerek sinerji kontrol edilir
- CheckSynergy: Piyade>%50 + Teknoloji>%25 = "Drone Takimi"
- Merge geldiğinde dominant path'e göre rol belirlenir

**AddBullet:**
- +1 mermi (max 5), küçük CP bonus
- Mermi sayısı = DPS çarpanı (2m = 2x DPS)
- V formasyonu spread (1m=düz, 5m=±18°, 0°, ±9°)
- "Daha fazla asker" hissini temsil eder (Faz 2'de gerçek asker olacak)

**Merge:**
- x1.5 CP çarpımı
- Path sayaçlarını sıfırlar, rol atar
- Oyunda 30 kapı → ortalama 2 Merge = x1.5 × x1.5 = x2.25 etki
- Erken Merge (CP=300): +150 CP → önemsiz
- Geç Merge (CP=3000): +1500 CP → tier atlama

**MultiplyCP x1.2:**
- Nadir sürpriz (%4)
- Her zaman x1.2 sabit — data.effectValue kullanılmıyor

**RiskReward:**
- -30% CP şimdi, sonraki 3 kapıya +50% bonus
- Oyuncu geçmek zorunda değil (stratejik seçim)
- Boss'a yakın çıkmaz (Pity Zone)

**NegativeCP:**
- Sabit -60 × scale
- %5 ağırlık — nadir ama sürpriz

---

## 3. ZORLUK SİSTEMİ (DDA)

```
BaseDifficulty = 1 + (Distance/1000)^1.3
EnemyHP    = 100 × Difficulty × PlayerPowerFactor
EnemyDmg   = 25  × Difficulty
EnemySpeed = min(4 + (Diff-1)×0.35, 7.5)
ExpectedCP = 200 × 1.15^(Distance/100)
SmoothedRatio = Lerp(prev, CP/ExpectedCP, 0.08f)

DifficultyManager her 50m'de hesaplar. Config olmadan çalışır.
```

### Düşman Tablosu

| Mesafe | HP  | Hasar | Hız | Düşman/Dalga |
|--------|-----|-------|-----|--------------|
| 0      | 100 | 25    | 4.0 | 2-3          |
| 300    | 185 | 42    | 4.8 | 3-4          |
| 600    | 340 | 72    | 5.7 | 4-6          |
| 900    | 520 | 100   | 6.5 | 6-7          |
| 1200   | 700 | 130   | 7.2 | 7-8          |

---

## 4. BOSS: GÖKMEDRESE MUHAFIZI

**HP:** 150000 | **Spawn:** Z = 1200m

```
Faz 1 (HP %100-60): Normal yaklaşma (2.5 birim/sn)
  → Oyuncu X hareket, auto-shoot
Faz 2 (HP %60-30):  Minyon spawn (3 adet, 8 sn arayla)
  → Renk: turuncu
Faz 3 (HP %30-0):   Hız x2.2
  → Renk: kırmızı, minyon yok

Anchor modu: boss gelince player durur, sadece X hareket
Zafer: +2000 CP, haritada yeni şehir
```

---

## 5. BÖLÜM TASARIMI (Sivas — Şu An)

| Mesafe | Zorluk | Dalga | Açıklama |
|--------|--------|-------|---------|
| 0-300  | Kolay  | Normal | Öğretici |
| 300-800 | Orta  | Normal+Agir | Ritim |
| 800-1200 | Zor  | Tümü | Yoğun |
| 1000-1200 | Pity | Sadece pozitif | Boss hazırlık |
| 1200+ | Boss  | — | Anchor modu |

**Tahmini süre:** 3-4 dakika

---

## 6. GELECEKTEKİ BÖLÜMLER (Türkiye Haritası)

```
Sivas    (Bozkır — başlangıç, şu an aktif)
  ↗ Tokat    (Orman — dalga tipi: pusu)
  → Kayseri  (Dağlık — yüksek savunmalı düşmanlar)
  ↘ Erzurum  (Karlı — yavaş koşu, teknoloji bonusu)
  ← Malatya  (Tarım — bol AddBullet kapısı)
```

### Biome Etkisi (İleride Implemente Edilecek)

Her biome farklı **path bonusu** verir:

| Biome   | Dominant Path | Bonus |
|---------|--------------|-------|
| Sivas   | Piyade       | Normal başlangıç |
| Kayseri | Mekanize     | Zırhlı düşmanlar, Mekanize etkili |
| Erzurum | Teknoloji    | Yavaş koşu ama +%30 ateş hızı |
| Malatya | Piyade       | +%50 AddBullet kapı ağırlığı |
| Tokat   | Teknoloji    | Pusu dalgaları (FlankWave %70) |

---

## 7. FAZ 2 ROADMAP (Komutan + Asker Sistemi)

### Fark: Faz 1 vs Faz 2

| Faz 1 (Şu An) | Faz 2 (Sonra) |
|---------------|--------------|
| 1 karakter koşar | 1 Komutan + N asker |
| BulletCount = "güç" | Her birim ayrı entity |
| CP = tier + ateş hızı | CP = birim toplam gücü |
| Düşman sadece player'a hasar | Düşman askerleri öldürebilir |
| +Mermi kapısı = daha fazla ateş | +Asker kapısı = yeni unit spawn |

### Faz 2 Matematik

```
Toplam DPS = KomutanDPS + Σ(AskerDPS_i)
KomutanDPS = Tier hasar × ateş hızı (şu anki sistem)
AskerDPS = AskerTier hasar × asker ateş hızı

Asker Tipleri (Path'e göre):
  Piyade:    HP=150, Hasar=40, Hız=5/sn, Ateş=1.5/sn
  Mekanize:  HP=280, Hasar=70, Hız=3/sn, Ateş=1.0/sn (ağır)
  Teknoloji: HP=100, Hasar=90, Hız=5/sn, Ateş=2.0/sn (kırılgan+güçlü)
```

### Gerekli Yeni Scriptler

```
ArmyManager.cs      — Asker pool, formation, kayıp takibi
SoldierUnit.cs      — Bireysel asker AI (takip + oto ateş)
FormationLayout.cs  — V/line/wedge formation kontrol
```

---

## 8. TEKNİK KISITLAR (Değişmez)

```
xLimit = 8           PlayerController + Enemy + SpawnManager AYNI
Player Rigidbody YOK
Cinemachine YOK
Input: Old/Legacy
Namespace YOK
GameEvents: Action<>  (Raise...() metod yok)
PlayerStats.CP: Property (field değil)
Gate Panel: Sprites/Default shader
Bullet: OverlapSphere (isTrigger çakışması — trigger+trigger = çarpışmaz)
Unicode sembol KULLANMA
DOTween kurulu
```

---

## 9. UNITY AYARLARI (Kontrol Listesi)

### Zorunlu Tag'lar

```
Player (GameObject tag)
Enemy  (Enemy prefab tag — EKSİK olursa mermi çarpmaz!)
Bullet (Bullet prefab tag — opsiyonel, kod GetComponent kullanıyor)
```

### ObjectPooler Kurulumu

```
PoolManager → ObjectPooler → Pools:
  Tag: "Bullet"  Prefab: BulletPrefab  Size: 40
  Tag: "Enemy"   Prefab: EnemyPrefab   Size: 25
```

### BulletPrefab Ayarları

```
Rigidbody: UseGravity=false, CollisionDetection=Continuous
SphereCollider: Radius=0.25, IsTrigger=true (görsel, hasar OverlapSphere)
Scale: (0.2, 0.2, 0.5) — ince uzun
```

### PlayerController Ayarları

```
FirePoint: Player altında Empty obje, pozisyon (0, 1, 1)
BulletPrefab: BulletPrefab bağlı
DetectRange: 40
```

### BossManager Kurulumu

```
Hierarchy → Create Empty → "BossManager" → BossManager.cs ekle
BossMaxHP: 150000
Prefab boş bırak (fallback kırmızı küp çalışır)
```

---

## 10. SCRIPT TABLOSU (Güncel)

| Script | Görev | Durum |
|--------|-------|-------|
| PlayerController.cs | Drag, ateş, anchor modu | v4 |
| PlayerStats.cs | CP, tier, path, bullet count | v3 |
| GameEvents.cs | Action<> event merkezi | v2 |
| GateData.cs | ScriptableObject + spawnWeight | v2 |
| Gate.cs | Sprites/Default, triggered flag | v7 |
| SpawnManager.cs | Ağırlıklı seçim, balanced gates | v9 |
| DifficultyManager.cs | DDA, standalone | v2 |
| BossManager.cs | 3 faz, anchor, HP=150k | v4 |
| BossHitReceiver.cs | Boss hasar alımı | v2 |
| Enemy.cs | Initialize DDA, separation | v4 |
| Bullet.cs | OverlapSphere hit, anında kaybol | v4 |
| EnemyHealthBar.cs | World-space HP bar | v1 |
| MorphController.cs | PrewarmModels, DOTween | v3 |
| ObjectPooler.cs | Queue pool | Gemini |
| ChunkManager.cs | Sonsuz yol | Gemini |
| SimpleCameraFollow.cs | X sabit kamera | v1 |
| GameHUD.cs | Observer UI, auto-build | v6 |
| GameOverUI.cs | Programatik, TimeScale=0 | v1 |
| GateFeedback.cs | DOTween scale pop | v1 |
| ProgressionConfig.cs | DDA sabitleri SO | v1 |

---

## 11. SONRAKI ADIMLAR (Öncelik Sırası)

| # | Görev | Durum |
|---|-------|-------|
| 1 | Mermi hasar testi (Enemy tag) | BEKLIYOR |
| 2 | Boss denge testi (HP=150k) | BEKLIYOR |
| 3 | Kapı boyutu/pozisyon fine-tune | BEKLIYOR |
| 4 | 3D model entegrasyonu (AI gen) | BEKLIYOR |
| 5 | Ses efektleri | BEKLIYOR |
| 6 | ArmyManager (Faz 2 başlangıç) | PLANLANDI |
| 7 | Türkiye harita sahnesi | PLANLANDI |
| 8 | Kayseri bölümü (2. biome) | PLANLANDI |
| 9 | Android build + test | PLANLANDI |
| 10 | Monetizasyon kancaları | İLERİ VADE |
