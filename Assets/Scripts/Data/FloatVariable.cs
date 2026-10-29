using System;
using UnityEngine;

namespace OrbitStrike.Data
{
    /// <summary>
    /// ScriptableObject variable representing a shared float value.
    /// Allows designers to tune live physics parameters (e.g. Gravity, Drag, Thrust) in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFloatVariable", menuName = "OrbitStrike/Architecture/Float Variable")]
    public class FloatVariable : ScriptableObject
    {
        [SerializeField] private float initialValue;
        [NonSerialized] private float _runtimeValue;

        public float Value
        {
            get => _runtimeValue;
            set
            {
                _runtimeValue = value;
                OnValueChanged?.Invoke(_runtimeValue);
            }
        }

        public event Action<float> OnValueChanged;

        private void OnEnable()
        {
            _runtimeValue = initialValue;
        }

        public void SetValue(float value) => Value = value;
        public void ApplyChange(float delta) => Value += delta;
    }
}

