using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject pressAnyKeyText;
    public GameObject menuPanel;

    public MenuItemAnim[] items;
    public Pointer pointer;
    public BackButton backButton;

    private AudioManager audioManager;

    [Header("Audio")]
    public AudioClip switchSound;
    public AudioClip confirmSound;
    public AudioClip startSound;

    public void Init()
    {
        audioManager = AudioManager.Instance;

        pressAnyKeyText.SetActive(true);
        menuPanel.SetActive(false);
    }

    public void StartMenu()
    {
        pressAnyKeyText.SetActive(false);
        audioManager.Play(startSound);

        Invoke(nameof(ShowMenu), 0.5f);
    }

    void ShowMenu()
    {
        menuPanel.SetActive(true);

        for (int i = 0; i < items.Length; i++)
            items[i].Show(i * 0.1f);

        pointer.Show(0.2f);
        pointer.Follow(GetRect(0));
    }

    public int MoveUp(int index)
    {
        index = (index - 1 + items.Length) % items.Length;

        audioManager.Play(switchSound);
        audioManager.Play(items[index].selectSound);

        pointer.Follow(GetRect(index));

        return index;
    }

    public int MoveDown(int index)
    {
        index = (index + 1) % items.Length;

        audioManager.Play(switchSound);
        audioManager.Play(items[index].selectSound);

        pointer.Follow(GetRect(index));

        return index;
    }

    public void Confirm(int index)
    {
        audioManager.Play(confirmSound);
        audioManager.Play(items[index].confirmSound);

        items[index].Confirm();

        for (int i = 0; i < items.Length; i++)
            items[i].Hide(i * 0.05f);

        pointer.Hide(0.05f);
        backButton.Show();
    }

    public void Reset(int index)
    {
        backButton.Hide();

        for (int i = 0; i < items.Length; i++)
            items[i].Show(i * 0.05f);

        pointer.Show(0.1f);
        pointer.Follow(GetRect(index));
    }

    public void SetIndex(int index)
    {
        pointer.Follow(GetRect(index));
    }

    RectTransform GetRect(int i)
    {
        return items[i].GetComponent<RectTransform>();
    }
}
