\# Top End War — Enemy Cost Table v1 + Packet Library v1

\_Hidden tuning for strong game feel\_



\---



\## 0. Belgenin amacı



Bu belge iki temel şeyi tanımlar:



1\. \*\*Enemy Cost Table v1\*\*  

&#x20;  Her düşmanın görünmez tuning değerleri:

&#x20;  - ne kadar zor temizlenir

&#x20;  - ne kadar baskı kurar

&#x20;  - hangi solve türünü çağırır



2\. \*\*Packet Library v1\*\*  

&#x20;  Düşmanları tek tek değil, \*\*anlamlı paketler\*\* halinde üretmek için kullanılacak temel kütüphane



\### Temel ilke

Oyuncu bunları \*\*asla sayısal olarak görmez\*\*.  

Oyuncu sadece şunu hisseder:

\- dalgalar farklı geliyor

\- düşmanlar boş değil

\- bazı anlar rahat

\- bazı anlar baskılı

\- build seçimim fark yaratıyor



\---



\## 1. Enemy Cost sistemi



\## 1.1 İki ana metrik



\### Clear Cost

Bu düşmanı öldürmenin maliyeti.



Etkileyen şeyler:

\- effective HP

\- armor

\- vurulma zorluğu

\- solve gereksinimi

\- hedef önceliği



\### Pressure Cost

Bu düşmanı görmezden gelirsen ne kadar baskı kurar?



Etkileyen şeyler:

\- ne kadar hızlı yaklaşır

\- oyuncuyu ne kadar zor karar vermeye iter

\- temas riski

\- temas cezası

\- lane/alan baskısı

\- panic factor



\---



\## 1.2 Çok önemli not

Pressure Cost bir damage type değildir.  

Bu:

\- tasarım/tuning metriğidir

\- spawn generator’ın kullandığı görünmez puandır



Yani oyun içinde “pressure = 8” diye bir şey görünmez.



\---



\## 2. Enemy Cost Table v1



\## 2.1 Değer ölçeği

Başlangıç ölçeği:

\- `1–3` = düşük

\- `4–6` = orta

\- `7–8` = yüksek

\- `9+` = çok yüksek



Bu ilk versiyon için yeterli.



\---



\## 2.2 Ana tablo



| Enemy | Clear Cost | Pressure Cost | Solve Focus | Ana Hissi |

|---|---:|---:|---|---|

| Trooper | 2 | 2 | Mixed | baseline |

| Swarm | 1 | 1 | Swarm | sayı birikirse tehlike |

| Charger | 3 | 6 | Priority | erken çözülmezse panik |

| Armored Brute | 6 | 4 | Armor | yavaş ama inatçı |

| Elite Charger | 5 | 8 | Elite / Priority | hızlı çözülmeli |

| Gatekeeper Walker | 10 | 9 | Armor / Telegraph | ilk gerçek sınav |

| War Machine | 11 | 8 | Area / Long Fight | alan baskısı |

| World 1 Final Boss | 16 | 12 | Mixed / Boss Prep | final sentez |



\---



\## 2.3 Açıklamalar



\### Trooper

\- düşük clear

\- düşük pressure

\- her şeyin referansı



\### Swarm

\- tekil tehdit düşük

\- ama paket halinde \*\*çarpanlı pressure\*\* üretir



\### Charger

\- öldürmesi çok pahalı değil

\- ama hayatta kalırsa baskısı çok yüksek



\### Armored Brute

\- solve yoksa aşırı yavaş temizlenir

\- baskısı charger kadar ani değil



\### Elite Charger

\- hem öncelik hem panic

\- oyuncuya “şimdi karar ver” dedirtir



\---



\## 2.4 Swarm için özel kural



Swarm’ın pressure değeri tek başına düşük tutulur.  

Ama packet içinde şu bonus uygulanır:



\### Swarm Pack Bonus

\- 2–3 Swarm birlikte → `+1 packet pressure`

\- 4–5 Swarm birlikte → `+2 packet pressure`

\- 6+ Swarm birlikte → `+3 packet pressure`



\### Neden?

Tekil swarm zayıf hissetmeli.  

Ama topluca lane baskısı kurmalı.



\---



\## 2.5 Solve Tags



Her düşman şu solve tag’lerinden 1 veya daha fazlasını taşır:



\- `mixed`

\- `swarm`

\- `priority`

\- `armor`

\- `elite`

\- `long\_fight`

\- `boss\_prep`



\### Örnek

\- Trooper → `mixed`

\- Swarm → `swarm`

\- Charger → `priority`

\- Brute → `armor`

\- Elite Charger → `elite`, `priority`

\- War Machine → `long\_fight`

\- Final Boss → `boss\_prep`, `mixed`



\---



\## 3. Packet Library v1



Biz bölüm yazmıyoruz.  

Biz \*\*packet\*\* yazıyoruz.



\### Packet ne?

Bir packet:

\- kısa bir düşman grubu

\- tek bir karar

\- tek bir hissiyat

\- küçük bir tempo anı



\---



\## 3.1 Packet alanları



Her packet şu alanları taşır:



\- `packetId`

\- `packetType`

\- `enemyComposition`

\- `solveTags`

\- `packetClearCost`

\- `packetPressureCost`

\- `entryStyle`

\- `preferredStageBands`

\- `reliefWeight`

\- `spikeWeight`



\---



\## 4. Packet türleri



\## 4.1 Baseline Packet



\### İçerik

\- 2–4 Trooper



\### Amaç

\- baseline akış

\- oyuncuya nefes aldırmayan ama yormayan doluluk

\- silahın çalıştığını hissettirmek



\### Cost

\- Clear: 4–8

\- Pressure: 4–6



\### Kullanım

\- her bandda olabilir

\- özellikle giriş packet’i olarak iyi



\---



\## 4.2 Dense Swarm Packet



\### İçerik

\- 1 Trooper

\- 2–5 Swarm



\### Amaç

\- lane baskısı

\- tempo testi

\- SMG / Launcher / Shotgun alanı açmak



\### Cost

\- Clear: 4–7

\- Pressure: 5–8



\### Kullanım

\- 1–5’te küçük

\- 6–20’de orta

\- 20+ bandlarda mixed destek



\---



\## 4.3 Delayed Charger Packet



\### İçerik

\- önce 2–3 Trooper

\- kısa gecikmeyle 1 Charger



\### Amaç

\- öncelik kararı

\- “önce bunu çöz” anı yaratmak



\### Cost

\- Clear: 7–9

\- Pressure: 8–11



\### Kullanım

\- 1–5’te çok hafif

\- 6–15’te öğretim

\- sonra mixed pressure içinde



\---



\## 4.4 Armor Check Packet



\### İçerik

\- 1 Armored Brute

\- yanında 1–2 Trooper



\### Amaç

\- armor solve gereksinimi

\- sniper/breacher değerini hissettirmek



\### Cost

\- Clear: 8–11

\- Pressure: 6–8



\### Kullanım

\- 6–10 tanıtım

\- 11+ varyantlı kullanım



\---



\## 4.5 Guarded Core Packet



\### İçerik

\- merkezde 1 Brute veya 1 Elite

\- yanında 2–3 support düşman



\### Amaç

\- “önce neyi vuracağım?”

\- mixed solve

\- build ayrıştırma



\### Cost

\- Clear: 10–14

\- Pressure: 8–11



\### Kullanım

\- 11+ sonrası daha iyi

\- specialisation bandında güçlü



\---



\## 4.6 Split Pressure Packet



\### İçerik

\- sağ/sola dağılmış küçük gruplar

\- merkezde hafif core



\### Amaç

\- düz çizgi hissini kırmak

\- sadece orta çizgiye kilitli oyunu bozmak



\### Cost

\- Clear: 6–10

\- Pressure: 6–9



\### Kullanım

\- 11+ daha uygun

\- launcher/smg/assault farkını gösterir



\---



\## 4.7 Elite Spike Packet



\### İçerik

\- 1 Elite Charger

\- yanında 1–2 support düşman



\### Amaç

\- panic + hedef önceliği

\- sniper/beam/elite solve alanı



\### Cost

\- Clear: 9–12

\- Pressure: 10–13



\### Kullanım

\- 6–10 sonrası kontrollü

\- 20+ bandlarda daha güçlü



\---



\## 4.8 Relief Packet



\### İçerik

\- düşük tehditli kısa grup

\- genelde 1–2 Trooper veya çok hafif swarm



\### Amaç

\- oyuncuya nefes

\- build gücünü hissettirme

\- tempo düz çizgi olmasın



\### Cost

\- Clear: 2–4

\- Pressure: 2–3



\### Kullanım

\- her bandda var

\- ama çok sık değil



\---



\## 4.9 Boss Prep Packet



\### İçerik

\- armor + elite + long-fight hisli küçük kombinasyon



\### Amaç

\- boss öncesi son uyarı

\- beam/sniper/breacher değerini hissettirme



\### Cost

\- Clear: 10–15

\- Pressure: 8–12



\### Kullanım

\- 31–34 bandı



\---



\## 5. Entry Style sistemi



Packet’ler sadece içerikle değil, giriş şekliyle de farklı hissettirir.



\### Entry Style tipleri

\- `straight`

\- `staggered`

\- `split`

\- `dense\_core`

\- `delayed\_spike`

\- `guarded`



\### Kural

Aynı enemy, farklı entry style ile farklı his verebilir.  

Bu sayede tek tek yeni enemy üretmeden çeşitlilik artar.



\---



\## 6. Packet seçme kuralları



\### Kural 1

Aynı packet tipi üst üste 2’den fazla gelmez.



\### Kural 2

Her 3–4 packet’te bir relief veya low-pressure an olur.



\### Kural 3

Yeni solve tanıtıldıysa, hemen arkasından onu okuma fırsatı veren daha temiz packet gelir.



\### Kural 4

Packet dizisi düz çizgi olmaz:

\- baseline

\- pressure

\- relief

\- spike

\- relief

\- mixed



gibi dalgalı akar.



\### Kural 5

Aynı solve tag üst üste çok tekrar edilmez, stage focus dışına taşmaz.



\---



\## 7. Tempo ve packet ilişkisi



Stage tempo template’i packet’leri seçer.



\### Örnek: Intro Template

\- Baseline

\- Baseline

\- DenseSwarm

\- Relief

\- DelayedCharger



\### Örnek: Discovery Template

\- Baseline

\- ArmorCheck

\- Relief

\- DelayedCharger

\- Baseline



\### Örnek: Pressure Template

\- DenseSwarm

\- SplitPressure

\- Relief

\- DelayedCharger

\- GuardedCore



\---



\## 8. World 1 bandlarına göre packet kullanımı



\## 1–5 Tutorial Core

Ağırlık:

\- Baseline

\- küçük DenseSwarm

\- hafif DelayedCharger

\- bol Relief



\### Yasak

\- ağır GuardedCore

\- yoğun EliteSpike

\- çok sert ArmorCheck zinciri



\---



\## 6–10 Build Discovery

Ağırlık:

\- ArmorCheck

\- DelayedCharger

\- küçük EliteSpike

\- Relief



\### Amaç

\- solve kavramını öğretmek



\---



\## 11–20 First Friction / Controlled Complexity

Ağırlık:

\- SplitPressure

\- GuardedCore

\- DenseSwarm

\- Geometry’yi parlatan packet’ler



\### Amaç

\- build farkı hissettirmek



\---



\## 21–30 Specialization / Pressure

Ağırlık:

\- GuardedCore

\- EliteSpike

\- Mixed Pressure

\- daha az Relief



\### Amaç

\- yanlış solve = hissedilen ceza

\- doğru build = hissedilen rahatlık



\---



\## 31–34 Final Prep

Ağırlık:

\- BossPrepPacket

\- EliteSpike

\- ArmorCheck

\- kontrollü Relief



\### Amaç

\- final boss öncesi zihinsel hazırlık



\---



\## 9. İlk otomasyon kuralı



Stage generator şu mantıkla packet dizer:



1\. Stage solve focus’u seçilir  

2\. Tempo template seçilir  

3\. Hedef clear budget ve pressure budget alınır  

4\. Uygun packet havuzundan seçim yapılır  

5\. Relief ve spike dengesi korunur  

6\. Toplam packet maliyeti stage bandına oturtulur  



\### Sonuç

Tek tek:

\- “buraya 2 düşman”

\- “buraya 3 düşman”

yazmak zorunda kalmazsın



\---



\## 10. Görünür oyun hissi hedefleri



Bu kütüphane sayesinde oyuncu şunları hissetmeli:



\- düşmanlar hep aynı ritimde gelmiyor

\- bazen nefes alıyorum

\- bazen baskı artıyor

\- aynı düşman farklı şekilde tehdit olabiliyor

\- build’im bazı packet’lerde gerçekten parlıyor

\- oyun boş değil

\- oyun boğucu da değil



\---



\## 11. Kısa örnek



\### Stage 3 — Intro / Mixed Light

Template:

\- Baseline

\- DenseSwarm

\- Relief

\- DelayedCharger

\- Relief



\### Stage 8 — Armor Discovery

Template:

\- Baseline

\- ArmorCheck

\- Relief

\- DelayedCharger

\- Baseline

\- MiniBossPrep



\### Stage 18 — Geometry / Mixed

Template:

\- DenseSwarm

\- SplitPressure

\- GuardedCore

\- Relief

\- DelayedCharger

\- DenseSwarm



Bunlar doğrudan stage recipe’ye bağlanabilir.



\---



\## 12. Son karar özeti



Enemy Cost Table v1 + Packet Library v1:

\- stage’leri tek tek el yapımı yazma ihtiyacını azaltır

\- düşman hissini çeşitlendirir

\- oyun temposunu matematikle ama görünmez şekilde kontrol eder

\- boş koşu simülatörü hissini engeller

\- sürekli kaçış oyununa da dönüştürmez

\- build, enemy ve stage solve ilişkisini sahada görünür kılar

