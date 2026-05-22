using UnityEngine;
using Util;

public class HangarSlot : MonoBehaviour
{
    public Transform anchor;
    public Transform cameraPoint;
    public Transform planePoint;

    [HideInInspector]
    public PlaneData plane;

    private GameObject spawnedPlane;

    public Transform LookTarget => planePoint;

    public void Setup(PlaneData data)
    {
        plane = data;

        if (spawnedPlane != null)
            Destroy(spawnedPlane);

        if (data != null && data.prefab != null)
        {
            spawnedPlane = Instantiate(
                data.prefab,
                planePoint.position,
                planePoint.rotation,
                planePoint
            );
        }
    }
}