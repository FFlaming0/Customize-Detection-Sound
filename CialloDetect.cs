using System.IO;
using System.Reflection;
using UnityEngine;

namespace CialloDetect
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private DetectionMonitor _detectionMonitor;
        private bool _isMonitoring = false;

        private void Awake()
        {
            string dllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ConfigManager.Initialize(dllPath);
            AudioManager.Initialize(dllPath);
            _detectionMonitor = new DetectionMonitor();
        }

        private void OnEnable()
        {
            if (!ConfigManager._modConfigInitialized)
            {
                Invoke(nameof(TryInitializeModConfig), 1f);
            }
            Invoke("StartAIMonitoring", 2f);
        }

        private void TryInitializeModConfig() => ConfigManager.TryInitializeModConfig();

        private void StartAIMonitoring()
        {
            _isMonitoring = true;
            Debug.Log("CialloDetect: 开始AI监控");
            // 每0.5秒调用一次MonitorAIStates
            InvokeRepeating(nameof(MonitorAIStates), 0f, 0.5f);
        }

        private void MonitorAIStates()
        {
            if (_isMonitoring && _detectionMonitor != null)
            {
                _detectionMonitor.MonitorAIStates();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                AudioManager.PlayPlayerSound();
            }
        }

        private void OnDisable()
        {
            _isMonitoring = false;
            CancelInvoke(nameof(MonitorAIStates));

            ConfigManager.Cleanup();
            _detectionMonitor?.Cleanup();
            Debug.Log("CialloDetect: Mod已禁用");
        }
    }
}