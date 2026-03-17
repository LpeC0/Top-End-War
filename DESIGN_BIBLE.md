# Top End War — DESIGN BIBLE v3
**Repo:** https://github.com/LpeC0/Top-End-War  
**Motor:** Unity 6.3 LTS (URP, 3D) | **Platform:** Android / iOS  
**Guncelleme:** Mart 2026

> Kod degisikliklerinde ONCE buraya bakilmali.  
> Herhangi bir AI'a verildiginde projeyi sifirdan anlatir.

---

## DEGİSİKLİK LOGU (v3)

| Degisiklik | Sebep |
|-----------|-------|
| GPT'nin tum degisiklikleri geri alindi | GPT GameEvents'i bozdu, partial class ekledi, namespace koydu |
| GameEvents: Raise...() metodlari KALDIRILDI | Bizim sistemimiz basit Action<> — abonelik += ile |
| PlayerStats: currentCP field → CP property | GPT public field yapmisti, yanlis |
| PlayerStats: partial class → normal class | Unity'de sorun cikariyor |
| HandleMerge PlayerStats icine alindi | Gate.cs'de dis bagimlilk olmadan |
| PlayerController.fireRate/damage field'i YOK | GPT ekledi ama bizde yok |
| Namespace YOK | GPT namespace TopEndWar.Progression eklemisti |
| DESIGN_BIBLE v1+v2+GPT merged dosyalar tek belgede toplandı | |

---

## VİZYON

**"Her 30-45 saniyede bir guçlenme hissi veren, stratejik kapi secimi olan Türkiye temali mobil runner."**

Oyuncu **kos → sec → guclen → yok et** dongusunde:
- Kapidan gece **secim yapar** (sol mu sag mi?)
- Dogru secim = tier atlama + dopamin
- Yanlis secim = geriye dusum
- Dusman oldurme CP kazandirir ama **kapi secimi asil guc kaynagi**

---

## CORE LOOP

```
Kos (auto Z, forwardSpeed=10)
  → Kapidan gec (sol/sag — oyuncu karari)
    → CP degisir
      → Tier atlarsa MORPH (DOTween scale pop)
  → Dusman dalgasi (auto-shoot, tier=atis hizi)
    → CP kazan
  → 1200m → BOSS (Gokmedrese Muhafizi)
    → Yenerse Turkiye haritasinda yeni sehir
```

---

## OYUNCU HAREKETI

- **Serbest surukleme** — 3 serit YOK
- Parmak basili + kaydirarak hareket, birakinca durur
- `xLimit = 8` (yol genisligi 16 birim)
- Kapiya girmek icin dogru konuma suruklemek gerekir (skill-based)
- Klavye: Sol/Sag ok tus (test)

---

## ATIS MATEMATIGI

Tier yukseldikce **atis HIZI** artar (spread degil — her seferinde 1 mermi):

| Tier | CP | Isim | Hasar | Atis/sn | DPS |
|------|----|------|-------|---------|-----|
| 1 | 0 | Gonullu Er | 60 | 1.5 | 90 |
| 2 | 300 | Elit Komando | 95 | 2.5 | 237 |
| 3 | 800 | Gatling Timi | 145 | 4.0 | 580 |
| 4 | 2000 | Hava Indirme | 210 | 6.0 | 1260 |
| 5 | 5000 | Suru Drone | 300 | 8.5 | 2550 |

Tier 1 → 120 HP dusmani ~1.3s. Tier 2'de 0.5s — fark hissedilir.

---

## KAPI SİSTEMİ

Yolda her zaman bir cift kapi cikar. Oyuncu birinden gecer.

### Kapi Turleri

| Tip | Kilo | Renk | Etki |
|-----|------|------|------|
| AddCP | %40 | Yesil | +deger |
| MultiplyCP | %25 | Mavi | xdeger |
| Merge | %10 | Mor | x1.8 + path skor kontrolu |
| PathBoost | %15 | Turuncu | +CP + path yonu |
| NegativeCP | %3 | Kirmizi | -deger |
| RiskReward | %7 | Sari | -30% CP + sonraki 3 kapiya +50% bonus |

### Pity Timer
Boss'a 200 birim kala NegativeCP ve RiskReward cikmaz.  
`SpawnManager.PickGate(pity=true)` → sadece pozitif havuz.

### Merge Kapisi Path Davranisi
Path skoruna gore:
- Teknoloji ≥ %50 → Drone Takimi rolü
- Piyade ≥ %50 → Piyade Timi rolü  
- Mekanize ≥ %50 → Mekanize Timi rolü
- Hicbiri → standart x1.8 CP

Su an rol efekti: x1.8 CP + path skoru sifirlanir.  
Gelecek: companion spawn, MorphController'a rol bildirimi.

---

## ZORLUK SISTEMI (DDA)

```
BaseDifficulty = 1 + (Distance/1000)^1.3
EnemyHP    = 100 × Difficulty × PlayerPowerFactor
EnemyDmg   = 25  × Difficulty
EnemySpeed = min(4 + (Diff-1)×0.35, 7.5)
ExpectedCP = 200 × 1.15^(Distance/100)  (her 100m %15 buyume)
SmoothedRatio = Lerp(prev, CP/ExpectedCP, 0.08f)  (ani spike azalt)
```

DifficultyManager her **50 birimde** hesaplar. Config olmadan da calısır.

### Zorluk Tablosu

| Mesafe | HP | Hasar | Hiz | Dusman/Dalga |
|--------|-----|-------|-----|-------------|
| 0 | 100 | 25 | 4.0 | 2–3 |
| 300 | 185 | 42 | 4.8 | 3–4 |
| 600 | 340 | 72 | 5.7 | 4–6 |
| 900 | 520 | 100 | 6.5 | 6–7 |
| 1200 | 700 | 130 | 7.2 | 7–8 |

---

## DALGA TİPLERİ

| Tip | Frekans | Duzen |
|-----|---------|-------|
| Normal | %50 (her zaman) | Grid 4 sutun |
| Agir | %25 (>%25 ilerleme) | Merkez yogunlasma |
| Kanat | %25 (>%25 ilerleme) | Iki yandan sarma |

---

## BOLUM TASARIMI (Sivas)

| Mesafe | Zorluk | Dalga | Aciklama |
|--------|--------|-------|---------|
| 0–300 | Kolay | Normal | Ogretici |
| 300–800 | Orta | Normal+Agir | Ritim artar |
| 800–1200 | Zor | Tumü | Yogun |
| 1000–1200 | Pity Zone | Sadece pozitif kapi | Boss hazirlik |
| 1200+ | Boss | — | Gokmedrese Muhafizi |

**Tahmini sure:** 3–4 dakika

---

## BOSS — GOKMEDRESE MUHAFIZI

**HP:** 3000 | **Tetiklenme:** Z=1200

| Faz | Isim | HP | Mekanik |
|-----|------|----|---------|
| 1 | Tas Zirh | %100–60 | Kalkan |
| 2 | Minyon | %60–30 | Kopyalar |
| 3 | Cekirdek | %30–0 | Hizlandi |

**Overload Kapisi:**
- All-In (Altin): Tum CP → x3 hasar
- Split (Mavi): CP yarilir → guvenli

**Hala yazilmadi** → siradaki buyuk gorev.

---

## PATH SİSTEMİ & SİNERJİLER

| Kombinasyon | Sinerji |
|-------------|---------|
| Piyade>50% + Mekanize>25% | Exosuit Komutu |
| Piyade>50% + Teknoloji>25% | Drone Takimi |
| Mekanize>40% + Teknoloji>30% | Fuzyon Robotu |
| Hepsi >25% | PERFECT GENETICS |

---

## ANCHOR MODU (Gelecek)

Belirli kapılar "Anchor" verir:
1. `forwardSpeed = 0`
2. Kamera yukari cekileir
3. 10s dalga savunmasi (auto-shooter)
4. `forwardSpeed = 10` — kosüye devam

---

## TEKNİK KISITLAR (Degistirilemez)

```
xLimit = 8              PlayerController + Enemy + SpawnManager.ROAD_HALF_WIDTH AYNI
Player Rigidbody YOK    transform.position ile hareket
Cinemachine YOK         SimpleCameraFollow (X sabit)
Input: Old/Legacy       New Input System degil
Namespace YOK           Tum scriptler global namespace'te
GameEvents: Action<>    Raise...() metod yok, abonelik += ile
PlayerStats.CP          Property (public field degil)
Gate shader: Sprites/Default  (renk garantili)
Gate Panel: QUAD (Cube degil)
Bullet: SetActive(false) pool'a don (Destroy degil)
Enemy: SetActive(false)  pool'a don
Unicode sembol KULLANMA  LiberationSans desteklemiyor
Player'a Enemy.cs/Bullet.cs EKLEME
mat.SetColor("_BaseColor") degil — mat.color veya Sprites/Default
DOTween kurulu
```

---

## SCRIPT TABLOSU

| Script | Gorev | Yazar | Durum |
|--------|-------|-------|-------|
| PlayerController.cs | Drag, Z hareket, tier atis hizi+hasari | Claude | AKTIF |
| PlayerStats.cs | CP property, tier, path, HandleMerge, RiskBonus | Claude | AKTIF |
| SimpleCameraFollow.cs | X sabit runner kamera | Claude | AKTIF |
| GameEvents.cs | Global Action<> event merkezi | Claude | AKTIF |
| GateData.cs | ScriptableObject, RiskReward dahil | Claude | AKTIF |
| Gate.cs | Sprites/Default shader, triggered flag | Claude | AKTIF |
| SpawnManager.cs | Grid+pity+3 dalga, standalone, runtime gate | Claude | AKTIF |
| GameHUD.cs | Observer UI, auto-build, flash | Claude | AKTIF |
| ObjectPooler.cs | Queue nesne havuzu | Gemini | AKTIF |
| ChunkManager.cs | Sonsuz yol chunkLength=50 | Gemini | AKTIF |
| MorphController.cs | PrewarmModels, DOTween pop, crash fix | Claude | AKTIF |
| Enemy.cs | Initialize DDA, separation cached, HP bar | Claude | AKTIF |
| Bullet.cs | Pool SetActive, SetDamage(), mor renk | Claude | AKTIF |
| EnemyHealthBar.cs | World-space HP bar, otomatik | Claude | AKTIF |
| ProgressionConfig.cs | DDA sabit SO (opsiyonel) | Claude | AKTIF |
| DifficultyManager.cs | Runtime DDA, pity zone, standalone | Claude | AKTIF |
| GameOverUI.cs | Programatik Canvas, Time.scale=0 | Claude | AKTIF |
| GateFeedback.cs | DOTween scale pop + cam shake | Claude | AKTIF |

---

## HIERARCHY

```
SampleScene
  PoolManager         ObjectPooler  (Bullet:20, Enemy:20)
  DifficultyManager   DifficultyManager + ProgressionConfig (opsiyonel)
  GameOverManager     GameOverUI
  Player              PlayerController + PlayerStats + MorphController + GateFeedback  [Tag:Player]
      FirePoint
  Main Camera         SimpleCameraFollow
  SpawnManager        SpawnManager  (GatePrefab, EnemyPrefab, GateDataList — hepsi opsiyonel)
  ChunkManager        ChunkManager  (RoadChunk X scale=1.6)
  Canvas
      CPText          TextMeshProUGUI
      TierText        TextMeshProUGUI    ← Inspector TEXT BOSALT
      PopupText       TextMeshProUGUI
      SynergyText     TextMeshProUGUI
      DamageFlash     Image (Stretch, alpha=0, RaycastTarget=false)
      PiyadeBar       Slider
      MekanizeBar     Slider
      TeknolojiBar    Slider
      HUDPanel        GameHUD.cs  ← tum referanslar bagla (veya bos birak, otomatik)
```

---

## YAPILACAKLAR

| # | Gorev | Durum |
|---|-------|-------|
| 1-13 | Temel sistem | TAMAM |
| 14 | DDA (DifficultyManager standalone) | TAMAM |
| 15 | RiskReward kapisi + Pity Timer | TAMAM |
| 16 | 3 dalga tipi | TAMAM |
| 17 | MorphController crash fix | TAMAM |
| 18 | Enemy HP bar | TAMAM |
| 19 | GameOver (programatik) | TAMAM |
| 20 | GateFeedback DOTween | TAMAM |
| 21 | HandleMerge path-bazli PlayerStats icerisinde | TAMAM |
| 22 | **BossManager** (3 faz, Overload) | BEKLIYOR |
| 23 | **Anchor Modu** | BEKLIYOR |
| 24 | Merge → companion spawn (PlayerPathManager) | BEKLIYOR |
| 25 | Turkiye haritasi sahnesi | BEKLIYOR |
| 26 | 3D model uretimi (AI prompt) | BEKLIYOR |
| 27 | Ses efektleri | BEKLIYOR |
| 28 | Android build | BEKLIYOR |

---

## RİSK VE DİKKAT NOKTALARI

| Risk | Etki | Cozum |
|------|------|-------|
| DifficultyManager config bos | Yok — dahili sabitler | Config opsiyonel |
| Gate tetiklenmiyor | Player Collider yok | CapsuleCollider(isTrigger=false) |
| xLimit farklı degerler | Sinir disina cikma | 3 scriptte AYNI = 8 |
| Unicode hata | Console spam | TR karakter kullanma |
| MorphController crash | Duzeltildi | PrewarmModels + SetActive |
| GameEvents Raise...() cagrisi | Compile hata | Action<> pattern kullan |

---

## GELECEK (Onceki Kararlar)

Henuz implement edilmedi, tasarimda var:
- **PlayerPathManager** — companion spawn (rol bazli)
- **Analytics events** — GateChosen, TierUp, BossFailReason
- **VFX Pooling** — morph particle pool'a alinabilir
- **A/B test** — NegativeCP agirlik %3 vs %5

---

## DEGİSİKLİK GECMİSİ

```
v1  Grok+Gemini: Python demo, Unity kurulum
v2  Claude: PlayerController, PlayerStats, Gate, GameHUD
v3  Gemini: ObjectPooler, ChunkManager, Z=44.8 bug fix
v4  Claude: Drag input, Bullet→Pool, MorphController, SpawnManager
v5  Claude: Serbest drag, coklu mermi, xLimit=8
v6  Claude: triggered flag, transparan gate, hasar flash
v7  Claude: RiskReward, Pity Timer, 3 dalga, DifficultyManager, DDA
v8  Claude: Enemy HP bar, GameOver, GateFeedback DOTween
v9  Claude: MorphController crash fix (PrewarmModels)
v10 Claude: GPT tahribati temizlendi — GameEvents, PlayerStats, namespace,
            HandleMerge PlayerStats'a tasindi, DESIGN_BIBLE v3
```
