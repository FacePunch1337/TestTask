using System.Collections.Generic;
using TestTask.Config;
using TestTask.ECS.Bridge;
using Unity.Mathematics;
using UnityEngine;

namespace TestTask.Core
{
    public sealed class EnemySpawner
    {
        private readonly GameConfigAsset _config;
        private readonly CombatEcsBridge _combat;
        private readonly List<float> _spawnMarks = new();
        private readonly List<int> _spawnWaveIndices = new();
        private readonly List<float> _barrelMarks = new();
        private int _nextSpawnIndex;
        private int _nextBarrelIndex;

        public EnemySpawner(GameConfigAsset config, CombatEcsBridge combat)
        {
            _config = config;
            _combat = combat;
        }

        public void Reset()
        {
            _nextSpawnIndex = 0;
            _nextBarrelIndex = 0;
            BuildPlan();
            BuildBarrelPlan();
        }

        public void Tick(float distance, Vector3 carPosition)
        {
            var wave = GetWaveForSpawnIndex(_nextSpawnIndex);
            var spawnAheadDistance = Mathf.Max(5f, wave?.SpawnAheadDistance ?? _config.EnemySpawnAheadDistance);
            while (_nextSpawnIndex < _spawnMarks.Count && _spawnMarks[_nextSpawnIndex] <= distance + spawnAheadDistance)
            {
                var mark = _spawnMarks[_nextSpawnIndex];
                var progress = Mathf.Clamp01(mark / _config.LevelLength);
                wave = GetWaveForSpawnIndex(_nextSpawnIndex);
                var halfWidth = _config.RoadWidth * 0.43f;
                var x = UnityEngine.Random.Range(-halfWidth, halfWidth);
                var z = carPosition.z + (mark - distance);
                var typeIndex = RollTypeIndex(progress, wave, out var presenterPrefabOverride);
                var difficulty = wave != null ? Mathf.Max(0f, wave.DifficultyMultiplier) : 1f + progress * 0.65f;
                _combat.SpawnEnemy(new float3(x, 0f, z), typeIndex, difficulty, presenterPrefabOverride);
                _nextSpawnIndex++;
                wave = GetWaveForSpawnIndex(_nextSpawnIndex);
                spawnAheadDistance = Mathf.Max(5f, wave?.SpawnAheadDistance ?? _config.EnemySpawnAheadDistance);
            }

            var barrelAheadDistance = Mathf.Max(5f, _config.BarrelSpawnAheadDistance);
            while (_nextBarrelIndex < _barrelMarks.Count && _barrelMarks[_nextBarrelIndex] <= distance + barrelAheadDistance)
            {
                var mark = _barrelMarks[_nextBarrelIndex];
                var halfWidth = _config.RoadWidth * 0.39f;
                var x = UnityEngine.Random.Range(-halfWidth, halfWidth);
                var z = carPosition.z + (mark - distance);
                _combat.SpawnBarrel(new float3(x, 0f, z));
                _nextBarrelIndex++;
            }
        }

        private int RollTypeIndex(float progress, EnemyWaveConfig wave, out GameObject presenterPrefabOverride)
        {
            presenterPrefabOverride = null;
            if (wave?.Choices != null && wave.Choices.Length > 0)
            {
                var totalWeight = 0f;
                for (var i = 0; i < wave.Choices.Length; i++)
                    totalWeight += Mathf.Max(0f, wave.Choices[i].Weight);

                if (totalWeight > 0f)
                {
                    var roll = UnityEngine.Random.Range(0f, totalWeight);
                    for (var i = 0; i < wave.Choices.Length; i++)
                    {
                        roll -= Mathf.Max(0f, wave.Choices[i].Weight);
                        if (roll <= 0f)
                        {
                            presenterPrefabOverride = wave.Choices[i].PresenterPrefabOverride;
                            return Mathf.Clamp(wave.Choices[i].EnemyTypeIndex, 0, _config.EnemyTypes.Length - 1);
                        }
                    }
                }
            }

            if (_config.EnemyTypes.Length <= 1)
                return 0;

            if (progress > 0.55f && UnityEngine.Random.value < 0.25f)
                return 1;

            if (progress > 0.25f && UnityEngine.Random.value < 0.32f)
                return 2;

            return 0;
        }

        private void BuildPlan()
        {
            _spawnMarks.Clear();
            _spawnWaveIndices.Clear();

            var waves = _config.EnemyWaves;
            if (waves == null || waves.Length == 0)
            {
                BuildFallbackPlan();
                return;
            }

            for (var waveIndex = 0; waveIndex < waves.Length; waveIndex++)
            {
                var wave = waves[waveIndex];
                var start = Mathf.Max(_config.EnemyFirstSpawnDistance, wave.StartDistance);
                var end = waveIndex + 1 < waves.Length ? Mathf.Max(start, waves[waveIndex + 1].StartDistance) : _config.LevelLength;
                if (wave.SpawnCount > 0)
                {
                    BuildFixedCountWave(waveIndex, start, end, wave);
                }
                else
                {
                    BuildSpacingWave(waveIndex, start, end, wave);
                }
            }

            SortSpawnMarks();
        }

        private void BuildBarrelPlan()
        {
            _barrelMarks.Clear();
            var mark = Mathf.Max(0f, _config.BarrelFirstSpawnDistance);
            var spacing = Mathf.Max(1f, _config.BarrelSpawnSpacing);
            var randomness = Mathf.Max(0f, _config.BarrelSpawnSpacingRandomness);
            var chance = Mathf.Clamp01(_config.BarrelSpawnChance);

            while (mark < _config.LevelLength)
            {
                if (UnityEngine.Random.value <= chance)
                    _barrelMarks.Add(mark);

                mark += spacing + UnityEngine.Random.Range(-randomness, randomness);
                if (_barrelMarks.Count > 0)
                    mark = Mathf.Max(mark, _barrelMarks[^1] + 1f);
            }
        }

        private void BuildFixedCountWave(int waveIndex, float start, float end, EnemyWaveConfig wave)
        {
            var count = Mathf.Max(1, wave.SpawnCount);
            var length = Mathf.Max(1f, end - start);
            var step = length / count;
            var jitter = Mathf.Min(Mathf.Max(0f, wave.SpawnSpacingRandomness), step * 0.45f);

            for (var i = 0; i < count; i++)
            {
                var mark = start + step * (i + 0.5f) + UnityEngine.Random.Range(-jitter, jitter);
                _spawnMarks.Add(Mathf.Clamp(mark, start, end));
                _spawnWaveIndices.Add(waveIndex);
            }
        }

        private void BuildSpacingWave(int waveIndex, float start, float end, EnemyWaveConfig wave)
        {
            var mark = start;
            var spacing = Mathf.Max(1f, wave.SpawnSpacing);
            var randomness = Mathf.Max(0f, wave.SpawnSpacingRandomness);

            while (mark < end)
            {
                _spawnMarks.Add(mark);
                _spawnWaveIndices.Add(waveIndex);
                mark += spacing + UnityEngine.Random.Range(-randomness, randomness);
                mark = Mathf.Max(mark, _spawnMarks[^1] + 1f);
            }
        }

        private void BuildFallbackPlan()
        {
            var mark = Mathf.Max(0f, _config.EnemyFirstSpawnDistance);
            var spacing = Mathf.Max(1f, _config.EnemySpawnSpacing);
            var randomness = Mathf.Max(0f, _config.EnemySpawnSpacingRandomness);

            while (mark < _config.LevelLength)
            {
                _spawnMarks.Add(mark);
                _spawnWaveIndices.Add(-1);
                mark += spacing + UnityEngine.Random.Range(-randomness, randomness);
                mark = Mathf.Max(mark, _spawnMarks[^1] + 1f);
            }

            SortSpawnMarks();
        }

        private void SortSpawnMarks()
        {
            for (var i = 0; i < _spawnMarks.Count - 1; i++)
            {
                for (var j = i + 1; j < _spawnMarks.Count; j++)
                {
                    if (_spawnMarks[i] <= _spawnMarks[j])
                        continue;

                    (_spawnMarks[i], _spawnMarks[j]) = (_spawnMarks[j], _spawnMarks[i]);
                    (_spawnWaveIndices[i], _spawnWaveIndices[j]) = (_spawnWaveIndices[j], _spawnWaveIndices[i]);
                }
            }
        }

        private EnemyWaveConfig GetWaveForSpawnIndex(int spawnIndex)
        {
            if (spawnIndex < 0 || spawnIndex >= _spawnWaveIndices.Count)
                return null;

            var waveIndex = _spawnWaveIndices[spawnIndex];
            return _config.EnemyWaves != null && waveIndex >= 0 && waveIndex < _config.EnemyWaves.Length
                ? _config.EnemyWaves[waveIndex]
                : null;
        }
    }
}
