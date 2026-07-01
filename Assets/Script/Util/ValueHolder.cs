using UnityEngine;

public class ValueHolder : MonoBehaviour
{
    public static ValueHolder Instance;

    [Header("Data")]
    public GameObject SelectedPlane;
    public int Points;
    public string SpecialWeaponName;

    [Header("Shop")]
    public bool[] ownedPlanes;
    public int equippedPlaneIndex = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start() { }
    void Update() { }

    public bool IsOwned(int index)
    {
        if (ownedPlanes == null || index < 0 || index >= ownedPlanes.Length)
            return false;

        return ownedPlanes[index];
    }

    public bool IsEquipped(int index)
    {
        return equippedPlaneIndex == index;
    }

    // Call this before reading/writing ownedPlanes so the array always
    // matches the current planes list length (and keeps old data on resize).
    public void EnsureOwnedArray(int size)
    {
        if (ownedPlanes != null && ownedPlanes.Length == size)
            return;

        bool[] newArray = new bool[size];

        if (ownedPlanes != null)
        {
            for (int i = 0; i < Mathf.Min(ownedPlanes.Length, size); i++)
                newArray[i] = ownedPlanes[i];
        }

        ownedPlanes = newArray;
    }
}