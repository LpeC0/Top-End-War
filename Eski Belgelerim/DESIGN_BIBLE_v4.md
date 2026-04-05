# Top End War — DESIGN BIBLE v4.0
**Repo:** https://github.com/LpeC0/Top-End-War  
**Motor:** Unity 6.3 LTS (URP, 3D) | **Platform:** Android / iOS  
**Güncelleme:** Mart 2026

---

## DEĞİŞİKLİK LOGU

| Versiyon | Tarih | Değişiklik |
|----------|-------|-----------|
| v1 | Oca 2026 | İlk tasarım — runner + kapı + boss temel |
| v2 | Şub 2026 | Denge ayarları, yeni kapılar, boss HP kalibrasyonu |
| v3 | Mar 2026 | Komutan+Asker sistemi, BiomeManager, CommanderHP |
| v3.1 | Mar 2026 | Path sistemi vizyon notu, kapı görsel şeması, SoldierUnit tag kaldırıldı, HP Slider düzeltildi |
| **v4.0** | **Mar 2026** | **Equipment v2 (6 slot, silah tipleri), Olay Kapısı sistemi, Risk Kapısı yeniden tasarımı, Asker hasar mekanizması kararı, Mob zorluk notları, Floating damage planı** |

---

## VİZYON

**"Koş, kapılardan geç, ordunu büyüt, doğru biyomu seç, boss'u ez."**

---

## CORE LOOP

```
Koş (auto Z)
  → Kapıdan geç (sol/sağ — veya olay kapısı)
    → Normal: 2 kapı, seç birini
    → DUEL: 1 iyi 1 kötü, hangisi hangisi belli değil (%50 swap)
    → TEKLİ: 1 büyük ödüllü kapı, altın sarısı
    → ÜÇLÜ: 3 farklı kapı aynı anda, ortadaki önde
  → Düşman dalgası
    → Askerler + Komutan ateş eder
    → Askerler hasar ALMAZ (tasarım kararı — v4 notu)
    → Komutan HP → 0: GAME OVER
  → 1200m → Boss (anchor modu)
  → Zafer → Türkiye haritasında yeni şehir
```

---

## 1. CP SİSTEMİ

**CP = Savaş Gücü + Tier kilidi. CAN DEĞİL.**

| Tier | CP | Komutan Adı | Atış/s | Hasar | Aura |
|------|----|-------------|--------|-------|------|
| 1 | 0 | Gönüllü Er | 1.5 | 60 | +0% |
| 2 | 500 | Elit Komando | 2.5 | 95 | +10% |
| 3 | 1500 | Gatling Timi | 4.0 | 145 | +20% |
| 4 | 4000 | Hava İndirme | 6.0 | 210 | +30% |
| 5 | 9000 | Sürü Drone | 8.5 | 300 | +40% |

---

## 2. KOMUTAN HP

| Tier | Baz HP |
|------|--------|
| 1 | 500 |
| 2 | 700 |
| 3 | 950 |
| 4 | 1200 |
| 5 | 1500 |

+ Zırh/Omuzluk/Dizlik ekipmanlarından `commanderHPBonus` eklenir.  
Komutan HP = 0 → GAME OVER.  
**HP Bar:** GameHUD'daki `commanderHPSlider` — Inspector'da referans bağlanmalı.

---

## 3. EKİPMAN SİSTEMİ v2

### Slotlar
| Slot | Etki |
|------|------|
| Silah | Atış hızı × + Hasar × |
| Zırh | HP + + DR |
| Omuzluk | CP + + küçük DR |
| Dizlik | HP + |
| Kolye | CP çarpanı |
| Yüzük | Genel buff |

### Silah Tipleri (Gerçekçi Değerler)
| Tür | Atış/s × | Hasar × | Hissi |
|-----|---------|---------|-------|
| Tabanca | ×1.5 | ×0.7 | Hızlı, kısa |
| Tüfek | ×1.0 | ×1.0 | Standart |
| Otomatik | ×2.2 | ×0.6 | Yüksek DPS |
| Keskin N. | ×0.35 | ×3.5 | Tek atış dev hasar |
| Pompalı | ×0.5 | ×2.0 | Yakın mesafe |

### Zırh Tipleri
| Tür | HP Bonus | DR |
|-----|---------|-----|
| Hafif | +%20 HP | +%5 |
| Orta | +%40 HP | +%12 |
| Ağır | +%70 HP | +%22 |
| Kalkan | +%30 HP | +%30 |

**Max Hasar Azaltma:** Tüm ekipman toplamı max %60 ile sınırlı.  
**Asset Oluşturma:** Assets → Create → TopEndWar → Equipment → slot seç → değerleri doldur → Player Inspector'a sürükle.

---

## 4. ASKER SİSTEMİ

Max 20 asker. V formasyonu, oyuncunun arkasından.

| Path | Silah | ATK | Atış/s | HP |
|------|-------|-----|--------|----|
| Piyade | Tüfek | 15 | 1.5 | 80 |
| Mekanik | Minigun | 8 | 4.0 | 120 |
| Teknoloji | Plazma | 30 | 0.8 | 50 |

### Merge
```
3× Lv1 (aynı path) → 1× Lv2  (×1.8)
3× Lv2 → 1× Lv3  (×3.5)
3× Lv3 → 1× Lv4  (×7.0, max)
```

### v4 Tasarım Kararı: Askerler Hasar Alır mı?

**Karar: ALMAZLAR** (en azından v4'te)

**Gerekçe:**
- Oyun oynanış hızı yüksek — oyuncu 20 askerin HP'sini takip edemez
- Asker kayıpları anlık ve dramatik olmalı: "HP 0 → düşer" değil "düşman temas eder → asker anında kaybolur"
- Alternatif mekanik: Düşman asker kolonu içine girerse o asker düşer (HP sistemi değil, **pozisyon sistemi**)
- HealSoldiers kapısı şimdilik %CP recovery olarak davransın — gerçek HP sistemi v5'e

---

## 5. KAPI SİSTEMİ v2

### Normal Kapılar
Her 40 birimde bir çift kapı.

### Olay Kapıları (her 5 normal kapıdan 1)
| Tip | Açıklama |
|-----|----------|
| TEKLİ | 1 büyük altın kapı, ölçek ×1.6 |
| DUEL | 2 kapı — iyi/kötü, %50 yer değiştirir |
| ÜÇLÜ | 3 kapı, ortadaki önde, yanlar geride +4z, ölçek ×0.75 |

İlerlemeye göre ağırlık:
- 0-30%: Tekli ağırlıklı (tanıtım)
- 30-70%: Duel ağırlıklı (gerilim)
- 70-100%: Üçlü ağırlıklı (kaos)

### Risk Kapısı Yeniden Tasarımı (v4 planı)

**Mevcut sorun:** %50 bonus sadece CP kapılarını etkiliyor. Diğer tipler için anlamsız.

**Planlanan mekanik:**
```
Risk kapısından geçince:
  CP kapıları    → ×1.5 (mevcut)
  AddSoldier     → +1 ekstra asker (3 yerine 2+1=3)
  Merge          → CP çarpanı +0.2 artar
  HealCommander  → %100 HP yerine MaxHP+100 kalıcı bonus
  HealSoldiers   → Sonraki düşman dalgasını %50 azaltır
  NegativeCP     → Etkisiz (risk zaten alındı)

Risk bonusu 3 kapı için geçerli (mevcut)
```
Bu değişiklik `PlayerStats.ApplyGateEffect()` içinde uygulanacak.

### Kapı Görsel Şeması
| Renk | Kategori |
|------|----------|
| Yeşil | CP artışı |
| Parlak yeşil | Piyade asker |
| Gri | Mekanik asker |
| Mavi | Teknoloji asker |
| Mor | Merge |
| Pembe | Komutan Heal |
| Açık yeşil | Asker Heal |
| Turuncu | PathBoost |
| Altın | Risk/Tekli Olay |
| Kırmızı | Negatif |

---

## 6. HASAR GÖRSELLEŞTİRME (v4 planı)

**DamagePopup sistemi:**
- Her isabet: düşman üzerinde hasarı gösteren WorldSpace text
- Renk: vuranın rengi (Komutan = mor, Piyade = yeşil, Mekanik = gri, Teknoloji = mavi)
- Hızlı ateşlerde üst üste gelmemesi: random ±X offset + yukarı kayma animasyonu
- Pool: ObjectPooler'a "DamagePopup" tag'i eklenecek (boyut: 30)
- `DamagePopup.cs` → `Enemy.TakeDamage(dmg, color)` imzası

---

## 7. HASAR FORMÜLÜ

```
Komutan Hasarı = BaseDMG × TierDmgMult × WeaponDamageMult
Asker Hasarı = BaseATK × MergeMultiplier × (1 + CommanderAura) × BiomeMultiplier
Gelen Hasar = EnemyDMG × (1 - TotalDamageReduction)
TotalDR = Zırh.DR + Omuzluk.DR + Dizlik.DR + Yüzük.DR + Pet.DR (max %60)
```

---

## 8. BİYOM × PATH MATRİSİ

| Biyom | Piyade | Mekanik | Teknoloji |
|-------|--------|---------|-----------|
| Taş (Sivas) | ×0.90 | ×1.10 | **×1.25** |
| Orman (Tokat) | **×1.20** | ×1.00 | ×0.85 |
| Çöl (Kayseri) | ×1.10 | **×1.20** | ×1.00 |
| Karlı (Erzurum) | ×1.15 | ×0.85 | **×1.15** |
| Tarım (Malatya) | **×1.25** | ×1.10 | ×0.80 |

---

## 9. BOSS — GÖKMEDRESE MUHAFIZI

**HP: 41.000 | Biyom: Taş | Zayıflık: Teknoloji ×1.25**

| Faz | HP | Mekanik |
|-----|-----|---------|
| 1 — Taş Zırh | %100-60 | Normal |
| 2 — Minyon | %60-30 | Her 8s 4 golem |
| 3 — Çekirdek | %30-0 | Hız ×2.2 |

---

## 10. MOB ZORLUK DENGESİ (v4 notu)

**Mevcut sorun:** Oyuncu çok erken kaçmak zorunda kalıyor.

**Kök sebep analizi:**
```
DifficultyManager: multiplier = 1 + (z/1000)^1.3
z=200m → multiplier ≈ 1.08 → baseHP*1.08=108 → normal
z=400m → multiplier ≈ 1.22 → baseHP=122 → hâlâ normal
z=600m → multiplier ≈ 1.39 → OK
z=800m → multiplier ≈ 1.59 → biraz sert
z=1000m → multiplier ≈ 2.0 → çok hızlı arttı

SORUN: Oyuncu CP birikmeden multiplier artıyor.
DDA (SmoothedPowerRatio) var ama Lerp 0.08 çok yavaş adapt oluyor.
```

**Planlanan düzeltme:**
- `playerCPScalingFactor: 0.9 → 0.7` (düşmanlar oyuncu gücüne daha az hassas)
- Veya `difficultyExponent: 1.3 → 1.1` (daha yavaş artış)

---

## 11. YAPILACAKLAR

| # | Görev | Durum |
|---|-------|-------|
| 1-30 | Runner + Boss + Equipment altyapısı | ✅ |
| 31 | Risk kapısı tüm gate tiplerine etki | 📋 PLAN HAZIR |
| 32 | DamagePopup (floating damage text) | 📋 PLAN HAZIR |
| 33 | Oyuncu HP bar — Inspector referans bağla | ⚠️ HEMEN |
| 34 | Mob zorluk dengesi (exponent düşür) | 📋 PLAN HAZIR |
| 35 | Equipment UI menü (in-game overlay) | 🔲 SIRADA |
| 36 | Asker pozisyon-hasar sistemi (HP değil) | 🔲 v5 |
| 37 | 3D model entegrasyonu | 🔲 ÖĞRENME |
| 38 | Ana menü + Chest/Summon | 🔲 İLERİDE |
| 39 | Türkiye harita sahnesi | 🔲 İLERİDE |
| 40 | Ses efektleri | 🔲 İLERİDE |
| 41 | Android build | 🔲 İLERİDE |

---

## 12. TEKNİK KISITLAR (Değişmez)

```
xLimit = 8
Player Rigidbody: YOK
Cinemachine: YOK
Input: Old/Legacy
Namespace: YOK
GameEvents: Action<> — Raise...() YOK
PlayerStats.CP: Property (_baseCP + equipment bonusları)
Gate shader: Sprites/Default
Bullet: OverlapSphere (OnTriggerEnter değil)
Enemy tag: "Enemy" (zorunlu, büyük E)
"Soldier" tag: YOK (Unity varsayılanı yok)
Asker Max: 20
DOTween: kurulu
```

---

## 13. PATH SİSTEMİ VİZYONU (v5 — henüz uygulanmadı)

```
Mevcut: PathBoost kapıları asker ekler
Hedef v5:
  PathBoost → sadece % skoru artırır (asker eklemez)
  AddSoldier → ayrı kapı tipi
  Merge → dominant path'e göre komutan MORPH yapar
    Teknoloji dominant → "Drone Takımı"
    Piyade dominant → "Exosuit Takımı"
    Perfect Genetics → üçlü morph
  Asker gelişimi → ana menü Chest/Summon (monetizasyon)
```
