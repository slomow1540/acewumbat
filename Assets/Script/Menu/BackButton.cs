using UnityEngine;
using UnityEngine.EventSystems;

public class BackButton : MonoBehaviour, IPointerClickHandler
{
    public GeneralUI label;
    public GeneralUI arrow;

    public UIManager manager;

    public void Show()
    {
        gameObject.SetActive(true);
        label.Show(0.1f, -100f);
        arrow.Show(0.1f, -100f);
    }

    public void Hide()
    {
        gameObject.SetActive(true);
        label.Hide(0.1f);
        arrow.Hide(0.1f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        manager.ResetMenu();
    }
}
