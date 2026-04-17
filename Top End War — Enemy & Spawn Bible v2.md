# Top End War — Enemy & Spawn Bible v2
_World 1 Behavior, Threat, Pattern ve Teaching Contract_

---

## 0. Belgenin amacı

Bu belge, **World 1 boyunca kullanılacak düşman davranış dilini** ve **spawn grammar** kurallarını tanımlar.

Amaç:
- düşmanları yalnızca farklı HP değerleri olan objeler olmaktan çıkarmak
- her düşman tipinin oyuncudan farklı bir karar istemesini sağlamak
- spawn sistemini “ip gibi sıra halinde gelen düşmanlar” hissinden kurtarmak
- silah rolleri, gate seçimleri ve stage öğretim yapısı ile uyumlu bir düşman/spawn dili kurmak
- World 1 boyunca oyunun gerçek combat öğretimini tamamlamak

### Kural
Bu belge:
- mevcut bible’ın üstüne yazılmış güncel düşman/spawn kanonudur
- testten önce doğru kabul edilir
- testten sonra gerekirse revize edilir
- kod ve config tarafının referans alacağı davranış sözleşmesidir

---

## 1. Ana ilke

### 1.1 Çekirdek düşman dili
Düşmanlar:
- yalnızca daha yüksek HP ile değil
- oyuncudan istedikleri **karar** ile ayrılır

### 1.2 Spawn dili
Wave’ler:
- yalnızca belirli sayıda düşmanın sıraya dizilmesi değildir
- bir “karar baskısı kompozisyonu” olarak düşünülür

### 1.3 World 1 hedefi
World 1 sonunda oyuncu:
- hangi düşmanın neden tehlikeli olduğunu
- hangi silahın hangi probleme cevap verdiğini
- hangi gate’in hangi düşman tipine karşı değerli olduğunu
- aynı düşmanları farklı pattern’lerde görünce farklı karar vermesi gerektiğini
anlamış olmalıdır

---

## 2. Enemy design pillars

Her düşman 5 sütunda tanımlanır:

1. **Role**  
   Oyundaki temel görevi

2. **Threat**  
   Oyuncuyu neyle zorlar

3. **Readability**  
   Oyuncu onu ilk bakışta nasıl tanır

4. **Counter**  
   Hangi silah / build / gate daha iyi cevap verir

5. **Spawn Use**  
   Hangi pattern içinde kullanıldığında anlamlı olur

---

## 3. Enemy roster — World 1

World 1’in ana roster’ı:

- Trooper
- Swarm
- Charger
- Armored Brute
- Elite Charger
- Gatekeeper Walker
- War Machine
- World 1 Final Boss

### Not
Vertical slice’ta yalnızca ilk çekirdek roster aktif olabilir; fakat World 1 tasarımı bu genişletilmiş set üstünden kurulacaktır.

---

## 4. Enemy behavior matrix

---

## 4.1 Trooper

### Rol
Temel referans düşman.  
Oyuncunun ateş hissini, gate kararlarını ve baseline gücünü test eder.

### Oyuncudan istediği karar
- doğru hedef seçimi istemez
- temel hasar yeterli mi onu ölçer
- “silahım çalışıyor mu?” hissini verir

### Threat
Düşük-orta.  
Tek başına değil, kompozisyon içinde anlamlıdır.

### Hareket
- düz ilerleme
- hafif lane tracking
- ani hız patlaması yok
- hafif separation ile paket görünümünü korur

### Okunurluk
- standart silüet
- standart hız
- nötr renk
- baseline health bar

### Counter
- tüm silahlar kabul edilebilir
- Assault doğal referans cevap
- SMG çok sayıda trooper’da parlayabilir
- Sniper israf hissi verebilir ama yanlış değildir

### Spawn kullanımı
- Wall
- Stagger Line
- Dense Core filler
- Support pack

### Yasak kullanım
- tek başına dramatik tehdit olarak kullanılmaz
- aşırı uzun tek sıra halinde dizilip oyunu monotona bağlamaz

---

## 4.2 Swarm

### Rol
Sayı baskısı.  
Oyuncuya “tek hedef gücü” ile “alan/tempo çözümü” farkını öğretir.

### Oyuncudan istediği karar
- lane temizleme
- tempo artırma
- SMG/alan etkisi değerini fark etme
- güvenli silahla bile bazen boğulabileceğini anlama

### Threat
Düşük tekil tehdit, yüksek toplu tehdit.

### Hareket
- daha küçük / daha hızlı görünür
- tam düz çizgi yerine hafif dağınık kümeler halinde ilerler
- mikro zig-zag ya da hafif yan sapma olabilir
- pack bütünlüğü korunur ama asker dizisi gibi görünmez

### Okunurluk
- kısa silüet
- hızlı animasyon ritmi
- toplu gelişte net “sayı geliyor” hissi
- ses cue ile desteklenebilir

### Counter
- SMG ana doğal cevap
- Launcher ve Shotgun ileride güçlü cevap
- Assault orta cevap
- Sniper düşük verim

### Spawn kullanımı
- Dense Core
- Split Swarm
- Side Support
- Delayed flood

### Yasak kullanım
- tek tek büyük aralıkla gelmez
- brute veya elite rolünü taklit etmez

---

## 4.3 Charger

### Rol
Öncelik tehdidi.  
Oyuncunun “önce bunu durdur” kararını test eder.

### Oyuncudan istediği karar
- hedef önceliği
- kısa süre içinde tepki verme
- yanlış hedefte oyalanmama

### Threat
Orta-yüksek.  
Özellikle mixed wave içinde değerlidir.

### Hareket
- normal ilerleme ile başlar
- belli menzil ya da zaman eşiğinde kısa hazırlık yapar
- sonra hızlı bir öne atılım / dash uygular
- bu dash okunabilir olmalı, aniden “hile gibi” olmamalı

### Okunurluk
- öne eğilen hazırlık pozu
- kısa telegraph duruşu
- hızlanmadan önce net cue
- normal trooper’dan daha agresif silhouette

### Counter
- Assault güvenli cevap
- SMG zamanında fark ederse iyi çözer
- Sniper doğru anda çok tatmin edici çözer
- sustain / HP gate’i charger hatalarını affedebilir

### Spawn kullanımı
- Stagger Threat
- Mixed Lane Test
- Delayed Priority Threat
- Trooper pack üzerine bindirilmiş support threat

### Yasak kullanım
- telegraphsız anlık vur-kaç yapmaz
- sürekli spam edilip okunurluğu bozmaz

---

## 4.4 Armored Brute

### Rol
Armor check.  
Oyuncuya “ham DPS yetmiyor, doğru cevap lazım” dedirtir.

### Oyuncudan istediği karar
- armor pen / pierce / sniper / breacher değeri
- yanlış build ile oyunun uzadığını hissetme
- bazı düşmanların herkese aynı ölmediğini öğrenme

### Threat
Yüksek dayanıklılık baskısı, düşük tempo baskısı.

### Hareket
- ağır ve kararlı ilerler
- charger gibi atılmaz
- yavaş ama durdurulmaz hissi verir
- çok hafif lane correction yapar

### Okunurluk
- büyük gövde
- ağır animasyon
- daha tok hit sesi
- armor hit feedback’i farklı olmalı

### Counter
- Sniper ana doğal cevap
- Breacher / Piercing gate’leri yüksek değer
- Beam ve Teknoloji desteği ileri World 1’de güçlü cevap
- SMG düşük verim
- Assault çalışır ama tam doğru cevap değildir

### Spawn kullanımı
- Armor Check Pair
- Mixed Pressure
- Guard Unit
- Boss prep kompozisyonu

### Yasak kullanım
- çok sayıda brute aynı anda erken oyunda kullanılmaz
- swarm rolünü üstlenmez

---

## 4.5 Elite Charger

### Rol
Panik + öncelik çakışması.  
Oyuncuya “yüksek tehdit ama tek doğru hedef değil” hissi verir.

### Oyuncudan istediği karar
- elite önceliği
- mixed wave içinde risk değerlendirmesi
- tek büyük hedefe körlenmeden çevreyi de okuma

### Threat
Yüksek.

### Hareket
- charger mantığının daha sert versiyonu
- daha net telegraph
- daha güçlü atılım
- normal charger’dan biraz daha dirençli ve daha dikkat çekici

### Okunurluk
- renk tonu / glow / outline
- daha yüksek ses cue
- daha tok giriş
- health bar ya da üst işaret ile ayrışma

### Counter
- Sniper çok iyi cevap
- Assault makul cevap
- Beam ileri World 1’de güçlü cevap
- elite damage gate’leri burada değerli görünür

### Spawn kullanımı
- Elite Spike
- Mixed Punishment
- Final prep practice
- brute + elite çift baskısı

### Yasak kullanım
- çok erken aşamalarda öğretimsiz kullanılmaz
- sıradan charger yerine her yerde geçmez

---

## 4.6 Gatekeeper Walker

### Rol
İlk mini-boss.  
İlk gerçek build sınavı.

### Oyuncudan istediği karar
- tek hedef baskısı
- telegraph okuma
- greed DPS’in cezalandırılması
- armor farkındalığı
- soldier layer’ın gerçekten katkı verdiğini hissetme

### Threat
Boss seviyesi, ama öğretici adil sınav.

### Hareket / Faz yapısı
- Faz 1: baseline pattern
- Transition Lock
- Faz 2: aynı bilgileri daha baskılı kombinasyonla test eder

### Ana saldırılar
- Line Shot
- Front Sweep
- Short Charge

### Counter
- Assault güvenli genelci
- Sniper yüksek değer
- yanlış build yine de kazanabilir, ama zorlanır
- boss ayrı “tek doğru silah” istemez

### Spawn kullanımı
- boss encounter
- bu boss öncesi stage’lerde benzer davranış mikro örnekleri oyuncuya öğretilmelidir

---

## 4.7 War Machine

### Rol
Orta-geç World 1 mini-boss.  
Alan baskısı ve yapı bozma testi.

### Oyuncudan istediği karar
- hareket alanı okuma
- lane değiştirme
- geniş tehdit alanına karşı sabit kalmama
- sustained damage ve burst penceresi ayrımı

### Threat
Alan kontrolü + hata cezalandırma.

### Hareket
- yavaş ama baskın
- sabit pattern’li ağır saldırılar
- ekranın belirli kısmını kısa süreli tehlikeli hale getirir

### Counter
- Assault güvenli cevap
- Sniper pencere bazlı değerli
- Beam uzun süre tutunabildiğinde yüksek değer
- launcher burada zorunlu olmamalı

### Spawn kullanımı
- mini-boss only

---

## 4.8 World 1 Final Boss

### Rol
World 1’in final sınavı.  
Yeni mekanik öğretmez; tüm World 1 bilgisini sınar.

### Oyuncudan istediği karar
- build kararı
- gate bilgisi
- silah rolü bilgisi
- elite / armor / telegraph / lane baskısı sentezi

### Beam ilişkisi
Beam bu boss öncesi tanıtılır ve bossa karşı neden güçlü olduğu gösterilir.  
Ama final boss:
- Beam zorunlu check’i olmaz
- Beam kullanmayan build’lerle de çözülebilir
- Beam yalnızca “iyi cevaplardan biri” olarak konumlanır

---

## 5. Spawn grammar

Spawn sistemi şu soruya cevap vermelidir:

> “Bu wave oyuncudan hangi kararı istiyor?”

Wave tasarımında yalnızca adet ve HP düşünülmez.  
Wave bir **davranış kompozisyonu** olarak kurulur.

---

## 5.1 Spawn pattern ailesi

### Wall
Amaç:
- baseline temizleme
- lane coverage testi
- genel DPS okuma

İçerik:
- çoğunlukla trooper
- bazen araya 1 brute ya da 1 charger karışabilir

### Dense Core
Amaç:
- swarm / launcher / SMG / shotgun değerini göstermek

İçerik:
- swarm yoğun merkezi paket
- destek trooper’lar
- nadiren arka brute

### Stagger Line
Amaç:
- hedef sırası bozulmadan ama ip gibi görünmeden akış verme

İçerik:
- küçük zaman farklarıyla gelen trooper / charger karışımı
- aynı z çizgisine yapışmaz

### Lane Pinch
Amaç:
- oyuncuyu tek hatta kilitlenmekten çıkarmak

İçerik:
- sol ve sağ destek baskısı
- ortada brute / elite olabilir

### Delayed Threat
Amaç:
- oyuncu ilk pakete odaklanınca sonradan tehdit bindirmek

İçerik:
- önce trooper/swarm
- kısa gecikmeyle charger/elite giriş

### Guarded Core
Amaç:
- “önce korumayı mı temizleyeyim, ana hedefe mi vurayım?” sorusu

İçerik:
- merkez brute
- yanında trooper / charger / elite support

### Boss Prep Pack
Amaç:
- yaklaşan boss tipini küçük örneklerle öğretmek

İçerik:
- armor + elite + telegraph karışımı
- final prep bandında Beam veya Sniper’a doğal alan açar

---

## 5.2 Spawn pattern kuralları

### Kural 1
Aynı düşman aynı hareketle aynı aralıkla uzun süre akmamalı.

### Kural 2
Her wave’de bir “ana karar” olmalı.
Örnek:
- önce charger mı?
- brute’a uygun build’im var mı?
- swarm mı eritmeliyim?
- elite’i mi kesmeliyim?

### Kural 3
Düşman dizileri tam cetvel gibi görünmemeli.
Küçük:
- x jitter
- z jitter
- spawn delay farkı
- entry ordering
kullanılmalı

### Kural 4
Karışık wave, kaos demek değildir.
Oyuncu tehditleri okuyabilmeli.

---

## 6. Spawn density & timing rules

### 6.1 Genel pacing
Wave’ler:
- birbirinin üstüne yapışmamalı
- gate okuma temposunu bozmamalı
- seçim → kısa sonuç → yeni baskı döngüsü kurmalı

### 6.2 Entry timing
Aynı kompozisyondaki düşmanlar:
- milimetrik aynı anda doğmamalı
- çok uzun aralıklı da olmamalı
- “tasarlanmış ama doğal” görünmeli

### 6.3 Density
Erken oyunda:
- daha okunur
- daha geniş nefesli

Orta oyunda:
- kombinasyon başlar

Geç World 1’de:
- baskı artar
- ama okunurluk bozulmaz

---

## 7. Enemy x Weapon interaction matrix

## Assault için parlayan hedefler
- Trooper
- mixed wave
- Charger
- güvenli boss DPS

## SMG için parlayan hedefler
- Swarm
- dense core
- lane pinch support
- hızlı zayıf hedef temizliği

## Sniper için parlayan hedefler
- Armored Brute
- Elite Charger
- Gatekeeper Walker
- Beam öncesi boss-prep yüksek öncelikli hedefler

## Shotgun için parlayan hedefler
- sıkışık yakın paket
- mekanik destekli ön hat
- punish entry wave

## Launcher için parlayan hedefler
- kümeli support pack
- delayed dense reinforcements
- guarded core dış destekleri

## Beam için parlayan hedefler
- Elite
- mini-boss
- final prep pack
- final boss

---

## 8. World 1 teaching by enemy/spawn

## 1–5 Tutorial Core
Öğretilen:
- Trooper baseline
- Swarm farkı
- Charger önceliği
- basit wall ve stagger pattern’leri

### Yasaklar
- brute spam
- elite spike
- aşırı çift tehdit bindirmesi

---

## 6–10 Build Discovery
Öğretilen:
- Armored Brute
- armor pen / sniper değeri
- elite charger’ın ilk baskısı
- Gatekeeper Walker ile ilk gerçek sınav

### Kullanılacak pattern’ler
- Guarded Core
- Delayed Threat
- küçük mixed pack’ler

---

## 11–15 First Friction
Öğretilen:
- Shotgun’a alan açan yakın pack
- Mekanik destek rolü
- aynı build her şeyi çözmez

---

## 16–20 Controlled Complexity
Öğretilen:
- Launcher
- denser support packs
- lane pinch
- punish geometry

---

## 21–25 Specialization
Öğretilen:
- oyuncu build yönü seçebilir
- brute/elite/swarm oranları farklı cevaplar ister

---

## 26–30 Pressure & Punishment
Öğretilen:
- yanlış solve gate veya yanlış silah daha görünür cezalandırılır
- sustain ve tactical gate değeri artar

---

## 31–34 Final Prep
Öğretilen:
- Beam tanıtımı
- Beam’in boss benzeri hedeflerde neden iyi olduğu
- Beam’in zorunlu değil tercih olduğu
- final boss öncesi doğru hazırlık

### Kullanılacak pattern’ler
- Boss Prep Pack
- elite + brute senaryoları
- kısa mini sınav dalgaları

---

## 35 Final Boss
Sınanan:
- tüm World 1 enemy dili
- build tercihi
- doğru gate toplama
- telegraph okuma
- threat önceliği
- sustain kararı

---

## 9. Readability contract

Her düşman ilk bakışta ayrışmalıdır.

## Silüet
- Trooper = nötr
- Swarm = küçük/çevik
- Charger = agresif/öne eğik
- Brute = büyük/ağır
- Elite Charger = vurgulu/işaretli
- Boss = açıkça boss

## Renk / VFX
- elite’ler ayrı ton
- armor hit ayrı feedback
- boss telegraph ayrı vurgu

## Ses
- elite spawn cue
- charge prep cue
- armor hit cue
- brute impact cue
- boss telegraph cue

---

## 10. Implementation notes

### EnemyArchetypeConfig tarafında tutulmalı
- HPFactor
- Armor
- IsEliteLike
- MoveSpeed
- ContactDamage
- Reward değeri

### Spawn tarafında tutulmalı
- pattern family
- lane bias
- spawn delay / burst delay
- support pack gecikmesi
- world band erişimi

### Runtime tarafında yönetilmeli
- reservation
- threat weight
- charge trigger zamanı
- telegraph state
- boss phase state

---

## 11. Tuning notes

### Başlangıç tuning prensibi
Önce şu hissi yakala:
- her düşman okunuyor mu?
- spawn ip gibi görünmüyor mu?
- silah farkı kararda hissediliyor mu?
- gate seçimi düşmana karşı anlamlı mı?
- oyuncu yanlış build ile “neden zorlandığını” anlayabiliyor mu?

### İlk testte bakılacaklar
- swarm fazla dağınık mı?
- charger haksız mı hissettiriyor?
- brute çok mu yavaş / çok mu boş?
- elite yeterince ayırt ediliyor mu?
- aynı kompozisyon fazla tekrar mı ediyor?

---

## 12. Açık tasarım notları

Bunlar açık bırakılabilir ama not düşülmelidir:

- Backline Operator World 2’ye mi kalacak?
- Siege Brute World 1 son bandına mı, World 2’ye mi kayacak?
- Beam yalnızca commander silahı mı olacak, soldier support varyantı da olacak mı?
- bazı elite’lerin küçük özel davranış modları gerekli mi?
- bazı pattern’lerde environment/obstacle entegrasyonu olacak mı?

---

## 13. Son karar özeti

Enemy & Spawn sistemi:
- düşmanı HP kutusu gibi kullanmaz
- oyuncudan karar ister
- spawn’ı görsel sıra dizisi gibi değil, karar kompozisyonu gibi kurar
- silah ve gate rollerini görünür kılar
- World 1 boyunca oyunun gerçek combat dilini öğretir
- final boss öncesi Beam’i tanıtır ama dayatmaz
- testten sonra revize edilebilir, ama revize edilene kadar kanonik kabul edilir