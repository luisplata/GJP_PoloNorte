using UnityEngine;

namespace Basuras
{
    public class BasuraBehavour : MonoBehaviour
    {
        public BasuraData data;

        [Header("Movimiento de marea")]
        [Tooltip(
            "Dirección en la que se moverá este objeto en espacio mundial (vector 3D). Solo se usarán X y Z para simular la marea horizontal.")]
        public Vector3 moveDirection = Vector3.forward;

        [Tooltip("Velocidad en unidades por segundo")]
        public float speed = 1f;

        [Tooltip("Si está en true, usará Rigidbody (si existe) para mover; si no, moverá por transform.")]
        public bool useRigidbody = true;

        // Opcional: destruir después de cierto tiempo para evitar acumulación
        [Tooltip("Tiempo en segundos tras el spawn para autodestruir (0 = no destruir)")]
        public float autoDestroyAfter = 0f;

        private Rigidbody _rb;
        private float _lifeTimer = 0f;

        private void Awake()
        {
            // Normalizar la dirección para que la velocidad sea consistente
            if (moveDirection == Vector3.zero) moveDirection = Vector3.forward;

            // Forzar a plano XZ: ignorar componente Y para simular movimiento de marea horizontal
            moveDirection = new Vector3(moveDirection.x, 0f, moveDirection.z).normalized;

            _rb = GetComponent<Rigidbody>();

            //dale un nombre unico a la basura con un GUID
            if (data != null)
            {
                data.nombre = $"{System.Guid.NewGuid()}";
            }
        }

        private void OnEnable()
        {
            _lifeTimer = 0f;
        }

        private void Update()
        {
            if (useRigidbody && _rb != null) return; // movimiento por FixedUpdate con Rigidbody

            // Mover por transform cada frame
            transform.position += moveDirection * (speed * Time.deltaTime);

            HandleAutoDestroy();
        }

        private void FixedUpdate()
        {
            if (!useRigidbody || _rb == null) return;

            // Movimiento físico usando MovePosition para evitar tunneling y respetar colisiones
            Vector3 next = _rb.position + moveDirection * (speed * Time.fixedDeltaTime);
            _rb.MovePosition(next);

            HandleAutoDestroy();
        }

        private void HandleAutoDestroy()
        {
            if (autoDestroyAfter <= 0f) return;

            _lifeTimer += (useRigidbody && _rb != null) ? Time.fixedDeltaTime : Time.deltaTime;
            if (_lifeTimer >= autoDestroyAfter)
            {
                Debug.Log($"[Basura] Auto-destruyendo '{gameObject.name}' después de {_lifeTimer:F1}s");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Setea la dirección de movimiento en tiempo de ejecución. Se normaliza automáticamente.
        /// </summary>
        public void SetDirection(Vector3 dir)
        {
            if (dir == Vector3.zero)
            {
                Debug.LogWarning("[Basura] SetDirection recibió Vector3.zero; no se cambiará la dirección.");
                return;
            }

            // Forzar a plano XZ: ignorar componente Y para simular movimiento de marea horizontal
            moveDirection = new Vector3(dir.x, 0f, dir.z).normalized;
        }

        /// <summary>
        /// Setea la velocidad en tiempo de ejecución.
        /// </summary>
        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }
    }
}