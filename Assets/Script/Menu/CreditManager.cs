using TMPro;
using UnityEngine;

public class CreditManager : MonoBehaviour
{
    [System.Serializable]
    public class CreditData
    {
        public string label;
        public string name;
        public string status;
        public string quote;
    }

    [Header("Credit Data")]
    public CreditData[] credits;

    public SlideIn[] arrowButtons;
    public SlideIn[] overlay;

    [Header("UI Text")]
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI quoteText;

    public CameraMover cameraMover;

    private int creditPos = 4;
    private bool isActive = false;

    void Update()
    {
        if (!isActive)
            return;

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            GoRight();
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            GoLeft();
        }
    }

    public void ResetPos()
    {
        creditPos = 4;
    }

    public void GoRight()
    {
        creditPos++;
        if (creditPos > 7)
            creditPos = 4;

        cameraMover.MoveTo(creditPos);
        UpdateText();
    }

    public void GoLeft()
    {
        creditPos--;
        if (creditPos < 4)
            creditPos = 7;

        cameraMover.MoveTo(creditPos);
        UpdateText();
    }

    public void UpdateText()
    {
        int index = creditPos - 4;

        if (index < 0 || index >= credits.Length)
            return;

        var data = credits[index];

        labelText.text = "<" + data.label + ">";
        nameText.text = data.name;
        statusText.text = data.status;
        quoteText.text = '"' + data.quote + '"';
    }

    public void Show()
    {
        isActive = true;
        for (int i = 0; i < arrowButtons.Length; i++)
        {
            arrowButtons[i].Show();
        }
        for (int i = 0; i < overlay.Length; i++)
        {
            overlay[i].Show();
        }
    }

    public void Hide()
    {
        isActive = false;
        for (int i = 0; i < arrowButtons.Length; i++)
        {
            arrowButtons[i].Hide();
        }
        for (int i = 0; i < overlay.Length; i++)
        {
            overlay[i].Hide();
        }
    }
}
