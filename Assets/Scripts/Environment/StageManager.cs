using System;
using System.Collections;
using System.Collections.Generic;
using Basuras;
using Icebergs;
using UnityEngine;

namespace Environment
{
    public class StageManager : MonoBehaviour
    {
        public List<StageConfig> stages;
        public PowerupSelector powerupSelector;
        public ScoringSystem scoringSystem;
        public Iceberg icebergPrefab;
        public IcebergRules icebergRules;
        public List<GameObject> icebergSpawnPoints;
        public BasuraSpawnController basuraSpawnController;

        [Tooltip("Intervalo en segundos para hacer logs de progreso de la etapa")]
        public float stageProgressLogInterval = 5f;

        public static event Action<StageConfig> OnStageStart;
        public static event Action<StageConfig> OnStageEnd;
        public static event Action OnAllStagesComplete;

        private int _currentIndex = 0;

        private List<Iceberg> _icebergs = new();
        private GameController _gameController;

        public void Configure(GameController gameController)
        {
            StartCoroutine(RunStages());
            _gameController = gameController;
        }

        private IEnumerator RunStages()
        {
            for (_currentIndex = 0; _currentIndex < stages.Count; _currentIndex++)
            {
                var stage = stages[_currentIndex];
                OnStageStart?.Invoke(stage);
                
                basuraSpawnController.StartSpawning();
                
                foreach (var iceberg in _icebergs)
                {
                    Destroy(iceberg.gameObject);
                }

                _icebergs = new List<Iceberg>();

                for (int i = 0; i < stage.icebergCount; i++)
                {
                    Vector3 spawnPosition = icebergSpawnPoints[i % icebergSpawnPoints.Count].transform.position;
                    Iceberg iceberg = Instantiate(icebergPrefab, spawnPosition, Quaternion.identity);
                    _icebergs.Add(iceberg);
                }

                icebergRules.Spawn(_icebergs, stage, OnIcebergLost);

                float elapsed = 0f;
                float nextLogTime = stageProgressLogInterval;

                while (elapsed < stage.duration)
                {
                    elapsed += Time.deltaTime;

                    if (elapsed >= nextLogTime)
                    {
                        float remaining = Mathf.Max(0f, stage.duration - elapsed);
                        nextLogTime += Mathf.Max(0.1f, stageProgressLogInterval);
                    }

                    yield return null;
                }
                
                OnStageEnd?.Invoke(stage);

                // calcular puntuación para la etapa
                scoringSystem.EvaluateStage(stage, _currentIndex);

                // Si no es la última etapa, abrir selector y esperar selección
                if (_currentIndex < stages.Count - 1)
                {
                    yield return StartCoroutine(powerupSelector.OpenAndWait());
                }
            }
            
            OnAllStagesComplete?.Invoke();
        }

        private void OnIcebergLost()
        {
            _gameController.GameOver();
        }
    }
}