using TMPro;
using UnityEngine;

public class planecontroller : MonoBehaviour
{
    [Header("Plane Stats")]
    [Tooltip("how much turst acceleraiton")]
    public float turstacceleration = 0.1f;

    [Tooltip("max thrust")]
    public float maxthrust = 200f;

    [Tooltip("how responsive the plane when manuver(roll,pitch,yaw)")]
    public float responsiveness = 10f;

    private float thrust;
    private float roll;
    private float pitch;
    private float yaw;

    private float responsiveModifier
    {
        get { return (rb.mass / 10f) * responsiveness; }
    }

    Rigidbody rb;

    public TextMeshProUGUI thrustText;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void HandleInputs()
    {
        roll = Input.GetAxis("Roll");
        pitch = Input.GetAxis("Pitch");
        yaw = Input.GetAxis("Yaw");

        if (Input.GetKey(KeyCode.Space))
            thrust += turstacceleration;
        else if (Input.GetKey(KeyCode.LeftControl))
            thrust -= turstacceleration;
        thrust = Mathf.Clamp(thrust, 0f, 100f);
    }

    private void Update()
    {
        HandleInputs();
        thrustText.text = "Thrust: " + thrust + "%";
    }

    private void FixedUpdate()
    {
        rb.AddForce(transform.forward * maxthrust * thrust);
        rb.AddTorque(transform.up * yaw * responsiveModifier);
        rb.AddTorque(transform.right * pitch * responsiveModifier);
        rb.AddTorque(transform.forward * roll * responsiveModifier);
    }
}
