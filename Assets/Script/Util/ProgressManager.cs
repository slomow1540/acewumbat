using UnityEngine;

namespace Util
{
    public static class ProgressManager
    {
        // ======================
        // LEVEL PROGRESS
        // ======================

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

        // ======================
        // CURRENCY
        // ======================

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

        // ======================
        // PLANE OWNERSHIP
        // ======================

        const string equippedPlaneKey = "equipped_plane";

        static string GetOwnedKey(string planeName)
        {
            return "owned_plane_" + planeName;
        }

        public static bool IsOwned(string planeName)
        {
            return PlayerPrefs.GetInt(GetOwnedKey(planeName), 0) == 1;
        }

        public static void UnlockPlane(string planeName)
        {
            PlayerPrefs.SetInt(GetOwnedKey(planeName), 1);

            PlayerPrefs.Save();
        }

        public static bool BuyPlane(PlaneData plane)
        {
            if (plane == null)
                return false;

            if (IsOwned(plane.planeName))
            {
                return true;
            }

            bool success = SpendCurrency(plane.price);

            if (!success)
                return false;

            UnlockPlane(plane.planeName);

            return true;
        }

        // ======================
        // EQUIPPED PLANE
        // ======================

        public static void EquipPlane(string planeName)
        {
            if (!IsOwned(planeName))
            {
                return;
            }

            PlayerPrefs.SetString(equippedPlaneKey, planeName);

            PlayerPrefs.Save();
        }

        public static string GetEquippedPlane()
        {
            return PlayerPrefs.GetString(equippedPlaneKey, "");
        }

        public static bool IsEquipped(string planeName)
        {
            return GetEquippedPlane() == planeName;
        }

        // ======================
        // FIRST TIME SETUP
        // ======================

        const string initializedKey = "player_initialized";

        public static void Initialize(string starterPlane, int starterCurrency)
        {
            bool initialized = PlayerPrefs.GetInt(initializedKey, 0) == 1;

            if (initialized)
                return;

            SetCurrency(starterCurrency);

            UnlockPlane(starterPlane);

            EquipPlane(starterPlane);

            PlayerPrefs.SetInt(initializedKey, 1);

            PlayerPrefs.Save();
        }
    }
}
