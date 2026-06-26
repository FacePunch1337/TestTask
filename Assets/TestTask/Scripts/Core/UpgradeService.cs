using System;
using System.Collections.Generic;
using TestTask.Config;
using UnityEngine;

namespace TestTask.Core
{
    public sealed class UpgradeService
    {
        private readonly GameConfigAsset _config;
        private readonly RunStats _runStats;
        private readonly UpgradeType[] _catalog =
        {
            UpgradeType.FireRate,
            UpgradeType.Damage,
            UpgradeType.PickUpRadius
        };
        private readonly Dictionary<UpgradeType, int> _upgradeLevels = new();

        public UpgradeService(GameConfigAsset config, RunStats runStats)
        {
            _config = config;
            _runStats = runStats;
        }

        public void Reset() => _upgradeLevels.Clear();

        public UpgradeDefinition[] RollChoices()
        {
            var result = new UpgradeDefinition[3];
            var used = new HashSet<int>();

            for (var i = 0; i < result.Length; i++)
            {
                var index = UnityEngine.Random.Range(0, _catalog.Length);
                while (!used.Add(index))
                    index = UnityEngine.Random.Range(0, _catalog.Length);

                result[i] = CreateDefinition(_catalog[index]);
            }

            return result;
        }

        public void Apply(UpgradeDefinition definition)
        {
            var type = definition.Type;
            var value = GetNextValue(type);
            switch (type)
            {
                case UpgradeType.FireRate:
                    _runStats.AddFireRate(value);
                    break;
                case UpgradeType.Damage:
                    _runStats.AddBulletDamage(value);
                    break;
                case UpgradeType.PickUpRadius:
                    _runStats.AddPickupMagnetRadius(value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _upgradeLevels[type] = GetLevel(type) + 1;
        }

        private UpgradeDefinition CreateDefinition(UpgradeType type)
        {
            var settings = GetSettings(type);
            var title = string.IsNullOrWhiteSpace(settings.Title) ? type.ToString() : settings.Title;
            var value = GetNextValue(type);
            return new UpgradeDefinition(type, title, $"+{FormatValue(value)}", settings.Icon);
        }

        private float GetNextValue(UpgradeType type)
        {
            var settings = GetSettings(type);
            return settings.BaseValue * Mathf.Pow(Mathf.Max(0f, settings.ValueMultiplier), GetLevel(type));
        }

        private int GetLevel(UpgradeType type) => _upgradeLevels.TryGetValue(type, out var level) ? level : 0;

        private UpgradeStatConfig GetSettings(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.FireRate => _config.FireRateUpgrade,
                UpgradeType.Damage => _config.DamageUpgrade,
                UpgradeType.PickUpRadius => _config.PickUpRadiusUpgrade,
                _ => _config.FireRateUpgrade
            };
        }

        private static string FormatValue(float value)
        {
            return Mathf.Approximately(value, Mathf.Round(value))
                ? Mathf.RoundToInt(value).ToString()
                : value.ToString("0.#");
        }
    }

    public enum UpgradeType
    {
        FireRate,
        Damage,
        PickUpRadius
    }

    public readonly struct UpgradeDefinition
    {
        public readonly UpgradeType Type;
        public readonly string Title;
        public readonly string Description;
        public readonly Sprite Icon;

        public UpgradeDefinition(UpgradeType type, string title, string description, Sprite icon)
        {
            Type = type;
            Title = title;
            Description = description;
            Icon = icon;
        }
    }
}
