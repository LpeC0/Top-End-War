\# Top End War — World 1 Progression \& Budget Model v1

\_Hidden math, visible clarity\_



\---



\## 0. Belgenin amacı



Bu belge, World 1 için:

\- oyuncunun ilk 1 saatte nasıl ilerleyeceğini

\- stage’lerin nasıl zorluk üreteceğini

\- spawn yoğunluğunun nasıl dengeleneceğini

\- build / gate / squad / stage bilgisinin nasıl birleşeceğini

\- sistemi tek tek el yapımı bölüm tasarlamadan nasıl otomasyona yaklaştıracağımızı



tanımlar.



\### Temel ilke

Oyuncu bu matematiği \*\*görmez\*\*.  

Oyuncu şunu hisseder:

\- oyun akıyor

\- düşmanlar anlamsız değil

\- seçimlerim işe yarıyor

\- bazen hata yapabiliyorum

\- bazen doğru build parlıyor

\- oyun boş değil ama boğucu da değil



\### Kural

\- arkada sistem güçlü olacak

\- önde sunum sade olacak



\---



\## 1. Tasarım hedefi



World 1’in amacı yalnızca “ilk dünya” olmak değildir.  

World 1, oyunun ana hissini ve ana öğretimini taşır.



Oyuncu World 1’de şunları öğrenmelidir:

\- hangi silah hangi problemi çözer

\- gate seçimi neden önemlidir

\- build yalnızca sayısal bonus değildir

\- squad desteği gerçek fark yaratır

\- düşmanlar farklı kararlar ister

\- boss hazırlığı anlamlıdır

\- hata yapılabilir ama her hata affedilmez



\---



\## 2. Oyuncu açısından görünür sadelik



Oyuncuya doğrudan gösterilecek bilgiler:



\- kısa stage threat tag’leri

\- kısa build snapshot

\- kısa gate başlığı

\- net enemy rolleri

\- temiz boss telegraph

\- net result / reward hissi



Oyuncuya \*\*gösterilmeyecek\*\* gizli tasarım katmanı:



\- clear budget

\- pressure budget

\- packet cost

\- expected power band

\- solve weight

\- tolerance ratio



\### Sonuç

Oyuncu:

> “Bunlarla mı uğraşacağım?”  

demez.



Bu sistem sadece tasarımcının ve oyunun arka planının kullandığı iskelet olur.



\---



\## 3. Temel model



World 1’de her stage, 4 görünmez tasarım ekseniyle kurulur:



\### A. Clear Budget

Bu stage’i temizlemek için ne kadar gerçek hasar/zaman gerekiyor?



\### B. Pressure Budget

Bu stage, oyuncuya ne kadar baskı kurabilir?



\### C. Tolerance Window

Oyuncu ne kadar hata yapabilir?



\### D. Solve Focus

Bu stage, hangi tür çözümü öne çıkarmak istiyor?



Bu 4 eksen doğru kurulduğunda:

\- stage boş hissetmez

\- stage unfair hissetmez

\- stage sürekli kaçma oyunu olmaz

\- stage dümdüz koşu simülatörü olmaz



\---



\## 4. Gizli metrikler



\## 4.1 Clear Cost

Bir düşmanı ya da packet’i \*\*temizlemenin maliyeti\*\*.



Bu şunlardan etkilenir:

\- effective HP

\- armor

\- hedefin çözüm gerektirip gerektirmemesi

\- hareketi yüzünden vurulma zorluğu

\- ölmeden önce baskı süresi



\### Basit anlamı

“Bunu öldürmek ne kadar iş?”



\---



\## 4.2 Pressure Cost

Bir düşman ya da packet \*\*öldürülmezse\*\* ne kadar baskı yaratıyor?



Bu bir damage type değildir.  

Bu, tasarım metriğidir.



Şunlardan etkilenir:

\- oyuncuya ne kadar hızlı ulaşıyor

\- lane’i kapatıyor mu

\- temas ederse ne kadar ceza veriyor

\- oyuncuyu öncelik değiştirmeye zorluyor mu

\- görmezden gelinince çığ gibi büyüyor mu



\### Basit anlamı

“Bunu görmezden gelirsem başım ne kadar belaya girer?”



\---



\## 4.3 Tolerance Cost

Oyuncunun bu stage’de ne kadar darbe yiyebileceği / hata yapabileceği.



Şunlardan oluşur:

\- commander HP

\- sustain gate ihtimali

\- squad buffer değeri

\- düşman temas ritmi

\- stage bandı



\### Basit anlamı

“Bu stage no-hit zorunluluğu istiyor mu, yoksa biraz affediyor mu?”



\---



\## 4.4 Solve Focus

Stage’in sorduğu ana soru.



Örnek solve focus:

\- SWARM

\- ARMOR

\- ELITE

\- MIXED

\- LONG FIGHT

\- BOSS PREP



Stage’in solve focus’u:

\- build snapshot ile

\- threat tag’lerle

\- enemy kompozisyonuyla

\- gate değerleriyle

oyuncuya hissettirilir.



\---



\## 5. Basit sayısal sistem



İlk sürümde aşırı karmaşık formül istemiyoruz.  

Bu yüzden puan tabanlı başlıyoruz.



\### Enemy tarafında

Her enemy tipi için:

\- `clearCost` = 1–10

\- `pressureCost` = 1–10



\### Packet tarafında

Her packet için:

\- `packetClearCost`

\- `packetPressureCost`

\- `solveTags`



\### Stage tarafında

Her stage için:

\- `targetClearBudget`

\- `targetPressureBudget`

\- `toleranceBand`

\- `solveFocus`

\- `tempoTemplate`



Bu kadar.



\### Neden böyle?

Çünkü ilk çalışan sistem için bu yeterince güçlü ve yeterince sade.



\---



\## 6. Enemy Cost tablosu — başlangıç



Bu ilk versiyon. Sonra testle ayarlanır.



| Enemy | Clear Cost | Pressure Cost | Ana Rol |

|---|---:|---:|---|

| Trooper | 2 | 2 | baseline |

| Swarm | 1 | 1 | sayı baskısı |

| Charger | 3 | 6 | öncelik tehdidi |

| Armored Brute | 6 | 4 | armor check |

| Elite Charger | 5 | 8 | panik + öncelik çakışması |

| Gatekeeper Walker | 10 | 9 | mini-boss sınavı |

| War Machine | 11 | 8 | alan baskısı |

| World 1 Final Boss | 16 | 12 | final sınavı |



\### Not 1

Swarm tek başına düşük pressure taşır.  

Ama packet içinde toplu geldiğinde \*\*çarpanlı pressure\*\* üretir.



\### Not 2

Brute’un clear cost’i yüksek, pressure’ı orta.  

Charger’ın clear cost’i orta, pressure’ı yüksek.  

Bu ayrım çok önemli.



\---



\## 7. Packet Library v1



Biz her bölümü el ile yazmayacağız.  

Packet kütüphanesi kuracağız.



\## 7.1 Packet türleri



\### Baseline Packet

İçerik:

\- 2–4 Trooper



Amaç:

\- akış başlatmak

\- baseline DPS testi



\### Dense Swarm Packet

İçerik:

\- 1 Trooper + 2–5 Swarm



Amaç:

\- tempo / lane temizliği

\- SMG değerini hissettirmek



\### Delayed Charger Packet

İçerik:

\- önce 2–3 normal

\- kısa gecikmeyle 1 Charger



Amaç:

\- öncelik testi



\### Armor Check Packet

İçerik:

\- 1 Brute

\- yanında 1–2 Trooper



Amaç:

\- sniper / solve / breacher değerini hissettirmek



\### Guarded Core Packet

İçerik:

\- merkezde Brute veya Elite

\- yanında support düşmanlar



Amaç:

\- “önce neyi vuracağım?” sorusu



\### Split Pressure Packet

İçerik:

\- sol/sağ dağılmış küçük gruplar



Amaç:

\- düz çizgi hissini kırmak

\- lane farkındalığı



\### Relief Packet

İçerik:

\- düşük tehditli kısa grup



Amaç:

\- kısa nefes alanı vermek



\### Boss Prep Packet

İçerik:

\- elite + armor + uzun hedef kombinasyonu



Amaç:

\- boss öncesi hazırlık öğretmek



\---



\## 7.2 Packet kuralları



\- aynı packet tipi üst üste fazla tekrarlanmaz

\- her packet tek bir ana karar sorar

\- packet’ler arasında küçük jitter vardır

\- her 3–4 packet’te bir relief görülür

\- yeni tehdit tanıtıldıysa hemen arkasından oyuncuya onu okuma fırsatı verilir



\---



\## 8. Tempo Templates v1



Stage’leri tek tek yazmak yerine tempo şablonları kullanacağız.



\## 8.1 Intro

`Baseline → Baseline → Dense → Relief → DelayedSpike`



Amaç:

\- erken öğretim

\- okunur akış



\## 8.2 Discovery

`Baseline → ArmorCheck → Relief → DelayedSpike → Baseline`



Amaç:

\- solve değeri tanıtmak



\## 8.3 Pressure

`Baseline → Dense → Split → DelayedSpike → Relief → Dense`



Amaç:

\- baskı kurmak ama boğmamak



\## 8.4 Mixed

`Baseline → GuardedCore → Dense → Relief → DelayedSpike → Split`



Amaç:

\- build farkını görünür kılmak



\## 8.5 Prep

`ArmorCheck → Relief → EliteSpike → BossPrepPacket → Relief`



Amaç:

\- boss öncesi zihinsel hazırlık



\---



\## 9. Stage Recipe sistemi



Her stage’i elle yazmak yerine recipe ile kuracağız.



Her StageConfig / stage recipe şu temel bilgiyi taşır:



\- `stageBand`

\- `solveFocus`

\- `tempoTemplate`

\- `targetClearBudget`

\- `targetPressureBudget`

\- `toleranceBand`

\- `packetCount`

\- `rewardBand`



\### Sonuç

Tek tek:

\- “Stage 7’de 55 birimde 3 düşman”

\- “Stage 8’de 120 birimde 2 düşman”



diye el yapımı yazmak zorunda kalmayız.



Sistem recipe + packet library ile üretir.



\---



\## 10. World 1 budget bantları



\## 10.1 Stage band hedefleri



\### 1–5 Tutorial Core

\- düşük clear budget

\- düşük pressure budget

\- yüksek tolerans

\- relief sık



\### 6–10 Build Discovery

\- orta clear budget

\- orta pressure budget

\- ilk solve baskısı

\- tolerans hâlâ makul



\### 11–15 First Friction

\- clear budget artar

\- pressure daha görünür olur

\- risk/reward açılır

\- tolerans biraz azalır



\### 16–20 Controlled Complexity

\- mixed packet artar

\- geometry ve support packet’ler girer

\- tempo daha dalgalı olur



\### 21–25 Specialization

\- build farkı daha net hissedilir

\- solve focus daha önemli olur

\- yanlış seçim hafif cezalandırır



\### 26–30 Pressure \& Punishment

\- pressure budget belirgin artar

\- tolerans azalır ama sıfırlanmaz

\- oyun hâlâ boş koşu olmaz



\### 31–34 Final Prep

\- long-fight

\- elite + armor

\- beam/sniper solve hissi

\- boss prep gate değeri



\### 35 Final Boss

\- packet yerine boss odaklı sınav

\- ama öncesindeki prep mantığının sonucu olarak okunur



\---



\## 11. Tolerance Model



Oyuncu sürekli kaçmak zorunda kalmamalı.  

Ama sürekli tanklayarak da geçmemeli.



Bu yüzden her stage’te görünmez bir tolerans hedefi var.



\## Erken oyun

Oyuncu:

\- birkaç temas affedebilir

\- yanlış pozisyonla bile hemen ölmez

\- öğrenme alanı vardır



\## Orta oyun

Oyuncu:

\- çözümsüz packet’lerde hasar yiyebilir

\- ama sustain veya doğru build ile toparlayabilir



\## Geç World 1

Oyuncu:

\- yanlış build ve kötü tempo ile cezalandırılır

\- ama doğru solve ve doğru gate ile rahatlar



\### Kural

Hiçbir stage:

\- saf no-hit oyunu istemez

\- ama sınırsız hata da affetmez



\---



\## 12. Görünür oyuncu ilerlemesi



Bu çok önemli.  

Oyuncu sadece sayı değil, \*\*gelişim hissi\*\* yaşamalı.



\## 12.1 İlk 1 saat beklentisi



\### 0–10 dakika

Oyuncu:

\- hareketi öğrenir

\- gate okur

\- baseline döngüyü anlar

\- 1–5 bandına doğal girer



\### 10–25 dakika

Oyuncu:

\- build’in yalnızca hasar olmadığını fark eder

\- first solve hissini yaşar

\- armor / elite fikrini görür

\- ilk mini-boss sınavına yaklaşır



\### 25–45 dakika

Oyuncu:

\- 6–10 bandında sağlam durabilir

\- doğru gate seçiminin fark yarattığını hisseder

\- support layer’ı daha bilinçli kullanmaya başlar



\### 45–60 dakika

Oyuncu ortalama olarak:

\- Stage 8–12 civarına gelmiş olmalı

\- çok iyi oynayan biraz öne gidebilir

\- ama World 1’i bir saatte söküp bitirmemeli



\---



\## 12.2 1 saatin sonunda oyuncunun elinde ne olmalı?



\- en az 1–2 silah yönü hakkında gerçek anlayış

\- birkaç anlamlı equipment/modifier

\- squad desteğinin ne işe yaradığını görmüş olmak

\- bir mini-boss deneyimi

\- armor / elite solve fikri

\- bir sonraki girişte daha iyi build kurma isteği



\### Kural

1 saat sonunda oyuncu:

\- “ben hâlâ hiçbir şey anlamadım” dememeli

\- “oyun bitti” de dememeli



\---



\## 13. Run içi ve run dışı ilerleme



\## 13.1 Run içi ilerleme

Run sırasında oyuncu güçlenir:



\- gate’ler

\- reinforce

\- sustain

\- solve etkileri

\- boss prep kararları



Bu geçicidir.



\## 13.2 Run dışı ilerleme

Run sonrasında oyuncu şunları kazanabilir:



\- yeni weapon archetype erişimi

\- yeni equipment seçenekleri

\- support preset / squad bias açılımı

\- yeni stage erişimi

\- küçük kalıcı gelişim



\### Çok önemli kural

Run dışı progression:

\- oyunu ezdirecek dev stat şişirmesi olmamalı

\- daha çok \*\*yeni seçenek\*\* açmalı



\---



\## 14. Şans sistemi



Şans oyuncuyu eğlendirmeli ama kapana kıstırmamalı.



\### Kural

\- tamamen çöp gate dizilimi olmamalı

\- her birkaç seçimde en az bir güvenli/nötr yol olmalı

\- solve ihtiyacı olan stage’de solve kapıları mantıklı sıklıkta gelmeli

\- aşırı şanslı oyuncu parlayabilir

\- aşırı şanssız oyuncu “run öldü” dememeli



\### Bunun için

Gate pool sistemi:

\- family dağılımı

\- stage solve focus

\- tekrar koruması

\- güvenli seçenek oranı

kullanır



\---



\## 15. Reward Band sistemi



Her stage yalnızca zorluk değil, ödül bandı da taşır.



\### Reward Band türleri

\- `Light`

\- `Standard`

\- `Spike`

\- `BossPrep`

\- `Boss`



\### Amaç

Oyuncu:

\- niye ilerlediğini

\- niye zorlandığını

\- ne kazandığını

hisseder



\### Kural

Zor stage’in ödülü görünür olmalı.  

Ama ödül ekonomiyi patlatmamalı.



\---



\## 16. Basit otomasyon formu



Bu sistemin generator mantığı sade tutulur.



\### Girdi

\- stageBand

\- solveFocus

\- expectedPowerBand

\- expectedSurvivabilityBand

\- tempoTemplate

\- packetPool



\### Çıktı

\- packet sayısı

\- packet tür dağılımı

\- toplam clear budget

\- toplam pressure budget

\- relief noktaları

\- reward band



\---



\## 17. Oyuncunun hissedeceği sonuç



Oyuncu matematiği görmez.  

Ama şunu hisseder:



\- oyun boş değil

\- düşmanlar aynı ritimde akmıyor

\- bazen rahatlıyorum, bazen baskı geliyor

\- build seçimim fark yaratıyor

\- yanlış solve ile uzuyorum

\- doğru solve ile akıyorum

\- bir sonraki stage için daha hazırlıklı olmak istiyorum



Bu, bu modelin asıl hedefidir.



\---



\## 18. Uygulama için sade veri sözleşmesi



\## EnemyArchetype

\- clearCost

\- pressureCost

\- solveTags

\- role

\- spawnWeightBand



\## Packet

\- packetType

\- enemyList

\- packetClearCost

\- packetPressureCost

\- solveTags



\## StageRecipe

\- stageBand

\- solveFocus

\- tempoTemplate

\- targetClearBudget

\- targetPressureBudget

\- toleranceBand

\- rewardBand

\- packetCount



Bu kadar.  

İlk sürüm için yeterlidir.



\---



\## 19. Riskler



\### Risk 1

Sistem arkada iyi ama önde çok açıklayıcı olursa oyuncu yorulur.



\### Risk 2

Pressure budget fazla sert kurulursa oyun sürekli kaçış oyununa döner.



\### Risk 3

Tolerance çok yüksek olursa oyun boş koşu simülatörü olur.



\### Risk 4

Solve focus çok sert olursa tek doğru build meta oluşur.



\### Risk 5

Reward pacing zayıf olursa ilerleme hissi ölür.



\---



\## 20. Son karar özeti



World 1 Progression \& Budget Model v1:

\- oyuncuya görünürde sade kalır

\- arkada güçlü matematik taşır

\- stage’leri tek tek elle yazma ihtiyacını azaltır

\- packet library + tempo template + stage recipe ile otomasyona yaklaşır

\- oyuncuyu sürekli kaçmaya zorlamaz

\- ama boş koşu simülatörüne de dönmez

\- 1 saat sonunda anlamlı ilerleme ve öğrenme hissi vermeyi hedefler

\- run dışı progression’ı seçenek açan yapıda tutar

\- şansı eğlenceli ama adil kullanır

