using UnityEngine;
using UnityEngine.Rendering;

public class SkyboxController : MonoBehaviour
{
    [System.Serializable]
    public class SkyboxPreset
    {
        [Header("Sky")]
        public Material skyboxMaterial;

        [Header("Ambient")]
        public Color ambientColor = Color.gray;
        public float ambientIntensity = 1f;

        [Header("Reflection")]
        public float reflectionIntensity = 1f;
    }

    public SkyboxPreset[] presets;

    public void ApplyPreset(int index)
    {
        if (index < 0 || index >= presets.Length)
            return;

        var preset = presets[index];

        RenderSettings.skybox = preset.skyboxMaterial;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = preset.ambientColor;
        RenderSettings.ambientIntensity = preset.ambientIntensity;

        RenderSettings.reflectionIntensity = preset.reflectionIntensity;

        DynamicGI.UpdateEnvironment();
    }
}
