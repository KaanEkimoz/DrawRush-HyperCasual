using DrawRush.Core;
using DrawRush.Enemy;
using UnityEngine;

namespace DrawRush.Boosters
{
    /// <summary>
    /// The shop pet: a little companion that trails the player and, every few seconds, bumps the
    /// nearest enemy in range away. It takes on part of the fight, never the drawing (research:
    /// a helper that draws would play the game for you). Heavy enemies shrug it off.
    /// Exists only once bought; upgrades shorten the cooldown and widen the reach.
    /// </summary>
    public sealed class BumpPet : MonoBehaviour
    {
        [SerializeField] private Material material;
        [SerializeField] private Color color = new(1f, 0.55f, 0.75f, 1f);
        [Tooltip("Cooldown (s) per pet level 1..3.")]
        [SerializeField] private float[] cooldowns = { 6f, 5f, 4f };
        [Tooltip("Reach (m) per pet level 1..3.")]
        [SerializeField] private float[] reaches = { 2.5f, 2.8f, 3.2f };
        [SerializeField] private float pushDistance = 2.5f;
        [SerializeField] private Vector3 followOffset = new(-0.9f, 0f, -0.7f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Transform _body;
        private float _ready;
        private float _hop;
        private readonly Transform[] _near = new Transform[1];
        private EnemyProximity _enemies;

        private void Start()
        {
            _enemies = FindFirstObjectByType<EnemyProximity>();
            BoosterUpgrades.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy() => BoosterUpgrades.Changed -= Refresh;

        private void Refresh()
        {
            bool owned = BoosterUpgrades.PetLevel > 0;
            if (owned && _body == null) Build();
            if (_body != null) _body.gameObject.SetActive(owned);
        }

        private void Build()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "BumpPet";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.45f;
            go.transform.position = transform.position + followOffset;
            var r = go.GetComponent<Renderer>();
            if (material != null) r.sharedMaterial = material;
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor(BaseColorId, color);
            mpb.SetColor(ColorId, color);
            r.SetPropertyBlock(mpb);
            _body = go.transform;
        }

        private void Update()
        {
            if (_body == null || !_body.gameObject.activeSelf) return;
            int level = Mathf.Clamp(BoosterUpgrades.PetLevel, 1, cooldowns.Length);

            Vector3 home = transform.position + followOffset;
            _hop = Mathf.MoveTowards(_hop, 0f, Time.deltaTime * 3f);
            Vector3 target = new Vector3(home.x, 0.35f + Mathf.Sin(_hop * Mathf.PI) * 0.8f + 0.05f * Mathf.Sin(Time.time * 5f), home.z);
            _body.position = Vector3.Lerp(_body.position, target, 1f - Mathf.Exp(-8f * Time.deltaTime));

            var state = GameServices.State;
            if (Time.time < _ready || _enemies == null || (state != null && state.IsGameWon)) return;
            if (_enemies.Nearest(transform.position, reaches[level - 1], _near, 1) == 0) return;
            EnemyFollow enemy = _near[0].GetComponent<EnemyFollow>();
            if (enemy == null || enemy.Unpushable) return;

            enemy.Push(enemy.transform.position - transform.position, pushDistance);
            _ready = Time.time + cooldowns[level - 1];
            _hop = 1f;
        }
    }
}
