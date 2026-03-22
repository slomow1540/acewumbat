using UnityEngine;
using UnityEngine.EventSystems;

public class BackButton : MonoBehaviour, IPointerClickHandler
{
    public GeneralUI label;
    public GeneralUI arrow;

    public MenuController controller;

    public void Show()
    {
        label.Show(0.1f, -100f);
        arrow.Show(0.1f, -100f);
    }

    public void Hide()
    {
        label.Hide(0.1f);
        arrow.Hide(0.1f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        controller.ResetMenu();
    }
}
