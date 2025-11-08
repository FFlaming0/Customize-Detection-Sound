using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CialloDetect
{
    public class DetectionMonitor
    {
        private Dictionary<AICharacterController, bool> aiDetectionStates = new Dictionary<AICharacterController, bool>();
        private Dictionary<AICharacterController, DamageReceiver> lastSearchedEnemies = new Dictionary<AICharacterController, DamageReceiver>();
        private Dictionary<AICharacterController, AIDetectionRecord> aiDetectionRecords = new Dictionary<AICharacterController, AIDetectionRecord>();

/*        public void StartAIMonitoring()
        {
            UnityEngine.Debug.Log("CialloDetect: 开始AI监控");
            // 每0.5秒检查一次AI状态
            MonoBehaviour mono = UnityEngine.Object.FindObjectOfType<MonoBehaviour>();
            mono.InvokeRepeating("MonitorAIStates", 0f, 0.5f);
        }*/

        public void MonitorAIStates()
        {

            try
            {
                var aiControllers = GameObject.FindObjectsOfType<AICharacterController>();
                UnityEngine.Debug.Log($"CialloDetect: 找到 {aiControllers.Length} 个AI控制器");

                foreach (var ai in aiControllers)
                {
                    CheckSearchedEnemyChange(ai);
                    CheckNoticedChange(ai);
                    CheckDirectDetection(ai);
                }

                CleanupDestroyedAI();
                CleanupExpiredRecords();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"CialloDetect: 监控AI状态时出错: {e}");
            }
        }

        private void CheckSearchedEnemyChange(AICharacterController ai)
        {
            try
            {
                var currentSearchedEnemy = ai.searchedEnemy;

                if (currentSearchedEnemy != null)
                {
                    UnityEngine.Debug.Log($"CialloDetect: AI {ai.gameObject.name} 类型: {ai.CharacterMainControl?.Team} - 当前目标: {currentSearchedEnemy.gameObject.name} 类型: {ai.CharacterMainControl?.Team}");
                    bool isPlayer = Utilities.IsPlayerCharacter(currentSearchedEnemy);
                    UnityEngine.Debug.Log($"CialloDetect: 目标是玩家: {isPlayer}");
                }

                if (lastSearchedEnemies.ContainsKey(ai))
                {
                    var lastEnemy = lastSearchedEnemies[ai];

                    if (lastEnemy == null && currentSearchedEnemy != null)
                    {
                        if (Utilities.IsPlayerCharacter(currentSearchedEnemy))
                        {
                            UnityEngine.Debug.Log($"CialloDetect: AI发现玩家！AI: {ai.gameObject.name} 类型: {ai.CharacterMainControl?.Team}, 目标: {currentSearchedEnemy.gameObject.name} 类型: {ai.CharacterMainControl?.Team}");

                            if (CanPlaySoundForAI(ai))
                            {
                                string audioPrefix = GetAudioPrefixForAI(ai);
                                AudioManager.PlayRandomDetectionSound(audioPrefix);
                                RecordAIDetection(ai);
                            }
                        }
                    }
                    else if (lastEnemy != null && currentSearchedEnemy == null)
                    {
                        UnityEngine.Debug.Log($"CialloDetect: AI丢失目标 AI: {ai.gameObject.name} 类型: {ai.CharacterMainControl?.Team}");
                    }
                }

                lastSearchedEnemies[ai] = currentSearchedEnemy;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"CialloDetect: 检查searchedEnemy变化时出错: {e}");
            }
        }

        private string GetAudioPrefixForAI(AICharacterController ai)
        {
            // 清理GameObject名称（移除(Clone)后缀）
            string cleanName = ai.gameObject.name.Replace("(Clone)", "").Trim();

            // 特殊处理超人AI
            if (cleanName.Contains("AIController_UltraMan"))
            {
                return "ultraman";
            }

            // 处理模板AI
            if (cleanName.Contains("AIControllerTemplate"))
            {
                if (ai.CharacterMainControl == null) return "default";
                return ai.CharacterMainControl.Team.ToString().ToLower();
            }

            // 处理近战AI
            if (cleanName.Contains("AIController_Melee"))
            {
                if (ai.CharacterMainControl == null) return "melee_default";

                string baseTeam = ai.CharacterMainControl.Team.ToString().ToLower();
                // 确保近战前缀格式正确 (melee_scav 而不是 melee-scav)
                return $"melee-{baseTeam}";
            }

            // 其他情况使用默认
            return "default";
        }

        private void CheckNoticedChange(AICharacterController ai)
        {
            try
            {
                var noticedField = typeof(AICharacterController).GetField("noticed", BindingFlags.NonPublic | BindingFlags.Instance);
                if (noticedField != null)
                {
                    bool currentNoticed = (bool)noticedField.GetValue(ai);

                    if (aiDetectionStates.ContainsKey(ai))
                    {
                        bool lastNoticed = aiDetectionStates[ai];

                        if (!lastNoticed && currentNoticed)
                        {
                            UnityEngine.Debug.Log($"CialloDetect: AI注意到玩家！AI: {ai.gameObject.name}");
                        }
                        else if (lastNoticed && !currentNoticed)
                        {
                            UnityEngine.Debug.Log($"CialloDetect: AI不再注意玩家 AI: {ai.gameObject.name}");
                        }
                    }

                    aiDetectionStates[ai] = currentNoticed;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"CialloDetect: 检查noticed变化时出错: {e.Message}");
            }
        }

        private void CheckDirectDetection(AICharacterController ai)
        {
            try
            {
                if (ai.searchedEnemy != null && Utilities.IsPlayerCharacter(ai.searchedEnemy))
                {
                    Vector3 aiPosition = ai.transform.position + Vector3.up * 1.5f;
                    Vector3 playerPosition = ai.searchedEnemy.transform.position + Vector3.up * 1.5f;
                    Vector3 direction = (playerPosition - aiPosition).normalized;
                    float distance = Vector3.Distance(aiPosition, playerPosition);

                    if (distance <= ai.sightDistance)
                    {
                        float angle = Vector3.Angle(ai.transform.forward, direction);
                        if (angle <= ai.sightAngle * 0.5f)
                        {
                            RaycastHit hit;
                            if (Physics.Raycast(aiPosition, direction, out hit, distance))
                            {
                                if (hit.collider.gameObject == ai.searchedEnemy.gameObject ||
                                    hit.collider.transform.IsChildOf(ai.searchedEnemy.transform))
                                {
                                    UnityEngine.Debug.Log($"CialloDetect: 直接检测到AI {ai.gameObject.name} 发现玩家，距离: {distance}, 角度: {angle}");

                                    if (CanPlaySoundForAI(ai))
                                    {
                                        AudioManager.PlayPlayerSound();
                                        RecordAIDetection(ai);
                                    }
                                }
                                else
                                {
                                    UnityEngine.Debug.Log($"CialloDetect: 视线被遮挡: {hit.collider.gameObject.name}");
                                }
                            }
                            else
                            {
                                UnityEngine.Debug.Log("CialloDetect: 射线检测未命中");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"CialloDetect: 直接检测时出错: {e.Message}");
            }
        }

        private void CleanupDestroyedAI()
        {
            var aiToRemove = new List<AICharacterController>();
            foreach (var ai in lastSearchedEnemies.Keys)
            {
                if (ai == null)
                {
                    aiToRemove.Add(ai);
                }
            }

            foreach (var ai in aiToRemove)
            {
                lastSearchedEnemies.Remove(ai);
                aiDetectionStates.Remove(ai);
                aiDetectionRecords.Remove(ai);
            }
        }

        private void CleanupExpiredRecords()
        {
            var recordsToRemove = new List<AICharacterController>();
            float currentTime = Time.time;

            foreach (var record in aiDetectionRecords)
            {
                if (record.Key == null || (currentTime - record.Value.lastDetectionTime) > (ConfigManager.cd * 2))
                {
                    recordsToRemove.Add(record.Key);
                }
            }

            foreach (var ai in recordsToRemove)
            {
                aiDetectionRecords.Remove(ai);
            }
        }

        private bool CanPlaySoundForAI(AICharacterController ai)
        {
            if (!aiDetectionRecords.ContainsKey(ai))
            {
                return true;
            }

            var record = aiDetectionRecords[ai];
            float currentTime = Time.time;

            if (record.isInCooldown)
            {
                if (currentTime - record.cooldownStartTime >= ConfigManager.cd)
                {
                    record.Reset();
                    UnityEngine.Debug.Log($"CialloDetect: AI {ai.gameObject.name} 冷却期结束，重置计数");
                    return true;
                }
                else
                {
                    UnityEngine.Debug.Log($"CialloDetect: AI {ai.gameObject.name} 处于冷却期，跳过音效播放");
                    return false;
                }
            }

            record.CleanupExpiredTimestamps(currentTime, ConfigManager.clock);
            return true;
        }

        private void RecordAIDetection(AICharacterController ai)
        {
            if (!aiDetectionRecords.ContainsKey(ai))
            {
                aiDetectionRecords[ai] = new AIDetectionRecord();
            }

            var record = aiDetectionRecords[ai];
            float currentTime = Time.time;

            record.detectionTimestamps.Add(currentTime);
            record.lastDetectionTime = currentTime;

            record.CleanupExpiredTimestamps(currentTime, ConfigManager.clock);

            if (record.detectionTimestamps.Count >= ConfigManager.counter)
            {
                record.isInCooldown = true;
                record.cooldownStartTime = currentTime;
                UnityEngine.Debug.Log($"CialloDetect: AI {ai.gameObject.name} 在{ConfigManager.clock}秒内触发{ConfigManager.counter}次，进入{ConfigManager.cd}秒冷却期");
            }

            UnityEngine.Debug.Log($"CialloDetect: AI {ai.gameObject.name} 当前计数: {record.detectionTimestamps.Count}/{ConfigManager.counter}");
        }

        public void Cleanup()
        {
            // GameObject.FindObjectOfType<MonoBehaviour>()?.CancelInvoke("MonitorAIStates");
            lastSearchedEnemies.Clear();
            aiDetectionStates.Clear();
            aiDetectionRecords.Clear();
        }
    }
}