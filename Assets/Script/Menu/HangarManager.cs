using System.Collections;
using UnityEngine;

public class HangarManager : MonoBehaviour
{
    public SlotManager slotManager;


    public float enterDuration = 3.5f;
    public float exitDuration = 2.2f;

    public void Show()
    {
        StartCoroutine(EnterRoutine());
    }

    public void Hide()
    {
        StartCoroutine(ExitRoutine());
    }

    IEnumerator EnterRoutine()
    {
        yield return new WaitForSeconds(
            GameManager.Instance.cameraMover.moveDuration
        );

        yield return slotManager.EnterSlots();
    }

    IEnumerator ExitRoutine()
    {
        yield return slotManager.ExitRoutine();

        // Setelah pesawat selesai hide & camera sudah di initial hangar,
        // GameManager akan gerak ke Idle — ditunggu UIManager
    }
}