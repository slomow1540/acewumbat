using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject pressAnyKeyText;
    public GameObject menuPanel;

    public MenuItemAnim[] mainItems;
    public Pointer mainPointer;

    [Header("Quit")]
    public GameObject quitPanel;
    public GeneralUI label;

    public MenuItemAnim[] quitItems;
    public Pointer quitPointer;

    int quitIndex;
    bool quitMode;

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
        quitPanel.SetActive(false);
        mainPointer.isBlinking = true;
        quitPointer.isBlinking = true;
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

        for (int i = 0; i < mainItems.Length; i++)
            mainItems[i].Show(i * 0.1f);

        mainPointer.Show(0.2f);
        mainPointer.Follow(GetRect(0));
    }

    public void ShowQuit()
    {
        quitMode = true;

        quitPanel.SetActive(true);

        label.Show();
        for (int i = 0; i < quitItems.Length; i++)
        {
            quitItems[i].Show(i * 0.05f);
        }

        quitIndex = 0;

        quitPointer.Show(0f);
        quitPointer.Follow(GetRect(0, true));
    }

    public void HideQuit()
    {
        quitMode = false;

        label.Hide();
        for (int i = 0; i < quitItems.Length; i++)
        {
            quitItems[i].Hide(i * 0.05f);
        }

        quitPointer.Hide(0f);
    }

    public void MoveUp(int index)
    {
        audioManager.Play(switchSound, AudioManager.AudioChannel.UI);
        audioManager.Play(mainItems[index].selectSound, AudioManager.AudioChannel.Narrator);
        mainPointer.Follow(GetRect(index));
    }

    public void MoveDown(int index)
    {
        audioManager.Play(switchSound, AudioManager.AudioChannel.UI);
        audioManager.Play(mainItems[index].selectSound, AudioManager.AudioChannel.Narrator);
        mainPointer.Follow(GetRect(index));
    }

    public void HideMenu()
    {
        audioManager.Play(confirmSound, AudioManager.AudioChannel.UI);

        for (int i = 0; i < mainItems.Length; i++)
            mainItems[i].Hide(i * 0.05f);

        mainPointer.Hide(0.05f);
    }

    public void HideMenuWithSound(int index)
    {
        audioManager.Play(confirmSound, AudioManager.AudioChannel.UI);
        audioManager.Play(mainItems[index].confirmSound, AudioManager.AudioChannel.Narrator);

        mainItems[index].Confirm();

        for (int i = 0; i < mainItems.Length; i++)
            mainItems[i].Hide(i * 0.05f);

        mainPointer.Hide(0.05f);
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
        for (int i = 0; i < mainItems.Length; i++)
            mainItems[i].Show(i * 0.05f);

        mainPointer.Show(0.1f);
        mainPointer.Follow(GetRect(index));
    }

    public void SetIndex(int index)
    {
        mainPointer.Follow(GetRect(index));
    }

    public void SetQuitIndex(int index)
    {
        audioManager.Play(switchSound, AudioManager.AudioChannel.UI);

        audioManager.Play(quitItems[index].selectSound, AudioManager.AudioChannel.Narrator);

        quitPointer.Follow(GetRect(index, true));
    }

    public void ConfirmQuit(int index)
    {
        audioManager.Play(confirmSound, AudioManager.AudioChannel.UI);
        audioManager.Play(quitItems[index].confirmSound, AudioManager.AudioChannel.Narrator);
        quitItems[index].Confirm();
    }

    RectTransform GetRect(int i, bool isQuit = false)
    {
        return (isQuit ? quitItems : mainItems)[i].GetComponent<RectTransform>();
    }

    public void StartMenuImmediate()
    {
        pressAnyKeyText.SetActive(false);
        ShowMenu();
    }
}
