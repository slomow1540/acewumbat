using UnityEngine;
using Util;

public class HangarManager : MonoBehaviour
{
    [Header("Data")]
    public PlaneData[] planes;

    [Header("Prefab")]
    public HangarSlot slotPrefab;

    [Header("Parent")]
    public Transform slotParent;

    [Header("Camera")]
    public CameraMover cameraMover;

    private HangarSlot[] slots;
    private int currentIndex;

    const float spacing = 0.07f;
    const float floorOffset = 0.0308f;
    const float rightX = 0.14f;
    const float leftX = 0f;

    void Start()
    {
        GenerateHangar();
        MoveTo(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
            Next();

        if (Input.GetKeyDown(KeyCode.A))
            Previous();
    }

    void GenerateHangar()
    {
        slots = new HangarSlot[18];

        int planeIndex = 0;

        for (int floor = 0; floor < 2; floor++)
        {
            float z = floor * floorOffset;

            for (int i = 0; i < 9; i++)
            {
                HangarSlot slot =
                    Instantiate(slotPrefab, slotParent);

                slot.transform.localPosition =
                    GetSlotPosition(i, z);

                slot.transform.localRotation =
                    Quaternion.identity;

                SetupAnchor(slot.anchor, i);

                if (planeIndex < planes.Length)
                    slot.Setup(planes[planeIndex]);

                slots[floor * 9 + i] = slot;

                planeIndex++;
            }
        }

        SetupCameraPoints();
    }

    Vector3 GetSlotPosition(int index, float floorZ)
    {
        switch (index)
        {
            // kanan
            case 0: return new Vector3(0f, 0f, floorZ);       // P1
            case 1: return new Vector3(0f, 0.07f, floorZ);    // P2
            case 2: return new Vector3(0f, 0.14f, floorZ);    // P3

            // belakang
            case 3: return new Vector3(0f, 0.21f, floorZ);    // P4
            case 4: return new Vector3(0.05f, 0.21f, floorZ); // P5
            case 5: return new Vector3(0.10f, 0.21f, floorZ); // P6

            // kiri
            case 6: return new Vector3(0.10f, 0.14f, floorZ); // P7
            case 7: return new Vector3(0.10f, 0.07f, floorZ); // P8
            case 8: return new Vector3(0.10f, 0f, floorZ);    // P9
        }

        return Vector3.zero;
    }

    void SetupAnchor(Transform anchor, int index)
    {
        float rotZ = 0f;

        switch (index)
        {
            // kanan
            case 0:
            case 1:
            case 2:
                rotZ = 0f;
                break;

            // belakang
            case 3:
                rotZ = -45f;
                break;

            case 4:
                rotZ = -90f;
                break;

            case 5:
                rotZ = -135f;
                break;

            // kiri
            case 6:
            case 7:
            case 8:
                rotZ = -180f;
                break;
        }

        anchor.localRotation =
            Quaternion.Euler(0f, 0f, rotZ);
    }

    void SetupCameraPoints()
    {
        cameraMover.points =
            new Transform[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            cameraMover.points[i] =
                slots[i].cameraPoint;
        }
    }

    public void Next()
    {
        currentIndex++;

        if (currentIndex >= slots.Length)
            currentIndex = 0;

        MoveTo(currentIndex);
    }

    public void Previous()
    {
        currentIndex--;

        if (currentIndex < 0)
            currentIndex = slots.Length - 1;

        MoveTo(currentIndex);
    }

    void MoveTo(int index)
    {
        cameraMover.MoveTo(index);

        PlaneData plane = slots[index].plane;

        if (plane != null)
            Debug.Log(plane.planeName);
    }
}