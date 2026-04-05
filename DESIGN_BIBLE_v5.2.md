# Top End War — DESIGN BIBLE v5.2
**Repo:** https://github.com/LpeC0/Top-End-War  
**Motor:** Unity 6.3 LTS (URP, 3D) | **Platform:** Android / iOS  
**Güncelleme:** Nisan 2026

---

## DEĞİŞİKLİK LOGU

| Versiyon | Tarih | Değişiklik |
|----------|-------|-----------|
| v1-v4 | Oca-Mar 2026 | Temel sistem |
| v5.0 | Nis 2026 | CP/DPS ikili mimari (hatalı formülle) |
| v5.1 | Nis 2026 | DPS formülü düzeltildi, UI Bible v1, Ekonomi/Loot |
| **v5.2** | **Nis 2026** | **Multi-World yapısı (35 stage = 1 dünya değil). Rarity world'e kilitlendi. CommanderData sistemi eklendi. Slot seviyesi ve rarity ilişkisi netleşti. "1 haftada altın silah" problemi çözüldü.** |

---

## VİZYON

**"Koş, kapılardan geç, ordunu büyüt, doğru biyomu seç, boss'u ez. Sonra haritada büyü — yeni dünyalar, yeni komutanlar, yeni biyomlar."**

Oyun türü: **Hybrid-Casual / Mid-Core**  
Ana motivasyon: **Aksiyon + Progression + Keşif (Hybrid)**  
Temel his: Oyun "bitmez" hissi — her dünyanın sonu yeni bir dünyanın kapısıdır.

---

## CORE LOOP

```
[Meta-Hub: Dünya Haritası]
  → Komutan seç, ekipman kur
  → Stage seç (World X - Stage Y)

[Runner Modu]
  → Kapılardan geç → Build oluştur
  → Düşman dalgaları → auto-shoot
  → 1200m → Boss

[Boss Modu]
  → Sabit HP boss
  → Phase Shield fazları
  → Kazan → Victory Chest + Dünya ilerlemesi
  → Öl   → Revive (1x reklam) veya Retreat (%20 loot koru)

[Meta-Hub'a Dön]
  → Sandık aç, silah yükselt, haritada şehir/dünya aç
  → Yeni dünya = yeni biome = yeni komutan fırsatı
```

---

## 1. MULTI-WORLD YAPISI (ANA MİMARİ)

### World / Stage Hiyerarşisi

```
Dünya 1 (Sivas — Taş Biome)
  Stage 1-1 → 1-15
  Boss: 1-15  ← Dünya 1 Final Boss
  Ödül: Dünya 1 Sandığı + Harita'da Sivas yeşillenir

Dünya 2 (Tokat — Orman Biome)
  Stage 2-1 → 2-20
  Boss: 2-20
  Yeni mekanik: Merge sistemi açılır

Dünya 3 (Kayseri — Çöl Biome)
  Stage 3-1 → 3-25
  Boss: 3-25
  Yeni mekanik: Risk kapıları tam devreye

Dünya 4 (Erzurum — Karlı Biome)
  Stage 4-1 → 4-30
  Boss: 4-30
  Yeni mekanik: Arena açılır

Dünya 5+ (Malatya — Tarım ve ötesi)
  Devam eder...
```

Her dünya = yeni biyom = yeni görsel tema = yeni army path avantajı.

### Stage İçi Yapı (Her Stage)
- ~1200 metre runner yolu
- Kapı seti (Normal + Olay Kapıları)
- 3-6 düşman dalgası
- Stage sonunda küçük loot sıçrayışı

### Pacing Hedefi (Süre)
- Stage 1 serisi: ~2 dakika/stage (tutorial temposu)
- Stage 10+ serisi: ~3-4 dakika/stage
- Boss savaşı: 30-45 saniye aktif savaş
- Bir dünya tamamlama: Ortalama 2-4 hafta (günlük 15-20 dakika oynanan kullanıcı için)

---

## 2. RARITY PROGRESSION — WORLD'E KİLİTLİ

**Eski hatalı yaklaşım:** Rarity sadece stage sayısına bağlıydı → oyuncu 1 haftada Altın ekipmana erişiyordu.

**Doğru sistem:** Bir rariry seviyesi sadece o seviyeyi "açan" dünyaya girildiğinde drop edebilir.

| Dünya | Biome | Erişilebilir Rarity | Giriş Koşulu |
|-------|-------|---------------------|-------------|
| 1 | Taş (Sivas) | Gri, Yeşil | Başlangıç |
| 2 | Orman (Tokat) | Yeşil, Mavi | Dünya 1 Boss kesilmeli |
| 3 | Çöl (Kayseri) | Mavi, Mor | Dünya 2 Boss kesilmeli |
| 4 | Karlı (Erzurum) | Mor | Dünya 3 Boss kesilmeli |
| 5+ | Tarım (Malatya)+ | Mor, Altın | Dünya 4 Boss kesilmeli |

**Sonuç:** Altın ekipman görmek en erken 2-3 ay oynayan bir oyuncunun işi. "1 haftada bitirme" mümkün değil.

---

## 3. DPS SİSTEMİ (DOĞRU FORMÜL)

### Neden CP → DPS Dönüşümü Yapılmaz

`TotalDPS = (CP / 10) × weaponMult` **YANLIŞ.**  
CP sadece meta-hub Gear Score'udur. DPS tamamen statlardan hesaplanır.

### Doğru DPS Formülü

```
BaseDMG[tier] = { 60, 95, 145, 210, 300 }   ← tier 1-5

CommanderDPS = BaseDMG[tier]
             × WeaponDamageMult
             × SlotLevelMult
             × RarityMult
             × GlobalMult(ring)

BulletDamage = CommanderDPS / (FinalFireRate × BulletCount)
FinalFireRate = BASE_FIRE_RATES[tier] × WeaponFireRateMult
```

### Slot Seviyesi Formülü (Netleştirildi)

```
SlotLevelMult = 1 + (slotLevel × 0.03)    [max level 50 → +%150]

RarityMult:
  Gri    = 1.0
  Yeşil  = 1.3
  Mavi   = 1.7
  Mor    = 2.2
  Altın  = 3.0
```

**Kritik kural:** RarityMult her zaman dominant. Level 50 slot + Gri silah < Level 1 slot + Mor silah. Böylece yeni silah bulma motivasyonu ölmez.

**Örnek hesap (Tier 3, Mavi silah, Slot 20):**
```
BaseDMG = 145
WeaponMult = 1.0 (Tüfek)
SlotMult = 1 + (20 × 0.03) = 1.60
RarityMult = 1.7
→ CommanderDPS = 145 × 1.0 × 1.60 × 1.7 = 394 DPS
```

### Slot Seviyesi — Azalan Verimler (Gemini'nin önerisinden düzeltildi)

```
Level 1-10:  Her seviye +%5   → 10 seviye = +%50
Level 11-30: Her seviye +%3   → 20 seviye = +%60
Level 31-50: Her seviye +%1.5 → 20 seviye = +%30
Toplam max: +%140 (ama Rarity çarpanı bunu her zaman geçer)
```

---

## 4. ÇOKLU KOMUTAN SİSTEMİ

**Bu vizyon Gemini oturumunda tamamen atlandı. Şimdi geri eklendi.**

### CommanderData.cs (ScriptableObject)

```
name             : string
lore             : string (kısa geçmiş)
baseDMG[5]       : float array (tier 1-5)
baseFireRate[5]  : float array
baseHP[5]        : int array
specialty        : enum { Sniper, Assault, Support, Swarm }
armySynergy      : enum { Piyade, Mekanik, Teknoloji, Hybrid }
unlockCondition  : string ("World 2 Boss defeated")
tierVisuals[5]   : GameObject prefab array (her tier farklı model)
```

### Komutan Unlock Tablosu (Taslak)

| Komutan | Specialty | Army Synergy | Unlock |
|---------|-----------|-------------|--------|
| Gonullu Er | Assault (dengeli) | Hybrid | Başlangıç |
| Keşifçi | Sniper (yüksek hasar, yavaş) | Teknoloji | Dünya 2 Boss |
| Zırhlı | Support (yüksek HP, düşük hasar) | Mekanik | Dünya 3 Boss |
| Drone Komutanı | Swarm (çok mermi, az hasar) | Teknoloji | Dünya 4 Boss |
| ... | ... | ... | Dünya 5+ |

**Mimari notu:** `PlayerStats.cs` içinde `CommanderData activeCommander` alanı açılır. Tier tabloları, HP değerleri, ateş hızları — hepsi aktif komutanın SO'sundan okunur. Mevcut kod minimum değişimle adapte edilir.

### Build Identity Komuşmaya Doğru Bağlandı

Her komutan farklı army synergy → farklı kapı öncelikleri → farklı biyom avantajı. Oyuncu "Keşifçi + Teknoloji Askerleri + Taş Biyomu = %25 bonus" kombinasyonunu keşfedince tekrar oynamak için motivasyon oluşur.

---

## 5. DÜNYA 1 ZORLUK CETVELİ (Sivas — Taş Biome)

15 stage. Hedef: Oyuncuya tüm temel mekanikleri öğretmek + Gri→Yeşil rarity geçişini tattırmak.

| Stage | Zorluk Hissi | Beklenen Silah | Gereken DPS | Boss HP | Yeni Mekanik |
|-------|-------------|----------------|-------------|---------|-------------|
| 1-1 | God Mode | Gri Tüfek | 90 | 3.600 | Hareket + kapı |
| 1-2 | Kolay | Gri | 110 | 4.400 | İlk kırmızı kapı |
| 1-3 | Kolay | Gri | 135 | 5.400 | Olay kapısı tanıtımı |
| 1-4 | Normal | Gri | 160 | 6.400 | Düşmanlar ateş eder |
| 1-5 | Normal | Gri→Yeşil | 200 | 8.000 | ★ Yeşil silah ödülü |
| 1-6 | HP sıçraması | Yeşil | 270 | 10.800 | Yeni silahı test et |
| 1-7 | Normal | Yeşil | 320 | 12.800 | |
| 1-8 | Normal | Yeşil | 370 | 14.800 | Risk kapısı giriyor |
| 1-9 | Normal | Yeşil | 420 | 16.800 | |
| 1-10 | Zor | Yeşil | 480 | 19.200 | Mini-Boss 1 |
| 1-11 | HP sıçraması | Yeşil+ | 580 | 23.200 | Merge açılıyor (öğreti) |
| 1-12 | Normal | Yeşil+ | 660 | 26.400 | |
| 1-13 | Normal | Yeşil+ | 740 | 29.600 | |
| 1-14 | Zor | Yeşil+ | 820 | 32.800 | |
| **1-15** | **Boss** | **Yeşil+** | **920** | **36.800** | **★ Dünya 1 Final Boss** |

**Dünya 1 Boss:** 36.800 HP, Phase Shield @%60 ve %30, biyom: Taş, Teknoloji ordusu ×1.25.  
Kazanınca: Sivas haritada yeşillenir, Dünya 2 açılır, Dünya 1 Sandığı düşer.

---

## 6. KAPI SİSTEMİ v2

### Kapı Türleri (Mevcut GateData.cs — 14 tip, effectType 0-11)

| Tür | Etki | Hissiyat |
|-----|------|---------|
| Flat Damage (+ATK) | BaseDMG artar | Erken oyun güçlü |
| Fire Rate (+%) | Görsel mermi yoğunluğu | Mid-game |
| Multiplier (+% DMG) | Bileşik büyüme | Late-game |
| AddSoldier | Ordu büyür | Army build |
| PathBoost | Path % skoru | Biyom hazırlığı |
| Merge | Asker güçlenir | Mid-game |
| HealCommander | HP yenilenir | Survival |
| HealSoldiers | Asker tam HP | Army build |
| NegativeCP | CP düşer | Risk önünde |
| RiskReward | 3 kapıya bonus | Stratejik kumar |

### Olay Kapıları (her 5 normalden 1)
| Tip | Açıklama |
|-----|----------|
| TEKLİ | 1 büyük kapı ×1.6, altın sarısı |
| DUEL | 2 kapı — %50 yer değiştirir |
| ÜÇLÜ | 3 kapı, ortadaki önde |

### Risk Kapısı v2 (MEVCUT KODDA UYGULANMIŞ)
3 sonraki kapıya tip bazlı bonus — AddCP×1.5, AddSoldier+1, Merge çarpan+0.2, HealCommander+100 kalıcı MaxHP, vb.

### Soft Cap (Gizli Bariyer)
```
Bir stage'de max artış: startCP × 2.5
if (playerCP > startCP × 2.0):
    Flat Damage / AddSoldier şansı ↓
    Heal / Hız / Crit şansı ↑
```

---

## 7. KOMUTAN HP VE TIER

| Tier | CP Eşiği | Komutan Adı (Gonullu Er) | Base FireRate/s | BaseDMG | HP |
|------|---------|--------------------------|----|---------|-----|
| 1 | 0 | Gonullu Er | 1.5 | 60 | 500 |
| 2 | 300 | Elit Komando | 2.5 | 95 | 700 |
| 3 | 900 | Gatling Timi | 4.0 | 145 | 950 |
| 4 | 2500 | Hava Indirme | 6.0 | 210 | 1200 |
| 5 | 6000 | Suru Drone | 8.5 | 300 | 1500 |

HP = TierHP + TotalEquipmentHPBonus()  
Tier atlayınca: Zırh görünümü değişir, Aura değişir, Mermi VFX heybetlenir, mini slow-mo event.

---

## 8. ASKER SİSTEMİ

Max 20 asker. V formasyonu.

| Path | ATK | Atış/s | Biome Bonus |
|------|-----|--------|-------------|
| Piyade | 15 | 1.5 | Orman ×1.20, Tarım ×1.25 |
| Mekanik | 8 | 4.0 | Çöl ×1.20, Taş ×1.10 |
| Teknoloji | 30 | 0.8 | Taş ×1.25, Karlı ×1.15 |

Askerler hasar ALMAZ — düşman temas ederse anında düşer (pozisyon sistemi).

Merge: 3× Lv1 → 1× Lv2 (×1.8) → Lv3 (×3.5) → Lv4 (×7.0)

---

## 9. BOSS SİSTEMİ — Phase Shield

**Her boss sabit HP ile gelir (stage'e göre), oyuncuya göre ölçeklenmez.**

| Faz | HP Aralığı | Mekanik |
|-----|-----------|---------|
| 1 | %100→%60 | Normal |
| Phase Shield | %60 | 2sn invuln + animasyon |
| 2 | %60→%30 | Minyon dalga |
| Phase Shield | %30 | 2sn invuln + Enrage |
| 3 (Enrage) | %30→0 | Hız ×2.2, hasar ↑ |

---

## 10. BİYOM × PATH MATRİSİ

| Biyom | Piyade | Mekanik | Teknoloji |
|-------|--------|---------|-----------|
| Taş (Sivas) | ×0.90 | ×1.10 | **×1.25** |
| Orman (Tokat) | **×1.20** | ×1.00 | ×0.85 |
| Çöl (Kayseri) | ×1.10 | **×1.20** | ×1.00 |
| Karlı (Erzurum) | ×1.15 | ×0.85 | **×1.15** |
| Tarım (Malatya) | **×1.25** | ×1.10 | ×0.80 |

---

## 11. EKONOMİ VE LOOT DÖNGÜSÜ

### Para Birimleri
| Tür | Kaynak | Kullanım |
|-----|--------|---------|
| Altın (Soft) | Stage sonları, offline gelir | Slot level up |
| Tech Core | Boss drop, combo ödülü, skill-based drop | Slot upgrade materyali |
| Mor Kristal (Hard) | Boss drop, satın alma | Big Scroll, Revive |
| Basic Scroll | Stage drop | Gri-Mavi rarity gacha |
| Big Scroll | Dünya Boss, premium | Mavi-Mor garantili |

**Tech Core kuralı:** Sadece oyun içi başarıdan düşer — Boss fazı atlatmak, kusursuz kapı serisi, combo. Mağazadan satılmaz. Skill-to-Progress.

### Slot Geliştirme (Slot Leveling)
Oyuncu tek tek silah değil "silah slotunu" geliştirir. Yeşil silah takıp sonra Mor bulduğunda slot seviyesi sıfırlanmaz.

```
UpgradeCost(level) = level × 100 Altın + Tech Core
Max slot level = 50
```

### Dünya Tamamlama Ödülü
Bir dünyayı bitirince:
- Dünya Sandığı (özel loot)
- Haritada o bölge yeşillenir + 3D figürler değişir
- Pasif Altın/saat artar
- Bir sonraki dünyanın kilidini açar

### Stage İçi Micro-Loot
Her stage'in %50 ilerleme noktasında küçük bir ara ödül (Altın kesesi veya 1 Tech Core).

### Offline Kazanç
- Her geçilen stage saatlik Altın üretimine katkı sağlar
- Dünya tamamlama Altın üretim çarpanını artırır
- Offline cap: 15 saat (oyuncu kapanda hissetmesin)

### Ölüm — Revive Paneli
```
[Revive]  → Reklam izle → Tam can + 3sn kalkan (run başına 1x)
[Retreat] → Run biter, %20 loot cebinde kalır
```

### Pity Timer
20 boş stage → 21. stage %100 Scroll garantisi.

---

## 12. UI BİBLE v1.0

### Görsel Stil
- Arka plan: #1A1A1A (Charcoal)
- Vurgu: #00F5FF (Neon Cyan)
- Tehlike: #FF3131 (Electric Red)
- Düşman corrupt bölge: Turuncu-kahverengi
- Ele geçirilmiş bölge: Canlı yeşil + bölgeye özgü 3D figürler

### Meta-Hub (Ana Ekran)
```
[ÜST BAR]   Altın | Tech Core | Kristal   (değişince parlar)

[MERKEZ]    TÜRKİYE HARİTASI
            → Corrupt bölge: turuncu, düşman figürleri
            → Temizlenmiş: yeşil, "Bölge Ele Geçirildi" transparan yazı
            → Altın ikonları havada yüzer (pasif kazanç)

[ALTI]      [TOPLA] butonu — biriken offline kazanç

[SOL ALT]   Komutan Avatar (Tier Aurası döner)
            Yanında: GEAR SCORE: 4.500

[ALT BAR]   📦 Envanter | 🛒 Dükkan | ⚔️ SAVAŞ | 🗺️ Harita | 🏆 Arena
```

### Oyun İçi HUD
```
[ÜST]        ━━━━━━━━━●━━━━━━━━  (Neon ilerleme barı)
[OYUNCU]     x15  (Asker sayacı)
[SAĞ ÜST]   450m  (Mesafe)
[KAPI]       "+50 ATK" → 0.3sn yukarı uçar
```

---

## 13. LOKALIZASYON (Erken Kurulum)

Hardcoded string YAZILMAZ. Tüm metinler anahtar (key) üzerinden çekilir:
```csharp
LocalizationManager.Instance.GetText("STG_SIVAS_1") // "Sivas - Sınır Boyu"
```
JSON veya Unity Localization Package kullanılacak (hardcoded Dictionary değil).

---

## 14. GELECEK PLANLARI (Future GDD Entegrasyonu)

- **3-Lane Challenge Mode:** Haftalık etkinlik veya Zorlu Bölge
- **Pet Sistemi:** 7. ekipman slotu, anchor'da heal/DR aura
- **Arena (Asenkron PvP):** Dünya 4 açılışıyla — oyuncuların kurduğu ordular karşılaşır
- **Biome Shader Değişimi:** Her dünyanın görsel atmosferi tamamen farklı
- **Level Editor:** Oyuncu kendi haritasını yapabilir (JSON)

---

## 15. TEKNİK KISITLAR (Değişmez)

```
xLimit = 8
Player Rigidbody: YOK
Cinemachine: YOK
Input: Old/Legacy
Namespace: YOK
GameEvents: Action<> — Raise...() YOK
PlayerStats.CP: Property
DPS: BaseDMG[tier] × mults  (CP/10 KULLANILMAZ)
Gate shader: Sprites/Default
Gate Panel: QUAD
Bullet: OverlapSphere
Enemy tag: "Enemy" (büyük E)
Asker Max: 20
DOTween: kurulu
startCP: KALDIRILDI — starter equipment zorunlu
Fixed Difficulty: Düşman HP stage'e göre sabit
```

---

## 16. YAPILACAKLAR (Öncelik Sırası)

| # | Görev | Durum |
|---|-------|-------|
| 1-36 | Mevcut runner/boss/equipment altyapısı | ✅ |
| 37 | PlayerStats v6: GetTotalDPS() tier bazlı | 📋 |
| 38 | PlayerController v5: BulletDamage = DPS/(rate×BulletCount) | 📋 |
| 39 | BossManager v6: Phase Shield %60 ve %30 | 📋 |
| 40 | DifficultyManager v3: exponent→1.1, cpScalingFactor→0.5 | 📋 |
| 41 | **CommanderData.cs:** ScriptableObject, 5 komutan alanları | 🔲 YENİ |
| 42 | **PlayerStats v7:** activeCommander referansı, SO'dan okuma | 🔲 YENİ |
| 43 | **WorldConfig.cs:** World ID, biome, stage sayısı, rarity cap SO | 🔲 YENİ |
| 44 | **StageConfig.cs:** Stage ID, World ID, HP cetveli SO | 🔲 YENİ |
| 45 | **StageManager.cs:** World/Stage yükleme, loot, offline katkı | 🔲 YENİ |
| 46 | EconomyManager.cs: Altın/TechCore/Kristal | 📋 |
| 47 | GameOverUI v3: Revive + Retreat | 📋 |
| 48 | SpawnManager v13: Soft Cap | 📋 |
| 49 | TierVisualizer.cs: Aura + model (boyut değil) | 📋 |
| 50 | MapManager.cs: Corrupt→Temiz geçiş | 📋 |
| 51 | UIManager.cs: Dashboard, panel geçişleri | 📋 |
| 52 | LocalizationManager.cs: JSON tabanlı | 🔲 |
| 53 | Equipment UI (6 slot overlay) | 🔲 |
| 54 | Ses efektleri | 🔲 İLERİDE |
| 55 | Android build | 🔲 İLERİDE |
