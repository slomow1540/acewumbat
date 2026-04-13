using System.Collections;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    [System.Serializable]
    public class HologramData
    {
        public Mesh mesh;

        public Color hologramColor = Color.white;
        public Color textureTintColor = Color.white;

        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale = Vector3.one;
    }

    [Header("UI")]
    public SettingButton[] tabs;
    public SettingPanel[] panels;

    [Header("Hologram")]
    public HologramData[] hologramData;
    public MeshFilter hologramMesh;
    public MeshRenderer hologramRenderer;

    [Header("Lighting")]
    public Light[] hologramLights;

    [Range(0f, 1f)]
    public float lightMultiplier = 0.35f;

    [Header("Animation")]
    public float fadeSpeed = 5f;

    [Header("Audio")]
    public AudioClip clickSound;
    public AudioClip enterSound;
    private AudioManager audioManager;

    private Material mat;
    private int currentTab = -1;

    [Header("Hologram")]
    public Hologram settingsHologram;

    [Header("Overlay")]
    public Overlay overlay;
    public float overlayFadeSpeed = 5f;

    void Start()
    {
        audioManager = AudioManager.Instance;

        if (hologramRenderer != null)
            mat = hologramRenderer.material;

        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].Setup(this, i);
        }

        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].HideInstant();
        }

        Hide(false);
    }

    public void Show()
    {
        audioManager.Play(enterSound);
        SelectTab(0);
        settingsHologram.Show();
        overlay.FadeTo(0.7f);
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].Show(i * 0.05f);
        }
    }

    public void Hide(bool play = true)
    {
        if (play)
        {
            audioManager.Play(enterSound);
            settingsHologram.Hide();
        }
        else
        {
            settingsHologram.HideInstant();
        }
        SelectTab(-1);
        overlay.FadeTo(0f);
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].Hide(i * 0.05f);
            panels[i].Hide();
        }
    }

    public void SelectTab(int index)
    {
        if (currentTab == index)
            return;

        currentTab = index;

        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].SetActive(i == index);
            if (i == index)
            {
                panels[i].Show();
            }
            else
            {
                panels[i].Hide();
            }
        }

        StopAllCoroutines();
        StartCoroutine(AnimateHologram(index));
    }

    void ApplyHologramInstant(HologramData data)
    {
        if (data == null)
            return;

        hologramMesh.mesh = data.mesh;

        Transform t = hologramMesh.transform;
        t.localPosition = data.position;
        t.localRotation = Quaternion.Euler(data.rotation);
        t.localScale = data.scale;

        if (mat != null)
        {
            mat.SetColor("_Hologram_Color", data.hologramColor);
            mat.SetColor("_Texture_Tint_Color", data.textureTintColor);
        }

        UpdateLights(data.textureTintColor);
    }

    IEnumerator AnimateHologram(int index)
    {
        if (index < 0 || index >= hologramData.Length)
            yield break;

        var data = hologramData[index];

        Transform t = hologramMesh.transform;

        Vector3 startScale = t.localScale;
        Vector3 targetScale = data.scale;

        Color startHolo = mat != null ? mat.GetColor("_Hologram_Color") : Color.white;
        Color startTint = mat != null ? mat.GetColor("_Texture_Tint_Color") : Color.white;

        float time = 0f;

        t.localScale *= 0.8f;

        yield return new WaitForSeconds(0.05f);

        hologramMesh.mesh = data.mesh;

        t.localPosition = data.position;
        t.localRotation = Quaternion.Euler(data.rotation);

        while (time < 1f)
        {
            time += Time.deltaTime * fadeSpeed;

            float tVal = Mathf.SmoothStep(0f, 1f, time);

            t.localScale = Vector3.Lerp(startScale, targetScale, tVal);

            if (mat != null)
            {
                mat.SetColor("_Hologram_Color", Color.Lerp(startHolo, data.hologramColor, tVal));
                mat.SetColor(
                    "_Texture_Tint_Color",
                    Color.Lerp(startTint, data.textureTintColor, tVal)
                );
            }

            UpdateLights(Color.Lerp(startHolo, data.textureTintColor, tVal));

            yield return null;
        }

        ApplyHologramInstant(data);
    }

    void UpdateLights(Color baseColor)
    {
        if (hologramLights == null)
            return;

        for (int i = 0; i < hologramLights.Length; i++)
        {
            if (hologramLights[i] == null)
                continue;

            Color darker = baseColor * lightMultiplier;
            darker.a = 1f;

            hologramLights[i].color = darker;
        }
    }
}
