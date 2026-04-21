\# Top End War — Boss Design Contract v1

\_World 1 Mini-Boss ve Final Boss tasarım sözleşmesi\_



\---



\## 0. Belgenin amacı



Bu belge, World 1 boyunca kullanılacak boss tasarım dilini tanımlar.



Şunları kilitler:

\- boss neyi sınar

\- boss neyi sınamaz

\- mini-boss ile final boss farkı ne

\- phase yapısı nasıl olmalı

\- telegraph / punish / recovery dengesi nasıl kurulmalı

\- build’ler boss karşısında nasıl değer kazanmalı



\### Temel ilke

Boss:

\- yeni oyun öğretmez

\- oyuncunun daha önce öğrendiğini sınar

\- et duvarı olmaz

\- hileli hissettirmez

\- build farkını görünür kılar

\- ama tek doğru build istemez



\---



\## 1. Boss felsefesi



\## 1.1 Boss nedir?

Boss, normal düşmanların daha büyük HP’li hali değildir.



Boss:

\- bir sınavdır

\- oyuncuya tempo değişimi yaşatır

\- build kalitesini görünür kılar

\- threat okuma ve karar verme ister



\---



\## 1.2 Boss ne istememeli?

Boss:

\- saf attrition duvarı olmamalı

\- sadece 10 kat HP ile uzatılmış dövüş olmamalı

\- görünmez unfair hasar vermemeli

\- oyuncudan öğrenmediği bir çözümü istememeli



\---



\## 1.3 Boss ne istemeli?

Boss şu 5 şeyi test etmeli:



1\. doğru threat okuma  

2\. doğru build hazırlığı  

3\. uzun dövüş sabrı  

4\. telegraph tanıma  

5\. solve farkındalığı  



\---



\## 2. World 1 boss yapısı



World 1’de boss sistemi iki katmanlıdır:



\### A. Mini-Boss

\- ilk ciddi sınav

\- daha küçük kapsamlı test

\- tek ana problemi öne çıkarır



\### B. Final Boss

\- World 1 boyunca öğretilenlerin sentezi

\- mixed solve ister

\- build kalitesini ve hazırlığı daha net ortaya çıkarır



\---



\## 3. Mini-Boss tasarım sözleşmesi



\## 3.1 Mini-Boss’un görevi

Mini-boss:

\- oyuncuya “artık normal wave’den farklı bir şey oynuyorsun” hissi vermeli

\- ama çok karmaşık olmamalı

\- 1 ana konu, 1 yan konu test etmeli



\### Mini-boss test örnekleri

\- armor check + telegraph

\- priority target + movement

\- sustained damage + punish window



\---



\## 3.2 Mini-Boss’un sorması gereken soru

Mini-boss oyuncuya şunu sordurmalı:



> “Bu build ile bu problemi çözebiliyor muyum?”



Ama şu hissi vermemeli:

> “Doğru silahı getirmediysem imkânsız.”



\---



\## 3.3 Mini-Boss kuralları

\- 2 fazı geçmemeli

\- saldırı seti okunur olmalı

\- her tehlikeli hareketin telegraph’ı net olmalı

\- punish window kısa ama anlaşılır olmalı

\- yanlış build ile daha uzun sürmeli ama imkânsız olmamalı



\---



\## 4. Final Boss tasarım sözleşmesi



\## 4.1 Final Boss’un görevi

Final boss:

\- World 1’in ana sentezi olmalı

\- oyuncuya “öğrendiklerin şimdi lazım” hissi vermeli

\- solve focus’ları birleştirmeli

\- ama bütün mekanikleri aynı anda oyuncunun üstüne yığmamalı



\---



\## 4.2 Final Boss neyi sınamalı?

Final boss şu alanlarda sınav olmalı:



\- mixed pressure okuma

\- elite / armor farkındalığı

\- long-fight sabrı

\- sustain ihtiyacı

\- boss prep değerinin farkı

\- build snapshot’ın gerçekten ne işe yaradığını hissettirme



\---



\## 4.3 Final Boss neyi istememeli?

\- tek bir zorunlu silah

\- pixel-perfect kaçış

\- ekranı anlamsız alanlarla doldurma

\- sürekli saldırı spamı

\- oyuncuyu sırf uzun sürsün diye oyalama



\---



\## 5. Boss phase mantığı



\## 5.1 Genel phase kuralı

Her boss için:

\- net giriş

\- phase 1

\- transition

\- phase 2

olmalı



İstersen final boss’ta küçük bir phase 3 hissi olabilir, ama bunu ayrı phase yerine phase 2 yoğunlaşması gibi düşünmek daha iyi.



\---



\## 5.2 Phase 1

\### Görevi

\- boss’un dilini öğretmek

\- ana saldırıları tanıtmak

\- oyuncuya ritmi okutmak



\### Kural

Phase 1:

\- çok sert başlamamalı

\- öğretici olmalı

\- ama sıkıcı da olmamalı



\---



\## 5.3 Transition

\### Görevi

\- “şimdi ikinci yarıya geçiyoruz” hissi vermek

\- oyuncuyu zihinsel olarak hazırlamak



\### Kural

Transition:

\- çok uzun cutscene gibi olmamalı

\- kısa, net, okunur olmalı



\---



\## 5.4 Phase 2

\### Görevi

\- phase 1’de tanıtılan şeyleri daha yoğun şekilde test etmek

\- yeni sistem değil, yeni baskı vermek



\### Kural

Phase 2:

\- tamamen başka oyun gibi olmamalı

\- phase 1 bilgisini büyütmeli



\---



\## 6. Telegraph Contract



\## 6.1 Telegraph nedir?

Boss’un tehlikeli hareketten önce verdiği açık sinyal.



\### Telegraph türleri

\- animasyon hazırlığı

\- ışık / glow

\- alan çizgisi

\- ses cue

\- kısa duraklama



\---



\## 6.2 Telegraph kuralları

\- her ağır saldırının telegraph’ı olmalı

\- telegraph çok kısa ama adil olmalı

\- erken oyunda daha okunur, final boss’ta biraz daha sıkı olabilir

\- aynı saldırının telegraph’ı tutarlı olmalı



\---



\## 6.3 Yasak

\- telegraf’sız yüksek hasar

\- ekranda fark edilmeyen alan tehdidi

\- saldırı ile telegraph arasında tutarsız zamanlama



\---



\## 7. Punish Window Contract



\## 7.1 Punish Window nedir?

Oyuncunun boss’un açığını kullanabildiği kısa dönem.



\### Neden gerekli?

Punish window yoksa boss:

\- sadece kovalayan duvar olur

\- build farkı görünmez

\- dövüş sıkıcı uzar



\---



\## 7.2 Punish Window kuralları

\- her boss pattern döngüsünde en az 1 net punish window olmalı

\- sniper / beam / assault gibi buildler bunu farklı şekillerde değerlendirebilmeli

\- window çok kısa olabilir ama görünür olmalı



\---



\## 8. Build ilişkisi



\## 8.1 Boss ve build

Boss, build’leri ayırmalı ama tek cevaba zorlamamalı.



\### Örnek

\- Assault → güvenli, genelci boss çözümü

\- Sniper → yüksek değerli açılarda çok ödüllendirici

\- Beam → long-fight ve boss prep sinerjisinde çok iyi

\- Shotgun → riskli ama bazı yakın punish window’larda güçlü

\- Launcher → doğrudan en iyi boss çözümü olmak zorunda değil ama destek rolü olabilir



\---



\## 8.2 Beam kuralı

Beam, World 1 final bossundan önce tanıtılır ve:

\- final boss’a karşı neden iyi olduğu gösterilir

\- ama final boss “beam check” olmaz



Yani:

\- Beam güçlü seçenek

\- ama zorunlu seçenek değil



\---



\## 8.3 Solve ilişkisi

Boss şu solve türlerinden 2–3 tanesini anlamlı kullanabilir:

\- armor

\- elite

\- long\_fight

\- priority



Ama hepsini aynı anda maksimum yoğunlukta kullanmamalı.



\---



\## 9. Mini-Boss önerisi — Gatekeeper Walker



\## 9.1 Rol

İlk gerçek build sınavı.



\## 9.2 Ana test

\- armor farkındalığı

\- telegraph okuma

\- greed DPS cezalandırma



\## 9.3 Faz yapısı

\### Faz 1

\- Line Shot

\- Front Sweep

\- yavaş baskı



\### Geçiş

\- kısa güçlenme / duraklama



\### Faz 2

\- aynı pattern’lerin daha hızlı ve daha sert hali

\- kısa charge tehdidi



\## 9.4 Oyuncuya hissettirmesi gereken

\- “bu artık sıradan dalga değil”

\- “doğru vuramıyorsam dövüş uzuyor”

\- “telegraph okuyunca rahatlıyorum”



\---



\## 10. Orta boss önerisi — War Machine



\## 10.1 Rol

Alan baskısı ve sabır sınavı.



\## 10.2 Ana test

\- movement alanı okuma

\- line / zone tehlikesi

\- sustained damage window kullanımı



\## 10.3 Faz yapısı

\### Faz 1

\- zone telegraph

\- ağır line attack

\- yavaş ama baskın tempo



\### Faz 2

\- alanlar biraz daha sık

\- punish pencereleri biraz daha değerli



\## 10.4 Oyuncuya hissettirmesi gereken

\- “burada sadece DPS yetmiyor”

\- “alanı okuyup pencereyi değerlendirmeliyim”



\---



\## 11. Final Boss önerisi — World 1 Final Boss



\## 11.1 Rol

World 1 sentezi.



\## 11.2 Ana test

\- mixed pressure

\- long fight

\- elite/armor farkındalığı

\- boss prep değerini hissettirme



\## 11.3 Faz yapısı

\### Faz 1

\- açık ve okunur pattern’ler

\- boss’un dilini tanıtma

\- armor / line / short pressure karışımı



\### Transition

\- kısa phase shift

\- “şimdi işler ciddileşiyor” hissi



\### Faz 2

\- aynı pattern’lerin daha kombinasyonlu hali

\- daha sık punish / daha sert punish

\- boss prep build’leri burada fark yaratır



\## 11.4 Oyuncuya hissettirmesi gereken

\- “hazırlığımın değeri var”

\- “beam iyi ama tek yol değil”

\- “öğrendiklerimi kullanıyorum”



\---



\## 12. Boss Reward Contract



\## 12.1 Mini-Boss reward

\- hissedilir ama aşırı büyük olmayan milestone

\- currency spike

\- equipment şansı

\- support / build yönü hissi



\## 12.2 Final Boss reward

\- büyük milestone

\- yeni world / yeni ana unlock

\- belirgin tatmin hissi



\### Kural

Boss ödülü:

\- yalnızca daha çok para değil

\- “eşik geçtim” hissi vermeli



\---



\## 13. Boss arena / pacing kuralları



\### Kural 1

Arena okunur olmalı.  

Gereksiz clutter olmamalı.



\### Kural 2

Boss dövüşü wave gibi hissetmemeli.  

Ama oyunun geri kalanından da kopmamalı.



\### Kural 3

Arena oyuncuyu gereksiz sıkıştırmamalı.  

Tehlike, okuma ve karar üzerinden gelsin.



\### Kural 4

Boss dövüşleri çok uzun olmamalı.

\- mini-boss: kısa-orta

\- final boss: orta



\---



\## 14. Görünür bilgi kuralları



Boss sırasında oyuncuya şu bilgiler net verilmeli:

\- boss HP

\- phase hissi

\- ağır saldırı telegraph’ı

\- vurulabilir pencere hissi



\### Gösterilmeyecek

\- karmaşık stat ekranı

\- çok fazla UI katmanı

\- boss’un tüm matematiği



\---



\## 15. Riskler



\### Risk 1

Boss sadece HP duvarına döner.



\### Risk 2

Boss çok fazla yeni şey öğretmeye çalışır.



\### Risk 3

Telegraph zayıf kalırsa unfair hissi doğar.



\### Risk 4

Tek doğru build oluşursa replay değeri düşer.



\### Risk 5

Punish window zayıfsa dövüş sıkıcı uzar.



\---



\## 16. Son karar özeti



Boss Design Contract v1:

\- boss’ları HP duvarı olmaktan çıkarır

\- mini-boss ve final boss rolünü ayırır

\- phase, telegraph ve punish pencerelerini standardize eder

\- build farkını görünür kılar

\- ama tek doğru çözüm yaratmaz

\- World 1’in final hissini taşıyan tasarım omurgasını kurar

