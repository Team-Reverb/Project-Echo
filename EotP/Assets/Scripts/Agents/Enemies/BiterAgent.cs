using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class BiterAgent : Agent

{

    [SerializeField] private Transform _goal;

    [SerializeField] private float _moveSpeed = 1.5f;
    [SerializeField] private float _rotationSpeed = 180f;
    private Vector2 _acceleration;

    private SpriteRenderer _renderer;
    private Quaternion _initialMouthRotation;

    public int CurrentEpisode = 0;
    public float CumulativeReward = 0f;

 

    public override void Initialize()
    {
        Debug.Log("Initialize()");

        _renderer = GetComponent<SpriteRenderer>();
        CurrentEpisode = 0;
        CumulativeReward = 0f;
        _acceleration = Vector2.zero;
        _initialMouthRotation = transform.rotation;


    }
    void LateUpdate()
    {
        transform.rotation = _initialMouthRotation;
    }

    public override void OnEpisodeBegin()
    {

        Debug.Log("OnEpisodeBegin()");

        CurrentEpisode++;
        CumulativeReward = 0f;
        _renderer.color = Color.blue;

        SpawnObjects();

    }


    private void SpawnObjects()
    {
        transform.localRotation = Quaternion.identity;
        transform.localPosition = Vector2.zero;

        Vector2 goalPosition = new Vector2(Random.Range(-6f, 6f), -1f);
        _goal.localPosition = goalPosition;

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float goalPosX_normalized = _goal.localPosition.x / 5f;
        float goalPosY_normalized = _goal.localPosition.y / 5f;

        float biterPosX_normalized = transform.localPosition.x / 5f;
        float biterPosY_normalized = transform.localPosition.y / 5f;

        float biterRotation_normalized = (transform.localRotation.eulerAngles.z / 360f) * 2f - 1f;

        sensor.AddObservation(goalPosX_normalized);
        sensor.AddObservation(goalPosY_normalized);
        sensor.AddObservation(biterPosX_normalized);
        sensor.AddObservation(biterPosY_normalized);
        sensor.AddObservation(biterRotation_normalized);

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        discreteActionsOut[0] = 0;

        if (Input.GetKey(KeyCode.UpArrow))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            discreteActionsOut[0] = 2;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            discreteActionsOut[0] = 3;
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        MoveAgent(actions.DiscreteActions);

        AddReward(-2f / MaxStep);

        CumulativeReward = GetCumulativeReward();
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var actions = act[0];

        switch (actions)
        {
            case 1:
                transform.position += (transform.up * _moveSpeed * Time.deltaTime);
                break;
            case 2:

                transform.Rotate(0f, -_rotationSpeed * Time.deltaTime, 0f);
                break;
            case 3:

                transform.Rotate(0f, _rotationSpeed * Time.deltaTime, 0f);
                break;

        }

    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Goal"))
        {
            GoalReached();
        }
    }

    private void GoalReached()
    {
        AddReward(1.0f);
        CumulativeReward = GetCumulativeReward();

        EndEpisode();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.05f);

            if (_renderer != null)
            {

                _renderer.color = Color.red;
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.01f * Time.fixedDeltaTime);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            if (_renderer != null)
            {
                _renderer.color = Color.blue;
            }
        }
    }
}
