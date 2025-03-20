using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [Header("Wheels")]
    public Transform frontWheel;
    public Transform rearWheel;
    public WheelJoint2D frontWheelJoint;
    public WheelJoint2D rearWheelJoint;

    [Header("Engine")]
    public float motorForce = 1500f;
    public float brakeForce = 1000f;
    public float airRotationTorque = 150f;
    public float groundRotationTorqueMultiplier = 0.2f;

    [Header("Suspension")]
    [Range(0.1f, 10f)] public float suspensionStiffness = 5f;
    [Range(0.1f, 5f)] public float suspensionDamping = 1f;

    // Input controls
    private float accelerationInput = 0f;
    private float brakeInput = 0f;

    // Components
    private Rigidbody2D vehicleRigidbody;
    private Rigidbody2D frontWheelRigidbody;
    private Rigidbody2D rearWheelRigidbody;
    private CircleCollider2D frontWheelCollider;
    private CircleCollider2D rearWheelCollider;

    // Wheel contact tracking
    private bool isFrontWheelGrounded = false;
    private bool isRearWheelGrounded = false;

    // Joint motor properties
    private JointMotor2D frontWheelMotor;
    private JointMotor2D rearWheelMotor;

    private void Awake()
    {
        // Get vehicle components
        vehicleRigidbody = GetComponent<Rigidbody2D>();

        // Get wheel components
        if (frontWheel != null)
        {
            frontWheelRigidbody = frontWheel.GetComponent<Rigidbody2D>();
            frontWheelCollider = frontWheel.GetComponent<CircleCollider2D>();
        }

        if (rearWheel != null)
        {
            rearWheelRigidbody = rearWheel.GetComponent<Rigidbody2D>();
            rearWheelCollider = rearWheel.GetComponent<CircleCollider2D>();
        }

        // Auto-find wheel joints if not assigned
        if (frontWheelJoint == null || rearWheelJoint == null)
        {
            AutoAssignWheelJoints();
        }

        // Setup suspension
        SetupSuspension();
    }

    private void AutoAssignWheelJoints()
    {
        // Get all wheel joints on the vehicle
        WheelJoint2D[] wheelJoints = GetComponents<WheelJoint2D>();

        if (wheelJoints.Length >= 2)
        {
            // Assume the joint connected to frontWheel is the frontWheelJoint
            foreach (WheelJoint2D joint in wheelJoints)
            {
                if (joint.connectedBody == frontWheelRigidbody)
                {
                    frontWheelJoint = joint;
                }
                else if (joint.connectedBody == rearWheelRigidbody)
                {
                    rearWheelJoint = joint;
                }
            }

            Debug.Log("Auto-assigned wheel joints: Front=" + (frontWheelJoint != null) + ", Rear=" + (rearWheelJoint != null));
        }
        else
        {
            Debug.LogWarning("Not enough wheel joints found on vehicle. Expected 2, found " + wheelJoints.Length);
        }
    }

    private void SetupSuspension()
    {
        if (frontWheelJoint != null)
        {
            var suspension = frontWheelJoint.suspension;
            suspension.frequency = suspensionStiffness;
            suspension.dampingRatio = suspensionDamping;
            frontWheelJoint.suspension = suspension;

            // Initialize front wheel motor
            frontWheelMotor = frontWheelJoint.motor;
        }

        if (rearWheelJoint != null)
        {
            var suspension = rearWheelJoint.suspension;
            suspension.frequency = suspensionStiffness;
            suspension.dampingRatio = suspensionDamping;
            rearWheelJoint.suspension = suspension;

            // Initialize rear wheel motor
            rearWheelMotor = rearWheelJoint.motor;
        }
    }

    private void Update()
    {
        // Get input (freccia destra muove a destra, freccia sinistra muove a sinistra)
        accelerationInput = Input.GetKey(KeyCode.RightArrow) ? 1f : 0f;
        brakeInput = Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f;

        // Check ground contact
        CheckWheelContact();
    }

    private void FixedUpdate()
    {
        // Apply motor forces to wheels
        ApplyDriveForce();

        // Apply rotation control when in air
        ApplyRotationControl();
    }

    private void CheckWheelContact()
    {
        // Verifica che i collider siano stati assegnati
        if (frontWheelCollider == null || rearWheelCollider == null)
            return;

        // We'll use a small raycast to check if wheels are touching ground
        float rayDistance = frontWheelCollider.radius + 0.1f;

        // Check front wheel
        RaycastHit2D hitFront = Physics2D.Raycast(
            frontWheel.position,
            Vector2.down,
            rayDistance,
            LayerMask.GetMask("Ground"));
        isFrontWheelGrounded = hitFront.collider != null;

        // Check rear wheel
        RaycastHit2D hitRear = Physics2D.Raycast(
            rearWheel.position,
            Vector2.down,
            rayDistance,
            LayerMask.GetMask("Ground"));
        isRearWheelGrounded = hitRear.collider != null;
    }

    private void ApplyDriveForce()
    {
        // Calculate motor speed and torque
        float motorSpeed = 0f;
        float motorTorque = 0f;

        if (accelerationInput > 0)
        {
            // Accelerazione positiva (verso destra)
            motorSpeed = -5000f; // Negativo per muoversi verso destra
            motorTorque = motorForce;
        }
        else if (brakeInput > 0)
        {
            // Frenata/retromarcia (verso sinistra)
            motorSpeed = 5000f; // Positivo per muoversi verso sinistra
            motorTorque = brakeForce;
        }

        // Apply to rear wheel (driving wheel)
        if (rearWheelJoint != null)
        {
            rearWheelMotor.motorSpeed = motorSpeed;
            rearWheelMotor.maxMotorTorque = motorTorque;
            rearWheelJoint.motor = rearWheelMotor;
            rearWheelJoint.useMotor = (accelerationInput > 0 || brakeInput > 0);
        }
    }

    private void ApplyRotationControl()
    {
        bool isGrounded = isFrontWheelGrounded || isRearWheelGrounded;

        if (!isGrounded)
        {
            // Air control - apply torque directly to vehicle body
            if (accelerationInput > 0)
            {
                // Rotazione quando si accelera in aria (verso destra)
                vehicleRigidbody.AddTorque(airRotationTorque * accelerationInput);
            }
            else if (brakeInput > 0)
            {
                // Rotazione quando si frena in aria (verso sinistra)
                vehicleRigidbody.AddTorque(-airRotationTorque * brakeInput);
            }
        }
        else
        {
            // Ground rotation effect - simulate weight transfer during acceleration
            float groundTorque = 0f;

            if (accelerationInput > 0)
            {
                // When accelerating on ground, vehicle tends to rotate back (wheelie effect)
                // This is affected by suspension compression
                float suspensionCompression = CalculateSuspensionCompression();
                groundTorque = airRotationTorque * groundRotationTorqueMultiplier *
                               accelerationInput * suspensionCompression;
            }

            vehicleRigidbody.AddTorque(groundTorque);
        }
    }

    private float CalculateSuspensionCompression()
    {
        // This is a simplified calculation - in a full implementation
        // you would check the actual compression of the suspension
        // through the wheel joint's current length vs rest length

        // For now, we'll use a simplified approach
        if (isFrontWheelGrounded && !isRearWheelGrounded)
        {
            // Front wheel touching but rear wheel not - high compression
            return 1.5f;
        }
        else if (isFrontWheelGrounded && isRearWheelGrounded)
        {
            // Both wheels touching - medium compression
            return 1.0f;
        }
        else if (!isFrontWheelGrounded && isRearWheelGrounded)
        {
            // Only rear wheel touching - low compression effect
            return 0.5f;
        }

        // No wheels touching ground
        return 0f;
    }
}