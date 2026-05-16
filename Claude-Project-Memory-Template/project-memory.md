# Proje Hafızası — DrawRush (DrawAndRush2)

> Projenin **canlı durumu**. Her önemli ilerlemeden sonra güncellenir.
> Yeni oturumda buraya bakıp "nerede kaldık" sorusuna cevap alabilmelisin.

---

## 🎯 Mevcut Durum — 2026-05-16 (chain-drawing milestone)

**Tek satır:** Çizim mekaniği baştan aşağı yeniden yazıldı — eski "DrawPart trail spawn'lar + uçar" modeli atıldı, player'da yere yatık (XZ düzleminde) persistent `TrailRenderer` ile **chain-anchor** modeli yapıldı, iki anchor arası `LineRenderer` ile **kalıcı kenar** çiziliyor ve trail her kenar tamamlandığında temizlenip sıradaki kenar için sıfırdan başlıyor. Closed-loop puzzle: 1 → 2 → 3 → … → 1.

**Repo state:** `master` HEAD = `3d5cd386` ("Merge claude-dev: ground-aligned trail + edge-by-edge clear"), tag `v0.24-chain-drawing` master'da, origin'e push edildi. `claude-dev` master'la merge edildi, 2 commit önde olduğu görünüyor sadece merge-base divergence sebebiyle (içerik aynı). Working tree clean. LFS aktif. Backup branch `backup-pre-claude-cleanup-1778895100` korunuyor.

**Test:** EditMode 35/35 PASS (PlayerHealth 11 + GameState 4 + GameServices 4 + EventChannel 3 + DrawPartStateMachine 8 + DrawPartCompletionWatcher 5). `TrailMath` testleri silindi (trail-lerp logic'i ile birlikte gitti).

**Unity bağlantısı:** MCP for Unity bridge v9.6.6 (CoplayDev) `Packages/manifest.json`'da kurulu, port 6401, instance `DrawRush@4ff3b85c`, Unity 6000.3.12f1, 66-80 paket (kullanıcı ProBuilder + VFX Graph eklemiş).

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

---

## 🚀 Sıradaki Adım

**Manuel iş (Kaan, Unity Editor'da yapacak):**
- [ ] Level 1'de Play tuşuna basıp yeni chain-drawing mekaniğini test et — golden path:
  - DrawArea'ya gir → cyan trail yerde görünüyor mu?
  - 1. nokta'ya değ → glow / trail Clear → trail sıfırdan başlıyor mu?
  - 2., 3., … noktaya sırayla → her birinde aralarına kalıcı çizgi spawn, trail clear?
  - Son nokta → 1. nokta'ya geri dön → closed loop kapanışı + wall reveal + win sequence?
  - DrawArea'dan çık → trail görünmez, geri gir → chain progress duruyor mu?
- [ ] Mevcut DrawPart instance'larını (Levels/Wall*.prefab içindeki köşe parçaları) yeni `Assets/_Project/Drawing/Prefabs/DrawPoint.prefab` ile değiştir — yere düz nokta görselleri. Test 1: tek bir Level'da swap, beğenirse yay.
- [ ] Cinemachine 2 → 3 upgrade'inden sonra Player prefab'ındaki CMVcam2 hâlâ doğru framing'de mi kontrol et.

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
