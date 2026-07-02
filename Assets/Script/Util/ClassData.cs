using UnityEngine;

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
    [Header("Info")]
    public string planeName;

    [Header("Economy")]
    public int price;

    [Header("Preview")]
    public GameObject prefab;

    public GameObject playerprefab;

    [Header("Flight Stats")]
    [Range(0, 100)]
    public float thrust;

    [Range(0, 100)]
    public float maneuverability;

    [Range(0, 100)]
    public float health;

    [Header("Gun")]
    [Range(0, 100)]
    public float gunDamage;

    [Range(0, 100)]
    public float gunFireRate;

    public int gunAmmoCount;

    [Range(0, 100)]
    public float aimAssistRange;

    [Header("Missile")]
    [Range(0, 100)]
    public float missileLockTime;

    [Range(0, 100)]
    public float missileRange;

    [Range(0, 100)]
    public float missileDamage;

    [Range(0, 100)]
    public float missileManeuverability;

    public int missileAmmoCount;

    public string GetType()
    {
        float mobilityScore = thrust + maneuverability;

        float gunScore = gunDamage + gunFireRate + aimAssistRange;

        float missileScore = missileDamage + missileRange + missileManeuverability;

        float durabilityScore = health;

        // Tanky aircraft
        if (durabilityScore >= 80 && missileAmmoCount >= 6)
        {
            return "HEAVY FIGHTER";
        }

        // Fast missile hunter
        if (mobilityScore >= 160 && missileScore > gunScore)
        {
            return "INTERCEPTOR";
        }

        // Gun-focused
        if (gunScore >= missileScore + 25)
        {
            return "AIR SUPERIORITY";
        }

        // Missile-focused
        if (missileScore >= gunScore + 25)
        {
            return "MISSILE PLATFORM";
        }

        // High agility close combat
        if (maneuverability >= 85 && gunFireRate >= 70)
        {
            return "DOGFIGHTER";
        }

        // Jack of all trades
        return "MULTIROLE FIGHTER";
    }
}

[CreateAssetMenu(fileName = "PlaneDatabase", menuName = "Game/Plane Database")]
public class PlaneDatabase : ScriptableObject
{
    public PlaneData[] planes;
}
