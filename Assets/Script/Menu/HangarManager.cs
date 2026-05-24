using System.Collections;
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
    private int currentCycle;

    private bool isTransitioning;

    private const int cycleSize = 18;

    const float floorOffset = 0.0308f;

    void Start()
    {
        GenerateHangar();
        RenderCycle();

        currentIndex = 0;
        MoveTo(currentIndex);
    }

    void Update()
    {
        if (isTransitioning)
            return;

        if (Input.GetKeyDown(KeyCode.D))
            Next();

        if (Input.GetKeyDown(KeyCode.A))
            Previous();
    }

    void GenerateHangar()
    {
        slots = new HangarSlot[cycleSize];

        for (int i = 0; i < cycleSize; i++)
        {
            int floor = i / 9;
            int localIndex = i % 9;

            float z = floor * floorOffset;

            HangarSlot slot = Instantiate(slotPrefab, slotParent);

            slot.transform.localPosition = GetSlotPosition(localIndex, z);

            slot.transform.localRotation = Quaternion.identity;

            SetupAnchor(slot.anchor, localIndex);

            // refresh posisi final
            slot.RefreshPosition();

            slots[i] = slot;
        }

        SetupCameraPoints();
    }

    void RenderCycle()
    {
        int startIndex = currentCycle * cycleSize;

        for (int i = 0; i < cycleSize; i++)
        {
            int planeIndex = startIndex + i;

            if (planeIndex >= planes.Length)
            {
                slots[i].plane = null;
                slots[i].gameObject.SetActive(false);
                continue;
            }

            slots[i].gameObject.SetActive(true);
            slots[i].Setup(planes[planeIndex]);
        }
    }

    Vector3 GetSlotPosition(int index, float floorZ)
    {
        switch (index)
        {
            // kanan
            case 0:
                return new Vector3(0f, 0f, floorZ);

            case 1:
                return new Vector3(0f, 0.07f, floorZ);

            case 2:
                return new Vector3(0f, 0.14f, floorZ);

            // belakang
            case 3:
                return new Vector3(0f, 0.21f, floorZ);

            case 4:
                return new Vector3(0.05f, 0.21f, floorZ);

            case 5:
                return new Vector3(0.10f, 0.21f, floorZ);

            // kiri
            case 6:
                return new Vector3(0.10f, 0.14f, floorZ);

            case 7:
                return new Vector3(0.10f, 0.07f, floorZ);

            case 8:
                return new Vector3(0.10f, 0f, floorZ);
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

        anchor.localRotation = Quaternion.Euler(0f, 0f, rotZ);
    }

    void SetupCameraPoints()
    {
        cameraMover.points = new Transform[cycleSize];

        for (int i = 0; i < cycleSize; i++)
        {
            cameraMover.points[i] = slots[i].cameraPoint;
        }
    }

    public void Next()
    {
        if (isTransitioning)
            return;

        currentIndex++;

        int visibleCount = GetVisibleCount(currentCycle);

        if (currentIndex >= visibleCount)
        {
            int targetCycle = currentCycle + 1;

            int totalCycles = Mathf.CeilToInt((float)planes.Length / cycleSize);

            if (targetCycle >= totalCycles)
                targetCycle = 0;

            currentIndex = 0;

            StartCoroutine(ChangeCycle(targetCycle));

            return;
        }

        MoveTo(currentIndex);
    }

    public void Previous()
    {
        if (isTransitioning)
            return;

        currentIndex--;

        if (currentIndex < 0)
        {
            int targetCycle = currentCycle - 1;

            if (targetCycle < 0)
            {
                targetCycle = Mathf.CeilToInt((float)planes.Length / cycleSize) - 1;
            }

            currentIndex = GetVisibleCount(targetCycle) - 1;

            StartCoroutine(ChangeCycle(targetCycle));

            return;
        }

        MoveTo(currentIndex);
    }

    int GetVisibleCount(int cycle)
    {
        int startIndex = cycle * cycleSize;

        return Mathf.Min(cycleSize, planes.Length - startIndex);
    }

    void MoveTo(int index)
    {
        cameraMover.MoveTo(index);

        int realIndex = currentCycle * cycleSize + index;

        if (realIndex >= planes.Length)
            return;

        PlaneData plane = planes[realIndex];

        if (plane != null)
        {
            Debug.Log("Selected Plane: " + plane.planeName);
        }
    }

    IEnumerator ChangeCycle(int newCycle)
    {
        isTransitioning = true;

        for (int i = 9; i < slots.Length; i++)
        {
            slots[i].Hide((i - 9) * 0.03f);
        }

        yield return new WaitForSeconds(0.22f);

        for (int i = 0; i < 9; i++)
        {
            slots[i].Hide(i * 0.03f);
        }

        yield return new WaitForSeconds(0.45f);

        currentCycle = newCycle;
        RenderCycle();

        for (int i = 9; i < slots.Length; i++)
        {
            if (slots[i].plane != null)
            {
                slots[i].Show((i - 9) * 0.03f);
            }
        }

        yield return new WaitForSeconds(0.22f);

        for (int i = 0; i < 9; i++)
        {
            if (slots[i].plane != null)
            {
                slots[i].Show(i * 0.03f);
            }
        }

        yield return new WaitForSeconds(0.45f);

        isTransitioning = false;

        MoveTo(currentIndex);
    }
}
