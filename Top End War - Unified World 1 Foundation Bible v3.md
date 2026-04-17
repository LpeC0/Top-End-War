# Top End War — Unified World 1 Foundation Bible v3
_Combat, Build, Enemy, Spawn, UI, Localization, Unity Hookup & Migration_

---

## 0. Belgenin statüsü

Bu belge:
- şu anki en doğru **World 1 kanonik tasarım şemasıdır**
- testten önce doğru kabul edilir
- testten sonra bilinçli olarak revize edilebilir
- kod, config, UI ve Unity kurulumunun ortak referansıdır
- “asla değişmez yasa” değil, **current canon** dokümanıdır

### Kural
Burada yazan bir karar:
- testten önce doğru kabul edilir
- test sonrası gerekirse değiştirilir
- değişene kadar ekip ve kod tarafı aynı dili konuşur

---

## 1. Bu belgenin amacı

Bu belge, Top End War için World 1’in:
- combat kimliğini
- build dilini
- gate ekonomisini
- enemy davranış ve spawn grammar’ını
- ekran akışını
- localization yaklaşımını
- Unity hookup ve migration ihtiyaçlarını

tek yerde toplar.

### Amaç
- “bir şeyleri yapıp sonra geri dönelim” döngüsünü azaltmak
- World 1’i oyunun gerçek öğretim alanı olarak kurmak
- vertical slice’ı tasarım sınırı değil, üretim checkpoint’i olarak konumlamak
- legacy ile aktif çekirdeği ayırmak
- kod yazmadan önce neyi neden kurduğumuzu netleştirmek

---

## 2. Kırmızı çizgiler

### 2.1 Zorluk
- DDA yok
- oyuncuya göre gizli düşman ölçekleme yok
- stage zorluğu stage’e bağlıdır
- oyuncu güçlendikçe eski içeriği daha rahat ezebilmelidir

### 2.2 Oynanış dili
Combat dili:
- CP merkezli soyut sayı dili değil
- silah rolü
- gate seçimi
- düşman tipi
- armor farkındalığı
- stage bilgisi
üzerinden okunur

### 2.3 Silah felsefesi
Silahlar:
- strict upgrade değil
- sidegrade mantığındadır
- farklı problemleri çözer

### 2.4 Gate felsefesi
- görünür olacak
- kısa okunacak
- taktik düşündürecek
- doğrudan “şunu seç” demeyecek
- run boyunca taşınan kalıcı etkiler verecek

### 2.5 Boss felsefesi
- boss yeni mekanik öğretmez
- önce öğretileni sınar
- et duvarı değildir
- ayrı shield dili yoktur
- HP + Armor + behavior + telegraph ile okunur

### 2.6 Scope disiplini
- önce üretilebilir çekirdek
- sonra genişleyen sistemler
- her iyi fikir hemen üretime girmez
- ama doğru fikirler bible’a backlog olarak girer

---

## 3. World 1’in görevi

World 1, yalnızca ilk 10 stage’lik bir demo alanı değildir.  
World 1, oyunun tüm ana oynanış dilini öğretir.

### World 1 oyuncuya şunları öğretmelidir
- hangi silah neyi çözer
- build yalnızca gate’ten oluşmaz
- commander + soldier + gate + stage knowledge birlikte çalışır
- düşmanlar sadece HP kutusu değildir; karar testidir
- boss öncesi hazırlık anlamlıdır
- tek doğru build yoktur

### World 1 sonunda oyuncu şunu anlamış olmalı
- hangi düşmanın neden tehlikeli olduğu
- hangi gate’in hangi build’i büyüttüğü
- armor / elite / swarm / lane baskısı ne demek
- soldier desteğinin build içinde ne işe yaradığı
- boss öncesi Beam gibi çözümlerin neden iyi ama zorunlu olmadığı

---

## 4. World 1 backbone

### 1–5 Tutorial Core
- hareket
- auto-shoot
- basic gate reading
- Assault ve SMG farkının ilk hissi
- soldier desteğinin ilk görünümü
- sustain neden vardır

### 6–10 Build Discovery
- armor
- brute çözümü
- sniper değeri
- elite önceliği
- ilk mini-boss

### 11–15 First Friction
- güvenli build her şeyi çözmez
- yakın risk / reward
- Shotgun tanıtımı
- Mekanik destek fikri

### 16–20 Controlled Complexity
- Launcher tanıtımı
- geometry / pack punishment
- daha karışık wave kompozisyonu

### 21–25 Specialization
- build yönü seçme
- squad desteği daha görünür olur
- solve gate’lerin gerçek değeri netleşir

### 26–30 Pressure & Punishment
- yanlış build daha görünür cezalandırılır
- sustain ve tactical gate değeri artar
- “her şeye assault” güveni azalır

### 31–34 Final Prep
- final bossa hazırlık
- Beam tanıtımı
- Beam’in boss tipi hedeflere neden iyi olduğu
- ama zorunlu olmadığı
- son loadout kararı

### 35 Final Boss
- tüm World 1 bilgisinin sınavı

---

## 5. Combat identity

## 5.1 Silah aileleri

### Assault
Rol:
- güvenli genelci
- mixed wave çözümü
- yüksek güvenilirlik
- “asla kötü değil” silahı

Güçlü:
- karışık kompozisyon
- baseline DPS
- güvenli öğrenme

Zayıf:
- çok ağır armor kontrolü
- aşırı yüksek tek hedef burst
- saf lane wipe

---

### SMG
Rol:
- swarm temizliği
- yüksek tempo
- lane baskısı
- yakın-orta menzil akış silahı

Güçlü:
- çok hedef
- hızlı tehdit temizleme
- tempo buildleri

Zayıf:
- brute / heavy armor
- boss uzun dövüşü
- yüksek tek hedef burst

---

### Sniper
Rol:
- elite / brute / mini-boss çözümü
- armor farkındalığı
- sabırlı ama güçlü hedef temizleme

Güçlü:
- armor
- elite
- yüksek öncelikli hedef

Zayıf:
- swarm
- sürekli yakın baskı
- lane spam

---

### Shotgun
Rol:
- yakın alan patlayıcı güç
- riskli ama tatmin edici çözüm
- ön sıra kırıcı

Güçlü:
- sıkışık pack
- yakın baskı
- mekanik asker sinerjisi

Zayıf:
- uzak tehdit
- dağınık dalga
- güvenli boss DPS

World 1 ilk tam tanıtım bandı:
- 11–15

---

### Launcher
Rol:
- alan hasarı
- pack punish
- kontrollü gecikmeli güç

Güçlü:
- kümeli düşmanlar
- support pack arkası
- guarded core dış katmanları

Zayıf:
- tek hızlı hedef
- charger tipi ani tehdit
- sürekli yakın tepki

World 1 ilk tam tanıtım bandı:
- 16–20

---

### Beam
Rol:
- sürekli baskı
- elite / boss çözümü
- final prep silahı

Güçlü:
- elite
- mini-boss
- final boss tipi hedefler
- uzun temas süresi isteyen hedefler

Zayıf:
- geniş swarm
- anlık lane wipe
- yanlış pozisyonda kalan oyuncu

### Beam öğretim kuralı
Beam, World 1 final bossundan önce tanıtılır.  
Ama final boss için zorunlu cevap değildir.

### Beam öğretim akışı
- önce güvenli karşılaşmada gösterilir
- sonra boss-benzeri hedefte kuvveti öğretilir
- bir önceki mini-boss / pre-boss sınavında parlatılır
- final boss öncesi seçim oyuncuya bırakılır

---

## 5.2 Silah tasarım ilkesi

Silahlar:
- strict upgrade değildir
- sidegrade mantığındadır
- kağıt üstünde yakın DPS taşıyabilir
- gerçek güçleri farklı problemlerde ortaya çıkar

### Denge kuralı
Bir silah:
- her durumda en iyi olamaz
- ama kendi problem alanında açıkça parlamalıdır

---

## 6. Build language

Build yalnızca gate’ten oluşmaz.

World 1 build dili 5 katmanlıdır:

### 1. Commander Layer
Komutanın:
- ana silah ailesi
- ekipmanları
- modifier’ları
- solve kapasitesi

### 2. Soldier Layer
Askerlerin:
- chassis tipi
- destek silah yönü
- sayısı
- destek rolü

### 3. Squad Equipment Layer
Asker desteği build’e katılır.  
Ama World 1’de bu **tek tek asker envanteri** gibi mikro yönetim olmaz.

### World 1 kuralı
Asker ekipmanı / silah yönü:
- squad preset
- support slot
- support archetype
mantığıyla okunmalıdır

Tek tek 8 askerin ayrı item ekranı World 1 için gereksiz mikro olur.

### 4. Run Layer
Run içinde alınan:
- gate etkileri
- reinforce
- sustain
- tactical solve
- boss prep kararları

### 5. Stage Knowledge Layer
Oyuncunun:
- hangi stage’in ne sorduğunu
- hangi tehdidin geldiğini
- build’inin neden iyi/kötü kaldığını
anlaması

---

## 6.1 Build formülü

Bir build’in gerçek gücü:

**Commander Weapon**  
+ **Commander Equipment Modifiers**  
+ **Soldier Support / Squad Preset**  
+ **Gate Effects**  
+ **Stage Knowledge**

### Sonuç
Gate çok önemlidir ama tek başına build değildir.

---

## 6.2 Soldier contract

Askerler:
- otomatik destek ateşi verir
- komutana bağlı hareket eder
- mikro yönetim istemez
- build görünürlüğünü büyütür
- run boyunca destek katmanı gibi çalışır

### Soldier chassis
- Piyade = dengeli destek
- Mekanik = ön sıra / yakın baskı desteği
- Teknoloji = arka sıra / özel hedef desteği

### World 1 kullanım kuralı
- erken oyun: Piyade baskın
- orta oyun: Mekanik ve Teknoloji görünür olur
- geç oyun: oyuncu squad yönünün etkisini hisseder

---

## 6.3 Eğlenceli buildler / özel sinerjiler

World 1’de aşağıdaki tipte buildler mümkün olmalı:

- Assault + Tempo + Reinforce = güvenli tempo ordusu
- SMG + Army + sustain = lane baskı sürüsü
- Sniper + Breacher + Elite damage = armor/elite avcısı
- Shotgun + Mekanik = yakın alan cezalandırıcı
- Launcher + Geometry/Tactical = kümeli wave cezalandırma
- Beam + Teknoloji + Boss Prep = geç oyun / boss hazırlığı

### Küçük eğlenceli sistem fikirleri
Bu fikirler World 1’de kontrollü şekilde açılabilir:
- **Kill Chain**: ardışık kill’lerde kısa tempo artışı
- **Mark Target**: teknoloji/sniper hedef işaretler, takım bonus vurur
- **Breach Armor**: mekanik/shotgun yakın temasla armor verimini düşürür
- **Overclock Window**: beam veya SMG kısa süreli delilik penceresi açar
- **Execution Bonus**: düşük can / elite üstünde ekstra verim

### Kural
Bu fikirler:
- tek doğru meta yaratmamalı
- ama “oha güzel hissettirdi” anları üretmeli

---

## 7. Gate economy

## 7.1 Gate aileleri

- Power
- Tempo
- Penetration
- Geometry
- Army
- Sustain
- Tactical
- Boss Prep

---

## 7.2 Gate güç bantları

### Minor
- yaklaşık %4–6 gerçek katkı
- erken öğretim için iyi

### Standard
- yaklaşık %8–12 gerçek katkı
- World 1 omurgası

### Solve
- her yerde iyi değil
- doğru senaryoda çok güçlü
- yaklaşık %0–35 efektif fark

### Army
- direkt DPS gibi görünmeyebilir
- ama savaş gücü ve hata toleransı sağlar
- yaklaşık %6–18 efektif katkı

### Sustain
- doğrudan DPS vermez
- hata affı ve süreklilik verir

### Boss Prep
- final prep bandında görünür
- final boss öncesi hazırlık değeri taşır

---

## 7.3 Gate yazım formatı

Her gate şu formatta tanımlanmalı:

- Etki
- Gizli tasarım amacı
- En iyi sinerji
- Zayıf kullanım
- İlk görünme bandı
- Güç bandı

---

## 7.4 World 1 gate örnekleri

### Hardline
- Etki: +8% Silah Gücü
- Amaç: en sade genel güç kapısı
- En iyi sinerji: Assault / Sniper
- Zayıf kullanım: çözüm üretmez, sadece sayıyı artırır
- İlk band: 1–5
- Güç bandı: Standard

### Overclock
- Etki: +10% Ateş Hızı
- Amaç: tempo öğretmek
- En iyi sinerji: SMG / Assault
- Zayıf kullanım: armor sorununu çözmez
- İlk band: 1–5
- Güç bandı: Standard

### Breacher
- Etki: +12 Armor Pen
- Amaç: armor’un solve problemi olduğunu öğretmek
- En iyi sinerji: Sniper / Beam / Assault
- Zayıf kullanım: armorsuz stage
- İlk band: 6–10
- Güç bandı: Solve

### Piercing Round
- Etki: +1 Pierce
- Amaç: çizgi temizleme / geometry öğretimi
- En iyi sinerji: Sniper / Assault
- Zayıf kullanım: dağınık wave
- İlk band: 6–15
- Güç bandı: Solve

### Reinforce: Piyade
- Etki: +2 Piyade
- Amaç: soldier layer’ı görünür kılmak
- En iyi sinerji: Assault / güvenli build
- Zayıf kullanım: sadece sayı olup çözüm üretmeyebilir
- İlk band: 1–10
- Güç bandı: Army

### Reinforce: Mekanik
- Etki: +1 Mekanik
- Amaç: yakın baskı desteği açmak
- En iyi sinerji: SMG / Shotgun
- Zayıf kullanım: uzak solve ihtiyacı varken
- İlk band: 11–20
- Güç bandı: Army

### Reinforce: Teknoloji
- Etki: +1 Teknoloji
- Amaç: özel hedef desteği açmak
- En iyi sinerji: Sniper / Beam / Launcher
- Zayıf kullanım: saf swarm stage
- İlk band: 16–25
- Güç bandı: Army

### Medkit
- Etki: Komutan +25% HP
- Amaç: hata affı
- En iyi sinerji: erken oyun / riskli oyuncu
- Zayıf kullanım: temiz oynayan için düşük tavan
- İlk band: 1–10
- Güç bandı: Sustain

### Field Repair
- Etki: Askerler %35 iyileşir
- Amaç: army layer sürdürülebilirliği
- En iyi sinerji: reinforce buildleri
- Zayıf kullanım: küçük ordu
- İlk band: 1–15
- Güç bandı: Sustain

### Execution Line
- Etki: +12% Elite Hasarı
- Amaç: elite önceliği öğretmek
- En iyi sinerji: Sniper / Beam
- Zayıf kullanım: elitsiz stage
- İlk band: 6–15
- Güç bandı: Solve

### Scatter Chamber
- Etki: yakın mesafede ek pellet / spread verimi
- Amaç: Shotgun kimliğini parlatmak
- En iyi sinerji: Shotgun / Mekanik
- Zayıf kullanım: uzak dağınık kompozisyon
- İlk band: 11–20
- Güç bandı: Standard

### Payload Chamber
- Etki: splash radius / blast verimi artar
- Amaç: Launcher pack punish rolünü öğretmek
- En iyi sinerji: Launcher
- Zayıf kullanım: tek hedef
- İlk band: 16–25
- Güç bandı: Solve

### Conductor Lens
- Etki: beam uptime / elite-boss verimi artar
- Amaç: Beam’i final prep diline bağlamak
- En iyi sinerji: Beam / Teknoloji
- Zayıf kullanım: swarm stage
- İlk band: 31–34
- Güç bandı: Boss Prep

### Final Prep: Stabilizer
- Etki: boss için güvenli güç / hata affı / stabilize solve
- Amaç: boss öncesi tek yol dayatmayan hazırlık
- En iyi sinerji: Sniper / Beam / Assault
- Zayıf kullanım: swarm stage
- İlk band: 31–34
- Güç bandı: Boss Prep

---

## 8. Enemy & Spawn

## 8.1 Ana ilke
Düşmanlar:
- yalnızca farklı HP değerleri olan objeler değildir
- oyuncudan farklı kararlar ister

Spawn:
- yalnızca “kaç düşman doğuyor” sorusu değildir
- karar kompozisyonu kurar

---

## 8.2 Enemy roster — World 1

- Trooper
- Swarm
- Charger
- Armored Brute
- Elite Charger
- Gatekeeper Walker
- War Machine
- World 1 Final Boss

---

## 8.3 Enemy behavior matrix

### Trooper
Rol:
- baseline referans düşman

İstediği karar:
- temel DPS yeterli mi?

Threat:
- düşük-orta

Hareket:
- düz ilerleme
- hafif lane tracking
- separation ile paket görünümünü korur

Counter:
- tüm silahlar
- Assault doğal cevap

Spawn use:
- Wall
- Stagger filler
- support pack

---

### Swarm
Rol:
- sayı baskısı

İstediği karar:
- alan / tempo çözümü
- lane temizleme

Threat:
- düşük tekil, yüksek toplu

Hareket:
- küçük kümeler
- hafif dağınık
- tek sıra asker dizisi gibi görünmez

Counter:
- SMG
- Launcher
- Shotgun
- Assault orta cevap

Spawn use:
- Dense Core
- Split Swarm
- Side Support

---

### Charger
Rol:
- öncelik tehdidi

İstediği karar:
- “önce bunu durdur” demek

Threat:
- orta-yüksek

Hareket:
- kısa normal ilerleme
- hazırlık
- okunabilir dash

Counter:
- Assault
- zamanında SMG
- iyi anda Sniper

Spawn use:
- Delayed Threat
- mixed lane test
- support threat

---

### Armored Brute
Rol:
- armor check

İstediği karar:
- solve gerekir mi?
- armor pen / sniper / pierce önemli mi?

Threat:
- yüksek dayanıklılık baskısı

Hareket:
- ağır ve kararlı
- yavaş ama inatçı

Counter:
- Sniper
- Breacher
- Beam
- Assault çalışır ama ideal değildir

Spawn use:
- Guarded Core
- Armor Check Pair
- mixed pressure

---

### Elite Charger
Rol:
- panik + öncelik çakışması

İstediği karar:
- elite’i ne zaman öne alacağım?

Threat:
- yüksek

Hareket:
- charger mantığı
- daha net telegraph
- daha sert giriş

Counter:
- Sniper
- Assault
- Beam
- elite damage gate’leri

Spawn use:
- Elite Spike
- mixed punishment
- final prep practice

---

### Gatekeeper Walker
Rol:
- ilk mini-boss

İstediği karar:
- tek hedef baskısı
- telegraph okuma
- greed DPS’i cezalandırma
- armor farkındalığı

Ana saldırılar:
- Line Shot
- Front Sweep
- Short Charge

Counter:
- Assault güvenli
- Sniper yüksek değer
- yanlış build ile de kazanılabilir ama zorlanılır

---

### War Machine
Rol:
- alan baskısı ve hareket alanı okuma sınavı

İstediği karar:
- lane değiştirme
- tehlike alanı okuma
- sustained vs burst penceresi ayrımı

Counter:
- Assault
- Sniper
- Beam

---

### World 1 Final Boss
Rol:
- tüm World 1 bilgisinin sınavı

İstediği karar:
- build kararı
- gate bilgisi
- threat önceliği
- telegraph okuma
- Beam’in neden iyi olduğunu anlamak ama ona mecbur olmamak

---

## 8.4 Spawn grammar

### Wave tasarım ilkesi
Her wave şu soruya cevap vermeli:
> Bu kompozisyon oyuncudan hangi kararı istiyor?

### Spawn pattern ailesi

#### Wall
- baseline temizleme
- lane coverage testi

#### Dense Core
- swarm / launcher / SMG değerini gösterir

#### Stagger Line
- ip gibi görünmeyen kontrollü akış

#### Lane Pinch
- oyuncuyu tek hatta kilitlenmekten çıkarır

#### Delayed Threat
- ilk pakete odaklanırken sonradan tehdit bindirir

#### Guarded Core
- “önce kimi vuracağım?” sorusunu doğurur

#### Boss Prep Pack
- yaklaşan boss bilgisini küçük örneklerle öğretir

### Spawn kuralları
- aynı düşman aynı aralıkla uzun süre akmamalı
- x jitter + z jitter + entry delay farkı kullanılmalı
- karışık wave = kaos değildir
- oyuncu tehditleri okuyabilmelidir

---

## 8.5 Enemy x Weapon interaction

### Assault parlar
- Trooper
- mixed wave
- güvenli boss DPS
- charger çözümü

### SMG parlar
- Swarm
- dense core
- lane pinch support
- hızlı zayıf hedef temizliği

### Sniper parlar
- Brute
- Elite Charger
- Gatekeeper Walker
- boss-benzeri yüksek öncelik hedefleri

### Shotgun parlar
- sıkışık yakın paket
- mekanik destekli ön hat
- punish entry wave

### Launcher parlar
- kümeli support pack
- delayed dense reinforcements
- guarded core dış halkaları

### Beam parlar
- elite
- mini-boss
- final prep pack
- final boss

---

## 9. Screen Flow / UI Flow

## 9.1 Full flow

```text
Splash / Boot
→ Main Menu
→ World Map
→ Stage Card / Stage Info
→ Loadout
→ Run Intro
→ Runner HUD
→ Mini-Boss / Boss
→ Victory veya Fail
→ Reward Summary
→ Upgrade / Progress
→ World Map'e Dönüş
Kural

Hiçbir halka placeholder bahanesiyle boş kalmamalı.

9.2 Screen listesi
Splash / Boot
kısa yükleme
logo / title
hızlı geçiş
Main Menu
Play / Continue
Settings
Loadout / inventory erişimi
sade giriş
World Map
World 1 görseli
stage node’ları
kilitli/açık durum
mini-boss / boss node farkı
ilerleme hissi
Stage Card
stage adı
threat tags
reward özeti
first clear varsa
Start butonu
Loadout
commander weapon
equipment slots
squad support özeti
build snapshot
start run
Run Intro
kısa stage geçişi
threat hissi
çok kısa olmalı
Runner HUD
commander HP
soldier summary
gate okunurluğu
danger telegraph
boss HP / faz
Boss Overlay
boss adı
boss HP
faz göstergesi
transition lock vurgusu
Fail Screen
başarısız oldun
kısa neden
retry
map’e dön
Victory Screen
stage clear
reward özeti
continue
Reward Summary
stage reward
mid-run reward
first clear bonus
kısa toplam
Upgrade / Progress
küçük anlamlı gelişim
sonraki hedef
map’e dönüş
9.3 Runner HUD ilkeleri
Öncelik sırası
hayatta kalma bilgisi
yakın karar bilgisi
güçlenme hissi
meta bilgi
Görülmesi gerekenler
commander HP
soldier summary
karşıdaki gate bilgisi
tehlike alanları
boss HP / faz
Gösterilmeyecekler
uzun gate açıklamaları
aşırı floating text
açık taktik tavsiyesi
panel kalabalığı
Gate görünüm formatı
+12 Zırh Delme
ARMOR • ELITE
9.4 Güzel UI ek fikirleri
Stage Threat Tags

Stage card üstünde:

SWARM
ARMOR
ELITE
BOSS PREP
Build Snapshot

Loadout’ta:

Balanced
Swarm Clear
Armor Break
Elite Hunt
Boss Prep
Result Recap

Run sonunda küçük özet:

silah
önemli gate’ler
squad özeti
Mid-run micro summary

Çok minimal gate build özeti olabilir
ama HUD’ı boğmamalı

10. Localization contract
10.1 Temel karar

Oyun baştan localization-ready kurulmalıdır.

İlk aktif diller:

English
Türkçe
Çok önemli kural

UI, gate, stage, item, enemy ve threat metinleri
hardcoded string olarak kodda yaşamamalı.

10.2 Lokalize edilecek alanlar
Zorunlu
ana menü metinleri
butonlar
world/stage adları
stage band adları
threat tag’leri
gate ana etki başlıkları
gate alt kısa tag’leri
silah adları
düşman adları
build snapshot etiketleri
fail/victory/reward metinleri
upgrade başlıkları
Sonra yapılabilir
uzun flavor yazıları
lore metinleri
debug/dev tool metinleri
10.3 Localization key standard

Örnek key yapısı:

ui.main.play
ui.main.settings
ui.result.victory
ui.result.fail
ui.loadout.start
ui.reward.continue

world.1.name
world.1.band.tutorial_core
world.1.band.final_prep

stage.1.name
stage.10.name
stage.35.name

weapon.assault.name
weapon.smg.name
weapon.sniper.name
weapon.shotgun.name
weapon.launcher.name
weapon.beam.name

enemy.trooper.name
enemy.swarm.name
enemy.charger.name
enemy.armored_brute.name
enemy.elite_charger.name
enemy.gatekeeper_walker.name

gate.hardline.title
gate.hardline.tag1
gate.hardline.tag2

tag.swarm
tag.armor
tag.elite
tag.boss_prep

build.balanced
build.swarm_clear
build.armor_break
build.elite_hunt
build.boss_prep
10.4 ScriptableObject localization rule

SO içinde doğrudan görünen string yerine mümkünse key tutulmalı.

WeaponArchetypeConfig
weaponNameKey
descriptionKey varsa
GateConfig
titleKey
tag1Key
tag2Key
StageConfig
stageNameKey
threatTagKeys
EnemyArchetypeConfig
displayNameKey
WorldConfig
worldNameKey
bandNameKeys gerekiyorsa
Not

Editor içinde debug için fallback text olabilir.
Ama runtime UI key üzerinden çalışmalıdır.

10.5 Kod tarafı localization kuralı

Kod içinde şu tip stringler bırakılmamalı:

"Play"
"Victory"
"Armor"
"Elite"
"Continue"
"Boss Prep"

Bunlar:

localization key
localization helper
text binder
üzerinden çekilmelidir.
10.6 Text expansion note

Türkçe ve İngilizce aynı uzunlukta değildir.

Bu yüzden
button genişlikleri esnek olmalı
gate alt tag alanı kısa ama toleranslı olmalı
autosize veya kontrollü truncate kullanılmalı
taşma testi yapılmalı
10.7 Font note

İlk iki dil için ortak font seti kullanılabilir.
Ama sistem ileride başka dillere açılabilecek şekilde kurulmalı.

Kural
font fallback düşünülmeli
tek fonta gömülüp sonra patlamamalı
10.8 Save / dil tercihi

Dil tercihi persistent save tarafında tutulur.

İlk açılış
cihaz dili okunabilir
destekleniyorsa atanır
desteklenmiyorsa English fallback olur
Settings
dil elle değiştirilebilir
UI anında yenilenir
10.9 Localization için şimdi yapılması gerekenler
Tüm görünen metinler için key standardını sabitle
Yeni SO alanları gerekiyorsa şimdi ekle
GateConfig / StageConfig / WeaponArchetype / EnemyArchetype üzerinde text key alanlarını planla
Hardcoded UI stringlerini backlog’a al
EN + TR temel table yapısını başlat
Gate alt tag’lerini kısa ve iki dilde de taşmayacak mantıkla seç
11. Unity Hookup & Migration
11.1 Hookup felsefesi

Bir sistem “kodda var” diye aktif kabul edilmez.
Aktif sayılması için:

tasarımda tanımlı olmalı
veri kaynağına bağlı olmalı
scene/prefab/UI referansları takılmış olmalı
test checklist’inde doğrulanmış olmalı
11.2 Static data checklist
Oluşturulacak ana SO’lar
WeaponArchetypeConfig
EnemyArchetypeConfig
GateConfig
GatePoolConfig
StageConfig
WaveConfig
BossConfig
EconomyConfig
RewardProfileConfig
WorldConfig
11.3 Weapon hookup
Unity asset üretimi

Create > Top End War > WeaponArchetypeConfig

World 1 için archetype asset’leri
Assault
SMG
Sniper
Shotgun
Launcher
Beam
Bağlanacak yerler
EquipmentData.weaponArchetype
ArmyManager.weaponConfigs
Loadout UI
build snapshot sistemi
11.4 Equipment hookup
EquipmentData rolü
item kimliği
rarity
modifier
icon
weaponArchetype referansı
Bağlanacak yerler
Inventorymanager
Equipmentloadout
Equipmentui
PlayerStats / commander çözümleme
11.5 Enemy hookup
Oluşturulacak archetype’lar
Trooper
Swarm
Charger
Armored Brute
Elite Charger
Gatekeeper Walker
War Machine
World 1 Final Boss
EnemyArchetypeConfig şunları taşımalı
HPFactor
Armor
IsEliteLike
MoveSpeed
ContactDamage
Reward
BehaviorTag
Bağlantılar
WaveConfig
SpawnManager
BossConfig
Stage threat mapping
11.6 Gate hookup
Gate aileleri
Power
Tempo
Penetration
Geometry
Army
Sustain
Tactical
Boss Prep
Her GateConfig taşımalı
effect id
titleKey
tag1Key
tag2Key
icon
family
stage band visibility
balance tier
Bağlanacak yerler
GatePoolConfig
Gate prefab UI
GameHUD / Gatefeedback
localization table
11.7 Stage hookup
Her StageConfig taşımalı
stage id
world id
stage band
stageNameKey
threat tags
target dps
reward profile
gate pool reference
wave sequence
boss reference varsa boss config
unlock rule
Bağlantılar
StageManager
World Map
Stage Card
Reward flow
progression unlock logic
11.8 Scene / prefab hookup
Main Menu
Play / Continue
Settings
loadout / inventory erişimi
World Map
WorldConfig
StageManager
SaveManager
Stage Card
stage name
threat tags
rewards
Start button
Loadout
Equipmentui
Equipmentloadout
Inventorymanager
archetype referansları
Runner scene
PlayerController
GameHUD
SpawnManager
ArmyManager
BossManager
gate prefab
enemy prefab
bullet pool
camera rig
run state başlangıcı
Result / Reward / Upgrade
fail state
victory state
reward summary
upgrade panel
world map dönüşü
11.9 Migration plan
Keep
mevcut SO mantığı
StageManager / SpawnManager temel akışı
GameHUD / Mainmenuui / Equipmentui iskeletleri
ArmyManager temel ordu omurgası
SaveManager / GameEvents
Transform
GateData / eski effect dili → GateConfig odaklı sistem
EquipmentData → item/modifier katmanı
WeaponArchetypeConfig → ana combat family verisi
Enemy hardcoded init → archetype-driven davranış dili
CP merkezli eski combat açıklamaları → build/gate/silah dili
Freeze
pet
morph
arena
alliance
level editor
challenge mode
world 2+
ağır monetization
live ops
Kural

Kodbase’de durabilirler, ama aktif çekirdek sayılmazlar.

11.10 Naming / cleanup note

İleride tek standarda çekilmeli:

PascalCase dosya adları
class name = file name
asset adları okunur ve tutarlı

Örnek legacy riskleri:

Weaponarchetypeconfig.cs
Soldierunit.cs
Bosshitreceiver.cs
12. Test checklist
12.1 Core flow test
Main Menu → World Map
World Map → Stage Card
Stage Card → Loadout
Loadout → Runner
Run end → Result → Reward → Upgrade → Map
12.2 Data hookup test
WeaponArchetype asset’leri bağlı mı
EquipmentData.weaponArchetype boşta mı
StageConfig referansları dolu mu
GateConfig localization key’leri dolu mu
EnemyArchetype armor/elite bilgisi çalışıyor mu
12.3 Localization test
EN / TR geçişi çalışıyor mu
gate text taşıyor mu
threat tag sığıyor mu
result ekranı bozuluyor mu
stage adları düzgün mü
12.4 Mobile test
portrait ratio
safe area
HUD clutter
button touch area
küçük cihaz testi
12.5 Combat readability test
swarm ip gibi mi görünüyor?
charger okunuyor mu?
brute sadece HP kutusu gibi mi?
elite ayırt ediliyor mu?
Beam boss için iyi ama zorunlu değil hissi veriyor mu?
13. Açık riskler / notlar
Risk 1

World Map / Stage Card / Loadout zayıf kalırsa oyun combat prototipi gibi görünür.

Risk 2

Localization geç eklenirse hardcoded string çöplüğü oluşur.

Risk 3

Gate tag’leri iki dilde taşarsa okunurluk bozulur.

Risk 4

Soldier equipment’i tek tek asker envanteri yapmaya çalışırsak World 1 gereksiz mikroya döner.

Risk 5

Beam fazla geç gelirse boss prep dili geç oturur; fazla erken gelirse diğer solve silahları gölgeler.

Risk 6

Enemy behavior yazılmadan sadece spawn sayılarıyla tuning yapılırsa yine ip gibi wave hissi geri gelir.

14. Güzel eklenebilecek fikirler
Build Snapshot

Loadout ve sonuç ekranlarında çok faydalı.

Stage Threat Tags

Stage card için çok güçlü ama sade yönlendirme sağlar.

Result Recap

Oyuncunun “neden kazandım/kaybettim”i anlamasını hızlandırır.

Language-aware short tags

Gate alt tag’leri dil başına kısa varyantla tutulabilir.

Boss Prep recommendations without hard advice

Açık “Beam seç” demeden:

ARMOR
ELITE
LONG FIGHT
BOSS PREP
gibi tag dili kurulabilir.
15. Son karar özeti

Bu belgeye göre World 1:

oyunun gerçek öğretim alanıdır
tüm temel combat dilini öğretir
build’i yalnızca gate’ten ibaret görmez
commander + soldier + squad support + gate + stage knowledge sistemini birlikte taşır
sidegrade silah felsefesini korur
Beam’i final boss öncesi tanıtır ama dayatmaz
düşmanları karar testi olarak kullanır
UI ve ekran akışını placeholder bile olsa boş bırakmaz
localization’ı sonradan değil baştan düşünür
Unity hookup ve migration işlerini tasarımın parçası kabul eder
revize edilene kadar kanonik kabul edilir