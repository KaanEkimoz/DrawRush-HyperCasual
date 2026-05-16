using System;
using UnityEngine;

namespace Studios208.DrawRush.Core
{
    /// <summary>
    /// Parameterless event channel. ScriptableObject so multiple systems can
    /// reference the same channel asset and stay decoupled.
    /// </summary>
    [CreateAssetMenu(fileName = "VoidEventChannel", menuName = "DrawRush/Events/Void Channel", order = 10)]
    public sealed class VoidEventChannel : ScriptableObject
    {
        public event Action Raised;

        public void Raise()
        {
            Raised?.Invoke();
        }
    }
}
