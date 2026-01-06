using System.Collections.Generic;
using Basuras;
using UnityEngine;

namespace Player.Hand
{
    public class Hand : MonoBehaviour
    {
        [Header("Pickup settings")]
        [Tooltip("Tag que identifica objetos basura (case-sensitive). Por defecto: 'basura'")]
        public string basuraTag = "basura";

        [Tooltip("Si true, al recoger la basura se destruirá el GameObject; si false solo se desactiva.")]
        public bool destroyOnPickup = true;

        [Tooltip("Lista que guarda los datos de las basuras recogidas (snapshot).")]
        public List<BasuraData> collected = new List<BasuraData>();

        [System.Serializable]
        public class BasuraData
        {
            public string name;
            public Vector3 position;
            public Vector3 direction;
            public float speed;
            public float timeCaptured;
            public string prefabName;
        }

        private bool _canPickup;
        private HandMediator _handMediator;

        public void StartCollect()
        {
            ClearCollected();
            _canPickup = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_canPickup) return;
            TryCollect(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_canPickup) return;
            TryCollect(collision.gameObject);
        }

        private void TryCollect(GameObject other)
        {
            if (other == null) return;

            if (!other.CompareTag(basuraTag)) return;


            // Crear snapshot
            CollectImmediate(other);
        }

        /// <summary>
        /// Fuerza la recolección de un objeto (útil para llamadas desde HandMediator cuando detectamos basura por raycast en modo transform).
        /// </summary>
        public void CollectImmediate(GameObject other)
        {
            if (other == null) return;

            // Aceptar objetos que tengan el tag o el componente BasuraBehavour en algún padre
            if (!other.CompareTag(basuraTag) && other.GetComponentInParent<BasuraBehavour>() == null) return;

            BasuraData data = null;
            var b = other.GetComponentInParent<BasuraBehavour>();
            if (b != null)
            {
                // Construir snapshot a partir de la información disponible en BasuraBehavour
                data = new BasuraData
                {
                    name = b.data != null && !string.IsNullOrEmpty(b.data.nombre) ? b.data.nombre : other.name,
                    position = b.transform.position,
                    direction = b.moveDirection,
                    speed = b.speed,
                    timeCaptured = Time.time,
                    prefabName = other.name
                };
            }
            else
            {
                // Fallback: crear un snapshot mínimo
                data = new BasuraData
                {
                    name = other.name,
                    position = other.transform.position,
                    direction = Vector3.zero,
                    speed = 0f,
                    timeCaptured = Time.time,
                    prefabName = other.name
                };
            }

            collected.Add(data);
            Debug.Log($"[Hand] Recogida basura {data.name}: (Total recogidas: {collected.Count})");
            _canPickup = false;

            // destruir o desactivar el objeto recogido si corresponde
            if (destroyOnPickup && other != null)
            {
                Destroy(other);
            }
            else if (other != null)
            {
                other.SetActive(false);
            }

            // notify mediator to return
            if (_handMediator != null) _handMediator.StartReturn();
        }

        /// <summary>
        /// Limpia la lista de objetos recogidos.
        /// </summary>
        public void ClearCollected()
        {
            collected.Clear();
        }

        public void Configure(HandMediator handMediator)
        {
            _handMediator = handMediator;
        }
    }
}