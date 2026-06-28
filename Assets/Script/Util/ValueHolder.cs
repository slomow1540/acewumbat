using UnityEngine;

public class ValueHolder : MonoBehaviour
{
    public static ValueHolder Instance;

    [Header("Data")]
    public GameObject SelectedPlane;

    public int Points;

    public string SpecialWeaponName;

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
}
