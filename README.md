# Top End War — README & AI Master Prompt v5

> Bu dosyayı herhangi bir yapay zekaya yapıştırarak projeyi sıfırdan anlatabilirsin.

---

## PROJE ÖZETİ

**Oyun Adı:** Top End War  
**Motor:** Unity 6.3 LTS (URP, 3D)  
**Platform:** Android / iOS  
**Repo:** https://github.com/LpeC0/Top-End-War  
**Tür:** Runner / Auto-Shooter  

---

## CORE LOOP

1. Koş — Player Z ekseninde otomatik ilerler
2. Sürükle — Basılı tut + sürükle ile şerit değiştir (40px eşik, göreceli)
3. Kapıdan Geç — +CP, x2, PathBoost, Merge, -CP kapıları
4. Düşman Yok Et — BoxCast auto-shoot, CP kazan
5. Tier Atla — CP eşiğinde model morph
6. Boss — Gökmedrese Muhafızı (henüz yapılmadı)

---

## HIERARCHy (Güncel)

```
SampleScene
  Directional Light
  PoolManager          → ObjectPooler.cs
  Player               → PlayerController + PlayerStats + MorphController  (Tag:Player)
      FirePoint
  Main Camera          → SimpleCameraFollow
  SpawnManager         → SpawnManager.cs  ← GateSpawner+EnemySpawner KALDIRILDI
  ChunkManager         → ChunkManager.cs
  Canvas
      CPText           TextMeshProUGUI
      TierText         TextMeshProUGUI — Inspector'da text BOŞ bırak!
      PopupText        TextMeshProUGUI
      SynergyText      TextMeshProUGUI
      PiyadeBar        Slider
      MekanizeBar      Slider
      TeknolojiBar     Slider
```

---

## SCRIPT TABLOSU

| Script | Açıklama | Yazar | Durum |
|--------|----------|-------|-------|
| PlayerController | Drag input, hareket, auto-shoot | Claude | OK |
| PlayerStats | CP/path/tier singleton, DefaultExecutionOrder(-10) | Claude | OK |
| SimpleCameraFollow | X sabit runner kamera | Claude | OK |
| GameEvents | Global static event merkezi | Gemini+Claude | OK |
| GateData | ScriptableObject kapı verisi | Gemini+Claude | OK |
| Gate | Fiziksel kapı, TMP yazı | Claude | OK |
| SpawnManager | Kapı+Düşman slot tabanlı, asla çakışmaz | Claude | OK |
| GameHUD | Observer pattern UI | Claude | OK |
| ObjectPooler | Queue nesne havuzu (tag:Bullet, size:20) | Gemini | OK |
| ChunkManager | Sonsuz yol (chunkLength=50) | Gemini | OK |
| MorphController | Tier→model swap+flash | Claude | OK |
| Enemy | Can, TakeDamage, cpReward, auto-cleanup | Claude | OK |
| Bullet | ObjectPooler SetActive | Claude | OK |
| ~~GateSpawner~~ | SpawnManager ile değiştirildi | - | KALDIR |
| ~~EnemySpawner~~ | SpawnManager ile değiştirildi | - | KALDIR |

---

## TEKNİK KARARLAR (DEĞİŞTİRİLMEZ)

- Player Rigidbody YOK
- Kamera X sabit, LateUpdate
- Input: Drag sürükleme (basılı tut+kaydır), Ok tuşu test
- Ateş: Physics.BoxCast
- Input Manager: Old/Legacy
- Cinemachine YOK
- Gate: Rigidbody(IsKinematic=true) + BoxCollider(IsTrigger=true)
- Spawn: SpawnManager slot sistemi (slotSize=30, hiç çakışma yok)
- Mermi: ObjectPooler.SpawnFromPool("Bullet")
- Player'da Bullet.cs ve Enemy.cs OLMAMALI

---

## TIER / CP

| Tier | CP | İsim |
|------|----|------|
| 1 | 0 | Gönüllü Er |
| 2 | 300 | Elit Komando |
| 3 | 800 | Gatling Timi |
| 4 | 2000 | Hava İndirme |
| 5 | 5000 | Sürü Drone |

## SİNERJİLER

| Kombinasyon | Sinerji |
|-------------|---------|
| Piyade>50% + Mekanize>25% | Exosuit Komutu |
| Piyade>50% + Teknoloji>25% | Drone Takımı |
| Mekanize>40% + Teknoloji>30% | Füzyon Robotu |
| Hepsi >25% | PERFECT GENETICS |

## KAPILER

| Tip | Etki |
|-----|------|
| AddCP | +value |
| MultiplyCP | xvalue |
| Merge | x1.8 |
| PathBoost_Piyade | +60CP PiyadePath+20 |
| PathBoost_Mekanize | +60CP MekanizePath+20 |
| PathBoost_Teknoloji | +60CP TeknolojiPath+20 |
| NegativeCP | -value (min20) |

---

## YAPILACAKLAR

| # | Görev | Durum |
|---|-------|-------|
| 1-13 | Hareket/kamera/gate/HUD/pool/chunk/spawn/morph/enemy | TAMAM |
| 14 | DOTween animasyonları (popup uçuş, tier punch) | SIRA |
| 15 | Game Over ekranı | BEKLIYOR |
| 16 | Boss sistemi (Gökmedrese Muhafızı, 3 faz) | BEKLIYOR |
| 17 | Türkiye haritası sahnesi | BEKLIYOR |
| 18 | Ses efektleri | BEKLIYOR |
| 19 | Android build + test | BEKLIYOR |

---

## DEĞİŞİKLİK LOG

```
[Mart 2026] Grok+Gemini: İlk kurulum, Python demo
[Mart 2026] Claude v1: PlayerController, PlayerStats, Gate sistemi, GameHUD
[Mart 2026] Gemini v4-v5: ObjectPooler, ChunkManager, Z=44.8 bug fix
[Mart 2026] Claude v2: Drag input, Bullet→ObjectPooler, Gate TMP yazı
[Mart 2026] Claude v3: MorphController, Enemy+cpReward, AddCPFromKill
[Mart 2026] Claude v4: SpawnManager (Gate+Enemy birleşik slot)
             PlayerStats [DefaultExecutionOrder(-10)]
             GateSpawner + EnemySpawner kaldırıldı
```

---

## YARDIM ŞABLONU

```
Projem Unity 6.3 LTS URP 3D mobil runner: Top End War
GitHub: https://github.com/LpeC0/Top-End-War
README tam hali: [bu dosya]

Scriptler: PlayerController, PlayerStats, SimpleCameraFollow,
GameEvents, GateData, Gate, SpawnManager, GameHUD, ObjectPooler,
ChunkManager, MorphController, Enemy, Bullet

[X] sistemini yazmak istiyorum.
Unity 6.3 LTS URP, Rigidbody YOK, Input Manager (Old), DOTween kurulu.
```
