# Asset Pipeline Tarifleri

> Bu dosya **referans** — yeni proje gerektirdiğinde aç ve uygula.
> `memory.md`'de yer kaplamasın diye buraya ayrıldı (her proje aynı asset türünü kullanmaz).

---

## Meshy MCP — 3D unit / character üretimi

- Paket: `@meshy-ai/meshy-mcp-server`. Free tier kredi sınırlı, batch'li çalış.
- Pipeline:
  1. `meshy_text_to_image` veya `meshy_image_to_image` (concept)
  2. `meshy_image_to_3d` (`should_remesh=true`, `target_polycount=8000`)
  3. `meshy_rig` (humanoid)
  4. FBX export → engine import
- Humanoid retarget: paylaşımlı clip'ler ile çoklu modele dağıtılır.
- Drift olan unit'lerde **per-unit native clip**: `meshy_animate(rig_task_id, action_id)` (3 kredi/clip).
- `meshy_list_tasks` rigging task'ları listelemiyor → Meshy workspace UI'dan task ID kopyalanır.

---

## Meshy texture pack — URP/Lit material setup

Zip 4 dosya: `<name>.fbx` + `_albedo.png` + `_normal.png` + `_metallic.png` + `_roughness.png`.

1. **Normal texture meta:** `textureType: 1` (NormalMap) + `sRGBTexture: 0` (Linear). Yoksa Unity gri linear yorumlar, normal etkisi sıfır.
2. URP/Lit material yarat (`manage_material create`).
3. `m_TexEnvs` slotlar:
   - `_BaseMap` ← albedo
   - `_BumpMap` ← normal
   - `_MetallicGlossMap` ← metallic
4. `_Metallic: 1` slider, Smoothness 0.3-0.5 default. Roughness URP/Lit'te direkt slot yok; istenirse roughness invert + metallic alpha → Smoothness Source: Metallic Alpha.
5. FBX `materialLocation: 1` (External Materials) varsa `Mat_<Name>` klasörde otomatik resolve eder.

---

## Mixamo / Humanoid clip — ayaklar bozuk olunca

1. **Bake Into Pose Y/XZ/Rotation** ✅ — clip FBX import → Animation tab. Mixamo "without skin" default Y kapalı, en yaygın sebep.
2. **Foot IK** — Animator Base Layer ⚙️ → IK Pass ✅. Animator → Apply Root Motion ✅. Clip → Foot IK ✅.
3. **Avatar Configure** — model FBX → Rig → Configure → Skeleton tab → LeftFoot/RightFoot/LeftToes/RightToes mavi (assigned).
4. **Avatar Definition: Create From This Model** ("Without skin" + Generic / Copy From Other Avatar = rig mismatch riski).
5. Yeni clip eklenince formül: `aps = 1 / clipLength` doğal hız için. Speed scale `clipLength / cooldown` ile sürekli swing.
6. Animation Events sistemi: per-unit `_attackHitTiming` yerine clip-içi event "OnAttackHit" → forwarder'a fire et.

---

## Stitch MCP — UI tasarım pipeline

- Google Stitch (UI tasarım, ex-Galileo AI) HTTP MCP, `~/.claude.json` user scope.
- Komut:
  ```
  claude mcp add stitch --transport http --scope user \
    --header "X-Goog-Api-Key: <KEY>" \
    -- https://stitch.googleapis.com/mcp
  ```
  > `--header` çoklu-değer flag olduğu için URL'den önce `--` ayracı **zorunlu**.
- Generate timeout (~2-3 dk normal). Tool description "DO NOT RETRY" diyor — `list_screens` ile yeni screen yarandı mı kontrol et.
- Screenshot URL'leri Google CDN thumbnail döndürür (~120 KB) — **`=s4096` parametresi** ile gerçek HD master gelir (1-3 MB).
- Generate edilen ekranlar Stitch palette + tipografi referans olur; native uGUI port manuel yapılır (sprite mockup → cookbook §1 pattern'i).

---

## Chrome MCP (Claude in Chrome)

- JS-rendered sayfada (Milanote vb.):
  ```
  tabs_create_mcp → navigate → read_page → javascript_tool (DOM deep query) → computer double_click ref=X
  ```
- Bot detection / CAPTCHA yapma — Anthropic policy yasakları.

---

## PDF / dokümantasyon üretimi

- `anthropic-skills:pdf` skill, reportlab + matplotlib.
- Türkçe karakter için **DejaVu Sans** font zorunlu.
- Komut: `uv run --with reportlab --with matplotlib --with Pillow python scripts/build_doc.py`
