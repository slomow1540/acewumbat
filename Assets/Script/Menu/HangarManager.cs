using UnityEngine;

public class HangarManager : MonoBehaviour
{
    public SlotManager slotManager;

    public GameObject panel;

    public GeneralUI header;

    public void Show()
    {
        slotManager.EnterHangar();
    }

    public void Hide()
    {
        slotManager.ExitHangar();
    }
}