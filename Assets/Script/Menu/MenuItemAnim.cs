using UnityEngine;
using UnityEngine.EventSystems;

public class MenuItemAnim
    : ListItem,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
{
    public int index;
    public UIManager manager;

    public AudioClip selectSound;
    public AudioClip confirmSound;

    private AudioManager audioManager;

    void Start()
    {
        audioManager = AudioManager.Instance;
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
}
