# Proje Hafızası — DrawRush (DrawAndRush2)

> Projenin **canlı durumu**. Her önemli ilerlemeden sonra güncellenir.
> Yeni oturumda buraya bakıp "nerede kaldık" sorusuna cevap alabilmelisin.

---

## 🎯 Mevcut Durum — 2026-05-16

**Tek satır:** DrawRush 2022'de yapılmış hyper-casual prototip; Unity 2020 → Unity 6 (6000.3.12f1, URP 17.3) upgrade'i sonrası ilk inceleme, refactor/feature-folder migrasyonu ve modern mimariye (ScriptableObject + GameServices, Awaitable, Addressables) taşıma fazına hazırlanılıyor.

**Repo state:** `master` branch HEAD `7877f08d build: deleted aab files`. Working tree çok kirli — Unity 6 upgrade'in ürettiği `.csproj`, `.mat` (URP migrasyon), `Library/` artefaktları staged değil. `refactor` adlı remote branch mevcut (önceki bir refactor denemesi `e9e7dbb7 Merge branch 'refactor'` ile master'a merge edilmiş).

**Aktif fazlar / blocker'lar:**
- ⛔ Working tree temizliği: Unity 6 upgrade kalıntıları (mat / csproj / Assets.meta) ya commit edilmeli ya stash'lenmeli — sonraki commit kirli baseline'a yapılmasın.
- ⛔ `.gitignore` minimal (sadece `Temp/UnityLockfile`) — Unity standardı eksik, Library/Logs/obj git history'sinde olmamalı. Repo disk usage GitHub'da 442 MB.
- ⛔ Hiç `.gitattributes` / LFS yok — FBX/PNG vs. binary'ler repo'yu şişiriyor.
- ⚠️ Hiç `asmdef` yok — full recompile her kod değişikliğinde tetikleniyor.

---

## 🚀 Sıradaki Adım

**Şimdi başlanabilecek:**
- [ ] `git stash` veya WIP commit ile working tree'yi temizle, `claude-dev` branch'ini master'dan oluştur (kuralı uygula).
- [ ] Unity-standard `.gitignore` + `.gitattributes` (LFS) ekle — `Library/`, `Logs/`, `Temp/`, `obj/`, `UserSettings/`, `*.csproj`, `*.sln` exclude.
- [ ] README.md güncel değil (Unity 2020.3.34f1 yazıyor; gerçek 6000.3.12f1) — versiyonu, paketleri, ekran görüntüsünü güncelle.
- [ ] 16 script'i `Assets/Scripts/*.cs` flat layout'tan `Assets/_Project/<Feature>/Scripts/` feature-bazlı yapıya migrate et (Combat / Drawing / Player / Enemy / Level / Core).
- [ ] `OldVersion/Pathfinding.cs` (12 satır, boş constructor) ve `OldVersion/Grid.cs` ölü kod — silinmeli (artık NavMeshAgent kullanılıyor).

**Manuel iş (Kaan):**
- [ ] AppsFlyer / GameAnalytics / FacebookSDK key'lerinin commit'lenmemiş olduğunu Editor'da doğrula (`Settings.asset` Resources altında, key boş olmalı).
- [ ] Android `applicationIdentifier` boş — bundle ID atanmalı (com.Studios208.DrawAndRush2).
- [ ] `AndroidTargetSdkVersion: 0` (Auto) — Play Store 2024+ için sabit 34/35 set edilmeli.

**Sıradaki büyük kalan:**
- Singleton/Find-tag anti-pattern temizliği: `GameObject.FindWithTag("Player")`, `GameObject.FindObjectsOfType<LineRenderer>()` kullanımı → `GameServices` locator + ScriptableObject ref.
- DrawPart `Update()` her frame `foreach` ile counter sayıyor — event-driven yapılmalı.

**Sonraki faza ertelenen:**
- Addressables migrasyonu (Resources/ altında sadece GameAnalytics Settings var, küçük).
- Unit test'ler (henüz yok).
- VContainer DI (scope büyürse).

---

## 📋 Proje Özeti

| Özellik | Değer |
|---|---|
| **İsim** | DrawRush (productName: DrawAndRush2) |
| **Tür** | Hyper-casual mobile drawing puzzle + chase/combat |
| **İlham / referans** | Voodoo "Draw Joust", Lion Studios "Pokey Ball" benzeri çiz-tamamla mekaniği |
| **Hedef Platform** | Android öncelik (mobile portrait), PC fallback |
| **Hedef kitle** | Hyper-casual mobile oyuncu (kısa session, basit kontrol) |
| **Engine / Stack** | Unity 6000.3.12f1 LTS + URP 17.3.0 |
| **Başlangıç** | 2022-04-29 (GitHub createdAt) |
| **Stüdyo** | Studios208 |
| **Mevcut version** | 0.23 |

---

## ⚙️ Sabit Parametreler / Tasarım Kararları

> Mevcut kod tabanından çıkarılan parametreler. Magic number temizliği yapılırken buradan referansla ScriptableObject `GameConfig.asset`'e taşınmalı.

| Parametre | Değer | Kaynak / Bağlam |
|---|---|---|
| Player speed | 1.5f | `ThirdPersonMovement.playerSpeed` |
| Turn smooth time | 0.1f | `ThirdPersonMovement.turnSmoothTime` |
| Gravity | -9.81f | `ThirdPersonMovement.Gravity` |
| Enemy damage | -1 | `EnemyCombat.damage` |
| GameWon delay | 3.0f | `DrawPart.Invoke(nameof(GameWon), 3.0f)` |
| Android Min SDK | 25 | `ProjectSettings.asset` |
| Default orientation | Portrait | `defaultScreenOrientation: 4` |

---

## 🛠️ Teknik Stack

- **Engine / Framework:** Unity 6000.3.12f1 LTS
- **Render Pipeline:** URP 17.3.0 (Universal RP)
- **Input:** Input System 1.19.0 (`PlayerControls` C# class, New Input System)
- **Test framework:** Unity Test Framework 1.6.0 — kurulu ama hiç test yok (Tests/ klasörü yok)
- **Paketler / Bağımlılıklar (önemliler):**
  - `com.unity.ai.navigation` 2.0.11 (NavMeshAgent — EnemyFollow.cs)
  - `com.unity.cinemachine` 2.10.7 (kamera — CMCamera.prefab)
  - `com.unity.render-pipelines.universal` 17.3.0
  - `com.unity.recorder` 5.1.5 (video kayıt — Recorder)
  - `com.unity.timeline` 1.8.11
- **External servisler (SDK'lar `Assets/!OtherAssets/`):**
  - AppsFlyer (attribution)
  - GameAnalytics (analytics, en büyük SDK — 29 MB)
  - FacebookSDK (social)
  - ExternalDependencyManager + PlayServicesResolver (Google Play services)
- **Asset / data kaynakları:**
  - Epic Toon FX (particle library, ~748 KB)
  - CodeMonkey (referans script kitleri, ~340 KB)
  - Toony Colors Pro (cel-shader, `PackageAsset/JMO Assets/`)
  - "beach" pack (39 MB — proje toplamının %33'ü)
  - TutorialInfo (Unity sample assets)
- **Git:** https://github.com/KaanEkimoz/DrawRush-HyperCasual (public, 39 star, 6 fork, MIT yok — license yok)
- **Branch:** `claude-dev` (Claude — henüz oluşturulmadı), `master` (default)

---

## 🗓️ Roadmap

| Faz | Süre | Task Sayısı | Acceptance Criteria | Durum |
|---|---|---|---|---|
| 0. Inventory + Memory bootstrap | <1 gün | 4 | Bu dosya doldu, CLAUDE.md root'ta, working tree durumu raporlandı | ✅ |
| 1. Repo hijyen | 1 gün | 5 | `.gitignore`/`.gitattributes` standardize, claude-dev branch açık, README güncel, Library git'te değil | ⬜ |
| 2. Klasör migrasyonu (feature-based) | 1-2 gün | 6 | Tüm Scripts `Assets/_Project/<Feature>/Scripts/` altında, asmdef'lerle bölünmüş, derleme başarılı | ⬜ |
| 3. Singleton/Find-tag temizliği | 2-3 gün | 8 | GameServices locator + ScriptableObject ref'ler, hiç `FindWithTag`/`FindObjectsOfType` yok | ⬜ |
| 4. Test framework + ilk EditMode test | 1 gün | 3 | NSubstitute kurulu, DrawPart ve PartManager için unit test, 10/10 PASS | ⬜ |
| 5. Build pipeline | 0.5 gün | 2 | Android signed APK output, `applicationIdentifier` + signing config | ⬜ |

---

## 📦 Mevcut Asset / Data Envanteri

| Tip | Sayı | Konum | Not |
|---|---|---|---|
| C# Script | 16 | `Assets/Scripts/` | Flat layout — feature klasörlemesi yok (`OldVersion/` 3, `Interfaces/` 1) |
| Sahne | 10 | `Assets/Scenes/` | 5'i Build'de aktif (Splash + Tutorial + Level 1-3), 5'i `Scenes/Test/` altında |
| Prefab | 20+ | `Assets/Prefabs/` | Objects/, DontDestroyOnLoads/, New/ (geometric parts), Levels/, Edges/ |
| Materyal | 47 | `Assets/Materials/` | URP upgrade ile çoğu modified, hepsinin shader path'i refresh edilmiş |
| Resources | 2 | `Assets/Resources/GameAnalytics/`, `PerformanceTest*.json` | Hot anti-pattern; Addressables'a geçirilebilir |
| 3rd-party SDK | 5 | `Assets/!OtherAssets/` | AppsFlyer, GameAnalytics, FacebookSDK, EDM, PlayServicesResolver |
| 3rd-party Asset | 5+ | `Assets/!OtherAssets/` | Epic Toon FX, CodeMonkey, Toony Colors Pro, beach (39MB), LevelAssets |

**Disk:**
- `Assets/` toplam: 116 MB
- `Library/`: 2.1 GB (git'te olmamalı, git'e dahil mi diye bakılmalı)
- Proje root: 2.7 GB
- GitHub diskUsage: 442 MB (Library dahil pushlanmış olabilir)

---

## 🎮 Sahne / Sayfa / Modül Listesi

| # | Ad | İşlev | Build'de | Durum |
|---|---|---|---|---|
| 0 | SplashScreen | Açılış / studio logo | ✅ index 0 | Stable |
| 1 | TutorialLevel | Mekanik öğretici | ✅ index 1 | Stable |
| 2 | Level 1 | İlk gerçek level | ✅ index 2 | Stable |
| 3 | Level 2 | 2. level | ✅ index 3 | Stable |
| 4 | Level 3 | 3. level | ✅ index 4 | Bug fix yapılmış (`19bd7cb7 Level 3 Bug Fixes`) |
| — | Scenes/Test/GidenLevel | Eski deneme | ❌ | Silinebilir |
| — | Scenes/Test/Level 4 | Yarım kalmış | ❌ | Revize / sil |
| — | Scenes/Test/TestScene_01/02/03 | Eski test sahneleri | ❌ | Silinebilir |

---

## 🧩 Kod Mimarisi (Mevcut Snapshot)

**Domain:** Drawing puzzle + chase combat hybrid.

**Aktif Script'ler (16 adet, `Assets/Scripts/` flat):**

| Script | Görev | Mimari notları |
|---|---|---|
| `ThirdPersonMovement.cs` | Player hareketi, CharacterController + NavMesh agent için cam-relative turn | New Input System (`PlayerControls`), gravity manual |
| `PlayerInteract.cs` | DrawArea trigger detection, çizim başlat/durdur | Trail prefab spawn, `_canDraw` flag |
| `DrawPart.cs` | Tek bir parça (DrawPart) — Interactable, çizilince LineRenderer ekler | `IInteractable` impl, `isDrawCompleted` flag, GameWon Invoke |
| `PartManager.cs` | Bir grup DrawPart'ı izler, hepsi tamamlanınca wall'u aktive eder | `Update()` her frame foreach — event-driven yapılabilir |
| `WallManager.cs` | Wall lifecycle | Minimal |
| `PlayerCombat.cs` | Player HP / damage handling | `[SerializeField]` HP |
| `EnemyCombat.cs` | Enemy damage uygulayıcı | Magic damage `-1` |
| `EnemyFollow.cs` | NavMeshAgent ile player'a chase | `GameObject.FindWithTag("Player")` ❌ anti-pattern |
| `GameManager.cs` | Sahne yönetimi, level transition, UI | `SceneManagement`, TMPro, 117 satır — God class riski |
| `DontDestroyOnLoad.cs` | Persistent objeler | Singleton-lite |
| `RandomMaterial.cs` | Rastgele materyal atayan helper | OK |
| `VideoEnd.cs` | VideoPlayer end callback | OK |
| `CreateJoystick.cs` (`Assets/CreateJoystick.cs`) | Joystick spawn | Yanlış konumda — flat root'ta |
| `Interfaces/IInteractable.cs` | Interact contract | Tek interface |
| `OldVersion/Grid.cs` | Eski grid pathfinding | ❌ Ölü kod |
| `OldVersion/PathNode.cs` | Eski path node | ❌ Ölü kod |
| `OldVersion/Pathfinding.cs` | Eski A* (12 satır, boş constructor) | ❌ Ölü kod |

**Pattern gözlemleri:**
- Field naming: `_camelCase` private ✅ (kurala uygun)
- Public property/field: `playerSpeed` camelCase ✅
- `[Header]` + `[SerializeField]` kullanımı tutarlı ✅
- `Singleton<T>` pattern yok — ama `FindWithTag`/`FindObjectsOfType` var ❌ (rules: Service Locator + ScriptableObject)
- Coroutine yerine `Invoke(nameof(...), float)` kullanılmış — `Awaitable` ile değiştirilebilir
- Hiç namespace yok ❌ (rules: file-scoped namespace bekleniyor)
- Hiç asmdef yok ❌ (rules: feature-bazlı bölme bekleniyor)

---

## 🚫 Bilinen Sorunlar / Blocker'lar

- **Working tree çok kirli**: 100+ modified file (URP shader migration sonrası `.mat` dosyaları, `.csproj`, Assembly file'lar). Commit veya stash kararı verilmeli.
- **`.gitignore` minimal**: sadece `Temp/UnityLockfile`. Library/obj/Logs git'te olabilir (GitHub diskUsage 442 MB bunu doğruluyor) — temizleme gerekli.
- **Hiç asmdef** → her kod değişikliğinde tam recompile.
- **README outdated**: Unity 2020.3.34f1 yazıyor, gerçek 6000.3.12f1.
- **3 ölü script** `OldVersion/` altında, üretimde kullanılmıyor.
- **`applicationIdentifier` boş** → Android build başlamaz.
- **License yok** → repo public ama lisans belirtilmemiş.

---

## 📜 Önemli Kararlar (ADR — Architecture Decision Records)

| Tarih | Karar | Bağlam | Tradeoff / Alternatif |
|---|---|---|---|
| 2022-04-29 | Hyper-casual drawing puzzle yap | İlk repo, prototip | Voodoo/Lion Studios pazarına uygun, scope dar |
| 2022-Q3 | Unity 2020.3.34f1 + Cinemachine + Input System (yeni) | İlk versiyon | Built-in RP kullanıldı, URP'ye sonra geçildi |
| 2022 | A* pathfinding bırakıldı, NavMeshAgent'a geçildi | `OldVersion/Pathfinding.cs` boş kaldı | NavMesh + Cinemachine basit; A* mobile için overkill |
| 2022 | AppsFlyer + GameAnalytics + Facebook SDK eklendi (`14c6c11d SDK's Added`) | Hyper-casual yayın için attribution + analytics gerekli | Repo size +30 MB |
| 2026-05-08 | Claude-Project-Memory-Template kuruldu | MFD pattern projeye aktarıldı | Manuel kuralları her seferinde anlatmaktansa dosyaya bağla |
| 2026-05-16 | Unity 6 LTS + URP 17.3'e upgrade | Modernize, mobile performans, Awaitable native | Materyaller modified, csproj regenerated, refactor gerekli |

---

## 📚 Faz Özetleri

**Faz 0 — Prototype (2022-04 → 2022-Q3):** Çekirdek drawing/draw-and-fill mekaniği, 3 level + tutorial + splash. Trail/LineRenderer combo ile çizim, NavMeshAgent'lı enemy chase, CharacterController-based player. `4f6b4c88 Bug Fixes` ... `424a9981 Particles Added. Level Tutorial,1,2,3,4 Added` ile prototype kapsamı tamamlandı.

**Faz 1 — Release Candidate (2022 sonu):** SDK entegrasyonu (`14c6c11d SDK's Added`), refactor (`50e672ff Refactored` + `refactor` branch merge), Release Version (`fde24632`). Yayına hazırlık.

**Faz 2 — Unity 6 Upgrade (2026-05):** Repo 4 yıl atıl kaldıktan sonra Unity 6000.3.12f1'e taşındı (`0a7c367e build: latest files`, `7877f08d build: deleted aab files`). URP migration material/.csproj regenerate'leri working tree'de henüz commit edilmedi.

**Faz 3 — Modernization (planned, 2026-Q2+):** Feature folder migrasyonu, ServiceLocator + ScriptableObject mimari, asmdef bölümlemesi, EditMode test, signed Android build.

---

_Bu dosya her önemli ilerleme sonrası güncellenir. ADR'ye yeni karar düşer, "Mevcut Durum" tarih + tek satır olarak revize edilir._
