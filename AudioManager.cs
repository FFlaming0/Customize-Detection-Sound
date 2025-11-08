using System;
using System.Collections.Generic;
using System.IO;
using FMOD;
using FMODUnity;
using Newtonsoft.Json;
using UnityEngine;

namespace CialloDetect
{
    public static class AudioManager
    {
        public static Sound playerSound;
        public static Dictionary<string, List<Sound>> audioGroups = new Dictionary<string, List<Sound>>();
        public static string audioFolderPath;
        private static string[] enemyPrefix = { "scav", "melee-scav", "wolf", "melee-wolf", "usec", "lab", "ultraman" };

        public static void Initialize(string dllPath)
        {
            audioFolderPath = Path.Combine(dllPath, "audios");
            LoadAudioFiles();
        }

        private static void LoadAudioFiles()
        {
            audioGroups.Clear();

            if (!Directory.Exists(audioFolderPath))
            {
                UnityEngine.Debug.LogWarning($"CialloDetect: 音频文件夹不存在: {audioFolderPath}");
                return;
            }

            string soundPath = Path.Combine(audioFolderPath, "Ciallo.wav");
            if (File.Exists(soundPath))
            {
                RESULT result = RuntimeManager.CoreSystem.createSound(soundPath, MODE._2D | MODE.LOOP_OFF, out playerSound);
                if (result == RESULT.OK)
                {
                    UnityEngine.Debug.Log("CialloDetect: 音效加载成功");
                }
                else
                {
                    UnityEngine.Debug.LogError($"CialloDetect: 音效加载失败: {result}");
                }
            }
            else
            {
                UnityEngine.Debug.LogError("CialloDetect: 音效文件未找到: " + soundPath);
            }

            string[] audioFiles = Directory.GetFiles(audioFolderPath, "*.wav");
            foreach (string filePath in audioFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                int underscoreIndex = fileName.IndexOf('_');

                if (underscoreIndex <= 0) continue;

                string prefix = fileName.Substring(0, underscoreIndex);
                Sound sound;
                RESULT result = RuntimeManager.CoreSystem.createSound(filePath, MODE._2D | MODE.LOOP_OFF, out sound);

                if (result == RESULT.OK)
                {
                    if (!audioGroups.ContainsKey(prefix))
                    {
                        audioGroups[prefix] = new List<Sound>();
                    }
                    audioGroups[prefix].Add(sound);
                    UnityEngine.Debug.Log($"CialloDetect: 加载音频 [{prefix}]: {Path.GetFileName(filePath)}");
                }
                else
                {
                    UnityEngine.Debug.LogError($"CialloDetect: 音频加载失败 [{prefix}]: {Path.GetFileName(filePath)} - {result}");
                }
            }

            if (audioGroups.Count > 0)
            {
                Dictionary<string, int> prefixCounts = new Dictionary<string, int>();
                foreach (var group in audioGroups)
                {
                    prefixCounts[group.Key] = group.Value.Count;
                }

                string json = JsonConvert.SerializeObject(
                    prefixCounts,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }
                );

                string[] lines = json.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                string formattedOutput = "已加载音频前缀统计:\n";
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        formattedOutput += $"  {line.Trim()}\n";
                    }
                }

                UnityEngine.Debug.Log($"CialloDetect: {formattedOutput.Trim()}");
            }
            else
            {
                UnityEngine.Debug.LogWarning("CialloDetect: 未加载任何有效音频文件");
            }

            List<string> missingPrefixes = new List<string>();
            foreach (string prefix in enemyPrefix)
            {
                if (!audioGroups.ContainsKey(prefix))
                {
                    missingPrefixes.Add(prefix);
                }
            }

            if (missingPrefixes.Count > 0)
            {
                string missingJson = JsonConvert.SerializeObject(
                    missingPrefixes,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }
                );

                string[] missingLines = missingJson.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                string missingOutput = "未加载的预定义前缀:\n";
                foreach (string line in missingLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        missingOutput += $"  {line.Trim()}\n";
                    }
                }

                UnityEngine.Debug.LogWarning($"CialloDetect: {missingOutput.Trim()}");
            }
            else
            {
                UnityEngine.Debug.Log("CialloDetect: 所有预定义前缀均已成功加载");
            }
        }

        public static void PlayPlayerSound()
        {
            if (!playerSound.hasHandle())
            {
                UnityEngine.Debug.LogError("CialloDetect: 音效未正确加载");
                return;
            }

            try
            {
                ChannelGroup channelGroup;
                RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out channelGroup);

                Channel channel;
                RuntimeManager.CoreSystem.playSound(playerSound, channelGroup, false, out channel);

                if (channel.hasHandle())
                {
                    channel.setVolume(ConfigManager.volume);
                }

                UnityEngine.Debug.Log("CialloDetect: 播放发现音效");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"CialloDetect: 播放音效失败: {e}");
            }
        }

        public static void PlayRandomDetectionSound(string audioPrefix = "default")
        {
            if (audioGroups.TryGetValue(audioPrefix, out List<Sound> sounds) && sounds.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, sounds.Count);
                PlaySound(sounds[randomIndex], audioPrefix, randomIndex);
                return;
            }

            /*            if (audioGroups.TryGetValue("default", out List<Sound> defaultSounds) && defaultSounds.Count > 0)
                        {
                            int randomIndex = UnityEngine.Random.Range(0, defaultSounds.Count);
                            PlaySound(defaultSounds[randomIndex], "default", randomIndex);
                            return;
                        }*/
            try
            {
                ChannelGroup channelGroup;
                RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out channelGroup);

                Channel channel;
                RuntimeManager.CoreSystem.playSound(playerSound, channelGroup, false, out channel);

                if (channel.hasHandle())
                {
                    channel.setVolume(ConfigManager.volume);
                }

                UnityEngine.Debug.Log("CialloDetect: 播放发现音效");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"CialloDetect: 播放音效失败: {e}");
            }


            UnityEngine.Debug.LogWarning($"CialloDetect: 未找到可用的音频 (前缀: {audioPrefix})");
        }

        private static void PlaySound(Sound sound, string audioPrefix, int index)
        {
            if (!sound.hasHandle()) return;

            try
            {
                ChannelGroup channelGroup;
                RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out channelGroup);

                Channel channel;
                RuntimeManager.CoreSystem.playSound(sound, channelGroup, false, out channel);

                if (channel.hasHandle())
                {
                    channel.setVolume(ConfigManager.volume);
                }

                UnityEngine.Debug.Log($"CialloDetect: 播放音频: {audioPrefix}_{index:00}.wav");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"CialloDetect: 播放音效失败: {e}");
            }
        }
    }
}