using DrawRush.Core;
using UnityEngine;

namespace DrawRush.Enemy
{
    /// <summary>
    /// A patch of glue the glue bottle leaves behind. The hero moves slower while standing in it
    /// (<see cref="SlowZones"/>). It dries and fades after a few seconds.
    /// </summary>
    public sealed class GluePuddle : MonoBehaviour
    {
        private float _radius, _slow, _born, _life;
        private SpriteRenderer _sprite;
        private Color _color;

        public static GluePuddle Spawn(Sprite sprite, Vector3 at, float radius, float slow, float life)
        {
            var go = new GameObject("GluePuddle");
            var puddle = go.AddComponent<GluePuddle>();
            puddle._sprite = go.AddComponent<SpriteRenderer>();
            puddle._sprite.sprite = sprite;
            puddle._sprite.sortingOrder = -50;
            float size = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
            go.transform.SetPositionAndRotation(new Vector3(at.x, 0.025f, at.z), Quaternion.Euler(90f, Random.Range(0f, 360f), 0f));
            go.transform.localScale = Vector3.one * (radius * 2f / Mathf.Max(0.01f, size));
            puddle._radius = radius;
            puddle._slow = slow;
            puddle._life = life;
            puddle._born = Time.time;
            puddle._color = puddle._sprite.color;
            return puddle;
        }

        private void Update()
        {
            float age = Time.time - _born;
            if (age >= _life) { Destroy(gameObject); return; }
            float fade = Mathf.Clamp01((_life - age) / 1f);
            _sprite.color = new Color(_color.r, _color.g, _color.b, _color.a * fade);

            Transform player = GameServices.Player;
            if (player == null) return;
            Vector3 d = player.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < _radius * _radius) SlowZones.Touch(_slow);
        }
    }
}
