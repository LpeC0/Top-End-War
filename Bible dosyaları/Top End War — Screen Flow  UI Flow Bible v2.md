# Top End War — Screen Flow / UI Flow Bible v2
_World 1 UI, Navigation, Readability ve Unity Hookup Contract_

---

## 0. Belgenin amacı

Bu belge, **Top End War** için World 1 boyunca kullanılacak ekran akışını, temel UI sözleşmelerini ve oyuncunun hangi bilgiyi nerede görmesi gerektiğini tanımlar.

Amaç:
- oyunun “boş hissediyor” sorununu çözmek
- sadece combat değil, **oyuna girişten stage sonuna kadar** deneyimi tanımlamak
- Unity tarafında hangi ekranların gerçekten gerekli olduğunu netleştirmek
- placeholder bile olsa hiçbir kritik ekranın boş bırakılmamasını sağlamak
- kod ve UI tasarımının aynı sırayla ilerlemesini sağlamak

### Kural
Bu belge:
- “final polish dokümanı” değildir
- önce **işlevsel ve okunur** ekran akışını kurar
- sonra estetik polish gelir
- testten önce kanonik kabul edilir
- testten sonra bilinçli olarak revize edilebilir

---

## 1. Ana ilke

Top End War’da UI:
- oyunun üstüne sonradan yapıştırılmış panel sistemi gibi görünmemeli
- oyuncuya sadece bilgi vermemeli, **oyun akışını taşımalı**
- combat, seçim ve ödül hissini netleştirmeli
- oyuncunun bir sonraki kararı almasını kolaylaştırmalı

### World 1 için UI görevi
UI şunları açıkça taşımalı:
- şu an neredeyim?
- ne oynuyorum?
- ne seçebilirim?
- neden kazandım / kaybettim?
- sonraki doğru adım ne?

---

## 2. Full screen flow

World 1 deneyiminin kanonik ekran akışı:

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

Bu halkalardan hiçbiri “şimdilik boş kalsın” diye atlanmamalı.
Placeholder olabilir, ama boş olamaz.

3. Screen listesi
3.1 Splash / Boot
Amaç

Oyuncuyu teknik olarak güvenli şekilde oyuna sokmak.

Gösterilecekler
logo / oyun adı
kısa yükleme göstergesi
ilk açılış kontrolü
Yapılmayacaklar
uzun intro
hikâye duvarı
3 farklı geçiş ekranı
Kural

Bu ekran hızlı olmalı.
World 1 deneyiminin odağı burada değil.

3.2 Main Menu
Amaç

Oyuncuya oyuna girişteki ana kararları vermek.

Zorunlu öğeler
Play / Continue
World giriş düğmesi
basit inventory/loadout erişimi
settings
opsiyonel profile/ID alanı
İlk açılışta
oyuncu fazla seçenek görmemeli
“başla” kararı hızlı olmalı
ilk stage’e erişim kolay olmalı
World 1 için kural

Ana menü sade olmalı.
Meta-hub karmaşık şehir/hub hissi şimdilik zorunlu değil.

3.3 World Map
Amaç

Oyuncuya ilerleme hissi ve hedef hissi vermek.

Gösterilecekler
World 1 adı / görsel kimliği
açılmış stage düğümleri
sıradaki stage vurgusu
mini-boss / boss node ayrışması
stage’lerin tamamlandı / kilitli durumu
Opsiyonel ama faydalı
stage bandı etiketi
Tutorial Core
Build Discovery
First Friction
Final Prep
ilk bakışta ne kadar ilerlediğini gösteren küçük progress hissi
Yapılmayacaklar
fazla ikon kalabalığı
her stage üstünde çok uzun bilgi
haritayı bilgi paneline boğmak
Kural

World map:

sadece seçim ekranı değil
ilerleme duygusu ekranı olmalı
3.4 Stage Card / Stage Info
Amaç

Oyuncuya stage’e girmeden önce kısa ama anlamlı bilgi vermek.

Gösterilecekler
stage numarası
kısa stage adı
primary threat tag’leri
önerilen problem tipi
SWARM
ARMOR
ELITE
MIXED
BOSS PREP
reward özeti
varsa first clear ödülü
“Start” butonu
Gösterilmeyecekler
çok sayısal zorluk formülleri
açık çözüm cümleleri
“Sniper seç”
“Breacher al”
uzun açıklama paragrafı
Kural

Oyuncu burada “bu stage ne soruyor?” sorusunun kısa cevabını almalı.

3.5 Loadout Screen
Amaç

Oyuncuya stage öncesi hazırlık kararı verdirmek.

Zorunlu öğeler
commander ana silahı
ekipman slotları
soldier support özeti
basit build etiketi
Start Run butonu
Build summary örnekleri
Balanced
Swarm Clear
Armor Break
Elite Hunt
Boss Prep
Kural

Bu ekran oyuncuya:

“ne taktım?”
“neyi güçlendirdim?”
“bu stage için mantıklı mıyım?”
sorusunu düşündürmeli.
Yapılmayacaklar
gereksiz gear-score duvarı
oyuncuyu boğan 12 istatistik paneli
çok fazla rarity parıltısı
World 1 için ideal
1 ana silah
birkaç görünür modifier
basit squad özeti
net Start butonu
3.6 Run Intro
Amaç

Stage’e geçişte oyuncuya zihinsel hazırlık vermek.

Gösterilecekler
stage adı / numarası
kısa threat tag
çok kısa geçiş animasyonu
Kural

2–3 saniyeyi aşmamalı.
“Şimdi ne oynuyorum?” hissini güçlendirmeli.

3.7 Runner HUD
Amaç

Combat sırasında sadece en gerekli bilgiyi göstermek.

Öncelik sırası
hayatta kalma bilgisi
yakın karar bilgisi
güçlenme hissi
meta bilgi
Zorunlu HUD öğeleri
commander HP
soldier summary
aktif ya da yaklaşan gate bilgisi
tehlike telegraph’ları
boss geldiğinde boss HP / faz göstergesi
Commander HP
sol üst ya da kolay okunur sabit bir noktada
dramatik ama sade
Soldier Summary
tam ordu paneli değil
küçük özet
örnek:
5 Piyade
2 Mekanik
1 Teknoloji
Gate Presentation
ekranda yaklaşan kapılar fiziksel dünyada görünür
HUD’de gerekirse çok hafif destek okunurluğu olabilir
kapı üstü görünüm esas taşıyıcıdır
Gate görünüm kuralı
Üst: Ana etki
Alt: 2 kısa etiket
Örnek
+12 Zırh Delme
ARMOR • ELITE
Telegraphs
charger dash hazırlığı
elite giriş cue’su
boss line shot / sweep warning
alan tehlikesi
Gösterilmeyecekler
çok fazla floating text
uzun gate açıklamaları
açık taktik tavsiyesi
panel yığını
World 1 HUD ilkesi

HUD:

bilgi yoğun değil
karar yoğun olmalı
3.8 Mini-Boss / Boss Overlay
Amaç

Normal run’dan boss moduna geçişi netleştirmek.

Gösterilecekler
boss adı
boss HP
faz göstergesi
telegraph cue
kısa transition lock görseli
Kural

Boss bar:

ayrı bir oyun hissi yaratmalı
ama başka bir oyuna geçmiş gibi görünmemeli
Yapılmayacaklar
ikinci bir karmaşık resource bar
shield katmanı gibi fazla katmanlı karmaşa
sürekli ekranda yanan büyük uyarılar
3.9 Fail Screen
Amaç

Yenilgiyi net ama hızlı şekilde okutmak.

Gösterilecekler
başarısız oldun
stage adı
kısa neden / summary
korunan ödül varsa küçük not
Retry
World Map’e Dön
Kural

Fail ekranı:

cezalandırıcı duvar gibi değil
hızlı yeniden deneme akışına hizmet etmeli
Yapılmayacaklar
çok uzun istatistik ekranı
oyuncuyu utandıran ağır sunum
gereksiz 4 farklı buton
3.10 Victory Screen
Amaç

Zafer hissini ve stage clear bilgisini vermek.

Gösterilecekler
stage clear
kısa başarı özeti
kazanılan ödüller
yeni açılan içerik varsa kısa vurgusu
Continue
Kural

Victory ekranı:

fail ekranından daha tatmin edici olmalı
ama gereksiz 8 adımlı kutlama akışına dönmemeli
3.11 Reward Summary
Amaç

Run sonunda oyuncunun ne kazandığını netleştirmek.

Gösterilecekler
stage reward
mid-run collected rewards
first clear bonus varsa o
kısa toplam
Gösterilmeyecekler
fazla muhasebe
combat diliyle ilgisiz soyut büyük sayı spam’i
Kural

Reward ekranı:

“bir şey kazandım” hissi vermeli
ama economy spreadsheet gibi görünmemeli
3.12 Upgrade / Progress Screen
Amaç

Oyuncuya oyundan sonra küçük ama anlamlı gelişim kararı verdirmek.

Gösterilecekler
weapon upgrade
basic equipment improvement
world progress hissi
bir sonraki hedef
Kural

Upgrade ekranı:

meta oyunu açmalı
ama asıl oyunun önüne geçmemeli
World 1 için ideal
sade upgrade listesi
1–2 görünür gelişim kolu
hemen world map’e dönülebilmeli
4. Tutorial UI Flow
Stage 1 ilk açılış akışı

Oyuncu ilk kez oyuna girince:

ana menüde boğulmaz
World 1 / Stage 1 kolay erişilir
varsayılan loadout hazır gelir
kısa bir “oyna” akışıyla sahaya iner
Stage 1 sırasında

öğretilecekler:

hareket
auto-shoot
basit gate okuma
Kural

Tek stage içinde en fazla 1 ana yeni fikir.
Uzun metin yok.
Kısa ipucu varsa ilk kez görünür.

5. UI readability rules
5.1 Text economy

Her bilgi:

mümkün olan en kısa biçimde görünmeli
özellikle run içinde uzun cümleye dönüşmemeli
5.2 Color economy

Renk:

bilgi katmanı taşımalı
rastgele görsel süs olmamalı

Örnek:

sustain = yeşil
armor / pen = turuncu / sarı
elite / threat = kırmızı / mor
army = mavi / çelik
tactical / geometry = farklı ama sabit bir ton
5.3 Motion economy

Animasyon:

butonlar ve panelleri modern hissettirebilir
ama her ekranı yavaşlatmamalı
5.4 Empty state kuralı

Placeholder ekran bile:

başlık
ana buton
temel bilgi
içermeli

Boş panel kabul edilmez.

6. Screen-specific implementation notes
Main Menu
mevcut Mainmenuui.cs sadeleştirilebilir
Play akışı World Map’e bağlanmalı
inventory / equipment erişimi burada ya da map’te olabilir
World Map
WorldConfig, StageConfig, StageManager ile bağlanmalı
açılmış stage / sıradaki stage durumu save’den okunmalı
Loadout
Equipmentui.cs
Equipmentloadout.cs
Inventorymanager.cs
üçlüsü burada görev almalı
Runner HUD
GameHUD.cs ana taşıyıcı olmalı
Gatefeedback.cs ve gate world-space sunumu ile çakışmadan çalışmalı
boss geldiğinde BossManager / boss UI çağrısı temiz olmalı
Fail / Victory
GameOverUI.cs yalnızca fail değil, daha genel sonuç akışına evrilebilir
ya tek sonuç ekranı
ya fail ve victory ayrı panel
ama mantık tek akıştan beslenmeli
Reward / Upgrade
economy ve progression tarafı sade bağlanmalı
EconomyManager, Progressionconfig, SaveManager ile ilişki net olmalı
7. Unity hookup checklist
Scene tarafı
Main Menu sahnesi ya da panel akışı hazır olmalı
World Map ekranı placeholder bile olsa bağlı olmalı
Stage start akışı world map’ten runner’a geçebilmeli
UI prefab tarafı
Main Menu panel
World Map panel
Stage Card panel
Loadout panel
Runner HUD
Boss overlay
Result panel
Reward summary panel
Upgrade panel
Data hookup
WorldConfig → world map verisi
StageConfig → stage kartı ve stage akışı
WeaponArchetypeConfig → loadout sunumu
EquipmentData → item slot gösterimi
GateConfig → gate metni ve icon
BossConfig → boss overlay bilgisi
Test checklist
portrait ratio test
gate okunurluğu test
HUD clutter test
fail → retry akışı test
victory → reward → upgrade → map dönüşü test
ilk açılış onboarding akışı test
8. Açık riskler / dikkat notları
Risk 1

World map fazla boş görünürse oyun “içi yapılmamış menü prototipi” gibi hissedebilir.

Çözüm
az ama güçlü görsel milestone
boss node vurgusu
ilerleme çizgisi
Risk 2

Loadout ekranı fazla teknik olursa oyuncu korkar.

Çözüm
karmaşık istatistik yerine kısa build etiketi
ana silah odaklı sunum
gereksiz sayı azaltma
Risk 3

Runner HUD fazla bilgi taşırsa gate okuma bozulur.

Çözüm
gate’in ana taşıyıcısı world-space olsun
HUD destekleyici kalsın
Risk 4

Fail ve reward ekranı kötü bağlanırsa oyuncu neyi kaybettiğini / neyi koruduğunu anlamaz.

Çözüm
kısa ve kesin sözleşme
“Run build reset”
“Collected reward kept”
gibi net dil
Risk 5

Boss overlay çok büyük olursa mobil ekranda alan öldürür.

Çözüm
ince ama belirgin üst bar
sadece faz ve HP
geri kalan tehlikeyi dünya içinde göster
9. Güzel eklenebilecek fikirler
A. Build Snapshot

Loadout ekranında küçük bir kart:

Swarm Clear
Armor Break
Elite Hunt
Boss Prep

Bu çok faydalı olur.
Çünkü oyuncuya gizli çözüm vermeden build’inin karakterini söyler.

B. Stage Threat Tags

Stage kartında 2–3 küçük tag:

SWARM
ARMOR
ELITE
BOSS PREP

Bu, fazla açıklama olmadan yön verir.

C. Mid-run Micro Summary

Runner içinde çok hafif bir “toplanan gate özeti” mini paneli olabilir.
Ama:

her an açık büyük panel olmamalı
çok minimal olmalı
D. Result Recap

Fail/victory ekranında:

silah
önemli gate’ler
soldier özeti
küçük bir recap kartı olabilir

Bu test ve öğrenme için çok değerli.

10. Son karar özeti

Screen Flow / UI Flow sistemi:

oyunun boş hissini kırar
combat dışındaki deneyimi de tasarlar
World 1 boyunca oyuncuya yön ve ilerleme hissi verir
gate, build, reward ve retry akışını görünür kılar
placeholder olsa bile hiçbir kritik ekranı boş bırakmaz
testten sonra revize edilebilir, ama revize edilene kadar kanonik kabul edilir