using UnityEngine;

namespace Util
{
    public static class ProgressManager
    {
        public static void SaveScore(int levelIndex, int score)
        {
            string key = "score_" + levelIndex;

            int current = PlayerPrefs.GetInt(key, 0);

            if (score > current)
            {
                PlayerPrefs.SetInt(key, score);
            }

            PlayerPrefs.Save();
        }

        public static int GetScore(int levelIndex)
        {
            return PlayerPrefs.GetInt("score_" + levelIndex, 0);
        }

        public static void SaveTime(int levelIndex, float time)
        {
            string key = "time_" + levelIndex;

            float current = PlayerPrefs.GetFloat(key, Mathf.Infinity);

            if (time < current)
            {
                PlayerPrefs.SetFloat(key, time);
            }

            PlayerPrefs.Save();
        }

        public static float GetTime(int levelIndex)
        {
            return PlayerPrefs.GetFloat("time_" + levelIndex, 0f);
        }

        const string currencyKey = "player_currency";

        public static int GetCurrency()
        {
            return PlayerPrefs.GetInt(currencyKey, 0);
        }

        public static void SetCurrency(int amount)
        {
            PlayerPrefs.SetInt(currencyKey, Mathf.Max(0, amount));

            PlayerPrefs.Save();
        }

        public static void AddCurrency(int amount)
        {
            int current = GetCurrency();

            current += amount;

            PlayerPrefs.SetInt(currencyKey, Mathf.Max(0, current));

            PlayerPrefs.Save();
        }

        public static bool SpendCurrency(int amount)
        {
            int current = GetCurrency();

            if (current < amount)
                return false;

            current -= amount;

            PlayerPrefs.SetInt(currencyKey, current);

            PlayerPrefs.Save();

            return true;
        }

        public static bool HasCurrency(int amount)
        {
            return GetCurrency() >= amount;
        }
    }
}
