using System.Collections;
using System.Collections.Generic;
using DrawRush.Core;
using DrawRush.Enemy;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DrawRush.Dev
{
    /// <summary>
    /// Test bench for enemies, used only by the EnemySandbox scene (not in the build). It loads
    /// the game scene, empties the current level of enemies, then spawns any enemy type in any
    /// colour on a key press. A gallery lays out every type in every colour, standing still.
    ///
    /// Keys: 1-8 spawn a type · Q/E previous/next colour · G gallery · X clear ·
    /// H god mode · F freeze all.
    /// </summary>
    public sealed class EnemySandbox : MonoBehaviour
    {
        private const string GameScene = "Assets/Scenes/01_DrawRushGame.unity";

        private static readonly EnemyVariant[] Types =
        {
            EnemyVariant.Normal, EnemyVariant.Sprinter, EnemyVariant.Tank, EnemyVariant.Eater,
            EnemyVariant.Dasher, EnemyVariant.Flanker, EnemyVariant.Orbiter, EnemyVariant.Boss,
        };

        private static readonly string[] TypeNames =
        {
            "Normal (zımba)", "Sprinter (makas)", "Tank (delgeç)", "Eater (kalemtıraş)",
            "Dasher (raptiye)", "Flanker (silgi)", "Orbiter (küçük silgi)", "Boss (dev silgi)",
        };

        [SerializeField] private float spawnDistance = 7f;
        [SerializeField] private float gallerySpacing = 2.1f;

        private readonly List<EnemyFollow> _spawned = new();
        private EnemyFollow _template;
        private LevelManager _levels;
        private DrawRush.Player.PlayerHealth _health;
        private Transform _parent;
        private int _colour = -2;   // -2 random, -1 the original colour, 0.. a palette colour
        private bool _god = true, _ready;
        private string _status = "Loading the game scene...";

        private IEnumerator Start()
        {
            if (SceneManager.GetSceneByPath(GameScene).isLoaded == false)
            {
#if UNITY_EDITOR
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    GameScene, new LoadSceneParameters(LoadSceneMode.Additive));
#else
                yield return SceneManager.LoadSceneAsync(GameScene, LoadSceneMode.Additive);
#endif
            }
            SceneManager.SetActiveScene(SceneManager.GetSceneByPath(GameScene));

            // Wait for the level and its 3-2-1 countdown.
            while ((_levels = FindFirstObjectByType<LevelManager>()) == null || Time.timeScale <= 0f) yield return null;
            yield return null;

            yield return new WaitForSeconds(0.5f);   // let the tier director dress the level first
            _health = GameServices.Health;

            foreach (EnemyFollow e in FindObjectsByType<EnemyFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (e.CanBeBoss) { _template = e; break; }
            if (_template == null) { _status = "No enemy with every look was found."; yield break; }

            _parent = _levels.GetLevel(_levels.CurrentIndex);
            Clear();
            _ready = true;
            _status = "Ready";
        }

        private void Update()
        {
            if (!_ready) return;
            if (_god && _health != null && _health.Current < _health.StartingValue) _health.Heal(_health.StartingValue - _health.Current);

            Keyboard k = Keyboard.current;
            if (k == null) return;
            Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8 };
            for (int i = 0; i < digits.Length; i++)
                if (k[digits[i]].wasPressedThisFrame) Spawn(i);
            if (k.qKey.wasPressedThisFrame) _colour = Mathf.Max(-2, _colour - 1);
            if (k.eKey.wasPressedThisFrame) _colour = Mathf.Min(3, _colour + 1);
            if (k.gKey.wasPressedThisFrame) Gallery();
            if (k.xKey.wasPressedThisFrame) Clear();
            if (k.hKey.wasPressedThisFrame) _god = !_god;
            if (k.fKey.wasPressedThisFrame) foreach (EnemyFollow e in _spawned) if (e != null) e.Freeze(4f);
        }

        /// <summary>Spawn one enemy of <paramref name="type"/> (index into the type list) near the player.</summary>
        public void Spawn(int type)
        {
            Transform player = GameServices.Player;
            if (player == null) return;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 at = player.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnDistance;
            EnemyFollow e = Make(at, Types[type], true);
            if (e != null) _status = $"{TypeNames[type]} · {ColourName(_colour)}";
        }

        // Every type in every colour, in rows in front of the player, standing still.
        private void Gallery()
        {
            Clear();
            Transform player = GameServices.Player;
            if (player == null) return;
            // Centred on the player, so the follow camera sees it all: types run down the page.
            Vector3 origin = player.position + new Vector3(-2f * gallerySpacing, 0f, 3.5f * gallerySpacing);
            int was = _colour;
            for (int row = 0; row < Types.Length; row++)
            {
                int colours = 1;
                for (int c = 0; c < colours; c++)
                {
                    _colour = c - 1;
                    EnemyFollow e = Make(origin + new Vector3(c * gallerySpacing, 0f, -row * gallerySpacing), Types[row], false);
                    if (c == 0 && e.GetComponentInChildren<EnemyPalette>(false) is { } p) colours += p.Count;
                }
            }
            _colour = was;
            _status = "Gallery: every type in every colour (rows top to bottom: Normal → Boss)";
        }

        private EnemyFollow Make(Vector3 at, EnemyVariant type, bool live)
        {
            if (NavMesh.SamplePosition(at, out NavMeshHit hit, 4f, NavMesh.AllAreas)) at = hit.position;
            bool wasActive = _template.gameObject.activeSelf;
            _template.gameObject.SetActive(false);   // the copy starts inactive, so it wakes at its new spot
            EnemyFollow e = Instantiate(_template, at, Quaternion.identity, _parent);
            _template.gameObject.SetActive(wasActive);
            e.name = "Sandbox_" + type;
            e.gameObject.SetActive(true);

            bool boss = type == EnemyVariant.Boss;
            float speed = type switch { EnemyVariant.Sprinter => 1.5f, EnemyVariant.Tank => 0.7f, EnemyVariant.Flanker => 1.1f, EnemyVariant.Orbiter => 1.25f, EnemyVariant.Boss => 0.675f, _ => 1f };
            float scale = type switch { EnemyVariant.Sprinter => 0.8f, EnemyVariant.Tank => 1.35f, EnemyVariant.Orbiter => 0.8f, EnemyVariant.Boss => live ? 1.8f : 1f, _ => 1f };
            e.ApplyVariant(type, speed, scale, boss || type == EnemyVariant.Tank, null);

            EnemyPalette palette = e.GetComponentInChildren<EnemyPalette>(false);
            if (palette != null && _colour > -2) palette.Apply(Mathf.Min(_colour, palette.Count - 1));

            if (!live)
            {
                e.enabled = false;
                if (e.TryGetComponent(out EnemyCombat combat)) combat.enabled = false;
                if (e.TryGetComponent(out NavMeshAgent agent) && agent.isOnNavMesh) agent.isStopped = true;
                e.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            _spawned.Add(e);
            return e;
        }

        private static string ColourName(int c) => c switch { -2 => "random colour", -1 => "original colour", _ => "colour " + (c + 1) };

        private void Clear()
        {
            foreach (EnemyFollow e in _spawned) if (e != null) Destroy(e.gameObject);
            _spawned.Clear();
            if (_levels != null) _parent = _levels.GetLevel(_levels.CurrentIndex);
            if (_parent == null) return;
            foreach (EnemyFollow e in _parent.GetComponentsInChildren<EnemyFollow>(false)) e.gameObject.SetActive(false);
            _status = "Cleared";
        }

        private void OnGUI()
        {
            float s = Mathf.Max(1f, Screen.height / 1300f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float h = 470f;
            GUILayout.BeginArea(new Rect(6, Screen.height / s - h - 90f, 230, h), GUI.skin.box);
            GUILayout.Label("<b>ENEMY SANDBOX</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16 });
            GUILayout.Label(_status);
            if (!_ready) { GUILayout.EndArea(); return; }
            for (int i = 0; i < Types.Length; i++)
                if (GUILayout.Button($"{i + 1}  {TypeNames[i]}")) Spawn(i);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀ Q")) _colour = Mathf.Max(-2, _colour - 1);
            GUILayout.Label(ColourName(_colour), GUILayout.Width(120));
            if (GUILayout.Button("E ▶")) _colour = Mathf.Min(3, _colour + 1);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("G  Gallery (all colours)")) Gallery();
            if (GUILayout.Button("F  Freeze all 4 s")) foreach (EnemyFollow e in _spawned) if (e != null) e.Freeze(4f);
            if (GUILayout.Button("X  Clear")) Clear();
            if (GUILayout.Button($"H  God mode: {(_god ? "ON" : "OFF")}")) _god = !_god;
            GUILayout.Label($"Enemies: {_spawned.Count} · keys 1-8, Q/E, G, F, X, H");
            GUILayout.EndArea();
        }
    }
}
