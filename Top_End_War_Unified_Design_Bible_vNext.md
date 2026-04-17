# Top End War — Unified Design Bible / Project Handoff vNext
_Güncel birleşik tasarım özeti, MVP kapsamı, vertical slice planı, veri mimarisi ve mevcut script envanteri_

## 0. Bu dosyanın amacı

Bu dosya, **Top End War** projesinin şu anki en güncel tasarım yönünü tek yerde toplar.

Amaç:
- yeni bir konuşmada projeyi sıfırdan anlatma ihtiyacını azaltmak
- başka bir kişi veya yapay zekânın projeyi hızlıca anlayabilmesini sağlamak
- eski `Design_Bible v5.2` içindeki değerli fikirleri koruyup, artık geride kalmış veya değişmiş kararları ayıklamak
- MVP ve vertical slice için gerçekten uygulanacak çekirdeği netleştirmek
- scope’u korumak ve oyunun tek kişilik geliştirme gerçekliğine uygun kalmasını sağlamak

> **Önemli not:** Bu dosya, eski belgeleri birebir tekrar etmek için değil; onların üstüne çıkıp güncel ve daha doğru bir “kanonik referans” olmak için yazılmıştır.

---

## 1. Projenin kimliği

### Oyun türü
**Hybrid-Casual / Mid-Core** mobil aksiyon oyunu

### Temel yapı
- Dünya haritası üzerinden stage seçimi
- Runner ana mod
- Auto-shoot combat
- Kapılardan geçerek build kurma
- Ordu büyütme / birlik katkısı
- Mini-boss ve final boss savaşları
- Uzun ömürlü progression hissi
- Sabit zorluk (Fixed Difficulty)

### Oyun hissi
Oyuncu, “sadece sayı toplayan” bir hyper-casual oyun değil, **silah, kapı, düşman ve stage bilgisiyle karar veren** bir fetih oyunu oynadığını hissetmeli.

### Ana vaat
> Koş, kapılardan geçerek build kur, doğru silah ve doğru güçlenme seçimleriyle bölümü çöz, ordunu büyüt, boss’u geç, haritada ilerle.

---

## 2. Kırmızı çizgiler / kanonik kararlar

### 2.1 Zorluk
- **DDA YOK**
- Oyuncuya göre gizli düşman ölçekleme yok
- Oyuncu güçlendikçe eski içerikleri daha rahat ezebilmelidir
- Stage zorluğu stage’e bağlı sabit yapıdadır

### 2.2 Oynanış dili
- Eski soyut `CP` mantığı oynanışın merkezi değildir
- Pervasive oynanış dili:
  - silah rolü
  - gate seçimi
  - düşman tipi
  - armor farkındalığı
  - stage bilgisi
- İstersek meta-hub tarafında CP/gear score benzeri büyük sayı kalabilir, ama combat dili bu değildir

### 2.3 Savunma katmanları
İlk sürümde:
- **HP**
- **Armor**

Şimdilik **Shield** gibi ayrı ve kafa karıştırıcı savunma katmanı yok.

### 2.4 Gate felsefesi
- Gate’ler görünür olacak
- Oyuncuya taktik düşündürecek
- Ama “şu build için bunu al” diye doğrudan akıl vermeyecek
- Kısa süreli buff veren gate yok
- Kalıcı, run boyunca taşınan etkiler var

### 2.5 Boss felsefesi
- Boss = et duvarı değil, sınav
- Yeni mekanik öğretmez
- Dünya boyunca öğretilen şeyleri test eder
- Faz geçişlerinde kısa `transition lock` olabilir
- Bu “shield bar” gibi ayrı okunmamalı

### 2.6 Scope disiplini
Bu proje tek kişi tarafından geliştirildiği için:
- öncelik **üretilebilir çekirdek**
- sonra büyüyen sistemler
- her iyi fikir şimdi yapılmayacak
- her iyi fikir ayrı bir backlog maddesi olabilir

---

## 3. Şu anki gerçek hedef

### 3.1 Büyük hedef
Uzun ömürlü oyunun sağlam çekirdeğini kurmak

### 3.2 Yakın hedef
**World 1 backbone + Vertical Slice**

### 3.3 En yakın üretim hedefi
**Vertical Slice = Stage 1–10**

Bu slice ile kanıtlanacaklar:
- silahlar farklı hissediyor mu
- gate seçimi anlamlı mı
- armor düşmanı silah seçimini etkiliyor mu
- mini-boss savaşı özel ve tatmin edici mi
- meta-hub → stage → run → reward → upgrade → tekrar loop’u çalışıyor mu

---

## 4. Scope planı: 3 + 2

Koddan önce gerekli tasarım paketleri:

### Zorunlu 3 belge
1. Boss Bible  
2. MVP Scope & Vertical Slice Bible  
3. Data Schema / Config Bible  

### Yardımcı 2 belge
4. Asset Production Sheet  
5. UI Information Hierarchy  

Bu 5 belge bu dosyanın içinde birleştirilmiştir.

> Buradan sonra daha fazla büyük tasarım derinliği yerine üretime geçilmelidir.

---

## 5. World yapısı

### 5.1 Genel ilke
Dünya başına stage sayısı **esnek** olabilir.  
Eski belgelerde geçen `1-15 / 2-20 / 3-25` gibi örnekler “kesin sabit sayı” değil, pacing örneğidir.

### 5.2 World 1 backbone
World 1 için şu omurga geçerli kabul edilir:
- **1–5** Tutorial Core
- **6–10** Build Discovery
- **11–15** First Friction
- **16–20** Controlled Complexity
- **21–25** Specialization
- **26–30** Pressure & Punishment
- **31–34** Final Prep
- **35** Final Boss

Bu yapı tam 35 stage gerektirir diye düşünülmemeli; ama şu anki öğretim tasarımının referans backbone’u budur.

---

## 6. Core loop

```text
Meta-Hub / World Map
→ Stage seçimi
→ Loadout / silah seçimi
→ Runner stage
→ Kapılar + düşman dalgaları + build oluşumu
→ Mini-boss / boss
→ Reward
→ Upgrade / harita ilerlemesi
→ Sonraki stage
```

---

## 7. Vertical Slice Final Pack

### 7.1 Slice içinde olacaklar
- Stage 1–10
- 3 aktif silah
- 5 aktif düşman
- 8 aktif kapı
- 1 mini-boss
- Meta-hub lite
- Basit gold economy
- Basit upgrade ekranı

### 7.2 Slice dışında kalacaklar
- Shotgun
- Launcher
- Backline Operator
- Siege Brute
- War Machine
- Final Boss
- Risk gate’lerin tam varyant sistemi
- Boss prep gate’ler
- Arena
- Alliance
- Level editor
- Challenge mode
- World 2+
- canlı etkinlik sistemleri
- derin monetization katmanları

---

## 8. Weapon Bible v1

İlk oyunun çekirdeğinde 5 ana silah ailesi tanımlıdır, ama vertical slice’ta 3 tanesi aktif olacaktır.

### 8.1 Tam silah aileleri
- Assault
- SMG / Gatling
- Sniper / DMR
- Shotgun
- Launcher / Mortar

### 8.2 Vertical Slice’ta aktif 3 silah
- Assault
- SMG
- Sniper

### Tasarım ilkesi
Silahlar **strict upgrade** değil, **sidegrade** mantığındadır.  
Ham DPS benzer olabilir, ama gerçek güçleri farklı problemlerde ortaya çıkar.

### Assault
- genelci
- güvenli seçim
- karışık wave’lerde iyi
- boss’ta orta-iyi
- runner’da yüksek güvenilirlik

### SMG
- swarm temizliği
- yüksek tempo
- lane baskısı
- boss / armor hedeflerinde zayıflar

### Sniper
- elite / brute / mini-boss çözümü
- yüksek armor pen
- düşük tempo
- swarm temizliğinde zayıf

### 8.3 Vertical slice statları

#### Assault
- Base Damage: 14
- Fire Rate: 3.6/s
- Raw DPS: 50.4
- Armor Pen: 6
- Pierce: 0
- Bounce: 0

#### SMG
- Base Damage: 5.2
- Fire Rate: 9.6/s
- Raw DPS: 49.9
- Armor Pen: 2
- Pierce: 0
- Bounce: 0

#### Sniper
- Base Damage: 52
- Fire Rate: 0.95/s
- Raw DPS: 49.4
- Armor Pen: 18
- Pierce: 0
- Bounce: 0

---

## 9. Enemy Bible v1

### 9.1 Çekirdek düşman dili
Düşmanlar “daha fazla HP” ile değil, oyuncudan istedikleri karar ile ayrılır.

İlk etapta kullanılan çekirdek tipler:
- Trooper
- Swarm
- Charger
- Armored Brute
- Elite Charger

Genişletilmiş roster:
- Backline Operator
- Siege Brute
- Mini-boss’lar
- Final Boss

### 9.2 Vertical slice aktif düşmanları

#### Trooper
- referans normal düşman
- HP Factor: 0.90
- Armor: 0

#### Swarm
- sayı baskısı
- HP Factor: 0.35
- Armor: 0

#### Charger
- öncelik tehdidi
- HP Factor: 0.65
- Armor: 0

#### Armored Brute
- armor check
- HP Factor: 1.25
- Armor: 28

#### Elite Charger
- panik / elite öncelik testi
- HP Factor: 3.40
- Armor: 8

### 9.3 Tasarım ilkesi
Biyom oyuncuya debuff vermez.  
Onun yerine düşman kompozisyonuna karakter verir.

---

## 10. Gate Bible v1

### 10.1 Genel gate ilkeleri
- Kapılar görünür olacak
- Üstte ana etki, altta kısa etiketler
- Oyuncuya doğrudan silah önerisi yazılmayacak
- Kısa süreli buff yok
- Kalıcı stat / davranış değişimi var

### 10.2 Görünüm şablonu
```text
Üst: ana etki
Alt: 2 kısa etiket
```

Örnek:
```text
+12 Zırh Delme
ARMOR • ELITE
```

### 10.3 Gate aileleri
- Power
- Tempo
- Geometry
- Army
- Sustain
- Tactical

### 10.4 Vertical slice aktif 8 gate
1. Hardline = +8% Silah Gücü  
2. Overclock = +10% Ateş Hızı  
3. Breacher = +12 Zırh Delme  
4. Piercing Round = +1 Delme  
5. Reinforce: Piyade = +2 Piyade  
6. Medkit = Komutan +25% HP  
7. Field Repair = Askerler %35 iyileşir  
8. Execution Line = +12% Elite Hasarı  

### 10.5 Slice açılış sırası
#### Stage 1–5
- Hardline
- Overclock
- Reinforce: Piyade
- Medkit
- Field Repair

#### Stage 6–10
- Breacher
- Piercing Round
- Execution Line

---

## 11. Stage Grammar v1

### 11.1 Band akışı
#### Stage 1–5: Tutorial Core
- hareket
- auto-shoot
- basit gate okuma
- swarm / charger tanıtımı
- sustain ve army ilk kez görünür

#### Stage 6–10: Build Discovery
- armor tanıtımı
- delme / armor pen değeri
- ilk elite baskısı
- ilk mini-boss

### 11.2 Stage 1–10 kısa plan

#### Stage 1
- ders: hareket + ateş
- düşman: trooper
- spotlight: assault

#### Stage 2
- ders: swarm farkı
- düşman: trooper + swarm
- spotlight: SMG

#### Stage 3
- ders: charger önceliği
- düşman: trooper + charger
- spotlight: assault

#### Stage 4
- ders: sustain ve army kapısı
- düşman: hafif karışık
- spotlight: assault

#### Stage 5
- ders: tutorial sınavı
- düşman: trooper + swarm + charger
- spotlight: assault

#### Stage 6
- ders: armor tanıtımı
- düşman: trooper + brute
- spotlight: sniper

#### Stage 7
- ders: delme ve armor pen
- düşman: trooper + brute
- spotlight: sniper

#### Stage 8
- ders: elite baskısı
- düşman: charger + elite charger
- spotlight: assault

#### Stage 9
- ders: karışık build testi
- düşman: swarm + brute + charger
- spotlight: assault / sniper

#### Stage 10
- ders: ilk mini-boss
- düşman: Gatekeeper Walker
- spotlight: assault / sniper

---

## 12. Boss Bible v1

### 12.1 Genel ilkeler
- Boss, yeni mekanik öğretmez
- Önceden öğretileni test eder
- Ayrı shield sistemi yok
- Sadece HP + Armor + Transition Lock

### 12.2 Mini-Boss 1 — Gatekeeper Walker
#### Rol
İlk gerçek build sınavı

#### Test ettiği şeyler
- tek hedef baskısı
- hafif armor farkındalığı
- telegraph okuma
- greed DPS’in cezalandırılması

#### Formül
- Boss HP = TargetDPS × 13

#### Stage 10 örneği
- TargetDPS = 232
- HP ≈ 3016
- Armor = 10
- süre hedefi = 12–15 sn

#### Saldırılar
- Line Shot
- Front Sweep
- Short Charge

#### Faz yapısı
- Faz 1: %100 → %50
- Transition Lock: 1.6 sn
- Faz 2: %50 → %0

### 12.3 İleri boss planı
Tam world için:
- Mini-Boss 2 = War Machine
- Final Boss = World 1 Final Boss

Ama bunlar vertical slice kapsamında zorunlu değildir.

---

## 13. Economy / Progression özeti

### 13.1 Vertical slice economy
Şimdilik yalnızca:
- Gold
- basit stage reward
- basit mid-run reward
- basic weapon upgrade

### 13.2 Slice stage gold ödülleri
- S1: 144
- S2: 156
- S3: 169
- S4: 182
- S5: 195
- S6: 208
- S7: 222
- S8: 236
- S9: 251
- S10: 266

### 13.3 Mid-run ödüller
- S1: 50
- S2: 55
- S3: 59
- S4: 64
- S5: 68
- S6: 73
- S7: 78
- S8: 83
- S9: 88
- S10: 93

### 13.4 Slice toplam gold
- Stage sonu toplam = 2029
- Mid-run toplam = 711
- Genel toplam = 2740

### 13.5 Slice upgrade eğrisi
- L1→2 = 220
- L2→3 = 273
- L3→4 = 338
- L4→5 = 419
- L5→6 = 519
- L6→7 = 644
- L7→8 = 799
- L8→9 = 990

Bu, oyuncuya tek silaha yatırım ya da iki silaha bölme kararı verir.

---

## 14. MVP Scope & Vertical Slice

### 14.1 MVP’de kesin olacaklar
- 1 world
- vertical slice olarak ilk 10 stage
- 3 aktif silah
- 5 aktif düşman
- 8 aktif kapı
- 1 mini-boss
- map lite
- upgrade lite
- reward loop

### 14.2 MVP’de bilerek olmayacaklar
- arena
- challenge mode
- alliance
- level editor
- world 2+
- talent tree
- battle pass
- live ops
- server-side rekabet
- ileri monetization
- tam anchor 3-lane modu

---

## 15. Data Schema / Config Bible

### 15.1 Ana veri katmanları
#### A. Static Design Data
ScriptableObject tarafı:
- WeaponArchetypeConfig
- EnemyArchetypeConfig
- GateConfig
- GatePoolConfig
- StageConfig
- WaveConfig
- BossConfig
- EconomyConfig
- RewardProfileConfig
- WorldConfig

#### B. Runtime Session Data
Run sırasında yaşayan veri:
- RunState
- alınan gate etkileri
- mevcut HP
- run içi gold
- boss fazı

#### C. Persistent Save Data
Oyuncunun kalıcı hesabı:
- açılmış stage’ler
- weapon progress
- currency
- tutorial flag’leri

#### D. Live Tunable Data
Şimdilik zorunlu değil.  
İleride patch/remote tuning için ayrılabilir.

### 15.2 En kritik ayrımlar
- StageConfig ≠ RunState
- WeaponArchetypeConfig ≠ WeaponProgressData
- GateConfig ≠ ActiveGateEffectState
- EnemyArchetypeConfig ≠ SpawnEntry

---

## 16. Asset Production Sheet

### 16.1 Vertical slice için minimum asset ihtiyacı
#### Komutan
- 1 model
- 1 rig
- koşu / idle / hit / fail

#### Birlik
- 1 ana model
- küçük varyasyonlarla tür ayrımı yapılabilir

#### Silah
- Assault
- SMG
- Sniper

#### Düşman
- Trooper
- Swarm
- Charger
- Armored Brute
- Elite Charger
- Gatekeeper Walker

#### Ortam
- 1 biome
- modüler yol
- gate prefab
- temel obstacle/prop

#### UI
- HUD
- Gate panel
- stage sonuç ekranı
- upgrade lite
- harita lite

### 16.2 Placeholder olabilir
- ileri shader kalitesi
- yüz detayları
- özel sinematik
- ağır VFX polish

### 16.3 Placeholder olmaması gerekenler
- vurma hissi
- gate okunurluğu
- düşman silüet farkı
- boss telegraphing
- hareket akıcılığı

---

## 17. UI Information Hierarchy

### 17.1 Runner HUD önceliği
1. hayatta kalma bilgisi  
2. yakın karar bilgisi  
3. güçlenme hissi  
4. meta bilgi  

### 17.2 Görülmesi gerekenler
- komutan HP
- birlik özet durumu
- karşıdaki gate bilgisi
- tehlike alanları
- boss geldiğinde boss HP / faz göstergesi

### 17.3 Gösterilmemesi gerekenler
- aşırı uzun gate yazıları
- oyuncuya açık taktik tavsiyesi
- fazla floating text
- aşırı panel kalabalığı

### 17.4 Gate görünüm ilkesi
Kapının üstünde ana etki, altında iki kısa etiket olacak.  
Bu yeterince görünür ama yeterince nötrdür.

---

## 18. Şu anki .cs dosyaları (görülen envanter)

Aşağıdaki liste, ekrandaki mevcut `.cs` dosyalarına göre hazırlanmıştır. `.meta` dosyaları özellikle dışarıda bırakılmıştır.

- ArmyManager.cs
- BiomeManager.cs
- Biomevisuals.cs
- Bosshitreceiver.cs
- BossManager.cs
- Bossconfig.cs
- Bullet.cs
- ChunkManager.cs
- Commanderdata.cs
- Damagepopup.cs
- DifficultyManager.cs
- Economyconfig.cs
- EconomyManager.cs
- Enemy.cs
- EnemyHealthBar.cs
- Enemyarchetypeconfig.cs
- EquipmentData.cs
- Equipmentloadout.cs
- Equipmentui.cs
- GameEvents.cs
- GameHUD.cs
- GameOverUI.cs
- Gamestartup.cs
- Gate.cs
- GateData.cs
- Gateconfig.cs
- Gatefeedback.cs
- Gatepoolconfig.cs
- Inventorymanager.cs
- Mainmenuui.cs
- MorphController.cs
- ObjectPooler.cs
- PetData.cs
- Petcontroller.cs
- PlayerController.cs
- PlayerStats.cs
- Progressionconfig.cs
- Runstate.cs
- Savemanager.cs
- SimpleCameraFollow.cs
- Soldierunit.cs
- SpawnManager.cs
- Stageconfig.cs
- Stagemanager.cs
- Tiervisualizer.cs
- Waveconfig.cs
- Weaponarchetypeconfig.cs
- Worldconfig.cs

> Not: Dosya adlarında büyük/küçük harf tutarsızlıkları olabilir. İleride tek bir naming standardına çekmek faydalı olur.

---

## 19. Kod / refactor yönü için kısa rehber

Bu dosya doğrudan kod yazmaz; ama yön verir.

### 19.1 Sağlam kalan parçalar
Bunlar büyük ihtimalle tamamen atılmamalı; audit ile korunmalı:
- ArmyManager
- SaveManager
- GameEvents
- BossManager’ın bazı parçaları
- StageManager / SpawnManager temel akışı
- mevcut SO mantığı

### 19.2 Dönüşmesi beklenen parçalar
- GateData / GateEffectType sistemi
- eski CP odaklı gate etkileri
- Enemy’nin hardcoded init mantığı
- DifficultyManager içindeki player scaling mantıkları
- PlayerStats içindeki eski gate uygulama katmanı

### 19.3 Yeni veri katmanları
- WeaponArchetypeConfig
- EnemyArchetypeConfig
- GateConfig
- GatePoolConfig
- WaveConfig
- BossConfig
- RunState

---

## 20. Claude veya başka bir AI ile çalışırken kullanım notu

Bu dosyayı okuyan AI’dan istenmesi gereken şey:
- projeyi baştan tasarlaması değil
- scope büyütmesi değil
- DDA önermesi değil
- önce audit, sonra migration planı vermesi
- sonra küçük paketler halinde refactor önermesi

### Doğru sıra
1. audit  
2. migration plan  
3. vertical slice odaklı küçük paketler  
4. placeholder’lı ilk çalışan sürüm  
5. test  
6. iyileştirme  

---

## 21. Şu noktadan sonra parkta tutulan fikirler

Bu fikirler kötü değil; sadece şu an MVP dışı:
- Arena
- Challenge mode
- Alliance
- Level editor
- 3-Lane Anchor tam sürümü
- World 2+
- commander talent tree
- ileri rarity / gacha
- battle pass
- season pass
- live events
- server-side yarışmalar
- ağır monetization katmanları

---

## 22. Son karar özeti

Eğer yeni bir konuşma açılırsa, bu projenin özü şudur:
- mobil
- solo-dev
- hybrid-casual / mid-core
- fixed difficulty
- visible tactical gates
- sidegrade silahlar
- armor farkındalığı
- World 1 backbone
- vertical slice = ilk 10 stage
- üretilebilir çekirdek
- scope disiplini

Bu proje “şimdi her şeyi yapan” değil, **şimdi çekirdeği doğru kurup sonra büyüyen** bir oyundur.

# Top End War — Design Patch v1
_Critical Gaps Closure_

## Amaç
Bu patch’in amacı, mevcut tasarım omurgasında açık kalan ama vertical slice öncesi netleşmesi gereken 6 kritik noktayı kapatmaktır:

1. Gate UX  
2. Soldier Contract  
3. Death / Retry Rule  
4. Tutorial Onboarding Flow  
5. Stage 1–10 TargetDPS görünürlüğü  
6. Minimal Audio Placeholder Planı  

Bu patch yeni büyük sistem önermez; yalnızca mevcut yapıyı üretilebilir hale getirir.

---

## 1) Gate UX Contract

### Temel karar
Runner içinde aynı anda **2 kapı** görünür.  
Oyuncu bunlardan birini **karakterini fiziksel olarak sola/sağa yönlendirip içinden geçerek** seçer.

### Yapılmayacaklar
- ayrı seçim menüsü yok
- pop-up karar ekranı yok
- swipe ile ayrı UI seçimi yok
- kapı üstüne uzun açıklama yok

### Görsel sözleşme
Her kapı:
- üstte **ana etki**
- altta **2 kısa etiket**
gösterir.

### Örnek
```text
+12 Zırh Delme
ARMOR • ELITE
```

### UX ilkeleri
- Kapı etkisi 1 saniyede okunmalı
- Oyuncuya “şunu seç” diye doğrudan yön verilmemeli
- Kapı farkı renk + ikon + kısa etiket ile anlaşılmalı
- Stage 1–5’te yalnızca en sade gate’ler görünmeli

### İlk slice kuralı
Vertical slice boyunca yalnızca şu gate tipi deneyimi gerekir:
- Single gate mantığı
- fiziksel geçişle seçim
- kısa görünür etki
- risk/duel sistemleri sonraya veya geç stage’lere kalabilir

---

## 2) Soldier Contract

### Soldier sistemi ne yapar?
Askerler, komutanın yanında veya çevresinde hareket eden, **otomatik destek ateşi** veren yardımcı birimlerdir.

### Yapacakları
- komutanla birlikte ilerlemek
- otomatik saldırmak
- DPS hissini büyütmek
- build ve gate seçimlerini görünür kılmak
- lane baskısını desteklemek

### Yapmayacakları
- oyuncu tarafından tek tek yönetilmeyecekler
- mikro komut almayacaklar
- ayrı “mini RTS” sistemine dönüşmeyecekler
- vertical slice’ta karmaşık formation/patrol mantığı istemeyecekler

### Vertical slice için sade sözleşme
İlk dilimde askerler:
- **otomatik saldırır**
- komutana bağlı kalır
- kaybedilebilir / iyileştirilebilir
- gate ile sayıları artabilir
- gate ile iyileşebilir

### Türler
Tam sistemde 3 aile tanımlı kalır:
- Piyade
- Mekanik
- Teknoloji

Ama vertical slice için davranış farkı **asgari** tutulur.

#### Vertical slice yaklaşımı
- Piyade = aktif ve net çalışan temel asker
- Mekanik / Teknoloji = veri şemasında bulunabilir ama davranışta aşırı derinleşmez
- İlk oynanabilir sürümde ordu davranışı tek çekirdek mantık üstünde çalışabilir

### Özet
Asker sistemi bu aşamada:
**“ekstra taktik mikro yönetim” değil, “build görünürlüğü ve destek gücü”** sistemidir.

---

## 3) Death / Retry Rule

### Temel karar
Ölüm halinde oyuncu:
- run içi geçici build’ini kaybeder
- stage completion ödülünü alamaz
- haritaya veya retry akışına döner

### Ödül sözleşmesi
#### Zaferde
- stage completion reward verilir
- stage clear kabul edilir
- world progress ilerler

#### Yenilgide
- stage completion reward verilmez
- run içi build resetlenir
- stage başarısız sayılır

### Mid-run reward kararı
Collected mid-run micro reward’lar **korunur**.  
Böylece oyuncu tamamen boş dönmez.

### Revive
Vertical slice için en sade karar:
- en fazla **1 revive**
- revive yoksa direkt fail akışı

### Retry akışı
Fail sonrası seçenekler:
- Retry
- Haritaya dön

Bu kadar. Fazla ekran yok.

---

## 4) Tutorial Onboarding Flow

### Temel karar
Stage 1–5 “Tutorial Core” bandıdır.  
Ama tutorial yalnızca stage bandı olarak değil, **oyuncuya neyin ne zaman gösterileceği** olarak da tanımlanmalıdır.

### İlk açılış akışı
Oyuncu ilk oyunu açtığında:
- karmaşık menü görmez
- World 1 / ilk stage erişilebilir olur
- varsayılan loadout hazır gelir
- “başla” kararı hızlı alınır

### Tutorial prensipleri
- Aynı stage içinde en fazla **1 ana yeni fikir**
- Uzun metin yok
- Gerekirse kısa tek satır ipucu
- İpucu sadece ilk kez gösterilir
- oyuncu oynayarak öğrenir

### Stage bazlı onboarding

#### Stage 1
Öğret:
- hareket
- auto-shoot
- basit kapı seçimi

#### Stage 2
Öğret:
- swarm farkı
- çoklu hedef temizliği

#### Stage 3
Öğret:
- charger önceliği
- yaklaşan tehdidi önce vur

#### Stage 4
Öğret:
- sustain / toparlanma gate’i
- army gate’in temel mantığı

#### Stage 5
Öğretmez, sınar:
- Stage 1–4’te görülenlerin küçük birleşimi

#### Stage 6
Öğret:
- armor farkı
- bazı düşmanların normalden daha dayanıklı olduğu

#### Stage 7
Öğret:
- armor pen / pierce gibi çözümlerin değeri

#### Stage 8
Öğret:
- elite tehdidin önceliği

#### Stage 10
Öğretmez, sınar:
- ilk mini-boss savaşı

### Tutorial UI ilkesi
Metin yerine:
- kısa etiket
- hedef işaretleme
- sade vurgulama
- kapı üstü okunurluk
tercih edilir.

---

## 5) Stage 1–10 TargetDPS Table

Aşağıdaki tablo, vertical slice’ın açık balans omurgasıdır.

| Stage | TargetDPS |
|---|---:|
| 1 | 70 |
| 2 | 81 |
| 3 | 94 |
| 4 | 109 |
| 5 | 126 |
| 6 | 142 |
| 7 | 160 |
| 8 | 181 |
| 9 | 205 |
| 10 | 232 |

### Düşman HP hesabı
Enemy HP, ayrı bir “base HP” yerine şu sistemle hesaplanır:

```text
EnemyHP = TargetDPS × HPFactor
```

### Slice enemy factor’ları
- Trooper = 0.90
- Swarm = 0.35
- Charger = 0.65
- Armored Brute = 1.25
- Elite Charger = 3.40

### Örnek hesap
#### Stage 6
- TargetDPS = 142
- Trooper HP = 128
- Brute HP = 178

#### Stage 10
- TargetDPS = 232
- Trooper HP = 209
- Swarm HP = 81
- Charger HP = 151
- Brute HP = 290
- Elite Charger HP = 789

### Boss formülü
Mini-Boss 1 için:

```text
BossHP = TargetDPS × 13
```

Stage 10’da:
- TargetDPS = 232
- Gatekeeper Walker HP ≈ 3016

### Not
Bu tablo, economy, gate value ve boss pacing hesapları için referans alınacaktır.  
Bundan sonra vertical slice içinde “hissettiğimize göre sayı” değil, bu omurgaya göre tuning yapılacaktır.

---

## 6) Minimal Audio Placeholder Plan

### Temel karar
Vertical slice’ta ses “sonradan bakarız” denecek bir konu değildir.  
Vurma hissi ve tehlike okunurluğu için **minimum placeholder audio paketi** zorunludur.

### İlk sürümde gereken ses kategorileri

#### Silah
- Assault fire
- SMG fire
- Sniper fire

#### Vuruş
- normal hit
- armor hit
- crit / güçlü hit

#### Düşman
- death pop / düşüş
- elite spawn cue

#### Gate
- gate seçimi / aktivasyon
- heal / reinforce alımı

#### Boss
- telegraph warning
- phase transition cue
- heavy impact cue
- boss death cue

#### Sistem
- victory
- fail
- UI confirm / click

### Kalite hedefi
İlk aşamada bu seslerin final kalite olması gerekmez.  
Ama:
- ritim doğru olmalı
- birbirinden ayırt edilebilir olmalı
- gameplay feedback’e hizmet etmeli

### Kural
“Şimdilik sessiz test” yapılmayacak.  
Placeholder ses bile olsa kullanılacak.

---

## 7) Park / Non-Canonical Systems Note

Aşağıdaki sistemler bu patch ile **aktif çekirdeğin dışında** kabul edilir.  
Kodbase’de dosyaları bulunabilir, ama vertical slice’ın zorunlu aktif tasarım parçası sayılmazlar:

- Pet sistemi
- Morph sistemi
- Tier görselleştirme kalıntıları
- tam equipment/CP legacy loop’ları
- arena
- challenge
- alliance
- level editor
- world 2+
- liveops / server rekabeti

### Kural
Kodbase’de bulunmaları, onları otomatik olarak aktif tasarım parçası yapmaz.  
Bir sistem yalnızca tasarımda **yeniden açıkça aktive edilirse** MVP kapsamına girer.

---

## 8) Sonuç

Bu patch ile şu boşluklar kapanmış sayılır:
- Gate UX net
- Soldier Contract net
- Death / Retry net
- Tutorial onboarding net
- TargetDPS görünür
- Audio placeholder kararı net

Bu noktadan sonra vertical slice için tasarım tarafında yeni büyük sistem açmak yerine:
- config
- implementation
- test
- tuning

akışına geçilmelidir.
