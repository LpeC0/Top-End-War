# Top End War — Localization Key Map v1
_EN / TR için temel key haritası_

---

## 0. Belgenin amacı

Bu belge, Top End War’da görünen tüm kritik metinler için
**tek bir localization key şeması** tanımlar.

Amaç:
- hardcoded string kullanımını en baştan engellemek
- English + Türkçe başlangıç desteğini güvenli kurmak
- SO ve UI tarafının aynı key dilini konuşmasını sağlamak
- sonradan “şu text nereden geliyor?” kaosunu önlemek

### Kural
Kod içinde görünen metin yazılmayacak.  
Mümkün olan her yerde **key** kullanılacak.

---

## 1. Genel key standardı

### Format
```text
kategori.altkategori.oge
Örnek
ui.main.play
world.1.name
stage.1.name
weapon.assault.name
enemy.trooper.name
gate.hardline.title
tag.armor
build.boss_prep
Kurallar
küçük harf
boşluk yok
Türkçe karakter yok
nokta ile ayrım
aynı anlam için farklı key açılmayacak
2. Dil kodları

İlk aktif diller:

en
tr
Kural

English fallback dilidir.
Bir key Türkçe’de eksikse English gösterilebilir.

3. UI key’leri
3.1 Main Menu
ui.main.play
ui.main.continue
ui.main.settings
ui.main.loadout
ui.main.inventory
ui.main.exit
3.2 World Map
ui.map.title
ui.map.world_select
ui.map.stage_locked
ui.map.stage_unlocked
ui.map.stage_cleared
ui.map.first_clear
ui.map.boss_node
ui.map.miniboss_node
3.3 Stage Card
ui.stage.start
ui.stage.back
ui.stage.rewards
ui.stage.first_clear_reward
ui.stage.primary_threats
ui.stage.recommended_build
ui.stage.boss_prep
3.4 Loadout
ui.loadout.title
ui.loadout.start_run
ui.loadout.commander
ui.loadout.squad
ui.loadout.weapon
ui.loadout.equipment
ui.loadout.build_snapshot
ui.loadout.swap
3.5 Runner HUD
ui.hud.hp
ui.hud.squad
ui.hud.boss
ui.hud.phase
ui.hud.warning
ui.hud.gate_choice
3.6 Result / Reward / Upgrade
ui.result.victory
ui.result.fail
ui.result.retry
ui.result.return_map
ui.result.summary
ui.reward.title
ui.reward.collected
ui.reward.first_clear
ui.reward.continue
ui.upgrade.title
ui.upgrade.upgrade
ui.upgrade.skip
ui.upgrade.next
3.7 Settings
ui.settings.title
ui.settings.language
ui.settings.audio
ui.settings.music
ui.settings.sfx
ui.settings.back
4. World key’leri
world.1.name
world.1.desc
World 1 band isimleri
world.1.band.tutorial_core
world.1.band.build_discovery
world.1.band.first_friction
world.1.band.controlled_complexity
world.1.band.specialization
world.1.band.pressure_punishment
world.1.band.final_prep
world.1.band.final_boss
5. Stage key’leri
Stage adı
stage.1.name
stage.2.name
stage.3.name
...
stage.35.name
Stage kısa açıklama / flavor

İstersen sonra aç:

stage.1.desc
stage.2.desc
...
Stage info kısa etiketleri
stage.info.primary_threats
stage.info.rewards
stage.info.first_clear
stage.info.start
6. Weapon key’leri
6.1 Silah aileleri
weapon.assault.name
weapon.assault.desc

weapon.smg.name
weapon.smg.desc

weapon.sniper.name
weapon.sniper.desc

weapon.shotgun.name
weapon.shotgun.desc

weapon.launcher.name
weapon.launcher.desc

weapon.beam.name
weapon.beam.desc
6.2 Weapon role kısa etiketleri
weapon.role.balanced
weapon.role.swarm_clear
weapon.role.armor_break
weapon.role.elite_hunt
weapon.role.close_burst
weapon.role.area_control
weapon.role.boss_pressure
7. Enemy key’leri
enemy.trooper.name
enemy.trooper.desc

enemy.swarm.name
enemy.swarm.desc

enemy.charger.name
enemy.charger.desc

enemy.armored_brute.name
enemy.armored_brute.desc

enemy.elite_charger.name
enemy.elite_charger.desc

enemy.gatekeeper_walker.name
enemy.gatekeeper_walker.desc

enemy.war_machine.name
enemy.war_machine.desc

enemy.world1_final_boss.name
enemy.world1_final_boss.desc
8. Gate key’leri

Her gate için:

title
tag1
tag2
kısa açıklama gerekirse desc
8.1 Örnek set
Hardline
gate.hardline.title
gate.hardline.tag1
gate.hardline.tag2
gate.hardline.desc
Overclock
gate.overclock.title
gate.overclock.tag1
gate.overclock.tag2
gate.overclock.desc
Breacher
gate.breacher.title
gate.breacher.tag1
gate.breacher.tag2
gate.breacher.desc
Piercing Round
gate.piercing_round.title
gate.piercing_round.tag1
gate.piercing_round.tag2
gate.piercing_round.desc
Reinforce Piyade
gate.reinforce_infantry.title
gate.reinforce_infantry.tag1
gate.reinforce_infantry.tag2
gate.reinforce_infantry.desc
Reinforce Mekanik
gate.reinforce_mechanic.title
gate.reinforce_mechanic.tag1
gate.reinforce_mechanic.tag2
gate.reinforce_mechanic.desc
Reinforce Teknoloji
gate.reinforce_tech.title
gate.reinforce_tech.tag1
gate.reinforce_tech.tag2
gate.reinforce_tech.desc
Medkit
gate.medkit.title
gate.medkit.tag1
gate.medkit.tag2
gate.medkit.desc
Field Repair
gate.field_repair.title
gate.field_repair.tag1
gate.field_repair.tag2
gate.field_repair.desc
Execution Line
gate.execution_line.title
gate.execution_line.tag1
gate.execution_line.tag2
gate.execution_line.desc
Scatter Chamber
gate.scatter_chamber.title
gate.scatter_chamber.tag1
gate.scatter_chamber.tag2
gate.scatter_chamber.desc
Payload Chamber
gate.payload_chamber.title
gate.payload_chamber.tag1
gate.payload_chamber.tag2
gate.payload_chamber.desc
Conductor Lens
gate.conductor_lens.title
gate.conductor_lens.tag1
gate.conductor_lens.tag2
gate.conductor_lens.desc
Final Prep Stabilizer
gate.final_prep_stabilizer.title
gate.final_prep_stabilizer.tag1
gate.final_prep_stabilizer.tag2
gate.final_prep_stabilizer.desc
9. Shared tag key’leri

Bunlar kısa ve tekrar kullanılabilir etiketlerdir.

tag.power
tag.tempo
tag.armor
tag.elite
tag.swarm
tag.army
tag.sustain
tag.tactical
tag.geometry
tag.boss
tag.boss_prep
tag.pierce
tag.beam
tag.shotgun
tag.launcher
tag.sniper
tag.assault
tag.smg
tag.heal
tag.reinforce
tag.damage
tag.control
tag.area
tag.range
tag.close
tag.safe
tag.risk
Kural

Gate alt tag’leri mümkün olduğunca bu ortak key’lerden seçilmeli.
Her gate için ayrı yeni kısa tag üretme.

10. Build Snapshot key’leri
build.balanced
build.swarm_clear
build.armor_break
build.elite_hunt
build.boss_prep
build.close_burst
build.area_control
build.safe_generalist
build.high_tempo
build.long_fight
11. Threat Tag key’leri

Stage kartında ve bazı özet ekranlarda kullanılacak.

threat.swarm
threat.armor
threat.elite
threat.mixed
threat.boss_prep
threat.close_pressure
threat.lane_pressure
threat.priority_target
threat.long_fight
12. Reward / progression key’leri
reward.gold
reward.first_clear
reward.mid_run
reward.stage_clear
reward.total

progress.upgrade_weapon
progress.upgrade_equipment
progress.next_stage
progress.unlocked
progress.level_up
13. Tutorial / onboarding key’leri
tutorial.move
tutorial.auto_shoot
tutorial.choose_gate
tutorial.swarm_warning
tutorial.charger_warning
tutorial.armor_warning
tutorial.elite_warning
tutorial.boss_warning
Kural

Tutorial text kısa olacak.
Uzun açıklama yazılmayacak.

14. Boss / phase key’leri
boss.phase_1
boss.phase_2
boss.transition
boss.warning.line_shot
boss.warning.front_sweep
boss.warning.charge
boss.defeated
15. SO bazlı kullanım kuralları
WeaponArchetypeConfig

Kullan:

weaponNameKey
descriptionKey
EnemyArchetypeConfig

Kullan:

displayNameKey
descriptionKey
GateConfig

Kullan:

titleKey
tag1Key
tag2Key
descriptionKey
StageConfig

Kullan:

stageNameKey
threatTagKeys
WorldConfig

Kullan:

worldNameKey
bandNameKeys
16. Kod tarafında şimdi yapılması gerekenler
1

Yeni görünen metin eklerken önce key aç.

2

Kod içine direkt string yazma:

"Play"
"Victory"
"Armor"
"Elite"
"Boss Prep"
gibi
3

UI scriptleri key üzerinden text basacak yapıya hazırlanmalı.

4

SO’larda mümkün olduğunca display text yerine key tutulmalı.

5

EN + TR dosyaları en azından boş şablon olarak oluşturulmalı.

17. İlk EN / TR doldurma önceliği

İlk doldurulacak alanlar:

UI butonları
Gate başlıkları ve kısa tag’ler
Silah adları
Düşman adları
Threat tag’leri
Build snapshot’lar
Result / reward metinleri
Sonra
açıklamalar
flavor text
lore
18. Risk notları
Risk 1

Gate tag’leri Türkçe’de uzunlaşırsa okunurluk bozulur.

Çözüm

Kısa shared tag sistemi kullan.

Risk 2

Aynı şey için birden fazla key açılırsa kaos olur.

Çözüm

Bu belge tek kaynak olsun.

Risk 3

SO içinde hem düz metin hem key karışırsa sistem çorba olur.

Çözüm

Runtime UI için key’i kanonik kabul et.

19. Son karar özeti

Localization Key Map:

şimdi kurulmalı
EN + TR başlangıcını taşımalı
SO + UI + stage + gate + weapon + enemy dilini tek standarda bağlamalı
sonradan temizlik işini ciddi azaltmalı
revize edilene kadar kanonik kabul edilmeli