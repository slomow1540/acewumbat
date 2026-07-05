using UnityEngine;

public class ManualManager : MonoBehaviour
{
    [Header("UI")]
    public Carousel carousel;
    public SlideIn[] arrowButtons;
    public PopOut book;

    [Header("Overlay")]
    public Overlay overlay;

    [Header("Audio")]
    public AudioClip slideSound;
    private AudioManager audioManager;

    bool isOpen;

    void Start()
    {
        audioManager = AudioManager.Instance;
        carousel.Hide();
        Hide();
        book.HideInstant();
    }

    void Update()
    {
        if (!isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextManual();
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousManual();
        }
    }

    public void Show()
    {
        isOpen = true;

        book.Show();

        overlay.FadeTo(0.7f);

        for (int i = 0; i < arrowButtons.Length; i++)
        {
            arrowButtons[i].Show();
        }

        carousel.Show();
        carousel.GoTo(0);
    }

    public void Hide()
    {
        isOpen = false;

        overlay.FadeTo(0f);

        for (int i = 0; i < arrowButtons.Length; i++)
        {
            arrowButtons[i].Hide();
        }

        carousel.Hide();

        book.Hide();
    }

    public void NextManual()
    {
        audioManager.Play(slideSound, AudioChannel.UI);
        carousel.Next();
    }

    public void PreviousManual()
    {
        audioManager.Play(slideSound, AudioChannel.UI);
        carousel.Previous();
    }
}
