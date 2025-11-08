using System;
using System.IO;
using Duckov.Modding;
using UnityEngine;

namespace CialloDetect
{
    public static class ConfigManager
    {
        public static string configPath;
        public static bool _modConfigInitialized = false;
        public static float volume = 1.0f;
        public static float clock = 3.0f;
        public static int counter = 2;
        public static float cd = 5.0f;
        private static bool modConfigAvailable = false;
        private const string MOD_NAME = "CialloDetect";

        public static void Initialize(string dllPath)
        {
            configPath = Path.Combine(dllPath, "CialloDetect.cfg");
        }

        public static void TryInitializeModConfig()
        {
            if (_modConfigInitialized) return;

            if (ModConfigAPI.IsAvailable())
            {
                modConfigAvailable = true;
                LoadConfigFromModConfig();
                InitializeModConfig();
                _modConfigInitialized = true;
                UnityEngine.Debug.Log("CialloDetect: ModConfig 初始化成功");
            }
            else
            {
                modConfigAvailable = false;
                LoadConfigFromFile();
                UnityEngine.Debug.Log("CialloDetect: 使用文件配置系统");
            }
        }

        private static void LoadConfigFromFile()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string[] lines = File.ReadAllLines(configPath);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();

                            if (key == "volume" && float.TryParse(value, out float vol))
                            {
                                volume = vol;
                            }
                            else if (key == "clock" && float.TryParse(value, out float clk))
                            {
                                clock = clk;
                            }
                            else if (key == "counter" && int.TryParse(value, out int cnt))
                            {
                                counter = cnt;
                            }
                            else if (key == "cd" && float.TryParse(value, out float coolDown))
                            {
                                cd = coolDown;
                            }
                        }
                    }
                    UnityEngine.Debug.Log("CialloDetect: 文件配置加载成功");
                    UnityEngine.Debug.Log($"CialloDetect: 防刷配置 - 时间窗口: {clock}s, 触发次数: {counter}, 冷却时间: {cd}s");
                }
                else
                {
                    File.WriteAllText(configPath, "volume=1.0\nclock=3.0\ncounter=2\ncd=5.0");
                    UnityEngine.Debug.Log("CialloDetect: 创建默认配置文件");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"CialloDetect: 加载文件配置失败: {e}");
            }
        }

        private static void LoadConfigFromModConfig()
        {
            if (!modConfigAvailable) return;

            volume = ModConfigAPI.SafeLoad<float>(MOD_NAME, "volume", 1.0f);
            clock = ModConfigAPI.SafeLoad<float>(MOD_NAME, "clock", 3.0f);
            counter = ModConfigAPI.SafeLoad<int>(MOD_NAME, "counter", 2);
            cd = ModConfigAPI.SafeLoad<float>(MOD_NAME, "cd", 5.0f);

            UnityEngine.Debug.Log($"CialloDetect: ModConfig配置加载成功");
            UnityEngine.Debug.Log($"CialloDetect: 音量={volume}, 时间窗口={clock}s, 触发次数={counter}, 冷却时间={cd}s");
        }

        private static void InitializeModConfig()
        {
            if (!modConfigAvailable) return;

            try
            {
                ModConfigAPI.SafeAddInputWithSlider(
                    MOD_NAME,
                    "volume",
                    "音效音量",
                    typeof(float),
                    volume,
                    new Vector2(0f, 1f)
                );

                ModConfigAPI.SafeAddInputWithSlider(
                    MOD_NAME,
                    "clock",
                    "统计时间窗口(秒)",
                    typeof(float),
                    clock,
                    new Vector2(1f, 30f)
                );

                ModConfigAPI.SafeAddInputWithSlider(
                    MOD_NAME,
                    "counter",
                    "触发次数阈值",
                    typeof(int),
                    counter,
                    new Vector2(1, 10)
                );

                ModConfigAPI.SafeAddInputWithSlider(
                    MOD_NAME,
                    "cd",
                    "冷却时间(秒)",
                    typeof(float),
                    cd,
                    new Vector2(1f, 10f)
                );

                ModConfigAPI.SafeAddOnOptionsChangedDelegate(OnConfigChanged);

                UnityEngine.Debug.Log("CialloDetect: ModConfig界面初始化成功");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"CialloDetect: ModConfig界面初始化失败: {e}");
            }
        }

        private static void OnConfigChanged(string key)
        {
            if (!modConfigAvailable) return;

            UnityEngine.Debug.Log($"CialloDetect: 配置变更: {key}");

            bool configUpdated = false;

            if (key == $"{MOD_NAME}_volume")
            {
                volume = ModConfigAPI.SafeLoad<float>(MOD_NAME, "volume", 1.0f);
                UnityEngine.Debug.Log($"CialloDetect: 音量更新为: {volume}");
                configUpdated = true;
            }
            else if (key == $"{MOD_NAME}_clock")
            {
                clock = ModConfigAPI.SafeLoad<float>(MOD_NAME, "clock", 3.0f);
                UnityEngine.Debug.Log($"CialloDetect: 时间窗口更新为: {clock}");
                configUpdated = true;
            }
            else if (key == $"{MOD_NAME}_counter")
            {
                counter = ModConfigAPI.SafeLoad<int>(MOD_NAME, "counter", 2);
                UnityEngine.Debug.Log($"CialloDetect: 触发次数更新为: {counter}");
                configUpdated = true;
            }
            else if (key == $"{MOD_NAME}_cd")
            {
                cd = ModConfigAPI.SafeLoad<float>(MOD_NAME, "cd", 5.0f);
                UnityEngine.Debug.Log($"CialloDetect: 冷却时间更新为: {cd}");
                configUpdated = true;
            }

            if (configUpdated)
            {
                try
                {
                    string content =
                        $"volume={volume}\n" +
                        $"clock={clock}\n" +
                        $"counter={counter}\n" +
                        $"cd={cd}";
                    File.WriteAllText(configPath, content);
                    UnityEngine.Debug.Log("CialloDetect: 配置已同步保存到文件: " + configPath);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"CialloDetect: 保存配置到文件失败: {e}");
                }
            }
        }

        public static void Cleanup()
        {
            if (modConfigAvailable)
            {
                ModConfigAPI.SafeRemoveOnOptionsChangedDelegate(OnConfigChanged);
            }
        }
    }
}