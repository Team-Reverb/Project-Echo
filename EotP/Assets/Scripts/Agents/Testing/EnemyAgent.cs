using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;

public class EnemyAgent : Agent
{

    [SerializeField] private Transform _goal;
    [SerializeField] private GameObject _mirrorer;
    [SerializeField] private float _moveSpeed = 1.5f;
    [SerializeField] private float _rotationSpeed = 180f;

    private SpriteRenderer _renderer;

    public int CurrentEpisode = 0;
    public float CumulativeReward = 0f;


    public override void Initialize()
    {
        Debug.Log("Initialize()");

        _renderer = GetComponent<SpriteRenderer>();
        CurrentEpisode = 0;
        CumulativeReward = 0f;

        
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

        float agentPosX_normalized = transform.localPosition.x / 5f;
        float agentPosY_normalized = transform.localPosition.y / 5f;

        float agentDirectionFacing = _mirrorer.transform.localScale.x; // 1 = right, -1 = left

        sensor.AddObservation(goalPosX_normalized);
        sensor.AddObservation(goalPosY_normalized);
        sensor.AddObservation(agentPosX_normalized);
        sensor.AddObservation(agentPosY_normalized);
        sensor.AddObservation(agentDirectionFacing);

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
                transform.position += (transform.right * _mirrorer.transform.localScale.x) * _moveSpeed * Time.deltaTime;
                break;
            case 2:
                // rotate eye
                _mirrorer.transform.localScale = new Vector3(1,1,1);             
                break;
            case 3:
                // rotate eye other way
                _mirrorer.transform.localScale = new Vector3(-1, 1, 1);
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
