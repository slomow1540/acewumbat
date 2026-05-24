using UnityEngine;

namespace Util
{
    [System.Serializable]
    public class LevelData
    {
        public string title;
        public string location;
        public string threat;

        public Sprite previewImage;
    }

    [System.Serializable]
    public class PlaneData
    {
        public string planeName;
        public int price;

        public float speed;
        public float armor;
        public float handling;

        public GameObject prefab;
    }
}
