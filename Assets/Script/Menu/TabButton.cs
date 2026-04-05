using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabButton : GeneralUI
{
    private Button button;

    private int index;
    private TabManager manager;

    void Start()
    {
        button = GetComponent<Button>();
    }

    public void Setup(TabManager m, int i)
    {
        manager = m;
        index = i;
    }

    public void OnClick()
    {
        manager.SelectTab(index);
    }

    public void SetActive(bool active)
    {
        ColorBlock cb = button.colors;

        Color c = cb.normalColor;

        c.a = active ? 0.4f : 0f;

        cb.normalColor = c;
        cb.pressedColor = c;

        button.colors = cb;
    }
}
