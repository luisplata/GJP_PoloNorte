using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Basuras
{
    public class BasuraSpawnController : MonoBehaviour
    {
        [Header("Spawn Points")]
        [Tooltip("Lista de transforms usados como posiciones de spawn (pueden ser GameObjects parent con Transform)")]
        public List<Transform> spawnPoints = new List<Transform>();

        [Header("Prefabs")]
        [Tooltip("Lista de prefabs posibles para spawnear (puede ser más de uno)")]
        public List<GameObject> prefabsToSpawn = new List<GameObject>();

        [Header("Timing")]
        [Tooltip("Intervalo en segundos entre spawns (si StartSpawning se ha activado)")]
        public float spawnInterval = 2f;

        [Tooltip("Si true, se elegirá un punto de spawn aleatorio; si false se harán en secuencia")]
        public bool randomizeSpawnPoint = true;

        [Tooltip("Si true, se elegirá un prefab aleatorio; si false se harán en secuencia")]
        public bool randomizePrefab = true;

        [Tooltip("Cantidad de instancias a spawnear cada vez que ocurre un tick de spawn")]
        public int spawnPerTick = 1;

        [Header("Spawned object settings")]
        [Tooltip("Dirección inicial que se aplicará a cada BasuraBehavour instanciado (en world space)")]
        public Vector3 initialDirection = Vector3.right;

        [Tooltip("Velocidad aplicada a cada BasuraBehavour instanciado (si el componente existe)")]
        public float initialSpeed = 1f;

        // estado interno
        private Coroutine _spawnRoutine;
        private int _nextSpawnPointIndex = 0;
        private int _nextPrefabIndex = 0;

        private void OnValidate()
        {
            if (spawnInterval < 0.01f) spawnInterval = 0.01f;
            if (spawnPerTick < 1) spawnPerTick = 1;
        }

        /// <summary>
        /// Inicia el coroutine que spawnea periódicamente.
        /// </summary>
        public void StartSpawning()
        {
            if (_spawnRoutine != null) return;
            _spawnRoutine = StartCoroutine(SpawnLoop());
            // Debug.Log("[BasuraSpawnController] StartSpawning");
        }

        /// <summary>
        /// Detiene el spawneo periódico.
        /// </summary>
        public void StopSpawning()
        {
            if (_spawnRoutine == null) return;
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
            // Debug.Log("[BasuraSpawnController] StopSpawning");
        }

        /// <summary>
        /// Spawn único: instanciará spawnPerTick objetos inmediatamente.
        /// </summary>
        public void SpawnOnce()
        {
            if (spawnPoints.Count == 0)
            {
                Debug.LogWarning("[BasuraSpawnController] No hay spawnPoints configurados.");
                return;
            }

            if (prefabsToSpawn.Count == 0)
            {
                Debug.LogWarning("[BasuraSpawnController] No hay prefabsToSpawn configurados.");
                return;
            }

            for (int i = 0; i < spawnPerTick; i++)
            {
                int spIndex = ChooseSpawnPointIndex();
                int pfIndex = ChoosePrefabIndex();
                SpawnAt(spIndex, pfIndex);
            }
        }

        /// <summary>
        /// Instancia un prefab en el spawn point indicado por índices.
        /// </summary>
        public GameObject SpawnAt(int spawnPointIndex, int prefabIndex)
        {
            if (spawnPoints.Count == 0 || prefabsToSpawn.Count == 0) return null;

            spawnPointIndex = Mathf.Clamp(spawnPointIndex, 0, spawnPoints.Count - 1);
            prefabIndex = Mathf.Clamp(prefabIndex, 0, prefabsToSpawn.Count - 1);

            Transform sp = spawnPoints[spawnPointIndex];
            GameObject prefab = prefabsToSpawn[prefabIndex];

            if (prefab == null || sp == null)
            {
                Debug.LogWarning("[BasuraSpawnController] Prefab o SpawnPoint nulo al intentar spawnear.");
                return null;
            }

            GameObject go = Instantiate(prefab, sp.position, sp.rotation);
            go.name = prefab.name; // evita clones con (Clone) si quieres

            // Intentar configurar BasuraBehavour si existe
            var behaviour = go.GetComponent<BasuraBehavour>();
            if (behaviour != null)
            {
                behaviour.SetDirection(initialDirection);
                behaviour.SetSpeed(initialSpeed);
            }

            // Debug.Log($"[BasuraSpawnController] Spawned '{go.name}' at '{sp.name}' (spawnPointIndex={spawnPointIndex}, prefabIndex={prefabIndex})");
            return go;
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                SpawnOnce();
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private int ChooseSpawnPointIndex()
        {
            if (spawnPoints.Count == 0) return 0;
            if (randomizeSpawnPoint)
            {
                return Random.Range(0, spawnPoints.Count);
            }
            else
            {
                int idx = _nextSpawnPointIndex;
                _nextSpawnPointIndex = (_nextSpawnPointIndex + 1) % spawnPoints.Count;
                return idx;
            }
        }

        private int ChoosePrefabIndex()
        {
            if (prefabsToSpawn.Count == 0) return 0;
            if (randomizePrefab)
            {
                return Random.Range(0, prefabsToSpawn.Count);
            }
            else
            {
                int idx = _nextPrefabIndex;
                _nextPrefabIndex = (_nextPrefabIndex + 1) % prefabsToSpawn.Count;
                return idx;
            }
        }
    }
}