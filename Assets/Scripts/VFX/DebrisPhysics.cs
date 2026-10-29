using System.Collections;
using UnityEngine;
using OrbitStrike.Systems;

namespace OrbitStrike.VFX
{
    /// <summary>
    /// Shattered fragments launched outward during target and rocket detonations.
    /// Recycles back into the object pool.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DebrisPhysics : MonoBehaviour
    {
        private Rigidbody _rb;
        private Coroutine _lifeRoutine;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public void ExplodeOutward(Vector3 origin, float force)
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();

            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            Vector3 randomDir = (transform.position - origin).normalized + Random.insideUnitSphere * 0.5f;
            _rb.AddForce(randomDir * force, ForceMode.Impulse);
            _rb.AddTorque(Random.insideUnitSphere * force * 10f, ForceMode.Impulse);

            if (_lifeRoutine != null) StopCoroutine(_lifeRoutine);
            _lifeRoutine = StartCoroutine(RecycleAfterDelay(3.0f));
        }

        private IEnumerator RecycleAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ObjectPooler.Instance?.Recycle("Debris", gameObject);
        }
    }
}

