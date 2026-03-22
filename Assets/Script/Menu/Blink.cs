using UnityEngine;

public class Blink : MonoBehaviour
{
    void Update()
    {
        float alpha = Mathf.Abs(Mathf.Sin(Time.time * 2));
        var color = GetComponent<TMPro.TextMeshProUGUI>().color;
        color.a = alpha;
        GetComponent<TMPro.TextMeshProUGUI>().color = color;
    }
}
