using UnityEngine;
using Studios208.DrawRush.Core;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Keeps a mesh facing the camera so its silhouette stays readable regardless of the
    /// camera's tilt — used by the paint-drop markers (a 3D drop read poorly when seen from
    /// the ~50° game camera). Aligns the object to the camera's rotation, then applies a
    /// fixed offset so the drop's tip points "up" on screen. Only rotates; position is
    /// untouched, so colliders/triggers are unaffected.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class BillboardToCamera : MonoBehaviour
    {
        [Tooltip("Local rotation offset applied after matching the camera, to orient the mesh " +
                 "(drop tip up). Tune in the inspector if the mesh's up axis differs.")]
        [SerializeField] private Vector3 offsetEuler = new Vector3(-90f, 0f, 0f);

        private Transform _cam;

        private void LateUpdate()
        {
            if (_cam == null)
            {
                _cam = GameServices.MainCamera;
                if (_cam == null) return;
            }
            transform.rotation = _cam.rotation * Quaternion.Euler(offsetEuler);
        }
    }
}
