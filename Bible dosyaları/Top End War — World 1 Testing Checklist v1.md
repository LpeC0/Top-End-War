\# Top End War — World 1 Testing Checklist v1

\_Test what matters, not what is easiest\_



\---



\## 0. Belgenin amacı



Bu belge, World 1 için test sürecinde neyin:

\- doğru çalıştığını

\- iyi hissettirdiğini

\- kötü hissettirdiğini

\- tekrar ayar gerektirdiğini



ayırt etmek için kullanılır.



\### Temel ilke

Her şeyi test etmeye çalışmayacağız.  

Önce en kritik hissi ve akışı test edeceğiz.



\### Kural

Testin amacı:

\- bug yakalamak

\- his kontrolü yapmak

\- yanlış yönü erken görmek

\- tekrar başa dönmeyi azaltmak



\---



\## 1. Test katmanları



World 1 için 5 ana test katmanı vardır:



1\. Runtime Stability

2\. Combat Feel

3\. Readability

4\. Progression \& Reward

5\. Stage/Band Flow



\---



\## 2. Runtime Stability Checklist



\## 2.1 Oyuncu hattı

\- oyuncu sahnede doğru yükseklikte mi

\- koşu akışı durmadan ilerliyor mu

\- anlamsız yere anchor/freeze oluyor mu

\- retry sonrası düzgün resetleniyor mu



\## 2.2 Düşman hattı

\- düşman spawn oluyor mu

\- pooled enemy tekrar kullanıldığında bozuluyor mu

\- temas hasarı sürekli çalışıyor mu

\- ölünce düzgün kapanıyor mu



\## 2.3 Gate hattı

\- gate trigger crash yapıyor mu

\- gate config null kalırsa oyun patlıyor mu

\- gate efekti gerçekten uygulanıyor mu

\- yanlış asset / boş config sahneyi bozuyor mu



\## 2.4 GameOver hattı

\- ölüm bir kez mi tetikleniyor

\- GameOver paneli geliyor mu

\- hareket duruyor mu

\- Retry çalışıyor mu

\- ana menü dönüşü çalışıyor mu



\### Runtime test sonucu

\- \*\*PASS\*\* = oynanış zinciri kırılmadan akıyor

\- \*\*FAIL\*\* = oynanış zincirini bozan bug var



\---



\## 3. Combat Feel Checklist



\## 3.1 Silah hissi

\- Assault güvenli genelci hissi veriyor mu

\- SMG swarm karşısında parlıyor mu

\- Sniper ağır hedefte anlamlı mı

\- buildler kağıt üstünde değil sahada farklı hissediliyor mu



\## 3.2 Vuruş hissi

\- mermiler boşa gidiyor hissi çok yüksek mi

\- hit feedback var mı

\- armor hedefe vurunca fark hissediliyor mu

\- elite/boss vurunca tatmin var mı



\## 3.3 Squad hissi

\- takım gerçekten katkı veriyor mu

\- sadece kalabalık gibi mi duruyor

\- ekrana sığıyor mu

\- build’in parçası gibi hissediliyor mu



\## 3.4 Temas / tehlike hissi

\- oyuncu tamamen rahat mı

\- sürekli kaçmak zorunda mı

\- orta bir gerilim oluşuyor mu



\### Combat feel sonucu

\- \*\*PASS\*\* = savaş akıyor ve his veriyor

\- \*\*WARN\*\* = çalışıyor ama boş/sert/garip his var

\- \*\*FAIL\*\* = mekanik çalışsa da oyun hissi yok



\---



\## 4. Readability Checklist



\## 4.1 Düşman okunurluğu

\- trooper / swarm / charger / brute ayırt ediliyor mu

\- elite ilk bakışta anlaşılır mı

\- boss normal düşmandan net ayrılıyor mu



\## 4.2 Gate okunurluğu

\- 1 saniyede okunuyor mu

\- iki kapı farkı anlaşılıyor mu

\- metin çok uzun mu

\- renk/metin birbirini destekliyor mu



\## 4.3 UI okunurluğu

\- HUD fazla kalabalık mı

\- HP, squad, boss bilgisi okunuyor mu

\- result ekranı sade mi

\- stage card yeterince kısa mı



\## 4.4 Ekran okunurluğu

\- oyuncu ekran dışına taşıyor mu

\- squad çok yayılıp kayboluyor mu

\- tehlike alanları görülebiliyor mu

\- mobil dikey ekranda bilgi kaybı var mı



\### Readability sonucu

\- \*\*PASS\*\* = oyuncu 1–2 saniyede doğru bilgiyi alıyor

\- \*\*WARN\*\* = bilgi var ama okunması zahmetli

\- \*\*FAIL\*\* = oyuncu savaşta ne olduğunu anlamıyor



\---



\## 5. Progression \& Reward Checklist



\## 5.1 Run sonrası his

\- oyuncu “boşa gitmedi” diyor mu

\- küçük de olsa bir kazanım hissi var mı

\- bir sonraki run için motivasyon doğuyor mu



\## 5.2 Reward pacing

\- ödül çok cılız mı

\- ödül aşırı büyük mü

\- kötü run tamamen cezalandırılıyor mu

\- iyi run tatmin ediyor mu



\## 5.3 Upgrade hissi

\- upgrade anlamlı mı

\- sadece küçük sayı artışı mı

\- yeni yön / seçenek açıyor mu

\- oyuncu neyi niye upgrade ettiğini anlıyor mu



\## 5.4 Unlock pacing

\- yeni weapon direction doğru zamanda mı açılıyor

\- support unlock’lar doğal mı

\- Beam çok erken/geç mi geliyor

\- oyuncu yeni içerikle boğuluyor mu



\### Progression sonucu

\- \*\*PASS\*\* = oyuncu tekrar oynamak istiyor

\- \*\*WARN\*\* = sistem var ama motivasyon zayıf

\- \*\*FAIL\*\* = ödül/upgrade anlamsız



\---



\## 6. Stage / Band Flow Checklist



\## 6.1 Stage akışı

\- stage çok boş mu

\- stage çok sert mi

\- tempo düz çizgi mi

\- relief hissediliyor mu



\## 6.2 Band geçişi

\- 1–5 gerçekten öğretici mi

\- 6–10 solve öğretimi yapıyor mu

\- 11–20 build farkını açıyor mu

\- 21–30 specialization hissi veriyor mu

\- 31–34 final prep gibi hissettiriyor mu



\## 6.3 Solve focus testi

\- stage’in istediği solve gerçekten sahada hissediliyor mu

\- oyuncu yanlış build ile zorlandığını anlayabiliyor mu

\- ama duvara toslamıyor mu



\### Stage flow sonucu

\- \*\*PASS\*\* = World 1 bandları ayrı hissediliyor

\- \*\*WARN\*\* = stage’ler çalışıyor ama birbirine benziyor

\- \*\*FAIL\*\* = World 1’in öğretim eğrisi görünmüyor



\---



\## 7. Boss Checklist



\## 7.1 Mini-boss

\- sıradan wave gibi mi duruyor

\- telegraph okunuyor mu

\- build farkı boss’ta hissediliyor mu

\- çok mu kolay / çok mu duvar



\## 7.2 Final boss

\- World 1 sentezi gibi mi

\- Beam iyi ama zorunlu değil mi

\- phase farkı hissediliyor mu

\- punish window anlaşılıyor mu



\### Boss sonucu

\- \*\*PASS\*\* = boss sınav gibi hissettiriyor

\- \*\*WARN\*\* = boss çalışıyor ama ruhu yok

\- \*\*FAIL\*\* = boss unfair / sıkıcı / anlamsız



\---



\## 8. Test türleri



\## 8.1 Smoke Test

Amaç:

\- oyun açılıyor mu

\- koşu başlıyor mu

\- hasar, ölüm, retry çalışıyor mu



\## 8.2 Feel Test

Amaç:

\- eğlenceli mi

\- build farkı hissediliyor mu

\- düşmanlar boş mu

\- gate seçimleri hoş mu



\## 8.3 Balance Test

Amaç:

\- stage boş mu

\- çok mu sert

\- solve focus doğru mu

\- relief oranı iyi mi



\## 8.4 Progression Test

Amaç:

\- 30–60 dakikada oyuncu ne hissediyor

\- ödül/upgrade döngüsü motive ediyor mu



\---



\## 9. Pratik test oturumu formatı



Her test oturumunda sadece şu kısa not tutulmalı:



\### A. Runtime

\- bug var mı

\- zincir kırıldı mı



\### B. Feel

\- ne iyi hissettirdi

\- ne boş hissettirdi

\- ne fazla sert geldi



\### C. Readability

\- neyi göremedim

\- neyi okuyamadım



\### D. Progression

\- ne kazandım

\- neden tekrar oynamak isterim / istemem



\### E. Öncelik

\- şimdi düzelt

\- sonra düzelt

\- sadece not al



\---



\## 10. “Şimdi düzelt / sonra düzelt” ayrımı



\## Şimdi düzelt

\- runtime kırıkları

\- ölüm / retry bozukluğu

\- hasar çalışmaması

\- gate okunmaması

\- düşman ayırt edilememe

\- oyuncunun ekrandan taşması



\## Sonra düzelt

\- buton hizası

\- font polish

\- renk tonu ince ayarı

\- ekstra efektler

\- daha güzel transition’lar



\### Kural

Önce çalışan ve okunur oyun, sonra güzellik.



\---



\## 11. Kısa test puanlama sistemi



İstersen her test sonrası 10 üzerinden puan ver:



\- Runtime

\- Combat Feel

\- Readability

\- Progression

\- Boss

\- Genel Eğlence



\### Yorum

\- `8+` = iyi

\- `6–7` = çalışıyor ama tuning lazım

\- `<6` = ciddi müdahale lazım



Bu çok işe yarar.



\---



\## 12. Son karar özeti



World 1 Testing Checklist v1:

\- neyi test edeceğimizi netleştirir

\- bug ile his sorununu ayırır

\- stage, build, reward ve boss hissini ayrı ayrı değerlendirmemizi sağlar

\- tekrar başa dönmeden kontrollü ilerlemeyi kolaylaştırır

\- önce neyi düzeltmemiz gerektiğini netleştirir

