# Top End War — World 1 Combat & Build Bible v2

## 0. Belgenin statüsü

Bu belge:

- “şu anki en doğru tasarım şemasıdır”
- test sonrası gerektiğinde revize edilir
- kodun ve Unity kurulumunun referans alacağı kanonik sürümdür
- eski vertical slice kısıtlarını birebir kopyalamaz
- World 1’i oyunun gerçek öğretim alanı olarak ele alır

**Kural**

Burada yazan bir karar:

- testten önce doğru kabul edilir
- test sonrası gerekirse bilinçli olarak güncellenir
- rastgele unutularak delinmez

## 1. World 1’in görevi

World 1 yalnızca ilk 10 stage’lik bir demo alanı değildir.  
World 1, oyunun tüm temel oynanış dilini öğretir ve oyuncuya şu sistemi kavratır:

- silah aileleri farklı problemleri çözer
- build yalnızca gate’den oluşmaz
- commander loadout, soldier desteği, gate seçimi ve stage bilgisi birlikte çalışır
- düşmanlar sadece HP kutusu değildir; karar testidir
- boss’lar yeni mekanik öğretmez, önce öğretileni sınar
- oyuncu tek bir “doğru build”e zorlanmaz

**World 1 sonunda oyuncu şunu anlamış olmalı:**

- hangi silah neyi çözer
- hangi gate hangi build’i büyütür
- hangi düşman hangi cevabı ister
- armor / elite / swarm / lane baskısı ne demektir
- ordu desteği nasıl güç verir
- boss öncesi hazırlık neden önemlidir

## 2. Combat identity

### 2.1 Silah aileleri

World 1 boyunca oyunun gerçek silah dili öğretilir.

**Assault**

**Rol:**
- güvenli genelci
- karışık dalga çözümü
- yüksek güvenilirlik
- “asla kötü değil” silahı

**Güçlü olduğu alan:**
- mixed wave
- tutarlı DPS
- öğrenme aşaması

**Zayıf olduğu alan:**
- çok ağır armor kontrolü
- aşırı yüksek tek hedef burst
- saf lane wipe

**SMG**

**Rol:**
- swarm temizliği
- yüksek tempo
- lane baskısı
- yakın-orta menzil akış silahı

**Güçlü olduğu alan:**
- çok hedef
- hızlı tehdit temizleme
- tempo odaklı build

**Zayıf olduğu alan:**
- brute / heavy armor
- boss uzun dövüşü
- yüksek tek atımlık çözüm

**Sniper**

**Rol:**
- elite / brute / mini-boss çözümü
- armor farkındalığı
- sabırlı ama güçlü hedef temizleme

**Güçlü olduğu alan:**
- armor
- elite
- yüksek öncelikli hedef

**Zayıf olduğu alan:**
- swarm
- sürekli yakın baskı
- lane spam

**Shotgun**

**Rol:**
- yakın alan patlayıcı güç
- riskli ama tatmin edici çözüm
- ön sıra kırıcı

**Güçlü olduğu alan:**
- sıkışık pack
- yakın baskı
- mekanik asker sinerjisi

**Zayıf olduğu alan:**
- uzak tehdit
- dağınık dalga
- boss’a güvenli DPS

**World 1 öğretim bandı**  
ilk gerçek tanıtım: 11–15  
tam anlamlı kullanım: 16–25

**Launcher**

**Rol:**
- alan hasarı
- pack punish
- kontrollü gecikmeli güç

**Güçlü olduğu alan:**
- kümeli düşmanlar
- support pack arkası
- ağır kompozisyon çözümü

**Zayıf olduğu alan:**
- tek hızlı hedef
- çok hareketli charger tipi tehdit
- sürekli yakın tepki

**World 1 öğretim bandı**  
ilk tanıtım: 16–20  
uzmanlaşma değeri: 21–30

**Beam**

**Rol:**
- sürekli baskı
- elite / boss çözümü
- geç oyun hazırlık silahı

**Güçlü olduğu alan:**
- boss
- elite
- uzun temas süresi olan hedefler

**Zayıf olduğu alan:**
- çok geniş swarm
- anlık lane wipe
- yanlış pozisyonda kalan oyuncu

**Beam kuralı**

Beam, World 1 final bossundan önce tanıtılır.  
Ama final boss için zorunlu cevap değildir.

**Beam öğretim planı**  
- Beam ilk kez final prep bandında açılır  
- oyuncu önce onu küçük güvenli bir karşılaşmada görür  
- sonra bir önceki mini-boss / pre-boss sınavında Beam’in boss benzeri hedeflere karşı neden güçlü olduğu gösterilir  
- final boss öncesi oyuncuya seçim bırakılır:  
  - Beam ile git  
  - mevcut build’ine sadık kal  
  - hibrit cevap kur

**Kural**

Beam:

- “boss silahı” diye tek doğru cevap olmayacak  
- ama “bossa karşı niye iyi olduğu” açıkça öğretilecek

### 2.2 Silah tasarım ilkesi

Silahlar:

- strict upgrade değildir
- sidegrade mantığındadır
- kağıt üzerindeki ham DPS yakın olabilir
- gerçek güçleri farklı problem türlerinde ortaya çıkar

**Bu yüzden denge kuralı**

Bir silah:

- her durumda en iyi olamaz
- ama kendi problemini çözerken açıkça parlamalıdır

## 3. Build language

Build yalnızca gate’ten oluşmaz.

World 1 build dili **4 katmanlıdır**:

1. **Commander Layer**  
   Komutanın:  
   - ana silah ailesi  
   - ekipmanı  
   - rarity/modifier’ları  
   - boss ve armor cevapları

2. **Squad Layer**  
   Askerlerin:  
   - chassis tipi  
   - sayısı  
   - destek silah yönü  
   - build’i destekleme rolü

3. **Run Layer**  
   Run içinde alınan:  
   - gate etkileri  
   - reinforcement  
   - sustain  
   - tactical çözüm kapıları

4. **Stage Knowledge Layer**  
   Oyuncunun:  
   - stage’de hangi tehditlerin geleceğini bilmesi  
   - hangi cevabın mantıklı olduğunu öğrenmesi  
   - build’i sadece “çok sayı” değil “doğru sayı” olarak kurması

### 3.1 Build formülü

Bir build’in gücü şu bileşenlerden oluşur:

- Commander Weapon
- Commander Equipment Modifiers
- Soldier Support Layer
- Gate Effects
- Stage Knowledge

**Sonuç**  
Gate önemli ama tek başına build değildir.

### 3.2 Soldier sistemi build içinde nasıl yer alır

Askerler mikro yönetim sistemi değildir.  
Ama build görünürlüğünün büyük parçasıdır.

**World 1 soldier contract**  
- askerler otomatik destek ateşi verir  
- komutana bağlı hareket eder  
- sayıları artabilir  
- iyileşebilir  
- build’in açık parçası olarak görünür

**Soldier chassis’ler**  
- Piyade = dengeli destek  
- Mekanik = ön sıra / yakın baskı desteği  
- Teknoloji = arka sıra / özel hedef desteği

**World 1 kuralı**

- Erken oyunda: Piyade daha baskın öğretilir  
- Orta oyunda: Mekanik ve Teknoloji desteği açılır  
- Geç World 1’de: oyuncu hangi destek tipini büyüttüğünü hisseder

### 3.3 OP build ve kırık build felsefesi

Oyuncu:

- sadece “en düz güvenli build” değil  
- özel sinerjiler kurabilmeli  
- bazen aşırı güçlü hissettiren kombinasyon yakalayabilmeli

Ama bu güç:

- tek bir zorunlu metaya dönüşmemeli  
- stage bilgisi ve kararla desteklenmeli

**World 1’de izin verilen eğlenceli build örnekleri**  
- Assault + Tempo + Reinforce = güvenli tempo ordusu  
- SMG + Army + sustain = lane baskı sürüsü  
- Sniper + Breacher + Elite damage = armor/elite avcısı  
- Shotgun + Mekanik = yakın alan temizleme  
- Launcher + Tactical geometry = kümeli dalga cezalandırma  
- Beam + boss prep + teknoloji desteği = final prep boss avı

## 4. Gate economy

### 4.1 Gate aileleri

Yeni sistemde gate aileleri şu şekilde okunur:

- Power = ham güç
- Tempo = ateş hızı / döngü hızı
- Penetration = armor / elite / pierce çözümleri
- Geometry = mermi davranışı / alan etkisi / çizgi kontrolü
- Army = asker sayısı / asker tipi / asker toparlama
- Sustain = HP / iyileşme / hata telafisi
- Tactical = özel problem çözümü
- Boss Prep = geç World 1’de bossa hazırlık odaklı kalıcı güçler

### 4.2 Gate güç bantları

Her gate aynı güçte olmaz.

**Minor Gate**  
küçük ama temiz katkı  
yaklaşık %4–6 gerçek güç  
erken öğretim için iyi

**Standard Gate**  
ana seçim kapısı  
yaklaşık %8–12 gerçek güç  
World 1’in omurgası

**Solve Gate**  
her durumda güçlü değildir  
doğru eşleşmede çok değerlidir  
yaklaşık %0–35 efektif fark (armor pen, elite damage, beam prep gibi)

**Army Gate**  
direkt DPS gibi görünmeyebilir  
ama efektif savaş gücü sağlar  
yaklaşık %6–18 efektif katkı (kompozisyona göre değişir)

**Sustain Gate**  
direkt DPS vermez  
hata toleransı ve süreklilik verir  
özellikle zor bandlarda değerlidir

**Boss Prep Gate**  
erken oyunda çıkmaz  
final prep bandında görünür  
final sınavına özel anlam taşır

### 4.3 Gate tasarım formatı

Her gate bible’da şu formatla yazılmalı:

- Etki
- Gizli tasarım amacı
- En iyi sinerji
- Zayıf kullanım senaryosu
- İlk görünme bandı
- Güç bandı

### 4.4 World 1 için temel gate örnekleri

**Hardline**  
Etki: +8% Silah Gücü  
Amaç: en temiz genel güç kapısı  
En iyi sinerji: Assault / Sniper  
Zayıf kullanım: build problemi çözmez, sadece sayıyı artırır  
İlk band: 1–5  
Güç bandı: Standard

**Overclock**  
Etki: +10% Ateş Hızı  
Amaç: tempo öğretmek  
En iyi sinerji: SMG / Assault  
Zayıf kullanım: armor sorununu çözmez  
İlk band: 1–5  
Güç bandı: Standard

**Breacher**  
Etki: +12 Armor Pen  
Amaç: armor’un sayı değil cevap problemi olduğunu öğretmek  
En iyi sinerji: Sniper / Beam / Assault  
Zayıf kullanım: armorsuz stage’de düşük değer  
İlk band: 6–10  
Güç bandı: Solve

**Piercing Round**  
Etki: +1 Pierce  
Amaç: çizgi temizleme ve geometry öğretmek  
En iyi sinerji: Sniper / Assault  
Zayıf kullanım: dağınık dalgada etkisi düşer  
İlk band: 6–15  
Güç bandı: Solve

**Reinforce: Piyade**  
Etki: +2 Piyade  
Amaç: soldier layer’ı ilk kez görünür kılmak  
En iyi sinerji: Assault / güvenli build  
Zayıf kullanım: kötü formasyonda ekranı doldurur ama çözüm üretmez  
İlk band: 1–10  
Güç bandı: Army

**Reinforce: Mekanik**  
Etki: +1 Mekanik  
Amaç: yakın baskı desteğini açmak  
En iyi sinerji: SMG / Shotgun  
Zayıf kullanım: uzak çözüme ihtiyacın varken değer düşer  
İlk band: 11–20  
Güç bandı: Army

**Reinforce: Teknoloji**  
Etki: +1 Teknoloji  
Amaç: özel hedef desteğini açmak  
En iyi sinerji: Sniper / Beam / Launcher  
Zayıf kullanım: swarm stage’de etkisi düşük kalabilir  
İlk band: 16–25  
Güç bandı: Army

**Medkit**  
Etki: Komutan +25% HP  
Amaç: hata affı ve öğrenme desteği  
En iyi sinerji: erken oyun / riskli oyuncu  
Zayıf kullanım: temiz oynayan için düşük tavan  
İlk band: 1–10  
Güç bandı: Sustain

**Field Repair**  
Etki: Askerler %35 iyileşir  
Amaç: army layer sürdürülebilirliğini öğretmek  
En iyi sinerji: reinforce build’leri  
Zayıf kullanım: ordun küçükse düşük değer  
İlk band: 1–15  
Güç bandı: Sustain

**Execution Line**  
Etki: +12% Elite Hasarı  
Amaç: elite hedef önceliğini öğretmek  
En iyi sinerji: Sniper / Beam  
Zayıf kullanım: elitsiz stage’de düşük değer  
İlk band: 6–15  
Güç bandı: Solve

**Scatter Chamber**  
Etki: kısa menzilde ek pellet / spread iyileştirmesi  
Amaç: Shotgun kimliğini parlatmak  
En iyi sinerji: Shotgun / Mekanik  
Zayıf kullanım: uzak ve dağınık kompozisyon  
İlk band: 11–20  
Güç bandı: Standard

**Payload Chamber**  
Etki: splash radius / blast verimliliği artar  
Amaç: Launcher’ın “pack punish” rolünü öğretmek  
En iyi sinerji: Launcher  
Zayıf kullanım: tek hedef  
İlk band: 16–25  
Güç bandı: Solve

**Conductor Lens**  
Etki: beam uptime / elite-boss verimliliği artar  
Amaç: Beam’i final prep diline bağlamak  
En iyi sinerji: Beam / Teknoloji  
Zayıf kullanım: swarm ağırlıklı stage  
İlk band: 31–34  
Güç bandı: Boss Prep

**Final Prep: Stabilizer**  
Etki: boss sırasında ilk hata affı / beam veya sniper için güvenli güç  
Amaç: final boss öncesi oyuncuya tek yol dayatmadan hazırlık vermek  
En iyi sinerji: Sniper / Beam / Assault  
Zayıf kullanım: swarm odaklı stage’de düşük değer  
İlk band: 31–34  
Güç bandı: Boss Prep

## 5. World 1 öğretim bantları

### 5.1 1–5 Tutorial Core

Öğretilen:
- hareket
- auto-shoot
- basic gate reading
- Assault ve SMG’nin temel farkı
- soldier desteğinin ilk görünümü
- sustain neden vardır

### 5.2 6–10 Build Discovery

Öğretilen:
- armor
- brute çözümü
- elite önceliği
- sniper’ın gerçek rolü
- ilk mini-boss sınavı

### 5.3 11–15 First Friction

Öğretilen:
- güvenli build her şeyi çözmez
- yakın risk / reward
- Shotgun tanıtımı
- Mekanik destek fikri

### 5.4 16–20 Controlled Complexity

Öğretilen:
- Launcher
- geometry ve pack punishment
- karışık wave çözümü
- yanlış kapının neden kötü hissettirdiği

### 5.5 21–25 Specialization

Öğretilen:
- oyuncu kendi build yönünü seçebilir
- tüm buildler aynı görünmez
- soldier support katmanı daha görünür olur

### 5.6 26–30 Pressure & Punishment

Öğretilen:
- hatalı build cezalandırılır
- sustain ve solve gate’lerin değeri artar
- “her şeye assault” güveni azalır

### 5.7 31–34 Final Prep

Öğretilen:
- final bossa hazırlık
- Beam tanıtımı
- Beam’in bossa neden iyi olduğu
- ama zorunlu olmadığı
- son loadout kararı

### 5.8 35 Final Boss

Sınanan:
- silah bilgisi
- gate bilgisi
- build tercihi
- threat önceliği
- boss okuma

## 6. Screen flow

World 1 deneyimi şu ekran akışıyla okunur:

- Splash / giriş
- Ana menü
- World map
- Stage kartı
- Loadout
- Runner
- Mini-boss / Boss
- Victory / Fail
- Reward
- Upgrade
- World map’e dönüş

**Kural**  
Bu akışın hiçbir halkası “sonradan bakarız” diye boş kalmamalı.  
Placeholder olabilir ama boş kalmamalı.

## 7. UI ilkeleri

**Runner HUD’de mutlaka görünenler**
- komutan HP
- birlik özeti
- yaklaşan gate ana etkisi
- tehlike alanı / telegraph
- boss HP ve faz göstergesi

**Runner HUD’de görünmemesi gerekenler**
- uzun paragraf açıklamalar
- oyuncuya açık çözüm tavsiyesi
- fazla kayan yazı
- gereksiz panel kalabalığı

**Gate metin formatı**  
üstte ana etki  
altta 2 kısa etiket

**Örnek:**
+12 Zırh Delme
ARMOR • ELITE

## 8. Unity Hookup Notes

**Silah sistemi**
- Create > Top End War > WeaponArchetypeConfig ile aktif silah asset’lerini oluştur
- World 1 için en az: Assault, SMG, Sniper, Shotgun, Launcher, Beam archetype asset’leri hazırlanmalı
- EquipmentData.weaponArchetype alanına ilgili archetype bağlanmalı
- ArmyManager.weaponConfigs listesine aktif archetype’lar sürüklenmeli

**Enemy sistemi**
- EnemyArchetypeConfig asset’leri: Trooper, Swarm, Charger, Armored Brute, Elite Charger, Gatekeeper Walker
- SpawnManager archetype armor ve elite bilgisini Enemy.ConfigureCombat() tarafına gerçekten işletmeli
- enemy prefab’larında collider, health bar ve hit feedback doğrulanmalı

**Gate sistemi**
- GateConfig asset’leri gate ailelerine göre ayrılmalı
- GatePoolConfig stage bantlarına göre dağıtılmalı
- erken stage’lerde sadece okunur temel gate’ler aktif olmalı

**UI / test**
- portrait ratio’da test şart
- sessiz test yapılmamalı
- placeholder sesler aktif olmalı
- gate okunurluğu her bandda ayrı test edilmeli

## 9. Legacy / park sistemleri

Aşağıdakiler aktif World 1 çekirdeği değildir:

- pet
- morph
- arena
- alliance
- level editor
- CP merkezli eski combat dili
- gereksiz gear score odaklı combat açıklamaları

**Kural**  
Kodbase’de bulunmaları, tasarımda aktif oldukları anlamına gelmez.

## 10. Son karar özeti

World 1:

- oyunun gerçek öğretim alanıdır
- tüm temel combat dilini taşır
- build’i yalnızca gate’ten ibaret görmez
- commander + soldier + gate + stage knowledge birlikte çalışır
- Beam’i final boss öncesi tanıtır ama dayatmaz
- sidegrade silah felsefesini korur
- oyuncuya tek bir doğru cevap vermez
- testten sonra revize edilebilir, ama revize edilene kadar kanonik kabul edilir