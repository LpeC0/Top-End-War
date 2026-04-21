\# Top End War — Build Snapshot \& Threat Tag System v1

\_Show simple, hide complexity\_



\---



\## 0. Belgenin amacı



Bu belge, World 1 boyunca oyuncuya:



\- build’inin neye iyi olduğunu

\- stage’in ne istediğini

\- gate seçiminin neden değerli olduğunu



\*\*çok kısa, sade ve okunur\*\* şekilde göstermeyi tanımlar.



\### Temel ilke

Arkada sistem karmaşık olabilir.  

Ama oyuncuya görünen yüz:

\- kısa

\- temiz

\- yön verici

\- bunaltmayan

olmalıdır.



\### Kural

Oyuncu:

> “Öf bunun datasını mı okuyacağım?”

dememeli.



Oyuncu sadece şunu hissetmeli:

\- benim build’im şu işe iyi

\- bu stage şu tehdidi soruyor

\- şu gate niye hoşuma gitti anlıyorum



\---



\## 1. Sistem neden gerekli?



Şu an tasarımda şunlar var:

\- solve focus

\- packet library

\- stage band

\- weapon role

\- gate economy

\- support layer



Ama bunların hepsi oyuncuya aynen gösterilirse oyun yorucu olur.



\### Bu sistemin görevi

Bütün derin sistemi oyuncuya şu 3 sade yüz üzerinden göstermek:



1\. \*\*Build Snapshot\*\*

2\. \*\*Threat Tags\*\*

3\. \*\*Gate Short Tags\*\*



\---



\## 2. Görünür katmanlar



\## 2.1 Build Snapshot

Oyuncunun mevcut build’inin kısa kimliği.



Örnek:

\- Balanced

\- Swarm Clear

\- Armor Break

\- Elite Hunt

\- Boss Prep

\- Close Burst

\- Area Control

\- High Tempo

\- Long Fight



Bu, loadout ekranında ve stage öncesi görünür.



\---



\## 2.2 Threat Tags

Stage’in oyuncudan ne istediğini kısa anlatan etiketler.



Örnek:

\- SWARM

\- ARMOR

\- ELITE

\- MIXED

\- PRIORITY

\- LONG FIGHT

\- BOSS PREP



Bu, stage kartında görünür.



\---



\## 2.3 Gate Short Tags

Kapının ne verdiğini iki kısa etiketle anlatır.



Örnek:

\- ARMOR • ELITE

\- POWER • SAFE

\- ARMY • HEAL

\- TEMPO • DPS

\- BOSS • BEAM



Bu, run içinde kapı üstünde görünür.



\---



\## 3. Build Snapshot sistemi



\## 3.1 Build Snapshot nedir?

Build snapshot, oyuncunun elindeki kurulumun gizli matematiğini özetleyen kısa etikettir.



Oyuncu bunu şöyle okur:

\- “Ben şu an ne oynuyorum?”

\- “Benim build’im neye yatkın?”

\- “Bu stage’e ne kadar uyuyorum?”



\---



\## 3.2 Snapshot kuralı

Her build’e uzun açıklama verilmeyecek.  

En fazla:

\- 1 ana snapshot

\- opsiyonel 1 alt snapshot



\### Örnek

Ana:

\- `Armor Break`



Alt:

\- `Long Fight`



\---



\## 3.3 Snapshot kaynakları

Snapshot şu katmanlardan hesaplanır:



\- commander weapon family

\- equipment modifier yönü

\- support bias

\- gate etkileri

\- varsa solve bonusları



Ama oyuncuya bu hesap gösterilmez.



\---



\## 3.4 Snapshot listesi v1



\### Balanced

Kullanım:

\- genel güvenli build

\- assault ağırlıklı

\- net bir solve’a aşırı yatmamış



\### Swarm Clear

Kullanım:

\- SMG

\- dense packet temizliği

\- tempo / area desteği



\### Armor Break

Kullanım:

\- sniper

\- breacher

\- armor pen

\- brute çözümü



\### Elite Hunt

Kullanım:

\- elite damage

\- sniper / beam

\- priority target odaklı



\### Close Burst

Kullanım:

\- shotgun

\- mekanik support

\- yakın baskı çözümü



\### Area Control

Kullanım:

\- launcher

\- geometry

\- swarm punish



\### High Tempo

Kullanım:

\- fire rate

\- fast cycle

\- chain hissi



\### Long Fight

Kullanım:

\- beam

\- sustain

\- boss prep

\- uzun temas isteyen yapı



\### Boss Prep

Kullanım:

\- final prep

\- beam/sniper solve

\- elite/armor ağırlıklı stage hazırlığı



\---



\## 3.5 Snapshot gösterim alanları



\### Loadout Screen

Mutlaka görünür.



Format:

```text

Build Snapshot

Armor Break



veya



Build Snapshot

Balanced

Sub: High Tempo

Result Screen



Kısa recap’te gösterilebilir.



Stage Start öncesi



İstersen küçük bilgi olarak görünebilir ama zorunlu değil.



4\. Threat Tag sistemi

4.1 Threat Tag nedir?



Stage’in oyuncuya sorduğu soruyu tek kelimelik kısa etiketlerle gösterir.



Oyuncu bunu şöyle okur:



“Bu bölüm ne istiyor?”

“Neye hazırlıklı olmalıyım?”

“Benim build’im buna uygun mu?”

4.2 Threat Tag listesi v1

SWARM



Kalabalık küçük baskı



ARMOR



Zırhlı hedef / brute çözümü



ELITE



Yüksek öncelikli hedef baskısı



PRIORITY



Geç kalırsan ceza yersin hissi



MIXED



Genelci / çok yönlü çözüm ister



LONG FIGHT



Uzun temas / boss tipi dayanım



BOSS PREP



Yaklaşan büyük sınava hazırlık



CLOSE PRESSURE



Yakın baskı / charger / yakın alan yoğunluğu



LANE PRESSURE



Sağ-sol/dağılım baskısı



4.3 Threat Tag kuralları

Kural 1



Bir stage’de en fazla 2 ana tag, en fazla 3 tag gösterilir.



Kural 2



Tag’ler oyuncuya çözümü doğrudan söylemez.

Sadece sorunu anlatır.



Yanlış örnek:



Use Sniper



Doğru örnek:



ARMOR

Kural 3



Aynı band içinde tag’ler değişebilir ama ana solve focus’la uyumlu olmalı.



4.4 Stage Card formatı



Örnek:



Stage 08

Threats: ARMOR • PRIORITY

Reward: Standard



veya



Stage 32

Threats: ELITE • BOSS PREP

Reward: Prep

5\. Build Snapshot + Threat Tag ilişkisi



Bu ikisi birlikte çalışır.



Oyuncu şunu görür:



Stage:



ARMOR • ELITE



Build:



Armor Break



Ve içinden şunu hisseder:



“Tamam, bu bölüm benim build’ime uygun.”



veya



Stage:



SWARM • LANE PRESSURE



Build:



Elite Hunt



Oyuncu içinden şunu hisseder:



“Burada çok rahat olmayabilirim.”



Ama sistem açıkça bunu yazmaz.



Bu çok önemli.



6\. Gate Short Tag sistemi

6.1 Gate dili



Kapının üst satırı:



doğrudan etki



Alt satırı:



2 kısa tag

Örnek

+12 Armor Pen

ARMOR • ELITE

Örnek

+2 Infantry

ARMY • SAFE

6.2 Gate tag havuzu v1

POWER



genel güç



SAFE



güvenli genel seçim



TEMPO



akış / hız



DPS



saf saldırı katkısı



ARMOR



armor solve



ELITE



elite solve



ARMY



support/squad büyümesi



HEAL



iyileşme / sürdürülebilirlik



AREA



alan etkisi



PACK



küme cezalandırma



CLOSE



yakın baskı ilişkisi



BURST



ani yüksek verim



LINE



çizgisel verim



CONTROL



geometry / spread / yön verimi



BOSS



boss odaklı



BEAM



beam sinerjisi



LONG



uzun dövüş



SUPPORT



arka sıra / yardımcı çözüm



SURVIVE



hayatta kalma



CONTACT



temas cezasına dayanma



MARK



işaretleme / odak yardımı



EXECUTE



bitirici



THREAT



tehdit okuma



6.3 Gate tag kuralı



Mümkün olduğunca ortak shared tag havuzu kullanılır.

Her gate için yeni kısa tag türetilmez.



Bu hem:



localization’ı kolaylaştırır

hem de oyuncuya tanıdık bir dil kurar

7\. UI yerleşimleri

7.1 Stage Card



Göster:



Stage Name

Threat Tags

Reward Band

opsiyonel kısa build uyumu hissi

Örnek

Threats: ARMOR • ELITE

7.2 Loadout Screen



Göster:



commander weapon

support summary

build snapshot

Örnek

Build Snapshot

High Tempo

Sub: Swarm Clear

7.3 Gate üstü world-space text



Göster:



effect title

2 kısa tag

Örnek

+1 Pierce

LINE • ARMOR

7.4 Result Screen



Gösterilebilir:



run sonunda kısa build recap

stage threat recap

kazanç özeti



Ama uzun analiz ekranı yapılmaz.



8\. Sistem nasıl hesaplanır?



Oyuncuya görünmez.



8.1 Build Snapshot hesaplama



Arka planda puan mantığıyla çalışır.



Örnek:



SMG + tempo gate + area modifier = Swarm Clear, High Tempo

Sniper + armor pen + elite bonus = Armor Break, Elite Hunt

Beam + sustain + long-fight gate = Boss Prep, Long Fight

Kural



En yüksek puanı alan snapshot ana etiket olur.



8.2 Threat Tag hesaplama



Stage recipe’den gelir.



Örnek:



solveFocus = armor

secondaryThreat = elite



→ göster:

ARMOR • ELITE



8.3 Gate Tag hesaplama



Gate config’ten sabit gelir.



Örnek:



Breacher

→ ARMOR • ELITE

9\. Oyuncu hissi hedefi



Bu sistemin sonunda oyuncu şunu hissetmeli:



oyun beni boğmuyor

ama aptal yerine de koymuyor

bölüm ne istiyor anlıyorum

build’im neye iyi anlıyorum

kapı neden hoşuma gitti anlıyorum

detay istersem daha derine bakabilirim

ama bakmasam da akış bozulmuyor

10\. Riskler

Risk 1



Çok fazla tag gösterirsek sistem sıkıcı olur.



Risk 2



Build snapshot çok sık değişirse oyuncu güven kaybeder.



Risk 3



Threat tag çözümü direkt söylerse keşif hissi ölür.



Risk 4



Gate tag’leri çok soyut olursa oyuncu anlam veremez.



11\. Güvenli kullanım kuralları

Kural 1



Stage Card: max 3 tag



Kural 2



Loadout: 1 ana snapshot, opsiyonel 1 alt snapshot



Kural 3



Gate: 2 kısa tag



Kural 4



Result ekranı: kısa recap, uzun analiz değil



12\. Son karar özeti



Build Snapshot \& Threat Tag System v1:



karmaşık sistemi oyuncuya sade yüzle gösterir

build, stage ve gate bilgisini okunur hale getirir

oyuncuyu yormadan yön verir

localization için kısa ve tutarlı dil üretir

World 1’in görünür UX omurgasını kurar

