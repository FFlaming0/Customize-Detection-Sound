using System.Collections.Generic;

namespace CialloDetect
{
    public class AIDetectionRecord
    {
        public List<float> detectionTimestamps = new List<float>(); // 检测时间戳列表
        public bool isInCooldown = false;                          // 是否处于冷却期
        public float cooldownStartTime = 0f;                       // 冷却开始时间
        public float lastDetectionTime = 0f;                       // 最后检测时间

        /// 清理过期的时间戳
        public void CleanupExpiredTimestamps(float currentTime, float timeWindow)
        {
            detectionTimestamps.RemoveAll(timestamp => currentTime - timestamp > timeWindow);
        }

        /// 重置记录
        public void Reset()
        {
            detectionTimestamps.Clear();
            isInCooldown = false;
            cooldownStartTime = 0f;
        }
    }
}