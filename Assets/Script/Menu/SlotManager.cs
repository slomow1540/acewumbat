using System;
using System.Collections;
using UnityEngine;
using Util;

public class SlotManager : MonoBehaviour
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

    private bool canControl;

    public Action<PlaneData> onPlaneChanged;

    private AudioManager audioManager;
    public AudioClip oneFloor;
    public AudioClip twoFloor;

    void Start()
    {
        audioManager = AudioManager.Instance;

        GenerateHangar();

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].HideInstant();
        }

        currentIndex = 0;
    }

    void Update()
    {
        if (!canControl)
            return;

        if (isTransitioning)
            return;

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            Next();
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Previous();
        }
    }

    public void EnableControl()
    {
        canControl = true;
    }

    public void DisableControl()
    {
        canControl = false;
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

            slot.RefreshPosition();

            slots[i] = slot;
        }
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
                slots[i].HideInstant();
                continue;
            }
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

            currentCycle = targetCycle;

            currentIndex = Mathf.Clamp(GetVisibleCount(currentCycle) - 1, 0, cycleSize - 1);

            StartCoroutine(ChangeCycle(currentCycle));

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
        int realIndex = currentCycle * cycleSize + index;

        if (realIndex >= planes.Length)
            return;

        PlaneData plane = planes[realIndex];

        if (plane == null)
            return;

        cameraMover.MoveToTransform(slots[index].cameraPoint);

        onPlaneChanged?.Invoke(plane);

        Debug.Log("Selected Plane: " + plane.planeName);
    }

    public IEnumerator ShowSlots()
    {
        currentCycle = 0;
        currentIndex = 0;

        RenderCycle();

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].HideInstant();
        }

        yield return new WaitForSeconds(0.35f);

        for (int i = 9; i < slots.Length; i++)
        {
            if (slots[i].plane != null)
            {
                slots[i].Show((i - 9) * 0.04f);
            }
        }

        yield return new WaitForSeconds(0.25f);

        for (int i = 0; i < 9; i++)
        {
            if (slots[i].plane != null)
            {
                slots[i].Show(i * 0.04f);
            }
        }

        yield return new WaitForSeconds(0.7f);

        MoveTo(0);

        yield return new WaitForSeconds(cameraMover.moveDuration);

        EnableControl();
    }

    public IEnumerator HideSlots()
    {
        for (int i = 9; i < slots.Length; i++)
        {
            if (slots[i].plane != null)
            {
                slots[i].Hide((i - 9) * 0.03f);
            }
        }

        yield return new WaitForSeconds(0.22f);

        for (int i = 0; i < 9; i++)
        {
            if (slots[i].plane != null)
            {
                slots[i].Hide(i * 0.03f);
            }
        }

        yield return new WaitForSeconds(0.65f);

        currentCycle = 0;
        currentIndex = 0;

        RenderCycle();

        canControl = false;
    }

    IEnumerator ChangeCycle(int newCycle)
    {
        isTransitioning = true;
        canControl = false;

        for (int i = 9; i < slots.Length; i++)
        {
            if (slots[i].plane != null)
            {
                slots[i].Hide((i - 9) * 0.03f);
            }
        }

        yield return new WaitForSeconds(0.22f);

        for (int i = 0; i < 9; i++)
        {
            if (slots[i].plane != null)
            {
                slots[i].Hide(i * 0.03f);
            }
        }

        yield return new WaitForSeconds(0.55f);

        currentCycle = newCycle;

        RenderCycle();
        PlayFloorSound();

        int visible = GetVisibleCount(currentCycle);

        currentIndex = Mathf.Clamp(currentIndex, 0, visible - 1);

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

        yield return new WaitForSeconds(0.75f);

        MoveTo(currentIndex);

        yield return new WaitForSeconds(cameraMover.moveDuration);

        isTransitioning = false;
        canControl = true;
    }

    void PlayFloorSound()
    {
        int visibleCount = GetVisibleCount(currentCycle);

        AudioClip clip = visibleCount <= 9 ? oneFloor : twoFloor;

        if (clip != null)
        {
            audioManager.Play(clip);
        }
    }

    public IEnumerator EnterSlots()
    {
        canControl = false;
        currentCycle = 0;
        currentIndex = 0;

        RenderCycle();

        PlayFloorSound();

        GameManager.Instance.cameraMover.SetHangarMode();

        for (int i = 9; i < slots.Length; i++)
        {
            if (slots[i].plane != null)
                slots[i].Show((i - 9) * 0.04f);
        }

        yield return new WaitForSeconds(0.25f);

        for (int i = 0; i < 9; i++)
        {
            if (slots[i].plane != null)
                slots[i].Show(i * 0.04f);
        }

        yield return new WaitForSeconds(0.7f);

        MoveTo(0);

        yield return new WaitForSeconds(cameraMover.moveDuration);

        canControl = true;
    }

    public IEnumerator ExitRoutine()
    {
        canControl = false;
        isTransitioning = true;

        // Balik ke initial hangar dulu (camera mode menu, titik 1)
        GameManager.Instance.cameraMover.SetMenuMode();
        cameraMover.MoveTo(1);

        yield return new WaitForSeconds(cameraMover.moveDuration);

        PlayFloorSound();

        // Baru hide slot setelah camera sudah di initial hangar
        for (int i = 9; i < slots.Length; i++)
        {
            if (slots[i].plane != null)
                slots[i].Hide((i - 9) * 0.03f);
        }

        yield return new WaitForSeconds(0.22f);

        for (int i = 0; i < 9; i++)
        {
            if (slots[i].plane != null)
                slots[i].Hide(i * 0.03f);
        }

        yield return new WaitForSeconds(0.65f);

        currentCycle = 0;
        currentIndex = 0;

        RenderCycle();

        isTransitioning = false;
    }

    int GetLastValidSlot()
    {
        int visibleCount = GetVisibleCount(currentCycle);

        return Mathf.Max(0, visibleCount - 1);
    }

    public IEnumerator ReturnToInitial()
    {
        canControl = false;

        cameraMover.SetMenuMode();

        cameraMover.MoveTo(1);

        yield return new WaitForSeconds(cameraMover.moveDuration);
    }

    public PlaneData GetCurrentPlane()
    {
        int realIndex = currentCycle * cycleSize + currentIndex;

        if (realIndex < 0 || realIndex >= planes.Length)
        {
            return null;
        }

        return planes[realIndex];
    }
}
