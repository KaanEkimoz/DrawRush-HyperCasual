using UnityEngine;

namespace Studios208.DrawRush.Core
{
    /// <summary>
    /// Marks a GameObject as persistent across scene loads. Duplicate detection by stable
    /// id (name + position + rotation, set once in Awake) — a second copy entering the
    /// scene self-destructs. Class name kept (was DontDestroyOnLoad) to preserve scene /
    /// prefab references; the Unity API of the same name is invoked via UnityEngine.Object.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class DontDestroyOnLoad : MonoBehaviour
    {
        [HideInInspector] public string objectID;

        private void Awake()
        {
            objectID = $"{name}|{transform.position}|{transform.eulerAngles}";
        }

        private void Start()
        {
            var existing = Object.FindObjectsByType<DontDestroyOnLoad>(FindObjectsSortMode.None);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != this && existing[i].objectID == objectID)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            Object.DontDestroyOnLoad(gameObject);
        }
    }
}
