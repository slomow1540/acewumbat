using UnityEngine;

public class TabManager : MonoBehaviour
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

    public GameObject[] panels;
    public TabButton[] tabs;
    public HologramData[] hologramData;

    public MeshFilter hologramMesh;
    public MeshRenderer hologramRenderer;
    public float fadeSpeed = 5f;
    private Material mat;

    public AudioClip clickSound;

    int currentTab = -1;

    void Start()
    {
        mat = hologramRenderer.material;
        SelectTab(0);
        Hide();
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].Setup(this, i);
        }
    }

    public void Show()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].Show(i * 0.05f);
        }
    }

    public void Hide()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].Hide(i * 0.05f);
        }
    }

    public void SelectTab(int index)
    {
        if (currentTab == index)
            return;

        currentTab = index;

        for (int i = 0; i < tabs.Length; i++)
        {
            // panels[i].SetActive(i == index);
            tabs[i].SetActive(i == index);
        }

        ApplyHologram(index);
    }

    void ApplyHologram(int index)
    {
        if (index < 0 || index >= hologramData.Length)
            return;

        var data = hologramData[index];

        hologramMesh.mesh = data.mesh;

        Transform t = hologramMesh.transform;
        t.localPosition = data.position;
        t.localRotation = Quaternion.Euler(data.rotation);
        t.localScale = data.scale;

        if (mat != null)
        {
            mat.SetColor("_HologramColor", data.hologramColor);
            mat.SetColor("_TextureTintColor", data.textureTintColor);
        }
    }
}
