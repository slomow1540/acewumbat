using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Systems")]
    public CameraMover cameraMover;
    public SkyboxController skyboxController;
    public LightingController lightingController;
    public PlaneAnim planeAnim;

    public enum MenuType
    {
        Idle,
        Hangar,
        Mission,
        Survival,
        Settings,
        Credits,
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        skyboxController.ApplyPreset(0);
        lightingController.ApplyLighting(LightingController.TimeOfDay.Morning);
        planeAnim.SetState(PlaneAnim.PlaneState.Idle);
        cameraMover.MoveTo(0);
    }

    public void ApplyMenu(MenuType type)
    {
        switch (type)
        {
            case MenuType.Idle:
                cameraMover.MoveTo(0);
                break;

            case MenuType.Hangar:
                cameraMover.MoveTo(1);
                break;

            case MenuType.Mission:
                cameraMover.MoveTo(2);
                skyboxController.ApplyPreset(0);
                lightingController.ApplyLighting(LightingController.TimeOfDay.Morning);
                planeAnim.SetState(PlaneAnim.PlaneState.Mission);
                break;

            case MenuType.Survival:
                cameraMover.MoveTo(3);
                skyboxController.ApplyPreset(1);
                lightingController.ApplyLighting(LightingController.TimeOfDay.Night);
                planeAnim.SetState(PlaneAnim.PlaneState.Survival);
                break;

            case MenuType.Settings:
                break;

            case MenuType.Credits:
                cameraMover.MoveTo(4);
                skyboxController.ApplyPreset(2);
                lightingController.ApplyLighting(LightingController.TimeOfDay.Sunset);
                break;
        }
    }
}
