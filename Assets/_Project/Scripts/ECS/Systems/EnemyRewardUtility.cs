using TestTask.ECS.Components;
using Unity.Mathematics;

namespace TestTask.ECS.Systems
{
    internal static class EnemyRewardUtility
    {
        public static RewardEvent CreateDrop(EnemyTypeData enemyType, float3 position)
        {
            return new RewardEvent
            {
                Coins = UnityEngine.Random.value <= enemyType.CoinDropChance ? enemyType.CoinReward : 0,
                Xp = UnityEngine.Random.value <= enemyType.XpDropChance ? enemyType.XpReward : 0,
                Repair = UnityEngine.Random.value <= enemyType.GearDropChance ? enemyType.GearHealAmount : 0f,
                Position = position,
                SpawnPickups = true
            };
        }
    }
}
