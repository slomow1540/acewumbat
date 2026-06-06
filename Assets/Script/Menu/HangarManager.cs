using System.Collections;
using UnityEngine;

public class HangarManager : MonoBehaviour
{
    public SlotManager slotManager;

    public SlideIn[] arrowButtons;
    public SlideIn[] overlay;

    public float enterDuration = 3.5f;
    public float exitDuration = 2.2f;

    [Header("Bars")]
    public StatusBar thrustBar;
    public StatusBar maneuverBar;
    public StatusBar healthBar;

    public StatusBar gunDamageBar;
    public StatusBar fireRateBar;

    public StatusBar missileDamageBar;
    public StatusBar missileRangeBar;

    [Header("Numbers")]
    public CountUp gunAmmo;
    public CountUp missileAmmo;
    public CountUp price;

    public void Show()
    {
        StartCoroutine(EnterRoutine());
    }

    public void Hide()
    {
        StartCoroutine(ExitRoutine());
    }

    public void Previous()
    {
        slotManager.Previous();
    }

    public void Next()
    {
        slotManager.Next();
    }

    IEnumerator EnterRoutine()
    {
        yield return new WaitForSeconds(GameManager.Instance.cameraMover.moveDuration);

        yield return slotManager.EnterSlots();

        for (int i = 0; i < arrowButtons.Length; i++)
        {
            arrowButtons[i].Show();
        }

        for (int i = 0; i < overlay.Length; i++)
        {
            overlay[i].Show();
        }
    }

    IEnumerator ExitRoutine()
    {
        for (int i = 0; i < arrowButtons.Length; i++)
        {
            arrowButtons[i].Hide();
        }

        for (int i = 0; i < overlay.Length; i++)
        {
            overlay[i].Show();
        }

        yield return slotManager.ExitRoutine();
    }
}
