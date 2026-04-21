# Top End War — Legacy Classification & Cleanup Sheet v1
_Keep / Transform / Freeze / Remove Later_

---

## 0. Belgenin amacı

Bu belge, projedeki mevcut sistemleri ve scriptleri 4 kategoriye ayırır:

- **Keep**
- **Transform**
- **Freeze**
- **Remove Later**

Amaç:
- aktif World 1 çekirdeğini legacy kalıntılardan ayırmak
- hangi sistemi hemen kullanacağımızı, hangisini dönüştüreceğimizi netleştirmek
- “kodbase’de var ama oyunun aktif parçası mı?” sorusunu net cevaplamak
- cleanup yaparken çalışan omurgayı kazara bozmamak

### Kural
Bir sistemin projede bulunması, onun aktif tasarım parçası olduğu anlamına gelmez.

---

## 1. Kategori tanımları

## 1.1 Keep
Şimdilik doğru omurga.  
Korunur, yalnızca küçük polish / entegrasyon alır.

## 1.2 Transform
Temel fikir doğrudur ama mevcut hali legacy, eksik veya yanlış bağlıdır.  
Çöpe atılmaz; yeni World 1 kanonuna göre dönüştürülür.

## 1.3 Freeze
Kodbase’de kalabilir ama aktif çekirdek değildir.  
UI’de, ana flow’da ve üretim önceliğinde görünmez.

## 1.4 Remove Later
Şu an dokunmak şart değildir ama ileride:
- sadeleştirme
- dosya birleştirme
- isim standardı
- gereksiz tekrarları temizleme
için hedeflenir.

---

## 2. KEEP

## 2.1 Çekirdek akış omurgası
- `StageManager.cs`
- `Runstate.cs`
- `GameEvents.cs`
- `Savemanager.cs`
- `Gamestartup.cs`

### Neden keep
Bunlar oyunun sahne akışı, run yönetimi ve genel omurgası için çekirdek taşıyıcıdır.

### Not
İsim standardı ve bağlama temizliği gerekebilir ama sistem olarak korunurlar.

---

## 2.2 Combat omurgası
- `PlayerController.cs`
- `Bullet.cs`
- `ObjectPooler.cs`
- `BossManager.cs`
- `Bosshitreceiver.cs`

### Neden keep
Temel hareket, atış, mermi ve boss çağrı omurgası zaten var.  
Bunlar sıfırdan yazılacak ilk şeyler değil.

### Not
Refactor gerekebilir ama “çalışan çekirdek” kabul edilir.

---

## 2.3 Static data omurgası
- `Stageconfig.cs`
- `Waveconfig.cs`
- `Gateconfig.cs`
- `Gatepoolconfig.cs`
- `Bossconfig.cs`
- `Enemyarchetypeconfig.cs`
- `Weaponarchetypeconfig.cs`
- `Worldconfig.cs`
- `Economyconfig.cs`
- `Progressionconfig.cs`

### Neden keep
SO tabanlı veri mimarisi doğru yönde.  
Bu projede static design data omurgası olarak korunmalı.

### Not
Alanlar güncellenebilir ama veri katmanı mantığı korunur.

---

## 2.4 HUD / ekran iskeletleri
- `GameHUD.cs`
- `Mainmenuui.cs`
- `GameOverUI.cs`
- `Equipmentui.cs`
- `Equipmentloadout.cs`
- `Inventorymanager.cs`

### Neden keep
Tam doğru UI akışı henüz kurulmamış olsa da bunlar yeni screen flow için kullanılabilecek temel iskeletlerdir.

### Not
Doğrudan final hali gibi kabul edilmez; ama çöpe de atılmaz.

---

## 2.5 Görsel / yardımcı temel parçalar
- `SimpleCameraFollow.cs`
- `EnemyHealthBar.cs`
- `Damagepopup.cs`
- `Gatefeedback.cs`
- `BiomeManager.cs`
- `Biomevisuals.cs`

### Neden keep
Bunlar oynanış okunurluğu ve sahne hissi için destekleyici, düşük riskli, faydalı parçalardır.

---

## 3. TRANSFORM

## 3.1 Army / Soldier sistemi
- `ArmyManager.cs`
- `Soldierunit.cs`

### Neden transform
Temel fikir doğru:
- asker desteği
- build görünürlüğü
- reinforce / heal / merge / support layer

Ama mevcut hali World 1’de:
- bazen fazla düz
- bazen fazla erken derinleşmiş
- bazen de UI/formation/hookup eksikliği yüzünden yanlış hissediyor

### Hedef dönüşüm
- soldier layer = mikro RTS değil
- squad support = build’in görünür parçası
- formation = okunur, kompakt, ekran dostu
- soldier role = world 1’e uygun derinlikte
- tek tek asker envanteri yerine support/preset mantığı

---

## 3.2 Enemy sistemi
- `Enemy.cs`

### Neden transform
Bu dosya aktif çekirdeğin kalbinde ama hâlâ:
- hardcoded init izleri
- legacy reward/combat dili
- behavior sözleşmesi eksikleri
- enemy distinction zayıflığı
taşıyor.

### Hedef dönüşüm
- archetype-driven behavior
- armor / elite / threat ayrımı
- readability contract
- reservation / targeting uyumu
- HP kutusu değil karar testi gibi davranan düşman

---

## 3.3 Spawn sistemi
- `SpawnManager.cs`

### Neden transform
Temel omurga doğru:
- gate spawn
- wave spawn
- stage wave sequence
- archetype + targetDps ilişkisi

Ama mevcut hali:
- bazı fallback wave’lerde fazla düz
- ip gibi sıra hissi üretebiliyor
- behavior grammar tam değil

### Hedef dönüşüm
- pattern-driven wave grammar
- jitter / delay / support pack dili
- enemy behavior ile uyumlu spawn kompozisyonu
- World 1 band bazlı öğretim desteği

---

## 3.4 Player stats / progression combat dili
- `PlayerStats.cs`
- `DifficultyManager.cs`
- `EconomyManager.cs`

### Neden transform
Bu dosyalarda legacy combat dili, CP/gate/scale kalıntıları ve eski soyut progresyon izleri olabilir.

### Hedef dönüşüm
- combat dili = silah / gate / enemy / stage knowledge
- fixed difficulty çizgisine uyum
- gereksiz player scaling kalıntılarını temizleme
- reward ve economy’yi World 1 kanonuna uyarlama

---

## 3.5 Gate sistemi
- `Gate.cs`
- `GateData.cs`
- `Gateconfig.cs`
- `Gatepoolconfig.cs`

### Neden transform
Gate sistemi aktif çekirdekte kalacak, ama:
- eski gate effect dili
- hardcoded / legacy isimlendirme
- localization eksikliği
- yeni gate economy’ye uyumsuzluk
taşıyabilir.

### Hedef dönüşüm
- GateConfig merkezli sistem
- kısa okunur gate UX
- family / effect / tags / stage band görünürlüğü
- localization-ready metin yapısı

### Not
`GateData.cs` büyük ihtimalle en fazla dönüşecek dosyalardan biri.

---

## 3.6 Equipment sistemi
- `EquipmentData.cs`
- `Equipmentloadout.cs`
- `Equipmentui.cs`

### Neden transform
Doğru yön şu:
- `WeaponArchetypeConfig` = silah family verisi
- `EquipmentData` = item / modifier katmanı

Bu ayrım yeni yeni oturuyor.

### Hedef dönüşüm
- equipment = item kimliği + modifier
- archetype = silahın doğası
- commander loadout ekranı = build snapshot’a uygun akış
- localization-ready item sunumu

---

## 3.7 UI flow / navigation
- `Mainmenuui.cs`
- `GameHUD.cs`
- `GameOverUI.cs`
- muhtemel map/stage geçiş scriptleri

### Neden transform
Mevcut parçalar var ama tam screen flow henüz kanonik sıraya oturmamış olabilir.

### Hedef dönüşüm
- Main Menu
- World Map
- Stage Card
- Loadout
- Runner
- Result
- Reward
- Upgrade
- geri dönüş zincirini tam bağlamak

---

## 3.8 Localization hazırlığı
- tüm UI metin kaynakları
- GateConfig metinleri
- StageConfig adları
- Weapon/Enemy display text alanları

### Neden transform
Şu an büyük ihtimalle bazı metinler hardcoded veya tek dil odaklı.

### Hedef dönüşüm
- key-based localization
- EN + TR başlangıcı
- SO text alanlarında key standardı
- text expansion dayanıklılığı

---

## 4. FREEZE

## 4.1 Pet sistemi
- `PetData.cs`
- `Petcontroller.cs`

### Neden freeze
World 1 çekirdeğinin zorunlu parçası değil.  
Kodbase’de kalabilir ama aktif üretim önceliği değil.

---

## 4.2 Morph sistemi
- `MorphController.cs`

### Neden freeze
Ana combat / build / world akışının parçası değil.  
Şimdilik görünmez tutulmalı.

---

## 4.3 Tier görselleştirme / eski görsel katmanlar
- `Tiervisualizer.cs`

### Neden freeze
Aktif World 1 kanonunun merkezi değil.  
Legacy görsel kalıntı gibi ele alınmalı.

---

## 4.4 Arena / challenge / alliance / editor yönü
Kod tarafında tam karşılığı şimdi görünmese de tasarım olarak freeze kabul edilir:
- Arena
- Challenge Mode
- Alliance
- Level Editor
- World 2+
- LiveOps / server rekabeti

### Neden freeze
Çekirdeği boğar.  
World 1 foundation tamamlanmadan aktif edilmez.

---

## 4.5 Ek / yan sistemler
- `Commanderdata.cs` içindeki World 1 dışı karmaşık planlar
- ileri meta veya monetization düşünceleri
- ağır rarity / collection dalları

### Neden freeze
Aktif üretim önceliği değil.

---

## 5. REMOVE LATER

## 5.1 Naming cleanup
Dosya adlarında standart dışı kalanlar:
- `Weaponarchetypeconfig.cs`
- `Enemyarchetypeconfig.cs`
- `Soldierunit.cs`
- `Bosshitreceiver.cs`
- `Worldconfig.cs`
- `Stageconfig.cs`
- `Gateconfig.cs`
- `Gatepoolconfig.cs`
- `Savemanager.cs`

### Neden remove later
Şimdi kırmak istemiyoruz.  
Ama ileride PascalCase ve class/file eşleşmesi temizlenmeli.

---

## 5.2 Tekrar eden / eski isimli veri yapıları
- `GateData.cs` varsa ve `GateConfig` ile rol çakışıyorsa
- eski CP / eski gate effect enum kalıntıları
- eski debug/fallback alanları

### Neden remove later
Önce yeni kanon çalışsın, sonra eski veri kalıntıları temizlenir.

---

## 5.3 Eski combat dili kalıntıları
- CP merkezli savaş açıklamaları
- oyuncuya açık olmayan ama koda gömülü eski progression dili
- World 1 combat kararlarına hizmet etmeyen sayı odaklı kalıntılar

### Neden remove later
Bunlar sessizce kafa karıştırır.  
Ama önce replacement sistem net olmalı.

---

## 5.4 Fallback/prototype kodu
- geçici procedural wave fallback’leri
- placeholder init mantıkları
- sadece null güvenliği için eklenmiş ama kalıcılaşmış bloklar

### Neden remove later
İlk çalışan sürüm için yararlılar.  
Ama kanonik sistem oturunca azaltılmalı.

---

## 5.5 Kullanılmayan UI parçaları
- sahnede bağlı olmayan paneller
- eski sonuç ekranı varyantları
- test için açılmış ama aktif akışta olmayan UI’lar

### Neden remove later
Önce gerçek flow bağlanmalı, sonra UI hurdası temizlenmeli.

---

## 6. World 1 için öncelikli cleanup sırası

### Faz 1 — Aktif çekirdeği sabitle
- ArmyManager
- Soldierunit
- Enemy
- SpawnManager
- PlayerStats
- Gate sistemi
- Weapon/Equipment ayrımı

### Faz 2 — Akışı görünür kıl
- Main Menu
- World Map
- Stage Card
- Loadout
- Runner HUD
- Result / Reward / Upgrade

### Faz 3 — Localization-ready yap
- UI key standardı
- Gate / Stage / Weapon / Enemy text key alanları
- EN / TR başlangıç tabloları

### Faz 4 — Freeze uygula
- pet
- morph
- tier visuals
- arena/challenge/editor yönü
- World 2+ fikirleri

### Faz 5 — Remove later temizliği
- naming standard
- duplicate/legacy data
- kullanılmayan UI
- eski fallback blokları

---

## 7. Küçük uyarılar

### Uyarı 1
Freeze edilen sistem sahnede görünmeye devam ederse oyuncu scope dağınıklığını hisseder.

### Uyarı 2
Transform gereken sistemleri yanlışlıkla keep sayarsak legacy davranışlar “doğru sistemmiş” gibi kalır.

### Uyarı 3
Localization’ı freeze gibi görmek hata olur.  
Bu hemen düşünülmeli.

### Uyarı 4
Army / Soldier / Enemy / Spawn tarafını aynı anda baştan yazmak yine bizi gereksiz yorabilir.  
Önce sözleşme, sonra küçük kontrollü patch mantığı korunmalı.

---

## 8. Son karar özeti

### KEEP
Çalışan omurga, veri mimarisi ve temel ekran/script iskeletleri

### TRANSFORM
World 1 kanonuna uyacak şekilde dönüşecek aktif çekirdek sistemler

### FREEZE
Kodbase’de kalıp aktif oyuna girmeyecek sistemler

### REMOVE LATER
Yeni temel oturduktan sonra temizlenecek legacy / tekrar / isim / prototype kalıntıları