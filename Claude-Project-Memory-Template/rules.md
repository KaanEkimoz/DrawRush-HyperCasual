# Kaan'ın Kalıcı Kuralları

> Bu dosya Kaan'ın koyduğu, her zaman uyman gereken kurallarını içerir.
> Yeni kural sadece Kaan açıkça "bu bir kural" dediğinde veya "bundan sonra hep böyle yap" dediğinde eklenir.
> Her kuralın **neden** konduğu kısaca yazılmalı ki edge-case'lerde doğru karar verebilesin.

---

## ⚡ Quick Reference (30 saniyede tara)

| Konu | Kural | Detay |
|---|---|---|
| Dil | Sohbet TR, kod EN | [İletişim] |
| Naming | `_camelCase` private, `PascalCase` public, `camelCase` local | [Kod Stili] |
| Field | `public` field yasak — `[SerializeField] private` veya property | [Kod Stili] |
| Update | `Find*ByType`, `GetComponent`, `Camera.main`, `Resources.Load` Awake'te cache | [Performance] |
| Branch | `claude-dev` Claude, `main` Kaan onayı + `--no-ff` | [Git] |
| Commit | Conventional Commits, her özellik anında commit, **amend yok** | [Git] |
| MCP | Unity işi → önce Unity MCP, bağlı değilse Kaan'a sor | [Tooling] |
| İzin gerekir | Package install, asmdef ekle, sahne sil, history rewrite, secret ekle | [İzin Sınırları] |
| İzin gerekmez | Kod ekle/edit, yeni sahne, prefab/SO yarat, normal commit | [İzin Sınırları] |
| Render Pipeline | URP default | [Unity Defaults → memory.md] |
| Input | Input System (yeni) default | [Unity Defaults → memory.md] |
| Asset loading | Addressables (Resources yasak) | [Unity Defaults → memory.md] |
| Secret | Dosyaya/commit'e API key/token YAZMA — `Secrets.cs` `.gitignore`'da | [Secrets] |

Detaylar aşağıda.

---

## Genel İletişim

### Konuşma Türkçe, kod ve yorumlar İngilizce
**Kural:** Claude ile sohbet, dokümantasyon ve commit dışı metinler Türkçe. Kod, değişken/sınıf/fonksiyon isimleri, kod içi yorumlar (`//`, `///`) ve commit mesajları İngilizce.
**Neden:** TR Kaan'ın ana dili — iletişim hızlı. EN kod ise Asset Store import'ları, Stack Overflow örnekleri ve olası takım üyeleriyle tutarlılığı garantiler.

### Plan sonrası uygula, izin isteme
**Kural:** Plan tartışıldıktan sonra Claude küçük kararlar için onay beklemez — uygular ve özet rapor verir.
**Neden:** Plan aşamasında yön zaten belirleniyor; her adımda izin sormak tempo kaybı.

### Scope-dışı temizlik proaktif değil
**Kural:** İş üstündeyken görülen warning / dead-code / stil sorunlarını yan iş olarak temizleme — Kaan söyleyince temizle.
**Neden:** Her commit'i izole tutmak, scope creep'ten kaçınmak.
**Nasıl uygulanır:** "Şurada X warning var, ayrı commit'te temizleyelim mi?" diye söz et ama dokunma.

---

## İzin Sınırları (destructive op listesi)

### İzin GEREKİR — Kaan'a sor önce
- `package.json` / `manifest.json` install / upgrade / remove
- Yeni assembly definition (asmdef) ekleme veya mevcut asmdef yapısını değiştirme
- Sahne **silme** (yaratma OK)
- Asset GUID değişikliği gerektirecek manuel `.meta` edit
- Player Settings / Quality Settings / Render Pipeline asset değişikliği
- Git history rewrite (`filter-repo`, `BFG`, `reset --hard`, `push --force`)
- API key / token / secret içeren herhangi bir dosya yaratma veya commit
- `main` branch'e direct push veya merge
- Bağımlılık (NuGet / Asset Store paket) kaldırma
- 50+ dosyayı etkileyecek bulk rename / move

### İzin GEREKMEZ — direkt yap
- Yeni `.cs` script yaratma + edit
- Yeni sahne yaratma
- Yeni prefab / Material / ScriptableObject yaratma
- Component ekleme / property set
- `claude-dev` branch'e commit
- `Assets/Scenes/<NewScene>.unity` yaratma
- `Assets/_Project/<Feature>/...` altına yeni dosya
- `read_console`, `refresh_unity`, `manage_scene get_*`, `find_gameobjects`

---

## Kod Stili (genel)

### Public field yasak — property veya `[SerializeField] private`
**Kural:** Inspector / serialization gerekiyorsa `[SerializeField] private`. Dış erişim gerekiyorsa property. **`public field` hiçbir durumda kullanılmaz.**
**Neden:** Encapsulation kor. Refactor zorlaşmasın. Performans farkı yok.

### Private field naming: `_camelCase`
**Kural:** Private field'lar `_` prefix + camelCase. Property `PascalCase`, local değişken `camelCase` (prefix'siz), parametre `camelCase`.
**Neden:** Bakışta scope anlaşılır. Microsoft / Roslyn stil kılavuzunun popüler varyantı.
**Otomasyon:** `.editorconfig` enforce eder (template'te dahil).

### `var` sadece type sağdan açıkça belliyse
**Kural:** Sağ taraftaki ifadeden type doğrudan anlaşılıyorsa `var`. Aksi halde explicit type.
**Neden:** Niyet saklanmasın; okuyucu satıra bakınca type'ı anlayabilmeli.

### Magic number'ları çıkar
**Kural:** `if (hp < 50)` yerine `if (hp < CriticalHealthThreshold)`. Hardcoded sabitleri `const` veya `[SerializeField]` field'a çıkar.
**Neden:** Tweaking kolaylığı + niyetin görünürlüğü.

### File-scoped namespace (Unity 6 / .NET 6+)
**Kural:** `namespace MyGame.Combat;` (file-scoped) tercih et — bracket'lı geleneksel namespace yerine.
**Neden:** İndentation tasarrufu, modern C# default.

---

## [Unity] Kod & Mimari

### [Unity] Inspector field'ları için `[SerializeField] private`
**Kural:** Inspector'dan değiştirilmesi gereken field'lar `[SerializeField] private`. Inspector'a çıkmayacak field sadece `private`.
**Neden:** Designer Inspector'dan değiştirebilir, dış kod field'ı yazamaz.

### [Unity] Namespace: `<ProjectName>.<Feature>`
**Kural:** Kendi yazdığımız kod `<ProjectName>` root namespace'i altında, feature-bazlı alt namespace'lerde (`MyGame.Combat`, `MyGame.UI`, `MyGame.Core`).
**Neden:** Asset Store paketleriyle isim çakışmasını önler.

### [Unity] Performance — Update'te yasaklar
**Kural:** `Update()`, `FixedUpdate()`, `LateUpdate()` içinde:
- `FindFirstObjectByType<T>()` / `FindAnyObjectByType<T>()` (Unity 6 modern; eski `FindObjectOfType` deprecated)
- `GetComponent<T>()`
- `Camera.main`
- `Resources.Load<T>()`
- `string` concatenation her frame
- LINQ (`.Where`, `.Select`) hot path
- `new List<T>()`, `new Vector3()` her frame
- `foreach` over `IEnumerable<T>` (boxing) — `for` veya `foreach` over `List<T>` OK (struct enumerator)

**Yap yerine:** `Awake`/`Start`'ta cache, reusable buffer field, `StringBuilder` veya cache.

### [Unity] Singleton yasak — ScriptableObject + Service Locator
**Kural:** Klasik `public static Instance` singleton kullanılmaz. Yerine:
- **Veri / state** → ScriptableObject asset, Inspector'dan referans alır.
- **Runtime servisi** (Audio, Pool, SceneLoader) → static `GameServices` locator pattern, bootstrap'te inject. Scope büyürse VContainer'a geç (Kaan onayı ile).
**Neden:** Singleton global state, test edilemez, sahne geçişinde lifetime dertleri.

### [Unity] Event sistemi 3 katmanlı
**Kural:**
- **Sistemler arası** (Combat → UI) → ScriptableObject Event Channel
- **Aynı obje içi** (aynı GameObject'teki component'ler) → C# `event` / `Action`
- **UI component'ler** (Button, Slider, Toggle) → Unity'nin kendi `UnityEvent`
- **Long-running async akış** → `Awaitable` (Unity 6 native) veya `UniTask` (cancellation token ile)

### [Unity] Composition over inheritance
**Kural:** Tek `Character` MonoBehaviour + sub-behavior'lar plain C# class (Awake'te `new`). MonoBehaviour sadece Unity callback'i (OnTriggerEnter, OnAnimatorMove vs.) gerekiyorsa.
**Neden:** Diamond inheritance + override hell + prefab variant cehennemi yerine, her sub-behavior bağımsız test edilir, swap edilir.

### [Unity] Log disiplini
**Kural:**
- `Debug.Log` sadece geliştirme sırasında — commit öncesi temizlenir.
- `Debug.LogError` / `Debug.LogWarning` gerçek anomali için production'da kalabilir.
- `try / catch` external input veya unreliable IO için.

### [Unity] AOT / IL2CPP gotcha
**Kural:** Reflection-heavy kod (`Activator.CreateInstance`, `MakeGenericMethod`, `Type.GetType` runtime'da), runtime-generated generic, dinamik IL emit IL2CPP build'da kırılır.
**Nasıl uygulanır:** AOT-şüpheli kod yazarken Kaan'ı uyar. Json serialization Newtonsoft varsayılan, IL2CPP'de polymorphism için `[Preserve]` + link.xml gerekebilir — Kaan'a danış.

---

## [Unity] Sahne Organizasyonu

### [Unity] Her sahnede Main Camera + Directional Light + grup yerleşimi
**Kural:** Her oynanabilir sahnede Main Camera (`MainCamera` tag'li) ve Directional Light bulunur. İkisi de `===CAMERA===` ve `===LIGHTING===` grupları altına yerleştirilir.
**Neden:** Sahne create'te Unity default kamera/ışık vermeyebilir; Game View siyah ekran açar, debug zor.

### [Unity] Hierarchy `===GROUP===` başlıklarıyla organize
**Kural:** Her Unity sahnesinde root-level GameObject'ler anlamlı gruplar altında toplanır. Başlık objeleri `===GROUP===` formatında (büyük harf, üst-alt üç eşittir), pozisyonu (0,0,0) ve boş Transform.
**Neden:** 3 obje 30 olur, 30 obje 100 olur. Düz root listesi 1 hafta sonra yönetilemez.
**Standart grup seti:**
```
===CAMERA===
===LIGHTING===
===UI===
===SERVICES===
===GAMEPLAY===
===ENVIRONMENT===
```

---

## [Unity] Asset & Klasör Yönetimi

### [Unity] Klasör/dosya naming: PascalCase, boşluksuz
**Kural:** Kendi yarattığımız klasör ve asset dosyaları PascalCase + boşluksuz (`Pawns/`, `MeleeCharacter.cs`). Asset Store / paket klasörleri olduğu gibi bırakılır.
**Neden:** Boşluklu path → CLI'da tırnak zorunluluğu, WebGL'de `%20` encoding, namespace ile inkonsistent.

### [Unity] Feature-bazlı klasör organizasyonu (Unity 6 standart)
**Kural:** Kendi kodumuz `Assets/_Project/<Feature>/` altında, feature içinde tip alt klasörler:

```
Assets/
├── _Project/
│   ├── Combat/
│   │   ├── Scripts/
│   │   ├── Prefabs/
│   │   └── Data/
│   ├── UI/
│   │   ├── Scripts/
│   │   └── Prefabs/
│   ├── Levels/
│   │   ├── Scenes/
│   │   └── Data/
│   └── Core/
│       ├── Scripts/
│       └── Bootstrap/
├── Settings/                   (Unity URP / Input default)
└── Import Assets/              (external — dokunma)
```
**Neden:** Tip-bazlı kök klasör (`Assets/Scripts`, `Assets/Prefabs`) 200+ asset'te ölü. Feature-bazlı her domain self-contained → asmdef ile bölmek kolay → iteration time düşer.
**Escape hatch:** Pilot/protip projede tip-bazlı OK, ama 50+ asset'ten önce feature-bazlıya migrate.

### [Unity] Assembly Definition (asmdef)
**Kural:** Her feature klasörünün kendi `.asmdef`'i olur. EditMode test ayrı asmdef. Editor extension ayrı asmdef.
**Neden:** Tek asmdef projeyi hıçırıklı domain reload yapar (her .cs değişikliğinde tüm proje recompile). Feature başına asmdef → sadece o feature recompile, iteration 5x hızlı.
**Naming:** `MyGame.Combat`, `MyGame.Combat.Tests.EditMode`, `MyGame.Combat.Editor`.

---

## [Unity] Tooling

### [Unity] Unity işleri için önce Unity MCP — bağlantı yoksa Kaan uyarılır
**Kural:** Sahne, prefab, GameObject, component, Animator, ScriptableObject, build settings gibi Unity-içi her iş için **önce** `mcp__UnityMCP__*` araçları denenir. Unity MCP kapalı / bağlantı yoksa, alternatif yola (Editor scripti, Bash, YAML edit) geçmeden önce Kaan açıkça uyarılır.
**Sıralama:** Unity Editor aç → `Window > MCP For Unity > Start Server` → `claude mcp list` ile `UnityMCP ✓ Connected` doğrula → Claude oturumu.
**Neden:** Unity MCP açık olduğunda native API ile tek call'da iş biter. YAML edit fileID conflict riski.

### [Unity] Editor/ klasörüne MonoBehaviour koyma
**Kural:** `Editor/` klasöründeki script'ler sadece UnityEditor assembly'sine girer, runtime sahne GameObject'ine attach edilemez. MonoBehaviour runtime klasörde, Inspector buttonu / `CustomEditor` / `MenuItem` `Editor/` klasöründe.

---

## Secrets

### API key / token / credential kuralı
**Kural:**
- Sohbet'te paylaşmak OK (Kaan tercih ediyor) — ama **dosyaya / commit'e ASLA YAZMA**.
- Kullanıcının lokal `Secrets.cs` partial class'ı veya `EditorPrefs` ile sakla.
- `Secrets.cs`, `.env`, `*.key`, `*.pem` `.gitignore`'da (template default).
- Yanlışlıkla commit edilirse: hemen rotate + history rewrite (Kaan onayı ile).
**Neden:** Public repo veya başkasıyla paylaşılan repo'da credential leak ciddi güvenlik sorunu.

---

## Git & Versiyon Kontrol

### Conventional Commits + her özellik sonrası commit
**Kural:** Tüm commit mesajları Conventional Commits formatında: `feat:`, `fix:`, `docs:`, `refactor:`, `chore:`, `test:`, `perf:`, `style:`, `ci:`. Her özellik bitiminde hemen commit.
**Format:** `<type>(<scope>): <özet>` — özet 72 karakteri geçmesin, imperative tense ("add", "fix").

### Claude kendi branch'inde çalışır
**Kural:** Claude her zaman `claude-dev` branch'inde çalışır. `main`'e geçiş veya merge sadece Kaan açıkça söylediğinde.

### Büyük özellikler için feature branch
**Kural:** Birkaç saatlik iş + test gerektiren özellik için `claude-dev-feat-<kısa-isim>` branch'inde, bitince `claude-dev`'e merge (`--no-ff`).

### Main'e merge: milestone + Kaan onayı
**Kural:** `claude-dev` → `main` merge sadece milestone + Kaan açıkça söyleyince. Otomatik ritim yok.

### Her mantıksal iş yeni commit — amend yok
**Kural:** Her bitmiş iş ayrı commit'tir. `--amend` yapılmaz.

### History rewrite öncesi tam backup
**Kural:** `git filter-repo`, BFG, `git reset --hard`, `git push --force` öncesi:
1. `cp -r .git .git.bak`
2. `git branch backup-pre-rewrite-$(date +%s)`
3. `.gitignore`'a `.git.bak/` ekle
**Neden:** Filter-repo working tree dosyalarını silebilir. Backup olmadan restore imkansız.

### Git LFS — binary asset'leri dışla
**Kural:** FBX / PNG / PSD / WAV / MP4 / unitypackage Git LFS'e gider. Template'in `.gitattributes`'unu kullan.
**Setup:** Yeni proje açılışında `git lfs install` (makinede tek seferlik) + `git lfs track` ile yeni pattern eklenirse `.gitattributes` commit.

---

### Kural Formatı (yeni kural eklerken kullan)

```
### [Kural başlığı]
**Kural:** [kuralın kendisi, net cümleyle]
**Neden:** [Kaan'ın verdiği sebep — geçmiş olay, tercih, tecrübe]
**Nasıl uygulanır:** [hangi durumlarda devreye girer]
**Eklendi:** [YYYY-MM-DD]
```
