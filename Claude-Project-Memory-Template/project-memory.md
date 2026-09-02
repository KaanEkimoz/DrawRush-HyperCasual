# Proje Hafızası — DrawRush (Ekimoz Games)

> Projenin **canlı durumu**. Her önemli ilerlemeden sonra güncellenir.
> Yeni oturumda buraya bakıp "nerede kaldık" sorusuna cevap alabilmelisin.

---

## 🎁 4 META ÖZELLİK — "yayına hazır" oturumu (2026-07, tam yetki) — **69/69 yeşil**
Kaan'ın tasarım-eksik listesi (yıldız / dükkân / juice / düşman çeşitliliği) baştan sona kuruldu. Her biri play mode'da doğrulandı, ayrı commit'lendi, hiçbiri push edilmedi.

**1. Yıldız derecesi (`747d5830`).** `LevelScore.Evaluate(can)` → 1-3 yıldız (3=hiç hasar yok, 2=yarıdan fazla can, 1=tamamladın). Coin ödülü yıldıza bağlı: **10/20/35**. `PlayerProgress.BestStars/RecordStars/TotalStars` (level index'e göre, çanta sırası önemsiz) + `TrySpendCoins`. `GameServices.Health` eklendi (WinCondition sağlığı per-level wire olmadan okusun). `GameState.LastStars/LastStarsWereRecord` — win paneli buradan okur. `WinPanelStars` yıldızları pop ile yakar. 🪤 Panel zaten aktifken tekrar `ShowWinPanel` OnEnable'ı tetiklemez — gerçek akışta panel önce gizlenip sonra gösterildiği için sorun değil.

**2. Coin dükkânı (`e0d0469b`).** `CosmeticLibrary` (6 renk skini, data asset) → karakter gövdesi + iz rengini değiştirir. `PlayerProgress` owned-set + equipped + `CosmeticChanged`. `PlayerSkin` (kalıcı oyuncuda) MPB ile boyar — paylaşılan materyali kopyalamaz. 🪤 Karakterin `player.mat`'i TCP2 ama **bu sefer `_HColor` beyaz, mavi `_BaseColor`'dan** (grass'ın tersi) → `_BaseColor` boyaması çalışır. `ShopController` kartları runtime'da kütüphaneden üretir (UI wire yok); tap satın alır+kuşanır ya da kuşanır; açınca timeScale=0, kapanınca **eski değerine** döner (countdown atlanmaz). Kontrolcü **her zaman aktif** GeneralCanvas'ta (panel kapalıyken de Open dinleyicisi kurulsun).

**3. Çekirdek juice (`4641c801`).** `ProceduralWall.Revealed` artık duvarı geçiriyor (+`Color` açıldı; SfxPlayer imzası güncellendi). `DrawJuice` her duvar belirince o rengin dünya-uzayı patlamasını atar. `ScreenFlash` tam ekran: hasar=kırmızı, kazanma=beyaz (unscaled fade, raycast off, panellerin altında). 🪤 Kamera punch'ı **bilerek ertelendi** — rig CM2-shim vcam (CM3 paketi altında), yanlış impulse tüm kamerayı bozabilir.

**4. Düşman çeşitliliği (`ff94a9ec`).** Tek `EnemyFollow`'da 3 davranış (geriye uyumlu): `speedMultiplier` (hızlı=turuncu), `wakeRadius` (uyuyan nöbetçi=mor, yaklaşınca uyanır+renk değişir). Renk MPB ile (`EnemyMaterial` URP/Lit `_BaseColor`, TCP2 değil). Audit'in **her-kare SetDestination** sorunu da düzeldi (~10Hz throttle). Dağıtım: her levelin 2. düşmanı nöbetçi (31 adet, güvenli çünkü nöbetçi ≤ takipçi zorluğu), 25-34 levellerin 1. düşmanı hızlı (10 adet). Level 25 üçünü birden gösteriyor.

**⚠️ Release için KALAN (kod değil, süreç):** imzalı AAB build (`play-console-release` skill'i var ama keystore şifresi Kaan'da — Claude giremez) · gerçek cihaz testi · Play Console listeleme · **denge playtesti** (yıldız eşikleri, dükkân fiyatları, hızlı-düşman adaleti Kaan'ın hissetmesini bekliyor). Ayrıca overlay UI ekran görüntüsüne düşmüyor → dükkân/panel yerleşimi **veriyle** doğrulandı, gözle değil.
**Önceki oturumdan devreden:** çiçek/yonca yaprakları yeniden kesilmeli (ölçekleme çözmez, ray 20 birime çıkıyor) · zemin rengi TCP2 `_HColor` ile (grass tuzağı, project-memory'de).

---

## 🚨 MCP ROUTING (önce BUNU oku — yoksa yanlış projeye yazarsın!)
Aynı anda **3 Unity editörü açık**: DrawRush@4ff3b85c (port 6400), Mini Fantasy Defense@6b0c8e78 (6700), Brick-Game@71b6f67c (6702).
- **DrawRush'a ulaşmak için CAPITAL `mcp__UnityMCP__*` tool'larını kullan** + `set_active_instance("DrawRush@4ff3b85c")`. Bu oturumda capital server doğru route etti.
- **lowercase `mcp__unityMCP__*` server Brick-Game'e KİLİTLENDİ** (`set_active_instance` + `unity_instance` param'ı bile yok saydı). Sahte "assembly not found / ref-loss" hataları aslında bu yüzdendi (Brick-Game'de o assembly yok).
- **HER build/save/play öncesi DOĞRULA:** `execute_code` → `Application.dataPath` `/DrawRush/` içermeli. Değilse DUR, server/instance'ı düzelt. (instances listesi: `ReadMcpResourceTool server=UnityMCP uri=mcpforunity://instances`.)
  ⚠️ **productName ile doğrulama YAPMA** — 2026-07'de `DrawAndRush2` → **`DrawRush`** olarak değişti (rebrand). Tek güvenilir guard `dataPath.Contains("DrawRush")`.

---

## 🌱 ZEMİN RENGİ: DENENDİ, GERİ ALINDI (2026-07-17)
Zemin (`Assets/Materials/grass.mat`, **tek materyal, 34 levelin hepsi**) neon yeşil: `RGBA(0.300, 0.906, 0.191)`. Ölçüm sorunu doğruluyor — çevre yeşilleri s=0.63/v=0.56 (çalı) ile s=0.72/**v=1.00** (açık yeşil) arasında; zemin **v=0.91** ile en parlak aksanla yarışıyor. Zemin, üstündeki nesnelerin altında kalmalı. Tek materyal olduğu için **en yüksek kaldıraçlı görsel iyileştirme burada.**

### 🪤 `_BaseColor` bu materyalde İŞE YARAMAZ
Shader **`Toony Colors Pro 2/Hybrid Shader`**. Neon'u dayatan `_BaseColor` değil, **`_HColor` (highlight) = `RGBA(0.157, 1.000, 0.000)`** — mavi kanalı 0, yeşil 1.0. `_BaseColor`'ı değiştirmek görüntüyü neredeyse hiç etkilemiyor (denendi, ekran görüntüsüyle doğrulandı).
**AMA:** `_BaseColor=beyaz` + `_HColor=hedef` denemesi de **çuvalladı** — zemin krem/sarı oldu. Yani çarpım modeli (`albedo × HColor`) YANLIŞ; ışık/ramp başka türlü karışıyor. TCP2'nin ramp'ını anlamadan deneme yanılmayla ayarlamaya kalkma.
**Durum:** `git checkout` ile birebir geri alındı, çalışma ağacı temiz. Yapılacaksa TCP2 dokümanı okunup `_HColor`/`_SColor`/ramp ilişkisi anlaşılarak yapılmalı — ve 34 levelin tamamında kontrol edilmeli.

---

## 🎨 OK, İŞARET ETTİĞİ KENARIN RENGİNİ GİYİYOR (2026-07-17) — **62/62 yeşil**
Kaan: "kafasındaki okun rengi çizdiği kenarın rengi olsa". Ok artık yön DEĞİL, **hangi kenar** diyor — o kenarın boyasıyla ve olacağı duvarla aynı renkte.
- `EdgeNetwork.ColorOf(edge)` eklendi (bağ zaten vardı: `Dictionary<DrawEdge, DrawEdgeAuthor> _authors`, sadece kapalıydı).
- `RailPaintController.TryGetGuidance(out heading, out color)`. `ResolveNextHeading` → `ResolveNextEdge(out heading)`: yön ve renk **aynı kenardan** gelmeli, yoksa ok bir kenarı gösterip başkasının rengini giyer.
- `RailDirectionArrow`: **MaterialPropertyBlock** ile boyanıyor (`_BaseColor` + `_Color`), renk değişince. Paylaşılan materyal hiç kopyalanmıyor — doğrulandı: block koyu renkteyken asset hâlâ sarı.
- Oyunda **26 farklı kenar rengi**; 7 level (suratlar/elma/karpuz/kiraz) **kendi içinde 2 renk** taşıyor → özellik gerçekten anlam kazanıyor. Suratların göz rengi `RGBA(0.12,0.10,0.18)` (22 kenar) neredeyse siyah; yeşil zeminde **iyi okunuyor**, ekran görüntüsüyle doğrulandı.
- 🪤 `bool show = rail != null && rail.TryGetGuidance(out Vector3 d, out Color c);` **derlenmez** — `&&` kısa devre yapınca out'lar atanmamış sayılır. Değişkenleri önce tanımla. (Eski tek-out'lu hâli bunu gizliyordu, o yüzden `TryGetGuidance` iki kez çağrılıyordu; o da temizlendi.)

### 🪤 Test ederken kendi kurallarım engel oldu (ürün hatası DEĞİL)
- Ok **düz yatay** (XZ). Sığ açıdan bakınca kenarından görünür = ince şerit. Ekran görüntüsü için **tepeden** (75-90°) bak.
- Düşmanlar temas edince `Detach()` çağırıyor → rehberlik sıfırlanıyor → ok kayboluyor. Level_25'te 3 düşman oyuncunun üstündeydi. Test için düşmanları kapat (play mode değişiklikleri çıkışta geri alınır — 110/110 doğrulandı).

---

## 👻 YERDE ÖNİZLEME + İNCE DUVAR + HİLAL ONARIMI (2026-07-17) — **62/62 yeşil**

**1. Noktalı hayalet iz (`DrawEdgeView`).** Kaan: "çizeceği yer önceden yerde belli olsa". Kritik nokta: hayalet **tüm kenarı değil, SADECE boyanmamış aralığı** (`fill.PaintedLow` → `PaintedHigh`) çiziyor. Boya iki uçtan noktaları yiyor → z-fighting yok, çift çizgi yok, ve anlamı doğru: "kalan yol". Duvar yükselince `Hide()` hepsini kapatıyor.
- Assetler: `Drawing/Textures/GhostDash.png` (wrap=**Repeat** şart) + `Drawing/Materials/GhostPath.mat` (URP/Unlit transparent, alpha 0.30, tiling **1.6**). `Kenar.prefab`'a bağlı → 192 kenarın 188'i miras aldı, 4'ü elle.
- 🪤 **`LineTextureMode.Tile` zaten dünya birimi başına tekrarlıyor.** İlk yazdığımda uzunlukla ayrıca çarpıyordum (çift uygulama) **ve** `_ghostSpan.material` ile kenar başına materyal kopyası sızdırıyordum. İkisi de kaldırıldı: desen sıklığı **materyal asset'inin tiling'inde** yaşıyor, kodda değil.

**2. Duvarlar %10 ince:** `wallThickness` 0.7 → **0.63** (192 kenar + prefab + script default). Köşe direkleri `EdgeNetwork`'te `WallThickness * 1.2` olduğu için kendiliğinden takip etti (0.756).
- 🪤 **Kesilebilecek toplu işlemde ÇARPMA kullanma, MUTLAK değer yaz.** MCP sınıflandırıcısı "servis yok" hatası verdi ama çağrı aslında çalışmıştı: 192 kenarın 174'ü 0.63 oldu, sonra tekrar denedim ve onlar 0.567'ye (0.7×0.9×0.9) düştü. Doğrulama yakaladı. `= 0.63` idempotenttir, `*= 0.9` değil.

**3. Hilal (Level_17) — oyunun en kötü leveliydi, ölçüldü.** 2 kenar: **16.82** ve 10.17 birim (yıldızın kenarı 2.6, üçgen 5.4). 4 anchor ama sadece **2 farklı konumda** → ekranda 2 küre ve aralarında görünmeyen iki yay, üstüne 5 düşman. 16.8 birim boyunca inilemeyen ray.
- **Çözüm:** dış yay 3'e (5.60 ×3), iç yay 2'ye (5.08 ×2) bölündü → **5 kenar, 5 küre**. Tüm yeni noktalar **orijinal iki çemberin üstünde** → siluet hiç değişmedi, sadece ray parçalandı. Şekil puanı 3.0 → 7.5 (otomatik, `ShapeScore` sahneyi okuyor).
- Değişmez kural korunuyor: merge 1.5 < en kısa kenar 5.08 ✓ · en yakın **farklı** anchor 3.27 > 1.5 ✓ (yanlış birleşme yok).
- 🪤 **`Kenar.prefab`'da `WP` çocuğu YOK** — waypoint opsiyonel, yay olan kenarlara elle eklenir. Yeni instance'larda `Find("WP")` null döner; yoksa yaratmak gerekir.
- ⚠️ Kaan'ın asıl teşhisi ("aynı yöne bakan 2 kenar") doğruydu ama sebep teğet belirsizliği DEĞİLDİ: `GetEdgesTouching` sadece kenarın KENDİ anchor'larını döndürüyor, yani seçim belirsizliği yok. Gerçek sorun uzunluk + görünmezlikti.

---

## 🔇 SESSİZLİĞİN SEBEBİ: LFS SMUDGE TUZAĞI (2026-07-17) — **çözüldü**
Kaan "hiç ses duymadım" dedi. Mute değildi, mixer değildi, kablo değildi: **5 WAV'ın her biri 130 baytlık git-LFS pointer METNİYDİ**, ses değil.

**Sebep:** LFS kilitlenmesini aşmak için kullanılan `git ... -c filter.lfs.smudge= -c filter.lfs.process= merge` komutu. O bayraklar tam olarak *"pointer'ı gerçek dosyaya çevirme"* demek → o merge **tüm LFS dosyalarını pointer metni olarak çalışma ağacına yazdı**. Workaround'un bedeli fark edilmemişti.

### 🪤 Neden sessiz: `git status` bunu TEMİZ gösterir
LFS'te index zaten pointer tutar. Çalışma ağacındaki dosya pointer metni olunca `clean` filtresi onu "zaten pointer" diye aynen geçirir → index'le eşleşir → **git hiçbir fark görmez**. Tarama şart:
```bash
git ls-files -z | while IFS= read -r -d '' f; do
  head -c 45 "$f" 2>/dev/null | grep -q "^version https://git-lfs" && echo "$f"
done          # ⚠️ `for f in $(git ls-files)` KULLANMA — "Epic Toon FX" gibi boşluklu yolları sessizce atlar
```
**Onarım:** `git lfs checkout` mevcut dosyanın üstüne YAZMAZ. Çalışan tek yol:
```bash
rm <dosya> && git checkout HEAD -- <dosya>     # silince smudge filtresi devreye girer
```
**Kapsam:** 741 dosya bozuktu.

### ❌ 2026-07-17: "Kalan 733 kullanılmıyor" TESPİTİM YANLIŞTI — hepsi geri yüklendi
İlk taramada sadece `_Project` altındaki 8'i (5 ses + 3 app ikonu) onarıp kalanına "kullanılmıyor" dedim. **Dayanağım eksikti:** sahnedeki UI Image'ların sprite'ına baktım, ama **parçacık materyallerinin dokularına bakmadım**. Kaan "confettiler full beyaz" deyince çıktı: `confetti_AB` / `glow1_ADD` / `cloud_2x2_default_AB` materyallerinin `mainTexture`'ı NULL'dı → beyaz kare. Sebep yine aynı LFS pointer'ları. Geri yükleyince **plaj çevresi de geldi** (şemsiye, deniz, kumkale, palmiye) — `beach` paketi de kullanılıyormuş. **Oyunun tüm çevre sanatı bozuktu.**
- **Ders:** "kullanılıyor mu" sorusunu sahne bileşenlerinin bir ALT KÜMESİNE bakarak cevaplama. Materyal→doku, parçacık, animasyon, prefab zincirleri gözden kaçar. Bozuksa onar; "kullanılmıyor" demek kanıt ister.
- 🪤 **İki ayrı Epic Toon FX kopyası var:** `_ImportedAssets/Epic Toon FX/` ve `_ImportedAssets/TemelAssetler/Epic Toon FX/`. Confetti efekti **her ikisinden** doku çekiyor (confetti.png birinden, glow1/cloud diğerinden). Paket sayımını klasör adına göre yapınca kullanılan kopya gözden kaçar.
- **Şu an:** `Assets/` altında **0 pointer** kaldı (690 dosya geri yüklendi). Commit gerekmedi — repo hep doğruydu. Tek diff: Unity'nin 2015 tarihli `confetti.png.meta`'yı serializedVersion 2→13'e yükseltmesi (GUID korundu, zararsız).
- Confetti neden beyazdı: `confetti.png` bir **3×3 atlas**, içinde **9 doygun renk**; parçacık rastgele hücre seçiyor. `startColor` beyaz olması DOĞRU — renk dokudan gelir. Doku yoktu, o yüzden beyazdı.
**Commit gerekmedi** — repo hep doğruydu, bozuk olan sadece çalışma ağacıydı. Ama build o ağaçtan alınır, o yüzden önemliydi.
**Ses zinciri sağlam:** `AddCoins(1)` → `CoinsChanged` → `SfxPlayer` → `PlayOneShot` → `isPlaying=True` ile uçtan uca doğrulandı (`source.Play()` ile değil).

---

## 🏷️ NAMESPACE: `Studios208.DrawRush.*` → `DrawRush.*` (2026-07-17) — **62/62 yeşil, 0 eksik script**
Kaan'ın seçimi: şirket adı **bilerek** koda girmedi (marka değişirse kod etkilenmesin). 55 `.cs` + 2 asmdef.

**Neden güvenliydi (önce ölçüldü):** projede **`[SerializeReference]` yok** → tüm script referansları dosya GUID'i üzerinden. Namespace/assembly adı serialize veriye girmiyor. Doğrulandı: 26.796 sahne bileşeni, **0 eksik script**; tüm SO'lar yeni tiplere bağlandı.
- `.asset`/`.prefab`'taki `m_EditorClassIdentifier: Studios208.DrawRush::...` sadece önbellek, yetkili değil — Unity yeniden yazar.
- asmdef dosyaları `.meta` ile **birlikte** `git mv` edildi → GUID korundu.

### 🪤 `[MovedFrom]`'a DOKUNMA
`PersistentObject.cs`'te:
```csharp
[MovedFrom(sourceNamespace: "Studios208.DrawRush.Core", sourceAssembly: "Studios208.DrawRush", sourceClassName: "DontDestroyOnLoad")]
```
Bu bir **tarihî kayıt** — "bu tip eskiden şuydu" deyip eski veriyi eşliyor. Toplu bul-değiştir bunu da vurdu, geri alındı. İçindeki isim yeniden adlandırılırsa eşleme kopar ve satır olmamış bir taşımayı anlatır. **Kodda `Studios208` geçen tek meşru yer burası.**

---

## 🎲 ZORLUK EĞRİSİ + ÇANTA SİSTEMİ (2026-07-16) — **62/62 test yeşil**
Kaan tutorial'dan sonra **3 düşmanlı yıldız** yedi. Suçlu: `WinPanel/I_NextLevel → GameManager.LoadRandomLevel` — level sırası diye bir şey yoktu, **düpedüz rastgele**ydi. (Kaan'ın save'i bunu kanıtladı: `LastLevelIndex=12` = yıldız.)

**Zorluk puanı:** `kenar×1.0 + yay×0.5 + düşman×3.0`. **Düşman ağırlığı Kaan'ın kararı** ("gerçekten zorlayıcı bir etmen") — kenar sadece çizimi *uzatır*, düşman ise rail'den kopmaya zorlar ve run'ı bitirebilir. Yan etkisi kasıtlı: düşman artık **kaba bir kadran**, ekleme = gerçek sıçrama. `AnEnemyOutweighsThreeEdges` testi bu kararı kilitliyor.

**Mimari — çanta ŞEKLİ seçer, düşman sayısı HEDEFİ tutturur.** İkisi çatışıyordu (tekrarsız çanta + testere eğrisi: kolay şekiller bitince düşük hedef karşılanamaz). Çözüm Kaan'ın kendi kaldıracı: *"gerekirse düşmanı çıkart"*.
- `LevelBag.cs` — saf C#, Unity tipi yok, unit-test edilebilir. Tekrarsız çekiliş + otomatik refill. Level ekledikçe çanta kendiliğinden büyür.
- `LevelDifficulty.cs` — `ShapeScore` / `EnemyBudget` / `SawtoothTarget` / `ApplyEnemyBudget`. Düşmanlar **spawn edilmez**, yazarındakiler açılıp kapanır (el konumları korunur).
- `LevelFlow.NextLevel` — testere hedefi → çantadan çek → düşman bütçesi → `ActivateLevel`. `LoadRandomLevel` artık `NextLevel`'e delege (sahne/prefab kablosu için emniyet); buton yine de dürüstçe `NextLevel`'e bağlandı.

### 🪤 Bu sistemde iki ölçülmüş tuzak (tekrar düşmeyin)
1. **Çantayı çıplak şekil puanına göre sıralamak.** Hedef düşmanı içerir, şekil puanı içermez → her yüksek hedef en karmaşık şekli çeker, düşman sadece *artık* kalır. Doğrusu `AttainableScore` = şekil + o hedefte açılacak düşmanlar. Orta bir şekil + düşman, yüksek hedefe cevap verebilmeli.
2. **Tabanı deneyimle yükseltmek** (ilk versiyonum böyleydi). ~30 level sonra taban 14.6'ya çakılıyordu: nefes molaları yok oluyor **ve** levellerin yarısı (üçgen tavan 3.0, altıgen 6.0, kare 10.0) o tabana çıkamıyor bile → testere düzleşiyor, kampanyanın yarısı kalıcı hedef-altı. Doğrusu: **taban sabit, TEPE yükselir.** Ölçüm: ort.|sapma| **3.40 → 0.83**. `EveryToothStartsBackAtTheFloor` testi koruyor.

**Ayar:** min 3 · max **21** · period 5 · ramp 30. Max 24 denendi, **çöp**: 34 levelin sadece 4'ü 24'e çıkabiliyor (9'u ≥20). **Tavanı içeriğin ulaşabildiği yerin üstüne koyma** — tepeler kalıcı hedef-altı kalır.
**Denenip ELENEN:** "eşit adaylar arasında en ağır şekli harca" (kolay şekilleri molalara sakla) → molalar +0.86→+0.63 iyileşti ama genel isabet 0.89→1.03 bozuldu, en kötü durum aynı. Ali'den alıp Veli'ye vermek — **eklenmedi**.

**Kalıcılık:** `LevelsPlayed` (eğri konumu) · `LevelBag` (CSV, cycle ortası çıkışta tekrar yememek için) · **`LastLevelEnemies`** — bu bir *bug fix*'ti: düşman sayısı artık levele gömülü değil, oyunu kapatıp açan oyuncu 1 düşmanlı molasını **4 düşmanla** geri alıyordu. `-1` = bilinmiyor, yazarındaki gibi bırak.

**Gerçek sahnede doğrulandı** (play mode'a girmeden, gerçek `DrawEdgeAuthor` verisiyle, 34 çekiliş): played=0 → **Level_02 (üçgen), 0 düşman, 3.0** · yıldız (L12) 30. sıraya düştü · **34/34 tekrarsız** · ort.|sapma| 0.94. Kuyrukta 1-2 level sapıyor (çantada sadece ağır şekiller kalır, şekil kendi puanının altına inemez) — **tekrarsızlığın kaçınılmaz bedeli**, kabul edildi.

⚠️ Kaan'ın save'i sıfırlandı (sıfırdan playtest için). Sahnede pasif duran **2 düşman aktif edildi** — normal akışta hükümsüz (`ApplyEnemyBudget` hepsini baştan yazar), ama bilinçli bir authoring'se söyle.

---

## 🔎 5-AJANLI DENETİM + DÜZELTMELER (2026-07-16)
5 paralel ajan taradı (bug / Unity-runtime / tasarım-boşluk / release / kalite). **Düzeltilenler (hepsi commit'li, 44/44 test yeşil):**
- **Kalıcılık:** `LevelFlow` PlayerPrefs'e yazıyordu ama **kimse okumuyordu** → oyuncu her açılışta L1'e dönüyordu. Artık `PlayerProgress.LastLevelIndex` + `LevelManager.ResolveResumeIndex()`.
- **`timeScale` kilidi:** `GameManager.Awake` 0 yapar, geri açan TEK şey countdown (`FindFirstObjectByType` inaktifi atlar) → countdown yoksa oyun **donmuş açılır**. `ActivateLevel`'a `else Time.timeScale=1f` eklendi.
- **Magenta duvarlar (ship bug):** WallMesh runtime'da yaratılıyor (materyal null) + `Kenar.wallMaterial` boş → her duvar `Shader.Find`'a düşüyordu; **stripped IL2CPP'de null** → telefonda hepsi magenta. `ProceduralWall.fallbackMaterial` + `ProceduralWall.mat` asset'i prefab'a bağlandı (asset ref shader'ı build'de tutar). Materyal sızıntısı da kapandı.
- **Yerçekimi:** `isGrounded` reset'i yoktu → dakikalar sonra oyuncu düşmüyor, **zeminden geçiyordu**.
- **Soft-lock:** `LevelManager.Reset`'te GameServices fallback yoktu → `IsGameWon` sonraki levele taşınıp oyuncuyu kalıcı donduruyordu.
- **Rail %95'te bırakma:** komşu kenarın drop'una değince kenar iptal oluyordu → artık `_localT>=0.5` sonrası yabancı drop'lar yok sayılıyor.
- **Bayat win paneli:** `WinSequenceDirector`'a run-token eklendi.
- **🚨 `cornerMergeDistance` (10 level):** kural **`dropGap < merge < enKısaKenar`**. Hepsi 1.5 default'uyla geliyordu ama inset kenarları 0.68'e kadar kısaltmıştı → kenarın kendi iki ucu tek köşeye düşüyor, **simetri bozuluyordu** (çiçek 5 yerine 3 köşe). Her level için pencere ölçülüp orta nokta yazıldı. **`LevelIntegrityTests` bunu artık kalıcı koruyor.**

**Yeni:** `SfxPlayer`+`SfxLibrary` (event-driven ses, ffmpeg placeholder klipler — **kulakla test edilmedi**), gerçek 3D render'dan **app ikonu** (18/18 Android slot), `ShowcaseDirector` artık `#if UNITY_EDITOR`.

### ⚠️ KALAN (denetimin bulduğu, henüz YAPILMADI)
Coin **hiçbir şeye harcanmıyor** (plan skin'i #1 öneriyor) · **power-up yok** (5 planlı) · **tek düşman tipi, boss yok** · **Perfect/yıldız/combo skor yok** · **tutorial hiçbir şey öğretmiyor** · **6 level eksik** (çark, Dev Kalp, Dev Gülücük…) · `EnemyFollow` her karede `SetDestination` (5-10 Hz'e düş) · `ProceduralWall` runtime NavMesh carving (hitch kaynağı) · PlayMode testi yok · `LevelFlow`'un "Level" PlayerPrefs'i artık atıl.
**Planın "içbükey şekiller winding-upgrade ister" varsayımı GEÇERSİZ** — per-component çözümü hallediyor, PDF'teki ~20 "UPGRADE" kartı aslında HAZIR.

---

## 🏗️ LEVEL BUILD DURUMU (2026-06-08, capital UnityMCP'de yapıldı)
**35 level** var (`===LEVELS===`, hepsi C=(1.79,-0.47) merkezli, ===ARENA=== border içinde; Level_N index N'de — `ActivateLevel(index)` İSME değil INDEX'e göre çalışır, çocuk sırası korunmalı):
- 0 Tutorial, 1 kare, 2 üçgen, 3 altıgen, 4 çember, 5 beşgen, 6 quad, 7 yedigen, 8 sekizgen, 9 quad, 10 oval, 11 çember6
- 12 yıldız, 13 artı, 14 uçurtma, 15 hexagram, 16 kalp, 17 hilal
- 18 elmas, 19 ok, 20 damla(v2 sivri-üst/yuvarlak-alt), 21 şimşek, 22 kalkan, 23 çiçek(5 yaprak), 24 yonca(4, koyu-yeşil)
- **EMOJI:** 25 smiley, 26 grin(açık ağız), 27 üzgün(frown), 28 şaşkın(O ağız) = yüz(4yay)+2göz(2yay)+ağız
- **MEYVE:** 29 limon(lens), 30 muz(hilal), 31 armut(damla yeşil), 32 elma(çukurlu çember+sap), 33 karpuz-dilimi(düz üst+yay), 34 kiraz(2× çember+sap)
- **2 KRİTİK BULGU:**
  1. centroid-outward **tüm içbükey tek-döngüleri** doğru render eder → winding-upgrade gereksiz.
  2. **ÇOK-PARÇALI** için EdgeNetwork'e **per-component interior** eklendi (commit a424fc7c): union-find, paylaşılan corner'la bağlı kenarlar=bir bileşen, her bileşen kendi centroid'ini interior alır → ayrık döngüler (göz/ağız) ters dönmez. 40/40 EditMode yeşil. **Kiraz kuralı:** 2 kirazı BİRBİRİNE bağlama (tek bileşen→centroid dışarı→flip); her "kiraz+kendi kısa sapı" ayrı bileşen, centroid kirazın içinde.
- **BUILD PIPELINE (kanıtlı):** Level_01 klonla → Edges temizle → Kenar.prefab'tan N `InstantiatePrefab` → anchorA=A+dir*inset / anchorB=B-dir*inset (`inset≈0.3-0.42`, cornerMergeDistance=1.5) → yay için child "WP" + author.waypoint ata → `wallColor` SerializedObject. Çok-parçalı: tüm parça kenarları tek Edges altında, tek EdgeNetwork hepsi boyanınca win.
- **🚨 NAVMESH:** Level_01 şablonunun `navMeshData=NULL`'du → klonlar NavMesh'siz (düşman spawn edemez: "no valid NavMesh"). DÜZELTİLDİ: L01+L12-L34 her biri `NavMeshSurface.BuildNavMesh()` ile bake (geçici SetActive(true)→bake→eski state). Açık zemin bake'lenir, duvarlar runtime carving ile oyulur. **Yeni level → MUTLAKA bake + AssetDatabase.SaveAssets.**
- **RENDER-TEST (play):** `ActivateLevel(idx)`; duvarları görmek için reflection: EdgeNetwork `_edges/_authors/_edgeInterior/CornerEndFor` → her author.Reveal(edge, _edgeInterior[edge], CornerEndFor(A), CornerEndFor(B)). Top-down screenshot: view_position=[1.79,15,-0.47] view_rotation=[90,0,0]. Play-modda klon kurup INDEX aktive etme (sıra kayar) — preview'de İSME göre aktive et.
- **KALAN:** her level'a PDF dalga-eğrisine göre düşman sayısı (şu an hepsi L01'in 2 düşmanını miras alıyor). Opsiyonel: çark/dişli, daha çok emoji/meyve.

---

## 📄 ÖNEMLİ — Level Tasarım Planı PDF'i (yeni session BUNA bak)

**Dosya: `Design-Docs/DrawRush-Level-Design-Plan.pdf`** (16 sayfa, **41 level**) — Kaan ile birlikte hazırlanan, görselli, çok detaylı geometrik level tasarım planı.
- **Kaynak script:** `Design-Docs/generate_plan.py` (PIL ile oyun-stili şekil thumbnail'ları + reportlab ile düzen). Düzenleyip tekrar üretmek için: `cd Design-Docs && python3 generate_plan.py` (reportlab + PIL kurulu; matplotlib YOK, kullanma).
- **İçerik:** (1) Tasarım felsefesi + **zorluk DALGASI** (testere eğrisi — sürekli yükselmez!), (2) düşman tasarımı (kademe başına sayı/hız/spawn), (3) 41 level kartı (şekil çizimi + spec + HAZIR/UPGRADE), (4) **Çeşitlilik & Eğlence Fikirleri** (power-up/engel/düşman-varyant/mod/meta/juice), (5) yol haritası.
- **Kademeler:** 1 Öğren (çiftlik) → 2 Kur → 3 Eğlence/İkonlar (kalp/yıldız/hilal/damla/çiçek/artı/ok/şimşek) → 4 Usta → 5 **Emoji/Çok-Parçalı** (gülücük/üzgün/şaşkın) → 6 **Meyveler** (elma/armut/limon/karpuz/muz/kiraz).
- **Önemli içgörüler (dokümanda):**
  - **Zorluk dalgası:** Sürekli yükselme = duyarsızlaşma = sıkılma. Yeni mekanik tanıt → zorluğu kısa süre DÜŞÜR (öğrensin) → tırmandır → ani spike'tan kaçın. (Kaan bir video paylaştı, prensip ondan.)
  - **Çok-parçalı level mümkün:** Bir level birden çok AYRIK parça içerebilir (yüz+gözler+ağız; 2 kiraz+sap) — `EdgeNetwork` hepsi boyanınca win verir. Emoji/meyve = yeni level tipi.
  - **İçbükey duvar upgrade'i gerekli:** Kalp/yıldız gibi içbükey şekillerde `ProceduralWall` dış-yönü centroid yerine **polygon winding**'den hesaplanmalı (tek seferlik). Dokümanda "UPGRADE" işaretli şekiller bunu bekliyor.
- **Kaan'ın "en yüksek etki" önerim olarak vurguladığım 3'lü:** (1) skin'ler (coin meta), (2) win-juice (gerçek-görsel parlama + konfeti + paylaşım), (3) çizgi-üstü coin toplama.
- ⚠️ Bu PDF/PNG'ler `Design-Docs/` altında (Assets dışında, Unity import etmez). Git'e commit edildi mi diye `git log -- Design-Docs/` ile bak.

**🏗️ LEVEL İNŞASI BAŞLADI (2026-06-08, `claude-dev`):** Kaan "levelleri kur, kalite > hız, çift dikiş test et, hepsini bensiz tamamla" dedi. Plandaki şekilleri yeni level olarak ekliyorum.
- **TAMAM + commit `2b61d587`:** Level_05 beşgen, 06 elmas, 07 yedigen, 08 sekizgen, 09 uçurtma. Hepsi play-verified (edges=walls, IsComplete, köşe 1/post, düşman onNavMesh). Toplam 10 level (Tutorial+01-09).
- **İnşa deseni (tekrarlanabilir):** Level_01'i `Object.Instantiate` ile klonla → Edges'i temizle → her kenar için Kenar prefab'ı `PrefabUtility.InstantiatePrefab` + AnchorA/B world pos (köşeden ~0.6 inset = 2 damla/köşe) + arc ise Waypoint child + `wallColor` set → düşman sayısı (ring'e yerleştir, gerekirse `Instantiate` ile çoğalt) → **taze NavMesh bake** (`surf.navMeshData=null; BuildNavMesh; CreateAsset NavMesh_Level_XX.asset`) → save. Hepsi merkez **C=(1.79,-0.47), y=0.42, R~4**. Kenar prefab: `Assets/_Project/Drawing/Prefabs/Kenar.prefab`.
- **⚠️ DOĞRULAMA KURALI:** Level'ı test ederken **iki-geçişli** aktive et — önce `foreach SetActive(false)` HEPSİNİ kapat, SONRA hedefi aç (LevelManager böyle yapar). Tek-geçiş `SetActive(l.name==x)` yaparsan sonraki-indeks level hâlâ aktifken hedefin EdgeNetwork'ü onun author'larını toplar (tüm leveller aynı merkezde) → köşe-postları karışık renk olur. Bu sadece test artefaktı, oyunda olmaz.
- **SIRADA (devam et):** arc-convex (oval, stadyum, D, kemer, çember-6, damla), star-convex (yıldız, artı, hexagram, çark, dev-yıldız), **içbükeyler → ÖNCE ProceduralWall winding-upgrade'i** (kalp, hilal, çiçek, yonca, muz), emoji çok-parçalı (gülücük/sırıt/üzgün/şaşkın), meyveler (elma/armut/limon/karpuz/muz/kiraz). Boyut/enemy değerleri PDF planındaki WAVE'e göre. Sonra: kampanya sırasını WAVE'e göre sibling-reorder + tema/renk.
- **PAYLAŞILAN BORDER (`f744133d`):** Oyuncu büyük levellerde per-level dekor collider'larına (Fences/Playground) takılıyordu. Çözüm: `===SHARED===/===ARENA===` altında **tek ortak görünmez border** (4 BoxCollider duvar, **±9 x ±8.5 around C=(1.79,-0.47)**, 48×56 zemin içinde) → tüm leveller aynı sınırı kullanır. Tüm levellerin (Level_01 base dahil → gelecek klonlar miras alır) **border-içi non-Ground Environment collider'ları DISABLE** edildi (dekor görsel kalır, engellemez); dıştaki GameEnd kutusu (±12-16) + farm (kuzey) korundu. Play-verified: oyuncu sınırda duruyor, içeride engel yok. **KURAL: per-level border EKLEME, ortak ===ARENA=== kullan.** (Görsel polish: görünür fence'ler ±5-6'da kalıyor, geniş sınırın görünür hali istenirse ayrı iş.)
- Origin'e PUSH yok (main Kaan onayı ister) — sadece claude-dev commit.

---

## 🎯 Mevcut Durum — 2026-06-08 (çember level + arc bug + bug-avı + tasarım dokümanı)

**Bu oturumda master'a push edilenler (`master`=`claude-dev`, origin'de):**
- **`v0.28-arc-procedural-walls`** milestone (yay kenarlar + procedural modular duvar + 1.2× şekil) — detay aşağıda 2026-06-07 bölümünde.
- **Duvar polish** (`484760ec`): kenar duvarı 0.4→**0.7** kalınlık (köşe 1.2×), inside-out yüz fix (centroid'den dışa + alt kapak), köşe-gap fix (duvar uçları köşe-posta uzar). EndWallParts temizliği de (`43922d7d`, chip'ten).
- **Damla dizimi** (`4411cb96`): Level_02 üçgen + Level_03 altıgen damlaları **regular + her köşede 2 droplet** olacak şekilde yeniden dizildi (L3 düz-tabanlı altıgen).
- **Arc bug fix** (`31146c38`): yay sonuna doğru karakter çizginin yanında kalıyordu — `arrivalDistance` snap'i aktif çizimde 0.15'e gated edildi (bırakılınca 0.6 korunur). *(Kaan cihazda feel-test edecek.)*
- **Level_04 = 4 yaydan ÇEMBER** (`8b670f2f`): L1'den klonlandı, 4 çeyrek-yay R=4 çember, taze `NavMesh_Level_04.asset` bake + Enemies grubu aktivasyonu (düşmanlar onNavMesh). Index 4, L3'ten sonra oynar.
- **Bug-avı fix** (`5105ee44`): (1) `EnemyFollow.Update` `SetDestination` off-mesh guard'ı eklendi (agent mesh dışındayken her frame exception atıyordu) — **HIGH**; (2) `EdgeNetwork` köşe kesişimi ~2 birime sınırlandı (near-parallel'de uzağa post). EditMode **40/40**.
- **Tasarım dokümanı PDF'i** (yukarıdaki bölüm) — `Design-Docs/`, henüz repo'ya commit EDİLMEDİ (Kaan'a soruldu).

**Açık konular:** Tutorial'da hâlâ aktif eski `Walls/EndWall` (statik dekor); içbükey-duvar winding upgrade'i (kalp/yıldız için); per-level renk/yükseklik; emoji/meyve çok-parçalı prototip.

---

## 🎯 Mevcut Durum — 2026-06-07 (yay kenarlar + procedural modular duvar + şekil büyütme, `claude-dev`)

**Tek satır:** Çizim geometrisi artık **tek kaynaktan** akıyor (`DrawEdge.PointAt/TangentAt/Length`); kenar düz **veya 3-noktadan geçen çember yayı** olabilir. Duvarlar elle dizilen cube-strip yerine **kenar boyunca runtime üretilen procedural mesh** (`ProceduralWall`) — her şekle/uzunluğa/eğriye uyar, köşeler otomatik kapanır. Şekiller %20 büyütüldü. **7 faz, hepsi `claude-dev`'de**, doğrulama sonrası master'a tek `--no-ff` merge edilecek.

**Yapılan (fazlı, her biri ayrı commit `claude-dev`):**
- **Faz 1 (`bbefb327`):** `DrawEdge` yay geometrisi. `Waypoint {get;set;}` (null⇒düz). XZ'de 3-nokta çember (circumcenter/R/signed sweep); degenerate fallback (waypoint null, collinear `|d|<1e-5`, `R>1e4`) → düz Lerp. `PointAt`/`TangentAt`/`Length`/`IsArc`. **Uç noktalar t=0→A, t=1→B'ye pinlendi** (cos/sin float drift'i `Vector3.Equals` testini kırıyordu). 2-arg ctor korundu. +3 yeni EditMode test (semicircle PointAt(.5)≈waypoint, Length≈π).
- **Faz 2 (`371946e7`):** `DrawEdgeView.SetSpan` yayı `arcSegments`(24) parçaya böler (`IsArc`); düz 2-nokta hızlı yol. Boya çizgisi yayı takip eder.
- **Faz 3 (`eb95bfa5`):** `RailPaintController` yay-farkında. `len=edge.Length`, `edgeT`, `heading=±TangentAt(edgeT)`, `_localT+=along*speed*dt/len` (arc-length normalize), `pos=edge.PointAt(edgeT)`, rotation tangent'ten. `TrySelectEdge` anchor tangent'iyle seçer. Sabit hız, yayda da düzgün.
- **Faz 4 (`5cc365cd`):** **`ProceduralWall.cs` (YENİ)** — Kenar root'unda; child "WallMesh" (MeshFilter+MeshRenderer+carving NavMeshObstacle, mesh `hideFlags=DontSave`). `Build(edge,h,thick,mat,color)` kesit-kutu extrude (N=yay?24:1, `side=Cross(up,tan)`, vert'ler `InverseTransformPoint` ile local-bake → parent scale'den bağımsız doğru world), RecalculateNormals. Yerin altından `riseSeconds`(0.5) SmoothStep yükselir. `DrawEdgeAuthor` yeniden yazıldı: `wallSegment` KALDIRILDI; `waypoint`+`wallMaterial`+`wallColor`(kırmızı 0.85,0.2,0.18)+`wallHeight`(0.9)+`wallThickness`(0.4) + auto `ProceduralWall`. `Reveal(edge)`→Build+Reveal+View.Hide+drop'ları gizle. `EdgeNetwork` `author.Reveal(edge)`.
- **Faz 5 (`4edaaded`):** Köşe postu boyutu artık author'ın `WallHeight`/`WallThickness*2`'sinden. Yay köşelerinde chord-kesişimi anlamsız → grup endpoint pozisyonu kullanılır (yay uçları anchor'a pinli); düz-düz köşeler hâlâ tam çizgi-kesişimiyle flush.
- **Faz 6 (`cbd96d15`):** Level_01/02/03 `Edges` grubu **1.2× ölçek** (ground zaten 48×56 birim, dev — sadece şekil büyütüldü; runtime duvarlar NavMesh'i carve ettiğinden mevcut ground bake geçerli kalır, rebake'e gerek yok). **Köşe-postu dedup fix:** geçmiş edit-mode rebuild'lerden sahneye **4 stale "CornerPosts" GO sızmıştı** (runtime'da duplike oluyordu) → silindi; `BuildCorners` artık spawn'dan önce isimle TÜM "CornerPosts" child'larını yok eder (bir daha birikemez/serialize olamaz).
- **Faz 7 (bu oturum):** Doğrulama + wiring. **Yay özelliği gerçek levelde uçtan uca kanıtlandı:** Level_01 üst kenara geçici waypoint → play'de `PointAt(.5)=(0.35,4.74)` tam waypoint'ten geçti, pürüzsüz eğik duvar mesh'i (screenshot), flush köşeler → sonra **geri alındı** (Level_01 temiz kare; yay'ı Kaan kendi koyacak, "3. noktayı biz belirleyeceğiz" dediği için). Level_03 altıgen play-test: 6 duvar + 6 köşe (1 kök, duplike yok). Kare/altıgen/yay hepsi doğrulandı.

**Duvar polish (post-v0.28, master `484760ec` — 2026-06-07):** Kaan "duvarlar çok ince, üst/alt render olmuyor, köşeyle az boşluk var" geri bildirimi → 3 düzeltme (commit `28bbcd86`+`8d16da6a`):
1. **Kalınlık:** kenar duvarı 0.4→**0.7** (dolgun "bütün bir duvar"); köşe postu `WallThickness*2`→**`*1.2`** (köşe duvardan sadece bir tık büyük); yükseklik 0.9 sabit. (prefab + C# default; instance'lar prefab'tan miras alıyor → sahne diff'i yok.)
2. **Inside-out yüz fix:** her kenar yalnız kendi A→B yönünü bildiğinden `cross(up,tan)` bazı kenar yönlerinde **içe** bakıyordu → üst/alt kenar duvarları tepeden kameradan culling'le boş görünüyordu. `EdgeNetwork.ComputeCentroid` (tüm anchor ort.) şekil iç noktasını `Reveal→Build`'e geçirir; `ProceduralWall` `side`'ı centroid'den **dışa** çevirir + **alt yüz kapağı** ekler.
3. **Köşe-gap fix:** damlalar köşenin biraz içinde olduğundan duvar uçları köşe-postuna yetişmiyordu. `EdgeNetwork` her köşenin `PostPosition`'ını saklar; `CornerEndFor(edge,endpoint)` ile edge başına A/B köşe pozisyonunu `Reveal→Build`'e verir; `ProceduralWall` iki **uç halkasını** o pozisyona koyar → düz duvarlar köşeden köşeye span = flush (köşe yoksa anchor'a fallback). Play-verified (dik açı + köşe yakın çekim), EditMode **40/40**.

**⚠️ Açık konular (Kaan için):**
- **Orphan `EndWallParts` temizliği — KISMEN TAMAM (2026-06-07, `claude-dev` `43922d7d`):** Level_01/02/03'teki **13 inaktif orphan `EndWallParts`** (eski cube-strip) **silindi** (L01=4, L02=3, L03=6). Önce kod taraması: hiçbir C# scripti bu objelere serialized ref tutmuyor (DrawEdgeAuthor'da `wallSegment` yok, PartManager zaten silinmiş, sadece GameAnalytics enum'unda alâkasız `Wall`). Sahne diskte kaydedildi (binary), tek-dosya commit. **Play-doğrulama (gameplay-level):** 3 level de `LevelManager.ActivateLevel` ile aktive edilip tüm kenarlar `DrawEdge.PaintFrom` ile programatik tamamlandı → **L01 4/4, L02 3/3, L03 6/6 procedural duvar gerçek mesh üretti** (vertexCount>0), `net.IsComplete=true`, 0 exception, konsolda NullRef / EndWall-ref hatası YOK (sadece MCP bridge "client handler exited" altyapı logu). EditMode **40/40**. **→ Bu temizlik artık master'da** (duvar-polish merge'iyle `484760ec`'e dahil oldu).
- **⏳ Tutorial (Level_00) kararı Kaan'a soruldu:** Tutorial'da hâlâ **aktif** eski `Walls/EndWall` var (39 cube, 3× "End Wall" Animator, pos (-4.59,3.13,7.23), **hiçbir kod artık sürmüyor** — PartManager silindiği için sadece statik duruyor) + ayrıca inaktif `Playground/SquarePart (1)/EndWallParts` orphan. Tutorial kenarı (`SquarePart (1)/Edge_0`) zaten `ProceduralWall` kuruyor. Seçenek: (a) procedural'a migrate = aktif `Walls/EndWall` + inaktif orphan sil; (b) `Walls/EndWall` dekorasyon olarak tut, sadece inaktif orphan sil; (c) tutorial'a hiç dokunma. **Master'a merge Kaan onayı bekliyor.**
- **Per-level duvar rengi:** hepsi default kırmızı; Kaan isterse `DrawEdgeAuthor.wallColor`'dan level başına değiştirir (drop rengi = duvar rengi).
- **Duvar yüksekliği** `wallHeight=0.9` default; Kaan "daha yüksek" isterse author'dan artırılır.
- **Yay authoring akışı:** Kenar'ın author'ına bir Transform child ekle (kenarın büküleceği yere koy) → `waypoint` alanına ata. Boş = düz kenar. Tamamen modular.

**Test:** EditMode bu oturum başında **40/40 PASS** (Faz 1-3 sonrası). Faz 4-6'daki tek C# değişikliği `EdgeNetwork.BuildCorners` dedup'ı (testlerle kaplı değil, geometri/EdgeFill testleri etkilenmez). Merge öncesi son run yapılacak. ⚠️ Bu oturumda play-stop sonrası MCP bridge bir süre "Timeout receiving Unity response" verdi (editor focus gerektirebilir) — tekrar dene.

---

## 🎯 Mevcut Durum — 2026-05-30 (polish & juice oturumu, master `979cde51`)

Kaan ile interaktif polish oturumu. Hepsi master'a `--no-ff` merge + push (her commit ayrı). Sırasıyla yapılanlar:

**Restart-state hijyen (devam):** `WinSequenceDirector` win-particles `won=false`'da kapanır; `RailPaintController.Detach()` artık `LevelManager.MovePlayerToSpawn`'da çağrılır; `EnemyFollow` Awake'te authored spawn pos/rot kaydeder, OnEnable'da `NavMeshAgent.Warp` ile döner (restart'ta enemy başlangıç konumuna gider).

**Authored-edge = her kenar kendi 2 küresi (KARAR DEĞİŞTİ):** Kaan "her kenarın 2 küresi olsun, köşede 2 küre" dedi → `Kenar.prefab`'a AnchorA+AnchorB child küreleri eklendi (paylaşım YOK), `DrawEdgeAuthor` anchor'ları local child. Tüm sahnedeki Kenar'lar migrate edildi, eski paylaşılan DrawPoint'ler silindi (Tut 2 / L01 8 / L02 6 / L03 12 küre). Edge tamamlanınca `Reveal()` 2 küreyi de `SetActive(false)` (bitmiş kenara dokunma engellendi), OnEnable geri açar. Bug fix: Level_01 kare topolojisi (eksik SOL ÜST küre + yanlış wallSegment eşleşmeleri); L02/L03 wallSegment'leri en-yakın-eşleme ile düzeltildi. **DrawableArea mantığı tamamen kaldırıldı** (PlayerInteract/RailPaintController/EnemyCombat'tan IsInDrawArea + sahneden 14 DrawableArea silindi); anchor trigger'ları her yerde çalışır, combat safe-zone artık YOK.

**Küreler:** tüm DrawPoint'ler world-scale 0.50'ye eşitlendi (uniform, level'lar arası tutarlı).

**Combat juice & feel:**
- **Knockback:** `PlayerKnockback` (Player prefab) — enemy temasında dik yönde itme, linear decay. `EnemyCombat.knockbackForce` (per-enemy, **9** = eski 12'nin %75'i), `PlayerKnockback.knockbackDuration`. ThirdPersonMovement + RailPaintController knockback aktifken yield.
- **Hit reaction anim:** `player_hit_react_small.fbx` (Generic, KeyframeReduction) → `PlayerHitReactSmall.anim`, Player.controller'da `Hit` state. `AnyState→Hit` (t_hit trigger, **exit time 0, duration 0** = anında). `PlayerCombat.TakeDamage` SetTrigger(Hit).
- **Damage feedback:** `PlayerHitFeedback` (Player prefab) — kırmızı flash (1 yanıp sönme, `flashDuration=0.8`, `flashBlinks=1`, MaterialPropertyBlock, _BaseColor/_Color) + scale **büyü→küçül** punch (Visuals, `punchScale=0.2`, `punchDuration=0.8`).
- **i-frames:** `PlayerCombat.invulnerabilityDuration` (**3s**) — hasar sonrası pencerede yeni hasar yok sayılır. `IsInvulnerable` public. (Knockback i-frame'de hâlâ uygulanıyor — Kaan isterse kapatılır.)
- **Idle:** `player_idle.fbx` → `PlayerIdle.anim`, `Run↔Idle` (f_speed eşik 0.1). **Idle'da üst gövde kilidi:** `UpperBodyMask.mask` (Generic, üst gövde aktif/hips+bacaklar kapalı) + Player.controller `IdleArmsLock` layer (ArmsHold=PlayerRun speed 0). `ThirdPersonMovement.Update` layer weight'i Base "Idle" state'inde 1'e lerp → idle'da kollar sabit (kalem tutma), bacaklar idle.

**Floating joystick:** sabit On-Screen Stick → `FloatingJoystick` (OnScreenControl, `Assets/_Project/UI/Scripts/`). Dokunulan yerde belirir, bırakınca kaybolur, `<Gamepad>/leftStick`'e yazar (pipeline değişmedi). Joystick container full-area transparent raycast touch-zone, ring+handle birlikte toggle (handle ring child'ı DEĞİL, ikisi de SetActive). `movementRange=110`.

**Level akışı:**
- **3-2-1 countdown:** `LevelStartCountdown` (GameManager'da, `T_Countdown` TMP). Her level aktivasyonunda (start/next/restart) oyunu dondurur (timeScale 0), unscaled 3→2→1, sonra timeScale 1. **TAP TO PLAY kaldırıldı** (otomatik başlar). LevelManager.ActivateLevel sonunda `countdown.Begin()`.
- **Win confetti:** `GeneralCanvas/Particles` (3 ConfettiBlastRainbow, ekranda farklı yerlerde) win'de aktive (`WinSequenceDirector.winParticles` + GameManager.legacyParticles). Restart'ta gizlenir.
- **Editör tek-level testi:** `LevelManager.autoActivateOnStart` (`#if UNITY_EDITOR`, scene'de **false**) → editörde aktif bırakılan level Play'de korunur, build her zaman normal akış.

**NavMesh (legacy → AI Navigation):** Legacy bake silindi. Her level grubuna `NavMeshSurface` (collectObjects=Children, **useGeometry=PhysicsColliders** → çiçek/dekor collider'sız olduğu için bake'e girmez, sadece zemin). Her surface kendi `NavMeshData` asset'i (`Assets/_Project/Navigation/NavMesh_Level_*.asset`), level enable/disable'da otomatik add/remove. Duvarlar: her wallSegment'e **carving `NavMeshObstacle`** (box, carveOnlyStationary, **local-space bounds** → rotated alt/üst duvarlarda 90° ters carve fix'lendi). Duvar reveal → carve → enemy geçemez; restart'ta gizlenince carve kalkar. Sadece enemy'leri etkiler (player CharacterController).

**⚠️ Notlar:** Working tree'de `Assets/Prefabs/Edges/Edge.prefab` silinmiş görünebilir (eski kullanılmayan prefab, Kaan'a soruldu). EditMode 37/37 PASS. ⚠️ EditMode run_tests bazen ilk denemede "initialize timeout" veriyor → tekrar çalıştır. ⚠️ Scene değişikliği yaparken Unity Play modunda olabilir → `manage_editor stop` veya MarkSceneDirty "cannot be used during play mode" hatası. **Sıradaki Kaan için (yayın yolu):** 10-15 yeni level (Kenar prefab drag-drop), ses (UI/hit/win), 512² icon + store screenshots; sonra internal test → production.

---

## 🎯 Mevcut Durum — 2026-05-28 (authored-edge sistemi, otonom oturum)

**Tek satır (2026-05-28):** Çizim artık **authored edge** modeli. Köşe küreleri (DrawPoint) paylaşılır; her kenar bir **`Kenar` prefab'ı** (`DrawEdgeAuthor`: anchorA/anchorB + wallSegment + DrawEdgeView). `EdgeNetwork` sahnedeki Kenar'ları toplar (komşu hesabı YOK), kenar boyanınca **kendi duvar parçasını reveal** eder (per-edge), hepsi bitince win. `RailPaintController` edge'leri `EdgeNetwork.GetEdgesTouching`'den seçer. **4 level de bu sisteme çevrildi** (Tutorial 1, Level_01 4, Level_02 üçgen 3, Level_03 altıgen 6 edge). Level kurma akışı: Kenar prefab'ını sürükle → anchorA/B + wallSegment ata. **Ödül:** `PlayerProgress.Coins` (PlayerPrefs) kazanınca +10, `CoinHud` HUD'da gösterir (I_Coins üst-sol, placement Kaan'a göster-onayla). **Lose-flow düzeltildi:** GameManager.playerHealth wire edildi (ölünce LosePanel + pause) + PlayerCombat artık player'ı Destroy etmiyor (Restart resetler). Ölü komşu kodu (DrawPartNeighborGraph + DrawPart.Neighbors) tamamen silindi; DrawPart yalın anchor.

**Repo state:** `claude-dev` = otonom oturum işi (`014542cf`→`1a0565c7`); milestone master'a merge+tag+push edilecek (**`v0.27-authored-edges`**). Önceki milestone `v0.26-edge-painting` master'da. Build sahneleri: `00_SplashScreen.unity` + `01_DrawRushGame.unity`. Kenar prefab: `Assets/_Project/Drawing/Prefabs/Kenar.prefab`. Part prefab'ları (`Assets/Prefabs/New/{Square,Triangle,Altigen}Part.prefab`) eski PartManager'dan temizlendi (PartManager SİLİNDİ). Working tree: DrawPointMat/GvhProjectSettings kendiliğinden dirty olabilir (zararsız, restore et). ⚠️ Overlay UI (GeneralCanvas Screen Space Overlay) Main-Camera screenshot'unda GÖRÜNMEZ — UI'ı görmek için geçici Screen Space Camera'ya al.

**[ESKİ] 2026-05-16 mega-scene:** Scene-per-level mimarisi **tek `Game.unity` mega-sahneye** dönüştürüldü. `===SHARED===` (Player, Camera+vcam, Light, GeneralCanvas, GameManager, __Bootstrap, LevelManager) bir kez yaşıyor; `===LEVELS===` altında `Level_00_Tutorial`/`Level_01`/`Level_02`/`Level_03` grupları sadece level-specific içeriği (Enviroment, enemy, WallManager) taşıyor. Yeni `LevelManager.ActivateLevel(i)` tek grubu enable eder + state reset (health/win/chain/spawn). Önceki fazlar: anchor görsel swap → dedup (her köşede tek küre: L1=4, L2=3, L3=6, Tut=2) → köşegen-yasak chain (`DrawPart.IsNeighborOf`, en yakın 2 komşu, **artık level-group scope'unda**).

**Repo state:** `master` = `claude-dev` = edge-painting tamamı + temizlik + tutorial-skip, **origin'e push'lu** (Kaan "her şeyi maine pushla" dedi → `--no-ff` merge + tag + push yapıldı). Tag **`v0.26-edge-painting`**. İçerik: edge-painting step 1–7 + cihaz-testi fix'leri (`c94daaab` çift-çizme trail, `22c87e3a` karşı-küre completion+free, `95319286` duvar reveal anim, `e0d30325` restart dans/lock reset, `ce690b7e` Animator Dance→Run, `060d1a3e` win/lose buton onClick → sahne GameManager, `18204f24` restart ActivateLevel disable→enable, `fb5b583b` boya çizgisi = duvar rengi PartManager.GetFillColor, `74896304` reveal'da iz gizlenir, `07773c61`+`2f2b70c8` EDM4U sessiz + `[InitializeOnLoad]` enforcer her load'da zorlar). **Kod borcu temizliği (`fec8b2a9`→`ae8fa03c`):** DrawPartCompletionWatcher/DrawPartStateMachine/DrawingPhase/IDrawPart/IInteractable silindi, DrawPart yalın anchor, ölü GameConfig alanları kaldırıldı, **WallManager→WinCondition** rename. **Tutorial-skip (`fcb4b202`):** `PlayerProgress.TutorialCompleted` (PlayerPrefs) — tutorial geçilince flag set, başlangıçta flag varsa Level_01 yoksa tutorial; `LevelManager.startLevelIndex=-1` (auto, dev-override). Build sahneleri: `00_SplashScreen.unity` + `01_DrawRushGame.unity` (mega-sahne); eski `Level 1/2/3` + `TutorialLevel` → `Assets/Scenes/Old/` arşivi. Working tree temiz. EdgeFill core (`d8ebccf3`) push'lu. DrawPointMat/GvhProjectSettings sahne yüklemede kendiliğinden dirty olabilir (restore/commit zararsız). LFS aktif. Backup `backup-pre-claude-cleanup-1778895100`. ⚠️ DERS: branch checkout sırasında Unity açıksa `git checkout -- <path>` el değişikliklerini ezebilir → önce `git status`.

**Test:** EditMode 37/37 PASS (EdgeFill/DrawEdge/GameState/GameServices/PlayerHealth/EventChannel + yeni PlayerProgressTests coin'ler). EdgeNetwork/WinCondition/RailPaintController/CoinHud için EditMode test yok — MonoBehaviour/scene-bağımlı, hepsi Play'de doğrulandı (4 level edge + win + lose + restart + coins + level etiketi).

**Otonom oturum (2026-05-28 03:44→) bensiz alınan kararlar:** (1) Authored-edge mimarisini Kaan'ın 3-soru cevabına göre kurdum; duvar parçası Kenar prefab'ının `wallSegment`'ine konuldu. (2) **Lose-flow bug fix (`5bfccf24`):** GameManager.playerHealth wire'sızdı (LosePanel hiç açılmıyordu) + PlayerCombat ölünce player'ı Destroy ediyordu (mega-scene'de restart'ı kırıyordu) → ikisi de düzeltildi. (3) **Coin ödülü (`1a0565c7`):** PlayerProgress.Coins + CoinHud (üst-sol HUD). **Coin HUD placement'ı overlay olduğu için Main-Camera screenshot'ta görünmedi; canvas'ı geçici Screen Space Camera'ya alıp doğruladım — "10" üst-solda düzgün; yine de Kaan görsel onaylasın.** (4) **Level etiketi fix (`c3305ef1`):** "Level 6" (stale PlayerPrefs) → LevelManager gerçek level'a göre "Tutorial/Level 1/2/3" yazar. (5) Level başına coin 5/10/15/20 (`21170c92`). (6) Master'a v0.27-authored-edges milestone + sonraki tüm bonus commit'ler push edildi (Kaan "önceki onayları kabul" + "tam yetki"). (7) **Review'da 2 restart-state bug bulundu+düzeltildi (master'da):** `29fb1e9c` DrawEdgeAuthor duvarı OnEnable'da gizler (restart'ta açık kalmıyordu); `f4622b6c` EnemyFollow OnEnable'da `_halted` sıfırlar (restart'ta düşman donuyordu). Düşman die-anim'i restart'ta Unity'nin Animator reset'iyle "Run"a dönüyor (bug yok). (8) **EDM4U** auto-resolution KAPALI'ya çekildi (`a2f3dc59`) — açıkken "Gradle failed to fetch dependencies" gürültüsü vardı; şimdi prompt yok + nag yok + build'de çözülür. (9) **Wake 2 (07:09 master `375e8197`):** Aynı restart-state hijyen pattern'ı bir kez daha — `WinSequenceDirector.OnGameWonChanged` sadece `won=true`'ya tepki veriyordu → restart'tan sonra win-particles GO `activeSelf=true` taşıyordu. `won=false`'da SetActive(false) eklendi (`a72aebfb`). + stale doc-comment refresh (RailDrawController/PartManager/WallManager → güncel isimler). 37/37 EditMode + console clean. (10) **Wake 3 (09:10 master `e0658e48`):** 4. restart-state hijyen bug'ı — `RailPaintController` persistent Player GO'da yaşadığı için level grup disable/enable cycle'ından ETKILENMIYOR; mid-paint ölüp restart yapan oyuncuda `_currentPart`/`_edge` eski level'ın referanslarını taşıyordu. `LevelManager.MovePlayerToSpawn` artık `RailPaintController.Detach()` çağırıyor (mevcut `PlayerInteract.ResetChain` yanına, `d4b26ae3`). 37/37 EditMode + console clean. **Riskli kör UI feature eklemekten kaçındım** (protokol: don't break things) — coin HUD görsel ince ayarı + yeni şekil/level authoring'i Kaan'a bırakıldı (Kenar prefab sürükle-bırak hazır). **Sıradaki Kaan için:** coin HUD konum/boyut onayı; yeni şekiller (emoji/yıldız) Kenar prefab'ıyla; per-edge duvar sanatı (şu an level başına EndWallParts'lar kenarlara best-effort atandı, görsel iyileştirilebilir).

**Unity bağlantısı:** MCP for Unity bridge (CoplayDev) `Packages/manifest.json`'da kurulu. **Port oturumdan oturuma değişir** (bu session 6600 — önce 6401 idi); `mcpforunity://instances`'tan instance doğrula. Instance `DrawRush@4ff3b85c`, Unity 6000.3.12f1. ⚠️ Aynı anda başka projeler de açık olabiliyor (OrbRecall, Mini Fantasy Defense) — `set_active_instance` ŞART.

---

## ✅ Tamamlanan Fazlar

| Faz | İçerik | Commit (master HEAD'inden ucu) |
|---|---|---|
| 0 | Safety baseline (backup branch, claude-dev oluşturma) | `d46bb015` chore: bootstrap Claude memory system |
| 1 | Repo hijyen — Unity-standard `.gitignore`, Git LFS `.gitattributes`, Library/obj/Logs/csproj untrack | dahil `d46bb015` |
| 2 | Dead code — `OldVersion/` 3 script, ekstra `.sln`'ler, Scenes/Test silindi | dahil |
| 3 | Feature folder migration — 14 script `Assets/_Project/<Feature>/Scripts/` altına .meta'larla taşındı (GUID korundu) | dahil |
| 4 | Code refactor — `Studios208.DrawRush.*` namespace, `GameServices` static locator, `GameConfig`/`GameState`/`PlayerHealth` SO'lar, `AnimatorIds`, event-driven win | dahil |
| 5 | asmdef — tek `Studios208.DrawRush.asmdef` (+ Tests asmdef) | dahil |
| 6 | Build settings — `applicationIdentifier=com.Studios208.DrawAndRush2`, AndroidTargetSdk 34, IL2CPP | dahil |
| 7 | README rebuild — Unity 6 + mimari diyagram + mekanik | dahil |
| 8 | Sahne entegrasyonu — Level 1'e `__Bootstrap` GO eklendi, GameConfig/GameState/PlayerHealth bağlandı | dahil |
| 9 | Prefab adaptasyonu — `Player.prefab` + `GameManager` SerializeField yeniden bind | dahil |
| 10 | EditMode test setup — Tests/EditMode/ + asmdef, 22 başlangıç test | dahil |
| 11 | Senior dev review (Unity + general SE) → uygulandı | `f2e96413` ve öncesi |
| 12 | Player speed +%80 (`GameConfig.playerSpeed` 1.5 → 2.7) | dahil |
| 13 | EDM4U temizliği — eski 1.2.135 + 1.2.144 silindi, modern 1.2.169 kaldı | dahil |
| 14 | **Chain-drawing mekaniği** — Trail prefab uçuş atıldı, persistent player trail, closed-loop chain | `0691eddd` |
| 15 | Combat-puzzle ayrımı — DrawArea içindeyken `EnemyCombat` damage yok, `EnemyFollow.ResetPath` win'de | dahil |
| 16 | `LevelFlow.NextLevel` out-of-bounds → `LoadRandomLevel` fallback | dahil |
| 17 | Player Trail prefab eklendi (cyan gradient TrailRenderer) + line material fallback | `4c6e3771` |
| 18 | Trail yerde + edge-by-edge clear — `alignment=TransformZ`, parent X=90°, Y=0.05, time=4s, `Clear()` her anchor temasında | `0ef06683` |
| 19 | **Anchor görsel swap** — DrawPoint cylinder→sphere + transparent material, 4 sahnede 28 hex/şekil görseli temizlenip sphere instance'ları konuldu. EndWallParts + DrawableArea korundu, anchor sayısı invariant (8/6/12/2). | `4ab852b3..d9a9ab17` |
| 20 | **Anchor dedup** — paylaşılan köşelerde çakışan kürelerin clustering (threshold 2.0u) ile teke indirilmesi, küre y'si 0.35 (yere oturur). Sphere sayıları yarıya indi: L1 8→4, L2 6→3, L3 12→6, Tut 2→2. | `870d89a6..136b8537` |
| 21 | **Neighbor-restricted chain** — `DrawPartNeighborGraph` pure helper + `DrawPart.IsNeighborOf` API; `PlayerInteract` mid-chain ve closure check'leri eklendi; auto-wire en yakın 2 komşu (Awake). Köşegen jump'lar reject. 5 EditMode test eklendi. | `f19450f0` |
| 22 | **Mega-scene** — tüm level'lar tek `Game.unity`'de grup grup; `LevelManager.ActivateLevel` switcher + state reset; `PlayerInteract.ResetChain`; `LevelFlow` LoadScene yerine LevelManager delegate; DrawPart neighbor + WallManager watcher level-group scope'una çekildi; build = Splash + Game. | `8348d330..750b23ca` |
| 23 | **Sahne reorganizasyonu** (Kaan) — `Game.unity`→`01_DrawRushGame.unity`, `SplashScreen`→`00_SplashScreen`, eski Level'lar→`Scenes/Old/`. Build = 00_+01_. Git rename ile GUID korundu. | `942e3ffb` |

---

## 🚀 Sıradaki Adım

**Manuel iş (Kaan, Unity Editor'da yapacak):**
- [ ] **`01_DrawRushGame.unity`'yi Play'le test et (mega-scene):** 00_SplashScreen → 01_DrawRushGame akışı; Level_01 açılışta görünüyor mu; `LevelManager.ActivateLevel`/NextLevel/Restart ile level geçişinde Player spawn'a gidiyor + health/win/chain/trail resetleniyor mu; her level'da köşegen-yasak + EndWallParts win reveal çalışıyor mu; sadece aktif level görünüyor mu (inactive'ler gizli). Başlangıç level'ı şu an Level_01 — Tutorial (Level_00) ile başlatmak istersen LevelManager'a Start'ta `ActivateLevel(0)` eklenebilir.
- [ ] (Opsiyonel) Scene view'da level'ları yan yana görmek istersen offset eklenebilir; şu an üst üste ama inactive'ler görünmediği için düzenlemede sorun değil.
- [ ] **Yeni dedup+neighbor-restricted 4 sahneyi Play'le test et** (Level 1 → 2 → 3 → Tutorial):
  - Her köşede TEK küre görüyor musun? (paylaşılan köşeler artık tek nokta)
  - 1. küreye değ → glow halo + trail Clear?
  - Köşegen denemesi: bir köşeye değ, sonra **karşı** köşeye git → bağlantı olmamalı (silent reject). Sadece komşu köşeye değince çizgi spawn olmalı.
  - Sırayla poligon kenarlarından dolaş → her arada kalıcı LineRenderer + trail per-kenar clear?
  - Closed loop kapanışı → EndWallParts Animator reveal (asıl risk noktası — sweep sırasında kardeş GameObject'ler silindi)?
  - Tutorial 2-anchor loop (1→2→1) çalışıyor mu?
- [ ] Küre boyutu/transparency tatmin edici mi? Scale 0.70 + alpha 0.45 → değişiklik DrawPoint.prefab + DrawPointMat tek edit.
- [ ] Cinemachine 2→3 upgrade sonrası Player prefab CMVcam2 framing kontrolü (küre yüksekliği değişti).

**✅ ÇİZİM MEKANİĞİ YENİDEN YAZILDI (edge-painting) — 7 adım tamam, claude-dev'de, Play smoke geçti:**
- Rail/chain sistemi (snap + tek-kenar kilit + closed-loop) defalarca iterasyona rağmen sorun çıkardı: çift-çizme, kitlenme, tek-yön, köşe takılma. Kaan "sıfırdan yaz" dedi.
- **Yeni model (UYGULANDI):** **Edge-painting.** İki küre = bir kenar. Küreye değince o kenar boyunca **ray** + geçtiği yeri **boyar** (kalıcı). Düşman değince ray'den çıkar (serbest=kaçış), boya izi kalır. Kenar **iki uçtan ayrı boyanıp ortada birleşince** complete (kısmi kalıcı). Tüm kenarlar → win.
- **Çalışıyor (Play doğrulandı):** mega-scene Play → Tutorial 1 edge kuruyor; `DrawEdge.PaintFrom`→`Completed`→`EdgeNetwork.AllCompleted`→`WallManager`→`GameState.IsGameWon`→player dance pozu; boya izi yerde cyan LineRenderer olarak render. Script hatası yok (sadece önceden var olan "BoxCollider negative scale" çevre uyarıları).
- **Onaylı çizim davranışı (Kaan 3-soru ile netleştirdi):** (1) Kenar bitip raydan çıkınca sıradaki kenar **tekrar küreye değerek** başlar (kenarlar arası serbest). (2) Kenar **karşı uca varınca VEYA iki yarı ortada birleşince** tamamlanır. (3) **Başlangıç küresine** geri kayınca ray bırakılmaz → o köşeden **farklı kenar seçilir**; ray yalnızca **KARŞI uçta** (ya da completion/enemy'de) bırakılır.
- **Kaan'ın gerçek-cihaz testinde çıkan 2 bug (düzeltildi):**
  - `c94daaab` — **çift çizme:** chain'den kalan cyan TrailRenderer fill çizgisinin üzerine ikinci çizgi bırakıyordu. PlayerInteract'ten trail mantığı çıkarıldı + Player prefab'ında Trail GO disable. Tek görsel = DrawEdgeView fill.
  - `22c87e3a` — **karşı küreye varınca raya kilitlenme:** `OnPartTouched` hedef küreye varınca yeniden anchor atıp kilitliyordu, kenar bitmiyordu. Artık karşı uca varış kenarı doldurup **Detach** eder (serbest); son kenarsa win. İki yarı ortada birleşince de complete+free (FixedUpdate IsComplete kontrolü). Reflection ile tam bug senaryosu Play'de doğrulandı (coverage 1.0, MovementLocked=false, IsGameWon=true).
  - `95319286` — **duvar reveal animasyonu geri geldi:** `PartManager` eskiden DrawPart.Complete() (artık çağrılmıyor) → DrawPartCompletionWatcher ile `EndWallParts.SetActive(true)` yapıp "End Wall" Animator'ını oynatıyordu. Edge-painting'de koptu. PartManager artık level EdgeNetwork'üne bağlı: kendi anchor'larına ait edge'ler (iki ucu da kendi child'ı olan) tamamlanınca duvarı aktive eder. EdgeNetwork'e per-edge `EdgeCompleted` event'i eklendi. Play'de doğrulandı (EndWallParts activeSelf=true + Animator oynuyor). Çok-part'lı level'larda her part kendi duvarını kendi edge'leri bitince açar.
- **⏳ Kaan'a kalan (elle Play feel testi):** square/triangle/hex level'larda akış; düşman Detach=kaçış→re-attach; boya çizgisi rengi/kalınlığı/yüksekliği (DrawEdgeView: color, width→GameConfig.lineWidth fallback, lineY=0.02); RailPaintController tuning (inputDeadzone 0.3, selectThreshold 0.4, railSpeed=0→Config); köşede stick titrekliğinde iki kenara az az bulaşma olursa selection histerezisi eklenebilir.
- **Tam plan:** `~/.claude/plans/unified-bouncing-lark.md` (edge-painting). Dosyalar: yeni `DrawEdge` + `EdgeNetwork` + `RailPaintController`; `PlayerInteract` chain kaldırılır; `WallManager`→EdgeNetwork; `EnemyCombat`→Detach; chain testleri edge fill testleriyle değişir. Korunan: DrawPart + DrawPartNeighborGraph + ThirdPersonMovement + mega-scene.
- **İmplementasyon ilerlemesi (7 adım):**
  - ✅ **Adım 1/7 (`d8ebccf3`):** `EdgeFill.cs` — pure paint-progress (PaintedLow/High, PaintFromA/B, IsComplete, Coverage). Plan'daki "DrawEdge" pure logic kısmı; görsel ertelendi. 7 EditMode test.
  - ✅ **Adım 2/7 (`0d4c0587`):** `EdgeNetwork.cs` (MonoBehaviour) + `DrawEdge.cs` (runtime edge: A/B + EdgeFill + Completed-once + Changed + PaintFrom/Contains/Other/PointAt) + `DrawPartNeighborGraph.BuildUndirectedPairs` (pure pairing). EdgeNetwork OnEnable'da active-scoped DrawPart tarar, Neighbors→adjacency→benzersiz edge, tüm edge complete→`AllCompleted`. 10 yeni EditMode test (5 pairing + 5 DrawEdge).
  - ✅ **Adım 3/7 (`acf6640f`):** `RailPaintController.cs` (eski `RailDrawController` silindi) — küreye değ→`PlayerInteract.PartTouched`→attach; input yönüne en hizalı komşu edge seç; kenarda localT ray + `edge.PaintFrom(fromEnd, edgeT)`; far küreye varınca re-anchor; `Detach()` enemy escape. `PlayerInteract` sadeleşti (chain/closed-loop/SpawnConnectionLine atıldı; sadece IsInDrawArea + CurrentPart + PartTouched + trail + ResetChain). Refs Awake'te GetComponent ile auto-resolve.
  - ✅ **Adım 3b/7 (`c010d793`):** `DrawEdgeView.cs` — edge başına 2 LineRenderer (low/high span), `DrawEdge.Changed`'e abone (per-frame poll yok). EdgeNetwork edge başına view spawn/teardown eder; `fillMaterial` SerializeField (boşsa Sprites/Default fallback).
  - ✅ **Adım 4/7 (`c211008a`):** `WallManager` → `EdgeNetwork.AllCompleted`'a abone → `GameState.IsGameWon`. DrawPartCompletionWatcher yolu bırakıldı (sınıf+testleri **silinmedi**, atıl duruyor — bkz. "bensiz kararlar").
  - ✅ **Adım 5/7 (`0d8ac0fd`):** `EnemyCombat` temasta `RailPaintController.Detach()` çağırır (safe-zone hasar early-return'ünden ÖNCE → temas her zaman ray'i bırakır; hasar hâlâ DrawArea dışında).
  - ✅ **Adım 6/7 (`d5b84a9f`):** Player.prefab → dangling RailDrawController (missing) atıldı + RailPaintController eklendi (headless LoadPrefabContents). Mega-scene → 4 level grubunun WallManager GO'suna EdgeNetwork eklendi (WallManager GetComponent ile auto-bind). Sahne kaydedildi.
  - ✅ **Adım 7/7 (`9ed32380`):** Play smoke + **sıralama bug fix** — EdgeNetwork.OnEnable, DrawPart.Awake komşuları wire'lamadan çalışabiliyordu → 0 edge. `DrawPart.EnsureNeighborsWired()` (idempotent, public) eklendi; EdgeNetwork Neighbors okumadan önce çağırır. Play'de doğrulandı.
- **Mevcut durum:** Edge-painting çalışır halde, claude-dev'de. **Master'a merge edilmedi, push edilmedi** (Kaan onayı + elle Play testi bekliyor). Atıl chain dosyaları **temizlendi** (`fec8b2a9`→`9812e90d`): DrawPartStateMachine/DrawPartCompletionWatcher/DrawingPhase/IDrawPart/IInteractable silindi, DrawPart yalın anchor (sadece Neighbors/IsNeighborOf/EnsureNeighborsWired + enter/exit highlight). `PartManager` KORUNDU (duvar reveal'i sürüyor, EdgeNetwork.EdgeCompleted'a bağlı).

---

**[ESKİ — geçersiz olacak] Rail-based drawing (`9b9fb056`):**
- Yeni `RailDrawController` (Player'da, 01_DrawRushGame). Çizim DIŞINDA serbest hareket korunur (kaçış).
- `PlayerInteract.ChainStarted` → `ThirdPersonMovement.MovementLocked=true`, rail devreye girer. Input yönüne en hizalı komşu kenar seçilir (`DrawPart.Neighbors`), o kenarda ileri-geri kayılır. Köşeye varış PlayerInteract'ın mevcut collision+neighbor-gated chain'iyle (line spawn) işlenir — RailDrawController sadece hareketi şekillendirir. Köşeye geri kayınca kenar bırakılır. `ChainEnded` → serbest moda dön.
- Wiring: PlayerInteract events + `CurrentAnchor`; ThirdPersonMovement `MoveInput`+`MovementLocked` (gravity sürer); DrawPart `Neighbors` accessor.
- **Açık uçlar (Play test sonrası):** köşeye tam "snap" eklenmedi (CharacterController.Move ile yaklaşık); tuning alanları (inputDeadzone 0.3, selectThreshold 0.4, railSpeed=Config fallback, cancelRadius 0.2) Inspector'da. Trail rail boyunca otomatik (PlayerInteract trail mantığı değişmedi).
- claude-dev'de commit'li, **main'e push Kaan onayı bekliyor**.

**Kod tarafı (sonraki sessions):**
- [ ] Tutorial overlay (TutorialLevel.unity'ye "swipe to move + touch points to connect" UI hint).
- [ ] `LevelConfig` ScriptableObject — per-level enemy count/speed/HP/spawn positions (difficulty curve).
- [ ] On-Screen Stick component (mobile virtual joystick) — Player Canvas'a ekle, PlayerControls'a bind.
- [ ] `AudioCue` SO + `AudioService` locator — death / win / connection / enemy-touch SFX. Hâlihazırda audio hiç yok.
- [ ] `LevelProgressState` SO — `GameManager.RandomLevelList` static mutable list'i SO'ya taşı.
- [ ] `WallManager` rename → `WinCondition` (notlar dosyada, scene-ref koruma için ertelendi).

**Sonraki faza ertelenen:**
- Addressables migrasyonu (Resources/ minimal, prematüre).
- VContainer DI (scope büyürse).
- PlayMode test'leri (gerçek physics + scene loading davranışı için).

---

## ⚙️ Yeni Çizim Mekaniği Spec

```
1. Player DrawArea trigger'ına girer  →  TrailRenderer.emitting = true
2. 1. DrawPoint'e değer
   ├─ DrawPart.OnPlayerEntered() + Interact() → DrawingPhase.Armed
   ├─ _firstPart = _previousPart = this
   └─ TrailRenderer.Clear()  (kenar başlangıcı temiz)
3. Player yürür → TrailRenderer yere cyan iz bırakır (4s time, ground-aligned)
4. 2. DrawPoint'e değer
   ├─ SpawnConnectionLine(prev → current)  → KALICI LineRenderer GameObject
   ├─ prev.Complete() → Done phase, Completed event raise
   ├─ current.OnPlayerEntered() + Interact() → Armed
   ├─ _previousPart = current  (chain devam)
   └─ TrailRenderer.Clear()  (sıradaki kenar için sıfırdan)
5. ... N noktaya kadar tekrar
6. Son nokta → 1. nokta (closed-loop closure)
   ├─ SpawnConnectionLine(prev → _firstPart)
   ├─ prev.Complete() + _firstPart.Complete()
   ├─ TrailRenderer.Clear()
   └─ Tüm parçalar Done → WallManager → GameState.IsGameWon = true
7. DrawArea'dan çıkış  →  TrailRenderer.emitting = false (chain progress KORUNUR)
   `resetProgressOnAreaExit` flag açılırsa legacy davranış (progress wipe).
```

**Mimari prensip:** TrailRenderer = "şu an çizilen kenarın geçici göstergesi", LineRenderer GameObject'leri = "kalıcı puzzle çizgileri". Bunlar farklı yaşam döngülerine sahip.

---

## 📋 Proje Özeti

| Özellik | Değer |
|---|---|
| **İsim** | DrawRush (productName: `DrawRush`, paket `com.ekimozgames.drawrush`) |
| **Tür** | Hyper-casual mobile drawing puzzle + chase combat |
| **Platform** | Android öncelik (mobile portrait), PC fallback |
| **Engine / Stack** | Unity 6000.3.12f1 LTS + URP 17.3.0 |
| **Stüdyo** | Ekimoz Games |
| **Mevcut version** | 0.27 (`v0.27-authored-edges` tag) |
| **Repo** | https://github.com/KaanEkimoz/DrawRush-HyperCasual (public, 39★, 6 fork) |
| **Default branch** | `master` |
| **Claude branch** | `claude-dev` |

---

## 🛠️ Teknik Stack

- **Render Pipeline:** URP 17.3.0
- **Input:** Input System 1.19.0 (`PlayerControls` C# class, New Input System)
- **Camera:** Cinemachine **3.1.6** (Kaan upgrade etti — 2.10.7 → 3.1.6, scene tarafı henüz validate edilmedi)
- **AI:** AI Navigation 2.0.11 (NavMeshAgent — `EnemyFollow.cs`)
- **Test:** Unity Test Framework 1.6.0 (EditMode 35 test green)
- **VFX:** Visual Effect Graph 17.3.0 (Kaan ekledi, kullanım örneği henüz yok)
- **Geometry:** ProBuilder 6.0.9 (Kaan ekledi, kullanım örneği henüz yok)
- **MCP:** com.coplaydev.unity-mcp (GitHub URL, MCPForUnity#main) — bridge v9.6.6, port 6401
- **External servisler (`Assets/!OtherAssets/`):** AppsFlyer, GameAnalytics, FacebookSDK, ExternalDependencyManager 1.2.169 (eski 1.2.135 + 1.2.144 purge edildi).

---

## 📦 Asset Envanteri

- **Scripts:** 22 first-party (`Assets/_Project/<Feature>/Scripts/`) + `PlayerControls.cs` (auto-generated)
- **Tests:** 35 EditMode tests in `Assets/_Project/Tests/EditMode/`
- **Asmdef:** `DrawRush` + `DrawRush.Tests.EditMode` (2026-07-17'de `Studios208.*`'tan taşındı; aşağıdaki Faz tablosu o günkü isimleri tarihî olarak korur)
- **ScriptableObject asset'ler:**
  - `Assets/_Project/Core/Data/GameConfig.asset` — playerSpeed=2.7, enemyTouchDamage=1, lineWidth=0.4, etc.
  - `Assets/_Project/Core/Data/GameState.asset` — IsGameWon flag
  - `Assets/_Project/Player/Data/PlayerHealth.asset` — startingValue=3
  - Event channels: `Assets/_Project/Core/Events/` (VoidEventChannel, IntEventChannel — test'lerde kullanılıyor, scene'de henüz wire edilmedi)
- **Yeni prefab:** `Assets/_Project/Drawing/Prefabs/DrawPoint.prefab` (yere düz nokta, cylinder + emissive cyan, DrawPart component)
- **Sahneler:** 5 build'de (SplashScreen, TutorialLevel, Level 1-3); 5 test sahnesi silindi.

---

## 🗒️ Notlar

- `GameManager` legacy facade olarak korundu (UI Button OnClick bindings) — gerçek iş `LevelFlow` + `HudPanels` + `WinSequenceDirector` sub-component'lerinde.
- `DontDestroyOnLoad` → `PersistentObject` rename edildi (`MovedFrom` attribute ile scene refs intact).
- `GameConfig` public field → `[field: SerializeField]` read-only property pattern'i uygulandı.
- `[Obsolete] DrawPart.isPlayerEntered` getter sahneler/prefab'lar için bırakıldı; yeni kod `OnPlayerEntered/OnPlayerExited` kullansın.
- Architecture review raporu: `Claude-Project-Memory-Template/reviews/2026-05-16-architecture-review.md` (senior software eng) — uygulanan kararların kaynağı.
- "DrawArea içinde enemy damage yok" safe zone kuralı `EnemyCombat.OnTriggerEnter`'da `PlayerInteract.IsInDrawArea` check ile.
