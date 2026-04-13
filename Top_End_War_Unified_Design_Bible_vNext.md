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
