using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabButton : GeneralUI
{
    private Button button;
    private int index;
    private TabManager manager;
    private AudioManager audioManager;

    public void Setup(TabManager m, int i)
    {
        manager = m;
        index = i;
    }

    public void OnClick()
    {
        audioManager = AudioManager.Instance;
        audioManager.Play(manager.clickSound);
        manager.SelectTab(index);
    }

    public void SetActive(bool active)
    {
        button = GetComponent<Button>();

        ColorBlock cb = button.colors;

        cb.normalColor = active ? new Color(1f, 1f, 1f, 0.26f) : new Color(1f, 1f, 1f, 0f);

        cb.selectedColor = cb.normalColor;

        button.colors = cb;
    }
}
