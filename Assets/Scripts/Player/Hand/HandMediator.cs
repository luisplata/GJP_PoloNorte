using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Icebergs;

namespace Player.Hand
{
    /// <summary>
    /// HandMediator (simplificado): comportamiento tipo gancho/hand
    /// - Press (Input.started) -> lanza la mano desde `handOrigin` hacia su forward
    /// - Hold -> la mano avanza en línea recta (sin gravedad) o en parábola si se configura
    /// - Release (Input.canceled) -> la mano vuelve al origen
    /// Opcional: usa Rigidbody para movimiento (constante) o mueve Transform directamente.
    /// </summary>
    public class HandMediator : MonoBehaviour
    {
        public Hand hand; // el objeto que representa la mano
        public Transform handOrigin; // punto de origen (p. ej. punta del arma)
        public RopeWithWave ropeStraight; // opcional: visual de cuerda

        [Header("Movement")] public bool useRigidbody = true; // si true usa Rigidbody; si false mueve Transform
        public float throwSpeed = 20f; // velocidad por defecto mientras avanza (si no se usa LaunchParams)
        public float returnSpeed = 25f; // velocidad de regreso
        public float maxDistance = 30f; // distancia máxima desde el origen
        public LayerMask hitLayers = ~0; // capas que bloquean el avance

        [Tooltip(
            "Si true, la mano usará gravedad al lanzarse (en Rigidbody) o se simulará verticalmente en Transform mode")]
        public bool applyGravityOnThrow = false;

        [Tooltip("Escala de la gravedad simulada cuando applyGravityOnThrow=true y useRigidbody=false")]
        public float gravityScale = 1f;

        [Header("Collision / Offset")]
        public float launchPositionOffset = 0.05f; // pequeño offset para situar la mano fuera del arma

        [Header("Impact")]
        [Tooltip(
            "Multiplicador aplicado a la velocidad de lanzamiento para calcular el 'amount' pasado a IIceberg.Rise(amount)")]
        public float riseMultiplier = 0.5f;

        // Nuevo: parámetros de lanzamiento configurables (útil para powerups)
        [System.Serializable]
        public struct LaunchParams
        {
            public float speed;        // magnitud inicial
            public float angleDeg;     // ángulo de elevación (grados)
            public bool useArc;        // si true hace lanzamiento parabólico, si false hace dirección plana
            public bool useGravity;    // si true habilita gravedad en Rigidbody o simulación
            public float gravityScale; // escala de gravedad para simulación
        }

        [Header("Launch Params")]
        public LaunchParams defaultLaunch = new LaunchParams { speed = 20f, angleDeg = 10f, useArc = false, useGravity = false, gravityScale = 1f };

        // estado actual de parámetros (pueden ser cambiados por powerups)
        private LaunchParams _launchParams;

        // Interno
        enum State
        {
            Idle,
            Thrown,
            Returning
        }

        State _state = State.Idle;

        Rigidbody _rb;
        bool _addedRigidbody;

        Transform _originalParent;
        Vector3 _originalLocalPos;
        Quaternion _originalLocalRot;

        Vector3 _originWorldPos;

        Vector3 _launchDir;   // dirección unit (world) hacia adelante (puede ser no horizontal si useArc)
        Vector3 _launchDirXZ; // dirección horizontal proyectada (XZ)
        float _verticalVelocity;

        // Para simulación ballistic en modo transform
        Vector3 _ballisticVelocity;

        void Awake()
        {
            // cache origin world position in Awake if possible
            if (handOrigin != null)
            {
                _originWorldPos = handOrigin.position;
            }

            // inicializar parámetros de lanzamiento
            _launchParams = defaultLaunch;

            hand.Configure(this);
        }

        void Update()
        {
            // Simple movement when throwing using transform (no collision checks here)
            if (_state == State.Thrown && !useRigidbody)
            {
                float dt = Time.deltaTime;
                // movimiento simple en la dirección de lanzamiento
                Vector3 delta = _launchDir * (_launchParams.speed * dt);
                hand.transform.position += delta;

                // check max distance
                if (Vector3.Distance(hand.transform.position, _originWorldPos) >= maxDistance)
                {
                    StartReturn();
                }
            }

            // Estado Returning: mover mano hacia origen
            if (_state == State.Returning)
            {
                float dt = Time.deltaTime;
                Vector3 target = handOrigin != null ? handOrigin.position : this.transform.position;
                // mueve con rapidez de returnSpeed
                Vector3 newPos = Vector3.MoveTowards(hand.transform.position, target, returnSpeed * dt);
                hand.transform.position = newPos;

                // si llegó, completar
                if (Vector3.Distance(hand.transform.position, target) <= 0.05f)
                {
                    ReturnComplete();
                }
            }
        }

        void FixedUpdate()
        {
            // Rigidbody driven movement: we rely on physics for collisions. Only ensure velocity is set on launch and check max distance.
            if (_state == State.Thrown && useRigidbody && _rb != null)
            {
                // ensure gravity flag matches params
                _rb.useGravity = _launchParams.useGravity;

                // check max distance
                if (Vector3.Distance(_rb.position, _originWorldPos) >= maxDistance)
                {
                    StartReturn();
                }
            }
        }

        // Input callback
        public void ThrowHand(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                if (_state == State.Idle)
                {
                    Launch();
                }
            }

            if (context.canceled)
            {
                if (_state == State.Thrown)
                {
                    StartReturn();
                }
            }
        }

        void Launch()
        {
            if (hand == null)
            {
                Debug.LogWarning("HandMediator.Launch: hand no asignada.");
                return;
            }

            hand.StartCollect();

            // guardar estado original
            _originalParent = hand.transform.parent;
            _originalLocalPos = hand.transform.localPosition;
            _originalLocalRot = hand.transform.localRotation;

            // origen en mundo
            _originWorldPos = (handOrigin != null) ? handOrigin.position : this.transform.position;

            // calcular direccion de lanzamiento (desde origin hacia forward del origin)
            _launchDir = (handOrigin != null) ? handOrigin.forward : this.transform.forward;
            _launchDir.Normalize();
            _launchDirXZ = new Vector3(_launchDir.x, 0f, _launchDir.z).normalized;

            // posicionar la mano ligeramente fuera del origen para evitar colisiones con el arma
            Vector3 launchPos = _originWorldPos + _launchDir * launchPositionOffset;
            hand.transform.position = launchPos;
            hand.transform.SetParent(null, true);

            // aplicar parámetros de lanzamiento actuales (_launchParams) al inicio
            // calcular velocidad inicial
            // calcular ángulo a usar: por defecto desde _launchParams.angleDeg
            float angleToUseDeg = _launchParams.angleDeg;
            if (_launchParams.useArc)
            {
                float angRad = angleToUseDeg * Mathf.Deg2Rad;
                Vector3 forwardXZ = _launchDirXZ;
                // componente horizontal
                Vector3 hor = forwardXZ * (_launchParams.speed * Mathf.Cos(angRad));
                // componente vertical
                Vector3 ver = Vector3.up * (_launchParams.speed * Mathf.Sin(angRad));
                _ballisticVelocity = hor + ver;
                _verticalVelocity = _ballisticVelocity.y;
            }
            else
            {
                // comportamiento plano: velocidad en la dirección full _launchDir
                _ballisticVelocity = _launchDir * _launchParams.speed;
                _verticalVelocity = _launchDir.y * _launchParams.speed;
            }

            // ajustar flags de gravedad según parámetros
            applyGravityOnThrow = _launchParams.useGravity;
            gravityScale = _launchParams.gravityScale;

            // activar cuerda/visual
            if (ropeStraight != null) ropeStraight.isActive = true;

            if (useRigidbody)
            {
                _rb = hand.GetComponent<Rigidbody>();
                if (_rb == null)
                {
                    _rb = hand.gameObject.AddComponent<Rigidbody>();
                    _addedRigidbody = true;
                }

                _rb.isKinematic = false;
                _rb.useGravity = _launchParams.useGravity;
                _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                _rb.interpolation = RigidbodyInterpolation.Interpolate;

                // aplicar velocidad inicial
                // aplicar un impulso directo para establecer la velocidad (simple)
                try
                {
                    _rb.AddForce(_ballisticVelocity, ForceMode.VelocityChange);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"HandMediator: fallo al aplicar impulso en Launch(): {ex.Message}");
                }
            }
            else
            {
                // en modo transform, inicializamos la velocidad ballistic que se integrará en Update
                // _ballisticVelocity ya está inicializada arriba
            }

            _state = State.Thrown;
        }

        /// <summary>
        /// Permite que powerups u otros sistemas modifiquen parámetros de lanzamiento.
        /// </summary>
        public void SetLaunchParams(LaunchParams p)
        {
            _launchParams = p;
        }

        /// <summary>
        /// Restablece los parámetros de lanzamiento al valor por defecto.
        /// </summary>
        public void ResetLaunchParams()
        {
            _launchParams = defaultLaunch;
        }

         public void StartReturn()
         {
             if (_state == State.Returning) return;

             _state = State.Returning;

             // Reparentar inmediatamente al padre original (manteniendo posición world) para que no quede suelta
             if (hand != null)
             {
                 if (_originalParent != null)
                     hand.transform.SetParent(_originalParent, true);
                 else
                     hand.transform.SetParent(this.transform, true);
             }

             // si usamos rigidbody, desactivamos su control para mover manualmente
             if (_rb != null)
             {
                 // Evitar escribir linearVelocity si ya es kinematic
                 if (!_rb.isKinematic)
                 {
                     try
                     {
                         _rb.linearVelocity = Vector3.zero;
                     }
                     catch
                     {
                     }
                 }

                 _rb.isKinematic = true; // pasamos a mover por transform
             }

             // desactivar cuerda si querés, o mantener
             if (ropeStraight != null) ropeStraight.isActive = true; // o false si prefieres ocultar
         }

         void ReturnComplete()
         {
             // restaurar parent y transform
             if (hand == null) return;

             // Primero reparentamos para que la mano vuelva a ser controlada por su padre de inmediato
             if (_originalParent != null)
             {
                 hand.transform.SetParent(_originalParent, false);
             }
             else
             {
                 // si no había padre original, la hacemos hija del HandMediator para no dejarla suelta
                 hand.transform.SetParent(this.transform, false);
             }

             // Restaurar transform local una vez que está parentada
             hand.transform.localPosition = _originalLocalPos;
             hand.transform.localRotation = _originalLocalRot;

             // quitar o dejar el rigidbody en estado seguro
             if (_rb != null)
             {
                 if (_addedRigidbody)
                 {
                     Destroy(_rb);
                 }
                 else
                 {
                     // Si no es kinematic, detener la velocidad; luego desactivar gravedad y activar kinematic
                     if (!_rb.isKinematic)
                     {
                         try
                         {
                             _rb.linearVelocity = Vector3.zero;
                         }
                         catch
                         {
                         }
                     }

                     _rb.useGravity = false;
                     _rb.isKinematic = true;
                 }

                 _rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                 _rb.interpolation = RigidbodyInterpolation.None;

                 _rb = null;
                 _addedRigidbody = false;
             }

             if (ropeStraight != null) ropeStraight.isActive = false;

             _state = State.Idle;
         }
     }
 }

