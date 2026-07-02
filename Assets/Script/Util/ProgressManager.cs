using UnityEngine;

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
    static string GetOwnedKey(int planeIndex)
    {
        return "owned_plane_" + planeIndex;
    }

    public static bool IsOwned(int planeIndex)
    {
        return PlayerPrefs.GetInt(GetOwnedKey(planeIndex), 0) == 1;
    }

    public static void UnlockPlane(int planeIndex)
    {
        PlayerPrefs.SetInt(GetOwnedKey(planeIndex), 1);
        PlayerPrefs.Save();
    }

    public static bool BuyPlane(int planeIndex, int price)
    {
        if (IsOwned(planeIndex))
            return true;

        bool success = SpendCurrency(price);

        if (!success)
            return false;

        UnlockPlane(planeIndex);
        return true;
    }

    // ======================
    // EQUIPPED PLANE
    // ======================

    const string equippedPlaneKey = "equipped_plane";

    public static void EquipPlane(int planeIndex)
    {
        if (!IsOwned(planeIndex))
            return;

        PlayerPrefs.SetInt(equippedPlaneKey, planeIndex);
        PlayerPrefs.Save();
    }

    public static int GetEquippedPlane()
    {
        return PlayerPrefs.GetInt(equippedPlaneKey, 0);
    }

    public static bool IsEquipped(int planeIndex)
    {
        return GetEquippedPlane() == planeIndex;
    }

    // ======================
    // FIRST TIME SETUP
    // ======================

    const string initializedKey = "player_initialized";

    public static void Initialize()
    {
        bool initialized = PlayerPrefs.GetInt(initializedKey, 0) == 1;

        if (initialized)
            return;

        SetCurrency(0);
        UnlockPlane(0);
        EquipPlane(0);

        PlayerPrefs.SetInt(initializedKey, 1);
        PlayerPrefs.Save();
    }
}
