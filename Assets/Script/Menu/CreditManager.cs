using UnityEngine;

public class CreditManager : MonoBehaviour
{
    public CreditButton[] creditButtons;

    public void Show()
    {
        for (int i = 0; i < creditButtons.Length; i++)
        {
            creditButtons[i].Show();
        }
    }

    public void Hide()
    {
        for (int i = 0; i < creditButtons.Length; i++)
        {
            creditButtons[i].Hide();
        }
    }
}
