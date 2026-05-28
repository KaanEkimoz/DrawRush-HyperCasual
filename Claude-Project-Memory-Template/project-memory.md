# Proje Hafızası — DrawRush (DrawAndRush2)

> Projenin **canlı durumu**. Her önemli ilerlemeden sonra güncellenir.
> Yeni oturumda buraya bakıp "nerede kaldık" sorusuna cevap alabilmelisin.

---

## 🎯 Mevcut Durum — 2026-05-28 (authored-edge sistemi, otonom oturum)

**Tek satır (2026-05-28):** Çizim artık **authored edge** modeli. Köşe küreleri (DrawPoint) paylaşılır; her kenar bir **`Kenar` prefab'ı** (`DrawEdgeAuthor`: anchorA/anchorB + wallSegment + DrawEdgeView). `EdgeNetwork` sahnedeki Kenar'ları toplar (komşu hesabı YOK), kenar boyanınca **kendi duvar parçasını reveal** eder (per-edge), hepsi bitince win. `RailPaintController` edge'leri `EdgeNetwork.GetEdgesTouching`'den seçer. **4 level de bu sisteme çevrildi** (Tutorial 1, Level_01 4, Level_02 üçgen 3, Level_03 altıgen 6 edge). Level kurma akışı: Kenar prefab'ını sürükle → anchorA/B + wallSegment ata. **Ödül:** `PlayerProgress.Coins` (PlayerPrefs) kazanınca +10, `CoinHud` HUD'da gösterir (I_Coins üst-sol, placement Kaan'a göster-onayla). **Lose-flow düzeltildi:** GameManager.playerHealth wire edildi (ölünce LosePanel + pause) + PlayerCombat artık player'ı Destroy etmiyor (Restart resetler). Ölü komşu kodu (DrawPartNeighborGraph + DrawPart.Neighbors) tamamen silindi; DrawPart yalın anchor.

**Repo state:** `claude-dev` = otonom oturum işi (`014542cf`→`1a0565c7`); milestone master'a merge+tag+push edilecek (**`v0.27-authored-edges`**). Önceki milestone `v0.26-edge-painting` master'da. Build sahneleri: `00_SplashScreen.unity` + `01_DrawRushGame.unity`. Kenar prefab: `Assets/_Project/Drawing/Prefabs/Kenar.prefab`. Part prefab'ları (`Assets/Prefabs/New/{Square,Triangle,Altigen}Part.prefab`) eski PartManager'dan temizlendi (PartManager SİLİNDİ). Working tree: DrawPointMat/GvhProjectSettings kendiliğinden dirty olabilir (zararsız, restore et). ⚠️ Overlay UI (GeneralCanvas Screen Space Overlay) Main-Camera screenshot'unda GÖRÜNMEZ — UI'ı görmek için geçici Screen Space Camera'ya al.

**[ESKİ] 2026-05-16 mega-scene:** Scene-per-level mimarisi **tek `Game.unity` mega-sahneye** dönüştürüldü. `===SHARED===` (Player, Camera+vcam, Light, GeneralCanvas, GameManager, __Bootstrap, LevelManager) bir kez yaşıyor; `===LEVELS===` altında `Level_00_Tutorial`/`Level_01`/`Level_02`/`Level_03` grupları sadece level-specific içeriği (Enviroment, enemy, WallManager) taşıyor. Yeni `LevelManager.ActivateLevel(i)` tek grubu enable eder + state reset (health/win/chain/spawn). Önceki fazlar: anchor görsel swap → dedup (her köşede tek küre: L1=4, L2=3, L3=6, Tut=2) → köşegen-yasak chain (`DrawPart.IsNeighborOf`, en yakın 2 komşu, **artık level-group scope'unda**).

**Repo state:** `master` = `claude-dev` = edge-painting tamamı + temizlik + tutorial-skip, **origin'e push'lu** (Kaan "her şeyi maine pushla" dedi → `--no-ff` merge + tag + push yapıldı). Tag **`v0.26-edge-painting`**. İçerik: edge-painting step 1–7 + cihaz-testi fix'leri (`c94daaab` çift-çizme trail, `22c87e3a` karşı-küre completion+free, `95319286` duvar reveal anim, `e0d30325` restart dans/lock reset, `ce690b7e` Animator Dance→Run, `060d1a3e` win/lose buton onClick → sahne GameManager, `18204f24` restart ActivateLevel disable→enable, `fb5b583b` boya çizgisi = duvar rengi PartManager.GetFillColor, `74896304` reveal'da iz gizlenir, `07773c61`+`2f2b70c8` EDM4U sessiz + `[InitializeOnLoad]` enforcer her load'da zorlar). **Kod borcu temizliği (`fec8b2a9`→`ae8fa03c`):** DrawPartCompletionWatcher/DrawPartStateMachine/DrawingPhase/IDrawPart/IInteractable silindi, DrawPart yalın anchor, ölü GameConfig alanları kaldırıldı, **WallManager→WinCondition** rename. **Tutorial-skip (`fcb4b202`):** `PlayerProgress.TutorialCompleted` (PlayerPrefs) — tutorial geçilince flag set, başlangıçta flag varsa Level_01 yoksa tutorial; `LevelManager.startLevelIndex=-1` (auto, dev-override). Build sahneleri: `00_SplashScreen.unity` + `01_DrawRushGame.unity` (mega-sahne); eski `Level 1/2/3` + `TutorialLevel` → `Assets/Scenes/Old/` arşivi. Working tree temiz. EdgeFill core (`d8ebccf3`) push'lu. DrawPointMat/GvhProjectSettings sahne yüklemede kendiliğinden dirty olabilir (restore/commit zararsız). LFS aktif. Backup `backup-pre-claude-cleanup-1778895100`. ⚠️ DERS: branch checkout sırasında Unity açıksa `git checkout -- <path>` el değişikliklerini ezebilir → önce `git status`.

**Test:** EditMode 37/37 PASS (EdgeFill/DrawEdge/GameState/GameServices/PlayerHealth/EventChannel + yeni PlayerProgressTests coin'ler). EdgeNetwork/WinCondition/RailPaintController/CoinHud için EditMode test yok — MonoBehaviour/scene-bağımlı, hepsi Play'de doğrulandı (4 level edge + win + lose + restart + coins + level etiketi).

**Otonom oturum (2026-05-28 03:44→) bensiz alınan kararlar:** (1) Authored-edge mimarisini Kaan'ın 3-soru cevabına göre kurdum; duvar parçası Kenar prefab'ının `wallSegment`'ine konuldu. (2) **Lose-flow bug fix (`5bfccf24`):** GameManager.playerHealth wire'sızdı (LosePanel hiç açılmıyordu) + PlayerCombat ölünce player'ı Destroy ediyordu (mega-scene'de restart'ı kırıyordu) → ikisi de düzeltildi. (3) **Coin ödülü (`1a0565c7`):** PlayerProgress.Coins + CoinHud (üst-sol HUD). **Coin HUD placement'ı overlay olduğu için Main-Camera screenshot'ta görünmedi; canvas'ı geçici Screen Space Camera'ya alıp doğruladım — "10" üst-solda düzgün; yine de Kaan görsel onaylasın.** (4) **Level etiketi fix (`c3305ef1`):** "Level 6" (stale PlayerPrefs) → LevelManager gerçek level'a göre "Tutorial/Level 1/2/3" yazar. (5) Level başına coin 5/10/15/20 (`21170c92`). (6) Master'a v0.27-authored-edges milestone push ettim (Kaan "önceki onayları kabul" + "tam yetki" dedi); sonraki bonus commit'ler de push edilecek. **Riskli kör UI feature eklemekten kaçındım** (protokol: don't break things) — coin HUD görsel ince ayarı + yeni şekil/level authoring'i Kaan'a bırakıldı (Kenar prefab sürükle-bırak hazır).

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
| **İsim** | DrawRush (productName: `DrawAndRush2`) |
| **Tür** | Hyper-casual mobile drawing puzzle + chase combat |
| **Platform** | Android öncelik (mobile portrait), PC fallback |
| **Engine / Stack** | Unity 6000.3.12f1 LTS + URP 17.3.0 |
| **Stüdyo** | Studios208 |
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
- **Asmdef:** `Studios208.DrawRush` + `Studios208.DrawRush.Tests.EditMode`
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
