using System;
using UnityEngine;

namespace Studios208.DrawRush.Core
{
    [CreateAssetMenu(fileName = "IntEventChannel", menuName = "DrawRush/Events/Int Channel", order = 11)]
    public sealed class IntEventChannel : ScriptableObject
    {
        public event Action<int> Raised;

        public void Raise(int value)
        {
            Raised?.Invoke(value);
        }
    }
}
