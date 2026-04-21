\# Top End War — Combat Readability Contract v1

\_If the player cannot read it, it does not exist\_



\---



\## 0. Belgenin amacı



Bu belge, World 1 boyunca oyuncunun savaş alanında neyi:

\- görmesi

\- ayırt etmesi

\- anlaması

\- önemsemesi



gerektiğini tanımlar.



\### Temel ilke

Bir sistem tasarımda var diye oyuncu onu yaşamış sayılmaz.  

Oyuncu:

\- tehdidi görebilmeli

\- tehdidi anlamlandırabilmeli

\- doğru kararı yeterince hızlı verebilmeli



\### Kural

\*\*Okunmayan mekanik, oyuncu için yoktur.\*\*



\---



\## 1. Combat readability’nin ana hedefi



Oyuncu savaş sırasında 1–2 saniye içinde şunları anlayabilmeli:



1\. Şu an en tehlikeli şey ne?

2\. Hangi düşman türü geliyor?

3\. Benim build’im burada iyi mi kötü mü?

4\. Kapı ne veriyor?

5\. Boss ne yapacak?

6\. Ben vuruyor muyum, boşa mı gidiyor?



\---



\## 2. Okunurluk katmanları



Combat readability 5 katmandan oluşur:



\### A. Threat Readability

Tehdit ne?



\### B. Build Readability

Ben ne oynuyorum?



\### C. Damage Readability

Vuruyor muyum, etkili mi?



\### D. Spatial Readability

Ekranda neresi güvenli / tehlikeli?



\### E. Tempo Readability

Baskı artıyor mu, rahatlama mı var?



\---



\## 3. Threat Readability



\## 3.1 Düşman silüeti

Her düşman ilk bakışta ayrışmalı.



\### Trooper

\- nötr silüet

\- en sade görünüm

\- baseline düşman



\### Swarm

\- daha küçük

\- daha çevik

\- grup hissi



\### Charger

\- öne eğik

\- agresif duruş

\- ani tehdit hissi



\### Armored Brute

\- büyük

\- ağır

\- tok / blok gibi silüet



\### Elite Charger

\- charger’a benzer ama daha net vurgulu

\- elite ayrımı ilk bakışta okunmalı



\### Boss

\- normal düşmanla karışmayacak kadar farklı



\---



\## 3.2 Düşman renk dili

Renk doğrudan bilgi taşımalı.



\### Temel öneri

\- normal düşman = nötr

\- armor vurgusu = sarı / metalik

\- elite = daha sıcak / daha parlak vurgu

\- boss = ayrı özel ton



\### Kural

Renk süs değil, bilgi taşıyacak.



\---



\## 3.3 Düşman hareket dili

Okunurluk sadece modelle değil, hareketle de gelir.



\### Trooper

\- düz / sade akış



\### Swarm

\- daha sıkışık / daha canlı akış



\### Charger

\- hazırlık → atılım

\- net telegraph



\### Brute

\- yavaş ama ağır

\- “geliyor” hissi



\### Kural

Aynı silüet + aynı hareket = ayırt edilemeyen düşman



\---



\## 4. Build Readability



\## 4.1 Oyuncu ne oynadığını anlamalı

Oyuncu kendi build’inin hissini sahada görmeli.



\### Örnek

\- SMG build → çok hedefe hızlı tepki

\- Sniper build → ağır hedeflerde net verim

\- Launcher build → paket patlatma hissi

\- Beam build → uzun temas / sürekli baskı



\### Kural

Build farkı yalnızca sayılarda değil, sahadaki hissiyatta da görünmeli.



\---



\## 4.2 Squad okunurluğu

Squad sadece arkadan gelen kalabalık olmamalı.



Oyuncu şunu görebilmeli:

\- takım büyüdü mü

\- support tipi değişti mi

\- takım gerçekten katkı veriyor mu



\### Kural

Ordu görünür ama ekranı kapatmaz.



\---



\## 4.3 Build Snapshot desteği

Loadout ve stage card’daki snapshot, sahadaki hisle uyumlu olmalı.



Yanlış örnek:

\- snapshot “Armor Break”

\- ama sahada hiçbir fark hissedilmiyor



Doğru örnek:

\- snapshot “High Tempo”

\- sahada gerçekten daha hızlı akış hissediliyor



\---



\## 5. Damage Readability



\## 5.1 Oyuncu vurduğunu hissetmeli

Şu 4 soru sahada okunmalı:



1\. isabet var mı?  

2\. hasar etkili mi?  

3\. armor’a takıldı mı?  

4\. elite/boss’a özel verim var mı?



\---



\## 5.2 Hit feedback katmanları



\### Normal hit

\- kısa, sade

\- küçük hit hissi



\### Strong hit

\- daha tok

\- daha görünür popup / impact



\### Armor hit

\- “vuruyorum ama tam işlemiyor” hissi

\- farklı ses / farklı renk / farklı impact



\### Elite/Boss hit

\- daha ciddi hissedilmeli

\- ama ekranı spamlememeli



\---



\## 5.3 Damage popup kuralları

Popup’lar okunurluğu desteklemeli, öldürmemeli.



\### Kural

\- az ve anlamlı

\- çok büyük sayı spam yok

\- kritik veya anlamlı vurular daha görünür

\- her mermi aynı drama seviyesinde görünmez



\---



\## 5.4 Boşa atış hissi

Bu çok kritik.



Oyuncu şunu hissetmemeli:

> “Hepsi en öndekine boş atıyor.”



\### Bunun için

\- hedef dağılımı okunur olmalı

\- kill dağılımı biraz çeşitlenmeli

\- düşman ölümü ve hedef geçişi akıcı olmalı



\### Kural

Hedefleme sistemi optimize edilmese bile \*\*algı olarak boşa atış hissi azaltılmalı\*\*.



\---



\## 6. Spatial Readability



\## 6.1 Oyuncu nerede olduğunu anlamalı

Ekranda:

\- oyuncu

\- squad

\- yaklaşan düşman

\- tehlike alanı

\- gate

aynı anda okunabilir olmalı



\### Kural

Oyuncu “ekranda kayboldum” dememeli.



\---



\## 6.2 Yol / lane okunurluğu

Yol çok dar ya da çok belirsiz olmamalı.



Oyuncu:

\- nereye kayabileceğini

\- ne kadar alanı olduğunu

\- düşmanın hangi taraftan geldiğini

hissetmeli



\---



\## 6.3 Tehlike alanı okunurluğu

Boss ve özel saldırılarda:

\- tehlike alanı net telegraph almalı

\- çok geç değil

\- çok erken değil



\### Kural

Alan tehdidi görünmez olmamalı.



\---



\## 7. Tempo Readability



\## 7.1 Oyuncu baskının arttığını hissetmeli

Stage düz çizgi gibi akmamalı.



Oyuncu şunu sezebilmeli:

\- şimdi rahat an

\- şimdi baskı artıyor

\- şimdi spike geldi

\- şimdi nefes var



\### Bu nasıl hissedilir?

\- packet yapısı

\- enemy çeşitlenmesi

\- spacing

\- ses / müzik yoğunluğu

\- ekrandaki hedef yoğunluğu



\---



\## 7.2 Relief anları görünür olmalı

Relief sadece matematikte değil, histe de relief olmalı.



Yanlış örnek:

\- sayısal relief var ama ekranda hâlâ aynı kaos sürüyor



Doğru örnek:

\- relief packet gelince oyuncu gerçekten toparlanıyor



\---



\## 8. Gate Readability



\## 8.1 Kapılar 1 saniyede okunmalı

Format sabit:



```text id="svue5x"

ANA ETKI

TAG • TAG

Kural

effect-first

2 kısa tag

paragraf yok

8.2 Gate renk dili



Gate aileleri renklerle desteklenebilir.



Öneri

Power = kırmızı / turuncu

Tempo = sarı

Solve = mor / sıcak vurgu

Geometry = mavi

Army = çelik / mavi

Sustain = yeşil

Tactical = mor / koyu vurgu

Boss Prep = açık enerji / altın

Kural



Renk bilgi taşımalı, ama metni gereksiz kılmamalı.



8.3 Gate kıyaslama okunurluğu



İki kapı arasındaki fark hızlı anlaşılmalı.



Oyuncu:



biri güvenli

biri solve

biri army

gibi farkı çok hızlı ayırt edebilmeli

9\. Boss Readability

9.1 Boss ekranda ayrı hissettirmeli



Boss geldiğinde oyuncu:



özel an başladığını

riskin yükseldiğini

normal wave’den farklı olduğunu

anlamalı

9.2 Boss telegraph



Her ağır saldırıda:



net ön sinyal

net yön

net zamanlama



olmalı.



Kural



Boss saldırısı:



görünmeden vurmaz

tutarsız davranmaz

9.3 Boss punish window



Oyuncu boss’un açığını fark edebilmeli.



Kural



Punish window:



küçücük olabilir

ama görünür olmalı

10\. UI Readability

10.1 HUD neyi göstermeli?



Mutlaka:



commander HP

squad summary

boss HP / faz

threat/gate okunurluğunu destekleyen minimum bilgi

Göstermemeli

uzun açıklama

çok satırlı combat rehberi

ekranı boğan ek panel yığını

10.2 Stage Card



Oyuncuya sadece:



stage adı

2–3 threat tag

reward hissi

opsiyonel build snapshot ilişkisi



göstermeli



10.3 Loadout



Oyuncu kendi build’ini kısa anlamalı:



ana silah

support yönü

build snapshot

11\. Ses okunurluğu

11.1 Ses de bilgi taşımalı



Sadece görsel değil.



Olmalı

elite spawn cue

charge prep cue

armor hit cue

boss telegraph cue

reward / victory cue

Kural



Ses bilgi taşımalı ama rahatsız edici spam olmamalı.



12\. Mobil ekran kuralı



Bu oyun mobil okunurlukla yaşar veya ölür.



Kural



Ekranda aynı anda:



oyuncu

squad

düşman grubu

gate

okunabilmeli

Yasak

ekrandan taşan ordu

aşırı küçük threat

mobilde fark edilmeyen telegraph

panel yığını

13\. Readability öncelik sırası



Eğer bir şey görünmüyorsa şu sırayla düzeltilir:



Threat silüeti

Tehlike alanı

Gate okunurluğu

Squad sıkışması

Damage popup spam

Görsel süs

Kural



Önce bilgi okunur, sonra güzellik gelir.



14\. Combat Readability checklist



Bir testte şu sorular sorulmalı:



Threat

Hangi düşmanın tehlikeli olduğu ilk bakışta anlaşılıyor mu?

Build

Kendi build’imin neye iyi olduğunu hissediyor muyum?

Damage

Vuruyor muyum, etkili vuruyor muyum?

Space

Ekranda nerede olduğumu biliyor muyum?

Tempo

Baskı ve relief anlarını hissediyor muyum?

Gate

Kapıyı okuyabiliyor muyum?

Boss

Ne zaman tehlike geliyor anlayabiliyor muyum?

15\. Son karar özeti



Combat Readability Contract v1:



oyun hissinin gerçekten oyuncuya geçmesini sağlar

bütün sistemleri görünür ve anlaşılır hale getirmeyi hedefler

threat, build, damage, space, tempo, gate ve boss okunurluğunu tek çatı altında toplar

“oyuncu görmüyorsa mekanik yoktur” ilkesini kanonik kılar

