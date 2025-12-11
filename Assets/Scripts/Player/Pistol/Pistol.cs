using Icebergs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Pistol
{
    public class Pistol : MonoBehaviour
    {
        [Header("References")] public Camera cam; // Cámara desde la que sale el rayo (si es null usará Camera.main)
        public LineRenderer laserLine; // LineRenderer que representa el láser (2 puntos)

        [Tooltip("Si se asigna, el láser partirá desde este Transform en lugar del centro de la cámara.")]
        public Transform laserOrigin;

        [Header("Laser Settings")] public float laserMaxDistance = 100f;
        public LayerMask hitLayers = ~0;

        public float throwSpeed = 20f; // velocidad mientras avanza

        // Estado del botón (presionado)
        private bool _isHolding;

        [Header("Impact")]
        [Tooltip("Multiplicador aplicado a la velocidad de lanzamiento para calcular el 'amount' pasado a IIceberg.Rise(amount)")]
        public float riseMultiplier = 0.5f;

        // Último punto impactado predicho por el raycast (para ajustar el láser)
        private Vector3 _lastPredictedHit;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (laserLine != null)
            {
                laserLine.positionCount = 2;
                laserLine.enabled = false;
                // Hacer que el Laser tenga propiedades similares al guide
                laserLine.startWidth = 0.04f;
                laserLine.endWidth = 0.04f;
                laserLine.material = new Material(Shader.Find("Sprites/Default"));
                laserLine.startColor = Color.cyan;
                laserLine.endColor = Color.cyan;
                laserLine.useWorldSpace = true;
            }
        }

        void Update()
        {
            if (cam == null) return;

            // Mostrar/ocultar el láser mientras se mantiene (controlado por OnFire callback)
            if (laserLine != null)
            {
                laserLine.enabled = _isHolding;
                if (_isHolding)
                {
                    // Dibujamos el láser hacia el último objetivo calculado en LateUpdate
                    Vector3 origin = (laserOrigin != null) ? laserOrigin.position : cam.transform.position;
                    SetLaserPositions(origin, _lastPredictedHit);
                }
            }
        }

        // Método para conectar con Input System: bindea la acción de disparo a este callback.
        // - context.started -> empieza a mantener
        // - context.performed -> evento de disparo (se usa para alternar lanzar/recoger)
        // - context.canceled -> deja de mantener
        public void OnFire(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                _isHolding = true;
            }

            if (context.canceled)
            {
                // Ocultamos el láser
                _isHolding = false;
            }
        }

        // LateUpdate: hacemos los raycasts de validación al final del frame para tener información
        // estable antes de dibujar/soltar. Primero raycast desde la cámara centro para obtener aimPoint;
        // luego comprobamos si hay obstrucción entre el origen físico (laserOrigin) y ese punto y
        // ajustamos `_lastPredictedHit` al primer obstáculo encontrado (si lo hay).
        void LateUpdate()
        {
            if (!_isHolding || cam == null) return;

            // Raycast desde el centro de la cámara para saber hacia dónde apunta la mira
            Ray camRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            Vector3 aimPoint;
            bool camHit = Physics.Raycast(camRay, out RaycastHit camHitInfo, laserMaxDistance, hitLayers);
            if (camHit)
            {
                aimPoint = camHitInfo.point;
            }
            else
            {
                aimPoint = camRay.origin + camRay.direction * laserMaxDistance;
            }

            // Ahora comprobamos obstrucción entre el origen físico y el aimPoint
            Vector3 origin = (laserOrigin != null) ? laserOrigin.position : cam.transform.position;
            Vector3 toAim = aimPoint - origin;
            float dist = toAim.magnitude;
            if (dist <= 1e-6f)
            {
                _lastPredictedHit = origin + camRay.direction * laserMaxDistance;
                return;
            }

            Vector3 dir = toAim / dist;
            if (Physics.Raycast(origin, dir, out RaycastHit blockHit, dist, hitLayers))
            {
                // Hay un obstáculo entre el origen y el punto de mira: apuntamos a ese obstáculo
                _lastPredictedHit = blockHit.point;
                // Mientras mantenemos, si el obstáculo es un iceberg lo hacemos subir de forma continua
                float amount = throwSpeed * riseMultiplier * Time.deltaTime;
                HandleHoldImpact(blockHit, amount);
            }
            else
            {
                // No hay obstáculo entre origen y objetivo: apuntamos al aimPoint
                _lastPredictedHit = aimPoint;
                // También comprobamos si al apuntar al aimPoint hay un iceberg (por ejemplo detectado por la cámara)
                float amount = throwSpeed * riseMultiplier * Time.deltaTime;
                if (camHit)
                {
                    HandleHoldImpact(camHitInfo, amount);
                }
            }
        }

        private void SetLaserPositions(Vector3 start, Vector3 end)
        {
            if (laserLine == null) return;
            laserLine.SetPosition(0, start);
            laserLine.SetPosition(1, end);
        }


        // Llamar a Rise en IIceberg si el collider golpeado pertenece a uno
        private void HandleImpact(RaycastHit hit)
        {
            if (hit.collider == null) return;
            // GetComponentInParent<T>() requires T : Component, so we search for MonoBehaviours
            // that implement IIceberg and call Rise on the first one found.
            try
            {
                var mbs = hit.collider.GetComponentsInParent<MonoBehaviour>(true);
                foreach (var mb in mbs)
                {
                    if (mb is IIceberg iceberg)
                    {
                        float amount = throwSpeed * riseMultiplier;
                        Debug.Log($"HandMediator: Impact detected on IIceberg '{mb.name}', calling Rise({amount})");
                        iceberg.Rise(amount);
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("HandleImpact: error calling IIceberg.Rise - " + ex.Message);
            }
        }

        // Manejo continuo de impacto mientras se mantiene el botón (llamado cada frame con amount en unidades por frame)
        private void HandleHoldImpact(RaycastHit hit, float amount)
        {
            if (hit.collider == null) return;
            var mbs = hit.collider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (var mb in mbs)
            {
                if (mb is IIceberg iceberg)
                {
                    iceberg.Rise(amount);
                    break;
                }
            }
        }
    }
}