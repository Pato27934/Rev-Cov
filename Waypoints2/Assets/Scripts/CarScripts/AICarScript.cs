using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class AICarScript : MonoBehaviour
{
    private Rigidbody rigidBody;

    public GameManager gameManager;
    public PlayerInfoUI carInfoUI;

    [Header("Player Data")]
    public string playerName = " ";
    public float velocity = 0.0f;
    public Color bodyColor;
    public string iconString = "";
    public int lapsCompleted = 0;

    [Header("Navigation")]
    public Transform[] path;
    public GameObject pathGroup;
    public int currentPathObj;
    public int remainingNodes;
    public float distFromPath = 20f;

    [Header("Specs")]
    public float maxSteer = 22f;
    public float maxTorque = 400f;
    public float currentSpeed;
    public float topSpeed = 150f;
    public float decelerationSpeed = 15f;
    public Transform centerOfMass;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    [Header("AI Sensors")]
    public Color sensorColor = Color.white;
    public float sensorLength = 30f;
    public float frontSensorStartPoint = 2.52f;
    public float frontSensorSideDistance = 1f;
    public float frontSensorAngle = 30f;
    public float sidewaysSensorLength = 25f;
    public float avoidSpeed = 30f;
    private int detectionFlag = 0;
    public float respawnWait = 1.5f;
    public float respawnCounter = 0.0f;
    public float sensorHeightOffset = 1.5f;
    public LayerMask interactionLayers = ~0;

    #region Base Functions

    void Awake()
    {
        pathGroup = GameObject.Find("Path");
        //carInfoUI = GameObject.Find("GameManager").GetComponent<PlayerInfoUI>();
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.centerOfMass = centerOfMass.localPosition;
    }

    void Start()
    {
        Renderer renderer = transform.GetChild(0).GetComponent<Renderer>();
        Material material = renderer.material;

        material.SetColor("_Color", bodyColor);
        GetPath();
    }

    void Update()
    {
        if (detectionFlag == 0)
        {
            distFromPath = 20f;
            GetSteer();
        }
        else
        {
            distFromPath = 40f;
        }

        Move();
        Sensors();
        Respawn();
        //SendInfo();

        if (path != null && path.Length > 0)
        {
            Debug.DrawLine(transform.position, path[currentPathObj].position, Color.green);
        }
        carInfoUI.updateLaps(lapsCompleted);
    }

    #endregion

    #region Navigation Functions

    void GetPath()
    {
        Transform[] path_nodes = pathGroup.GetComponentsInChildren<Transform>();
        path = new Transform[path_nodes.Length - 1];

        for (int i = 1; i < path_nodes.Length; i++)
        {
            path[i - 1] = path_nodes[i];
        }
        remainingNodes = path.Length;
    }

    #endregion

    #region Movement Functions

    void GetSteer()
    {
        UnityEngine.Vector3 steerVector = transform.InverseTransformPoint(new UnityEngine.Vector3(path[currentPathObj].position.x, transform.position.y, path[currentPathObj].position.z));

        float newSteer = maxSteer * (steerVector.x / steerVector.magnitude);
        frontLeft.steerAngle = newSteer;
        frontRight.steerAngle = newSteer;

        if (steerVector.magnitude < distFromPath)
        {
            currentPathObj++;
            remainingNodes--;
            if (currentPathObj >= path.Length)
            {
                currentPathObj = 0;
                remainingNodes = path.Length;
                lapsCompleted++;

                gameManager.CheckConditions(gameObject, lapsCompleted, remainingNodes);
            }
        }
    }

    void Move()
    {
        // Use Rigidbody velocity for current speed
        currentSpeed = rigidBody.velocity.magnitude;

        // Clamp velocity to topSpeed
        float targetSpeed = Mathf.Min(velocity, topSpeed);

        // Drift detection: angle between velocity and forward direction
        float driftAngle = Vector3.Angle(rigidBody.velocity, transform.forward);
        bool isDrifting = currentSpeed > 8f && driftAngle > 2f; // adjust threshold as needed

        if (isDrifting)
        {
            rigidBody.drag = 3.5f; // High drag for rapid slowdown (tune as needed)
        }
        else
        {
            rigidBody.drag = 0.05f; // Default drag (tune as needed)
        }

        if (currentSpeed < targetSpeed - 1f) // accelerate if below target (with a small deadzone)
        {
            if (isDrifting)
            {
                rearLeft.motorTorque = maxTorque * 0f;
                rearRight.motorTorque = maxTorque * 0f;
                rearLeft.brakeTorque = decelerationSpeed;
                rearRight.brakeTorque = decelerationSpeed;
            }
            else
            {
                rearLeft.motorTorque = maxTorque;
                rearRight.motorTorque = maxTorque;
                rearLeft.brakeTorque = 0f;
                rearRight.brakeTorque = 0f;
            }
        }
        else if (currentSpeed > targetSpeed + 1f) // brake if above target (with a small deadzone)
        {
            rearLeft.motorTorque = 0f;
            rearRight.motorTorque = 0f;
            rearLeft.brakeTorque = decelerationSpeed;
            rearRight.brakeTorque = decelerationSpeed;
        }
        else // maintain speed (coast)
        {
            rearLeft.motorTorque = 0f;
            rearRight.motorTorque = 0f;
            rearLeft.brakeTorque = 0f;
            rearRight.brakeTorque = 0f;
        }
    }

    #endregion

    #region AI Functions
    void Sensors()
    {
        detectionFlag = 0;
        float avoidSensitivity = 0f;

        UnityEngine.Vector3 pos;
        RaycastHit hit;

        // Braking Sensor
        pos = transform.position;
        pos += transform.forward * frontSensorStartPoint;
        Debug.DrawLine(pos, pos + Vector3.up * 0.5f, Color.white); // <--- Marker

        if (Physics.Raycast(pos, transform.forward, out hit, sensorLength))
        {
            if (hit.transform.tag != "Untagged")
            {
                detectionFlag++;
                Debug.Log("Avoiding Right");
                Debug.DrawLine(pos, hit.point, sensorColor);
            }
        }

        // Right Sensor
        pos += transform.right * frontSensorSideDistance;
        Debug.DrawLine(pos, pos + Vector3.up * 0.5f, Color.red); // <--- Marker

        if (Physics.SphereCast(pos, 0.5f, transform.forward, out hit, sensorLength))
        {
            if (hit.transform.tag != "Untagged")
            {
                detectionFlag++;
                avoidSensitivity -= 1f;
                Debug.Log("Avoiding Right");
                Debug.DrawLine(pos, hit.point, sensorColor);
            }
        }
        // Left Sensor
        pos = transform.position;
        pos += transform.forward * frontSensorStartPoint;
        pos -= transform.right * frontSensorSideDistance;
        Debug.DrawLine(pos, pos + Vector3.up * 0.5f, Color.blue); // <--- Marker

        if (Physics.SphereCast(pos, 0.5f, transform.forward, out hit, sensorLength))
        {
            if (hit.transform.tag != "Untagged")
            {
                detectionFlag++;
                avoidSensitivity += 1f;
                Debug.Log("Avoiding Left");
                Debug.DrawLine(pos, hit.point, sensorColor);
            }
        }

        // Side Right
        Debug.DrawLine(transform.position, transform.position + Vector3.up * 0.5f, Color.magenta); // <--- Marker

        if (Physics.Raycast(transform.position, transform.right, out hit, sidewaysSensorLength))
        {
            if (hit.transform.tag != "Untagged")
            {
                detectionFlag++;
                avoidSensitivity -= 0.5f;
                Debug.Log("Avoiding Right");
                Debug.DrawLine(transform.position, hit.point, sensorColor);
            }
        }

        // Side Left
        Debug.DrawLine(transform.position, transform.position + Vector3.up * 0.5f, Color.cyan); // <--- Marker

        if (Physics.Raycast(transform.position, -transform.right, out hit, sidewaysSensorLength))
        {
            if (hit.transform.tag != "Untagged")
            {
                detectionFlag++;
                avoidSensitivity += 0.5f;
                Debug.Log("Avoiding Left");
                Debug.DrawLine(transform.position, hit.point, sensorColor);
            }
        }

        // Mid Sensor
        pos = transform.position;
        pos += transform.forward * frontSensorStartPoint;
        Debug.DrawLine(pos, pos + Vector3.up * 0.5f, Color.yellow); // <--- Marker

        if (avoidSensitivity == 0)
        {
            if (Physics.Raycast(pos, transform.forward, out hit, sensorLength))
            {
                if (hit.transform.tag != "Untagged")
                {
                    if (hit.normal.x < 0)
                    {
                        avoidSensitivity = -1f;
                    }
                    else
                    {
                        avoidSensitivity = 1f;
                    }
                    Debug.Log("Avoiding Mid");
                    Debug.DrawLine(pos, hit.point, sensorColor);
                }
            }
        }

        //---------------------------------------- Flag Validation ----------------------------------------

        // Draw all sensor rays for visualization
        Vector3 basePos = transform.position + transform.forward * frontSensorStartPoint;
        basePos.y += sensorHeightOffset;
        Vector3 rightPos = basePos + transform.right * frontSensorSideDistance;
        Vector3 leftPos = basePos - transform.right * frontSensorSideDistance;
        Vector3 sideRightPos = transform.position;
        sideRightPos.y += sensorHeightOffset;
        Vector3 sideLeftPos = transform.position;
        sideLeftPos.y += sensorHeightOffset;

        Debug.DrawRay(rightPos, transform.forward * sensorLength, Color.red);    // Right Sensor
        Debug.DrawRay(leftPos, transform.forward * sensorLength, Color.blue);    // Left Sensor
        Debug.DrawRay(sideRightPos, transform.right * sidewaysSensorLength, Color.magenta); // Side Right
        Debug.DrawRay(sideLeftPos, -transform.right * sidewaysSensorLength, Color.cyan);   // Side Left
        Debug.DrawRay(basePos, transform.forward * sensorLength, Color.yellow); // Mid Sensor

        if (detectionFlag != 0)
        {
            AvoidSteer(avoidSensitivity);
        }
    }

    void AvoidSteer(float sensitivity)
    {
        frontLeft.steerAngle = avoidSpeed * sensitivity;
        frontRight.steerAngle = avoidSpeed * sensitivity;
    }

    #endregion

    #region Miscellaneous

    void Respawn()
    {
        if (rigidBody.velocity.magnitude < 0.1f || transform.position.y < -5f)
        {
            respawnCounter += Time.deltaTime;
            if (respawnCounter >= respawnWait)
            {
                if (currentPathObj == 0)
                {
                    gameObject.transform.position = path[path.Length - 1].position;
                }
                else
                {
                    gameObject.transform.position = path[currentPathObj - 1].position;
                }
                respawnCounter = 0;
                gameObject.transform.Rotate(gameObject.transform.rotation.x, gameObject.transform.rotation.y, 0);
            }

        }
    }
/*
    void SendInfo()
    {
        carInfoUI.UpdateInfo(playerName, velocity, bodyColor, iconString, lapsCompleted);
    }
*/
    #endregion
}