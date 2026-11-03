using System.Collections.Generic;
using DrawRush.Core;
using DrawRush.Enemy;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace DrawRush.Dev
{
    /// <summary>
    /// Test bench for enemies, in its own small scene (Assets/Scenes/Dev/EnemySandbox.unity,
    /// not in the build): a paper floor, a pencil you walk with WASD or the arrow keys, and a
    /// panel that spawns any enemy type in any colour. "Show all" lines up every type in every
    /// colour, standing still. Enemies cannot hurt the pencil here.
    /// </summary>
    public sealed class EnemySandbox : MonoBehaviour
    {
        private sealed class Kind
        {
            public string Name;
            public EnemyVariant Variant;
            public float Speed, Scale;
            public bool Heavy;
        }

        private static readonly Kind[] Kinds =
        {
            new() { Name = "Zımba", Variant = EnemyVariant.Normal, Speed = 1f, Scale = 1f },
            new() { Name = "Makas · hızlı", Variant = EnemyVariant.Sprinter, Speed = 1.5f, Scale = 0.8f },
            new() { Name = "Delgeç · ağır", Variant = EnemyVariant.Tank, Speed = 0.7f, Scale = 1.35f, Heavy = true },
            new() { Name = "Kalemtıraş · boya yer", Variant = EnemyVariant.Eater, Speed = 1f, Scale = 1f },
            new() { Name = "Raptiye · atılır", Variant = EnemyVariant.Dasher, Speed = 1f, Scale = 1f },
            new() { Name = "Silgi · önünü keser", Variant = EnemyVariant.Flanker, Speed = 1.1f, Scale = 1f },
            new() { Name = "Hamur silgi · parça atar", Variant = EnemyVariant.Orbiter, Speed = 1.25f, Scale = 0.8f },
            new() { Name = "Dev silgi · boss", Variant = EnemyVariant.Boss, Speed = 0.675f, Scale = 1.8f, Heavy = true },
            new() { Name = "Yapıştırıcı · yavaşlatır", Variant = EnemyVariant.Glue, Speed = 0.8f, Scale = 1f },
        };

        // Palette order on every enemy look: teal, orange, green, purple (the eraser has no purple).
        private static readonly string[] ColourNames = { "Rastgele", "Orijinal", "Turkuaz", "Turuncu", "Yeşil", "Mor" };
        private static readonly Color[] ColourSwatches =
        {
            new(0.85f, 0.85f, 0.85f), new(0.25f, 0.35f, 0.85f), new(0.2f, 0.8f, 0.75f),
            new(1f, 0.6f, 0.2f), new(0.35f, 0.8f, 0.3f), new(0.6f, 0.35f, 0.85f),
        };

        [SerializeField] private EnemyFollow enemyPrefab;
        [SerializeField] private Transform player;
        [SerializeField] private NavMeshSurface floor;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float floorHalfSize = 38f;
        [SerializeField] private float spawnDistance = 7f;

        private readonly List<EnemyFollow> _enemies = new();
        private readonly List<(string label, Vector3 at)> _rowLabels = new();
        private Vector3 _galleryCentre;
        private int _colour;   // index into ColourNames; 0 = random
        private float _cameraHeight = 16f, _cameraTarget = 16f;
        private GUIStyle _panel, _title, _button, _small, _rowLabel;

        private void Awake()
        {
            floor.BuildNavMesh();
            GameServices.Register(player, player, viewCamera.transform, null, null);
        }

        private void OnDestroy() => GameServices.Clear();

        private void Update()
        {
            Walk();
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
                for (int i = 0; i < digits.Length; i++)
                    if (k[digits[i]].wasPressedThisFrame) Spawn(i);
            }
        }

        private void LateUpdate()
        {
            _cameraHeight = Mathf.Lerp(_cameraHeight, _cameraTarget, 1f - Mathf.Exp(-4f * Time.deltaTime));
            // Look a little below the pencil so it sits above the panel at the bottom of the screen.
            Vector3 focus = (_rowLabels.Count > 0 ? _galleryCentre : player.position) + new Vector3(0f, 0f, -_cameraHeight * 0.22f);
            // The gallery is seen flatter, so tall and flat enemies both read from the front.
            float back = _rowLabels.Count > 0 ? 0.8f : 0.55f;
            viewCamera.transform.position = focus + new Vector3(0f, _cameraHeight, -_cameraHeight * back);
            viewCamera.transform.LookAt(focus);
        }

        private void Walk()
        {
            Keyboard k = Keyboard.current;
            if (k == null) return;
            Vector3 move = Vector3.zero;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) move.z += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) move.z -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) move.x += 1f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) move.x -= 1f;
            if (move.sqrMagnitude < 0.01f) return;
            move.Normalize();
            Vector3 p = player.position + move * (walkSpeed * Time.deltaTime);
            p.x = Mathf.Clamp(p.x, -floorHalfSize, floorHalfSize);
            p.z = Mathf.Clamp(p.z, -floorHalfSize, floorHalfSize);
            player.position = p;
            player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(move), 1f - Mathf.Exp(-12f * Time.deltaTime));
        }

        private void Spawn(int kind)
        {
            if (_rowLabels.Count > 0) Clear();
            _cameraTarget = 16f;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Make(player.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnDistance, Kinds[kind], _colour, true);
        }

        // Every type in every colour, one row per type, standing still around the pencil.
        private void ShowAll()
        {
            Clear();
            const float gap = 3.2f;
            Vector3 origin = player.position + new Vector3(-0.5f * gap, 0f, 4f * gap);
            _galleryCentre = origin + new Vector3(0.9f * gap, 0f, -4.2f * gap);   // leaves room for the row names
            for (int row = 0; row < Kinds.Length; row++)
            {
                Vector3 rowStart = origin + new Vector3(0f, 0f, -row * gap);
                _rowLabels.Add((Kinds[row].Name, rowStart + new Vector3(-gap * 0.9f, 0f, 0f)));
                for (int c = 1; c < ColourNames.Length; c++)
                {
                    EnemyFollow e = Make(rowStart + new Vector3((c - 1) * gap, 0f, 0f), Kinds[row], c, false);
                    // The eraser has one colour fewer: drop the repeat.
                    EnemyPalette p = e.GetComponentInChildren<EnemyPalette>(false);
                    if (c > 1 && (p == null || c - 2 >= p.Count)) { _enemies.Remove(e); Destroy(e.gameObject); }
                }
            }
            _cameraTarget = 31f;
        }

        private EnemyFollow Make(Vector3 at, Kind kind, int colour, bool live)
        {
            if (NavMesh.SamplePosition(at, out NavMeshHit hit, 4f, NavMesh.AllAreas)) at = hit.position;
            EnemyFollow e = Instantiate(enemyPrefab, at, Quaternion.Euler(0f, 180f, 0f));
            e.ApplyVariant(kind.Variant, kind.Speed, live ? kind.Scale : Mathf.Min(kind.Scale, 1.2f), kind.Heavy, null);
            EnemyPalette palette = e.GetComponentInChildren<EnemyPalette>(false);
            if (palette != null && colour > 0) palette.Apply(Mathf.Min(colour - 2, palette.Count - 1));
            if (e.TryGetComponent(out EnemyCombat combat)) combat.enabled = live;
            if (!live)
            {
                e.enabled = false;
                if (e.TryGetComponent(out NavMeshAgent agent)) agent.enabled = false;
            }
            _enemies.Add(e);
            return e;
        }

        private void Clear()
        {
            foreach (EnemyFollow e in _enemies) if (e != null) Destroy(e.gameObject);
            _enemies.Clear();
            _rowLabels.Clear();
            _cameraTarget = 16f;
        }

        private void OnGUI()
        {
            float s = Screen.height / 1280f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float w = Screen.width / s;
            Styles();

            foreach (var (label, at) in _rowLabels)
            {
                Vector3 sp = viewCamera.WorldToScreenPoint(at);
                if (sp.z > 0f) GUI.Label(new Rect(sp.x / s - 230f, (Screen.height - sp.y) / s - 14f, 220f, 28f), label, _rowLabel);
            }

            const float panelHeight = 556f;
            GUILayout.BeginArea(new Rect(12f, Screen.height / s - panelHeight - 12f, w - 24f, panelHeight), _panel);
            GUILayout.Label("Düşman testi", _title);
            GUILayout.Label("WASD / ok tuşları: yürü · düşmanlar hasar vermez", _small);

            for (int i = 0; i < Kinds.Length; i += 2)
            {
                GUILayout.BeginHorizontal();
                for (int j = i; j < i + 2 && j < Kinds.Length; j++)
                    if (GUILayout.Button($"{j + 1}. {Kinds[j].Name}", _button, GUILayout.Height(52f))) Spawn(j);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8f);
            GUILayout.Label("Renk", _small);
            GUILayout.BeginHorizontal();
            for (int c = 0; c < ColourNames.Length; c++)
            {
                Color was = GUI.backgroundColor;
                GUI.backgroundColor = ColourSwatches[c];
                string text = c == _colour ? $"[{ColourNames[c]}]" : ColourNames[c];
                if (GUILayout.Button(text, _button, GUILayout.Height(44f))) _colour = c;
                GUI.backgroundColor = was;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hepsini göster", _button, GUILayout.Height(52f))) ShowAll();
            if (GUILayout.Button("Temizle", _button, GUILayout.Height(52f))) Clear();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void Styles()
        {
            if (_title != null) return;
            var dark = new Texture2D(1, 1);
            dark.SetPixel(0, 0, new Color(0.1f, 0.12f, 0.24f, 0.92f));
            dark.Apply();
            _panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 10, 10) };
            _panel.normal.background = dark;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
            _title.normal.textColor = Color.white;
            _small = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            _small.normal.textColor = new Color(0.85f, 0.87f, 0.95f);
            _button = new GUIStyle(GUI.skin.button) { fontSize = 21 };
            _button.normal.textColor = _button.hover.textColor = Color.white;
            _rowLabel = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, wordWrap = false };
            _rowLabel.normal.textColor = new Color(0.12f, 0.15f, 0.36f);
        }
    }
}
