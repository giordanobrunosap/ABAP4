using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [Header("Wheels")]
    public Transform frontWheel;
    public Transform rearWheel;
    public WheelJoint2D frontWheelJoint;
    public WheelJoint2D rearWheelJoint;
    public float wheelRadius = 0.5f;
    public PhysicsMaterial2D wheelMaterial;

    [Header("Engine")]
    public float motorForce = 1500f;
    public float brakeForce = 1000f;
    public float maxSpeed = 20f;
    public float accelerationCurve = 1f;

    [Header("Body")]
    public float vehicleMass = 1.5f;               // Ridotto per maggiore manovrabilità
    public float vehicleLinearDrag = 0.05f;         // Ridotto per meno resistenza
    public float vehicleAngularDrag = 1.0f;         // Aumentato per stabilità
    public float gravityScale = 1f;
    public Vector2 centerOfMass = new Vector2(0, -0.1f); // Centro di massa equilibrato

    [Header("Suspension")]
    [Range(0.1f, 10f)] public float suspensionStiffness = 4f;  // Valore equilibrato
    [Range(0.1f, 5f)] public float suspensionDamping = 0.7f;   // Valore equilibrato 
    [Range(0f, 10f)] public float suspensionFrequency = 4f;    // Valore equilibrato
    public float suspensionAngle = 90f;
    public float suspensionDistance = 0.3f;                    // Ridotto per stabilità

    [Header("Collision")]
    public bool useContinuousCollision = true;
    public float collisionDetectionRadius = 0.2f;
    public LayerMask groundLayer = 1;
    public float friction = 0.4f;
    public float bounciness = 0.1f;

    // Input controls
    private float accelerationInput = 0f;
    private float brakeInput = 0f;

    // Rigidbody reference
    private Rigidbody2D rb;
    private CircleCollider2D frontWheelCollider;
    private CircleCollider2D rearWheelCollider;

    // Configurator GUI
    private bool showConfigurator = false;
    private Rect configuratorWindow;
    private Vector2 scrollPosition;

    // Tab labels for categorization
    private string[] tabLabels = { "Engine", "Suspension", "Physics", "Wheels", "Collision", "Debug" };
    private int selectedTab = 0;

    // Debug options
    private bool useDirectInput = true;
    private string lastInputLog = "";
    private bool showPhysicsGizmos = true;

    // Performance monitoring
    private float frameRate = 0f;
    private float physicsTimestep = 0f;
    private int frameCounter = 0;
    private float timeCounter = 0f;
    // END PART1
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        configuratorWindow = new Rect(10, 10, 400, 500);

        // Find colliders
        if (frontWheel != null) frontWheelCollider = frontWheel.GetComponent<CircleCollider2D>();
        if (rearWheel != null) rearWheelCollider = rearWheel.GetComponent<CircleCollider2D>();

        // Apply initial settings
        ApplyPhysicsSettings();
        UpdateSuspension();
        UpdateWheelProperties();
    }

    private void Start()
    {
        // Verifica i componenti essenziali
        if (rb == null)
            Debug.LogError("Manca il Rigidbody2D sul veicolo!");
        if (rearWheelJoint == null)
            Debug.LogError("Manca il WheelJoint2D posteriore!");

        // Applica impostazioni bilanciate iniziali    
        ApplyBalancedSettings();

        Time.fixedDeltaTime = 0.01f; // 100Hz physics as default
        physicsTimestep = Time.fixedDeltaTime;
    }

    private void ApplyBalancedSettings()
    {
        // Impostazioni di equilibratura base
        rb.mass = vehicleMass;
        rb.centerOfMass = centerOfMass;
        rb.drag = vehicleLinearDrag;
        rb.angularDrag = vehicleAngularDrag;

        // Assicura che entrambe le ruote abbiano impostazioni identiche
        if (frontWheelJoint != null && rearWheelJoint != null)
        {
            // Impostazioni delle sospensioni equilibrate
            JointSuspension2D suspension = new JointSuspension2D();
            suspension.dampingRatio = suspensionDamping;
            suspension.frequency = suspensionFrequency;
            suspension.angle = suspensionAngle;

            frontWheelJoint.suspension = suspension;
            rearWheelJoint.suspension = suspension;
        }

        UpdateSuspension();
        ApplyPhysicsSettings();
        UpdateWheelProperties();
    }

    private void Update()
    {
        // Calculate framerate
        frameCounter++;
        timeCounter += Time.deltaTime;
        if (timeCounter >= 0.5f)
        {
            frameRate = frameCounter / timeCounter;
            frameCounter = 0;
            timeCounter = 0f;
        }

        // Input handling - Destra/Sinistra controllano l'accelerazione
        if (!useDirectInput)
        {
            // Controlli normali attraverso Input.GetAxis
            // Invertito il segno per correggere la direzione
            accelerationInput = -Input.GetAxis("Horizontal");
            brakeInput = Input.GetKey(KeyCode.Space) ? 1.0f : 0.0f;
        }
        else
        {
            // Controlli diretti con direzioni corrette
            // D/Right va a destra (valore negativo per il motore)
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                accelerationInput = -1.0f;  // Valore negativo per andare a destra
                lastInputLog = "DESTRA premuto: " + Time.time;
            }
            // A/Left va a sinistra (valore positivo per il motore)
            else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                accelerationInput = 1.0f;  // Valore positivo per andare a sinistra
                lastInputLog = "SINISTRA premuto: " + Time.time;
            }
            else
            {
                accelerationInput = 0f;  // Nessun input
            }

            brakeInput = Input.GetKey(KeyCode.Space) ? 1.0f : 0.0f;
        }

        // UI toggles
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            showConfigurator = !showConfigurator;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            useDirectInput = !useDirectInput;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            showPhysicsGizmos = !showPhysicsGizmos;
        }

        // Performance adjustment keys
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            AdjustPhysicsTimestep(-0.001f); // Decrease timestep (increase rate)
        }
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            AdjustPhysicsTimestep(0.001f); // Increase timestep (decrease rate)
        }
    }

    private void AdjustPhysicsTimestep(float amount)
    {
        physicsTimestep = Mathf.Clamp(physicsTimestep + amount, 0.005f, 0.05f);
        Time.fixedDeltaTime = physicsTimestep;
    }

    private void FixedUpdate()
    {
        // Apply motor force
        ApplyMotorForce();
    }
    //END PART2
    private void ApplyMotorForce()
    {
        if (rearWheelJoint != null)
        {
            JointMotor2D motor = rearWheelJoint.motor;

            if (accelerationInput > 0)
            {
                // Accelerate left (A/Left) - direzione corretta
                float speed = maxSpeed * 5000f;
                // Apply acceleration curve if configured
                if (accelerationCurve != 1f)
                {
                    speed *= Mathf.Pow(Mathf.Abs(accelerationInput), accelerationCurve);
                }
                motor.motorSpeed = speed;
                motor.maxMotorTorque = motorForce;
            }
            else if (accelerationInput < 0)
            {
                // Accelerate right (D/Right) - direzione corretta
                float speed = -maxSpeed * 5000f;
                // Apply acceleration curve if configured
                if (accelerationCurve != 1f)
                {
                    speed *= Mathf.Pow(Mathf.Abs(accelerationInput), accelerationCurve);
                }
                motor.motorSpeed = speed;
                motor.maxMotorTorque = motorForce;
            }
            else if (brakeInput > 0)
            {
                // Brake (Space)
                motor.motorSpeed = 0;
                motor.maxMotorTorque = brakeForce;
            }
            else
            {
                // Coast with very minimal resistance
                motor.motorSpeed = 0;
                motor.maxMotorTorque = 0.1f;
            }

            rearWheelJoint.motor = motor;
            rearWheelJoint.useMotor = true;
        }
    }

    private bool IsGrounded()
    {
        float rayDistance = suspensionDistance;
        bool frontGrounded = Physics2D.Raycast(frontWheel.position, Vector2.down, rayDistance, groundLayer);
        bool rearGrounded = Physics2D.Raycast(rearWheel.position, Vector2.down, rayDistance, groundLayer);

        return frontGrounded || rearGrounded;
    }

    private void ApplyPhysicsSettings()
    {
        if (rb != null)
        {
            rb.mass = vehicleMass;
            rb.drag = vehicleLinearDrag;
            rb.angularDrag = vehicleAngularDrag;
            rb.gravityScale = gravityScale;
            rb.centerOfMass = centerOfMass;
            rb.collisionDetectionMode = useContinuousCollision ?
                CollisionDetectionMode2D.Continuous : CollisionDetectionMode2D.Discrete;
        }
    }

    private void UpdateWheelProperties()
    {
        UpdateWheelCollider(frontWheelCollider);
        UpdateWheelCollider(rearWheelCollider);
    }

    private void UpdateWheelCollider(CircleCollider2D collider)
    {
        if (collider != null)
        {
            collider.radius = wheelRadius;

            if (wheelMaterial != null)
            {
                collider.sharedMaterial = wheelMaterial;
            }
            else
            {
                // Create dynamic material if none assigned
                PhysicsMaterial2D material = new PhysicsMaterial2D("WheelMaterial");
                material.friction = friction;
                material.bounciness = bounciness;
                collider.sharedMaterial = material;
            }
        }
    }

    private void UpdateSuspension()
    {
        if (frontWheelJoint != null)
        {
            JointSuspension2D suspension = frontWheelJoint.suspension;
            suspension.dampingRatio = suspensionDamping;
            suspension.frequency = suspensionFrequency;
            suspension.angle = suspensionAngle;
            frontWheelJoint.suspension = suspension;

            // Additionally set the connectedAnchor distance if the joint supports it
            frontWheelJoint.connectedAnchor = new Vector2(
                frontWheelJoint.connectedAnchor.x,
                suspensionDistance
            );
        }

        if (rearWheelJoint != null)
        {
            JointSuspension2D suspension = rearWheelJoint.suspension;
            suspension.dampingRatio = suspensionDamping;
            suspension.frequency = suspensionFrequency;
            suspension.angle = suspensionAngle;
            rearWheelJoint.suspension = suspension;

            // Additionally set the connectedAnchor distance if the joint supports it
            rearWheelJoint.connectedAnchor = new Vector2(
                rearWheelJoint.connectedAnchor.x,
                suspensionDistance
            );
        }
    }

    private void ResetVehicle()
    {
        transform.position = new Vector3(0, 3, 0);
        transform.rotation = Quaternion.identity;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.WakeUp();
    }

    private void ApplyAllSettings()
    {
        ApplyPhysicsSettings();
        UpdateSuspension();
        UpdateWheelProperties();
    }
    //END PART3
    private void OnGUI()
    {
        // Visualizza sempre la posizione del veicolo
        GUI.Label(new Rect(10, 10, 300, 20), "Pos: " + transform.position.ToString("F1") + " Vel: " + rb.velocity.magnitude.ToString("F1") + " m/s");

        // Display FPS
        float msPerFrame = 1000.0f / frameRate;
        GUI.Label(new Rect(10, 30, 300, 20), string.Format("FPS: {0:F1} ({1:F1}ms) - Physics: {2:F1}Hz", frameRate, msPerFrame, 1.0f / physicsTimestep));

        if (showConfigurator)
        {
            configuratorWindow = GUILayout.Window(0, configuratorWindow, DrawConfiguratorWindow, "Vehicle Configurator");
        }

        // Input mode indicator
        GUI.Label(new Rect(10, Screen.height - 30, 350, 20),
            "Input: " + (useDirectInput ? "DIRECT KEYS (D=Destra, A=Sinistra)" : "NORMAL") +
            " | Tab: Config | G: Gizmos | +/-: Physics Rate");
    }

    private void DrawConfiguratorWindow(int windowID)
    {
        // Add tabs at the top of the window
        selectedTab = GUILayout.Toolbar(selectedTab, tabLabels);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0: // Engine
                DrawEngineSettings();
                break;
            case 1: // Suspension
                DrawSuspensionSettings();
                break;
            case 2: // Physics
                DrawPhysicsSettings();
                break;
            case 3: // Wheels
                DrawWheelSettings();
                break;
            case 4: // Collision
                DrawCollisionSettings();
                break;
            case 5: // Debug
                DrawDebugSettings();
                break;
        }

        GUILayout.EndScrollView();

        // Debug info
        GUILayout.Space(10);
        GUILayout.Label($"Speed: {rb.velocity.magnitude:F1} m/s");
        GUILayout.Label($"Grounded: {IsGrounded()}");
        GUILayout.Label($"Input: Accel={accelerationInput:F2}");

        // Vehicle info
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset Vehicle", GUILayout.Width(120)))
        {
            ResetVehicle();
        }
        if (GUILayout.Button("Reset to Balanced", GUILayout.Width(120)))
        {
            ApplyBalancedSettings();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Apply Settings", GUILayout.Height(30)))
        {
            ApplyAllSettings();
        }

        // Make the window draggable
        GUI.DragWindow();
    }
    //END PART4
    private void DrawEngineSettings()
    {
        GUILayout.Label("Engine Settings", "box");

        GUILayout.Label("Motor Force: " + motorForce);
        motorForce = GUILayout.HorizontalSlider(motorForce, 500f, 15000f);

        GUILayout.Label("Brake Force: " + brakeForce);
        brakeForce = GUILayout.HorizontalSlider(brakeForce, 500f, 5000f);

        GUILayout.Label("Max Speed: " + maxSpeed);
        maxSpeed = GUILayout.HorizontalSlider(maxSpeed, 5f, 40f);

        GUILayout.Label("Acceleration Curve: " + accelerationCurve.ToString("F2"));
        accelerationCurve = GUILayout.HorizontalSlider(accelerationCurve, 0.5f, 3f);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Soft Start", GUILayout.Width(100)))
        {
            accelerationCurve = 2.0f;
        }
        if (GUILayout.Button("Linear", GUILayout.Width(100)))
        {
            accelerationCurve = 1.0f;
        }
        if (GUILayout.Button("Instant", GUILayout.Width(100)))
        {
            accelerationCurve = 0.5f;
        }
        GUILayout.EndHorizontal();
    }

    private void DrawSuspensionSettings()
    {
        GUILayout.Label("Suspension Settings", "box");

        GUILayout.Label("Suspension Stiffness: " + suspensionStiffness);
        float newStiffness = GUILayout.HorizontalSlider(suspensionStiffness, 0.1f, 10f);

        GUILayout.Label("Suspension Damping: " + suspensionDamping);
        float newDamping = GUILayout.HorizontalSlider(suspensionDamping, 0.1f, 5f);

        GUILayout.Label("Suspension Frequency: " + suspensionFrequency);
        float newFrequency = GUILayout.HorizontalSlider(suspensionFrequency, 0.1f, 10f);

        GUILayout.Label("Suspension Angle: " + suspensionAngle);
        float newAngle = GUILayout.HorizontalSlider(suspensionAngle, 0f, 180f);

        GUILayout.Label("Suspension Distance: " + suspensionDistance);
        float newDistance = GUILayout.HorizontalSlider(suspensionDistance, 0.1f, 1f);

        // Check for changes and update suspension
        if (newStiffness != suspensionStiffness ||
            newDamping != suspensionDamping ||
            newFrequency != suspensionFrequency ||
            newAngle != suspensionAngle ||
            newDistance != suspensionDistance)
        {
            suspensionStiffness = newStiffness;
            suspensionDamping = newDamping;
            suspensionFrequency = newFrequency;
            suspensionAngle = newAngle;
            suspensionDistance = newDistance;
            UpdateSuspension();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Soft", GUILayout.Width(100)))
        {
            suspensionStiffness = 1f;
            suspensionDamping = 0.3f;
            suspensionFrequency = 2f;
            UpdateSuspension();
        }
        if (GUILayout.Button("Medium", GUILayout.Width(100)))
        {
            suspensionStiffness = 5f;
            suspensionDamping = 0.5f;
            suspensionFrequency = 5f;
            UpdateSuspension();
        }
        if (GUILayout.Button("Hard", GUILayout.Width(100)))
        {
            suspensionStiffness = 10f;
            suspensionDamping = 1f;
            suspensionFrequency = 8f;
            UpdateSuspension();
        }
        GUILayout.EndHorizontal();
    }
    //END PART5
    private void DrawPhysicsSettings()
    {
        GUILayout.Label("Physics Settings", "box");

        GUILayout.Label("Vehicle Mass: " + vehicleMass);
        float newMass = GUILayout.HorizontalSlider(vehicleMass, 0.1f, 20f);

        GUILayout.Label("Linear Drag: " + vehicleLinearDrag);
        float newLinearDrag = GUILayout.HorizontalSlider(vehicleLinearDrag, 0f, 2f);

        GUILayout.Label("Angular Drag: " + vehicleAngularDrag);
        float newAngularDrag = GUILayout.HorizontalSlider(vehicleAngularDrag, 0f, 5f);

        GUILayout.Label("Gravity Scale: " + gravityScale);
        float newGravityScale = GUILayout.HorizontalSlider(gravityScale, 0.1f, 5f);

        GUILayout.Label("Center of Mass X: " + centerOfMass.x);
        float newCOMX = GUILayout.HorizontalSlider(centerOfMass.x, -1f, 1f);

        GUILayout.Label("Center of Mass Y: " + centerOfMass.y);
        float newCOMY = GUILayout.HorizontalSlider(centerOfMass.y, -1f, 1f);

        bool physicsChanged = false;

        if (newMass != vehicleMass ||
            newLinearDrag != vehicleLinearDrag ||
            newAngularDrag != vehicleAngularDrag ||
            newGravityScale != gravityScale ||
            newCOMX != centerOfMass.x ||
            newCOMY != centerOfMass.y)
        {
            vehicleMass = newMass;
            vehicleLinearDrag = newLinearDrag;
            vehicleAngularDrag = newAngularDrag;
            gravityScale = newGravityScale;
            centerOfMass = new Vector2(newCOMX, newCOMY);

            physicsChanged = true;
        }

        GUILayout.Label("Physics Simulation Speed");
        float newTimestep = GUILayout.HorizontalSlider(physicsTimestep, 0.005f, 0.05f);
        if (newTimestep != physicsTimestep)
        {
            physicsTimestep = newTimestep;
            Time.fixedDeltaTime = physicsTimestep;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("60Hz", GUILayout.Width(80)))
        {
            physicsTimestep = 1f / 60f;
            Time.fixedDeltaTime = physicsTimestep;
        }
        if (GUILayout.Button("100Hz", GUILayout.Width(80)))
        {
            physicsTimestep = 1f / 100f;
            Time.fixedDeltaTime = physicsTimestep;
        }
        if (GUILayout.Button("144Hz", GUILayout.Width(80)))
        {
            physicsTimestep = 1f / 144f;
            Time.fixedDeltaTime = physicsTimestep;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Continuous", GUILayout.Width(120)))
        {
            useContinuousCollision = true;
            physicsChanged = true;
        }
        if (GUILayout.Button("Discrete", GUILayout.Width(120)))
        {
            useContinuousCollision = false;
            physicsChanged = true;
        }
        GUILayout.EndHorizontal();

        // Update physics settings if needed
        if (physicsChanged)
        {
            ApplyPhysicsSettings();
        }
    }
    //END PART6
    private void DrawWheelSettings()
    {
        GUILayout.Label("Wheel Settings", "box");

        GUILayout.Label("Wheel Radius: " + wheelRadius);
        float newRadius = GUILayout.HorizontalSlider(wheelRadius, 0.1f, 2f);

        GUILayout.Label("Wheel Friction: " + friction);
        float newFriction = GUILayout.HorizontalSlider(friction, 0f, 1f);

        GUILayout.Label("Wheel Bounciness: " + bounciness);
        float newBounciness = GUILayout.HorizontalSlider(bounciness, 0f, 1f);

        bool wheelPropertiesChanged = false;

        if (newRadius != wheelRadius ||
            newFriction != friction ||
            newBounciness != bounciness)
        {
            wheelRadius = newRadius;
            friction = newFriction;
            bounciness = newBounciness;

            wheelPropertiesChanged = true;
        }

        if (wheelPropertiesChanged)
        {
            UpdateWheelProperties();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Slick Tires", GUILayout.Width(100)))
        {
            friction = 0.8f;
            bounciness = 0.1f;
            UpdateWheelProperties();
        }
        if (GUILayout.Button("All-Terrain", GUILayout.Width(100)))
        {
            friction = 0.6f;
            bounciness = 0.3f;
            UpdateWheelProperties();
        }
        if (GUILayout.Button("Bouncy", GUILayout.Width(100)))
        {
            friction = 0.3f;
            bounciness = 0.8f;
            UpdateWheelProperties();
        }
        GUILayout.EndHorizontal();
    }

    private void DrawCollisionSettings()
    {
        GUILayout.Label("Collision Settings", "box");

        GUILayout.Label("Continuous Collision: " + useContinuousCollision);
        bool newContinuous = GUILayout.Toggle(useContinuousCollision, "Enable Continuous Collision");

        GUILayout.Label("Collision Detection Radius: " + collisionDetectionRadius);
        float newRadius = GUILayout.HorizontalSlider(collisionDetectionRadius, 0.1f, 1f);

        // Layer selection
        GUILayout.Label("Ground Layer: " + LayerMask.LayerToName(Mathf.RoundToInt(Mathf.Log(groundLayer.value, 2))));

        bool collisionChanged = false;

        if (newContinuous != useContinuousCollision ||
            newRadius != collisionDetectionRadius)
        {
            useContinuousCollision = newContinuous;
            collisionDetectionRadius = newRadius;

            collisionChanged = true;
        }

        if (collisionChanged)
        {
            ApplyPhysicsSettings();
        }

        GUILayout.Space(10);

        GUILayout.Label("Advanced Box2D Settings", "box");

        // Sleeper settings
        GUILayout.Label("Sleep Settings");
        if (GUILayout.Button("Wake All Physics Objects"))
        {
            var allRigidbodies = FindObjectsOfType<Rigidbody2D>();
            foreach (var rb in allRigidbodies)
            {
                rb.WakeUp();
            }
        }

        // Global Physics2D settings access
        GUILayout.Label("Global Physics2D Settings");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Default Quality", GUILayout.Width(120)))
        {
            Physics2D.velocityIterations = 8;
            Physics2D.positionIterations = 3;
        }
        if (GUILayout.Button("High Quality", GUILayout.Width(120)))
        {
            Physics2D.velocityIterations = 16;
            Physics2D.positionIterations = 6;
        }
        GUILayout.EndHorizontal();

        // Show current Physics2D.settings
        GUILayout.Label("Current Physics2D Settings");
        GUILayout.Label("Velocity Iterations: " + Physics2D.velocityIterations);
        GUILayout.Label("Position Iterations: " + Physics2D.positionIterations);
        GUILayout.Label("Default Contact Offset: " + Physics2D.defaultContactOffset);
    }
    //END PART7
    private void DrawDebugSettings()
    {
        GUILayout.Label("Debug Options", "box");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(useDirectInput ? "Use Normal Input" : "Use Direct Keys"))
        {
            useDirectInput = !useDirectInput;
        }
        if (GUILayout.Button(showPhysicsGizmos ? "Hide Gizmos" : "Show Gizmos"))
        {
            showPhysicsGizmos = !showPhysicsGizmos;
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Debug Wheel Joints"))
        {
            if (rearWheelJoint != null)
            {
                Debug.Log("=== REAR WHEEL JOINT DEBUG ===");
                Debug.Log("Use Motor: " + rearWheelJoint.useMotor);
                Debug.Log("Motor Speed: " + rearWheelJoint.motor.motorSpeed);
                Debug.Log("Motor Torque: " + rearWheelJoint.motor.maxMotorTorque);
                Debug.Log("Connected Body: " + (rearWheelJoint.connectedBody != null ? rearWheelJoint.connectedBody.name : "None"));
                Debug.Log("Anchor: " + rearWheelJoint.anchor);
                Debug.Log("Connected Anchor: " + rearWheelJoint.connectedAnchor);
                Debug.Log("Suspension: Damping=" + rearWheelJoint.suspension.dampingRatio + ", Freq=" + rearWheelJoint.suspension.frequency);
            }
            else
            {
                Debug.LogError("Rear wheel joint is missing!");
            }
        }

        // Input debug
        GUILayout.Label("Input Debug:", "box");
        GUILayout.Label("D/Right: " + (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)));
        GUILayout.Label("A/Left: " + (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)));
        GUILayout.Label("Space (Brake): " + Input.GetKey(KeyCode.Space));
        GUILayout.Label("Horizontal Axis: " + Input.GetAxis("Horizontal"));
        GUILayout.Label("Final Input Value: " + accelerationInput);

        GUILayout.Space(10);
        GUILayout.Label("Physics Debug:", "box");
        GUILayout.Label("Velocity: " + rb.velocity.ToString("F2"));
        GUILayout.Label("Angular Velocity: " + rb.angularVelocity.ToString("F2"));
        GUILayout.Label("Is Sleeping: " + rb.IsSleeping());
        GUILayout.Label("Is Awake: " + rb.IsAwake());

        if (GUILayout.Button("Force Awake"))
        {
            rb.WakeUp();
        }

        GUILayout.Space(10);

        GUILayout.Label("Test Controls:", "box");
        if (GUILayout.Button("Apply Force Right"))
        {
            rb.AddForce(Vector2.right * 2000f);
        }
        if (GUILayout.Button("Apply Force Left"))
        {
            rb.AddForce(Vector2.left * 2000f);
        }
        if (GUILayout.Button("Apply Torque"))
        {
            rb.AddTorque(500f);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showPhysicsGizmos) return;

        // Only draw detailed gizmos during play mode
        if (!Application.isPlaying) return;

        // Draw center of mass
        if (rb != null)
        {
            Gizmos.color = Color.red;
            Vector3 com = transform.TransformPoint(rb.centerOfMass);
            Gizmos.DrawSphere(com, 0.1f);
        }

        // Draw wheel colliders
        if (frontWheel != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(frontWheel.position, wheelRadius);
        }

        if (rearWheel != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rearWheel.position, wheelRadius);
        }

        // Draw ground detection rays
        if (frontWheel != null && rearWheel != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(frontWheel.position, frontWheel.position + Vector3.down * suspensionDistance);
            Gizmos.DrawLine(rearWheel.position, rearWheel.position + Vector3.down * suspensionDistance);
        }
    }
}