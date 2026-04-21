\\# Top End War — World 1 Progression \\\& Budget Model v1



\\\_Hidden math, visible clarity\\\_







\\---







\\## 0. Belgenin amacı







Bu belge, World 1 için:



\\- oyuncunun ilk 1 saatte nasıl ilerleyeceğini



\\- stage’lerin nasıl zorluk üreteceğini



\\- spawn yoğunluğunun nasıl dengeleneceğini



\\- build / gate / squad / stage bilgisinin nasıl birleşeceğini



\\- sistemi tek tek el yapımı bölüm tasarlamadan nasıl otomasyona yaklaştıracağımızı







tanımlar.







\\### Temel ilke



Oyuncu bu matematiği \\\*\\\*görmez\\\*\\\*.  



Oyuncu şunu hisseder:



\\- oyun akıyor



\\- düşmanlar anlamsız değil



\\- seçimlerim işe yarıyor



\\- bazen hata yapabiliyorum



\\- bazen doğru build parlıyor



\\- oyun boş değil ama boğucu da değil







\\### Kural



\\- arkada sistem güçlü olacak



\\- önde sunum sade olacak







\\---







\\## 1. Tasarım hedefi







World 1’in amacı yalnızca “ilk dünya” olmak değildir.  



World 1, oyunun ana hissini ve ana öğretimini taşır.







Oyuncu World 1’de şunları öğrenmelidir:



\\- hangi silah hangi problemi çözer



\\- gate seçimi neden önemlidir



\\- build yalnızca sayısal bonus değildir



\\- squad desteği gerçek fark yaratır



\\- düşmanlar farklı kararlar ister



\\- boss hazırlığı anlamlıdır



\\- hata yapılabilir ama her hata affedilmez







\\---







\\## 2. Oyuncu açısından görünür sadelik







Oyuncuya doğrudan gösterilecek bilgiler:







\\- kısa stage threat tag’leri



\\- kısa build snapshot



\\- kısa gate başlığı



\\- net enemy rolleri



\\- temiz boss telegraph



\\- net result / reward hissi







Oyuncuya \\\*\\\*gösterilmeyecek\\\*\\\* gizli tasarım katmanı:







\\- clear budget



\\- pressure budget



\\- packet cost



\\- expected power band



\\- solve weight



\\- tolerance ratio







\\### Sonuç



Oyuncu:



> “Bunlarla mı uğraşacağım?”  



demez.







Bu sistem sadece tasarımcının ve oyunun arka planının kullandığı iskelet olur.







\\---







\\## 3. Temel model







World 1’de her stage, 4 görünmez tasarım ekseniyle kurulur:







\\### A. Clear Budget



Bu stage’i temizlemek için ne kadar gerçek hasar/zaman gerekiyor?







\\### B. Pressure Budget



Bu stage, oyuncuya ne kadar baskı kurabilir?







\\### C. Tolerance Window



Oyuncu ne kadar hata yapabilir?







\\### D. Solve Focus



Bu stage, hangi tür çözümü öne çıkarmak istiyor?







Bu 4 eksen doğru kurulduğunda:



\\- stage boş hissetmez



\\- stage unfair hissetmez



\\- stage sürekli kaçma oyunu olmaz



\\- stage dümdüz koşu simülatörü olmaz







\\---







\\## 4. Gizli metrikler







\\## 4.1 Clear Cost



Bir düşmanı ya da packet’i \\\*\\\*temizlemenin maliyeti\\\*\\\*.







Bu şunlardan etkilenir:



\\- effective HP



\\- armor



\\- hedefin çözüm gerektirip gerektirmemesi



\\- hareketi yüzünden vurulma zorluğu



\\- ölmeden önce baskı süresi







\\### Basit anlamı



“Bunu öldürmek ne kadar iş?”







\\---







\\## 4.2 Pressure Cost



Bir düşman ya da packet \\\*\\\*öldürülmezse\\\*\\\* ne kadar baskı yaratıyor?







Bu bir damage type değildir.  



Bu, tasarım metriğidir.







Şunlardan etkilenir:



\\- oyuncuya ne kadar hızlı ulaşıyor



\\- lane’i kapatıyor mu



\\- temas ederse ne kadar ceza veriyor



\\- oyuncuyu öncelik değiştirmeye zorluyor mu



\\- görmezden gelinince çığ gibi büyüyor mu







\\### Basit anlamı



“Bunu görmezden gelirsem başım ne kadar belaya girer?”







\\---







\\## 4.3 Tolerance Cost



Oyuncunun bu stage’de ne kadar darbe yiyebileceği / hata yapabileceği.







Şunlardan oluşur:



\\- commander HP



\\- sustain gate ihtimali



\\- squad buffer değeri



\\- düşman temas ritmi



\\- stage bandı







\\### Basit anlamı



“Bu stage no-hit zorunluluğu istiyor mu, yoksa biraz affediyor mu?”







\\---







\\## 4.4 Solve Focus



Stage’in sorduğu ana soru.







Örnek solve focus:



\\- SWARM



\\- ARMOR



\\- ELITE



\\- MIXED



\\- LONG FIGHT



\\- BOSS PREP







Stage’in solve focus’u:



\\- build snapshot ile



\\- threat tag’lerle



\\- enemy kompozisyonuyla



\\- gate değerleriyle



oyuncuya hissettirilir.







\\---







\\## 5. Basit sayısal sistem







İlk sürümde aşırı karmaşık formül istemiyoruz.  



Bu yüzden puan tabanlı başlıyoruz.







\\### Enemy tarafında



Her enemy tipi için:



\\- `clearCost` = 1–10



\\- `pressureCost` = 1–10







\\### Packet tarafında



Her packet için:



\\- `packetClearCost`



\\- `packetPressureCost`



\\- `solveTags`







\\### Stage tarafında



Her stage için:



\\- `targetClearBudget`



\\- `targetPressureBudget`



\\- `toleranceBand`



\\- `solveFocus`



\\- `tempoTemplate`







Bu kadar.







\\### Neden böyle?



Çünkü ilk çalışan sistem için bu yeterince güçlü ve yeterince sade.







\\---







\\## 6. Enemy Cost tablosu — başlangıç







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







\\### Not 1



Swarm tek başına düşük pressure taşır.  



Ama packet içinde toplu geldiğinde \\\*\\\*çarpanlı pressure\\\*\\\* üretir.







\\### Not 2



Brute’un clear cost’i yüksek, pressure’ı orta.  



Charger’ın clear cost’i orta, pressure’ı yüksek.  



Bu ayrım çok önemli.







\\---







\\## 7. Packet Library v1







Biz her bölümü el ile yazmayacağız.  



Packet kütüphanesi kuracağız.







\\## 7.1 Packet türleri







\\### Baseline Packet



İçerik:



\\- 2–4 Trooper







Amaç:



\\- akış başlatmak



\\- baseline DPS testi







\\### Dense Swarm Packet



İçerik:



\\- 1 Trooper + 2–5 Swarm







Amaç:



\\- tempo / lane temizliği



\\- SMG değerini hissettirmek







\\### Delayed Charger Packet



İçerik:



\\- önce 2–3 normal



\\- kısa gecikmeyle 1 Charger







Amaç:



\\- öncelik testi







\\### Armor Check Packet



İçerik:



\\- 1 Brute



\\- yanında 1–2 Trooper







Amaç:



\\- sniper / solve / breacher değerini hissettirmek







\\### Guarded Core Packet



İçerik:



\\- merkezde Brute veya Elite



\\- yanında support düşmanlar







Amaç:



\\- “önce neyi vuracağım?” sorusu







\\### Split Pressure Packet



İçerik:



\\- sol/sağ dağılmış küçük gruplar







Amaç:



\\- düz çizgi hissini kırmak



\\- lane farkındalığı







\\### Relief Packet



İçerik:



\\- düşük tehditli kısa grup







Amaç:



\\- kısa nefes alanı vermek







\\### Boss Prep Packet



İçerik:



\\- elite + armor + uzun hedef kombinasyonu







Amaç:



\\- boss öncesi hazırlık öğretmek







\\---







\\## 7.2 Packet kuralları







\\- aynı packet tipi üst üste fazla tekrarlanmaz



\\- her packet tek bir ana karar sorar



\\- packet’ler arasında küçük jitter vardır



\\- her 3–4 packet’te bir relief görülür



\\- yeni tehdit tanıtıldıysa hemen arkasından oyuncuya onu okuma fırsatı verilir







\\---







\\## 8. Tempo Templates v1







Stage’leri tek tek yazmak yerine tempo şablonları kullanacağız.







\\## 8.1 Intro



`Baseline → Baseline → Dense → Relief → DelayedSpike`







Amaç:



\\- erken öğretim



\\- okunur akış







\\## 8.2 Discovery



`Baseline → ArmorCheck → Relief → DelayedSpike → Baseline`







Amaç:



\\- solve değeri tanıtmak







\\## 8.3 Pressure



`Baseline → Dense → Split → DelayedSpike → Relief → Dense`







Amaç:



\\- baskı kurmak ama boğmamak







\\## 8.4 Mixed



`Baseline → GuardedCore → Dense → Relief → DelayedSpike → Split`







Amaç:



\\- build farkını görünür kılmak







\\## 8.5 Prep



`ArmorCheck → Relief → EliteSpike → BossPrepPacket → Relief`







Amaç:



\\- boss öncesi zihinsel hazırlık







\\---







\\## 9. Stage Recipe sistemi







Her stage’i elle yazmak yerine recipe ile kuracağız.







Her StageConfig / stage recipe şu temel bilgiyi taşır:







\\- `stageBand`



\\- `solveFocus`



\\- `tempoTemplate`



\\- `targetClearBudget`



\\- `targetPressureBudget`



\\- `toleranceBand`



\\- `packetCount`



\\- `rewardBand`







\\### Sonuç



Tek tek:



\\- “Stage 7’de 55 birimde 3 düşman”



\\- “Stage 8’de 120 birimde 2 düşman”







diye el yapımı yazmak zorunda kalmayız.







Sistem recipe + packet library ile üretir.







\\---







\\## 10. World 1 budget bantları







\\## 10.1 Stage band hedefleri







\\### 1–5 Tutorial Core



\\- düşük clear budget



\\- düşük pressure budget



\\- yüksek tolerans



\\- relief sık







\\### 6–10 Build Discovery



\\- orta clear budget



\\- orta pressure budget



\\- ilk solve baskısı



\\- tolerans hâlâ makul







\\### 11–15 First Friction



\\- clear budget artar



\\- pressure daha görünür olur



\\- risk/reward açılır



\\- tolerans biraz azalır







\\### 16–20 Controlled Complexity



\\- mixed packet artar



\\- geometry ve support packet’ler girer



\\- tempo daha dalgalı olur







\\### 21–25 Specialization



\\- build farkı daha net hissedilir



\\- solve focus daha önemli olur



\\- yanlış seçim hafif cezalandırır







\\### 26–30 Pressure \\\& Punishment



\\- pressure budget belirgin artar



\\- tolerans azalır ama sıfırlanmaz



\\- oyun hâlâ boş koşu olmaz







\\### 31–34 Final Prep



\\- long-fight



\\- elite + armor



\\- beam/sniper solve hissi



\\- boss prep gate değeri







\\### 35 Final Boss



\\- packet yerine boss odaklı sınav



\\- ama öncesindeki prep mantığının sonucu olarak okunur







\\---







\\## 11. Tolerance Model







Oyuncu sürekli kaçmak zorunda kalmamalı.  



Ama sürekli tanklayarak da geçmemeli.







Bu yüzden her stage’te görünmez bir tolerans hedefi var.







\\## Erken oyun



Oyuncu:



\\- birkaç temas affedebilir



\\- yanlış pozisyonla bile hemen ölmez



\\- öğrenme alanı vardır







\\## Orta oyun



Oyuncu:



\\- çözümsüz packet’lerde hasar yiyebilir



\\- ama sustain veya doğru build ile toparlayabilir







\\## Geç World 1



Oyuncu:



\\- yanlış build ve kötü tempo ile cezalandırılır



\\- ama doğru solve ve doğru gate ile rahatlar







\\### Kural



Hiçbir stage:



\\- saf no-hit oyunu istemez



\\- ama sınırsız hata da affetmez







\\---







\\## 12. Görünür oyuncu ilerlemesi







Bu çok önemli.  



Oyuncu sadece sayı değil, \\\*\\\*gelişim hissi\\\*\\\* yaşamalı.







\\## 12.1 İlk 1 saat beklentisi







\\### 0–10 dakika



Oyuncu:



\\- hareketi öğrenir



\\- gate okur



\\- baseline döngüyü anlar



\\- 1–5 bandına doğal girer







\\### 10–25 dakika



Oyuncu:



\\- build’in yalnızca hasar olmadığını fark eder



\\- first solve hissini yaşar



\\- armor / elite fikrini görür



\\- ilk mini-boss sınavına yaklaşır







\\### 25–45 dakika



Oyuncu:



\\- 6–10 bandında sağlam durabilir



\\- doğru gate seçiminin fark yarattığını hisseder



\\- support layer’ı daha bilinçli kullanmaya başlar







\\### 45–60 dakika



Oyuncu ortalama olarak:



\\- Stage 8–12 civarına gelmiş olmalı



\\- çok iyi oynayan biraz öne gidebilir



\\- ama World 1’i bir saatte söküp bitirmemeli







\\---







\\## 12.2 1 saatin sonunda oyuncunun elinde ne olmalı?







\\- en az 1–2 silah yönü hakkında gerçek anlayış



\\- birkaç anlamlı equipment/modifier



\\- squad desteğinin ne işe yaradığını görmüş olmak



\\- bir mini-boss deneyimi



\\- armor / elite solve fikri



\\- bir sonraki girişte daha iyi build kurma isteği







\\### Kural



1 saat sonunda oyuncu:



\\- “ben hâlâ hiçbir şey anlamadım” dememeli



\\- “oyun bitti” de dememeli







\\---







\\## 13. Run içi ve run dışı ilerleme







\\## 13.1 Run içi ilerleme



Run sırasında oyuncu güçlenir:







\\- gate’ler



\\- reinforce



\\- sustain



\\- solve etkileri



\\- boss prep kararları







Bu geçicidir.







\\## 13.2 Run dışı ilerleme



Run sonrasında oyuncu şunları kazanabilir:







\\- yeni weapon archetype erişimi



\\- yeni equipment seçenekleri



\\- support preset / squad bias açılımı



\\- yeni stage erişimi



\\- küçük kalıcı gelişim







\\### Çok önemli kural



Run dışı progression:



\\- oyunu ezdirecek dev stat şişirmesi olmamalı



\\- daha çok \\\*\\\*yeni seçenek\\\*\\\* açmalı







\\---







\\## 14. Şans sistemi







Şans oyuncuyu eğlendirmeli ama kapana kıstırmamalı.







\\### Kural



\\- tamamen çöp gate dizilimi olmamalı



\\- her birkaç seçimde en az bir güvenli/nötr yol olmalı



\\- solve ihtiyacı olan stage’de solve kapıları mantıklı sıklıkta gelmeli



\\- aşırı şanslı oyuncu parlayabilir



\\- aşırı şanssız oyuncu “run öldü” dememeli







\\### Bunun için



Gate pool sistemi:



\\- family dağılımı



\\- stage solve focus



\\- tekrar koruması



\\- güvenli seçenek oranı



kullanır







\\---







\\## 15. Reward Band sistemi







Her stage yalnızca zorluk değil, ödül bandı da taşır.







\\### Reward Band türleri



\\- `Light`



\\- `Standard`



\\- `Spike`



\\- `BossPrep`



\\- `Boss`







\\### Amaç



Oyuncu:



\\- niye ilerlediğini



\\- niye zorlandığını



\\- ne kazandığını



hisseder







\\### Kural



Zor stage’in ödülü görünür olmalı.  



Ama ödül ekonomiyi patlatmamalı.







\\---







\\## 16. Basit otomasyon formu







Bu sistemin generator mantığı sade tutulur.







\\### Girdi



\\- stageBand



\\- solveFocus



\\- expectedPowerBand



\\- expectedSurvivabilityBand



\\- tempoTemplate



\\- packetPool







\\### Çıktı



\\- packet sayısı



\\- packet tür dağılımı



\\- toplam clear budget



\\- toplam pressure budget



\\- relief noktaları



\\- reward band







\\---







\\## 17. Oyuncunun hissedeceği sonuç







Oyuncu matematiği görmez.  



Ama şunu hisseder:







\\- oyun boş değil



\\- düşmanlar aynı ritimde akmıyor



\\- bazen rahatlıyorum, bazen baskı geliyor



\\- build seçimim fark yaratıyor



\\- yanlış solve ile uzuyorum



\\- doğru solve ile akıyorum



\\- bir sonraki stage için daha hazırlıklı olmak istiyorum







Bu, bu modelin asıl hedefidir.







\\---







\\## 18. Uygulama için sade veri sözleşmesi







\\## EnemyArchetype



\\- clearCost



\\- pressureCost



\\- solveTags



\\- role



\\- spawnWeightBand







\\## Packet



\\- packetType



\\- enemyList



\\- packetClearCost



\\- packetPressureCost



\\- solveTags







\\## StageRecipe



\\- stageBand



\\- solveFocus



\\- tempoTemplate



\\- targetClearBudget



\\- targetPressureBudget



\\- toleranceBand



\\- rewardBand



\\- packetCount







Bu kadar.  



İlk sürüm için yeterlidir.







\\---







\\## 19. Riskler







\\### Risk 1



Sistem arkada iyi ama önde çok açıklayıcı olursa oyuncu yorulur.







\\### Risk 2



Pressure budget fazla sert kurulursa oyun sürekli kaçış oyununa döner.







\\### Risk 3



Tolerance çok yüksek olursa oyun boş koşu simülatörü olur.







\\### Risk 4



Solve focus çok sert olursa tek doğru build meta oluşur.







\\### Risk 5



Reward pacing zayıf olursa ilerleme hissi ölür.







\\---







\\## 20. Son karar özeti







World 1 Progression \\\& Budget Model v1:



\\- oyuncuya görünürde sade kalır



\\- arkada güçlü matematik taşır



\\- stage’leri tek tek elle yazma ihtiyacını azaltır



\\- packet library + tempo template + stage recipe ile otomasyona yaklaşır



\\- oyuncuyu sürekli kaçmaya zorlamaz



\\- ama boş koşu simülatörüne de dönmez



\\- 1 saat sonunda anlamlı ilerleme ve öğrenme hissi vermeyi hedefler



\\- run dışı progression’ı seçenek açan yapıda tutar



\\- şansı eğlenceli ama adil kullanır



\-------------

\\# Top End War — Enemy Cost Table v1 + Packet Library v1



\\\_Hidden tuning for strong game feel\\\_







\\---







\\## 0. Belgenin amacı







Bu belge iki temel şeyi tanımlar:







1\\. \\\*\\\*Enemy Cost Table v1\\\*\\\*  



\&#x20;  Her düşmanın görünmez tuning değerleri:



\&#x20;  - ne kadar zor temizlenir



\&#x20;  - ne kadar baskı kurar



\&#x20;  - hangi solve türünü çağırır







2\\. \\\*\\\*Packet Library v1\\\*\\\*  



\&#x20;  Düşmanları tek tek değil, \\\*\\\*anlamlı paketler\\\*\\\* halinde üretmek için kullanılacak temel kütüphane







\\### Temel ilke



Oyuncu bunları \\\*\\\*asla sayısal olarak görmez\\\*\\\*.  



Oyuncu sadece şunu hisseder:



\\- dalgalar farklı geliyor



\\- düşmanlar boş değil



\\- bazı anlar rahat



\\- bazı anlar baskılı



\\- build seçimim fark yaratıyor







\\---







\\## 1. Enemy Cost sistemi







\\## 1.1 İki ana metrik







\\### Clear Cost



Bu düşmanı öldürmenin maliyeti.







Etkileyen şeyler:



\\- effective HP



\\- armor



\\- vurulma zorluğu



\\- solve gereksinimi



\\- hedef önceliği







\\### Pressure Cost



Bu düşmanı görmezden gelirsen ne kadar baskı kurar?







Etkileyen şeyler:



\\- ne kadar hızlı yaklaşır



\\- oyuncuyu ne kadar zor karar vermeye iter



\\- temas riski



\\- temas cezası



\\- lane/alan baskısı



\\- panic factor







\\---







\\## 1.2 Çok önemli not



Pressure Cost bir damage type değildir.  



Bu:



\\- tasarım/tuning metriğidir



\\- spawn generator’ın kullandığı görünmez puandır







Yani oyun içinde “pressure = 8” diye bir şey görünmez.







\\---







\\## 2. Enemy Cost Table v1







\\## 2.1 Değer ölçeği



Başlangıç ölçeği:



\\- `1–3` = düşük



\\- `4–6` = orta



\\- `7–8` = yüksek



\\- `9+` = çok yüksek







Bu ilk versiyon için yeterli.







\\---







\\## 2.2 Ana tablo







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







\\---







\\## 2.3 Açıklamalar







\\### Trooper



\\- düşük clear



\\- düşük pressure



\\- her şeyin referansı







\\### Swarm



\\- tekil tehdit düşük



\\- ama paket halinde \\\*\\\*çarpanlı pressure\\\*\\\* üretir







\\### Charger



\\- öldürmesi çok pahalı değil



\\- ama hayatta kalırsa baskısı çok yüksek







\\### Armored Brute



\\- solve yoksa aşırı yavaş temizlenir



\\- baskısı charger kadar ani değil







\\### Elite Charger



\\- hem öncelik hem panic



\\- oyuncuya “şimdi karar ver” dedirtir







\\---







\\## 2.4 Swarm için özel kural







Swarm’ın pressure değeri tek başına düşük tutulur.  



Ama packet içinde şu bonus uygulanır:







\\### Swarm Pack Bonus



\\- 2–3 Swarm birlikte → `+1 packet pressure`



\\- 4–5 Swarm birlikte → `+2 packet pressure`



\\- 6+ Swarm birlikte → `+3 packet pressure`







\\### Neden?



Tekil swarm zayıf hissetmeli.  



Ama topluca lane baskısı kurmalı.







\\---







\\## 2.5 Solve Tags







Her düşman şu solve tag’lerinden 1 veya daha fazlasını taşır:







\\- `mixed`



\\- `swarm`



\\- `priority`



\\- `armor`



\\- `elite`



\\- `long\\\_fight`



\\- `boss\\\_prep`







\\### Örnek



\\- Trooper → `mixed`



\\- Swarm → `swarm`



\\- Charger → `priority`



\\- Brute → `armor`



\\- Elite Charger → `elite`, `priority`



\\- War Machine → `long\\\_fight`



\\- Final Boss → `boss\\\_prep`, `mixed`







\\---







\\## 3. Packet Library v1







Biz bölüm yazmıyoruz.  



Biz \\\*\\\*packet\\\*\\\* yazıyoruz.







\\### Packet ne?



Bir packet:



\\- kısa bir düşman grubu



\\- tek bir karar



\\- tek bir hissiyat



\\- küçük bir tempo anı







\\---







\\## 3.1 Packet alanları







Her packet şu alanları taşır:







\\- `packetId`



\\- `packetType`



\\- `enemyComposition`



\\- `solveTags`



\\- `packetClearCost`



\\- `packetPressureCost`



\\- `entryStyle`



\\- `preferredStageBands`



\\- `reliefWeight`



\\- `spikeWeight`







\\---







\\## 4. Packet türleri







\\## 4.1 Baseline Packet







\\### İçerik



\\- 2–4 Trooper







\\### Amaç



\\- baseline akış



\\- oyuncuya nefes aldırmayan ama yormayan doluluk



\\- silahın çalıştığını hissettirmek







\\### Cost



\\- Clear: 4–8



\\- Pressure: 4–6







\\### Kullanım



\\- her bandda olabilir



\\- özellikle giriş packet’i olarak iyi







\\---







\\## 4.2 Dense Swarm Packet







\\### İçerik



\\- 1 Trooper



\\- 2–5 Swarm







\\### Amaç



\\- lane baskısı



\\- tempo testi



\\- SMG / Launcher / Shotgun alanı açmak







\\### Cost



\\- Clear: 4–7



\\- Pressure: 5–8







\\### Kullanım



\\- 1–5’te küçük



\\- 6–20’de orta



\\- 20+ bandlarda mixed destek







\\---







\\## 4.3 Delayed Charger Packet







\\### İçerik



\\- önce 2–3 Trooper



\\- kısa gecikmeyle 1 Charger







\\### Amaç



\\- öncelik kararı



\\- “önce bunu çöz” anı yaratmak







\\### Cost



\\- Clear: 7–9



\\- Pressure: 8–11







\\### Kullanım



\\- 1–5’te çok hafif



\\- 6–15’te öğretim



\\- sonra mixed pressure içinde







\\---







\\## 4.4 Armor Check Packet







\\### İçerik



\\- 1 Armored Brute



\\- yanında 1–2 Trooper







\\### Amaç



\\- armor solve gereksinimi



\\- sniper/breacher değerini hissettirmek







\\### Cost



\\- Clear: 8–11



\\- Pressure: 6–8







\\### Kullanım



\\- 6–10 tanıtım



\\- 11+ varyantlı kullanım







\\---







\\## 4.5 Guarded Core Packet







\\### İçerik



\\- merkezde 1 Brute veya 1 Elite



\\- yanında 2–3 support düşman







\\### Amaç



\\- “önce neyi vuracağım?”



\\- mixed solve



\\- build ayrıştırma







\\### Cost



\\- Clear: 10–14



\\- Pressure: 8–11







\\### Kullanım



\\- 11+ sonrası daha iyi



\\- specialisation bandında güçlü







\\---







\\## 4.6 Split Pressure Packet







\\### İçerik



\\- sağ/sola dağılmış küçük gruplar



\\- merkezde hafif core







\\### Amaç



\\- düz çizgi hissini kırmak



\\- sadece orta çizgiye kilitli oyunu bozmak







\\### Cost



\\- Clear: 6–10



\\- Pressure: 6–9







\\### Kullanım



\\- 11+ daha uygun



\\- launcher/smg/assault farkını gösterir







\\---







\\## 4.7 Elite Spike Packet







\\### İçerik



\\- 1 Elite Charger



\\- yanında 1–2 support düşman







\\### Amaç



\\- panic + hedef önceliği



\\- sniper/beam/elite solve alanı







\\### Cost



\\- Clear: 9–12



\\- Pressure: 10–13







\\### Kullanım



\\- 6–10 sonrası kontrollü



\\- 20+ bandlarda daha güçlü







\\---







\\## 4.8 Relief Packet







\\### İçerik



\\- düşük tehditli kısa grup



\\- genelde 1–2 Trooper veya çok hafif swarm







\\### Amaç



\\- oyuncuya nefes



\\- build gücünü hissettirme



\\- tempo düz çizgi olmasın







\\### Cost



\\- Clear: 2–4



\\- Pressure: 2–3







\\### Kullanım



\\- her bandda var



\\- ama çok sık değil







\\---







\\## 4.9 Boss Prep Packet







\\### İçerik



\\- armor + elite + long-fight hisli küçük kombinasyon







\\### Amaç



\\- boss öncesi son uyarı



\\- beam/sniper/breacher değerini hissettirme







\\### Cost



\\- Clear: 10–15



\\- Pressure: 8–12







\\### Kullanım



\\- 31–34 bandı







\\---







\\## 5. Entry Style sistemi







Packet’ler sadece içerikle değil, giriş şekliyle de farklı hissettirir.







\\### Entry Style tipleri



\\- `straight`



\\- `staggered`



\\- `split`



\\- `dense\\\_core`



\\- `delayed\\\_spike`



\\- `guarded`







\\### Kural



Aynı enemy, farklı entry style ile farklı his verebilir.  



Bu sayede tek tek yeni enemy üretmeden çeşitlilik artar.







\\---







\\## 6. Packet seçme kuralları







\\### Kural 1



Aynı packet tipi üst üste 2’den fazla gelmez.







\\### Kural 2



Her 3–4 packet’te bir relief veya low-pressure an olur.







\\### Kural 3



Yeni solve tanıtıldıysa, hemen arkasından onu okuma fırsatı veren daha temiz packet gelir.







\\### Kural 4



Packet dizisi düz çizgi olmaz:



\\- baseline



\\- pressure



\\- relief



\\- spike



\\- relief



\\- mixed







gibi dalgalı akar.







\\### Kural 5



Aynı solve tag üst üste çok tekrar edilmez, stage focus dışına taşmaz.







\\---







\\## 7. Tempo ve packet ilişkisi







Stage tempo template’i packet’leri seçer.







\\### Örnek: Intro Template



\\- Baseline



\\- Baseline



\\- DenseSwarm



\\- Relief



\\- DelayedCharger







\\### Örnek: Discovery Template



\\- Baseline



\\- ArmorCheck



\\- Relief



\\- DelayedCharger



\\- Baseline







\\### Örnek: Pressure Template



\\- DenseSwarm



\\- SplitPressure



\\- Relief



\\- DelayedCharger



\\- GuardedCore







\\---







\\## 8. World 1 bandlarına göre packet kullanımı







\\## 1–5 Tutorial Core



Ağırlık:



\\- Baseline



\\- küçük DenseSwarm



\\- hafif DelayedCharger



\\- bol Relief







\\### Yasak



\\- ağır GuardedCore



\\- yoğun EliteSpike



\\- çok sert ArmorCheck zinciri







\\---







\\## 6–10 Build Discovery



Ağırlık:



\\- ArmorCheck



\\- DelayedCharger



\\- küçük EliteSpike



\\- Relief







\\### Amaç



\\- solve kavramını öğretmek







\\---







\\## 11–20 First Friction / Controlled Complexity



Ağırlık:



\\- SplitPressure



\\- GuardedCore



\\- DenseSwarm



\\- Geometry’yi parlatan packet’ler







\\### Amaç



\\- build farkı hissettirmek







\\---







\\## 21–30 Specialization / Pressure



Ağırlık:



\\- GuardedCore



\\- EliteSpike



\\- Mixed Pressure



\\- daha az Relief







\\### Amaç



\\- yanlış solve = hissedilen ceza



\\- doğru build = hissedilen rahatlık







\\---







\\## 31–34 Final Prep



Ağırlık:



\\- BossPrepPacket



\\- EliteSpike



\\- ArmorCheck



\\- kontrollü Relief







\\### Amaç



\\- final boss öncesi zihinsel hazırlık







\\---







\\## 9. İlk otomasyon kuralı







Stage generator şu mantıkla packet dizer:







1\\. Stage solve focus’u seçilir  



2\\. Tempo template seçilir  



3\\. Hedef clear budget ve pressure budget alınır  



4\\. Uygun packet havuzundan seçim yapılır  



5\\. Relief ve spike dengesi korunur  



6\\. Toplam packet maliyeti stage bandına oturtulur  







\\### Sonuç



Tek tek:



\\- “buraya 2 düşman”



\\- “buraya 3 düşman”



yazmak zorunda kalmazsın







\\---







\\## 10. Görünür oyun hissi hedefleri







Bu kütüphane sayesinde oyuncu şunları hissetmeli:







\\- düşmanlar hep aynı ritimde gelmiyor



\\- bazen nefes alıyorum



\\- bazen baskı artıyor



\\- aynı düşman farklı şekilde tehdit olabiliyor



\\- build’im bazı packet’lerde gerçekten parlıyor



\\- oyun boş değil



\\- oyun boğucu da değil







\\---







\\## 11. Kısa örnek







\\### Stage 3 — Intro / Mixed Light



Template:



\\- Baseline



\\- DenseSwarm



\\- Relief



\\- DelayedCharger



\\- Relief







\\### Stage 8 — Armor Discovery



Template:



\\- Baseline



\\- ArmorCheck



\\- Relief



\\- DelayedCharger



\\- Baseline



\\- MiniBossPrep







\\### Stage 18 — Geometry / Mixed



Template:



\\- DenseSwarm



\\- SplitPressure



\\- GuardedCore



\\- Relief



\\- DelayedCharger



\\- DenseSwarm







Bunlar doğrudan stage recipe’ye bağlanabilir.







\\---







\\## 12. Son karar özeti







Enemy Cost Table v1 + Packet Library v1:



\\- stage’leri tek tek el yapımı yazma ihtiyacını azaltır



\\- düşman hissini çeşitlendirir



\\- oyun temposunu matematikle ama görünmez şekilde kontrol eder



\\- boş koşu simülatörü hissini engeller



\\- sürekli kaçış oyununa da dönüştürmez



\\- build, enemy ve stage solve ilişkisini sahada görünür kılar



\-------------------------



\\# Top End War — Tempo Templates v1 + Stage Recipe Format v1



\\\_From packet library to stage generation\\\_







\\---







\\## 0. Belgenin amacı







Bu belge, mevcut:



\\- Enemy Cost Table



\\- Packet Library



\\- World 1 Progression \\\& Budget Model







üzerine, stage üretimini otomasyona yaklaştıracak iki üst katmanı kurar:







1\\. \\\*\\\*Tempo Templates v1\\\*\\\*  



\&#x20;  Packet’lerin hangi sırayla ve hangi nabızla akacağını tanımlar.







2\\. \\\*\\\*Stage Recipe Format v1\\\*\\\*  



\&#x20;  Her stage’in tek tek el yapımı değil, tanımlı parametrelerle üretilmesini sağlar.







\\### Temel ilke



Biz her stage’i tek tek bestelemiyoruz.  



Biz:



\\- packet kütüphanesi



\\- tempo şablonları



\\- recipe formatı







kuruyoruz.







\\---







\\## 1. Tempo Template nedir?







Tempo template:



\\- bir stage’in nabzıdır



\\- packet’lerin sırasını belirler



\\- relief / pressure / spike dengesini kurar



\\- oyuncunun ne zaman rahatlayacağını ve ne zaman karar vereceğini belirler







\\### Oyuncunun hissettiği şey



Oyuncu template’i görmez.  



Oyuncu şunu hisseder:



\\- oyun düz değil



\\- bazen sakin



\\- bazen baskılı



\\- bazen ani tehdit var



\\- ama kaotik değil







\\---







\\## 2. Tempo Template’in yapı taşları







Her template şu anları kullanır:







\\### Warmup



\\- stage’e giriş



\\- baseline okuma



\\- oyuncunun ritmi anlaması







\\### Build



\\- baskının yavaş artışı



\\- packet yoğunluğu büyür







\\### Spike



\\- anlık karar isteyen tehdit



\\- charger / elite / guarded core gibi







\\### Relief



\\- kısa nefes anı



\\- build gücünü hissetme







\\### Resolve



\\- solve gerektiren net soru



\\- armor / elite / swarm / boss prep







\\### Exit / Lead-out



\\- mini-boss veya sonraki hissi hazırlayan son geçiş







\\---







\\## 3. Tempo Templates v1







\\## 3.1 Intro Template



\\### Amaç



\\- oyunu öğretmek



\\- korkutmadan akıtmak



\\- baseline hissi vermek







\\### Akış



\\- Warmup



\\- Warmup



\\- Build



\\- Relief



\\- Light Spike







\\### Uygun packet’ler



\\- Baseline



\\- DenseSwarm (küçük)



\\- Relief



\\- DelayedCharger (çok hafif)







\\### Kullanım



\\- World 1 Stage 1–3







\\---







\\## 3.2 Guided Discovery Template



\\### Amaç



\\- yeni solve fikrini öğretmek



\\- ama oyuncuyu ezmemek







\\### Akış



\\- Warmup



\\- Resolve



\\- Relief



\\- Build



\\- Light Spike



\\- Relief







\\### Uygun packet’ler



\\- Baseline



\\- ArmorCheck



\\- Relief



\\- DelayedCharger



\\- küçük DenseSwarm







\\### Kullanım



\\- Stage 4–8



\\- ilk armor / elite / priority öğretimi







\\---







\\## 3.3 Pressure Wave Template



\\### Amaç



\\- oyuncuyu akış içinde baskılamak



\\- boş koşu hissini kırmak







\\### Akış



\\- Warmup



\\- Build



\\- Build



\\- Spike



\\- Relief



\\- Build







\\### Uygun packet’ler



\\- DenseSwarm



\\- SplitPressure



\\- DelayedCharger



\\- GuardedCore



\\- Relief







\\### Kullanım



\\- Stage 8–15



\\- swarm ve tempo odaklı alanlar







\\---







\\## 3.4 Mixed Test Template



\\### Amaç



\\- build farkını görünür kılmak



\\- farklı solve’ların aynı stage içinde anlam kazanması







\\### Akış



\\- Warmup



\\- Resolve



\\- Build



\\- Relief



\\- Spike



\\- Resolve







\\### Uygun packet’ler



\\- Baseline



\\- ArmorCheck



\\- GuardedCore



\\- DelayedCharger



\\- DenseSwarm



\\- SplitPressure







\\### Kullanım



\\- Stage 10–20







\\---







\\## 3.5 Specialization Template



\\### Amaç



\\- oyuncunun build yönünün fark yaratmasını sağlamak



\\- yanlış cevapları hafif cezalandırmak







\\### Akış



\\- Build



\\- Resolve



\\- Spike



\\- Relief



\\- Resolve



\\- Build







\\### Uygun packet’ler



\\- GuardedCore



\\- ArmorCheck



\\- EliteSpike



\\- DenseSwarm



\\- Relief







\\### Kullanım



\\- Stage 18–28







\\---







\\## 3.6 Prep Template



\\### Amaç



\\- yaklaşan boss/mini-boss mantığını öğretmek



\\- beam/sniper/elite solve’u öne çıkarmak







\\### Akış



\\- Resolve



\\- Relief



\\- Spike



\\- Build



\\- Boss Prep



\\- Relief







\\### Uygun packet’ler



\\- ArmorCheck



\\- EliteSpike



\\- BossPrepPacket



\\- kısa Relief







\\### Kullanım



\\- Stage 28–34







\\---







\\## 3.7 MiniBoss Lead-In Template



\\### Amaç



\\- mini-boss öncesi oyuncuyu hazırlamak



\\- aşırı yormadan sınav hissi oluşturmak







\\### Akış



\\- Warmup



\\- Resolve



\\- Relief



\\- Spike



\\- Lead-in







\\### Uygun packet’ler



\\- Baseline



\\- ArmorCheck



\\- DelayedCharger



\\- küçük EliteSpike



\\- Relief







\\### Kullanım



\\- mini-boss öncesi son normal stage







\\---







\\## 4. Template kullanım kuralları







\\### Kural 1



Aynı template çok uzun seri halinde kullanılmaz.







\\### Kural 2



Aynı solve focus taşıyan template’ler art arda gelirse packet dağılımı değişir.







\\### Kural 3



Her template içinde en az 1 relief anı olur.







\\### Kural 4



Spike packet’ler erken oyunda daha hafif, geç oyunda daha sert olur.







\\### Kural 5



Template stage’in ana sorusunu destekler, stage’i tamamen belirlemez.







\\---







\\## 5. Stage Recipe Format v1







Her stage şu parametrelerle tanımlanır:







\\### Kimlik



\\- `stageId`



\\- `worldId`



\\- `stageBand`







\\### Ana tasarım



\\- `solveFocus`



\\- `tempoTemplate`



\\- `primaryThreats`



\\- `secondaryThreats`







\\### Bütçe



\\- `targetClearBudget`



\\- `targetPressureBudget`



\\- `toleranceBand`







\\### Yapı



\\- `packetCount`



\\- `allowedPacketPool`



\\- `reliefFrequency`



\\- `spikeIntensity`







\\### Ödül



\\- `rewardBand`



\\- `unlockHint`



\\- `buildSnapshotHint`







\\---







\\## 6. Stage Recipe alan açıklamaları







\\## 6.1 solveFocus



Stage’in ana sorduğu soru.







Örnek:



\\- `mixed`



\\- `swarm`



\\- `armor`



\\- `elite`



\\- `boss\\\_prep`



\\- `long\\\_fight`







\\---







\\## 6.2 tempoTemplate



Stage’in nabzını belirler.







Örnek:



\\- `intro`



\\- `guided\\\_discovery`



\\- `pressure\\\_wave`



\\- `mixed\\\_test`



\\- `specialization`



\\- `prep`



\\- `miniboss\\\_lead\\\_in`







\\---







\\## 6.3 primaryThreats



Bu stage’in en görünür tehditleri.







Örnek:



\\- Trooper + Swarm



\\- Charger + Trooper



\\- Brute + Support



\\- Elite + Armor







Oyuncuya doğrudan bu listede gösterilmez, ama stage tag’lerini besler.







\\---







\\## 6.4 targetClearBudget



Stage’in toplam temizleme maliyeti.







Bu:



\\- oyuncunun beklenen run gücüne göre belirlenir



\\- çok düşükse boş



\\- çok yüksekse boğucu olur







\\---







\\## 6.5 targetPressureBudget



Stage’in toplam baskı maliyeti.







Bu:



\\- oyuncuya sürekli kaçma zorunluluğu getirmeyecek



\\- ama tehdit hissini canlı tutacak







\\---







\\## 6.6 toleranceBand



Oyuncunun ne kadar hata yapabileceği.







Örnek:



\\- `High`



\\- `Medium`



\\- `Low`







\\### World 1 yorumu



\\- 1–5 → High



\\- 6–15 → Medium-High



\\- 16–25 → Medium



\\- 26–34 → Medium-Low



\\- 35 → boss özel







\\---







\\## 6.7 packetCount



Stage’in kaç packet’ten oluşacağı.







İlk sürüm için öneri:



\\- kısa stage: `5–6`



\\- standart stage: `6–8`



\\- yüksek baskı stage: `7–9`



\\- prep stage: `5–7`







\\---







\\## 6.8 allowedPacketPool



Bu stage’in kullanabileceği packet tipleri.







Örnek:



```text



\\\[Baseline, DenseSwarm, Relief, DelayedCharger]







Bu sayede erken stage’e yanlış packet karışmaz.







6.9 reliefFrequency







Stage’in ne sıklıkla nefes verdiği.







Örnek:







every\\\_2\\\_packets



every\\\_3\\\_packets



light\\\_relief\\\_only



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







7\\. Örnek Stage Recipe’ler



Stage 1



stageId: W1-01



stageBand: tutorial\\\_core



solveFocus: mixed



tempoTemplate: intro



primaryThreats: trooper



secondaryThreats: swarm\\\_light



targetClearBudget: low



targetPressureBudget: low



toleranceBand: high



packetCount: 5



allowedPacketPool: \\\[Baseline, DenseSwarm, Relief, DelayedCharger\\\_Light]



reliefFrequency: every\\\_2\\\_packets



spikeIntensity: low



rewardBand: light



buildSnapshotHint: balanced



Stage 7



stageId: W1-07



stageBand: build\\\_discovery



solveFocus: armor



tempoTemplate: guided\\\_discovery



primaryThreats: armored\\\_brute



secondaryThreats: trooper, charger



targetClearBudget: medium



targetPressureBudget: medium



toleranceBand: medium\\\_high



packetCount: 6



allowedPacketPool: \\\[Baseline, ArmorCheck, Relief, DelayedCharger, DenseSwarm\\\_Light]



reliefFrequency: every\\\_3\\\_packets



spikeIntensity: low\\\_medium



rewardBand: standard



buildSnapshotHint: armor\\\_break



Stage 18



stageId: W1-18



stageBand: controlled\\\_complexity



solveFocus: mixed



tempoTemplate: mixed\\\_test



primaryThreats: swarm, charger



secondaryThreats: brute



targetClearBudget: medium\\\_high



targetPressureBudget: medium\\\_high



toleranceBand: medium



packetCount: 7



allowedPacketPool: \\\[DenseSwarm, SplitPressure, ArmorCheck, Relief, DelayedCharger, GuardedCore]



reliefFrequency: every\\\_3\\\_packets



spikeIntensity: medium



rewardBand: standard



buildSnapshotHint: mixed\\\_pressure



Stage 32



stageId: W1-32



stageBand: final\\\_prep



solveFocus: boss\\\_prep



tempoTemplate: prep



primaryThreats: elite, armor



secondaryThreats: long\\\_fight



targetClearBudget: high



targetPressureBudget: medium\\\_high



toleranceBand: medium\\\_low



packetCount: 6



allowedPacketPool: \\\[ArmorCheck, EliteSpike, Relief, BossPrepPacket, GuardedCore\\\_Light]



reliefFrequency: every\\\_3\\\_packets



spikeIntensity: medium\\\_high



rewardBand: prep



buildSnapshotHint: boss\\\_prep



8\\. Stage üretim kuralları



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







9\\. World 1 için önerilen template dağılımı



1–3



intro



4–8



guided\\\_discovery



9–15



pressure\\\_wave



mixed\\\_test



16–22



mixed\\\_test



specialization



23–30



specialization



pressure\\\_wave



31–34



prep



miniboss\\\_lead\\\_in



35



boss özel



10\\. Görünürde oyuncuya ne yansır?







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







11\\. Gelecekte otomasyon







Bu yapı daha sonra rahatça:







packet generator



stage builder



pool selector



threat scaler







sistemlerine bağlanabilir.







Yani bugünden kurduğumuz şey, yarınki üretim hızını arttırır.







12\\. Son karar özeti







Tempo Templates v1 + Stage Recipe Format v1:







stage’leri tek tek el yapımı yazma ihtiyacını azaltır



packet library’yi üretime bağlar



zorluk, relief ve pressure dengesini otomasyona yaklaştırır



oyuncuya arkadaki matematiği göstermeden güçlü oyun hissi üretir



World 1’i daha düzenli, daha kontrollü ve daha ölçeklenebilir hale getirir



\-------------------



\\# Top End War — World 1 Stage Band Table v1



\\\_The canonical backbone of World 1\\\_







\\---







\\## 0. Belgenin amacı







Bu belge, World 1’i tek tek stage yazmadan yönetebilmek için



\\\*\\\*ana bantlara bölünmüş kanonik tabloyu\\\*\\\* tanımlar.







Bu tablo şunları kilitler:



\\- her bantta oyuncuya ne öğretildiği



\\- hangi solve focus’ların öne çıktığı



\\- hangi tempo template’lerin kullanılacağı



\\- hangi reward band’in baskın olduğu



\\- oyuncunun o band sonunda ne hissetmesi ve ne öğrenmesi gerektiği



\\- hangi build / gate / squad fikirlerinin açılması gerektiği







\\### Kural



Bu tablo oyuncuya gösterilmez.  



Bu tablo:



\\- tasarım rehberi



\\- tuning rehberi



\\- stage generator rehberi



olarak kullanılır.







\\---







\\## 1. World 1’in ana bantları







World 1 toplamda 8 banda ayrılır:







1\\. Tutorial Core



2\\. Build Discovery



3\\. First Friction



4\\. Controlled Complexity



5\\. Specialization



6\\. Pressure \\\& Punishment



7\\. Final Prep



8\\. Final Boss







\\---







\\## 2. Kanonik Stage Band Tablosu







| Band | Stage Aralığı | Ana Amaç | Baskın Solve Focus | Baskın Tempo Template | Reward Band | Tolerance | Oyuncu Hissi |



|---|---|---|---|---|---|---|---|



| Tutorial Core | 1–5 | Oyunu öğretmek | mixed / swarm\\\_light | intro | light | high | güvenli öğrenme |



| Build Discovery | 6–10 | build ve solve fikrini açmak | armor / priority / elite\\\_light | guided\\\_discovery | standard | medium\\\_high | “doğru seçim fark ediyor” |



| First Friction | 11–15 | güvenli build her şeyi çözmez | close\\\_burst / armor / mixed | pressure\\\_wave | standard | medium | “artık dikkat etmeliyim” |



| Controlled Complexity | 16–20 | farklı packet hislerini açmak | swarm / geometry / mixed | mixed\\\_test | standard | medium | “oyun genişliyor” |



| Specialization | 21–25 | build yönünü netleştirmek | armor / elite / army / geometry | specialization | spike | medium | “benim build’im oluşuyor” |



| Pressure \\\& Punishment | 26–30 | yanlış solve’u hissettirmek | mixed / elite / priority / long\\\_fight | pressure\\\_wave + specialization | spike | medium\\\_low | “yanlış kurarsam zorlanırım” |



| Final Prep | 31–34 | boss öncesi hazırlık | boss\\\_prep / armor / elite / long\\\_fight | prep + miniboss\\\_lead\\\_in | prep | medium\\\_low | “bir sınav geliyor” |



| Final Boss | 35 | World 1 sentezi | mixed / boss\\\_prep | boss\\\_special | boss | boss\\\_special | “öğrendiklerimin sınavı” |







\\---







\\## 3. Bant bazlı detaylar







\\---







\\## 3.1 Tutorial Core



\\### Stage aralığı



1–5







\\### Ana amaç



\\- hareketi öğretmek



\\- auto-shoot mantığını öğretmek



\\- gate okuma hissini başlatmak



\\- ilk düşman farklılıklarını tanıtmak



\\- oyuncuya “bu oyun akıyor” dedirtmek







\\### Baskın solve focus



\\- `mixed`



\\- `swarm\\\_light`







\\### Kullanılacak enemy ağırlığı



\\- Trooper baskın



\\- hafif Swarm



\\- çok hafif Charger preview







\\### Kullanılacak packet türleri



\\- Baseline Packet



\\- küçük Dense Swarm Packet



\\- Relief Packet



\\- çok hafif Delayed Charger Packet







\\### Baskın tempo template



\\- `intro`







\\### Reward band



\\- `light`







\\### Tolerance



\\- `high`







\\### Oyuncu hissi



\\- güvenli öğrenme



\\- baskı var ama bunaltmıyor



\\- birkaç hata affediliyor







\\### Bu bant sonunda oyuncu



\\- gate okumanın mantığını anlamış olmalı



\\- Assault / SMG farkını hissetmiş olmalı



\\- squad desteğinin varlığını fark etmiş olmalı







\\---







\\## 3.2 Build Discovery



\\### Stage aralığı



6–10







\\### Ana amaç



\\- build yalnızca düz hasar değildir fikrini açmak



\\- armor solve’u tanıtmak



\\- priority target fikrini başlatmak



\\- sniper’ın neden var olduğunu hissettirmek



\\- ilk mini-boss mantığını hazırlamak







\\### Baskın solve focus



\\- `armor`



\\- `priority`



\\- `elite\\\_light`







\\### Kullanılacak enemy ağırlığı



\\- Trooper



\\- Charger



\\- ilk Armored Brute



\\- çok kontrollü Elite baskısı







\\### Kullanılacak packet türleri



\\- Armor Check Packet



\\- Delayed Charger Packet



\\- Baseline Packet



\\- Relief Packet







\\### Baskın tempo template



\\- `guided\\\_discovery`







\\### Reward band



\\- `standard`







\\### Tolerance



\\- `medium\\\_high`







\\### Oyuncu hissi



\\- “her şeye aynı cevap vermek doğru değil”



\\- “doğru kapı gerçekten fark yaratıyor”







\\### Bu bant sonunda oyuncu



\\- armor solve mantığını öğrenmiş olmalı



\\- ilk gerçek yanlış seçim hissini yaşamış olmalı



\\- ilk mini-bossa zihinsel olarak hazır olmalı







\\---







\\## 3.3 First Friction



\\### Stage aralığı



11–15







\\### Ana amaç



\\- güvenli build’in artık tek başına yetmeyeceğini hissettirmek



\\- yakın risk / reward alanı açmak



\\- Shotgun / Mekanik support düşüncesine alan açmak



\\- pressure’ın görünür şekilde artmasını sağlamak







\\### Baskın solve focus



\\- `close\\\_burst`



\\- `armor`



\\- `mixed`







\\### Kullanılacak enemy ağırlığı



\\- Charger daha görünür



\\- Brute daha anlamlı



\\- Swarm artık daha organize packet’lerle gelir







\\### Kullanılacak packet türleri



\\- Pressure Wave Packet setleri



\\- Delayed Charger



\\- Armor Check



\\- küçük Guarded Core



\\- Relief







\\### Baskın tempo template



\\- `pressure\\\_wave`







\\### Reward band



\\- `standard`







\\### Tolerance



\\- `medium`







\\### Oyuncu hissi



\\- “daha dikkatli olmam lazım”



\\- “bazı buildler bazı anlarda daha iyi”







\\### Bu bant sonunda oyuncu



\\- düz tempo oyunu yerine karar anlarını fark etmeli



\\- sustain ve solve farkını hissetmeli



\\- yakın baskı kavramını anlamalı







\\---







\\## 3.4 Controlled Complexity



\\### Stage aralığı



16–20







\\### Ana amaç



\\- packet çeşitliliğini büyütmek



\\- geometry etkilerini açmak



\\- Launcher alanını hazırlamak



\\- aynı oyunun daha zengin hissedebileceğini göstermek







\\### Baskın solve focus



\\- `swarm`



\\- `geometry`



\\- `mixed`







\\### Kullanılacak enemy ağırlığı



\\- Swarm paketleri



\\- Split baskılar



\\- Brute destekli karma packet’ler







\\### Kullanılacak packet türleri



\\- Dense Swarm Packet



\\- Split Pressure Packet



\\- Guarded Core Packet



\\- Relief Packet







\\### Baskın tempo template



\\- `mixed\\\_test`







\\### Reward band



\\- `standard`







\\### Tolerance



\\- `medium`







\\### Oyuncu hissi



\\- “oyun genişliyor”



\\- “aynı düşmanlar bile farklı hissedebiliyor”







\\### Bu bant sonunda oyuncu



\\- packet mantığını sezgisel olarak hissetmeli



\\- geometry / lane baskısı farkını sezmelidir



\\- build tercihinin daha görünür olduğunu fark etmelidir







\\---







\\## 3.5 Specialization



\\### Stage aralığı



21–25







\\### Ana amaç



\\- oyuncunun build yönünü netleştirmek



\\- squad support katmanını daha görünür kılmak



\\- gate economy’nin gerçek değerini hissettirmek







\\### Baskın solve focus



\\- `armor`



\\- `elite`



\\- `army`



\\- `geometry`







\\### Kullanılacak enemy ağırlığı



\\- daha anlamlı Elite kullanımı



\\- Brute + support paketleri



\\- mixed packet’ler







\\### Kullanılacak packet türleri



\\- Guarded Core



\\- Elite Spike



\\- Armor Check



\\- Relief



\\- Dense Swarm varyantları







\\### Baskın tempo template



\\- `specialization`







\\### Reward band



\\- `spike`







\\### Tolerance



\\- `medium`







\\### Oyuncu hissi



\\- “benim build’im artık şekilleniyor”



\\- “seçimlerim daha görünür fark yaratıyor”







\\### Bu bant sonunda oyuncu



\\- kendi tercih ettiği ana build yönünü seçmiş olmalı



\\- yanlış gate / doğru gate farkını net hissetmeli



\\- support layer’ın build parçası olduğunu anlamalı







\\---







\\## 3.6 Pressure \\\& Punishment



\\### Stage aralığı



26–30







\\### Ana amaç



\\- yanlış solve’un artık görünür şekilde cezalandırılması



\\- ama oyunun tamamen unfair olmaması



\\- doğru build kuran oyuncunun gerçekten rahatlaması







\\### Baskın solve focus



\\- `mixed`



\\- `elite`



\\- `priority`



\\- `long\\\_fight`







\\### Kullanılacak enemy ağırlığı



\\- daha yoğun Charger baskısı



\\- Elite Spike’lar



\\- karma pressure paketleri



\\- Brute destekli yoğun sekanslar







\\### Kullanılacak packet türleri



\\- Pressure packet setleri



\\- Elite Spike



\\- Guarded Core



\\- Dense Swarm



\\- kısa Relief







\\### Baskın tempo template



\\- `pressure\\\_wave`



\\- `specialization`







\\### Reward band



\\- `spike`







\\### Tolerance



\\- `medium\\\_low`







\\### Oyuncu hissi



\\- “yanlış kurarsam zorlanırım”



\\- “doğru seçim yaptığımda rahatlıyorum”







\\### Bu bant sonunda oyuncu



\\- solve kavramını gerçek anlamda öğrenmiş olmalı



\\- build gücünü yalnızca sayı değil, doğru cevap olarak hissetmeli







\\---







\\## 3.7 Final Prep



\\### Stage aralığı



31–34







\\### Ana amaç



\\- final boss öncesi zihinsel ve mekanik hazırlık



\\- Beam’i tanıtmak



\\- Beam’in neden iyi olduğunu göstermek ama zorunlu kılmamak



\\- long-fight mantığını hissettirmek







\\### Baskın solve focus



\\- `boss\\\_prep`



\\- `armor`



\\- `elite`



\\- `long\\\_fight`







\\### Kullanılacak enemy ağırlığı



\\- elite + armor birlikteliği



\\- boss-benzeri küçük sınav packet’leri



\\- uzun süren hedefler







\\### Kullanılacak packet türleri



\\- Boss Prep Packet



\\- Armor Check Packet



\\- Elite Spike Packet



\\- Relief







\\### Baskın tempo template



\\- `prep`



\\- `miniboss\\\_lead\\\_in`







\\### Reward band



\\- `prep`







\\### Tolerance



\\- `medium\\\_low`







\\### Oyuncu hissi



\\- “bir sınav geliyor”



\\- “şimdi doğru hazırlanmalıyım”







\\### Bu bant sonunda oyuncu



\\- boss öncesi hazırlık kavramını anlamış olmalı



\\- Beam’i bir seçenek olarak görmüş olmalı



\\- final için build’ini bilinçli seçmelidir







\\---







\\## 3.8 Final Boss



\\### Stage aralığı



35







\\### Ana amaç



\\- World 1 boyunca öğretilen her şeyi sınamak



\\- yeni sistem tanıtmamak



\\- build / gate / solve / squad / tempo bilgisinin sentezini istemek







\\### Baskın solve focus



\\- `mixed`



\\- `boss\\\_prep`







\\### Baskın tempo



\\- `boss\\\_special`







\\### Reward band



\\- `boss`







\\### Tolerance



\\- `boss\\\_special`







\\### Oyuncu hissi



\\- “öğrendiklerimin sınavı”







\\### Bu bant sonunda oyuncu



\\- World 1’i gerçekten anlamış hisseder



\\- World 2 veya sonraki içerik için doğal motivasyon kazanır







\\---







\\## 4. Bant geçiş kuralları







\\### Kural 1



Bir banttan diğerine geçişte:



\\- solve focus biraz kayar



\\- ama tamamen kopmaz







\\### Kural 2



Template değişir ama eski template tamamen kaybolmaz.







\\### Kural 3



Reward band büyüdükçe risk de görünür artar.







\\### Kural 4



Tolerance bir anda sert düşmez; bant bant azalır.







\\### Kural 5



Yeni solve türü tanıtıldığı bantta relief biraz daha görünür olur.







\\---







\\## 5. Oyuncu ilerleme beklentisi







\\## Ortalama oyuncu



İlk 1 saatte:



\\- Stage 8–12 civarına gelir



\\- ilk solve mantıklarını öğrenir



\\- mini-boss bandını görür ya da yaklaşır



\\- build kurmanın değerini anlar







\\## İyi oyuncu



\\- biraz daha ileri gidebilir



\\- ama World 1’i bir oturuşta söküp atmaz







\\## Zayıf oyuncu



\\- yine de sistem öğrenir



\\- tamamen duvara çarpmaz



\\- bir sonraki run için anlamlı bir şey kazanmış hisseder







\\---







\\## 6. Reward band yorumu







| Reward Band | Hissi | Kullanım |



|---|---|---|



| light | küçük ama tatlı | erken öğretim |



| standard | düzenli ilerleme | ana omurga |



| spike | “bir şey başardım” | specialization ve pressure bandı |



| prep | “hazırlanıyorum” | final prep |



| boss | gerçek dönüm noktası | final sınav |







\\### Kural



Ödül yalnızca sayı değil, yön açmalıdır:



\\- yeni silah yönü



\\- yeni support seçeneği



\\- yeni modifier



\\- build çeşitliliği







\\---







\\## 7. Stage generator için kilit sonuç







Bu tablo sayesinde stage generator şunu bilir:



\\- hangi bandta hangi solve focus ağır basmalı



\\- hangi template kullanılmalı



\\- oyuncudan ne kadar hata tolere etmesi beklenmeli



\\- ne kadar relief verilmesi normal



\\- hangi ödül hissi üretilmeli







Yani artık World 1:



\\- tek tek el yapımı stage listesi olmaktan çıkar



\\- kurallı bir üretim omurgasına dönüşür







\\---







\\## 8. Son karar özeti







World 1 Stage Band Table v1:



\\- World 1’in kanonik omurgasını kilitler



\\- her bandın ne öğretmesi gerektiğini netleştirir



\\- solve focus, tempo template, reward band ve tolerance ilişkisini sabitler



\\- oyuncuya görünürde sade, arkada güçlü bir sistem bırakır



\\- ileride tekrar başa dönmeden tuning yapmamızı kolaylaştırır



\-----------------



\\# Top End War — World 1 Unlock \\\& Reward Matrix v1



\\\_What the player earns, unlocks, and feels across World 1\\\_







\\---







\\## 0. Belgenin amacı







Bu belge, World 1 boyunca oyuncunun:







\\- ne kazandığını



\\- ne açtığını



\\- neyi hissetmesi gerektiğini



\\- hangi ilerleme tipinin hangi bantta verilmesi gerektiğini







kilitler.







\\### Temel ilke



Ödül sistemi:



\\- sadece sayı vermemeli



\\- sadece grind hissi üretmemeli



\\- oyuncuya \\\*\\\*yeni seçenek\\\*\\\*, \\\*\\\*yeni yön\\\*\\\*, \\\*\\\*yeni umut\\\*\\\* vermeli







\\### Kural



World 1 ödül yapısı:



\\- erken oyunda öğretici



\\- orta oyunda motive edici



\\- geç oyunda hazırlayıcı



\\- finalde tatmin edici olmalı







\\---







\\## 1. Ödül türleri







World 1’de 5 ana ödül türü vardır:







\\### 1. Currency Reward



\\- gold / soft currency



\\- upgrade ve meta kararları için







\\### 2. Unlock Reward



\\- yeni silah erişimi



\\- yeni support yönü



\\- yeni gate ailesi görünürlüğü



\\- yeni build potansiyeli







\\### 3. Equipment Reward



\\- yeni ekipman



\\- modifier açılımı



\\- build çeşitliliği







\\### 4. Progress Reward



\\- yeni stage / yeni band erişimi



\\- mini-boss / final prep geçişi



\\- world progression hissi







\\### 5. Milestone Reward



\\- “bir şey başardım” hissi



\\- boss sonrası



\\- mini-boss sonrası



\\- belirli band sonlarında







\\---







\\## 2. World 1 için ana ödül felsefesi







\\### Kural 1



Oyuncuya sürekli küçük ödüller verilir.







\\### Kural 2



Her band sonunda hissedilir bir dönüm noktası ödülü verilir.







\\### Kural 3



Kalıcı ödüller oyunu ezdirecek saf stat şişmesi yerine



\\\*\\\*yeni seçenekler\\\*\\\* açmalıdır.







\\### Kural 4



Şanslı drop heyecanı olabilir ama ana ilerleme tamamen RNG’ye bağlı olmamalı.







\\### Kural 5



Oyuncu kötü run’da bile “boşa gitmedim” hissi almalı.







\\---







\\## 3. Oyuncu ilerlemesinin iki katmanı







\\## 3.1 Run içi ödül



Run sırasında alınır, run sonunda sıfırlanır:



\\- gate güçleri



\\- reinforce



\\- sustain



\\- solve bonusları



\\- boss prep etkileri







\\## 3.2 Run dışı ödül



Kalıcıdır:



\\- yeni archetype erişimi



\\- equipment havuzu genişlemesi



\\- support/squad yönü açılımı



\\- meta currency



\\- stage/world progression







\\---







\\## 4. Reward Band davranışı







| Reward Band | Hedef His | Ne Verir |



|---|---|---|



| light | küçük ama tatlı ilerleme | az currency, küçük unlock hissi |



| standard | düzenli gelişim | currency + küçük equipment şansı |



| spike | belirgin ilerleme | daha iyi currency + unlock veya güçlü item |



| prep | hazırlanıyorum | solve/build odaklı ödül |



| boss | dönüm noktası | büyük unlock + milestone hissi |







\\---







\\## 5. Unlock türleri







\\## 5.1 Weapon Unlock



Yeni weapon family erişimi veya kullanılabilir hale gelmesi.







\\### World 1 kuralı



\\- hepsi bir anda verilmez



\\- oyuncuya sindirerek açılır



\\- yeni silah “bak yeni silah” değil, “bak yeni çözüm yolu” hissi vermelidir







\\---







\\## 5.2 Squad / Support Unlock



Oyuncunun support layer’ını zenginleştiren açılımlar.







Örnek:



\\- Piyade reinforce ağırlığı



\\- Mekanik support erişimi



\\- Teknoloji support erişimi



\\- support preset slotu







\\---







\\## 5.3 Equipment Unlock



Yeni ekipman, yeni modifier, yeni slot hissi.







\\### Kural



World 1’de equipment:



\\- build’i çeşitlendirmeli



\\- ama oyuncuyu loot çöplüğüne boğmamalı







\\---







\\## 5.4 System Unlock



Oyuncuya yeni karar alanı açan ödüller.







Örnek:



\\- build snapshot görünürlüğü



\\- boss prep gate görünürlüğü



\\- yeni gate ailelerinin aktifleşmesi



\\- gelişmiş support seçimi







\\---







\\## 6. Band bazlı Unlock \\\& Reward Matrix







\\---







\\## 6.1 Tutorial Core (Stage 1–5)







\\### Oyuncunun bu bantta hissetmesi gereken



\\- “İlerliyorum”



\\- “Boşa oynamıyorum”



\\- “Sistem bana yavaş yavaş bir şey açıyor”







\\### Verilecek ödül tipi



\\- light currency



\\- ilk equipment parçaları



\\- ilk build yönünü ima eden küçük ödüller







\\### Açılabilecek şeyler



\\- temel equipment drop havuzu



\\- Piyade support’un görünür hale gelmesi



\\- ilk build snapshot hissi







\\### Bu bant sonunda oyuncuya verilmesi gereken büyük his



\\- “Tamam, oyunu anlıyorum. Devam etmek mantıklı.”







\\---







\\## 6.2 Build Discovery (Stage 6–10)







\\### Oyuncunun hissetmesi gereken



\\- “Artık seçimlerim gerçekten fark ediyor.”







\\### Verilecek ödül tipi



\\- standard currency



\\- solve odaklı equipment/modifier



\\- ilk ciddi unlock hissi







\\### Açılabilecek şeyler



\\- Sniper / armor solve yönünün görünürleşmesi



\\- ilk Mekanik veya Teknoloji support sinyali



\\- solve odaklı item/modifier







\\### Bant sonu milestone



\\- ilk mini-boss öncesi veya sonrası hissedilir ödül







\\### Oyuncuya vermesi gereken his



\\- “Artık sadece koşup ateş etmiyorum, bir şey kuruyorum.”







\\---







\\## 6.3 First Friction (Stage 11–15)







\\### Oyuncunun hissetmesi gereken



\\- “Yeni oyun alanları açılıyor.”







\\### Verilecek ödül tipi



\\- standard reward



\\- Shotgun yönünü açan ödüller



\\- yakın risk / reward odaklı itemler



\\- support çeşitliliği







\\### Açılabilecek şeyler



\\- Mekanik support hattı



\\- Shotgun erişimi ya da Shotgun’a zemin hazırlığı



\\- daha görünür reinforce/army sinerjileri







\\### Bu bant sonunda oyuncuya verilmesi gereken his



\\- “Yeni tarzlar deneyebilirim.”







\\---







\\## 6.4 Controlled Complexity (Stage 16–20)







\\### Oyuncunun hissetmesi gereken



\\- “Oyun genişliyor ama kontrol bende.”







\\### Verilecek ödül tipi



\\- standard + spike arası



\\- geometry / area / swarm solve ödülleri



\\- Launcher yönünü açan ya da parlatan ödüller







\\### Açılabilecek şeyler



\\- Launcher erişimi veya launcher-support ilişkisi



\\- Geometry odaklı equipment/modifier



\\- Teknoloji support’un daha görünür açılımı







\\### Bant sonu hissi



\\- “Artık build’im gerçekten farklı oynuyor.”







\\---







\\## 6.5 Specialization (Stage 21–25)







\\### Oyuncunun hissetmesi gereken



\\- “Benim build yönüm belli oluyor.”







\\### Verilecek ödül tipi



\\- spike reward



\\- belirgin build odaklı modifier’lar



\\- support preset kalitesi artışı



\\- yeni equipment bandı







\\### Açılabilecek şeyler



\\- ikinci support yönü seçeneği



\\- daha net build specialization item’ları



\\- elite / armor / geometry odaklı kalıcı seçimler







\\### Bant sonu hissi



\\- “Benim karakterim/ordum artık başkasından farklı.”







\\---







\\## 6.6 Pressure \\\& Punishment (Stage 26–30)







\\### Oyuncunun hissetmesi gereken



\\- “Doğru oynarsam büyük kazanırım.”







\\### Verilecek ödül tipi



\\- spike reward



\\- daha güçlü currency payout



\\- solve odaklı büyük item / modifier



\\- final prep’e giden açılım







\\### Açılabilecek şeyler



\\- boss-prep odaklı item’ların görünmeye başlaması



\\- Beam için ön hazırlık



\\- long-fight odaklı seçenekler







\\### Bant sonu hissi



\\- “Şimdi ciddi oynuyorum, ödülü de ciddi.”







\\---







\\## 6.7 Final Prep (Stage 31–34)







\\### Oyuncunun hissetmesi gereken



\\- “Final için hazırlanıyorum.”







\\### Verilecek ödül tipi



\\- prep reward



\\- boss solve / beam solve / sniper solve odaklı ödüller



\\- final boss’a yönelik bilinçli seçim imkânı







\\### Açılabilecek şeyler



\\- Beam erişimi veya Beam’i anlamlı kılan ödüller



\\- boss prep gate değerini artıran meta seçenekler



\\- long-fight ve elite çözümünü güçlendiren item’lar







\\### Bant sonu hissi



\\- “Hazırım ya da neye ihtiyacım olduğunu biliyorum.”







\\---







\\## 6.8 Final Boss (Stage 35)







\\### Oyuncunun hissetmesi gereken



\\- “Bu bir dönüm noktası.”







\\### Verilecek ödül tipi



\\- boss reward



\\- büyük milestone currency



\\- büyük unlock



\\- World 2 veya sonraki aşamaya motivasyon







\\### Açılabilecek şeyler



\\- yeni world kapısı



\\- yeni silah family hazırlığı



\\- yeni support tier



\\- meta progression’de görünür sıçrama







\\### Final boss sonrası his



\\- “Bunu geçtim, gerçekten ilerledim.”







\\---







\\## 7. Weapon unlock pacing







\\## Önerilen açılım







\\### Başlangıç



\\- Assault



\\- SMG







\\### Build Discovery bandı



\\- Sniper yönü görünürleşir







\\### First Friction / Controlled Complexity



\\- Shotgun



\\- Launcher







\\### Final Prep



\\- Beam







\\### Kural



Yeni silah bir anda “daha iyi silah” gibi gelmemeli.  



Her yeni silah:



\\- yeni çözüm



\\- yeni his



\\- yeni build yönü



olarak sunulmalı.







\\---







\\## 8. Squad unlock pacing







\\### Başlangıç



\\- Piyade support







\\### 11–15



\\- Mekanik support görünürleşir







\\### 16–20



\\- Teknoloji support görünürleşir







\\### 21+



\\- support bias / preset mantığı netleşir







\\### Kural



Support unlock:



\\- mikro yönetim açılımı değil



\\- build kimliği açılımı olmalı







\\---







\\## 9. Equipment / modifier pacing







\\## Erken bantlar



\\- basit, okunur modifier’lar



\\- düz hasar / tempo / HP / küçük sustain







\\## Orta bantlar



\\- solve odaklı modifier’lar



\\- armor pen



\\- elite damage



\\- support synergy



\\- geometry ilişkileri







\\## Geç bantlar



\\- long-fight



\\- boss prep



\\- beam/sniper parlatan modifier’lar







\\### Kural



Modifier’lar:



\\- build’i büyütsün



\\- ama oyuncuyu tooltip mezarlığına boğmasın







\\---







\\## 10. Currency pacing







\\## Erken oyun



\\- küçük ama sık



\\- “ilerliyorum” hissi







\\## Orta oyun



\\- düzenli ve hissedilir



\\- yeni denemeleri destekler







\\## Geç oyun



\\- daha değerli



\\- ama economy’yi patlatmaz







\\### Kural



Currency:



\\- grind hissi yaratmamalı



\\- her run’dan sonra küçük ilerleme vermeli







\\---







\\## 11. Kötü run ödül kuralı







Oyuncu kötü run’da bile:



\\- biraz currency



\\- biraz progression hissi



\\- belki küçük bir item / şans



\\- en azından öğrenme duygusu



almalı







\\### Sonuç



Oyuncu:



> “Bu run tamamen çöpe gitti”  



dememeli.







\\---







\\## 12. İyi run ödül kuralı







İyi run:



\\- daha yüksek currency



\\- daha iyi drop bandı



\\- milestone ilerlemesi



\\- daha hızlı world progression







Ama:



\\- kötü run oynayan oyuncuyu tamamen dışlamamalı







\\---







\\## 13. 1 saatlik meta beklenti







İlk 1 saatin sonunda oyuncu ideal olarak:







\\- 1–2 yeni weapon direction görmüş



\\- birkaç equipment/modifier toplamış



\\- support sistemini anlamış



\\- ilk mini-boss çevresini görmüş



\\- yeni build deneme motivasyonu kazanmış



\\- elinde anlamlı ama oyunu kırmayan bir meta birikim oluşturmuş olmalı







\\### Çok önemli



1 saat sonunda oyuncu:



\\- ne çok güçsüz



\\- ne de oyunu kıracak kadar güçlü



olmamalı







\\---







\\## 14. Görünür ödül sunumu







Oyuncuya gösterilecek şeyler sade olmalı:







\\### Stage sonrası



\\- kazandığın currency



\\- varsa yeni equipment



\\- varsa açılan yön / özellik



\\- kısa bir sonraki hedef hissi







\\### Gösterilmeyecek şeyler



\\- karmaşık drop tabloları



\\- 15 satır ekonomi özeti



\\- gereksiz tooltip yığını







\\---







\\## 15. Unlock \\\& Reward Matrix — kısa tablo







| Band | Ana Unlock | Ana Reward | Oyuncu Hissi |



|---|---|---|---|



| 1–5 | temel equipment / piyade support görünürlüğü | light currency | “ilerliyorum” |



| 6–10 | solve yönü / sniper farkı | standard reward | “doğru seçim fark ediyor” |



| 11–15 | mekanik support / shotgun yönü | standard reward | “yeni tarzlar var” |



| 16–20 | teknoloji support / launcher yönü | standard+ | “oyun genişliyor” |



| 21–25 | specialization item’ları | spike reward | “build’im oluşuyor” |



| 26–30 | boss-prep ön açılımları | spike reward | “ciddi ödül geliyor” |



| 31–34 | beam / boss prep yönü | prep reward | “finale hazırlanıyorum” |



| 35 | büyük milestone unlock | boss reward | “bir eşiği geçtim” |







\\---







\\## 16. Son karar özeti







World 1 Unlock \\\& Reward Matrix v1:



\\- ödül sistemini sadece sayı değil, yön açan yapı haline getirir



\\- oyuncunun ilk 1 saatlik motivasyonunu düzenler



\\- her bandın ne vermesi gerektiğini kilitler



\\- yeni silah, support, equipment ve meta ilerlemeyi kontrollü açar



\\- kötü run’ı boşa gitmiş hissettirmez



\\- iyi run’ı tatmin edici hale getirir



\\- World 1 sonunda oyuncuya gerçek ilerleme hissi verir



\-------------



\\# Top End War — Build Snapshot \\\& Threat Tag System v1



\\\_Show simple, hide complexity\\\_







\\---







\\## 0. Belgenin amacı







Bu belge, World 1 boyunca oyuncuya:







\\- build’inin neye iyi olduğunu



\\- stage’in ne istediğini



\\- gate seçiminin neden değerli olduğunu







\\\*\\\*çok kısa, sade ve okunur\\\*\\\* şekilde göstermeyi tanımlar.







\\### Temel ilke



Arkada sistem karmaşık olabilir.  



Ama oyuncuya görünen yüz:



\\- kısa



\\- temiz



\\- yön verici



\\- bunaltmayan



olmalıdır.







\\### Kural



Oyuncu:



> “Öf bunun datasını mı okuyacağım?”



dememeli.







Oyuncu sadece şunu hissetmeli:



\\- benim build’im şu işe iyi



\\- bu stage şu tehdidi soruyor



\\- şu gate niye hoşuma gitti anlıyorum







\\---







\\## 1. Sistem neden gerekli?







Şu an tasarımda şunlar var:



\\- solve focus



\\- packet library



\\- stage band



\\- weapon role



\\- gate economy



\\- support layer







Ama bunların hepsi oyuncuya aynen gösterilirse oyun yorucu olur.







\\### Bu sistemin görevi



Bütün derin sistemi oyuncuya şu 3 sade yüz üzerinden göstermek:







1\\. \\\*\\\*Build Snapshot\\\*\\\*



2\\. \\\*\\\*Threat Tags\\\*\\\*



3\\. \\\*\\\*Gate Short Tags\\\*\\\*







\\---







\\## 2. Görünür katmanlar







\\## 2.1 Build Snapshot



Oyuncunun mevcut build’inin kısa kimliği.







Örnek:



\\- Balanced



\\- Swarm Clear



\\- Armor Break



\\- Elite Hunt



\\- Boss Prep



\\- Close Burst



\\- Area Control



\\- High Tempo



\\- Long Fight







Bu, loadout ekranında ve stage öncesi görünür.







\\---







\\## 2.2 Threat Tags



Stage’in oyuncudan ne istediğini kısa anlatan etiketler.







Örnek:



\\- SWARM



\\- ARMOR



\\- ELITE



\\- MIXED



\\- PRIORITY



\\- LONG FIGHT



\\- BOSS PREP







Bu, stage kartında görünür.







\\---







\\## 2.3 Gate Short Tags



Kapının ne verdiğini iki kısa etiketle anlatır.







Örnek:



\\- ARMOR • ELITE



\\- POWER • SAFE



\\- ARMY • HEAL



\\- TEMPO • DPS



\\- BOSS • BEAM







Bu, run içinde kapı üstünde görünür.







\\---







\\## 3. Build Snapshot sistemi







\\## 3.1 Build Snapshot nedir?



Build snapshot, oyuncunun elindeki kurulumun gizli matematiğini özetleyen kısa etikettir.







Oyuncu bunu şöyle okur:



\\- “Ben şu an ne oynuyorum?”



\\- “Benim build’im neye yatkın?”



\\- “Bu stage’e ne kadar uyuyorum?”







\\---







\\## 3.2 Snapshot kuralı



Her build’e uzun açıklama verilmeyecek.  



En fazla:



\\- 1 ana snapshot



\\- opsiyonel 1 alt snapshot







\\### Örnek



Ana:



\\- `Armor Break`







Alt:



\\- `Long Fight`







\\---







\\## 3.3 Snapshot kaynakları



Snapshot şu katmanlardan hesaplanır:







\\- commander weapon family



\\- equipment modifier yönü



\\- support bias



\\- gate etkileri



\\- varsa solve bonusları







Ama oyuncuya bu hesap gösterilmez.







\\---







\\## 3.4 Snapshot listesi v1







\\### Balanced



Kullanım:



\\- genel güvenli build



\\- assault ağırlıklı



\\- net bir solve’a aşırı yatmamış







\\### Swarm Clear



Kullanım:



\\- SMG



\\- dense packet temizliği



\\- tempo / area desteği







\\### Armor Break



Kullanım:



\\- sniper



\\- breacher



\\- armor pen



\\- brute çözümü







\\### Elite Hunt



Kullanım:



\\- elite damage



\\- sniper / beam



\\- priority target odaklı







\\### Close Burst



Kullanım:



\\- shotgun



\\- mekanik support



\\- yakın baskı çözümü







\\### Area Control



Kullanım:



\\- launcher



\\- geometry



\\- swarm punish







\\### High Tempo



Kullanım:



\\- fire rate



\\- fast cycle



\\- chain hissi







\\### Long Fight



Kullanım:



\\- beam



\\- sustain



\\- boss prep



\\- uzun temas isteyen yapı







\\### Boss Prep



Kullanım:



\\- final prep



\\- beam/sniper solve



\\- elite/armor ağırlıklı stage hazırlığı







\\---







\\## 3.5 Snapshot gösterim alanları







\\### Loadout Screen



Mutlaka görünür.







Format:



```text



Build Snapshot



Armor Break







veya







Build Snapshot



Balanced



Sub: High Tempo



Result Screen







Kısa recap’te gösterilebilir.







Stage Start öncesi







İstersen küçük bilgi olarak görünebilir ama zorunlu değil.







4\\. Threat Tag sistemi



4.1 Threat Tag nedir?







Stage’in oyuncuya sorduğu soruyu tek kelimelik kısa etiketlerle gösterir.







Oyuncu bunu şöyle okur:







“Bu bölüm ne istiyor?”



“Neye hazırlıklı olmalıyım?”



“Benim build’im buna uygun mu?”



4.2 Threat Tag listesi v1



SWARM







Kalabalık küçük baskı







ARMOR







Zırhlı hedef / brute çözümü







ELITE







Yüksek öncelikli hedef baskısı







PRIORITY







Geç kalırsan ceza yersin hissi







MIXED







Genelci / çok yönlü çözüm ister







LONG FIGHT







Uzun temas / boss tipi dayanım







BOSS PREP







Yaklaşan büyük sınava hazırlık







CLOSE PRESSURE







Yakın baskı / charger / yakın alan yoğunluğu







LANE PRESSURE







Sağ-sol/dağılım baskısı







4.3 Threat Tag kuralları



Kural 1







Bir stage’de en fazla 2 ana tag, en fazla 3 tag gösterilir.







Kural 2







Tag’ler oyuncuya çözümü doğrudan söylemez.



Sadece sorunu anlatır.







Yanlış örnek:







Use Sniper







Doğru örnek:







ARMOR



Kural 3







Aynı band içinde tag’ler değişebilir ama ana solve focus’la uyumlu olmalı.







4.4 Stage Card formatı







Örnek:







Stage 08



Threats: ARMOR • PRIORITY



Reward: Standard







veya







Stage 32



Threats: ELITE • BOSS PREP



Reward: Prep



5\\. Build Snapshot + Threat Tag ilişkisi







Bu ikisi birlikte çalışır.







Oyuncu şunu görür:







Stage:







ARMOR • ELITE







Build:







Armor Break







Ve içinden şunu hisseder:







“Tamam, bu bölüm benim build’ime uygun.”







veya







Stage:







SWARM • LANE PRESSURE







Build:







Elite Hunt







Oyuncu içinden şunu hisseder:







“Burada çok rahat olmayabilirim.”







Ama sistem açıkça bunu yazmaz.







Bu çok önemli.







6\\. Gate Short Tag sistemi



6.1 Gate dili







Kapının üst satırı:







doğrudan etki







Alt satırı:







2 kısa tag



Örnek



+12 Armor Pen



ARMOR • ELITE



Örnek



+2 Infantry



ARMY • SAFE



6.2 Gate tag havuzu v1



POWER







genel güç







SAFE







güvenli genel seçim







TEMPO







akış / hız







DPS







saf saldırı katkısı







ARMOR







armor solve







ELITE







elite solve







ARMY







support/squad büyümesi







HEAL







iyileşme / sürdürülebilirlik







AREA







alan etkisi







PACK







küme cezalandırma







CLOSE







yakın baskı ilişkisi







BURST







ani yüksek verim







LINE







çizgisel verim







CONTROL







geometry / spread / yön verimi







BOSS







boss odaklı







BEAM







beam sinerjisi







LONG







uzun dövüş







SUPPORT







arka sıra / yardımcı çözüm







SURVIVE







hayatta kalma







CONTACT







temas cezasına dayanma







MARK







işaretleme / odak yardımı







EXECUTE







bitirici







THREAT







tehdit okuma







6.3 Gate tag kuralı







Mümkün olduğunca ortak shared tag havuzu kullanılır.



Her gate için yeni kısa tag türetilmez.







Bu hem:







localization’ı kolaylaştırır



hem de oyuncuya tanıdık bir dil kurar



7\\. UI yerleşimleri



7.1 Stage Card







Göster:







Stage Name



Threat Tags



Reward Band



opsiyonel kısa build uyumu hissi



Örnek



Threats: ARMOR • ELITE



7.2 Loadout Screen







Göster:







commander weapon



support summary



build snapshot



Örnek



Build Snapshot



High Tempo



Sub: Swarm Clear



7.3 Gate üstü world-space text







Göster:







effect title



2 kısa tag



Örnek



+1 Pierce



LINE • ARMOR



7.4 Result Screen







Gösterilebilir:







run sonunda kısa build recap



stage threat recap



kazanç özeti







Ama uzun analiz ekranı yapılmaz.







8\\. Sistem nasıl hesaplanır?







Oyuncuya görünmez.







8.1 Build Snapshot hesaplama







Arka planda puan mantığıyla çalışır.







Örnek:







SMG + tempo gate + area modifier = Swarm Clear, High Tempo



Sniper + armor pen + elite bonus = Armor Break, Elite Hunt



Beam + sustain + long-fight gate = Boss Prep, Long Fight



Kural







En yüksek puanı alan snapshot ana etiket olur.







8.2 Threat Tag hesaplama







Stage recipe’den gelir.







Örnek:







solveFocus = armor



secondaryThreat = elite







→ göster:



ARMOR • ELITE







8.3 Gate Tag hesaplama







Gate config’ten sabit gelir.







Örnek:







Breacher



→ ARMOR • ELITE



9\\. Oyuncu hissi hedefi







Bu sistemin sonunda oyuncu şunu hissetmeli:







oyun beni boğmuyor



ama aptal yerine de koymuyor



bölüm ne istiyor anlıyorum



build’im neye iyi anlıyorum



kapı neden hoşuma gitti anlıyorum



detay istersem daha derine bakabilirim



ama bakmasam da akış bozulmuyor



10\\. Riskler



Risk 1







Çok fazla tag gösterirsek sistem sıkıcı olur.







Risk 2







Build snapshot çok sık değişirse oyuncu güven kaybeder.







Risk 3







Threat tag çözümü direkt söylerse keşif hissi ölür.







Risk 4







Gate tag’leri çok soyut olursa oyuncu anlam veremez.







11\\. Güvenli kullanım kuralları



Kural 1







Stage Card: max 3 tag







Kural 2







Loadout: 1 ana snapshot, opsiyonel 1 alt snapshot







Kural 3







Gate: 2 kısa tag







Kural 4







Result ekranı: kısa recap, uzun analiz değil







12\\. Son karar özeti







Build Snapshot \\\& Threat Tag System v1:







karmaşık sistemi oyuncuya sade yüzle gösterir



build, stage ve gate bilgisini okunur hale getirir



oyuncuyu yormadan yön verir



localization için kısa ve tutarlı dil üretir



World 1’in görünür UX omurgasını kurar



\----------

