# Top End War — DESIGN BIBLE v3
**Repo:** https://github.com/LpeC0/Top-End-War
**Motor:** Unity 6.3 LTS (URP, 3D) | **Platform:** Android / iOS
**Güncelleme:** Mart 2026

> Kod değişikliklerinde ÖNCE buraya bakılmalı.
> Herhangi bir AI'a verildiğinde projeyi sıfırdan anlatır.

---

## DEĞİŞİKLİK LOGU (v2 → v3)

| Değişiklik | Sebep |
|-----------|-------|
| Tam Komutan+Asker sistemi tasarlandı | Vizyon netleşti |
| CP = güç göstergesi + tier kilidi | Can sistemi ayrıldı |
| HP sistemi: Komutan+Asker ayrı HP barları | Ölüm yeni mekanik |
| Biyom × Path hasar matrisi | Strateji derinliği |
| Asker Merge: 3×Lv1 → 1×Lv2 | Birleşme mantığı |
| Komutan tier: renk + stat farkı | Hem görsel hem mekanik |
| Chest/Summon monetizasyon planı | Gelir modeli |
| Boss HP: 180k → 41k (ordu DPS'e göre) | Kalibrasyon |
| Düşman HP: 100-3750 (ordu DPS'e göre) | Kalibrasyon |

---

## VİZYON

**"Koş, kapılardan geç, ordunu büyüt, doğru biyomu seç, boss'u ez."**

Oyuncu 3 katmanlı karar verir:
- **Hangi path** (Piyade/Mekanik/Teknoloji) asker toplarım?
- **Merge mi yoksa sayı mı** daha iyi?
- **Bu boss'a karşı** hangi kombinasyon avantajlı?

---

## CORE LOOP

```
Koş (auto Z)
  → Kapıdan geç → CP + Asker + path biriktir
    → Merge kapısı → 3 aynı-tip asker → 1 güçlü asker
      → Komutan tier atlarsa MORPH
  → Düşman dalgası
    → Askerler + Komutan otomatik ateş eder
    → Asker HP → 0: asker düşer (kayıp)
    → Komutan HP → 0: GAME OVER
  → 1200m → Anchor modu
    → Boss karşıdan gelir
    → Biyom doğru seçildiyse bonus hasar
    → Boss yenilir → Zafer → Haritada yeni şehir
```

---

## 1. CP SİSTEMİ

**CP = Savaş Gücü göstergesi + Tier kilidi.**
Can sistemi artık ayrı (Komutan HP + Asker HP ayrı barlar).

| Tier | CP Eşiği | Komutan Rengi | Aura Etkisi |
|------|----------|---------------|-------------|
| 1 | 0 | Gri | +0% |
| 2 | 500 | Mavi | +10% asker hasar |
| 3 | 1500 | Turuncu | +20% asker hasar |
| 4 | 4000 | Mor | +30% asker hasar |
| 5 | 9000 | Altın | +40% asker hasar |

Kapılardan ve düşman öldürmekten CP kazanılır. CP düşmez.

---

## 2. KOMUTAN SİSTEMİ

### Tier Stat Tablosu
| Tier | ATK | Ateş/sn | HP | Aura |
|------|-----|---------|-----|------|
| 1 | 25 | 1.5 | 500 | +0% |
| 2 | 40 | 2.0 | 700 | +10% |
| 3 | 60 | 2.5 | 950 | +20% |
| 4 | 90 | 3.0 | 1200 | +30% |
| 5 | 130 | 4.0 | 1500 | +40% |

### Görünüm Sistemi
- **Tier = büyüklük + renk yoğunluğu** (T1 küçük gri → T5 büyük altın)
- **Biyom = renk ailesi:**
  - Taş: Gri/Koyu tonlar
  - Orman: Yeşil tonlar
  - Çöl: Sarı/Kahverengi
  - Karlı: Beyaz/Açık mavi
- Aynı tier + farklı biyom = farklı renk, aynı stat

### Chest/Summon ile Açılabilir Komutanlar
```
Common Chest:  %70 T1 asker, %25 T2 asker, %5 T3 asker
Rare Chest:    %60 T2 asker/komutan, %35 T3, %5 T4
Epic Chest:    %50 T3, %40 T4, %10 T5

Özel Komutanlar (gelecek):
  "Kizil Elma" — tüm biyomda +15% bonus
  "Dede Korkut" — aura menzili 2x
```

---

## 3. ASKER SİSTEMİ

**Max 20 asker ekranda** (mobil performans).
Küçük kapsül/sprite, V formasyonunda.

### Path Bazlı Asker Tipleri
| Path | Silah | Base ATK | Ateş/sn | Base DPS | HP |
|------|-------|----------|---------|----------|----|
| Piyade | Tüfek | 15 | 1.5 | 22.5 | 80 |
| Mekanik | Minigun | 8 | 4.0 | 32.0 | 120 |
| Teknoloji | Plazma | 30 | 0.8 | 24.0 | 50 |

### Asker Seviyeleri
```
Lv1: base stat           (temel kapsül)
Lv2: base × 1.8         (biraz büyür)
Lv3: base × 3.5         (belirgin renk değişimi)
Lv4: base × 7.0         (elite, parlak)
```

### Formasyon
```
         [KOMUTAN]          ← Öne, ortada
     [A]  [A]  [A]  [A]    ← 1. sıra
  [A] [A] [A] [A] [A]      ← 2. sıra
     [A]  [A]  [A]  [A]    ← 3. sıra
         [A] [A]            ← 4. sıra

Her asker en yakın düşmana ateş eder (otomatik).
Asker düşünce formasyon kapanır.
```

---

## 4. HASAR FORMÜLÜ

```
Damage = (BaseATK + CommanderAura) × PathBonus × BiomeMultiplier
```

Burada:
- `CommanderAura` = komutan tier'ine göre asker base ATK'sına eklenen yüzde
- `PathBonus` = merge seviyesinden gelen ek çarpan (Lv1=1.0, Lv2=1.8...)
- `BiomeMultiplier` = aşağıdaki Biyom×Path tablosundan

### Ordu DPS Örneği
```
T3 Komutan + 10 Teknoloji Lv1 + Taş biyom:
  Asker DPS:   24 × 1.25 (biome) × 1.20 (aura) = 36 × 10 = 360
  Komutan DPS: 60 × 2.5 × 1.25 = 188
  TOPLAM:      548 DPS
  Boss 41k HP: ~75sn ✓
```

---

## 5. BİYOM × PATH MATRİSİ

Oyunun stratejik kalbi. Oyuncu biyomu öğrenirse path seçimini optimize eder.

| Biyom | Piyade | Mekanik | Teknoloji | Boss |
|-------|--------|---------|-----------|------|
| **Taş** (Sivas) | ×0.90 | ×1.10 | **×1.25** | Gökmedrese Muhafızı |
| **Orman** (Tokat) | **×1.20** | ×1.00 | ×0.85 | Orman Canavarı |
| **Çöl** (Kayseri) | ×1.10 | **×1.20** | ×1.00 | Kum Deviği |
| **Karlı** (Erzurum) | ×1.15 | ×0.85 | **×1.15** | Buz Muhafızı |
| **Tarım** (Malatya) | **×1.25** | ×1.10 | ×0.80 | Tarla Ruhu |

> Yanlış path seçilirse boss ~3-4× daha uzun sürer.
> UI: biyom ikonunu ve hangisinin avantajlı olduğunu kapı başında göster.

---

## 6. MERGE SİSTEMİ

### Anında Merge (Kapıdan Geçince)
```
Koşul: Aynı Path + Aynı Biyom + Aynı Seviye
  3× Lv1 → 1× Lv2  (2 asker kaybolur, 1 güçlü gelir)
  3× Lv2 → 1× Lv3
  3× Lv3 → 1× Lv4 (maksimum)
```

Merge kapısına girildiğinde:
1. Formasyonda 3 adet aynı-tip-aynı-level var mı? → MERGE
2. Yoksa → sadece küçük CP bonusu

### Merge Stratejisi
```
Erken merge: az asker, yüksek hasar — boss için iyi
Geç merge:   çok asker, az hasar — dalga düşmanlar için iyi
Oyuncu seçer!
```

---

## 7. KAPILARIN YENİ HÂLİ

| Tip | Ağırlık | Etki |
|-----|---------|------|
| AddCP +80 | 0.25 | CP artışı (scale ile) |
| AddCP +45 | 0.20 | CP artışı |
| AddSoldier (Piyade) | 0.08 | +1-2 Piyade Lv1 |
| AddSoldier (Mekanik) | 0.08 | +1-2 Mekanik Lv1 |
| AddSoldier (Teknoloji) | 0.06 | +1-2 Teknoloji Lv1 |
| Merge | 0.07 | Aynı-tip askerler birleşir |
| HealCommander | 0.04 | Komutan HP +300 |
| HealSoldiers | 0.04 | Tüm asker HP +50% |
| RiskReward | 0.04 | -30% CP, 3 kapı +50% |
| MultiplyCP | 0.04 | ×1.2 CP |
| NegativeCP | 0.05 | -CP |

**Kapı scale:** `1 + distance / 2400` (0m=1x, 1200m=1.5x)
**Pity Zone:** Son 200m → Sadece pozitif kapılar

---

## 8. HP SİSTEMİ

### Komutan HP
```
Tier 1: 500 | Tier 2: 700 | Tier 3: 950 | Tier 4: 1200 | Tier 5: 1500
```
Komutan HP = 0 → GAME OVER

### Asker HP
```
Piyade: 80  (ucuz, çok sayıda)
Mekanik: 120 (dayanıklı, yavaş)
Teknoloji: 50 (kırılgan, çok güçlü)
```
Asker ölünce formasyondan çıkar. Tüm askerler ölerse komutan açığa çıkar.

### Düşman HP Tablosu (yeni, ordu DPS'e göre)
| Tür | HP | Temas Hasarı |
|-----|-----|-------------|
| Normal | 1100 | 30 |
| Zırhlı | 2250 | 60 |
| Elite | 3750 | 100 |
| Boss (Taş) | 41000 | 150/sn |

---

## 9. BOSS — GÖKMEDRESE MUHAFIZI (v3)

**HP: 41.000 | Biyom: Taş | Zayıflık: Teknoloji ×1.25**

| Faz | HP | Mekanik |
|-----|-----|---------|
| 1 — Taş Zırh | %100-60 | Piyade -%10 hasar (taş direnci) |
| 2 — Minyon | %60-30 | Her 8sn 3 Taş Golem minyon |
| 3 — Çekirdek | %30-0 | Hız ×2, AoE hasar |

---

## 10. TÜRKIYE HARİTASI

```
Sivas (Taş)       ← ŞU AN
  → Tokat (Orman)     Piyade avantajlı
  → Kayseri (Çöl)     Mekanik avantajlı
  → Erzurum (Karlı)   Teknoloji+Piyade avantajlı
  → Malatya (Tarım)   Piyade avantajlı
```

Her şehir = aynı runner mekanik + farklı düşman görünümü + biyom matrisi.

---

## 11. MONETİZASYON

```
F2P:           T1-T2 komutan + Lv1-2 asker (oynanabilir, yavaş)
Pay-to-Speed:  Rare/Epic Chest → T3-T4 hızlıca
Whale:         T5 guaranteed summon, exclusive skin

ÖNEMLİ: PAY-TO-WIN değil, PAY-TO-SPEED.
F2P oyuncu sabırla aynı noktaya ulaşabilir.
```

---

## 12. YAPILACAKLAR

| # | Görev | Durum |
|---|-------|-------|
| 1-25 | Runner + Boss temel sistemi | ✅ TAMAM |
| 26 | **ArmyManager.cs** — 20 asker, formasyon | 🔲 SIRADA |
| 27 | **SoldierUnit.cs** — asker AI, ateş, HP | 🔲 SIRADA |
| 28 | **CommanderHP** — ayrı HP barı + GameOver | 🔲 SIRADA |
| 29 | **Kapı güncellemesi** — AddSoldier, Heal | 🔲 SIRADA |
| 30 | **BiomeManager.cs** — path × biyom çarpanı | 🔲 SIRADA |
| 31 | Türkiye haritası sahnesi | 🔲 BEKLIYOR |
| 32 | Chest/Summon UI | 🔲 BEKLIYOR |
| 33 | 3D model pipeline | 🔲 BEKLIYOR |
| 34 | Ses efektleri | 🔲 BEKLIYOR |
| 35 | Android build | 🔲 BEKLIYOR |

---

## 13. TEKNİK KISITLAR (Değişmez)

```
xLimit = 8              PlayerController + Enemy + SpawnManager AYNI
Player Rigidbody YOK    transform.position hareketi
Cinemachine YOK         SimpleCameraFollow (X sabit)
Input: Old/Legacy
Namespace YOK
GameEvents: Action<>    Raise...() metod YOK
PlayerStats.CP          Property (public field değil)
Bullet: OverlapSphere   OnTriggerEnter değil
Enemy tag: "Enemy"      Büyük E — zorunlu
Asker Max 20            Mobil sınır
DOTween kurulu
```

---

## 14. MASTER_PROMPT EKİ (kısa, senin dosyana ekle)

```
Sistem: Komutan (T1-T5, biyom rengi) + max 20 asker (kapsül, path bazlı).
Hasar: (baseAtk + komutanAura) × pathBonus × biomeMultiplier.
Merge: 3×Lv1 → 1×Lv2, aynı path+biyom, kapıdan geçince anında.
HP: Komutan HP ayrı, Asker HP ayrı. Komutan=0 → GameOver.
Biyom×Path matrisi: Taş=Teknoloji×1.25, Orman=Piyade×1.20, Cul=Mekanik×1.20.
Kapı: AddSoldier(Piyade/Mekanik/Teknoloji), HealCommander, HealSoldiers eklendi.
Boss HP: 41000 (ordu DPS kalibre). Dusman HP: Normal=1100, Zirh=2250, Elite=3750.
```
