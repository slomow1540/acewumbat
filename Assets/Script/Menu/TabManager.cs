using UnityEngine;

public class TabManager : MonoBehaviour
{
    public GameObject[] panels;
    public TabButton[] tabs;

    int currentTab = -1;

    void Start()
    {
        SelectTab(0);
        Hide();
    }

    public void Show()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].Show(i * 0.05f);
        }
    }

    public void Hide()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].Hide(i * 0.05f);
        }
    }

    public void SelectTab(int index)
    {
        if (currentTab == index)
            return;

        currentTab = index;

        for (int i = 0; i < tabs.Length; i++)
        {
            // panels[i].SetActive(i == index);
            tabs[i].SetActive(i == index);
        }
    }
}
