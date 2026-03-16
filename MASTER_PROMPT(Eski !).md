# Top End War — Master Prompt v3

## (Bu prompt'u herhangi bir yapay zekaya yapıştırarak projeyi sıfırdan anlatabilmek için yaptık.)

---

## PROJE ÖZETİ

"Top End War" adlı bir mobil runner/auto-shooter oyunu geliştiriyorum.
Unity 6.3 LTS (URP, 3D) kullanıyorum. Hedef platform: Android/iOS.
Oyun tasarımı tamamen benim, teknik uygulama için yardım istiyorum.

---

## CORE LOOP (ANA DÖNGÜ)

1. **Koş:** Player Z ekseninde otomatik ileri koşar. Oyuncu sağa/sola swipe/tap ile 3 şeritten birini seçer.
2. **Kapıdan Geç:** Yolda matematiksel kapılar çıkar (+60, ×2, Merge vb.). Oyuncu şerit seçerek hangi kapıdan geçeceğini belirler.
3. **CP Artar:** Kapıdan geçince Combat Power (CP) değişir. CP yükseldikçe ordu morph eder.
4. **Otomatik Savaş:** Player önündeki düşmanlara otomatik mermi atar.
5. **Boss Savaşı:** Koşunun sonunda Boss çıkar. "All-In" veya "Split" Overload kapısı.
6. **Meta Döngü:** Boss loot'u kalıcı gelişim sağlar. Türkiye haritasında yeni bölgeler açılır.

---

## TEKNİK DURUM (Mevcut Unity Sahnesi)

**Hierarchy:**
SampleScene
├── Directional Light
├── Terrain
├── PoolManager         (ObjectPooler.cs eklendi, mermiler için)
├── Player              (PlayerController + PlayerStats, Tag: "Player")
│     └── FirePoint     (boş child, ateş noktası)
├── GateSpawner         (GateSpawner.cs)
└── Canvas
├── HUD\_Panel     (GameHUD.cs)
└── ...



---

## 🛠 GELİŞİM GEÇMİŞİ (VERSION HISTORY)

\[Mart 2026] — KULLANICI \& AI BEYİN FIRTINASI:

* Mimari fikirler tartışıldı.
* Gemini: ScriptableObject + Observer Pattern + GameEvents mimari önerisi
* Gemini: CameraFollow X sabit runner kamerası konsepti
* Gemini: GateData, PlayerStats, Gate, GameEvents ilk versiyonları

\[Mart 2026] — CLAUDE KATILIMI (v1):

* PlayerController.cs temizden yazıldı (Claude)
* SimpleCameraFollow.cs — X sabit, LateUpdate (Claude)
* Enemy.cs + Bullet.cs temizlendi (Claude)
* MASTER\_PROMPT.md v1 oluşturuldu (Claude)
* WorldMap\_Plan.md sahne yapısı planı (Claude)
* PlayerStats.cs — Gemini tabanı + singleton + event sistemi güçlendirildi (Claude)
* GateData.cs — Gemini + Claude birleşik versiyon (Claude)
* Gate.cs — Gemini + Claude birleşik versiyon (Claude)
* GateSpawner.cs — procedural spawn, otomatik temizleme (Claude)
* GameHUD.cs — Observer pattern UI, CP/Tier/Path/Popup (Claude)
* MASTER\_PROMPT.md v2: yapılacaklar + AI tartışma bölümü (Claude)

\[Mart 2026] — CLAUDE KATILIMI (v2):

* Gemini scriptleri ile çakışma tespit edildi ve temizlendi (Claude)
* GateData.cs, Gate.cs, GateSpawner.cs, PlayerStats.cs, GameEvents.cs tek ve temiz versiyona indirildi (Claude)
* PlayerController.cs'e Swipe input eklendi (40px eşik + tap fallback) (Claude)

[Mart 2026] — GEMINI GÜNCELLEMESİ (v4):
- **ObjectPooler.cs Entegrasyonu:** Mermiler için bellek dostu Queue mimarisi kuruldu. (Gemini)
- **Hız & Pacing Ayarı:** Kapı aralarına düşman yerleşimi için `forwardSpeed` 14f'ten 10f'e düşürüldü, `spacingBetweenGates` 50f olarak güncellendi. (Gemini)
- **Kritik Hata Onarımı:** Player'ın 44.8 birimde silinmesine yol açan "Auto-Destroy" mantığı Player objesinden temizlendi. (Gemini)
- **Sonsuz Yol Kararı:** "Chunk-Based" (yol parçası taşıma) sistemi teknik standart olarak belirlendi. (Gemini)
[Mart 2026] — GEMINI GÜNCELLEMESİ (v5):
- **Kritik Bug Çözümü:** Player objesine yanlışlıkla eklenen `Bullet.cs` ve `Enemy.cs` scriptleri tespit edilip temizlendi. Karakterin kapılara çarpıp kendini "mermi" sanarak imha etmesi sorunu (Z=44.8 bug'ı) kökten çözüldü.
- **Sıradaki Aşama:** Terrain sisteminden vazgeçilip, "ChunkManager" ile sonsuz yol (Procedural Generation) mekaniğini oluşturduk.
---

💡 YAPAY ZEKA FİKİR ALIŞVERIŞI ALANI
🟡 TARTIŞMA #1 — Object Pooling Zamanlaması
Soru: Mermi Destroy() şu an sorun değil ama boss sahnesinde 50+ mermi aynı anda girince GC spike yapabilir. Pooling'i şimdi mi kuralım, boss bittikten sonra mı?

Claude: Boss öncesi ekle, sonradan refactor daha maliyetli. Ama prototip aşamasında Destroy() kabul edilebilir.
Gemini: Kesinlikle Boss öncesi kurulmalı. Prototip aşamasını geçtik, mobil optimizasyon şart. Mermiler için ObjectPooler (Queue sistemi) kuruldu ve entegre edildi.
Grok: Katılıyorum, ama chunk sistemiyle birleştir: Chunk silinince içindeki objeleri pool'a dön. Erken ekle, FPS düşüşü önle.

🟡 TARTIŞMA #2 — Sonsuz Yol Tekniği
Soru: İki yöntem: A) Chunk sistemi (yolu parça spawn et) B) Reverse movement (Player Z sabit, zemin -Z'ye hareket eder).

Claude: B daha az fizik hatası, daha az karmaşık. Öneri: B ile başla.
Gemini (İtiraz): Projede ileriye mermi atıyoruz. Zemin geriye akarsa mermi fiziklerini relative velocity ile hesaplamak gerekir ve BoxCast'ler bozulur. Klasik Runner mantığı olan A (Chunk Sistemi) uygulanmalı. Zemin parçaları arkadan alınıp öne taşınmalı.
Grok: A (Chunk) ile git, ama object pooling ile zeminleri silme, öne taşı (recycle). Fizik yok, transform ile %100 stabil.

🟡 TARTIŞMA #3 — Morph Tekniği
Soru: Tier atlarken A) Prefab Swap, B) Blend Shapes, C) Shader Dissolve

Claude: A ile başla → C'ye geç. En iyi game feel, custom model gerektirmez.
Gemini: Claude ile %100 mutabıkım. A (Prefab Swap) MVP için en hızlısıdır. Araya particle effect koyup göz yanılsamasıyla geçiş yapacağız.
Grok: A ile başla, ama DOTween ile scale/position tween ekle (ücretsiz package). B/C ileride, model import için.

🟡 TARTIŞMA #4 — Dünya Haritası Tipi
Soru: A) 2D flat PNG harita B) 3D dönen globe

Claude: A ile başla, runner bittikten sonra B ekle.
Gemini: A (2D Flat Map) ile başlamak UI akışını en hızlı test etme yöntemidir, destekliyorum.
Grok: A ile başla, Canvas UI ile button'lar ekle. B için Mapbox SDK (ücretsiz) kullan, ama MVP için overkill.

🟡 TARTIŞMA #5 — UI Optimizasyon (Yeni)
Soru: GameHUD'da TMPro vs. Unity UI, performance için ne yapalım?

Claude: (Sorulmadı)
Gemini: (Sorulmadı)
Grok: TMPro kal, ama Update'de değil event'lerle güncelle (Observer Pattern). UI profiler kullan, FPS düşüşü yakala.---

### 🟡 TARTIŞMA #6 — DDA (Dinamik Zorluk) ve İlerleme Eğrisi
**Soru:** Yeni kurulan ProgressionConfig ve DifficultyManager sisteminde oyuncunun "güçlü hissetmesini" sağlarken oyunu nasıl kırılmaz (unbreakable) yaparız?
- **Claude / ChatGPT:** PlayerPowerRatio (gerçek CP / beklenen CP) 1 frame yerine son 30 saniyenin ortalaması alınarak yumuşatılmalı (Lerp). Negatif kapılar için A/B testi yapılmalı (%3 vs %5).
- **Gemini:** Matematiksel polinomsal eğri `(Distance/1000)^1.3` harika bir seçim. Ancak MorphController içindeki `Instantiate` kullanımı bu harika DDA sistemini anlık FPS droplarıyla baltalayabilir. Morph modelleri acilen Object Pool'a dahil edilmeli. Ayrıca Boss öncesi "Negatif Kapı Çıkmama" (Pity Timer) kuralı eklenmeli.

### 🟡 TARTIŞMA #7 — Hibrit Oyun Modları (Runner + Hold/Defense)
**Soru:** "Anchor" alarak belirli noktalarda koşuyu durdurup kule savunmasına (auto-shooter) geçiş fikri nasıl uygulanmalı?
- **Gemini:** "Count Masters" vizyonuna tam oturuyor. Zemin hareket etmediği için (Chunk sistemi) oyuncu bir "Anchor Kapısı"ndan geçtiğinde `PlayerController`'daki `forwardSpeed = 0` yapılır. Kamera hafif yukarı/geriye çekilir (FOV genişler). Z ekseninden dalga dalga düşman gelir. Süre bitince `forwardSpeed = 10` yapılarak koşuya devam edilir. Yapması çok kolay, oyuncu etkisi çok yüksek.

## YAPILACAKLAR SIRASI (Öncelik Sırası)

1. ✅ Player hareket (ileri koşu + 3 şerit + Lerp)
2. ✅ Kamera takip (SimpleCameraFollow, X sabit)
3. ✅ Matematiksel Kapılar (yola spawn, içinden geçince CP değişimi + UI feedback)
4. ✅ GameHUD (Observer Pattern ile CP, Tier, Sinerji gösterimi)
5. ✅ Object Pooling (Mermiler için Optimize edildi)
6. ✅ **Chunk Sistemi** (sonsuz yol, Terrain yerine parça zemin spawn/taşıma)
7. ✅ **Prefab Swap Morph** (tier atlarken model değişimi + particle)
8. 🔲 **Boss Savaşı** (Gökmedrese Muhafızı, faz sistemi, Overload kapısı)
9. 🔲 **Meta UI** (Dünya haritası, il seçimi, kalıcı upgrade ekranı)
