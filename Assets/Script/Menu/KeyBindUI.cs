using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeybindUI : MonoBehaviour
{
    public InputActionReference action;
    public TextMeshProUGUI keyText;

    private RectTransform rectTransform;
    private Button button;

    private float minWidth = 100f;
    private float padding = 20f;

    private ColorBlock normalColors;
    public Color rebindingColor = new Color(1f, 1f, 1f, 0.7f);

    private InputActionRebindingExtensions.RebindingOperation rebinding;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        button = GetComponent<Button>();

        normalColors = button.colors;

        UpdateUI();
    }

    public void StartRebind()
    {
        var colors = button.colors;
        colors.normalColor = rebindingColor;
        colors.selectedColor = rebindingColor;
        button.colors = colors;

        action.action.Disable();

        rebinding = action
            .action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnComplete(operation =>
            {
                action.action.Enable();
                operation.Dispose();

                button.colors = normalColors;

                UpdateUI();
            });

        rebinding.Start();
    }

    void UpdateUI()
    {
        string bindingText = action.action.GetBindingDisplayString();

        keyText.text = bindingText;
    }
}
