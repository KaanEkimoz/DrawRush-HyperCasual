using UnityEngine;
using UnityEngine.Serialization;
using DrawRush.Core;
using DrawRush.Player;

namespace DrawRush.Enemy
{
    /// <summary>
    /// Hits the player who stands inside the red cone on the ground in front of it, so the danger
    /// you see is exactly the danger there is: from behind an enemy cannot touch you. Plays death
    /// anim when the game is won. Self-subscribes to <see cref="GameState.GameWonChanged"/> instead of being
    /// poked by GameManager — inverts the legacy Core → Enemy dependency.
    /// </summary>
    public sealed class EnemyCombat : MonoBehaviour
    {
        [SerializeField] private Animator enemyAnim;

        [Tooltip("Damage applied on touch (positive magnitude). If 0, uses GameConfig.enemyTouchDamage.")]
        [FormerlySerializedAs("damage")]
        [SerializeField] private int damageOverride;

        [Header("Knockback")]
        [Tooltip("Push speed (units/sec) applied to the player on contact, decaying to 0 over " +
                 "PlayerKnockback.knockbackDuration. 0 disables knockback.")]
        [SerializeField] private float knockbackForce = 9f;

        [Header("Being shoved")]
        [Tooltip("Metres a shield throws back the enemy that hit it.")]
        [SerializeField] private float shieldRepel = 3f;
        [Tooltip("Metres the hero shoves a frozen enemy by walking into it.")]
        [SerializeField] private float frozenShove = 2f;
        [Tooltip("Touching distance (m) per unit of enemy scale.")]
        [SerializeField] private float touchPerScale = 1f;

        [Header("Attack cone")]
        [Tooltip("Cone length (m) per unit of enemy scale, so big enemies reach further.")]
        [SerializeField] private float reachPerScale = 1.4f;
        [Tooltip("Reach multiple for the Giant Eraser, on top of its size.")]
        [SerializeField] private float bossReachScale = 0.8f;
        [Tooltip("Full width of the cone (degrees).")]
        [SerializeField, Range(30f, 360f)] private float coneAngle = 100f;
        [SerializeField] private Color coneColor = new(0.92f, 0.16f, 0.16f, 0.3f);
        [Tooltip("World height of the cone: on the page, over the drawn lines.")]
        [SerializeField] private float coneY = 0.03f;

        [Header("Attack move")]
        [Tooltip("The attack move starts when the hero is this many cone lengths away.")]
        [SerializeField] private float swingReach = 1.35f;
        [SerializeField] private float swingCooldown = 0.9f;

        private float _nextSwing;

        private const int ConeSegments = 24;
        private const float RimStart = 0.93f;

        private PlayerCombat _playerCombat;
        private GameState _state;
        private EnemyFollow _follow;
        private Transform _cone;
        private MeshRenderer _coneRenderer;
        private Mesh _coneMesh;
        private Material _coneMaterial;

        // A dead hero is left alone: the enemies just stand there while the pencil falls.
        private bool CanHit => _follow != null && _follow.CanAttack
                               && (GameServices.Health == null || GameServices.Health.Current > 0);

        private float Reach => reachPerScale * transform.lossyScale.x
                               * (_follow != null && _follow.Variant == EnemyVariant.Boss ? bossReachScale : 1f);

        /// <summary>True when <paramref name="target"/> is within <paramref name="reach"/> m of
        /// <paramref name="origin"/> and no more than <paramref name="halfAngle"/> degrees off
        /// <paramref name="forward"/>, measured on the ground.</summary>
        public static bool InCone(Vector3 origin, Vector3 forward, Vector3 target, float reach, float halfAngle)
        {
            Vector3 to = target - origin;
            to.y = 0f;
            forward.y = 0f;
            if (to.sqrMagnitude > reach * reach) return false;
            return to.sqrMagnitude < 1e-6f || Vector3.Angle(forward, to) <= halfAngle;
        }

        private void Awake()
        {
            if (enemyAnim == null)
            {
                enemyAnim = GetComponentInChildren<Animator>();
            }
            _follow = GetComponent<EnemyFollow>();
            BuildCone();
        }

        // Not a child: the enemy tints and blinks all its renderers, and the cone must not join in.
        private void BuildCone()
        {
            var go = new GameObject(name + "_Cone", typeof(MeshFilter), typeof(MeshRenderer));
            _cone = go.transform;
            _coneMesh = ConeMesh(coneAngle, coneColor);
            go.GetComponent<MeshFilter>().sharedMesh = _coneMesh;
            _coneMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Auto_EnemyCone", renderQueue = 3050 };
            _coneRenderer = go.GetComponent<MeshRenderer>();
            _coneRenderer.sharedMaterial = _coneMaterial;
            _coneRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _coneRenderer.receiveShadows = false;
            go.SetActive(false);
        }

        // A unit fan pointing along +Z, soft inside with a stronger rim. A full circle has no
        // sides to close, every other width just ends at its two edges.
        private static Mesh ConeMesh(float angle, Color color)
        {
            var vertices = new Vector3[1 + (ConeSegments + 1) * 2];
            var colors = new Color[vertices.Length];
            var triangles = new int[ConeSegments * 9];
            Color soft = color, rim = new(color.r, color.g, color.b, Mathf.Min(1f, color.a * 2.2f));
            colors[0] = soft;
            for (int i = 0; i <= ConeSegments; i++)
            {
                float a = (i / (float)ConeSegments - 0.5f) * angle * Mathf.Deg2Rad;
                Vector3 dir = new(Mathf.Sin(a), 0f, Mathf.Cos(a));
                vertices[1 + i * 2] = dir * RimStart;
                vertices[2 + i * 2] = dir;
                colors[1 + i * 2] = soft;
                colors[2 + i * 2] = rim;
                if (i == ConeSegments) break;
                int t = i * 9, inner = 1 + i * 2;
                triangles[t] = 0; triangles[t + 1] = inner; triangles[t + 2] = inner + 2;
                triangles[t + 3] = inner; triangles[t + 4] = inner + 1; triangles[t + 5] = inner + 3;
                triangles[t + 6] = inner; triangles[t + 7] = inner + 3; triangles[t + 8] = inner + 2;
            }
            var mesh = new Mesh { name = "EnemyCone", vertices = vertices, colors = colors, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnEnable()
        {
            // Only needed for the win-guard in OnTriggerEnter; the death animation + VFX + sound are
            // owned by EnemyDeath, which handles both the win and the lose case.
            _state = GameServices.State;
        }

        private void OnDisable()
        {
            if (_cone != null) _cone.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_cone != null) Destroy(_cone.gameObject);
            Destroy(_coneMesh);
            Destroy(_coneMaterial);
        }

        private void Update()
        {
            Transform player = GameServices.Player;
            if (player == null) return;
            if (_follow != null && _follow.IsFrozen) { ShoveWhenTouched(player); return; }
            if (!CanHit) return;
            // Swing at a hero close in front, a little before the hit lands, so every hit has a move.
            if (Time.time >= _nextSwing && InCone(transform.position, transform.forward, player.position, Reach * swingReach, coneAngle * 0.5f))
            {
                _nextSwing = Time.time + swingCooldown;
                _follow.PlayAttack();
            }
            if (InCone(transform.position, transform.forward, player.position, Reach, coneAngle * 0.5f)) Hit(player);
        }

        /// <summary>Damage the hero from <paramref name="from"/> (a thrown chunk's landing point),
        /// with the same shield, invulnerability and knockback rules as a touch.</summary>
        public void HitPlayer(Transform player, Vector3 from)
        {
            if (GameServices.Health != null && !GameServices.Health.IsAlive) return;
            HitFrom(player, from, repel: false);
        }

        // A frozen enemy is just a block on the page: the hero barges it along.
        private void ShoveWhenTouched(Transform player)
        {
            if (_follow.SlideAmount > 0f || (_state != null && _state.IsGameWon)) return;
            if (GameServices.Health != null && !GameServices.Health.IsAlive) return;
            Vector3 away = transform.position - player.position;
            away.y = 0f;
            float touch = touchPerScale * transform.lossyScale.x;
            if (away.sqrMagnitude > touch * touch) return;
            _follow.Push(away, frozenShove, evenHeavy: true);
            _follow.Thaw();   // the shove breaks the ice: it skids, then comes back for you
            Haptics.Light();
        }

        private void LateUpdate()
        {
            bool show = CanHit;
            if (_cone.gameObject.activeSelf != show) _cone.gameObject.SetActive(show);
            if (!show) return;
            Vector3 p = transform.position;
            _cone.SetPositionAndRotation(new Vector3(p.x, coneY, p.z), Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            _cone.localScale = Vector3.one * Reach;
        }

        private void Hit(Transform other) => HitFrom(other, transform.position, repel: true);

        private void HitFrom(Transform other, Vector3 from, bool repel)
        {
            if (_state != null && _state.IsGameWon) return;

            // During the post-hit invulnerability window the player is intangible to enemies:
            // no damage, no knockback — contact does nothing at all.
            if (_playerCombat == null) _playerCombat = other.GetComponent<PlayerCombat>();
            if (_playerCombat != null && _playerCombat.IsInvulnerable) return;

            // A shield takes the hit and bounces the attacker off; the hero isn't moved.
            if (_playerCombat != null && _playerCombat.ShieldCharges > 0)
            {
                _playerCombat.TakeDamage(1);
                if (repel && _follow != null) _follow.Push(transform.position - other.position, shieldRepel, evenHeavy: true);
                return;
            }

            // Knock the player away from the enemy along the horizontal contact direction.
            if (knockbackForce > 0f)
            {
                var kb = other.GetComponent<PlayerKnockback>();
                if (kb != null)
                {
                    Vector3 dir = other.transform.position - from;
                    kb.ApplyKnockback(dir, knockbackForce);
                }
            }

            if (_playerCombat == null) return;

            int dmg = damageOverride > 0
                ? damageOverride
                : (GameServices.Config != null ? GameServices.Config.enemyTouchDamage : 1);
            _playerCombat.TakeDamage(Mathf.Abs(dmg));
        }
    }
}
