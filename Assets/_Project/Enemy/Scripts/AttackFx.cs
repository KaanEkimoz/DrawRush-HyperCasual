using UnityEngine;

namespace DrawRush.Enemy
{
    /// <summary>
    /// A cartoon hit mark (a star burst, a slash, a shockwave ring) that pops where an attack
    /// lands and fades, so a strike reads even when the body move is small on screen.
    /// </summary>
    public sealed class AttackFx : MonoBehaviour
    {
        private const float PopTime = 0.07f;
        private const float Life = 0.32f;

        private SpriteRenderer _sprite;
        private float _born, _size;
        private bool _billboard;

        /// <summary>Show <paramref name="sprite"/> at <paramref name="at"/>, <paramref name="size"/> m wide.
        /// Flat on the page when <paramref name="flat"/>, else facing the camera.</summary>
        public static void Spawn(Sprite sprite, Vector3 at, float size, bool flat)
        {
            if (sprite == null) return;
            var go = new GameObject("AttackFx");
            var fx = go.AddComponent<AttackFx>();
            fx._sprite = go.AddComponent<SpriteRenderer>();
            fx._sprite.sprite = sprite;
            fx._sprite.sortingOrder = 50;
            fx._size = size / Mathf.Max(0.01f, Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
            fx._born = Time.time;
            fx._billboard = !flat;
            go.transform.SetPositionAndRotation(at, flat ? Quaternion.Euler(90f, Random.Range(0f, 360f), 0f) : Quaternion.identity);
            go.transform.localScale = Vector3.zero;
            fx.Update();
        }

        private void Update()
        {
            float age = Time.time - _born;
            if (age >= Life) { Destroy(gameObject); return; }
            float pop = age < PopTime ? Mathf.SmoothStep(0f, 1.15f, age / PopTime) : Mathf.Lerp(1.15f, 1f, (age - PopTime) / 0.08f);
            transform.localScale = Vector3.one * (_size * pop);
            float fade = Mathf.Clamp01((Life - age) / 0.15f);
            _sprite.color = new Color(1f, 1f, 1f, fade);
            if (_billboard && Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        }
    }
}
