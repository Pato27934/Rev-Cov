using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICar : MonoBehaviour
{
    private Rigidbody rigidBody;

    [Header("AI Settings")]
    public float velocity = 0.0f;
    public float maxSteer = 22f;
    public float maxTorque = 400f;
    public float decelerationSpeed = 15f;
    public float topSpeed = 150f;
    public float distFromPath = 20f;

    [Header("Path Settings")]
    public Transform[] path;
    public GameObject pathGroup;
    public int currentPathObj;
    public int remainingNodes;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;
    public Transform centerOfMass;

    [Header("Sensors")]
    public Color sensorColor = Color.white;
    public float sensorLength = 30f;
    public float frontSensorStartPoint = 2.52f;
    public float frontSensorSideDistance = 1f;
    public float sidewaysSensorLength = 25f;
    public float avoidSpeed = 30f;
    public float sensorHeightOffset = 1.5f;
    public LayerMask interactionLayers = ~0;

    private int detectionFlag = 0;

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        if (centerOfMass != null)
            rigidBody.centerOfMass = centerOfMass.localPosition;
    }

    void Start()
    {
        // Inicializar ruta si ya está asignada por el spawner
        if (pathGroup != null)
            InitializePath();
    }

    private void InitializePath()
    {
        if (pathGroup == null) return;

        Transform[] path_nodes = pathGroup.GetComponentsInChildren<Transform>();
        path = new Transform[path_nodes.Length - 1];

        for (int i = 1; i < path_nodes.Length; i++)
        {
            path[i - 1] = path_nodes[i];
        }

        remainingNodes = path.Length;
        currentPathObj = 0;
    }

    void Update()
    {
        if (path != null && path.Length > 0 && currentPathObj < path.Length)
        {
            Debug.DrawLine(transform.position, path[currentPathObj].position, Color.green);
        }

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
    }

    #region Navigation

    void GetSteer()
    {
        if (path == null || path.Length == 0) return;

        Vector3 steerVector = transform.InverseTransformPoint(
            new Vector3(path[currentPathObj].position.x, transform.position.y, path[currentPathObj].position.z)
        );

        float newSteer = maxSteer * (steerVector.x / steerVector.magnitude);
        frontLeft.steerAngle = newSteer;
        frontRight.steerAngle = newSteer;

        if (steerVector.magnitude < distFromPath)
        {
            if (currentPathObj < path.Length - 1)
            {
                currentPathObj++;
                remainingNodes--;
            }
            else
            {
                ReachEndOfPath(); // Último nodo alcanzado
            }
        }
    }

    private void ReachEndOfPath()
    {
        // Frenar el auto
        rearLeft.motorTorque = 0f;
        rearRight.motorTorque = 0f;
        rearLeft.brakeTorque = decelerationSpeed * 2f;
        rearRight.brakeTorque = decelerationSpeed * 2f;

        // Desactivar IA y collider
        this.enabled = false;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Destruir suavemente después de 2 segundos
        Destroy(gameObject, 2f);
    }

    #endregion

    #region Movement

    void Move()
    {
        float currentSpeed = rigidBody.velocity.magnitude;
        float targetSpeed = Mathf.Min(velocity, topSpeed);

        // Drift detection
        float driftAngle = Vector3.Angle(rigidBody.velocity, transform.forward);
        bool isDrifting = currentSpeed > 8f && driftAngle > 2f;

        rigidBody.drag = isDrifting ? 3.5f : 0.05f;

        if (currentSpeed < targetSpeed - 1f)
        {
            if (isDrifting)
            {
                rearLeft.motorTorque = 0f;
                rearRight.motorTorque = 0f;
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
        else if (currentSpeed > targetSpeed + 1f)
        {
            rearLeft.motorTorque = 0f;
            rearRight.motorTorque = 0f;
            rearLeft.brakeTorque = decelerationSpeed;
            rearRight.brakeTorque = decelerationSpeed;
        }
        else
        {
            rearLeft.motorTorque = 0f;
            rearRight.motorTorque = 0f;
            rearLeft.brakeTorque = 0f;
            rearRight.brakeTorque = 0f;
        }
    }

    #endregion

    #region AI Sensors

    void Sensors()
    {
        detectionFlag = 0;
        float avoidSensitivity = 0f;
        Vector3 pos;
        RaycastHit hit;

        // Front sensor
        pos = transform.position + transform.forward * frontSensorStartPoint;
        if (Physics.Raycast(pos, transform.forward, out hit, sensorLength, interactionLayers))
        {
            detectionFlag++;
            Debug.DrawLine(pos, hit.point, sensorColor);
        }

        // Side sensors
        if (Physics.Raycast(transform.position + transform.right * frontSensorSideDistance, transform.forward, out hit, sensorLength, interactionLayers))
        {
            detectionFlag++;
            avoidSensitivity -= 1f;
            Debug.DrawLine(transform.position, hit.point, sensorColor);
        }

        if (Physics.Raycast(transform.position - transform.right * frontSensorSideDistance, transform.forward, out hit, sensorLength, interactionLayers))
        {
            detectionFlag++;
            avoidSensitivity += 1f;
            Debug.DrawLine(transform.position, hit.point, sensorColor);
        }

        if (detectionFlag != 0)
            AvoidSteer(avoidSensitivity);
    }

    void AvoidSteer(float sensitivity)
    {
        frontLeft.steerAngle = avoidSpeed * sensitivity;
        frontRight.steerAngle = avoidSpeed * sensitivity;
    }

    #endregion
}
