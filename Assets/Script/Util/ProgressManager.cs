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
                PlayerPrefs.SetInt(key, score);
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
                PlayerPrefs.SetFloat(key, time);
        }

        public static float GetTime(int levelIndex)
        {
            float t = PlayerPrefs.GetFloat("time_" + levelIndex, 0f);
            return t;
        }
    }
}