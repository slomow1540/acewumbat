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
        pointer.isBlinking = true;
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

        audioManager.Play(switchSound, AudioManager.AudioChannel.UI);
        audioManager.Play(items[index].selectSound, AudioManager.AudioChannel.Narrator);

        pointer.Follow(GetRect(index));

        return index;
    }

    public int MoveDown(int index)
    {
        index = (index + 1) % items.Length;

        audioManager.Play(switchSound, AudioManager.AudioChannel.UI);
        audioManager.Play(items[index].selectSound, AudioManager.AudioChannel.Narrator);

        pointer.Follow(GetRect(index));

        return index;
    }

    public void HideMenu()
    {
        audioManager.Play(confirmSound, AudioManager.AudioChannel.UI);

        for (int i = 0; i < items.Length; i++)
            items[i].Hide(i * 0.05f);

        pointer.Hide(0.05f);
    }

    public void HideMenuWithSound(int index)
    {
        audioManager.Play(confirmSound, AudioManager.AudioChannel.UI);
        audioManager.Play(items[index].confirmSound, AudioManager.AudioChannel.Narrator);

        items[index].Confirm();

        for (int i = 0; i < items.Length; i++)
            items[i].Hide(i * 0.05f);

        pointer.Hide(0.05f);
    }

    public void ShowBack()
    {
        backButton.Show();
    }

    public void HideBack()
    {
        backButton.Hide();
        audioManager.Play(confirmSound, AudioManager.AudioChannel.UI);
    }

    public void Reset(int index)
    {
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
