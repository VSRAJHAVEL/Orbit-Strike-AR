using System;
using UnityEngine;

namespace OrbitStrike.Data
{
    /// <summary>
    /// ScriptableObject variable representing a shared 3D vector (e.g. Wind Velocity, Gravity Vector).
    /// </summary>
    [CreateAssetMenu(fileName = "NewVector3Variable", menuName = "OrbitStrike/Architecture/Vector3 Variable")]
    public class Vector3Variable : ScriptableObject
    {
        [SerializeField] private Vector3 initialValue;
        [NonSerialized] private Vector3 _runtimeValue;

        public Vector3 Value
        {
            get => _runtimeValue;
            set
            {
                _runtimeValue = value;
                OnValueChanged?.Invoke(_runtimeValue);
            }
        }

        public event Action<Vector3> OnValueChanged;

        private void OnEnable()
        {
            _runtimeValue = initialValue;
        }

        public void SetValue(Vector3 value) => Value = value;
    }
}

