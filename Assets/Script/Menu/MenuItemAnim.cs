using UnityEngine;
using UnityEngine.EventSystems;

public class MenuItemAnim : GeneralUI, IPointerEnterHandler, IPointerClickHandler
{
    public int index;
    public MenuController controller;

    public AudioClip selectSound;
    public AudioClip confirmSound;

    private bool isHovered = false;
    private bool isConfirmed = false;

    protected override void Update()
    {
        base.Update();
        HandleVisual();
    }

    void HandleVisual()
    {
        float targetScale = 1f;

        if (isConfirmed)
            targetScale = 1.18f;
        else if (isHovered)
            targetScale = 1.08f;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            Vector3.one * targetScale,
            Time.deltaTime * 10f
        );
    }

    public void Confirm()
    {
        isConfirmed = true;
        Invoke(nameof(ResetConfirm), 0.2f);
    }

    void ResetConfirm()
    {
        isConfirmed = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        controller.SetIndexFromMouse(index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        controller.SelectFromMouse(index);
    }
}
