using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Collections;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Unity.InferenceEngine;
using Random = UnityEngine.Random;

public  class State
{
    protected BiterAgent _agent;

    public virtual void OnEnter() { }

    public virtual void OnUpdate() { }

    public virtual void OnExit() { }
}


public class WanderingState : State
{
    private Vector2 _goal;
    public override void OnEnter() 
    {
        _agent._acceleration = 0;
        RandomGoal();
    }

    private void RandomGoal()
    {
        _goal = _agent.PatrolPoints[Random.Range(0, _agent.PatrolPoints.Count)];
    }

    public override void OnUpdate() 
    {
        GameObject gameObject = _agent.gameObject;
        Vector2 sub = _goal - new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);
        _agent.FinalAcceleration = Vector2.Normalize(sub) * _agent._moveSpeed;

        if (sub.magnitude < 0.25f) { RandomGoal(); }
    }

    public override void OnExit() 
    {
        _goal = Vector2.zero;
    }
}

public class ChasingState : State
{
    public override void OnEnter() 
    {
        _agent.GetComponent<DecisionRequester>().enabled = true;
    }

    public override void OnUpdate() 
    { 
        // let the agent handle it
    }

    public override void OnExit() 
    {
        _agent.GetComponent<DecisionRequester>().enabled = false;
    }
}

public class BiterAgent : Agent

{

    [SerializeField] private GameObject _goal;

    public float _moveSpeed = 1.5f;
    [SerializeField] private float _rotationSpeed = 180f;
    

    private SpriteRenderer _renderer;
    private Rigidbody2D _rigidBody;
    private Quaternion _initialMouthRotation;

    public int CurrentEpisode = 0;
    public float CumulativeReward = 0f;
    public List<Vector2> PatrolPoints;
    public float _acceleration;
    [HideInInspector] public Vector2 FinalAcceleration;
    public float MaxAcceleration;
    public ModelAsset NeuralNetwork;

    private List<State> _states;
    private State _currentState;


 

    public override void Initialize()
    {
        Debug.Log("Initialize");

        _renderer = GetComponent<SpriteRenderer>();
        CurrentEpisode = 0;
        CumulativeReward = 0f;
        _acceleration = 0;
        _initialMouthRotation = transform.rotation;

        _states = new List<State>();
        _states.Add(new WanderingState());
        _states.Add(new ChasingState());
        _currentState = _states[1];

        _rigidBody = GetComponent<Rigidbody2D>();


    }
    void LateUpdate()
    {
        transform.Find("Mouth").gameObject.transform.rotation = _initialMouthRotation;
    }

    void FixedUpdate()
    {
        // acceleration being 0 means wander state is active
        if (_acceleration > 0 ) FinalAcceleration = transform.up * _acceleration;
        _rigidBody.AddForce(FinalAcceleration, ForceMode2D.Force);
    }
    

    private void ChangeState(int index)
    {
        _currentState = _states[index];
    }

    public override void OnEpisodeBegin()
    {

        Debug.Log("OnEpisodeBegin");

        CurrentEpisode++;
        CumulativeReward = 0f;
        _renderer.color = Color.gray;

        SpawnObjects();

    }


    private void SpawnObjects()
    {
        transform.localRotation = Quaternion.identity;
        transform.localPosition = Vector2.zero;

        RandomizeGoal();

    }

    private void RandomizeGoal()
    {
        int[] vals = { -7, 0, 7 };

        int x = vals[Random.Range(0, 2)];
        int y = vals[Random.Range(0, 2)];

        Vector2 goalPosition = new Vector2(x, y);

        _goal.transform.localPosition = goalPosition;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float goalPosX_normalized = _goal.transform.localPosition.x / 5f;
        float goalPosY_normalized = _goal.transform.localPosition.y / 5f;

        float biterPosX_normalized = transform.localPosition.x / 5f;
        float biterPosY_normalized = transform.localPosition.y / 5f;

        float biterRotation_normalized = (transform.localRotation.eulerAngles.z / 360f) * 2f - 1f;

        float acceleration_normalized = _acceleration / MaxAcceleration;

        sensor.AddObservation(goalPosX_normalized);
        sensor.AddObservation(goalPosY_normalized);
        sensor.AddObservation(biterPosX_normalized);
        sensor.AddObservation(biterPosY_normalized);
        sensor.AddObservation(acceleration_normalized);
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
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            discreteActionsOut[0] = 4;
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
                float newAccel = _moveSpeed * Time.deltaTime;
                if (_acceleration + newAccel < MaxAcceleration) _acceleration += newAccel;
                break;
            case 2:

                transform.Rotate(0f, 0f, -_rotationSpeed * Time.deltaTime);
                break;
            case 3:

                transform.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime);
                break;
            case 4:
                float newBackAccel = _moveSpeed * Time.deltaTime;
                if (_acceleration - newBackAccel > 0) _acceleration -= newBackAccel;
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

        RandomizeGoal();

        //EndEpisode();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Debug.Log("Collision");
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
                _renderer.color = Color.grey;
            }
        }
    }
}
