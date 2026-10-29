using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitStrike.Data
{
    /// <summary>
    /// ScriptableObject-based event channel following Unity Architect guidelines.
    /// Provides zero-allocation, decoupled cross-system messaging without static singletons.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "OrbitStrike/Architecture/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<Action> _listeners = new List<Action>();

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i]?.Invoke();
            }
        }

        public void RegisterListener(Action listener)
        {
            if (!_listeners.Contains(listener)) _listeners.Add(listener);
        }

        public void UnregisterListener(Action listener)
        {
            if (_listeners.Contains(listener)) _listeners.Remove(listener);
        }
    }
}

