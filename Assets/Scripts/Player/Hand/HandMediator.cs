using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Icebergs;

namespace Player.Hand
{
    /// <summary>
    /// HandMediator (simplificado): comportamiento tipo gancho/hand
    /// - Press (Input.started) -> lanza la mano desde `handOrigin` hacia su forward
    /// - Hold -> la mano avanza en línea recta (sin gravedad)
    /// - Release (Input.canceled) -> la mano vuelve al origen
    /// Opcional: usa Rigidbody para movimiento (constante) o mueve Transform directamente.
    /// </summary>
    public class HandMediator : MonoBehaviour
    {
        public GameObject hand; // el objeto que representa la mano
        public Transform handOrigin; // punto de origen (p. ej. punta del arma)
        public RopeWithWave ropeStraight; // opcional: visual de cuerda

        [Header("Movement")] public bool useRigidbody = true; // si true usa Rigidbody; si false mueve Transform
        public float throwSpeed = 20f; // velocidad mientras avanza
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

        Vector3 _launchDir;
        Vector3 _launchDirXZ;
        float _verticalVelocity;

        void Awake()
        {
            // cache origin world position in Awake if possible
            if (handOrigin != null)
            {
                _originWorldPos = handOrigin.position;
            }
        }

        void Update()
        {
            // Estado Thrown: si no usamos rigidbody, mover por transform aquí
            if (_state == State.Thrown && !useRigidbody)
            {
                float dt = Time.deltaTime;
                Vector3 currentPos = hand.transform.position;
                Vector3 delta;
                if (applyGravityOnThrow)
                {
                    // separamos movimiento horizontal y vertical
                    Vector3 horiz = _launchDirXZ * (throwSpeed * dt);
                    _verticalVelocity += Physics.gravity.y * gravityScale * dt;
                    Vector3 vert = Vector3.up * (_verticalVelocity * dt);
                    delta = horiz + vert;
                }
                else
                {
                    delta = _launchDir * (throwSpeed * dt);
                }

                Vector3 nextPos = currentPos + delta;

                // check collision between currentPos and nextPos
                float stepDist = delta.magnitude;
                Vector3 checkDir = (delta.sqrMagnitude > 1e-6f) ? delta.normalized : _launchDir;
                if (Physics.Raycast(currentPos, checkDir, out RaycastHit hit, stepDist, hitLayers))
                {
                    // impact
                    hand.transform.position = hit.point;
                    StartReturn();
                    return;
                }

                hand.transform.position = nextPos;

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
                Vector3 target = handOrigin.transform.position;
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
            if (_state == State.Thrown && useRigidbody && _rb != null)
            {
                // aplicar velocidad inicial y controlar si la gravedad debe aplicarse
                _rb.useGravity = applyGravityOnThrow;
                // si usamos gravedad dejamos que Unity la modifique, pero inicializamos la velocidad
                try
                {
                    _rb.linearVelocity = _launchDir * throwSpeed;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"HandMediator: fallo al asignar linearVelocity en FixedUpdate: {ex.Message}");
                }

                // comprobar distancia
                if (Vector3.Distance(_rb.position, _originWorldPos) >= maxDistance)
                {
                    StartReturn();
                }

                // chequeo simple de colisión por delante
                float linMag = 0f;
                try
                {
                    linMag = _rb.linearVelocity.magnitude;
                }
                catch
                {
                    linMag = 0f;
                }

                float checkDist = (linMag * Time.fixedDeltaTime) + 0.05f;
                Vector3 rbCheckDir;
                try
                {
                    rbCheckDir = (_rb.linearVelocity.sqrMagnitude > 1e-6f) ? _rb.linearVelocity.normalized : _launchDir;
                }
                catch
                {
                    rbCheckDir = _launchDir;
                }

                if (Physics.Raycast(_rb.position, rbCheckDir, out RaycastHit hit, checkDist, hitLayers))
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

            // activar cuerda/visual
            if (ropeStraight != null) ropeStraight.isActive = true;

            if (useRigidbody)
            {
                _rb = hand.GetComponent<Rigidbody>();
                if (_rb == null)
                {
                    _rb = hand.AddComponent<Rigidbody>();
                    _addedRigidbody = true;
                }

                _rb.isKinematic = false;
                _rb.useGravity = applyGravityOnThrow;
                _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                _rb.interpolation = RigidbodyInterpolation.Interpolate;

                // aplicar velocidad inicial
                _rb.linearVelocity = _launchDir * throwSpeed;
                // inicializamos verticalVelocity para modo transform si fuera necesario
                _verticalVelocity = _launchDir.y * throwSpeed;
            }

            _state = State.Thrown;
        }

        void StartReturn()
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