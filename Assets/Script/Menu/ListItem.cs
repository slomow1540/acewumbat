using UnityEngine;

public class ListItem : GeneralUI
{
    protected bool isHovered = false;
    protected bool isConfirmed = false;

    protected override void Update()
    {
        base.Update();
        HandleVisual();
    }

    protected virtual void HandleVisual()
    {
        float targetScale = 1f;

        if (isConfirmed)
            targetScale = 1.18f;
        else if (isHovered)
            targetScale = 1.08f;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            Vector3.one * targetScale,
            Time.deltaTime * 10f
        );
    }

    public virtual void SetHovered(bool value)
    {
        isHovered = value;
    }

    public virtual void Confirm()
    {
        isConfirmed = true;
        Invoke(nameof(ResetConfirm), 0.2f);
    }

    void ResetConfirm()
    {
        isConfirmed = false;
    }
}
