using UnityEngine;
using System;
using System.IO;

/// <summary>
/// Cross-platform save/load. Uses JsonUtility instead of BinaryFormatter:
/// BinaryFormatter is obsolete, has known deserialization vulnerabilities, and
/// is unreliable under IL2CPP (the backend both Android and iOS builds use).
/// </summary>
public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "one_more_save.json");

    [Serializable]
    public class SaveData
    {
        public int bestScore;
        public bool soundEnabled = true;
        public bool musicEnabled = true;
        public bool hapticsEnabled = true;
        public int totalGamesPlayed;
        public int totalScore;
        public bool attRequested; // iOS App Tracking Transparency prompt already shown
    }

    private static SaveData cache;

    public static SaveData LoadGameData()
    {
        if (cache != null) return cache;

        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                cache = JsonUtility.FromJson<SaveData>(json);
                if (cache == null) cache = new SaveData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Failed to load save data, using defaults: {e.Message}");
                cache = new SaveData();
            }
        }
        else
        {
            cache = new SaveData();
        }

        return cache;
    }

    public static void SaveGameData(SaveData data)
    {
        cache = data;
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
        }
    }

    public static void SaveBestScore(int bestScore)
    {
        SaveData data = LoadGameData();
        data.bestScore = bestScore;
        SaveGameData(data);
    }

    public static int LoadBestScore() => LoadGameData().bestScore;

    public static void IncrementTotalGames()
    {
        SaveData data = LoadGameData();
        data.totalGamesPlayed++;
        SaveGameData(data);
    }

    public static void SetAudioSettings(bool sound, bool music, bool haptics)
    {
        SaveData data = LoadGameData();
        data.soundEnabled = sound;
        data.musicEnabled = music;
        data.hapticsEnabled = haptics;
        SaveGameData(data);
    }

    public static void SetATTRequested(bool requested)
    {
        SaveData data = LoadGameData();
        data.attRequested = requested;
        SaveGameData(data);
    }
}
