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

    private Renderer _renderer;

    private int _currentEpisode = 0;
    private float _cumulativeReward = 0f;


    public override void Initialize()
    {
        Debug.Log("Initialize()");

        _renderer = GetComponent<Renderer>();
        _currentEpisode = 0;
        _cumulativeReward = 0f;

        
    }

    public override void OnEpisodeBegin()
    {
        
        Debug.Log("OnEpisodeBegin()");

        _currentEpisode++;
        _cumulativeReward = 0f;
        _renderer.material.color = Color.blue;

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

    public override void OnActionReceived(ActionBuffers actions)
    {
        MoveAgent(actions.DiscreteActions);

        AddReward(-2f / MaxStep);

        _cumulativeReward = GetCumulativeReward();
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

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Goal"))
        {
            GoalReached();
        }
    }

    private void GoalReached()
    {
        AddReward(1.0f);
        _cumulativeReward = GetCumulativeReward();

        EndEpisode();
    }
}
