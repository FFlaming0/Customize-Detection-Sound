using UnityEngine;

namespace CialloDetect
{
    public static class Utilities
    {
        public static bool IsPlayerCharacter(DamageReceiver damageReceiver)
        {
            try
            {
                if (damageReceiver == null) return false;

                if (CharacterMainControl.Main != null &&
                    CharacterMainControl.Main.mainDamageReceiver == damageReceiver)
                {
                    return true;
                }

                var healthProperty = damageReceiver.GetType().GetProperty("health");
                if (healthProperty != null)
                {
                    var health = healthProperty.GetValue(damageReceiver);
                    var tryGetCharacterMethod = health.GetType().GetMethod("TryGetCharacter");

                    if (tryGetCharacterMethod != null)
                    {
                        var character = tryGetCharacterMethod.Invoke(health, null) as CharacterMainControl;
                        if (character != null && CharacterMainControl.Main != null && character == CharacterMainControl.Main)
                        {
                            return true;
                        }
                    }
                }

                if (damageReceiver.gameObject.CompareTag("Player") ||
                    damageReceiver.gameObject.name.Contains("Player"))
                {
                    return true;
                }

                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"CialloDetect: 检查是否是玩家时出错: {e.Message}");
                return false;
            }
        }
    }
}