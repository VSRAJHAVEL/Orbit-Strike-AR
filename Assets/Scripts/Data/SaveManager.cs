using System;
using System.IO;
using UnityEngine;
using OrbitStrike.Core;

namespace OrbitStrike.Data
{
    /// <summary>
    /// Manages persistent player profile, progress, unlocked rockets, shop upgrades, and settings.
    /// Serializes to JSON on disk across Android, iOS, and PC.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SaveFileName = "orbit_strike_save.json";
        private string _savePath;
        private PlayerData _data;

        public PlayerData Data => _data;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
            Load();
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(_data, true);
                File.WriteAllText(_savePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save game data: {ex.Message}");
            }
        }

        public void Load()
        {
            if (File.Exists(_savePath))
            {
                try
                {
                    string json = File.ReadAllText(_savePath);
                    _data = JsonUtility.FromJson<PlayerData>(json);
                }
                catch
                {
                    _data = new PlayerData();
                }
            }
            else
            {
                _data = new PlayerData();
                Save();
            }
        }

        public void AddCoins(int amount)
        {
            _data.coins += amount;
            EventBus.TriggerCoinsChanged(_data.coins, amount);
            Save();
        }

        public bool SpendCoins(int amount)
        {
            if (_data.coins >= amount)
            {
                _data.coins -= amount;
                EventBus.TriggerCoinsChanged(_data.coins, -amount);
                Save();
                return true;
            }
            return false;
        }

        public void SaveLevelStars(int levelIndex, int stars)
        {
            _data.SetLevelStars(levelIndex, stars);

            if (levelIndex >= _data.highestUnlockedLevel && levelIndex < 30)
            {
                _data.highestUnlockedLevel = levelIndex + 1;
            }

            Save();
        }

        public bool IsRocketUnlocked(string rocketId)
        {
            return _data.unlockedRockets.Contains(rocketId);
        }

        public void UnlockRocket(string rocketId)
        {
            if (!_data.unlockedRockets.Contains(rocketId))
            {
                _data.unlockedRockets.Add(rocketId);
                Save();
            }
        }
    }
}

