using System;
using TMPro;
using UnityEngine;

namespace DrawRush.Collection
{
    /// <summary>The "12/34" under a collection button. It lives on panels that hide and show, so
    /// it reads the count each time it appears.</summary>
    public sealed class CollectionCountLabel : MonoBehaviour
    {
        private TMP_Text _label;
        private Func<string> _text;

        public void Bind(TMP_Text label, Func<string> text)
        {
            _label = label;
            _text = text;
            OnEnable();
        }

        private void OnEnable()
        {
            if (_label != null) _label.text = _text();
        }
    }
}
