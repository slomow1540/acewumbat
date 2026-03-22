using UnityEngine;

public class LightingController : MonoBehaviour
{
    public enum TimeOfDay
    {
        Morning,
        Sunset,
        Night,
    }

    public Light[] outsideLights;

    public void ApplyLighting(TimeOfDay time)
    {
        foreach (var l in outsideLights)
        {
            switch (time)
            {
                case TimeOfDay.Morning:
                    l.enabled = false;
                    l.intensity = 0f;
                    break;

                case TimeOfDay.Sunset:
                    l.enabled = true;
                    l.intensity = 0.4f;
                    break;

                case TimeOfDay.Night:
                    l.enabled = true;
                    l.intensity = 1f;
                    break;
            }
        }
    }
}
