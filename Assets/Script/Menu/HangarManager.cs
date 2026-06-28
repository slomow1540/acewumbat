using System.Collections;
using TMPro;
using UnityEngine;
using Util;

public class HangarManager : MonoBehaviour
{
    public SlotManager slotManager;

    [Header("Navigation")]
    public SlideIn[] arrowButtons;
    public SlideIn[] overlay;

    public float enterDuration = 3.5f;
    public float exitDuration = 2.2f;

    [Header("Plane Info")]
    public TMP_Text planeName;
    public TMP_Text type;

    [Header("Left Stats")]
    public StatusBar thrustBar;
    public StatusBar maneuverBar;
    public StatusBar healthBar;
    public StatusBar fireRateBar;
    public StatusBar missileTurnBar;

    [Header("Right Stats")]
    public StatusBar gunDamageBar;
    public StatusBar aimAssistBar;
    public StatusBar missileDamageBar;
    public StatusBar missileRangeBar;

    [Header("Numbers")]
    public Counting gunAmmo;
    public Counting missileAmmo;
    public Counting missileLockTime;
    public Counting price;

    [Header("Action UI")]
    public SlideIn purchaseButton;
    public SlideIn equipButton;
    public SlideIn equippedText;

    public Counting currentCR;

    PlaneData currentPlane;
    int currentPlaneIndex;
    bool canShowActionUI;
    Coroutine updateRoutine;

    void Start()
    {
        slotManager.onPlaneChanged += OnPlaneChanged;
        slotManager.onPlaneReady += UpdatePlaneUI;
    }

    void OnDestroy()
    {
        slotManager.onPlaneChanged -= OnPlaneChanged;
        slotManager.onPlaneReady -= UpdatePlaneUI;
    }

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
        canShowActionUI = false;

        HideActionUI();

        yield return new WaitForSeconds(GameManager.Instance.cameraMover.moveDuration);

        yield return slotManager.EnterSlots();

        for (int i = 0; i < arrowButtons.Length; i++)
            arrowButtons[i].Show();

        for (int i = 0; i < overlay.Length; i++)
            overlay[i].Show();

        // Semua UI sudah muncul, baru isi data
        currentCR.SetValue(ProgressManager.GetCurrency());

        canShowActionUI = true;

        ApplyPlaneUI();
    }


    IEnumerator ExitRoutine()
    {
        canShowActionUI = false;

        for (int i = 0; i < arrowButtons.Length; i++)
            arrowButtons[i].Hide();

        for (int i = 0; i < overlay.Length; i++)
            overlay[i].Hide();

        yield return slotManager.ExitRoutine();

        ResetUI();
    }


    void OnPlaneChanged(PlaneData plane, int index)
    {
        // Simpan data saja, belum apply UI
        currentPlane = plane;
        currentPlaneIndex = index;
    }

    void UpdatePlaneUI(
        PlaneData plane,
        int index
    )
    {
        if (plane == null)
            return;

        currentPlane = plane;
        currentPlaneIndex = index;

        if (!canShowActionUI)
            return;

        if (updateRoutine != null)
        {
            StopCoroutine(updateRoutine);
        }

        updateRoutine =
            StartCoroutine(
                DelayedApplyUI()
            );
    }

    IEnumerator DelayedApplyUI()
    {
        yield return new WaitForSeconds(
            slotManager.cameraMover.moveDuration
        );

        yield return new WaitForSeconds(
            0.2f
        );

        ApplyPlaneUI();
    }


    public void OnPurchase()
    {
        if (currentPlane == null)
            return;

        bool success = ProgressManager.BuyPlane(currentPlaneIndex, currentPlane.price);

        if (!success)
            return;

        currentCR.SetValue(ProgressManager.GetCurrency());
        ProgressManager.EquipPlane(currentPlaneIndex);
        RefreshButtonState();
    }

    public void OnEquip()
    {
        if (currentPlane == null)
            return;

        ProgressManager.EquipPlane(currentPlaneIndex);
        RefreshButtonState();
    }


    void RefreshButtonState()
    {
        if (!canShowActionUI)
        {
            HideActionUI();
            return;
        }

        bool owned = ProgressManager.IsOwned(currentPlaneIndex);
        bool equipped = ProgressManager.IsEquipped(currentPlaneIndex);

        HideActionUI();

        if (!owned)
        {
            purchaseButton.Show();
            var button = purchaseButton.GetComponent<UnityEngine.UI.Button>();
            button.interactable = ProgressManager.HasCurrency(currentPlane.price);
            return;
        }

        if (!equipped)
        {
            equipButton.Show();
            return;
        }

        equippedText.Show();
    }

    void ApplyPlaneUI()
    {
        if (currentPlane == null)
            return;

        // penting: aktifkan UI dulu
        RefreshButtonState();

        planeName.text =
            currentPlane.planeName;

        type.text =
            currentPlane.GetType();

        thrustBar.SetValue(
            currentPlane.thrust
        );

        maneuverBar.SetValue(
            currentPlane.maneuverability
        );

        healthBar.SetValue(
            currentPlane.health
        );

        fireRateBar.SetValue(
            currentPlane.gunFireRate
        );

        missileTurnBar.SetValue(
            currentPlane.missileManeuverability
        );

        gunDamageBar.SetValue(
            currentPlane.gunDamage
        );

        aimAssistBar.SetValue(
            currentPlane.aimAssistRange
        );

        missileDamageBar.SetValue(
            currentPlane.missileDamage
        );

        missileRangeBar.SetValue(
            currentPlane.missileRange
        );

        gunAmmo.SetValue(
            currentPlane.gunAmmoCount
        );

        missileAmmo.SetValue(
            currentPlane.missileAmmoCount
        );

        missileLockTime.SetValue(
            Mathf.RoundToInt(
                currentPlane.missileLockTime * 10f
            )
        );

        price.SetValue(
            currentPlane.price
        );
    }

    void ResetUI()
    {
        planeName.text = "";
        type.text = "";

        thrustBar.ResetBar();
        maneuverBar.ResetBar();
        healthBar.ResetBar();
        fireRateBar.ResetBar();
        missileTurnBar.ResetBar();

        gunDamageBar.ResetBar();
        aimAssistBar.ResetBar();
        missileDamageBar.ResetBar();
        missileRangeBar.ResetBar();

        gunAmmo.ResetCount();
        missileAmmo.ResetCount();
        missileLockTime.ResetCount();
        price.ResetCount();

        currentCR.ResetCount();
    }

    void HideActionUI()
    {
        purchaseButton?.Hide();
        equipButton?.Hide();
        equippedText?.Hide();
    }
}
