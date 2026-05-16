# Draw Rush

> **Hyper-casual mobile drawing puzzle** — connect the dots, raise the wall, dodge the chasers.
> A [Studios208](https://github.com/KaanEkimoz) production, originally a 2022 prototype, rebuilt on Unity 6 LTS with a modern Service Locator + ScriptableObject architecture.

![Unity](https://img.shields.io/badge/Unity-6000.3.12f1_LTS-000000?logo=unity)
![URP](https://img.shields.io/badge/Render-URP_17.3-blue)
![Input](https://img.shields.io/badge/Input-New_Input_System-blue)
![Platform](https://img.shields.io/badge/Platform-Android-3DDC84?logo=android)
![Architecture](https://img.shields.io/badge/Arch-IL2CPP_ARM64-orange)
![Version](https://img.shields.io/badge/version-0.24-lightgrey)

https://user-images.githubusercontent.com/79421047/170832980-9d741e53-741d-41fa-9035-098829b40dfb.mp4

---

## Gameplay

You play a small runner on a 3D plane. Inside a drawable area, walking past two **DrawPart** anchors connects them with a `LineRenderer`. Connect all parts in a group → a hidden wall reveals. Connect every group in the scene → the level is won, enemies die mid-step, particles fire, win panel shows. Enemies chase you via NavMesh in the meantime; three hits and you lose.

| Mechanic | How it works |
|---|---|
| **Drawing** | `PlayerInteract` detects `IInteractable` triggers; first hit anchors `_previousPart`, second hit fires `MarkCompleted()` on both parts and spawns a connecting `LineRenderer` that self-destroys after `GameConfig.lineDestroyDelay` |
| **Wall reveal** | `PartManager` subscribes to every child `DrawPart.Completed` event; when all parts in the group are done, the assigned wall `SetActive(true)` |
| **Win condition** | `WallManager` listens to every `DrawPart` in the scene; when all complete, flips `GameState.IsGameWon = true` which raises an event that `GameManager`, `EnemyFollow`, and `ThirdPersonMovement` all react to |
| **Combat** | Enemies trigger-collide with the player tag → `PlayerHealth.Apply(GameConfig.enemyTouchDamage)`. Hits 0 → `Died` event → `Destroy(player)` → `GameManager.Update()` detects missing player → lose panel |
| **Movement** | New Input System (`PlayerControls`) feeds `Vector2 _move`; camera-relative direction computed from `GameServices.MainCamera`; `CharacterController.Move()` with manual gravity |

---

## Architecture

```
Assets/_Project/                      ← all first-party code lives here, scoped by single asmdef
├── Common/Scripts/
│   └── IInteractable.cs              ← cross-feature contract (Drawing ↔ Player)
├── Core/
│   ├── Data/
│   │   ├── GameConfig.asset+cs       ← magic numbers (ScriptableObject)
│   │   └── GameState.asset+cs        ← IsGameWon flag + Action<bool> event
│   ├── Events/
│   │   ├── VoidEventChannel.cs       ← parameterless event channel (SO)
│   │   ├── IntEventChannel.cs        ← int payload channel (SO)
│   │   ├── GameWonChannel.asset
│   │   ├── PlayerDiedChannel.asset
│   │   └── PlayerHealthChangedChannel.asset
│   └── Scripts/
│       ├── GameServices.cs           ← static locator (Player, Camera, Config, State)
│       ├── GameBootstrap.cs          ← per-scene wiring, [DefaultExecutionOrder(-1000)]
│       ├── GameManager.cs            ← UI panels + scene flow + event listener
│       └── DontDestroyOnLoad.cs      ← persistent object marker
├── Drawing/Scripts/
│   ├── DrawPart.cs                   ← IInteractable; raises Completed event
│   ├── PartManager.cs                ← group listener (HashSet, no Update polling)
│   └── WallManager.cs                ← scene-wide listener → flips IsGameWon
├── Player/
│   ├── Data/
│   │   └── PlayerHealth.asset+cs     ← health as SO with Changed/Died events
│   ├── Input/
│   │   ├── PlayerControls.cs         ← Input System auto-gen (kept in asmdef scope)
│   │   └── PlayerControls.inputactions
│   └── Scripts/
│       ├── ThirdPersonMovement.cs    ← New Input + GameServices.MainCamera + GameConfig
│       ├── PlayerInteract.cs         ← drawing trigger logic
│       └── PlayerCombat.cs           ← wires PlayerHealth to UI label + destroys on death
├── Enemy/Scripts/
│   ├── EnemyFollow.cs                ← NavMeshAgent → GameServices.Player
│   └── EnemyCombat.cs                ← OnTriggerEnter → PlayerCombat.TakeDamage
├── UI/Scripts/
│   └── CreateJoystick.cs             ← spawns joystick at touch position
└── Utilities/Scripts/
    ├── RandomMaterial.cs             ← rolls a material at Start
    └── VideoEnd.cs                   ← Awaitable splash delay → next scene
```

### Why this layout?

| Decision | Rationale |
|---|---|
| **Single `Studios208.DrawRush.asmdef`** instead of one per feature | 14 scripts is too small to justify N assemblies. Feature isolation is achieved at the namespace level (`Studios208.DrawRush.<Feature>`). When the project grows past ~50 scripts, split the asmdef by feature. |
| **Service Locator over Singleton** | `GameObject.FindWithTag("Player")` in every Awake() is fragile (additive scene loads, prefab placeholders). `GameServices` resolved **once** in `GameBootstrap.Awake()` and read everywhere else. |
| **ScriptableObject for runtime state** (`GameState`, `PlayerHealth`) | UI labels, enemy damage handlers, save systems all reference the same asset. No "find the manager and read its field" coupling. |
| **Event channels (SO)** for cross-feature signaling | `GameState.IsGameWon` change → `Action<bool>` → all listeners (movement, manager, enemy) react. Replaces 5+ poll-every-frame `if (GameManager.isGameWon)` checks. |
| **`DrawPart.Completed` C# event** | Drawing → PartManager → WallManager chain is now a one-way notification. No external mutation of `isDrawCompleted` field, no Update() foreach. |
| **`_Project/` prefix** | Sorts before `!OtherAssets/` (vendor SDKs) and Unity-generated `Settings/`/`Materials/` in the Project window — first-party code is always at the top. |

---

## Packages

| Unity Package | Version | Purpose |
|---|---|---|
| `com.unity.render-pipelines.universal` | 17.3.0 | URP — mobile-friendly rendering |
| `com.unity.inputsystem` | 1.19.0 | New Input System (`PlayerControls`) |
| `com.unity.ai.navigation` | 2.0.11 | NavMeshAgent for enemy chase |
| `com.unity.cinemachine` | 3.1.6 | Camera (CMCamera prefab) |
| `com.unity.timeline` | 1.8.11 | Timeline (intro / cutscenes) |
| `com.unity.recorder` | 5.1.5 | Video capture for promo clips |
| `com.unity.probuilder` | 6.0.9 | Greybox geometry |
| `com.unity.visualeffectgraph` | 17.3.0 | VFX Graph particle effects |
| `com.unity.test-framework` | 1.6.0 | NUnit + PlayMode tests (folder TBD) |
| `com.coplaydev.unity-mcp` | git/main | AI-assisted Editor automation (dev only) |

**Third-party SDKs** (in `Assets/!OtherAssets/`): AppsFlyer, GameAnalytics, Facebook SDK, ExternalDependencyManager, PlayServicesResolver, Epic Toon FX, CodeMonkey utilities, Toony Colors Pro shaders.

---

## Build settings

| Setting | Value |
|---|---|
| applicationIdentifier | `com.Studios208.DrawAndRush2` |
| AndroidMinSdkVersion | 26 |
| AndroidTargetSdkVersion | 34 (Play Store 2024+ requirement) |
| Scripting Backend | IL2CPP |
| Architectures | ARMv7 + ARM64 (Play Store 64-bit requirement) |
| Managed Stripping | Low |
| bundleVersion | 0.24 |
| bundleVersionCode | 24 |
| Default Orientation | Portrait |
| Render Pipeline | URP |

**Build scenes** (in order): `SplashScreen` → `TutorialLevel` → `Level 1` → `Level 2` → `Level 3`.

---

## Getting started

```bash
git clone https://github.com/KaanEkimoz/DrawRush-HyperCasual.git
cd DrawRush-HyperCasual
git lfs install     # binaries (FBX/PNG/WAV) come through LFS per .gitattributes
git lfs pull
```

Open `Unity 6000.3.12f1` (or later 6000.x LTS) from Unity Hub, point it at the cloned folder, wait for package resolve. Open `Assets/Scenes/SplashScreen.unity` and press Play.

### Optional: Claude Code MCP integration

This repo ships with MCP for Unity (`com.coplaydev.unity-mcp`) in the manifest. When the Editor opens, `Window > MCP For Unity` will appear. Start the bridge → connect from Claude Code or any MCP client. See `Claude-Project-Memory-Template/` for the project's persistent rules/memory system.

---

## Roadmap

- [x] Faz 0 — Safety baseline + Unity 6 upgrade snapshot
- [x] Faz 1 — Repo hygiene (`.gitignore`, `.gitattributes` + LFS, untrack Library/Logs/csproj/sln)
- [x] Faz 2 — Dead code purge (`OldVersion/` A* stub, `Scenes/Test/`, legacy `.sln`)
- [x] Faz 3 — Feature folder migration (flat `Scripts/` → `_Project/<Feature>/Scripts/`)
- [x] Faz 4 — Clean architecture refactor (ServiceLocator, SO state, event-driven)
- [x] Faz 5 — Project-level asmdef (`Studios208.DrawRush`)
- [x] Faz 6 — Build settings (applicationIdentifier, IL2CPP, target SDK 34)
- [x] Faz 7 — README rebuild (this document)
- [ ] Faz 8 — First EditMode tests (DrawPart.Completed firing, PlayerHealth.Apply edges, PartManager event subscription)
- [ ] Faz 9 — Signed Android build pipeline (keystore configured outside repo)
- [ ] Faz 10 — Replace `Resources/` (GameAnalytics Settings) with Addressables

---

## Studio

[Studios208](https://github.com/KaanEkimoz) (a.k.a. 208 Studios) — Kaan Ekimoz. Hyper-casual mobile games on Unity 6 LTS.

## License

License not specified — defaults to "all rights reserved" until one is added. See [Choosing a License](https://choosealicense.com/) if you intend to open-source this fork.
