# Top End War — Unity Hookup & Migration Notes v2
_World 1 Setup, Scene Hookup, Localization ve Cleanup Contract_

---

## 0. Belgenin amacı

Bu belge, tasarım bible’larda tanımlanan sistemlerin Unity içinde **gerçekten ayağa kalkması** için gereken kurulum, bağlama ve migration notlarını tanımlar.

Amaç:
- “tasarım var ama Unity’de bağlı değil” sorununu bitirmek
- hangi asset’in oluşturulacağı ve nereye bağlanacağı konusunda tek referans vermek
- legacy sistemleri temizlerken çalışan çekirdeği korumak
- World 1 üretimi sırasında kaybolmamak
- çok dilliliği baştan doğru temele oturtmak

### Kural
Bu belge:
- final kod belgesi değildir
- sahne, SO, prefab, UI ve migration bağlantı sözleşmesidir
- revize edilene kadar kanonik kabul edilir

---

## 1. Hookup felsefesi

### 1.1 Temel ilke
Bir sistem “kodda var” diye aktif kabul edilmez.  
Aktif sayılması için şu 4 şey tamam olmalıdır:

1. Tasarımda tanımlı olması  
2. ScriptableObject veya runtime veri kaynağına bağlı olması  
3. Scene/prefab/UI içinde referanslarının bağlı olması  
4. Test checklist’inde doğrulanmış olması  

### 1.2 World 1 için öncelik
Önce şu zincir ayağa kalkmalı:

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

Bu zincir eksikse combat ne kadar iyi olursa olsun oyun boş hisseder.

2. Ana veri hookup katmanları
2.1 Static Design Data

Unity’de asset olarak oluşturulacak ana veriler:

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
2.2 Runtime Session Data

Scene veya run sırasında yaşayan veriler:

RunState
aktif gate etkileri
current HP
boss phase
run içi ödül / gold
stage içi progression
2.3 Persistent Save Data

Kalıcı kayıt tarafı:

açılmış stage’ler
weapon progress
currency
tutorial flags
settings
dil tercihi
3. Required asset checklist
3.1 Weapon assets
Oluşturulacak

Create > Top End War > WeaponArchetypeConfig

World 1 için hazırlanacak archetype’lar
Assault
SMG
Sniper
Shotgun
Launcher
Beam
Not

Hepsi tasarımda hazırlanmalı.
Ama aktif kullanım bandını sen sonra ayarlayabilirsin.

Bağlanacak yerler
EquipmentData.weaponArchetype
ArmyManager.weaponConfigs
Loadout UI
Stage recommendation / build snapshot sistemi
3.2 Equipment assets
Oluşturulacak

Create > Top End War > Equipment

Kural

EquipmentData:

item kimliğidir
silahın özü değildir
weaponArchetype referansı taşır
rarity / modifier / icon / description burada olur
Bağlanacak yerler
Inventorymanager
Equipmentloadout
Equipmentui
PlayerStats / commander equipment çözümlemesi
3.3 Enemy assets
Oluşturulacak enemy archetype’lar
Trooper
Swarm
Charger
Armored Brute
Elite Charger
Gatekeeper Walker
War Machine
World 1 Final Boss
Kural

EnemyArchetypeConfig:

HP factor
armor
elite flag
move speed
contact damage
reward
behavior tag
taşımalıdır
Bağlanacak yerler
WaveConfig
SpawnManager
BossConfig
StageConfig threat mapping
3.4 Gate assets
Oluşturulacak gate aileleri
Power
Tempo
Penetration
Geometry
Army
Sustain
Tactical
Boss Prep
Her GateConfig şunları taşımalı
effect id
display key
short tag key 1
short tag key 2
icon
family
stage band visibility
balance tier
Bağlanacak yerler
GatePoolConfig
Gate prefab UI
GameHUD / Gatefeedback
localization table
3.5 Stage assets
Her StageConfig en az şunları taşımalı
stage id
world id
stage band
stage display key
threat tags
target dps
reward profile
gate pool reference
wave sequence
boss reference varsa boss config
unlock rule
Bağlanacak yerler
StageManager
World Map
Stage Card
Reward flow
progression unlock logic
4. Scene & prefab hookup
4.1 Main Menu scene / panel

Gerekenler:

Play / Continue
Settings
Loadout veya Inventory erişimi
World Map’e geçiş
Test
oyuna ilk giriş buradan çalışmalı
dead-end buton olmamalı
4.2 World Map scene / panel

Gerekenler:

World 1 görseli
stage node’ları
kilitli/açık durum
mini-boss / boss node farkı
seçilen stage ile Stage Card açılması
Bağlantılar
WorldConfig
StageManager
SaveManager
4.3 Stage Card panel

Gerekenler:

stage adı
threat tags
reward özeti
first clear reward varsa
Start button
Loadout’a git veya direkt start
Kural

Uzun açıklama yok.
Kısa, okunur, karar verdiren bilgi var.

4.4 Loadout panel

Gerekenler:

commander weapon
equipment slots
soldier summary
build snapshot
start run
Build snapshot örnekleri
Balanced
Swarm Clear
Armor Break
Elite Hunt
Boss Prep
Bağlantılar
Equipmentui
Equipmentloadout
Inventorymanager
WeaponArchetypeConfig
localization keys
4.5 Runner scene

Gerekenler:

PlayerController
GameHUD
SpawnManager
ArmyManager
BossManager
Gate prefab’lar
enemy prefab
bullet pool
camera rig
run state başlangıcı
Test
scene direkt açıldığında tek başına da bozulmamalı
proper references eksikse anlaşılır hata vermeli
4.6 Result / Reward / Upgrade panels

Gerekenler:

fail state
victory state
reward summary
upgrade panel
world map’e dönüş
Kural

Bu akış tek seferde tamamlanmalı:

Run end → Result → Reward → Upgrade / Skip → World Map
5. Runner HUD hookup
5.1 Gerekli HUD elemanları
commander HP
soldier summary
gate readability support
danger telegraphs
boss HP / phase
Kaynakları
PlayerStats
ArmyManager
Gate world-space data
BossManager
localized UI keys
5.2 World-space gate presentation

Kapıların ana taşıyıcısı world-space olmalı.
HUD yalnızca destekleyici olmalı.

Format
Main Effect
TAG • TAG
Kural

1 saniyede okunmalı.

6. Localization contract
6.1 Temel karar

Oyun en baştan localization-ready kurulmalıdır.

İlk aktif diller:

English
Türkçe
Çok önemli kural

UI, gate, stage, item ve threat metinleri hardcoded string olarak kodda yaşamamalı.

6.2 Ne lokalize edilir
Zorunlu
ana menü metinleri
buton yazıları
world/stage adları
stage band adları
threat tag’leri
gate ana etki metinleri
gate kısa tag’leri
silah adları
düşman adları
reward ekran metinleri
fail/victory mesajları
upgrade ekran başlıkları
Sonra yapılabilir
flavor açıklamaları
uzun lore metinleri
debug / dev tool metinleri
6.3 Localization key standard
Kural

Gösterilen her metin için key kullanılmalı.

Örnek key yapısı
ui.main.play
ui.main.settings
ui.result.victory
ui.result.fail

world.1.name
world.1.band.tutorial_core

stage.1.name
stage.10.name

weapon.assault.name
weapon.smg.name
weapon.sniper.name
weapon.beam.name

enemy.trooper.name
enemy.elite_charger.name

gate.hardline.title
gate.hardline.tag1
gate.hardline.tag2

tag.swarm
tag.armor
tag.elite
tag.boss_prep
6.4 ScriptableObject localization rule

SO içinde doğrudan görünen string yerine mümkünse key tutulmalı.

Örnek
WeaponArchetypeConfig
weaponName yerine weaponNameKey
kısa açıklama varsa descriptionKey
GateConfig
titleKey
tag1Key
tag2Key
StageConfig
stageNameKey
threatTagKeys
EnemyArchetypeConfig
displayNameKey
Not

Debug için editor-only fallback text olabilir.
Ama runtime UI mümkünse key üzerinden çalışmalı.

6.5 Kod tarafı kuralı

Kod içinde:

"Play"
"Victory"
"Armor"
"Elite"
"Continue"
gibi direkt string bırakılmamalı.

Yerine:

loc key
localization helper
UI binder

kullanılmalı.

6.6 Text expansion note

İngilizce ve Türkçe metinler aynı uzunlukta olmayabilir.

Bu yüzden
button genişlikleri sabit dar olmamalı
gate tag satırı çok uzun text’i kaldırabilecek kadar esnek olmalı
autosize veya kontrollü kısaltma kullanılmalı
satır taşması test edilmelidir
6.7 Font note

İlk iki dil için ortak font seti kullanılabilir.
Ama sistem ileride başka dillere açılabilecek şekilde kurulmalı.

Kural
font fallback desteği düşünülmeli
text component’leri tek fonta kilitlenip sonradan patlamamalı
6.8 Save / language setting

Dil tercihi persistent save tarafında saklanmalı.

İlk açılışta
cihaz dili okunabilir
destekleniyorsa otomatik atanır
desteklenmiyorsa English fallback olur
Settings içinden
dil elle değiştirilebilir
UI anında yenilenir
7. Migration plan
7.1 Keep

Şunlar korunur, sadece bağlanır:

mevcut SO mantığı
StageManager / SpawnManager temel akışı
GameHUD / Mainmenuui / Equipmentui gibi mevcut ekran scriptleri
ArmyManager temel ordu omurgası
SaveManager / GameEvents temel omurga
7.2 Transform

Şunlar dönüştürülür:

GateData / effect eski dili → GateConfig odaklı yeni sistem
EquipmentData → item/modifier katmanı
WeaponArchetypeConfig → ana combat family verisi
Enemy hardcoded init → archetype driven behavior/config dili
eski CP merkezli görünen combat metinleri → build/gate/silah dili
7.3 Freeze

Şunlar şimdilik aktif çekirdek dışıdır:

pet
morph
arena
level editor
challenge mode
world 2+
ağır monetization
live ops
Kural

Kodbase’de durabilirler.
Ama sahnede, menüde ve ana akışta görünmezler.

8. Naming & structure notes
Dosya adı standardı

İleride şu tutarsızlıklar temizlenmeli:

Weaponarchetypeconfig.cs
Soldierunit.cs
Bosshitreceiver.cs
gibi adlar tek standarda çekilmeli
Hedef standard
PascalCase dosya adları
class name = file name
asset adları okunur ve tutarlı
9. Test checklist
9.1 Core flow test
Main Menu → World Map çalışıyor mu
World Map → Stage Card çalışıyor mu
Stage Card → Loadout çalışıyor mu
Loadout → Runner çalışıyor mu
Run end → Result → Reward → Upgrade → Map çalışıyor mu
9.2 Data hookup test
WeaponArchetype asset’leri gerçekten bağlı mı
EquipmentData.weaponArchetype boşta mı
StageConfig reward/gate/wave referansları dolu mu
GateConfig localization key’leri dolu mu
EnemyArchetypeConfig armor/elite bilgisi geçiyor mu
9.3 Localization test
English / Türkçe arasında geçiş oluyor mu
gate text taşması oluyor mu
threat tag’leri sığıyor mu
result ekranı bozuluyor mu
stage adları düzgün görünüyor mu
9.4 Mobile test
portrait ratio test
safe area test
HUD clutter test
button touch area test
büyük text / küçük cihaz testi
10. Açık riskler
Risk 1

Localization geç eklenirse SO’lar ve UI’lar hardcoded string çöplüğüne döner.

Çözüm

Localization key sistemini şimdi kur.

Risk 2

World Map / Stage Card / Loadout zayıf kalırsa oyun “combat prototipi” gibi görünür.

Çözüm

Bu üç ekranı placeholder bile olsa erken ayağa kaldır.

Risk 3

Gate metinleri iki dilde aynı uzunlukta olmayacağı için okunurluk bozulabilir.

Çözüm

Kısa etiket sistemi korunmalı ve test edilmelidir.

Risk 4

Legacy sistemler menüde görünür kalırsa scope dağılır.

Çözüm

Freeze listesi uygulanmalı.

11. Güzel eklenebilecek küçük fikirler
A. Language-aware gate tags

Bazı dillerde gate alt tag’leri kısa, bazılarında daha kısa varyantla tutulabilir.

Örnek:

English short tag
Turkish short tag

Bu çok faydalı olur.

B. Build Snapshot localization

Build summary key üzerinden çalışırsa:

Balanced
Armor Break
Boss Prep
gibi etiketler iki dilde temiz kalır.
C. Threat Tag strip

Stage kartındaki threat tag şeridi localization-ready yapılırsa ileride World 2+ için de bedava değer üretir.

12. Son karar özeti

Unity Hookup & Migration sistemi:

tasarımı gerçek Unity kurulumuna bağlar
hangi asset’in üretileceğini netleştirir
hangi referansın nereye takılacağını söyler
localization’ı sonradan değil baştan düşünür
legacy ile aktif çekirdeği ayırır
revize edilene kadar kanonik kabul edilir