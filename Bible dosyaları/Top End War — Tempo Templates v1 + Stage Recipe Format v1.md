\# Top End War — Tempo Templates v1 + Stage Recipe Format v1

\_From packet library to stage generation\_



\---



\## 0. Belgenin amacı



Bu belge, mevcut:

\- Enemy Cost Table

\- Packet Library

\- World 1 Progression \& Budget Model



üzerine, stage üretimini otomasyona yaklaştıracak iki üst katmanı kurar:



1\. \*\*Tempo Templates v1\*\*  

&#x20;  Packet’lerin hangi sırayla ve hangi nabızla akacağını tanımlar.



2\. \*\*Stage Recipe Format v1\*\*  

&#x20;  Her stage’in tek tek el yapımı değil, tanımlı parametrelerle üretilmesini sağlar.



\### Temel ilke

Biz her stage’i tek tek bestelemiyoruz.  

Biz:

\- packet kütüphanesi

\- tempo şablonları

\- recipe formatı



kuruyoruz.



\---



\## 1. Tempo Template nedir?



Tempo template:

\- bir stage’in nabzıdır

\- packet’lerin sırasını belirler

\- relief / pressure / spike dengesini kurar

\- oyuncunun ne zaman rahatlayacağını ve ne zaman karar vereceğini belirler



\### Oyuncunun hissettiği şey

Oyuncu template’i görmez.  

Oyuncu şunu hisseder:

\- oyun düz değil

\- bazen sakin

\- bazen baskılı

\- bazen ani tehdit var

\- ama kaotik değil



\---



\## 2. Tempo Template’in yapı taşları



Her template şu anları kullanır:



\### Warmup

\- stage’e giriş

\- baseline okuma

\- oyuncunun ritmi anlaması



\### Build

\- baskının yavaş artışı

\- packet yoğunluğu büyür



\### Spike

\- anlık karar isteyen tehdit

\- charger / elite / guarded core gibi



\### Relief

\- kısa nefes anı

\- build gücünü hissetme



\### Resolve

\- solve gerektiren net soru

\- armor / elite / swarm / boss prep



\### Exit / Lead-out

\- mini-boss veya sonraki hissi hazırlayan son geçiş



\---



\## 3. Tempo Templates v1



\## 3.1 Intro Template

\### Amaç

\- oyunu öğretmek

\- korkutmadan akıtmak

\- baseline hissi vermek



\### Akış

\- Warmup

\- Warmup

\- Build

\- Relief

\- Light Spike



\### Uygun packet’ler

\- Baseline

\- DenseSwarm (küçük)

\- Relief

\- DelayedCharger (çok hafif)



\### Kullanım

\- World 1 Stage 1–3



\---



\## 3.2 Guided Discovery Template

\### Amaç

\- yeni solve fikrini öğretmek

\- ama oyuncuyu ezmemek



\### Akış

\- Warmup

\- Resolve

\- Relief

\- Build

\- Light Spike

\- Relief



\### Uygun packet’ler

\- Baseline

\- ArmorCheck

\- Relief

\- DelayedCharger

\- küçük DenseSwarm



\### Kullanım

\- Stage 4–8

\- ilk armor / elite / priority öğretimi



\---



\## 3.3 Pressure Wave Template

\### Amaç

\- oyuncuyu akış içinde baskılamak

\- boş koşu hissini kırmak



\### Akış

\- Warmup

\- Build

\- Build

\- Spike

\- Relief

\- Build



\### Uygun packet’ler

\- DenseSwarm

\- SplitPressure

\- DelayedCharger

\- GuardedCore

\- Relief



\### Kullanım

\- Stage 8–15

\- swarm ve tempo odaklı alanlar



\---



\## 3.4 Mixed Test Template

\### Amaç

\- build farkını görünür kılmak

\- farklı solve’ların aynı stage içinde anlam kazanması



\### Akış

\- Warmup

\- Resolve

\- Build

\- Relief

\- Spike

\- Resolve



\### Uygun packet’ler

\- Baseline

\- ArmorCheck

\- GuardedCore

\- DelayedCharger

\- DenseSwarm

\- SplitPressure



\### Kullanım

\- Stage 10–20



\---



\## 3.5 Specialization Template

\### Amaç

\- oyuncunun build yönünün fark yaratmasını sağlamak

\- yanlış cevapları hafif cezalandırmak



\### Akış

\- Build

\- Resolve

\- Spike

\- Relief

\- Resolve

\- Build



\### Uygun packet’ler

\- GuardedCore

\- ArmorCheck

\- EliteSpike

\- DenseSwarm

\- Relief



\### Kullanım

\- Stage 18–28



\---



\## 3.6 Prep Template

\### Amaç

\- yaklaşan boss/mini-boss mantığını öğretmek

\- beam/sniper/elite solve’u öne çıkarmak



\### Akış

\- Resolve

\- Relief

\- Spike

\- Build

\- Boss Prep

\- Relief



\### Uygun packet’ler

\- ArmorCheck

\- EliteSpike

\- BossPrepPacket

\- kısa Relief



\### Kullanım

\- Stage 28–34



\---



\## 3.7 MiniBoss Lead-In Template

\### Amaç

\- mini-boss öncesi oyuncuyu hazırlamak

\- aşırı yormadan sınav hissi oluşturmak



\### Akış

\- Warmup

\- Resolve

\- Relief

\- Spike

\- Lead-in



\### Uygun packet’ler

\- Baseline

\- ArmorCheck

\- DelayedCharger

\- küçük EliteSpike

\- Relief



\### Kullanım

\- mini-boss öncesi son normal stage



\---



\## 4. Template kullanım kuralları



\### Kural 1

Aynı template çok uzun seri halinde kullanılmaz.



\### Kural 2

Aynı solve focus taşıyan template’ler art arda gelirse packet dağılımı değişir.



\### Kural 3

Her template içinde en az 1 relief anı olur.



\### Kural 4

Spike packet’ler erken oyunda daha hafif, geç oyunda daha sert olur.



\### Kural 5

Template stage’in ana sorusunu destekler, stage’i tamamen belirlemez.



\---



\## 5. Stage Recipe Format v1



Her stage şu parametrelerle tanımlanır:



\### Kimlik

\- `stageId`

\- `worldId`

\- `stageBand`



\### Ana tasarım

\- `solveFocus`

\- `tempoTemplate`

\- `primaryThreats`

\- `secondaryThreats`



\### Bütçe

\- `targetClearBudget`

\- `targetPressureBudget`

\- `toleranceBand`



\### Yapı

\- `packetCount`

\- `allowedPacketPool`

\- `reliefFrequency`

\- `spikeIntensity`



\### Ödül

\- `rewardBand`

\- `unlockHint`

\- `buildSnapshotHint`



\---



\## 6. Stage Recipe alan açıklamaları



\## 6.1 solveFocus

Stage’in ana sorduğu soru.



Örnek:

\- `mixed`

\- `swarm`

\- `armor`

\- `elite`

\- `boss\_prep`

\- `long\_fight`



\---



\## 6.2 tempoTemplate

Stage’in nabzını belirler.



Örnek:

\- `intro`

\- `guided\_discovery`

\- `pressure\_wave`

\- `mixed\_test`

\- `specialization`

\- `prep`

\- `miniboss\_lead\_in`



\---



\## 6.3 primaryThreats

Bu stage’in en görünür tehditleri.



Örnek:

\- Trooper + Swarm

\- Charger + Trooper

\- Brute + Support

\- Elite + Armor



Oyuncuya doğrudan bu listede gösterilmez, ama stage tag’lerini besler.



\---



\## 6.4 targetClearBudget

Stage’in toplam temizleme maliyeti.



Bu:

\- oyuncunun beklenen run gücüne göre belirlenir

\- çok düşükse boş

\- çok yüksekse boğucu olur



\---



\## 6.5 targetPressureBudget

Stage’in toplam baskı maliyeti.



Bu:

\- oyuncuya sürekli kaçma zorunluluğu getirmeyecek

\- ama tehdit hissini canlı tutacak



\---



\## 6.6 toleranceBand

Oyuncunun ne kadar hata yapabileceği.



Örnek:

\- `High`

\- `Medium`

\- `Low`



\### World 1 yorumu

\- 1–5 → High

\- 6–15 → Medium-High

\- 16–25 → Medium

\- 26–34 → Medium-Low

\- 35 → boss özel



\---



\## 6.7 packetCount

Stage’in kaç packet’ten oluşacağı.



İlk sürüm için öneri:

\- kısa stage: `5–6`

\- standart stage: `6–8`

\- yüksek baskı stage: `7–9`

\- prep stage: `5–7`



\---



\## 6.8 allowedPacketPool

Bu stage’in kullanabileceği packet tipleri.



Örnek:

```text

\[Baseline, DenseSwarm, Relief, DelayedCharger]



Bu sayede erken stage’e yanlış packet karışmaz.



6.9 reliefFrequency



Stage’in ne sıklıkla nefes verdiği.



Örnek:



every\_2\_packets

every\_3\_packets

light\_relief\_only

6.10 spikeIntensity



Ani karar anlarının şiddeti.



Örnek:



low

medium

high



Bu, DelayedCharger / EliteSpike / GuardedCore yoğunluğunu belirler.



6.11 rewardBand



O stage’in verdiği hissedilir ödül seviyesi.



Örnek:



light

standard

spike

prep

boss

6.12 unlockHint



Bu stage, meta tarafta neyi ima ediyor?



Örnek:



“armor answer needed soon”

“squad value growing”

“boss prep coming”



Bu oyuncuya tam yazılmak zorunda değil; tasarım rehberi olarak da kullanılır.



7\. Örnek Stage Recipe’ler

Stage 1

stageId: W1-01

stageBand: tutorial\_core

solveFocus: mixed

tempoTemplate: intro

primaryThreats: trooper

secondaryThreats: swarm\_light

targetClearBudget: low

targetPressureBudget: low

toleranceBand: high

packetCount: 5

allowedPacketPool: \[Baseline, DenseSwarm, Relief, DelayedCharger\_Light]

reliefFrequency: every\_2\_packets

spikeIntensity: low

rewardBand: light

buildSnapshotHint: balanced

Stage 7

stageId: W1-07

stageBand: build\_discovery

solveFocus: armor

tempoTemplate: guided\_discovery

primaryThreats: armored\_brute

secondaryThreats: trooper, charger

targetClearBudget: medium

targetPressureBudget: medium

toleranceBand: medium\_high

packetCount: 6

allowedPacketPool: \[Baseline, ArmorCheck, Relief, DelayedCharger, DenseSwarm\_Light]

reliefFrequency: every\_3\_packets

spikeIntensity: low\_medium

rewardBand: standard

buildSnapshotHint: armor\_break

Stage 18

stageId: W1-18

stageBand: controlled\_complexity

solveFocus: mixed

tempoTemplate: mixed\_test

primaryThreats: swarm, charger

secondaryThreats: brute

targetClearBudget: medium\_high

targetPressureBudget: medium\_high

toleranceBand: medium

packetCount: 7

allowedPacketPool: \[DenseSwarm, SplitPressure, ArmorCheck, Relief, DelayedCharger, GuardedCore]

reliefFrequency: every\_3\_packets

spikeIntensity: medium

rewardBand: standard

buildSnapshotHint: mixed\_pressure

Stage 32

stageId: W1-32

stageBand: final\_prep

solveFocus: boss\_prep

tempoTemplate: prep

primaryThreats: elite, armor

secondaryThreats: long\_fight

targetClearBudget: high

targetPressureBudget: medium\_high

toleranceBand: medium\_low

packetCount: 6

allowedPacketPool: \[ArmorCheck, EliteSpike, Relief, BossPrepPacket, GuardedCore\_Light]

reliefFrequency: every\_3\_packets

spikeIntensity: medium\_high

rewardBand: prep

buildSnapshotHint: boss\_prep

8\. Stage üretim kuralları

Kural 1



Her stage tek bir ana solve focus taşır.

Secondary threat olabilir ama ana soru tek olmalı.



Kural 2



Template seçimi solve focus ile uyumlu olmalı.



Kural 3



Pressure budget toleransı aşmamalı.



Kural 4



Relief penceresi tamamen kaldırılmaz.



Kural 5



Packet pool stage bandına uygun olmalı.



Kural 6



Aynı stage üst üste aynı hissi vermemeli; komşu stage’lerde template veya solve focus değişmeli.



9\. World 1 için önerilen template dağılımı

1–3

intro

4–8

guided\_discovery

9–15

pressure\_wave

mixed\_test

16–22

mixed\_test

specialization

23–30

specialization

pressure\_wave

31–34

prep

miniboss\_lead\_in

35

boss özel

10\. Görünürde oyuncuya ne yansır?



Bu sistemin oyuncuya görünen sade yüzü:



Stage Card

2–3 threat tag

kısa reward vurgusu

build snapshot hint

Run içinde

düşmanlar farklı hislerle gelir

ama kaos olmaz

Sonuç



Oyuncu şunu düşünür:



“bu stage armor istiyordu”

“bu stage çok kalabalıktı”

“bu stage boss öncesi hazırlık gibiydi”



Ama bunu spreadsheet gibi okumaz.



11\. Gelecekte otomasyon



Bu yapı daha sonra rahatça:



packet generator

stage builder

pool selector

threat scaler



sistemlerine bağlanabilir.



Yani bugünden kurduğumuz şey, yarınki üretim hızını arttırır.



12\. Son karar özeti



Tempo Templates v1 + Stage Recipe Format v1:



stage’leri tek tek el yapımı yazma ihtiyacını azaltır

packet library’yi üretime bağlar

zorluk, relief ve pressure dengesini otomasyona yaklaştırır

oyuncuya arkadaki matematiği göstermeden güçlü oyun hissi üretir

World 1’i daha düzenli, daha kontrollü ve daha ölçeklenebilir hale getirir

