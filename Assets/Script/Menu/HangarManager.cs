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
    bool canShowActionUI;

    void Start()
    {
        slotManager.onPlaneChanged += UpdatePlaneUI;
    }

    void OnDestroy()
    {
        slotManager.onPlaneChanged -= UpdatePlaneUI;
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

        ShowUIObjects(true);

        HideActionUI();

        // update CR saat buka hangar
        currentCR.SetValue(ProgressManager.GetCurrency());

        yield return new WaitForSeconds(GameManager.Instance.cameraMover.moveDuration);

        yield return slotManager.EnterSlots();

        for (int i = 0; i < arrowButtons.Length; i++)
            arrowButtons[i].Show();

        for (int i = 0; i < overlay.Length; i++)
            overlay[i].Show();

        canShowActionUI = true;

        RefreshButtonState();
    }

    IEnumerator ExitRoutine()
    {
        for (int i = 0; i < arrowButtons.Length; i++)
            arrowButtons[i].Hide();

        for (int i = 0; i < overlay.Length; i++)
            overlay[i].Hide();

        yield return slotManager.ExitRoutine();

        // Nonaktifkan UI setelah exit
        ShowUIObjects(false);

        ResetUI();
    }

    void ShowUIObjects(bool active)
    {
        purchaseButton.gameObject.SetActive(active);
        equipButton.gameObject.SetActive(active);
        equippedText.gameObject.SetActive(active);

        thrustBar.gameObject.SetActive(active);
        maneuverBar.gameObject.SetActive(active);
        healthBar.gameObject.SetActive(active);
        fireRateBar.gameObject.SetActive(active);
        missileTurnBar.gameObject.SetActive(active);

        gunDamageBar.gameObject.SetActive(active);
        aimAssistBar.gameObject.SetActive(active);
        missileDamageBar.gameObject.SetActive(active);
        missileRangeBar.gameObject.SetActive(active);

        gunAmmo.gameObject.SetActive(active);
        missileAmmo.gameObject.SetActive(active);
        missileLockTime.gameObject.SetActive(active);
        price.gameObject.SetActive(active);
    }

    void UpdatePlaneUI(PlaneData plane)
    {
        if (plane == null)
            return;

        currentPlane = plane;

        // Info
        planeName.text = plane.planeName;

        type.text = plane.GetType();

        // LEFT
        thrustBar.SetValue(plane.thrust);

        maneuverBar.SetValue(plane.maneuverability);

        healthBar.SetValue(plane.health);

        fireRateBar.SetValue(plane.gunFireRate);

        missileTurnBar.SetValue(plane.missileManeuverability);

        // RIGHT
        gunDamageBar.SetValue(plane.gunDamage);

        aimAssistBar.SetValue(plane.aimAssistRange);

        missileDamageBar.SetValue(plane.missileDamage);

        missileRangeBar.SetValue(plane.missileRange);

        // COUNT
        gunAmmo.SetValue(plane.gunAmmoCount);

        missileAmmo.SetValue(plane.missileAmmoCount);

        missileLockTime.SetValue(Mathf.RoundToInt(plane.missileLockTime * 10f));

        price.SetValue(plane.price);

        RefreshButtonState();
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

    void BuyPlane()
    {
        bool success = ProgressManager.BuyPlane(currentPlane);

        if (!success)
            return;

        currentCR.SetValue(ProgressManager.GetCurrency());

        EquipPlane();
    }

    public void OnPurchase()
    {
        if (currentPlane == null)
            return;

        bool success = ProgressManager.BuyPlane(currentPlane);

        if (!success)
            return;

        currentCR.SetValue(ProgressManager.GetCurrency());

        // auto equip
        ProgressManager.EquipPlane(currentPlane.planeName);

        RefreshButtonState();
    }

    public void OnEquip()
    {
        if (currentPlane == null)
            return;

        ProgressManager.EquipPlane(currentPlane.planeName);

        RefreshButtonState();
    }

    void EquipPlane()
    {
        ProgressManager.EquipPlane(currentPlane.planeName);

        RefreshButtonState();
    }

    void RefreshButtonState()
    {
        if (!canShowActionUI)
        {
            HideActionUI();
            return;
        }

        bool owned = ProgressManager.IsOwned(currentPlane.planeName);

        bool equipped = ProgressManager.IsEquipped(currentPlane.planeName);

        HideActionUI();

        // belum punya
        if (!owned)
        {
            purchaseButton.Show();

            UnityEngine.UI.Button button = purchaseButton.GetComponent<UnityEngine.UI.Button>();

            button.interactable = ProgressManager.HasCurrency(currentPlane.price);

            return;
        }

        // punya tapi belum equip
        if (!equipped)
        {
            equipButton.Show();
            return;
        }

        // sedang dipakai
        equippedText.Show();
    }

    void HideActionUI()
    {
        purchaseButton?.Hide();
        equipButton?.Hide();
        equippedText?.Hide();
    }
}
