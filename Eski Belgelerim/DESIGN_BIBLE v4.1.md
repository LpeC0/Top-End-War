# Top End War — DESIGN BIBLE v3.1
**Repo:** https://github.com/LpeC0/Top-End-War
**Motor:** Unity 6.3 LTS (URP, 3D) | **Platform:** Android / iOS
**Güncelleme:** Mart 2026

---

## DEĞİŞİKLİK LOGU

| Versiyon | Değişiklik |
|---------|-----------|
| v3.1 | Path sistemi vizyon notu eklendi (ileride yapılacak) |
| v3.1 | Kapı görsel şeması netleştirildi |
| v3.1 | SoldierUnit: "Soldier" tag kaldırıldı (Unity'de varsayılan yok) |
| v3.1 | CommanderHP slider fill rect düzeltildi |
| v3   | Tam Komutan+Asker sistemi tasarlandı |
| v2   | Denge ayarları, yeni kapılar, boss HP |

---

## ⚠️ İLERİSİ İÇİN VIZYON NOTU (v4'te yapılacak)

> Oyuncunun önerisi — şu an aktif değil, ileride implemente edilecek.

### Path Sistemi (Gelecek Versiyon)
Şu anki sistem: Piyade/Mekanik/Teknoloji kapıları **asker ekler**.
Önerilen gelecek sistem:

```
Piyade/Mekanik/Teknoloji kapıları  →  o path'e +%25 ekler (asker eklemez)
Asker sayısı                        →  ayrı "Asker Ekle" kapıları ile belirlenir
Merge kapısı tetiklenince           →  dominant path'e göre komutan + askerler MORPH yapar
  Örn: Teknoloji dominant  → "Drone Takımı" görünümü
  Örn: Piyade dominant     → "Exosuit Takımı" görünümü
  Örn: Perfect Genetics    → özel üçlü morph
Asker gelişimi              →  ana menü Chest/Summon sistemine bağlı (monetizasyon)
```

Bu değişiklik için yapılması gerekenler:
- `GateEffectType.PathBoost_*` → asker eklemekten çıkar, sadece % ekler
- `GateEffectType.AddSoldier_*` → ayrı kapı tipi olarak kalır
- `MorphController` → path durumuna göre model set'i seçer
- Ana menü Chest/Summon UI → `ArmyManager`'a asker inject eder

**Şu an mevcut sistem** (AddSoldier kapıları) basitçe bu sisteme dönüştürülebilir.
Altyapı hazır.

---

## VİZYON

**"Koş, kapılardan geç, ordunu büyüt, doğru biyomu seç, boss'u ez."**

---

## CORE LOOP

```
Koş (auto Z)
  → Kapıdan geç (sol/sağ karar)
    → CP artışı VEYA asker ekleme VEYA heal
    → Merge → aynı tip 3 asker birleşir → Lv+1
    → Komutan tier atlarsa MORPH
  → Düşman dalgası (askerler + komutan otomatik ateş)
    → Asker HP → 0: asker düşer
    → Komutan HP → 0: GAME OVER
  → 1200m → Boss karşıdan gelir (anchor modu)
    → Doğru biyom path'i seçiliyse bonus hasar
    → Zafer → Türkiye haritasında yeni şehir
```

---

## 1. CP SİSTEMİ

**CP = Savaş Gücü + Tier kilidi. CAN DEĞİL.**
Can sistemi = Komutan HP (ayrı).

| Tier | CP | Komutan Rengi | Aura |
|------|----|---------------|------|
| 1 | 0 | Gri | +0% |
| 2 | 500 | Mavi | +10% asker hasar |
| 3 | 1500 | Turuncu | +20% asker hasar |
| 4 | 4000 | Mor | +30% asker hasar |
| 5 | 9000 | Altın | +40% asker hasar |

---

## 2. KOMUTAN HP

| Tier | Max HP |
|------|--------|
| 1 | 500 |
| 2 | 700 |
| 3 | 950 |
| 4 | 1200 |
| 5 | 1500 |

Komutan HP = 0 → GAME OVER. Düşman temasında HP düşer (CP değil).

---

## 3. ASKER SİSTEMİ

Max 20 asker. Küçük kapsül, V formasyonu.

| Path | Silah | ATK | Ateş/sn | HP |
|------|-------|-----|---------|-----|
| Piyade | Tüfek | 15 | 1.5 | 80 |
| Mekanik | Minigun | 8 | 4.0 | 120 |
| Teknoloji | Plazma | 30 | 0.8 | 50 |

### Merge
```
3× Lv1 (aynı path + biyom) → 1× Lv2  (stat ×1.8)
3× Lv2 → 1× Lv3  (stat ×3.5)
3× Lv3 → 1× Lv4  (stat ×7.0, max)
```

---

## 4. HASAR FORMÜLÜ

```
Damage = BaseATK × MergeMultiplier × (1 + CommanderAura) × BiomeMultiplier
```

---

## 5. BİYOM × PATH MATRİSİ

| Biyom | Piyade | Mekanik | Teknoloji |
|-------|--------|---------|-----------|
| Taş (Sivas) | ×0.90 | ×1.10 | **×1.25** |
| Orman (Tokat) | **×1.20** | ×1.00 | ×0.85 |
| Çöl (Kayseri) | ×1.10 | **×1.20** | ×1.00 |
| Karlı (Erzurum) | ×1.15 | ×0.85 | **×1.15** |
| Tarım (Malatya) | **×1.25** | ×1.10 | ×0.80 |

---

## 6. KAPI GÖRSEL ŞEMASI

| Renk | Kategori | Örnekler |
|------|----------|----------|
| 🟢 Yeşil | CP artışı | "CP +80", "CP +45" |
| 🟩 Parlak yeşil | Piyade asker | "PIY x2" |
| ⬜ Gri | Mekanik asker | "MEK x2" |
| 🔵 Mavi | Teknoloji asker | "TEK x2" |
| 🟣 Mor | Merge | "MERGE" |
| 🩷 Pembe | Komutan Heal | "KMT +HP" |
| 💚 Açık yeşil | Asker Heal | "ASK HP+" |
| 🟠 Turuncu | PathBoost | "PIY +25%", "MEK +25%" |
| 🟡 Sarı | Risk | "RISK !" |
| 🟡 Sarı-turuncu | Çarpan | "CP x1.2" |
| 🔴 Kırmızı | Negatif | "CP -60" |

---

## 7. BOSS — GÖKMEDRESE MUHAFIZI

**HP: 41.000 | Biyom: Taş | Zayıflık: Teknoloji ×1.25**

| Faz | HP | Mekanik |
|-----|-----|---------|
| 1 — Taş Zırh | %100-60 | Piyade -10% hasar |
| 2 — Minyon | %60-30 | 3 golem her 8sn |
| 3 — Çekirdek | %30-0 | Hız ×2 |

---
## 8.MATEMATİKSEL DENGE
  1. Güç Artış Denklemi (The Power Progression)
  
  Oyuncunun her zaman geliştiğini hissetmesi için doğrusal (1, 2, 3...) değil, Üstel (Exponential) bir maliyet ama Logaritmik bir güç artışı kullanıyoruz.CP Gereksinimi (Tier Atlam): $CP_{n+1} = CP_n \times 1.5 + (Level \times 100)$Neden? Her Tier bir öncekinden %50 daha zor ulaşılır olur, bu da oyuncuyu "daha iyi bir silah" veya "daha yüksek seviyeli bir pet" aramaya iter.Hasar Azaltma (Anchor Mode DR): $DR = 1 - e^{-\lambda t}$Anlamı: Sabit durduğun ilk saniyelerde savunman hızla artar, ancak 5. saniyeden sonra artış yavaşlar (Diminishing Returns). Oyuncu "sonsuza kadar ölümsüz" olamaz ama "doğru zamanda durmanın" ödülünü alır.
  
  2. Sürdürülebilirlik Döngüsü (Retention Loop)
  
  Oyuncunun yeni şeyler istemesini sağlayan 3'lü sac ayağı:Görsel Ödül (Morphing): Sadece sayı artmaz. Her Tier'da karakterin zırhı, mermisi ve yanındaki petin boyutu değişir. (İnsani dürtü: "Bir sonraki model nasıl görünüyor?")Stratejik Derinlik (Synergy): NotebookLM'in de önerdiği gibi; Piyade + Teknoloji yolu seçilirse "Drone Takımı" aktif olur. (İnsani dürtü: "Tüm kombinasyonları denemeliyim.")Zorluk Adaptasyonu (DDA - Dynamic Difficulty Adjustment): Senin kodundaki SmoothedPowerRatio bunu zaten yapıyor. Oyuncu çok güçlüyse düşman HP'si gizlice %10 artar. (İnsani dürtü: "Hala zorlanıyorum, daha fazla gelişmeliyim.")

---

## 8. YAPILACAKLAR

| # | Görev | Durum |
|---|-------|-------|
| 1-25 | Runner + Boss temel | ✅ |
| 26 | ArmyManager + SoldierUnit | ✅ |
| 27 | BiomeManager | ✅ |
| 28 | CommanderHP sistemi | ✅ |
| 29 | Kapı isimleri netleşti | ✅ |
| 30 | HP Slider fix | ✅ |
| 31 | **BossManager HP=41k + biyom zayıflığı** | 🔲 SIRADA |
| 32 | **Path sistemi v4 (morph + chest)** | 🔲 İLERİDE |
| 33 | Ana menü + Chest/Summon UI | 🔲 İLERİDE |
| 34 | Türkiye haritası | 🔲 İLERİDE |
| 35 | 3D model pipeline | 🔲 İLERİDE |
| 36 | Ses efektleri | 🔲 İLERİDE |
| 37 | Android build | 🔲 İLERİDE |

---

## 9. TEKNİK KISITLAR (Değişmez)

```
xLimit = 8
Player Rigidbody YOK
Cinemachine YOK
Input: Old/Legacy
Namespace YOK
GameEvents: Action<>  — Raise...() YOK
PlayerStats.CP        — Property (field değil)
Bullet: OverlapSphere — OnTriggerEnter değil
Enemy tag: "Enemy"    — Zorunlu, büyük E
Asker Max 20          — Mobil sınır
"Soldier" tag YOK     — Unity'de varsayılan değil, ekleme
DOTween kurulu
```
