using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelItem : ListItem, IPointerEnterHandler, IPointerClickHandler
{
    private TextMeshProUGUI label;
    public int index;
    public MissionManager manager;

    [Header("Optional Visual")]
    public float selectedAlpha = 1f;
    public float normalAlpha = 0.5f;

    void Awake()
    {
        base.Awake();
        label = GetComponent<TextMeshProUGUI>();
    }

    protected override void HandleVisual()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            Vector3.one,
            Time.deltaTime * 10f
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHovered(true);
        manager.SetIndexFromMouse(index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHovered(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        manager.SelectFromMouse(index);
    }

    public void SetText(string text)
    {
        if (label != null)
            label.text = text;
    }

    public void SetSelected(bool value)
    {
        isConfirmed = value;
    }
}
