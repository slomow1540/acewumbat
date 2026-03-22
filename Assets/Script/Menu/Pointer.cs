using UnityEngine;

public class Pointer : GeneralUI
{
    public void Follow(RectTransform target)
    {
        basePos.y = target.anchoredPosition.y;
        Move(-50f);
    }
}
