using UnityEngine;
using OrbitStrike.Core;

namespace OrbitStrike.Data
{
    [CreateAssetMenu(fileName = "NewWorldConfig", menuName = "OrbitStrike/World Config")]
    public class WorldConfig : ScriptableObject
    {
        [Header("World Identity")]
        public WorldType worldType;
        public string worldName;
        [TextArea(2, 3)]
        public string worldDescription;
        public Color primaryColor;
        public Color ambientLightingColor;

        [Header("Physics Defaults")]
        public float gravity = -9.81f;
        public float typicalWindSpeed = 2.0f;

        [Header("Audio")]
        public AudioClip ambientMusic;
    }
}

