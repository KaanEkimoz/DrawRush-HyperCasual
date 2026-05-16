using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Studios208.DrawRush.Core
{
    /// <summary>
    /// Marks a GameObject as persistent across scene loads. Duplicate detection by
    /// stable id (name + position + rotation, set once in Awake) — a second copy
    /// entering the scene self-destructs.
    ///
    /// Class was previously named <c>DontDestroyOnLoad</c>, which shadowed the
    /// Unity API of the same name. <see cref="MovedFromAttribute"/> preserves
    /// existing serialized references on scenes and prefabs.
    /// </summary>
    [MovedFrom(autoUpdateAPI: true, sourceNamespace: "Studios208.DrawRush.Core", sourceAssembly: "Studios208.DrawRush", sourceClassName: "DontDestroyOnLoad")]
    [DefaultExecutionOrder(-500)]
    public sealed class PersistentObject : MonoBehaviour
    {
        [HideInInspector, SerializeField] private string objectID;

        private void Awake()
        {
            objectID = $"{name}|{transform.position}|{transform.eulerAngles}";
        }

        private void Start()
        {
            var existing = Object.FindObjectsByType<PersistentObject>(FindObjectsSortMode.None);
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
