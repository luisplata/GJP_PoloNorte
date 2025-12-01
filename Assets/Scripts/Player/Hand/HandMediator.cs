using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandMediator : MonoBehaviour
{
    public GameObject hand;
    public RopeWithWave ropeStraight;
    
    [Tooltip("Tiempo tras el cual el rigidbody volverá a kinematic (0 = no volver a kinematic automáticamente).")]
    public float autoDisableAfter;

    private Coroutine _reenableCoroutine;
    
    // Campos para restaurar el estado cuando recogemos la mano
    private Transform _originalParent;
    private Vector3 _originalLocalPosition;
    private Quaternion _originalLocalRotation;
    private bool _addedRigidbody;
    private bool _isLaunched;

    /// <summary>
    /// Lanza el GameObject pasado aplicando una fuerza física.
    /// Si el GameObject no tiene Rigidbody, se añadirá uno.
    /// </summary>
    /// <param name="handObj">GameObject de la mano a lanzar</param>
    /// <param name="force">Vector de fuerza a aplicar (por ejemplo: dirección * potencia)</param>
    /// <param name="mode">ForceMode usado para la aplicación (por defecto Impulse)</param>
    /// <param name="angularImpulse">Magnitud de torque aleatorio para rotación (0 = sin torque)</param>
    public void Throw(GameObject handObj, Vector3 force, ForceMode mode = ForceMode.Impulse, float angularImpulse = 0f)
    {
        if (handObj == null)
        {
            Debug.LogWarning("HandMediator.Throw recibió null como handObj.");
            return;
        }

        Rigidbody rb = handObj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            // Si no hay Rigidbody, lo añadimos para que funcione con física.
            rb = handObj.AddComponent<Rigidbody>();
            rb.mass = 1f;
            _addedRigidbody = true;
        }

        // Guardamos información para poder restaurar cuando recojamos la mano
        _originalParent = handObj.transform.parent;
        _originalLocalPosition = handObj.transform.localPosition;
        _originalLocalRotation = handObj.transform.localRotation;

        // Desparentamos para que la física no se vea afectada por el padre.
        handObj.transform.SetParent(null, true);

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        if (ropeStraight != null) ropeStraight.isActive = true;
        rb.AddForce(force, mode);

        if (angularImpulse != 0f)
        {
            rb.AddTorque(Random.onUnitSphere * angularImpulse, mode);
        }

        if (autoDisableAfter > 0f)
        {
            if (_reenableCoroutine != null) StopCoroutine(_reenableCoroutine);
            _reenableCoroutine = StartCoroutine(MakeKinematicAfter(rb, autoDisableAfter));
        }
        
        // Marcamos que la mano fue lanzada
        _isLaunched = true;
    }

    private IEnumerator MakeKinematicAfter(Rigidbody rb, float time)
    {
        yield return new WaitForSeconds(time);
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    public void ThrowHand(InputAction.CallbackContext context)
    {
        // Mantengo el nombre por compatibilidad, pero ahora alterna lanzamiento/recogida con el mismo botón
        if (!context.performed) return;

        if (hand == null)
        {
            Debug.LogWarning("HandMediator.ThrowHand: 'hand' es null.");
            return;
        }

        if (!_isLaunched)
        {
            Vector3 throwDirection = transform.forward; // Dirección hacia adelante del objeto
            float throwForce = 10f; // Fuerza de lanzamiento (ajustable)
            Throw(hand, throwDirection * throwForce);
        }
        else
        {
            RetrieveHand();
        }
    }
    
    /// <summary>
    /// Recoge la mano lanzada y restaura su transform/parent para poder lanzarla de nuevo.
    /// Si se pasa un parent, la mano se reparenta a ese transform; si no, se reparenta al padre original
    /// (si existe) o al GameObject que tiene este HandMediator.
    /// </summary>
    public void RetrieveHand(Transform returnParent = null, bool restoreLocalTransform = true)
    {
        if (hand == null)
        {
            Debug.LogWarning("HandMediator.RetrieveHand: 'hand' es null.");
            return;
        }

        // Detener coroutine de re-enable si existe
        if (_reenableCoroutine != null)
        {
            StopCoroutine(_reenableCoroutine);
            _reenableCoroutine = null;
        }

        Rigidbody rb = hand.GetComponent<Rigidbody>();
        if (rb)
        {
            // Detener movimiento y desactivar física activa
            // Usar linearVelocity (API recomendada)
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            // Si añadimos el Rigidbody en Throw, podemos eliminarlo para volver al estado original
            if (_addedRigidbody)
            {
                Destroy(rb);
                _addedRigidbody = false;
            }
        }

        // Desactivar la visualización de la cuerda
        if (ropeStraight != null)
        {
            ropeStraight.isActive = false;
        }

        // Reparentar
        Transform targetParent = returnParent != null ? returnParent : (_originalParent != null ? _originalParent : this.transform);
        hand.transform.SetParent(targetParent, false);

        if (restoreLocalTransform)
        {
            hand.transform.localPosition = _originalLocalPosition;
            hand.transform.localRotation = _originalLocalRotation;
        }
        
        // Marcamos que la mano ya no está lanzada
        _isLaunched = false;
    }
}