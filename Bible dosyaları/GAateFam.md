# Top End War — Gate System Redesign v2
_World 1 Canon Gate Language_

---

## 0. Belgenin amacı

Bu belge, Top End War için yeni gate sistemini tanımlar.

Amaç:
- gate’leri yalnızca rastgele sayı bonusu olmaktan çıkarmak
- build language’in görünür omurgası yapmak
- silah, enemy ve stage bilgisini oyuncuya doğrudan söylemeden hissettirmek
- World 1 boyunca gate öğretimini kontrollü açmak
- localization ve UI için net bir veri sözleşmesi bırakmak

### Kural
Gate:
- kolay okunmalı
- kısa anlaşılmalı
- ama sığ olmamalı

---

## 1. Gate’in oyundaki gerçek görevi

Gate sistemi 4 şeyi yapar:

1. **Build yönü seçtirir**
2. **Sorun çözdürür**
3. **Orduyu görünür kılar**
4. **Stage bilgisini ödüllendirir**

### Sonuç
Gate yalnızca “+daha çok sayı” sistemi değildir.  
Gate, oyuncuya şu soruyu sordurur:

> “Bu run’da neyi büyütmeliyim ve neyi çözmeliyim?”

---

## 2. Gate UX sözleşmesi

Bu kısım korunur:

- aynı anda 2 kapı görünür
- seçim fiziksel geçişle yapılır
- ayrı seçim menüsü yok
- kapı üstünde uzun paragraf yok

### Görünüm formatı
```text
ANA ETKI
TAG • TAG
Örnek
+12 Armor Pen
ARMOR • ELITE
Okunurluk kuralı
1 saniyede okunmalı
üst satır ana kararı taşımalı
alt satır build/senaryo ipucu vermeli
oyuncuya açık taktik komut vermemeli
3. Gate aileleri — yeni kanon

Yeni sistemde gate aileleri şunlar:

Power
Tempo
Solve
Geometry
Army
Sustain
Tactical
Boss Prep
Neden eski 6 değil 8?

Eski aile yapısı iyiydi ama yeni build language’de iki şey daha net ayrılmalı:

Solve = armor/elite/problem çözen kapılar
Boss Prep = geç World 1’de özel hazırlık kapıları

Bunları ayrı tutmak daha doğru.

4. Gate güç bantları

Her gate aynı tür güç vermez.

4.1 Minor
küçük ama temiz katkı
yaklaşık %4–6 gerçek güç
erken öğretim için iyi
4.2 Standard
run omurgası
yaklaşık %8–12 gerçek güç
çoğu seçim burada döner
4.3 Solve
her yerde güçlü değildir
doğru durumda çok değerlidir
yaklaşık %0–35 efektif fark yaratabilir
4.4 Army
direkt DPS gibi görünmez
ama savaş gücü verir
yaklaşık %6–18 efektif katkı üretir
4.5 Sustain
direkt DPS vermez
hata toleransı ve devamlılık verir
4.6 Boss Prep
final prep bandında görünür
boss öncesi seçim değeri taşır
5. Gate ailelerinin görevi
5.1 Power

Görev:

güvenli genel güç

Ne verir:

weapon power
base damage
flat/percent genel güç

Kimin için iyi:

Assault
Sniper
genel güvenli buildler

Risk:

solve problemi çözmez
5.2 Tempo

Görev:

akışı hızlandırmak
baskıyı yumuşatmak
SMG/Assault tempo buildlerini beslemek

Ne verir:

fire rate
attack cycle
tempo stack
kısa ama run-boyu etkili tempo büyümesi

Kimin için iyi:

SMG
Assault
kalabalık temizleme buildleri

Risk:

brute/armor sorununu kendi başına çözmez
5.3 Solve

Görev:

“yanlış silahla uzayan problemi” çözmek

Ne verir:

armor pen
elite damage
pierce
boss-specific solve
armor break türü etkiler

Kimin için iyi:

Sniper
Beam
Assault’un eksik kaldığı durumlar

Risk:

doğru tehdit yoksa düşük değer
5.4 Geometry

Görev:

mermi davranışını değiştirmek
wave şeklini cezalandırmak

Ne verir:

spread verimi
projectile count
splash
line clear
cluster punish

Kimin için iyi:

Shotgun
Launcher
bazı Assault varyantları

Risk:

yanlış kompozisyonda boşa gidebilir
5.5 Army

Görev:

soldier layer’ı görünür büyütmek

Ne verir:

reinforce
soldier heal
squad type bias
formation verimi
support layer güçlendirmesi

Kimin için iyi:

tempo buildler
güvenli buildler
oyuncunun riskini azaltan kurulumlar

Risk:

sadece sayı artarsa ve çözüm artmazsa zayıf kalabilir
5.6 Sustain

Görev:

hata affı
run ömrü uzatma

Ne verir:

commander HP
squad heal
fail toleransı
uzun dövüş güvenliği

Kimin için iyi:

yeni oyuncu
riskli build
boss prep dışı güvenli kurulum

Risk:

direkt öldürme gücü vermez
5.7 Tactical

Görev:

belirli senaryoyu dolaylı çözmek
tek başına büyük sayı yerine doğru araç olmak

Ne verir:

target mark
execute
threat priority yardımcısı
lane utility
short conditional solve

Kimin için iyi:

ileri seviye buildler
yüksek fark yaratmak isteyen oyuncu

Risk:

yanlış kullanılırsa değersiz hissedebilir
5.8 Boss Prep

Görev:

final prep bandında oyuncuya son kararları verdirmek

Ne verir:

long-fight verimi
beam/sniper sinerjisi
boss-specific stabilize etkiler
elite/boss solve

Kimin için iyi:

final prep
Beam
Sniper
güvenli Assault boss çözümü

Risk:

erken oyunda görünürse build dilini bozar
6. Gate yazım formatı

Her gate şu alanlarla tanımlanmalı:

gateId
family
balanceTier
titleKey
tag1Key
tag2Key
descriptionKey
bestSynergy
firstBand
isBossPrepOnly
Kural

Görünen UI yalnızca:

titleKey
tag1Key
tag2Key

kullanır.

descriptionKey daha çok:

codex
detay panel
debug
stage preview
için kalır.
7. World 1 için aktif gate çekirdeği
7.1 Erken oyun (1–5)

Amaç:

okunurluk
en sade karar

Aktif gate aileleri:

Power
Tempo
Army
Sustain

Örnek kapılar:

Hardline
Overclock
Reinforce: Piyade
Medkit
Field Repair
Kural

Solve ve Tactical çok erken yüklenmez.

7.2 Build Discovery (6–10)

Amaç:

solve değerini öğretmek

Eklenenler:

Solve
ilk Tactical kıvılcımları

Örnek kapılar:

Breacher
Piercing Round
Execution Line
Kural

Oyuncu burada ilk kez “yanlış cevapla run uzuyor” hissini görmeli.

7.3 First Friction (11–15)

Amaç:

Shotgun
yakın risk / reward
mekanik support etkisi

Eklenenler:

Geometry
Army’nin yeni alt varyantları

Örnek:

Scatter Chamber
Reinforce: Mekanik
7.4 Controlled Complexity (16–20)

Amaç:

Launcher
wave geometry
support pack cezalandırma

Örnek:

Payload Chamber
line punish / splash buff
mixed tactical kapılar
7.5 Specialization (21–25)

Amaç:

build yönü gerçekten ayrışsın

Burada:

daha güçlü Solve
daha anlamlı Army destekleri
daha net Tactical seçimleri
aktif olur
7.6 Pressure & Punishment (26–30)

Amaç:

yanlış kapı = hissedilen ceza
doğru solve = görünür rahatlama

Burada:

kapılar daha “karar testi” olur
düz +damage seçimi her zaman en iyi hissettirmez
7.7 Final Prep (31–34)

Amaç:

Beam tanıtımı
boss prep
son run kimliği

Aktif olur:

Boss Prep
Beam sinerjili Solve/Tactical
uzun dövüş odaklı kapılar

Örnek:

Conductor Lens
Final Prep: Stabilizer
8. Yeni örnek gate seti
Power
Hardline
Etki: +8% Weapon Power
Amaç: en güvenli genel güç
Tag: POWER • SAFE
Sharpened Core
Etki: +5 Flat Damage
Amaç: düşük taban hasarlı silahlara görünür destek
Tag: DAMAGE • CORE
Tempo
Overclock
Etki: +10% Fire Rate
Amaç: tempo öğretimi
Tag: TEMPO • DPS
Rush Circuit
Etki: Kills slightly boost firing rhythm
Amaç: kill-chain hissi
Tag: TEMPO • CHAIN
Solve
Breacher
Etki: +12 Armor Pen
Amaç: armor solve
Tag: ARMOR • ELITE
Execution Line
Etki: +12% Elite Damage
Amaç: elite solve
Tag: ELITE • HUNT
Piercing Round
Etki: +1 Pierce
Amaç: line solve
Tag: PIERCE • LINE
Geometry
Scatter Chamber
Etki: Close spread becomes stronger
Amaç: shotgun identity
Tag: CLOSE • BURST
Payload Chamber
Etki: Blast radius increased
Amaç: launcher cluster punish
Tag: AREA • PACK
Converging Path
Etki: Projectiles keep tighter line
Amaç: assault/sniper geometry control
Tag: LINE • CONTROL
Army
Reinforce: Piyade
Etki: +2 Infantry
Amaç: erken support büyümesi
Tag: ARMY • SAFE
Reinforce: Mekanik
Etki: +1 Mechanic
Amaç: yakın baskı desteği
Tag: ARMY • FRONT
Reinforce: Teknoloji
Etki: +1 Tech
Amaç: special target support
Tag: ARMY • SUPPORT
Field Repair
Etki: Soldiers heal 35%
Amaç: support sustain
Tag: ARMY • HEAL
Sustain
Medkit
Etki: Commander +25% HP
Amaç: hata affı
Tag: HEAL • SAFE
Fortified Frame
Etki: Take less contact damage
Amaç: charger/brute hata affı
Tag: SURVIVE • CONTACT
Tactical
Target Uplink
Etki: High-priority targets take extra team focus
Amaç: elite/sniper/tech sinerjisi
Tag: MARK • ELITE
Breakpoint
Etki: Low-health enemies are finished faster
Amaç: finisher build
Tag: EXECUTE • TEMPO
Threat Pulse
Etki: Approaching priority threats become easier to read
Amaç: oyuncuya açık tavsiye vermeden okunurluk desteği
Tag: READ • THREAT
Boss Prep
Conductor Lens
Etki: Beam performs better in long fights
Amaç: Beam prep
Tag: BEAM • BOSS
Final Prep: Stabilizer
Etki: Safer sustained damage for long encounters
Amaç: boss öncesi güvenli solve
Tag: LONG • SAFE
Siege Focus
Etki: Single-target pressure increases in boss-like fights
Amaç: sniper/assault boss prep
Tag: BOSS • FOCUS
9. Gate ile build ilişkisi
Assault buildler

En iyi:

Power
Tempo
hafif Solve
güvenli Army
SMG buildler

En iyi:

Tempo
Army
bazı Tactical
swarm odaklı Geometry
Sniper buildler

En iyi:

Solve
Tactical
Boss Prep
sınırlı Power
Shotgun buildler

En iyi:

Geometry
Army (Mekanik)
Sustain
kısa solve
Launcher buildler

En iyi:

Geometry
Tactical
support pack solve
Beam buildler

En iyi:

Boss Prep
Solve
Teknoloji support
uzun dövüş Tactical
10. Şimdi dikkat etmemiz gereken riskler
Risk 1

Gate sayısı artar ama aileler çakışırsa oyuncu hepsini aynı hisseder.

Risk 2

Çok fazla Solve gate koyarsak “yanlış cevap” duygusu fazla sert olur.

Risk 3

Army gate’ler sadece sayı artışı gibi kalırsa support layer anlamsız görünür.

Risk 4

Boss Prep çok erken görünürse Beam ve final prep dili bozulur.

Risk 5

Tactical gate fazla soyut kalırsa oyuncu niye aldığını anlamaz.

11. Benim ek önerilerim
1. Gate rolleri renk diline bağla
Power = kırmızı/turuncu
Tempo = sarı
Solve = mor/turuncu karışımı
Geometry = mavi
Army = çelik/mavi
Sustain = yeşil
Tactical = mor
Boss Prep = beyaz/altın/enerji

Bu çok şey kazandırır.

2. Gate başlıkları mümkün olduğunca effect-first olsun

Yani:

+12 Armor Pen
+1 Pierce
+2 Infantry

Bu daha iyi.

3. Tag sistemini shared-key üstünden yürüt

Her gate için özel kısa tag yazmaya çalışma.
Localization’da çok iş çıkarır.

4. Stage band görünürlüğü GatePoolConfig üstünden yürüsün

Yani gate redesign önce veri sözleşmesi olarak bitsin, sonra pool dağılımı yapılır.