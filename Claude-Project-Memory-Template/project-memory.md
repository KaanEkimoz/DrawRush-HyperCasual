# Proje Hafızası — DrawRush (DrawAndRush2)

> Projenin **canlı durumu**. Her önemli ilerlemeden sonra güncellenir.
> Yeni oturumda buraya bakıp "nerede kaldık" sorusuna cevap alabilmelisin.

---

## 🎯 Mevcut Durum — 2026-05-16 (mega-scene mimarisi)

**Tek satır:** Scene-per-level mimarisi **tek `Game.unity` mega-sahneye** dönüştürüldü. `===SHARED===` (Player, Camera+vcam, Light, GeneralCanvas, GameManager, __Bootstrap, LevelManager) bir kez yaşıyor; `===LEVELS===` altında `Level_00_Tutorial`/`Level_01`/`Level_02`/`Level_03` grupları sadece level-specific içeriği (Enviroment, enemy, WallManager) taşıyor. Yeni `LevelManager.ActivateLevel(i)` tek grubu enable eder + state reset (health/win/chain/spawn). Önceki fazlar: anchor görsel swap → dedup (her köşede tek küre: L1=4, L2=3, L3=6, Tut=2) → köşegen-yasak chain (`DrawPart.IsNeighborOf`, en yakın 2 komşu, **artık level-group scope'unda**).

**Repo state:** `claude-dev` HEAD = `750b23ca` (build settings). Master HEAD hâlâ `236d83c9` (v0.24 handoff). claude-dev master'dan **18 commit önde**, push edilmedi, main'e merge Kaan onayı bekler. Working tree: DrawPointMat kendiliğinden dirty olabiliyor (Unity material re-serialize, restore edilir); Level 2 LightingData/ReflectionProbe artifact'ları untracked (gitignore ayrı iş). Eski Level/Tutorial sahneleri build'den çıktı ama diske duruyor (rollback). LFS aktif.

**Test:** EditMode 42/42 PASS (LevelManager/LevelFlow refactor data+kod, mevcut suite green; LevelManager için test eklenmedi — MonoBehaviour/scene-bağımlı).

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

---

## 🚀 Sıradaki Adım

**Manuel iş (Kaan, Unity Editor'da yapacak):**
- [ ] **Game.unity'yi Play'le test et (mega-scene):** Splash → Game akışı; Level_01 açılışta görünüyor mu; `LevelManager.ActivateLevel`/NextLevel/Restart ile level geçişinde Player spawn'a gidiyor + health/win/chain/trail resetleniyor mu; her level'da köşegen-yasak + EndWallParts win reveal çalışıyor mu; sadece aktif level görünüyor mu (inactive'ler gizli). Başlangıç level'ı şu an Level_01 — Tutorial (Level_00) ile başlatmak istersen LevelManager'a Start'ta `ActivateLevel(0)` eklenebilir.
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
| **Mevcut version** | 0.24 (`v0.24-chain-drawing` tag) |
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
