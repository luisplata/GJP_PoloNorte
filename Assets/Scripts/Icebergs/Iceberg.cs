using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Icebergs
{
    public class Iceberg : MonoBehaviour, IIceberg
    {
        [SerializeField] private List<Transform> animalSpawnPoints;

        public Action OnLoseIceberg;
        private float _limitDepth;
        private bool _validate = true;
        private float _maxRiseHeight;
        private float _velocityToFall;

        private int _numberOfAnimalsOnIceberg;

        public void Configure(float timeStep, float limitDepth, float maxRiseHeight, float velocityToFall,
            int numberOfAnimalsOnIceberg)
        {
            _limitDepth = limitDepth;
            _maxRiseHeight = maxRiseHeight;
            _velocityToFall = velocityToFall;
            _numberOfAnimalsOnIceberg = numberOfAnimalsOnIceberg;
            SpawnAnimals();
            StartCoroutine(ValidatePosition(timeStep));
        }

        private void SpawnAnimals()
        {
            System.Random rand = new System.Random();
            HashSet<int> chosenIndices = new HashSet<int>();

            while (chosenIndices.Count < _numberOfAnimalsOnIceberg && chosenIndices.Count < animalSpawnPoints.Count)
            {
                int index = rand.Next(animalSpawnPoints.Count);
                chosenIndices.Add(index);
            }

            foreach (int index in chosenIndices)
            {
                Transform spawnPoint = animalSpawnPoints[index];
                GameObject animal = GameObject.CreatePrimitive(PrimitiveType.Cube); // Placeholder para el animal
                animal.transform.position = spawnPoint.position;
                animal.transform.localScale = Vector3.one * 0.5f; // Escala más pequeña para el "animal"
                animal.transform.parent = transform; // Hacer que el animal sea hijo del iceberg
                
                _velocityToFall *= 1.05f; // Aumentar la velocidad de caída por cada animal
            }
        }

        private IEnumerator ValidatePosition(float timeStep)
        {
            while (_validate)
            {
                yield return new WaitForSeconds(timeStep);
                // Debug.Log($"Iceberg '{name}': Validating position at y={transform.position.y}, limitDepth={_limitDepth}");
                if (transform.position.y < _limitDepth)
                {
                    OnLoseIceberg?.Invoke();
                }
            }
        }

        private void FixedUpdate()
        {
            transform.position += Vector3.down * (_velocityToFall * Time.fixedDeltaTime);
        }

        public void Rise(float amount)
        {
            if (transform.position.y >= _maxRiseHeight)
            {
                Debug.Log(
                    $"Iceberg '{name}': Already at or above max rise height ({_maxRiseHeight}), cannot rise further.");
                return;
            }

            transform.position += Vector3.up * amount;
        }

        private void OnDestroy()
        {
            _validate = false;
        }
    }
}