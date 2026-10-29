#pragma warning disable 0618
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace OrbitStrikeAR
{
    // ========================================================================
    // 1. ENUMS & DATA DEFINITIONS (5 WORLDS, 4 ROCKETS, 4 TARGET TIERS)
    // ========================================================================
    public enum RocketArchetype { ViperBasic, TitanHeavy, MachSpeedster, IonPrecision }
    public enum TargetTier { ConcentricBullseye, ShieldedForcefield, HoverDrone, BossDreadnought }
    public enum PlanetaryWorld { MojaveEarth, FrostpeakArctic, NeoMetropolis, LunarBaseAlpha, MarsOlympus }

    [System.Serializable]
    public class RocketStats
    {
        public string Name;
        public RocketArchetype Type;
        public float Mass;
        public float ThrustMultiplier;
        public float BurnDuration;
        public float BlastRadius;
        public int DirectDamage;
        public Color PrimaryColor;
        public Color GlowColor;
    }

    [System.Serializable]
    public class WorldProfile
    {
        public string WorldName;
        public PlanetaryWorld WorldType;
        public float GravityY;
        public Vector2 WindSpeedRange;
        public Color SkyTint;
        public Color PlatformGlow;
    }

    // ========================================================================
    // 2. PROCEDURAL SYNTH AUDIO & CAMERA SHAKE (ZERO EXTERNAL CLIPS NEEDED)
    // ========================================================================
    public class ProceduralAudioAndFX : MonoBehaviour
    {
        public static ProceduralAudioAndFX Instance;
        private AudioSource _sfxSource;
        private Transform _camTransform;
        private float _shakeIntensity;

        private void Awake()
        {
            Instance = this;
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.spatialBlend = 0f;
            if (Camera.main != null) _camTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (_shakeIntensity > 0.001f && _camTransform != null)
            {
                _camTransform.localPosition += Random.insideUnitSphere * _shakeIntensity;
                _shakeIntensity = Mathf.Lerp(_shakeIntensity, 0f, Time.deltaTime * 10f);
            }
        }

        public void TriggerShake(float intensity) => _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);

        public void PlayBeep(float freq = 880f, float duration = 0.12f)
        {
            int samples = Mathf.CeilToInt(44100 * duration);
            AudioClip clip = AudioClip.Create("Beep", samples, 1, 44100, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / 44100f;
                float env = 1f - ((float)i / samples);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.35f;
            }
            clip.SetData(data, 0);
            _sfxSource.PlayOneShot(clip);
        }

        public void PlayExplosionSound(bool isBossOrBig)
        {
            float duration = isBossOrBig ? 0.75f : 0.45f;
            int samples = Mathf.CeilToInt(44100 * duration);
            AudioClip clip = AudioClip.Create("Boom", samples, 1, 44100, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float env = Mathf.Pow(1f - ((float)i / samples), 1.8f);
                float subBass = Mathf.Sin(2f * Mathf.PI * 55f * (i / 44100f));
                data[i] = (Random.Range(-1f, 1f) * 0.6f + subBass * 0.4f) * env * 0.6f;
            }
            clip.SetData(data, 0);
            _sfxSource.PlayOneShot(clip);
        }
    }

    // ========================================================================
    // 3. FLOATING 3D COMBAT TEXT (+100 BULLSEYE, SHIELD CRACKED, ETC.)
    // ========================================================================
    public class FloatingCombatText : MonoBehaviour
    {
        private TextMesh _textMesh;
        private float _timer;

        public static void Spawn(Vector3 worldPos, string message, Color color, float scale = 1f)
        {
            GameObject go = new GameObject("FloatingCombatText");
            go.transform.position = worldPos + Vector3.up * 0.12f;
            go.transform.localScale = Vector3.one * (0.015f * scale);

            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = message;
            tm.fontSize = 64;
            tm.characterSize = 0.2f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;

            FloatingCombatText fct = go.AddComponent<FloatingCombatText>();
            fct._textMesh = tm;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            transform.position += Vector3.up * (Time.deltaTime * 0.35f);
            if (Camera.main != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
            }
            if (_timer > 1.3f) Destroy(gameObject);
        }
    }

    // ========================================================================
    // 4. MULTI-TIER INTERACTIVE TARGET SYSTEM (BULLSEYE, SHIELD, DRONE, BOSS)
    // ========================================================================
    public class InteractiveTarget : MonoBehaviour
    {
        public TargetTier Tier;
        public int MaxHP = 100;
        public int CurrentHP = 100;
        public bool HasActiveShield;

        private GameObject _shieldDome;
        private Transform _healthBarFill;
        private Transform _turretOrRotor;
        private Vector3 _anchorLocalPos;
        private float _moveSeed;

        public void Setup(TargetTier tier, Vector3 localOffset)
        {
            Tier = tier;
            _anchorLocalPos = localOffset;
            transform.localPosition = localOffset;
            _moveSeed = Random.Range(0f, 100f);

            switch (tier)
            {
                case TargetTier.ConcentricBullseye:
                    MaxHP = 100;
                    HasActiveShield = false;
                    BuildBullseyeGeometry();
                    break;
                case TargetTier.ShieldedForcefield:
                    MaxHP = 100;
                    HasActiveShield = true;
                    BuildBullseyeGeometry();
                    BuildForcefieldShield();
                    break;
                case TargetTier.HoverDrone:
                    MaxHP = 120;
                    HasActiveShield = false;
                    BuildHoverDroneGeometry();
                    break;
                case TargetTier.BossDreadnought:
                    MaxHP = 350;
                    HasActiveShield = true;
                    BuildBossDreadnoughtGeometry();
                    BuildForcefieldShield(0.36f);
                    break;
            }
            CurrentHP = MaxHP;
            Build3DHealthBar();
        }

        private void Update()
        {
            // Hover Drone & Boss dynamic movement
            if (Tier == TargetTier.HoverDrone)
            {
                float xWave = Mathf.Sin((Time.time + _moveSeed) * 2.2f) * 0.35f;
                float yWave = Mathf.Cos((Time.time + _moveSeed) * 3.1f) * 0.12f;
                transform.localPosition = _anchorLocalPos + new Vector3(xWave, 0.25f + yWave, 0f);
                if (_turretOrRotor != null) _turretOrRotor.Rotate(0f, 720f * Time.deltaTime, 0f, Space.Self);
            }
            else if (Tier == TargetTier.BossDreadnought)
            {
                float xWave = Mathf.Sin((Time.time + _moveSeed) * 1.1f) * 0.25f;
                transform.localPosition = _anchorLocalPos + new Vector3(xWave, 0.15f, 0f);
                if (_turretOrRotor != null) _turretOrRotor.Rotate(0f, 45f * Time.deltaTime, 0f, Space.Self);
            }
        }

        public bool ApplyRocketHit(Vector3 worldHitPoint, int baseDamage, out int scoreAwarded, out string hitLabel)
        {
            scoreAwarded = 0;

            // 1. Check Forcefield Shield First
            if (HasActiveShield)
            {
                HasActiveShield = false;
                if (_shieldDome != null) Destroy(_shieldDome);
                scoreAwarded = 50;
                hitLabel = "SHIELD SHATTERED! (+50)";
                return false; // Target survives, shield down
            }

            // 2. Calculate Concentric Accuracy Multiplier
            float distFromCenter = Vector3.Distance(worldHitPoint, transform.position + Vector3.up * 0.2f);
            int accuracyBonus = distFromCenter < 0.06f ? 100 : (distFromCenter < 0.13f ? 50 : 25);
            CurrentHP -= baseDamage;

            if (_healthBarFill != null)
            {
                float pct = Mathf.Clamp01((float)CurrentHP / MaxHP);
                _healthBarFill.localScale = new Vector3(pct, 1f, 1f);
            }

            if (CurrentHP <= 0)
            {
                scoreAwarded = accuracyBonus + (Tier == TargetTier.BossDreadnought ? 300 : 50);
                hitLabel = accuracyBonus == 100 ? $"BULLSEYE DESTRUCTION! (+{scoreAwarded})" : $"TARGET DESTROYED! (+{scoreAwarded})";
                return true;
            }
            else
            {
                scoreAwarded = accuracyBonus;
                hitLabel = $"ARMOR HIT -{baseDamage} HP (+{scoreAwarded})";
                return false;
            }
        }

        private void BuildBullseyeGeometry()
        {
            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(stand.GetComponent<Collider>());
            stand.transform.SetParent(transform, false);
            stand.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            stand.transform.localScale = new Vector3(0.04f, 0.1f, 0.04f);
            stand.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatDarkMetal;

            GameObject discRoot = new GameObject("Discs");
            discRoot.transform.SetParent(transform, false);
            discRoot.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            discRoot.transform.localRotation = Quaternion.Euler(78f, 0f, 0f);

            CreateRing(discRoot.transform, 0.30f, 0f, LegendaryAssetBuilder.MatNeonCyan);
            CreateRing(discRoot.transform, 0.20f, -0.005f, LegendaryAssetBuilder.MatGoldGlow);
            CreateRing(discRoot.transform, 0.09f, -0.010f, LegendaryAssetBuilder.MatCrimsonGlow);

            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.center = new Vector3(0f, 0.22f, 0f);
            sc.radius = 0.17f;
        }

        private void CreateRing(Transform parent, float diameter, float yOffset, Material mat)
        {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(cyl.GetComponent<Collider>());
            cyl.transform.SetParent(parent, false);
            cyl.transform.localPosition = new Vector3(0f, yOffset, 0f);
            cyl.transform.localScale = new Vector3(diameter, 0.008f, diameter);
            cyl.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private void BuildForcefieldShield(float radius = 0.24f)
        {
            _shieldDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(_shieldDome.GetComponent<Collider>());
            _shieldDome.transform.SetParent(transform, false);
            _shieldDome.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            _shieldDome.transform.localScale = Vector3.one * (radius * 2f);
            _shieldDome.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatForcefield;
        }

        private void BuildHoverDroneGeometry()
        {
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(core.GetComponent<Collider>());
            core.transform.SetParent(transform, false);
            core.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            core.transform.localScale = new Vector3(0.18f, 0.12f, 0.18f);
            core.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatDarkMetal;

            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(eye.GetComponent<Collider>());
            eye.transform.SetParent(core.transform, false);
            eye.transform.localPosition = new Vector3(0f, 0f, -0.45f);
            eye.transform.localScale = new Vector3(0.45f, 0.45f, 0.3f);
            eye.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatCrimsonGlow;

            GameObject rotor = new GameObject("RotorHub");
            rotor.transform.SetParent(transform, false);
            rotor.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            _turretOrRotor = rotor.transform;

            for (int i = 0; i < 4; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(blade.GetComponent<Collider>());
                blade.transform.SetParent(_turretOrRotor, false);
                blade.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
                blade.transform.localScale = new Vector3(0.32f, 0.008f, 0.03f);
                blade.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatNeonCyan;
            }

            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.center = new Vector3(0f, 0.2f, 0f);
            sc.radius = 0.16f;
        }

        private void BuildBossDreadnoughtGeometry()
        {
            GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(hull.GetComponent<Collider>());
            hull.transform.SetParent(transform, false);
            hull.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            hull.transform.localScale = new Vector3(0.42f, 0.16f, 0.26f);
            hull.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatDarkMetal;

            GameObject reactor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(reactor.GetComponent<Collider>());
            reactor.transform.SetParent(transform, false);
            reactor.transform.localPosition = new Vector3(0f, 0.22f, -0.12f);
            reactor.transform.localScale = new Vector3(0.16f, 0.16f, 0.1f);
            reactor.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatCrimsonGlow;

            GameObject radar = new GameObject("BossTurret");
            radar.transform.SetParent(transform, false);
            radar.transform.localPosition = new Vector3(0f, 0.34f, 0f);
            _turretOrRotor = radar.transform;

            GameObject dish = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(dish.GetComponent<Collider>());
            dish.transform.SetParent(_turretOrRotor, false);
            dish.transform.localScale = new Vector3(0.28f, 0.05f, 0.08f);
            dish.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatGoldGlow;

            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.22f, 0f);
            bc.size = new Vector3(0.46f, 0.28f, 0.3f);
        }

        private void Build3DHealthBar()
        {
            GameObject barRoot = new GameObject("HealthBar3D");
            barRoot.transform.SetParent(transform, false);
            barRoot.transform.localPosition = new Vector3(0f, 0.44f, 0f);

            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(bg.GetComponent<Collider>());
            bg.transform.SetParent(barRoot.transform, false);
            bg.transform.localScale = new Vector3(0.26f, 0.025f, 0.01f);
            bg.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatDarkMetal;

            GameObject fillPivot = new GameObject("FillPivot");
            fillPivot.transform.SetParent(barRoot.transform, false);
            fillPivot.transform.localScale = Vector3.one;
            _healthBarFill = fillPivot.transform;

            GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(fill.GetComponent<Collider>());
            fill.transform.SetParent(_healthBarFill, false);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.005f);
            fill.transform.localScale = new Vector3(0.25f, 0.02f, 0.012f);
            fill.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatEmeraldGlow;
        }
    }

    // ========================================================================
    // 5. PROCEDURAL ASSET & SHADER BUILDER (PBR METALLICS, SCORCH CRATERS, DEBRIS)
    // ========================================================================
    public static class LegendaryAssetBuilder
    {
        public static Material MatDarkMetal;
        public static Material MatTitanium;
        public static Material MatNeonCyan;
        public static Material MatCrimsonGlow;
        public static Material MatGoldGlow;
        public static Material MatEmeraldGlow;
        public static Material MatForcefield;

        public static void InitializeMaterials()
        {
            if (MatDarkMetal != null) return;
            MatDarkMetal = CreateMat(new Color(0.13f, 0.15f, 0.19f), 0.9f, 0.75f);
            MatTitanium = CreateMat(new Color(0.92f, 0.95f, 0.98f), 0.75f, 0.9f);
            MatNeonCyan = CreateMat(new Color(0f, 0.9f, 1f), 0.4f, 0.9f, new Color(0f, 0.85f, 1f), 2.6f);
            MatCrimsonGlow = CreateMat(new Color(1f, 0.12f, 0.25f), 0.5f, 0.9f, new Color(1f, 0.1f, 0.2f), 2.8f);
            MatGoldGlow = CreateMat(new Color(1f, 0.72f, 0.05f), 0.6f, 0.85f, new Color(1f, 0.65f, 0f), 2.4f);
            MatEmeraldGlow = CreateMat(new Color(0.05f, 1f, 0.45f), 0.4f, 0.9f, new Color(0f, 1f, 0.4f), 2.5f);
            MatForcefield = CreateMat(new Color(0f, 0.65f, 1f, 0.35f), 0.1f, 1f, new Color(0f, 0.55f, 1f), 1.8f);
        }

        public static Material CreateMat(Color baseCol, float metallic, float smoothness, Color? emission = null, float emissionPower = 2f)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            Material m = new Material(sh);
            m.color = baseCol;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseCol);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);

            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                Color hdr = emission.Value * Mathf.Pow(2f, emissionPower);
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", hdr);
            }
            return m;
        }

        public static GameObject CreateRocketByArchetype(RocketStats stats, out ParticleSystem exhaustPlume)
        {
            InitializeMaterials();
            GameObject root = new GameObject($"Rocket_{stats.Name}");

            Material bodyMat = CreateMat(stats.PrimaryColor, 0.8f, 0.9f);
            Material glowMat = CreateMat(stats.GlowColor, 0.3f, 0.95f, stats.GlowColor, 2.8f);

            float widthScale = stats.Type == RocketArchetype.TitanHeavy ? 0.062f : (stats.Type == RocketArchetype.MachSpeedster ? 0.036f : 0.046f);
            float heightScale = stats.Type == RocketArchetype.MachSpeedster ? 0.095f : 0.075f;

            // Fuselage
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, heightScale + 0.02f, 0f);
            body.transform.localScale = new Vector3(widthScale, heightScale, widthScale);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;

            // Nosecone
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(nose.GetComponent<Collider>());
            nose.transform.SetParent(root.transform, false);
            nose.transform.localPosition = new Vector3(0f, heightScale * 2f + 0.015f, 0f);
            nose.transform.localScale = new Vector3(widthScale, heightScale * 1.25f, widthScale);
            nose.GetComponent<Renderer>().sharedMaterial = glowMat;

            // 4 Swept Delta Fins
            for (int i = 0; i < 4; i++)
            {
                GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(fin.GetComponent<Collider>());
                fin.transform.SetParent(root.transform, false);
                float rad = i * 90f * Mathf.Deg2Rad;
                fin.transform.localPosition = new Vector3(Mathf.Cos(rad) * (widthScale * 0.65f), 0.035f, Mathf.Sin(rad) * (widthScale * 0.65f));
                fin.transform.localRotation = Quaternion.Euler(12f, -i * 90f, 0f);
                fin.transform.localScale = new Vector3(0.028f, 0.048f, 0.005f);
                fin.GetComponent<Renderer>().sharedMaterial = glowMat;
            }

            // Thruster Plume
            GameObject vfxObj = new GameObject("RocketPlumeVFX");
            vfxObj.transform.SetParent(root.transform, false);
            vfxObj.transform.localPosition = Vector3.zero;
            vfxObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            exhaustPlume = vfxObj.AddComponent<ParticleSystem>();
            var main = exhaustPlume.main;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = exhaustPlume.emission;
            emission.rateOverTime = 110f;

            var shape = exhaustPlume.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.012f;

            vfxObj.GetComponent<ParticleSystemRenderer>().sharedMaterial = glowMat;

            // Physics
            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.1f, 0f);
            col.radius = widthScale * 0.6f;
            col.height = 0.22f;

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = stats.Mass;
            rb.drag = 0.12f;
            rb.angularDrag = 2.5f;
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            return root;
        }

        public static void SpawnDetonationAndDebris(Vector3 pos, Color glowColor, bool spawnCrater)
        {
            InitializeMaterials();

            // 1. Particle Fireball
            GameObject vfx = new GameObject("ExplosionFireball");
            vfx.transform.position = pos;
            ParticleSystem ps = vfx.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 5.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.gravityModifier = 0.5f;
            main.loop = false;

            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 75) });
            vfx.GetComponent<ParticleSystemRenderer>().sharedMaterial = CreateMat(glowColor, 0.2f, 1f, glowColor, 3.2f);
            ps.Play();
            Object.Destroy(vfx, 1.5f);

            // 2. Flying Physics Shrapnel Debris (10 cubes with Rigidbody)
            for (int i = 0; i < 10; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.transform.position = pos + Random.insideUnitSphere * 0.06f;
                shard.transform.localScale = Vector3.one * Random.Range(0.015f, 0.035f);
                shard.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? MatDarkMetal : CreateMat(glowColor, 0.5f, 0.9f, glowColor, 2.2f);
                Rigidbody rb = shard.AddComponent<Rigidbody>();
                rb.mass = 0.1f;
                rb.AddExplosionForce(Random.Range(80f, 160f), pos, 0.6f, 0.3f);
                Object.Destroy(shard, Random.Range(1.2f, 2.2f));
            }

            // 3. Scorch Crater Decal on Miss/Ground Hit
            if (spawnCrater)
            {
                GameObject crater = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.Destroy(crater.GetComponent<Collider>());
                crater.transform.position = pos + Vector3.up * 0.003f;
                crater.transform.localScale = new Vector3(0.22f, 0.002f, 0.22f);
                crater.GetComponent<Renderer>().sharedMaterial = CreateMat(new Color(0.05f, 0.05f, 0.05f), 0.1f, 0.2f, new Color(1f, 0.25f, 0f), 0.8f);
                Object.Destroy(crater, 10f);
            }
        }
    }

    // ========================================================================
    // 6. ACTIVE BALLISTIC ROCKET PHYSICS COMPONENT
    // ========================================================================
    public class ActiveRocketProjectile : MonoBehaviour
    {
        private Rigidbody _rb;
        private ParticleSystem _plume;
        private RocketStats _stats;
        private Vector3 _gravity;
        private Vector3 _wind;
        private float _burnLeft;
        private float _sustainedAccel;
        private bool _inFlight;

        public System.Action<Collision, Vector3> OnImpactCallback;

        public void Setup(RocketStats stats, ParticleSystem plume)
        {
            _stats = stats;
            _plume = plume;
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
        }

        public void Ignite(Vector3 initialVel, float sustainedAccel, Vector3 gravity, Vector3 wind)
        {
            transform.SetParent(null, true);
            _rb.isKinematic = false;
            _rb.useGravity = false;
            _rb.velocity = initialVel;

            _sustainedAccel = sustainedAccel;
            _burnLeft = _stats.BurnDuration;
            _gravity = gravity;
            _wind = wind;
            _inFlight = true;

            if (_plume != null) _plume.Play();
            Destroy(gameObject, 8f);
        }

        private void FixedUpdate()
        {
            if (!_inFlight) return;

            // Planetary Gravity + Crosswind Drift
            _rb.AddForce(_gravity + _wind, ForceMode.Acceleration);

            // Sustained Rocket Engine Burn
            if (_burnLeft > 0f)
            {
                _burnLeft -= Time.fixedDeltaTime;
                _rb.AddForce(transform.up * _sustainedAccel, ForceMode.Acceleration);
                if (_burnLeft <= 0f && _plume != null) _plume.Stop();
            }

            // Aerodynamic Fin Stabilization Torque
            if (_rb.velocity.sqrMagnitude > 0.05f)
            {
                Quaternion lookRot = Quaternion.LookRotation(_rb.velocity.normalized) * Quaternion.Euler(90f, 0f, 0f);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, lookRot, Time.fixedDeltaTime * 12f));
            }
        }

        private void OnCollisionEnter(Collision col)
        {
            if (!_inFlight) return;
            _inFlight = false;

            if (_plume != null)
            {
                _plume.Stop();
                _plume.transform.SetParent(null);
                Destroy(_plume.gameObject, 1.2f);
            }

            Vector3 pt = col.contacts.Length > 0 ? col.contacts[0].point : transform.position;
            OnImpactCallback?.Invoke(col, pt);
            Destroy(gameObject);
        }
    }

    // ========================================================================
    // 7. MASTER ORBIT STRIKE AR GAMEPLAY & DIORAMA CONTROLLER
    // ========================================================================
    public class OrbitStrikeLegendaryEngine : MonoBehaviour
    {
        // AR References
        private ARRaycastManager _raycastManager;
        private Camera _arCam;
        private static readonly List<ARRaycastHit> ArHits = new List<ARRaycastHit>();

        // Playspace & Diorama Hierarchy (Supports Pinch-to-Scale & Twist-to-Rotate)
        private GameObject _reticle;
        private Transform _playspaceRoot;
        private Transform _turretPivot;
        private Transform _rocketMount;
        private GameObject _obstacleWall;
        private InteractiveTarget _activeTarget;
        private ActiveRocketProjectile _stagedRocket;
        private LineRenderer _trajectoryRenderer;

        // Databases
        private List<RocketStats> _rocketArsenal;
        private List<WorldProfile> _worlds;
        private int _selectedRocketIdx = 0;
        private int _currentWorldIdx = 0;

        // Campaign & Ballistic State
        private bool _isPlaced = false;
        private bool _isLaunching = false;
        private int _currentLevel = 1;
        private int _totalScore = 0;
        private int _coins = 150;
        private float _thrustSliderVal = 6.8f;
        private float _pitchSliderVal = 55f;
        private float _yawSliderVal = 0f;
        private float _playspaceScale = 1.0f;
        private Vector3 _currentWind;
        private Vector3 _currentGravity;

        // UI Elements
        private Text _headerTelemetryText;
        private Text _missionStatusText;
        private Text _windGravityText;
        private GameObject _bottomHUD;
        private Button _launchBtn;
        private List<Button> _rocketButtons = new List<Button>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootMasterEngine()
        {
            if (Object.FindAnyObjectByType<OrbitStrikeLegendaryEngine>() == null)
            {
                GameObject engineGo = new GameObject("[ORBIT_STRIKE_LEGENDARY_ENGINE]");
                engineGo.AddComponent<OrbitStrikeLegendaryEngine>();
                Debug.Log("[OrbitStrikeLegendaryEngine] Master Engine automatically initialized.");
            }
        }

        private void Awake()
        {
            LegendaryAssetBuilder.InitializeMaterials();
            gameObject.AddComponent<ProceduralAudioAndFX>();

            _arCam = Camera.main;
            if (_arCam == null)
            {
#if UNITY_2023_1_OR_NEWER
                _arCam = Object.FindAnyObjectByType<Camera>();
#else
                _arCam = Object.FindObjectOfType<Camera>();
#endif
                if (_arCam == null)
                {
                    GameObject camGo = new GameObject("AR_Main_Camera");
                    camGo.tag = "MainCamera";
                    _arCam = camGo.AddComponent<Camera>();
                    camGo.AddComponent<AudioListener>();
                }
            }

#if UNITY_EDITOR
            // CRITICAL EDITOR SAFEGUARD: Disable ARCameraBackground so screen is NEVER black in Editor!
            var bg = _arCam.GetComponent<ARCameraBackground>();
            if (bg != null) bg.enabled = false;

            _arCam.clearFlags = CameraClearFlags.SolidColor;
            _arCam.backgroundColor = new Color(0.06f, 0.08f, 0.14f);

            // Position camera to look down at the diorama in Editor mode
            if (_arCam.transform.parent != null)
            {
                _arCam.transform.localPosition = new Vector3(0f, 1.6f, -2.4f);
                _arCam.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
            }
            else
            {
                _arCam.transform.position = new Vector3(0f, 1.6f, -2.4f);
                _arCam.transform.rotation = Quaternion.Euler(24f, 0f, 0f);
            }

            // Editor visual simulation floor
            if (GameObject.Find("AR_Simulation_Floor") == null)
            {
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "AR_Simulation_Floor";
                floor.transform.position = new Vector3(0f, -0.55f, 0f);
                floor.transform.localScale = new Vector3(3f, 1f, 3f);
                var mr = floor.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = LegendaryAssetBuilder.MatDarkMetal;
            }
#endif

#if UNITY_2023_1_OR_NEWER
            _raycastManager = Object.FindAnyObjectByType<ARRaycastManager>();
#else
            _raycastManager = Object.FindObjectOfType<ARRaycastManager>();
#endif

            InitializeDatabases();
            SetupSceneLighting();
            CreateReticleAndTrajectoryLine();
            BuildCommandHUD();
            ApplyWorldEnvironment(0);
        }

        private void Start()
        {
#if UNITY_EDITOR
            // In Unity Editor, automatically deploy diorama after 0.25s so user never waits on black screen
            StartCoroutine(AutoDeployInEditor());
#endif
        }

#if UNITY_EDITOR
        private IEnumerator AutoDeployInEditor()
        {
            yield return new WaitForSeconds(0.25f);
            if (!_isPlaced)
            {
                DeployPlayspaceDiorama(new Pose(new Vector3(0f, -0.55f, 0f), Quaternion.identity));
            }
        }
#endif

        private void InitializeDatabases()
        {
            _rocketArsenal = new List<RocketStats>
            {
                new RocketStats { Name = "VIPER-I", Type = RocketArchetype.ViperBasic, Mass = 1.0f, ThrustMultiplier = 1.0f, BurnDuration = 0.55f, BlastRadius = 0.25f, DirectDamage = 100, PrimaryColor = new Color(0.92f, 0.95f, 1f), GlowColor = new Color(0f, 0.9f, 1f) },
                new RocketStats { Name = "TITAN-HVY", Type = RocketArchetype.TitanHeavy, Mass = 1.8f, ThrustMultiplier = 0.85f, BurnDuration = 0.75f, BlastRadius = 0.45f, DirectDamage = 175, PrimaryColor = new Color(0.25f, 0.28f, 0.32f), GlowColor = new Color(1f, 0.45f, 0f) },
                new RocketStats { Name = "MACH-X", Type = RocketArchetype.MachSpeedster, Mass = 0.7f, ThrustMultiplier = 1.35f, BurnDuration = 0.4f, BlastRadius = 0.20f, DirectDamage = 90, PrimaryColor = new Color(1f, 0.15f, 0.25f), GlowColor = new Color(1f, 0.85f, 0f) },
                new RocketStats { Name = "ION-PRO", Type = RocketArchetype.IonPrecision, Mass = 0.9f, ThrustMultiplier = 1.15f, BurnDuration = 0.85f, BlastRadius = 0.30f, DirectDamage = 130, PrimaryColor = new Color(0.1f, 0.8f, 0.5f), GlowColor = new Color(0f, 1f, 0.55f) }
            };

            _worlds = new List<WorldProfile>
            {
                new WorldProfile { WorldName = "WORLD 1: MOJAVE RANGE (EARTH)", WorldType = PlanetaryWorld.MojaveEarth, GravityY = -9.81f, WindSpeedRange = new Vector2(-0.3f, 0.3f), SkyTint = new Color(1f, 0.95f, 0.85f), PlatformGlow = new Color(0f, 0.9f, 1f) },
                new WorldProfile { WorldName = "WORLD 2: FROSTPEAK GLACIER", WorldType = PlanetaryWorld.FrostpeakArctic, GravityY = -9.81f, WindSpeedRange = new Vector2(-1.4f, 1.4f), SkyTint = new Color(0.8f, 0.95f, 1f), PlatformGlow = new Color(0.2f, 0.7f, 1f) },
                new WorldProfile { WorldName = "WORLD 3: NEO-METROPOLIS", WorldType = PlanetaryWorld.NeoMetropolis, GravityY = -9.81f, WindSpeedRange = new Vector2(-0.9f, 0.9f), SkyTint = new Color(0.95f, 0.8f, 1f), PlatformGlow = new Color(1f, 0.1f, 0.65f) },
                new WorldProfile { WorldName = "WORLD 4: LUNAR BASE ALPHA (MOON)", WorldType = PlanetaryWorld.LunarBaseAlpha, GravityY = -1.62f, WindSpeedRange = Vector2.zero, SkyTint = new Color(0.85f, 0.85f, 0.95f), PlatformGlow = new Color(0f, 1f, 0.55f) },
                new WorldProfile { WorldName = "WORLD 5: OLYMPUS CITADEL (MARS)", WorldType = PlanetaryWorld.MarsOlympus, GravityY = -3.71f, WindSpeedRange = new Vector2(-0.7f, 0.7f), SkyTint = new Color(1f, 0.75f, 0.6f), PlatformGlow = new Color(1f, 0.35f, 0.05f) }
            };
        }

        private void SetupSceneLighting()
        {
            Light dirLight = FindObjectOfType<Light>();
            if (dirLight == null)
            {
                GameObject lObj = new GameObject("Directional_Sun");
                dirLight = lObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }
            dirLight.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            dirLight.intensity = 1.35f;
        }

        private void CreateReticleAndTrajectoryLine()
        {
            // Holographic Reticle
            _reticle = new GameObject("AR_Placement_Reticle");
            for (int i = 0; i < 12; i++)
            {
                GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(seg.GetComponent<Collider>());
                seg.transform.SetParent(_reticle.transform, false);
                float a = i * 30f * Mathf.Deg2Rad;
                seg.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.14f, 0.003f, Mathf.Sin(a) * 0.14f);
                seg.transform.localRotation = Quaternion.Euler(0f, -i * 30f, 0f);
                seg.transform.localScale = new Vector3(0.01f, 0.003f, 0.04f);
                seg.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatNeonCyan;
            }

            // Trajectory Predictor LineRenderer
            GameObject lineGo = new GameObject("TrajectoryPredictorLine");
            _trajectoryRenderer = lineGo.AddComponent<LineRenderer>();
            _trajectoryRenderer.positionCount = 35;
            _trajectoryRenderer.startWidth = 0.016f;
            _trajectoryRenderer.endWidth = 0.004f;
            _trajectoryRenderer.useWorldSpace = true;
            _trajectoryRenderer.sharedMaterial = LegendaryAssetBuilder.MatNeonCyan;
            _trajectoryRenderer.enabled = false;
        }

        private void Update()
        {
            if (!_isPlaced)
            {
                UpdateARPlacementReticle();
            }
            else
            {
                HandleARPinchAndTwistGestures();
                UpdateLaunchPadAim();
                RenderBallisticTrajectory();
            }
        }

        // ====================================================================
        // STEP 1: AR SURFACE DETECTION, PLACEMENT & PINCH/TWIST GESTURES
        // ====================================================================
        private void UpdateARPlacementReticle()
        {
            Pose hitPose = Pose.identity;
            bool hitFound = false;
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            if (_raycastManager != null && _raycastManager.Raycast(center, ArHits, TrackableType.PlaneWithinPolygon))
            {
                hitPose = ArHits[0].pose;
                hitFound = true;
            }
            else if (_arCam != null)
            {
                Plane editorFloor = new Plane(Vector3.up, new Vector3(0f, -0.55f, 0f));
                Ray r = _arCam.ScreenPointToRay(center);
                if (editorFloor.Raycast(r, out float d) && d < 7f)
                {
                    Vector3 pt = r.GetPoint(d);
                    Vector3 fwd = Vector3.ProjectOnPlane(_arCam.transform.forward, Vector3.up).normalized;
                    hitPose = new Pose(pt, Quaternion.LookRotation(fwd));
                    hitFound = true;
                }
            }

            if (hitFound)
            {
                _reticle.SetActive(true);
                _reticle.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
                _reticle.transform.Rotate(0f, 60f * Time.deltaTime, 0f, Space.Self);

                bool isOverUI = EventSystem.current != null && (
                    (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) ||
                    EventSystem.current.IsPointerOverGameObject()
                );

                bool tapped = (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began && !isOverUI)
                              || (Input.GetMouseButtonDown(0) && !isOverUI);

                if (tapped)
                {
                    DeployPlayspaceDiorama(hitPose);
                }
            }
        }

        private void DeployPlayspaceDiorama(Pose pose)
        {
            _isPlaced = true;
            _reticle.SetActive(false);
            ProceduralAudioAndFX.Instance.PlayBeep(1046f, 0.15f);

            // Root container for Pinch-to-Scale & Twist-to-Rotate
            GameObject rootGo = new GameObject("OrbitStrike_AR_Playspace");
            _playspaceRoot = rootGo.transform;
            _playspaceRoot.SetPositionAndRotation(pose.position, pose.rotation);

            // Build Launch Pad Platform
            GameObject basePlatform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlatform.transform.SetParent(_playspaceRoot, false);
            basePlatform.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            basePlatform.transform.localScale = new Vector3(0.36f, 0.015f, 0.36f);
            basePlatform.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatDarkMetal;

            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(rim.GetComponent<Collider>());
            rim.transform.SetParent(_playspaceRoot, false);
            rim.transform.localPosition = new Vector3(0f, 0.016f, 0f);
            rim.transform.localScale = new Vector3(0.38f, 0.005f, 0.38f);
            rim.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatNeonCyan;

            // Articulated Turret & Rail
            GameObject pivotGo = new GameObject("TurretPivot");
            pivotGo.transform.SetParent(_playspaceRoot, false);
            pivotGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            _turretPivot = pivotGo.transform;

            GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(rail.GetComponent<Collider>());
            rail.transform.SetParent(_turretPivot, false);
            rail.transform.localPosition = new Vector3(0f, 0.12f, -0.045f);
            rail.transform.localScale = new Vector3(0.022f, 0.24f, 0.022f);
            rail.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatGoldGlow;

            GameObject mountGo = new GameObject("RocketMount");
            mountGo.transform.SetParent(_turretPivot, false);
            mountGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            _rocketMount = mountGo.transform;

            // Build Level Target & Spawn First Rocket
            BuildCampaignStage(_currentLevel);
            SpawnRocketOnPad();

            _bottomHUD.SetActive(true);
            _missionStatusText.text = "PINCH TO SCALE • AIM SLIDERS • TAP LAUNCH!";
        }

        private void HandleARPinchAndTwistGestures()
        {
            if (_playspaceRoot == null) return;

            // Mobile Two-Finger Pinch & Twist
            if (Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                Vector2 prev0 = t0.position - t0.deltaPosition;
                Vector2 prev1 = t1.position - t1.deltaPosition;

                float prevDist = (prev0 - prev1).magnitude;
                float currDist = (t0.position - t1.position).magnitude;
                if (prevDist > 1f)
                {
                    float scaleFactor = currDist / prevDist;
                    _playspaceScale = Mathf.Clamp(_playspaceScale * scaleFactor, 0.25f, 2.0f);
                    _playspaceRoot.localScale = Vector3.one * _playspaceScale;
                }

                float prevAngle = Mathf.Atan2(prev1.y - prev0.y, prev1.x - prev0.x) * Mathf.Rad2Deg;
                float currAngle = Mathf.Atan2(t1.position.y - t0.position.y, t1.position.x - t0.position.x) * Mathf.Rad2Deg;
                _playspaceRoot.Rotate(0f, -(currAngle - prevAngle), 0f, Space.Self);
            }

            // Editor Mouse Scroll Scale & Right-Click Drag Rotate
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _playspaceScale = Mathf.Clamp(_playspaceScale + scroll * 0.5f, 0.25f, 2.0f);
                _playspaceRoot.localScale = Vector3.one * _playspaceScale;
            }
            if (Input.GetMouseButton(1))
            {
                _playspaceRoot.Rotate(0f, Input.GetAxis("Mouse X") * 4f, 0f, Space.Self);
            }
        }

        // ====================================================================
        // STEP 2: CAMPAIGN LEVEL GENERATOR & ROCKET ARCHETYPE SPAWNER
        // ====================================================================
        private void ApplyWorldEnvironment(int worldIndex)
        {
            _currentWorldIdx = worldIndex % _worlds.Count;
            WorldProfile wp = _worlds[_currentWorldIdx];
            _currentGravity = new Vector3(0f, wp.GravityY, 0f);
            float windX = Random.Range(wp.WindSpeedRange.x, wp.WindSpeedRange.y);
            _currentWind = new Vector3(windX, 0f, 0f);

            if (_windGravityText != null)
            {
                string windDir = Mathf.Abs(windX) < 0.05f ? "CALM" : (windX > 0 ? $"EAST +{windX:F1}m/s" : $"WEST {windX:F1}m/s");
                _windGravityText.text = $"{wp.WorldName}   |   GRAVITY: {wp.GravityY:F2}m/s²   |   WIND: {windDir}";
            }
        }

        private void BuildCampaignStage(int level)
        {
            // Switch Planetary World every 2 levels for rapid showcase variety
            ApplyWorldEnvironment((level - 1) / 2);

            if (_activeTarget != null) Destroy(_activeTarget.gameObject);
            if (_obstacleWall != null) Destroy(_obstacleWall);

            // Select Target Tier based on progression
            TargetTier tier = TargetTier.ConcentricBullseye;
            if (level % 4 == 2) tier = TargetTier.ShieldedForcefield;
            else if (level % 4 == 3) tier = TargetTier.HoverDrone;
            else if (level % 4 == 0) tier = TargetTier.BossDreadnought;

            float distZ = Random.Range(1.5f, 2.4f);
            float offsetX = Random.Range(-0.35f, 0.35f);
            Vector3 targetLocalPos = new Vector3(offsetX, 0f, distZ);

            GameObject targetGo = new GameObject($"Stage_{level}_Target");
            targetGo.transform.SetParent(_playspaceRoot, false);
            _activeTarget = targetGo.AddComponent<InteractiveTarget>();
            _activeTarget.Setup(tier, targetLocalPos);

            // Spawn Protective Obstacle Barrier on Level 3+ (forces high-arc shots)
            if (level >= 3 && tier != TargetTier.HoverDrone)
            {
                _obstacleWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _obstacleWall.name = "Obstacle_Barrier";
                _obstacleWall.transform.SetParent(_playspaceRoot, false);
                _obstacleWall.transform.localPosition = new Vector3(offsetX * 0.5f, 0.14f, distZ * 0.52f);
                _obstacleWall.transform.localScale = new Vector3(0.32f, 0.28f, 0.04f);
                _obstacleWall.GetComponent<Renderer>().sharedMaterial = LegendaryAssetBuilder.MatDarkMetal;
            }

            UpdateHeaderTelemetry();
        }

        private void SpawnRocketOnPad()
        {
            if (_rocketMount == null) return;
            if (_stagedRocket != null) Destroy(_stagedRocket.gameObject);

            RocketStats stats = _rocketArsenal[_selectedRocketIdx];
            GameObject rGo = LegendaryAssetBuilder.CreateRocketByArchetype(stats, out ParticleSystem plume);
            rGo.transform.SetParent(_rocketMount, false);
            rGo.transform.localPosition = Vector3.zero;
            rGo.transform.localRotation = Quaternion.identity;

            _stagedRocket = rGo.AddComponent<ActiveRocketProjectile>();
            _stagedRocket.Setup(stats, plume);
            _stagedRocket.OnImpactCallback += OnRocketCollision;

            _isLaunching = false;
            if (_launchBtn != null) _launchBtn.interactable = true;
            _trajectoryRenderer.enabled = true;
        }

        // ====================================================================
        // STEP 3: FORCE/ANGLE SLIDERS & REAL-TIME BALLISTIC TRAJECTORY ARC
        // ====================================================================
        private void UpdateLaunchPadAim()
        {
            if (_turretPivot == null) return;
            float tiltFromVertical = 90f - _pitchSliderVal;
            _turretPivot.localRotation = Quaternion.Euler(tiltFromVertical, _yawSliderVal, 0f);
        }

        private void RenderBallisticTrajectory()
        {
            if (_isLaunching || _stagedRocket == null || !_trajectoryRenderer.enabled) return;

            RocketStats stats = _rocketArsenal[_selectedRocketIdx];
            Vector3 pos = _rocketMount.position;
            Vector3 dir = _rocketMount.up;

            float effectiveForce = _thrustSliderVal * stats.ThrustMultiplier * _playspaceScale;
            Vector3 vel = dir * effectiveForce;
            float sustainedAccel = effectiveForce * 0.65f;
            float dt = 0.04f;

            for (int i = 0; i < _trajectoryRenderer.positionCount; i++)
            {
                _trajectoryRenderer.SetPosition(i, pos);
                float t = i * dt;
                Vector3 accel = (_currentGravity + _currentWind) * _playspaceScale;
                if (t < stats.BurnDuration) accel += dir * sustainedAccel;

                vel += accel * dt;
                pos += vel * dt;
            }
        }

        // ====================================================================
        // STEP 4: 3-2-1 COUNTDOWN, AUDIO BEEPS & IGNITION LIFTOFF
        // ====================================================================
        private void TriggerLaunchSequence()
        {
            if (_isLaunching || _stagedRocket == null) return;
            StartCoroutine(LaunchCountdownRoutine());
        }

        private IEnumerator LaunchCountdownRoutine()
        {
            _isLaunching = true;
            _launchBtn.interactable = false;
            _trajectoryRenderer.enabled = false;

            string[] steps = { "IGNITION IN 3...", "IGNITION IN 2...", "IGNITION IN 1..." };
            for (int i = 0; i < steps.Length; i++)
            {
                _missionStatusText.text = steps[i];
                ProceduralAudioAndFX.Instance.PlayBeep(660f + i * 220f, 0.1f);
                yield return new WaitForSeconds(0.32f);
            }

            _missionStatusText.text = "LIFTOFF! BALLISTIC TRACKING ACTIVE";
            ProceduralAudioAndFX.Instance.PlayBeep(1320f, 0.22f);
            ProceduralAudioAndFX.Instance.TriggerShake(0.015f);

            RocketStats stats = _rocketArsenal[_selectedRocketIdx];
            Vector3 dir = _rocketMount.up;
            float effectiveForce = _thrustSliderVal * stats.ThrustMultiplier * _playspaceScale;

            _stagedRocket.Ignite(
                dir * effectiveForce,
                effectiveForce * 0.65f,
                _currentGravity * _playspaceScale,
                _currentWind * _playspaceScale
            );
        }

        // ====================================================================
        // STEP 5: TARGET COLLISION, SHIELD/BOSS DAMAGE, CRATERS & SCORING
        // ====================================================================
        private void OnRocketCollision(Collision col, Vector3 hitPoint)
        {
            RocketStats stats = _rocketArsenal[_selectedRocketIdx];
            InteractiveTarget hitTarget = col.gameObject.GetComponentInParent<InteractiveTarget>();

            // Also check Blast Radius splash damage if rocket hits right next to target
            if (hitTarget == null && _activeTarget != null)
            {
                float splashDist = Vector3.Distance(hitPoint, _activeTarget.transform.position + Vector3.up * 0.2f);
                if (splashDist <= stats.BlastRadius * _playspaceScale)
                {
                    hitTarget = _activeTarget;
                }
            }

            if (hitTarget != null)
            {
                bool destroyed = hitTarget.ApplyRocketHit(hitPoint, stats.DirectDamage, out int pts, out string label);
                _totalScore += pts;
                _coins += pts / 2;

                ProceduralAudioAndFX.Instance.PlayExplosionSound(destroyed);
                ProceduralAudioAndFX.Instance.TriggerShake(destroyed ? 0.045f : 0.025f);
                LegendaryAssetBuilder.SpawnDetonationAndDebris(hitPoint, stats.GlowColor, false);
                FloatingCombatText.Spawn(hitPoint, label, Color.yellow, 1.2f);

                if (destroyed)
                {
                    _currentLevel++;
                    _missionStatusText.text = $"{label} — ADVANCING TO STAGE {_currentLevel}!";
                    Destroy(hitTarget.gameObject);
                    StartCoroutine(RespawnStageAndRocket(1.5f, true));
                }
                else
                {
                    _missionStatusText.text = $"{label} — RELOADING ROCKET!";
                    StartCoroutine(RespawnStageAndRocket(1.1f, false));
                }
            }
            else
            {
                ProceduralAudioAndFX.Instance.PlayExplosionSound(false);
                ProceduralAudioAndFX.Instance.TriggerShake(0.018f);
                LegendaryAssetBuilder.SpawnDetonationAndDebris(hitPoint, new Color(1f, 0.35f, 0.05f), true);
                FloatingCombatText.Spawn(hitPoint, "MISSED TARGET!", new Color(1f, 0.3f, 0.3f), 1.0f);
                _missionStatusText.text = "IMPACT MISS! ADJUST THRUST OR ANGLE & FIRE AGAIN.";
                StartCoroutine(RespawnStageAndRocket(1.1f, false));
            }

            UpdateHeaderTelemetry();
        }

        private IEnumerator RespawnStageAndRocket(float delay, bool advanceStage)
        {
            yield return new WaitForSeconds(delay);
            if (advanceStage) BuildCampaignStage(_currentLevel);
            SpawnRocketOnPad();
        }

        private void UpdateHeaderTelemetry()
        {
            if (_headerTelemetryText != null)
            {
                string tierName = _activeTarget != null ? _activeTarget.Tier.ToString().ToUpper() : "TARGET";
                _headerTelemetryText.text = $"STAGE {_currentLevel:00} [{tierName}]   |   SCORE: {_totalScore:0000}   |   CREDITS: {_coins} CR";
            }
        }

        // ====================================================================
        // PROCEDURAL COMMAND HUD (HANGAR ARCHETYPE BAR + SLIDERS + TELEMETRY)
        // ====================================================================
        private void BuildCommandHUD()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            GameObject canvasGo = new GameObject("OrbitStrike_CommandHUD");
            Canvas c = canvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = canvasGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
            {
                var fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0) font = fonts[0];
            }

            // Top Telemetry Panel
            GameObject topPanel = MakePanel(canvasGo.transform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1020f, 175f), new Color(0.03f, 0.07f, 0.13f, 0.88f));
            _headerTelemetryText = MakeText(topPanel.transform, "STAGE 01 [BULLSEYE]   |   SCORE: 0000   |   CREDITS: 150 CR", 28, new Color(0f, 0.95f, 1f), new Vector2(0f, 50f), font);
            _windGravityText = MakeText(topPanel.transform, "WORLD 1: MOJAVE RANGE   |   GRAVITY: -9.81m/s²", 22, new Color(1f, 0.8f, 0.2f), new Vector2(0f, 5f), font);
            _missionStatusText = MakeText(topPanel.transform, "SCAN FLOOR & TAP SCREEN TO DEPLOY LAUNCH PAD", 25, Color.white, new Vector2(0f, -45f), font);

            // Bottom Control & Hangar Deck
            _bottomHUD = MakePanel(canvasGo.transform, new Vector2(0.5f, 0f), new Vector2(0f, 310f), new Vector2(1020f, 560f), new Color(0.03f, 0.07f, 0.13f, 0.90f));

            // Rocket Archetype Selector Bar (4 Archetypes)
            MakeText(_bottomHUD.transform, "SELECT ROCKET ARCHETYPE (HANGAR):", 22, new Color(0.7f, 0.85f, 1f), new Vector2(0f, 235f), font);
            for (int i = 0; i < _rocketArsenal.Count; i++)
            {
                int idx = i;
                RocketStats rs = _rocketArsenal[i];
                float xPos = -360f + (i * 240f);
                GameObject btnGo = MakePanel(_bottomHUD.transform, new Vector2(0.5f, 0.5f), new Vector2(xPos, 175f), new Vector2(220f, 68f), idx == 0 ? new Color(0f, 0.65f, 0.85f, 1f) : new Color(0.14f, 0.2f, 0.28f, 1f));
                Button b = btnGo.AddComponent<Button>();
                MakeText(btnGo.transform, $"{rs.Name}\nDMG:{rs.DirectDamage}", 20, Color.white, Vector2.zero, font);
                _rocketButtons.Add(b);

                b.onClick.AddListener(() =>
                {
                    _selectedRocketIdx = idx;
                    for (int k = 0; k < _rocketButtons.Count; k++)
                    {
                        _rocketButtons[k].GetComponent<Image>().color = (k == idx) ? new Color(0f, 0.65f, 0.85f, 1f) : new Color(0.14f, 0.2f, 0.28f, 1f);
                    }
                    ProceduralAudioAndFX.Instance.PlayBeep(980f, 0.08f);
                    SpawnRocketOnPad();
                });
            }

            // Ballistic Sliders (Thrust, Angle, Azimuth Yaw)
            MakeSlider(_bottomHUD.transform, "THRUST POWER", 3.5f, 13.0f, _thrustSliderVal, new Vector2(0f, 80f), font, v => _thrustSliderVal = v);
            MakeSlider(_bottomHUD.transform, "LAUNCH PITCH", 15f, 85f, _pitchSliderVal, new Vector2(0f, -5f), font, v => _pitchSliderVal = v);
            MakeSlider(_bottomHUD.transform, "AZIMUTH YAW", -55f, 55f, _yawSliderVal, new Vector2(0f, -90f), font, v => _yawSliderVal = v);

            // Launch Button
            GameObject launchGo = MakePanel(_bottomHUD.transform, new Vector2(0.5f, 0.5f), new Vector2(-130f, -195f), new Vector2(620f, 95f), new Color(1f, 0.26f, 0.08f, 1f));
            _launchBtn = launchGo.AddComponent<Button>();
            _launchBtn.onClick.AddListener(TriggerLaunchSequence);
            MakeText(launchGo.transform, "IGNITE & LAUNCH STRIKE", 32, Color.white, Vector2.zero, font);

            // Switch Planetary World Button
            GameObject worldBtnGo = MakePanel(_bottomHUD.transform, new Vector2(0.5f, 0.5f), new Vector2(330f, -195f), new Vector2(240f, 95f), new Color(0.18f, 0.35f, 0.55f, 1f));
            Button worldBtn = worldBtnGo.AddComponent<Button>();
            MakeText(worldBtnGo.transform, "NEXT WORLD\nGRAVITY", 22, new Color(0f, 0.95f, 1f), Vector2.zero, font);
            worldBtn.onClick.AddListener(() =>
            {
                ApplyWorldEnvironment(_currentWorldIdx + 1);
                ProceduralAudioAndFX.Instance.PlayBeep(740f, 0.1f);
            });

            _bottomHUD.SetActive(false);
        }

        private GameObject MakePanel(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color col)
        {
            GameObject go = new GameObject("UI_Panel");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.color = col;
            return go;
        }

        private Text MakeText(Transform parent, string str, int size, Color col, Vector2 pos, Font font)
        {
            GameObject go = new GameObject("UI_Text");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(960f, 70f);
            rt.anchoredPosition = pos;
            Text t = go.AddComponent<Text>();
            t.font = font;
            t.text = str;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = col;
            return t;
        }

        private void MakeSlider(Transform parent, string label, float min, float max, float init, Vector2 pos, Font font, UnityEngine.Events.UnityAction<float> callback)
        {
            GameObject row = new GameObject(label + "_Row");
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(920f, 65f);
            rt.anchoredPosition = pos;

            Text lbl = MakeText(row.transform, $"{label}: {init:F1}", 24, new Color(0f, 0.92f, 1f), new Vector2(-280f, 0f), font);
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(340f, 60f);

            GameObject bg = MakePanel(row.transform, new Vector2(0.5f, 0.5f), new Vector2(140f, 0f), new Vector2(540f, 26f), new Color(0.14f, 0.22f, 0.32f, 1f));
            GameObject fill = MakePanel(bg.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0f, 0.85f, 1f, 0.9f));
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;

            GameObject handle = MakePanel(bg.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 46f), Color.white);

            Slider s = bg.AddComponent<Slider>();
            s.fillRect = fillRt;
            s.handleRect = handle.GetComponent<RectTransform>();
            s.minValue = min;
            s.maxValue = max;
            s.value = init;
            s.onValueChanged.AddListener(v =>
            {
                lbl.text = $"{label}: {v:F1}";
                callback?.Invoke(v);
            });
        }
    }
}
