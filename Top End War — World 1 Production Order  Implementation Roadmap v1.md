# Top End War — World 1 Production Order / Implementation Roadmap v1
_From Canon to Playable_

---

## 0. Amaç

Bu belge, artık tasarım konuşmalarından çıkıp **uygulanabilir üretim sırasını** netleştirir.

Amaç:
- neyi önce yapacağımızı kilitlemek
- tasarım → Unity hookup → implementasyon → test sırasını netleştirmek
- tekrar başa dönme riskini azaltmak
- World 1’i parça parça değil, kontrollü paketlerle ayağa kaldırmak

### Kural
Bu roadmap:
- her şeyi aynı anda yap demek değildir
- küçük, test edilebilir üretim paketleri tanımlar
- her faz sonunda oyun daha oynanabilir hale gelmelidir

---

## 1. Üretim felsefesi

### Ana kural
Önce:
- görünür akış
- okunur combat
- bağlı veri
- test edilebilir sahne

Sonra:
- tuning
- polish
- genişletme

### Yasak
- 5 sistemi aynı anda refactor etmek
- sahne bağlantıları yokken combat tuning’e gömülmek
- localization’ı sona bırakmak
- placeholder’sız final kalite beklemek

---

## 2. Fazlar

---

## Faz 1 — Canon Freeze
### Hedef
World 1 kanonunu kilitlemek.

### Çıktılar
- Unified World 1 Foundation Bible
- Legacy Classification & Cleanup Sheet
- bu roadmap

### Bitti sayılma koşulu
- artık “ne yapıyoruz?” sorusu kalmayacak
- sadece “hangi sırayla yapıyoruz?” kalacak

---

## Faz 2 — Data & Localization Foundation
### Hedef
Kod yazmadan önce veri katmanını ve metin katmanını güvenli hale getirmek.

### Yapılacaklar
- WeaponArchetypeConfig alanlarını netleştir
- EnemyArchetypeConfig alanlarını netleştir
- GateConfig text key alanlarını ekle
- StageConfig name/threat key alanlarını netleştir
- EquipmentData → weaponArchetype referansını aktif kullan
- localization key standardını başlat
- EN + TR temel tabloyu aç

### Çıktılar
- localization-ready static data
- hardcoded metin kullanımını azaltacak temel yapı
- Unity’de oluşturulması gereken SO’ların net listesi

### Faz sonunda test
- archetype asset’leri oluşmuş mu
- EquipmentData referansları bağlı mı
- gate/stage/enemy/weapon text key alanları hazır mı

---

## Faz 3 — Screen Flow Skeleton
### Hedef
Combat’e girmeden önce oyunun boş hissettiren ekran eksiklerini kapatmak.

### Öncelik ekranları
1. Main Menu  
2. World Map  
3. Stage Card  
4. Loadout  
5. Runner HUD  
6. Result / Reward / Upgrade

### Yapılacaklar
- placeholder ama çalışan ekran akışı kur
- menüden stage başlatılabilsin
- run sonunda map’e dönüş çalışsın
- Stage Threat Tags ve Build Snapshot placeholder olarak görünsün

### Çıktılar
- “ekransız prototip” hissi biter
- oyuncu ne oynadığını ve neden oynadığını anlamaya başlar

### Faz sonunda test
```text
Main Menu
→ World Map
→ Stage Card
→ Loadout
→ Runner
→ Result
→ Reward
→ Upgrade
→ World Map
Faz 4 — Runner Readability Pass
Hedef

Runner sahnesini okunur ve test edilebilir hale getirmek.

Yapılacaklar
kamera framing
gate okunurluğu
HUD temel elemanları
commander HP
soldier summary
boss overlay placeholder
telegraph alanları
portrait ratio test
Çıktılar
gate okuyabiliyor muyuz?
düşmanı fark ediyor muyuz?
ekrandan taşma azalıyor mu?
boss geldiği an hissediliyor mu?
Faz sonunda test
9:16 görünüm
gate okuma süresi
HUD clutter
boss bar alanı
soldier görünürlüğü
Faz 5 — Combat Core Stabilization
Hedef

Şu anki mevcut combat omurgasını kırmadan World 1’e uygun hale getirmek.

Öncelikli sistemler
PlayerController
Bullet
Enemy
SpawnManager
ArmyManager
SoldierUnit
Yapılacaklar
mevcut çalışan yapı korunur
küçük patch’lerle düzeltilir
target, damage, armor, elite, soldier support temel akışı netleştirilir
CP merkezli combat dilinin etkisi azaltılır
hardcoded combat mantıkları archetype/gate diline yaklaştırılır
Çok önemli kural

Bu fazda:

büyük rewrite yok
küçük patch var
önce compile, sonra davranış
Çıktılar
çalışan, çökmeyen, test edilebilir combat çekirdeği
Faz 6 — Enemy & Spawn Behavior Pass
Hedef

Enemy’leri “HP kutusu” olmaktan çıkarmak.

Yapılacaklar
Trooper / Swarm / Charger / Brute / Elite Charger davranış kontratlarını uygula
spawn pattern setlerini uygula
ip gibi gelen wave hissini kır
delayed threat / guarded core / dense core gibi pattern’leri sahaya taşı
telegraph okunurluğunu artır
Çıktılar
düşman farkı gerçekten hissedilir
silah rolleri görünür hale gelir
gate solve değeri anlaşılır olur
Faz sonunda test
swarm gerçekten swarm mı?
charger okunuyor mu?
brute armor check gibi hissediyor mu?
elite spike adil mi?
wave’ler artık düz sıra gibi görünmüyor mu?
Faz 7 — Build & Gate Economy Pass
Hedef

Build’i yalnızca sayısal değil, karar odaklı hale getirmek.

Yapılacaklar
gate ailelerini aktif sisteme bağla
solve gate’lerin gerçek kullanım değerini test et
reinforce / sustain / power / tempo dengesini kur
build snapshot mantığını UI’de görünür yap
commander + squad + gate + stage knowledge zincirini çalıştır
Çıktılar
“hangi gate neden iyi?” sorusunun cevabı görünür olur
yanlış kapı seçimi hissedilir
build çeşitliliği başlar
Faz 8 — World 1 Teaching Order Pass
Hedef

Stage 1–35 öğretim sırasını çalışır hale getirmek.

Yapılacaklar
1–5 Tutorial Core akışı
6–10 Build Discovery akışı
11–35 bantlarını placeholder bile olsa stage planına oturt
Beam tanıtım ve boss prep akışını yerleştir
War Machine / Final Boss öğretim zincirini planla
Çıktılar
World 1 artık sadece “ilk 10 stage prototipi” değil
gerçek öğretim omurgası olan bir dünya olur
Faz 9 — Reward / Progress / Retry Pass
Hedef

Combat sonrası akışı tamamlamak.

Yapılacaklar
fail → retry / map
victory → reward → upgrade → map
mid-run reward keep kuralı
stage completion reward
first clear mantığı
progression hissi
Çıktılar
run kazanmak ve kaybetmek anlamlı hale gelir
oyun loop’u kapanır
Faz 10 — Audio & Feedback Placeholder Pass
Hedef

Sessiz ve cansız prototip hissini bitirmek.

Yapılacaklar
Assault / SMG / Sniper fire
normal hit / armor hit / güçlü hit
elite spawn cue
gate seçim sesi
boss telegraph sesi
fail / victory / UI click
Kural

Final kalite gerekmez.
Ama sessiz test bırakılmaz.

3. İş sırası — kısa versiyon
Hemen şimdi
Canon freeze
Data + localization foundation
Screen flow skeleton
Sonra
Runner readability
Combat core stabilization
Enemy & spawn behavior
Sonra
Build & gate economy
World 1 teaching order
Reward/progression
Audio placeholder
4. Hangi fazda gerçekten “işleme geçmiş” olacağız?
Minimum “artık gerçekten implementasyondayız” eşiği

Aşağıdaki 3 şey tamam olunca:

Data & localization foundation
Screen flow skeleton
Runner readability

Bu üçü tamamlanınca artık tasarımda değil, doğrudan üretim ve test modunda oluruz.

Pratik cevap

İşleme geçmek için kalan:

1 data/localization setup turu
1 screen flow hookup turu

Yani:
çok az kaldı.

5. Tahmini yakınlık
Bugünkü durum
Tasarım omurgası: güçlü
Üretim sırası: netleşiyor
Kod temeli: var ama patch istiyor
Scene/UI hookup: eksik ama çözülebilir
Localization: tam doğru anda ele alınıyor
Genel his

World 1 için “kafada netleşme” kısmının büyük bölümü bitti.
Şu andan sonra yapılan iş:

tasarım icadı değil
üretim sırası ile yerine oturtma işi
6. Fazlara göre başarı ölçütü
Faz 2 başarılıysa
localization-ready data oluşur
SO yapısı güvenli hale gelir
Faz 3 başarılıysa
oyun artık boş hissettirmez
oyuncu nerede olduğunu anlar
Faz 4 başarılıysa
gate ve combat okunur hale gelir
Faz 5 başarılıysa
combat stabil ve test edilebilir olur
Faz 6 başarılıysa
oyun ilk kez gerçekten “eğlenceli” his vermeye başlar
Faz 7 başarılıysa
build kurmak anlamlı hale gelir
Faz 8 sonrası
World 1 artık bütünlüklü bir oyun omurgası olur
7. Riskler
Risk 1

Combat koduna çok erken gömülmek

ekran akışı eksik kalır
oyun yine boş hisseder
Risk 2

Localization’ı ertelemek

hardcoded string çöplüğü oluşur
Risk 3

Enemy davranışı yazılmadan sadece HP tuning yapmak

ip gibi wave hissi geri döner
Risk 4

Soldier sistemini World 1’de gereğinden fazla mikroya çevirmek

üretim hızını bozar
oyunun kimliğini bulanıklaştırır
8. Güzel ek not
Build Snapshot + Threat Tags

Bunlar roadmap’in erken fazına alınmalı.
Çünkü:

tasarımla UI’yi bağlar
oyuncuya açık taktik vermeden yön verir
stage ve loadout ekranını çok değerli hale getirir
Localization short-tag discipline

Gate alt etiketlerini şimdiden kısa yaz.
Bu, iki dilde de çok iş kurtarır.

9. Son karar özeti

World 1 üretim sırası:

önce veri ve akış
sonra okunurluk
sonra combat stabilizasyonu
sonra enemy/spawn davranışı
sonra build/gate ekonomisi
sonra progression/polish