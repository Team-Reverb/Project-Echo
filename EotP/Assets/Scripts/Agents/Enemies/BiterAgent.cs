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
using Unity.Mathematics;
using Unity.MLAgents.Policies;

public  class State
{
    protected BiterAgent _agent;

    public State(GameObject agent)
    {
        _agent = agent.GetComponent<BiterAgent>();
    }
    

    public virtual void OnEnter() { }

    public virtual void OnUpdate() { }

    public virtual void OnExit() { }
}


public class WanderingState : State
{
    private Vector2 _goal;

    public WanderingState(GameObject agent) : base(agent)
    {

    }

    public override void OnEnter() 
    {
        RandomGoal();
    }

    private void RandomGoal()
    {
        _goal = _agent.PatrolPoints[Random.Range(0, _agent.PatrolPoints.Count)];
    }

    public override void OnUpdate() 
    {
        Debug.Log("R");
        GameObject gameObject = _agent.gameObject;
        Vector2 sub = _goal - new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);

        _agent.gameObject.GetComponent<Rigidbody2D>().AddForce(Vector2.Normalize(sub) * (_agent._moveSpeed / 1.5f )* Time.deltaTime, ForceMode2D.Force);

        if (sub.magnitude < 0.25f) { RandomGoal(); }

        if (_agent._goal != null) { _agent.ChangeState(1); }
    }

    public override void OnExit() 
    {
        _goal = Vector2.zero;
    }
}

public class ChasingState : State
{

    public ChasingState(GameObject agent) : base(agent)
    {

    }

    public override void OnEnter() 
    {

        //  _agent.GetComponent<BehaviorParameters>().Model = _agent.NeuralNetwork;
        _agent.GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.InferenceOnly;
    }

    public override void OnUpdate() 
    { 
        // let the agent handle it
    }

    public override void OnExit() 
    {
        // _agent.GetComponent<BehaviorParameters>().Model = null;
        _agent.GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.HeuristicOnly;
    }
}

public class BiterAgent : Agent

{

    public GameObject _goal;
    [SerializeField] private GameObject _dummyGoal;
    public List<Vector2> noticedPoints;

    public float _moveSpeed = 1.5f;
    [SerializeField] private float _rotationSpeed = 180f;
    public bool DebugMode;
    

    private SpriteRenderer _renderer;
    private Rigidbody2D _rigidBody;
    private Quaternion _initialMouthRotation;

    public GameObject Environment;

    public int CurrentEpisode = 0;
    public float CumulativeReward = 0f;
    public List<Vector2> PatrolPoints;
    public ModelAsset NeuralNetwork;

    private List<State> _states;
    private State _currentState;

    private Vector2 _originalPosition;


 

    public override void Initialize()
    {
        Debug.Log("Initialize");

        _renderer = GetComponent<SpriteRenderer>();
        CurrentEpisode = 0;
        CumulativeReward = 0f;
        _initialMouthRotation = transform.rotation;
      

        _states = new List<State>();
        _states.Add(new WanderingState(gameObject));
        _states.Add(new ChasingState(gameObject));

        if (DebugMode) _currentState = _states[1];
        else _currentState = _states[0];

        _originalPosition = transform.position;
  

        _rigidBody = GetComponent<Rigidbody2D>();


    }

    void Update()
    {
        _currentState.OnUpdate();
    }

    void LateUpdate()
    {
        transform.Find("Mouth").gameObject.transform.rotation = _initialMouthRotation;
    }

    void FixedUpdate()
    {
        
      //  if (Acceleration.magnitude > MaxAcceleration) Acceleration = Acceleration.normalized * MaxAcceleration;
        


    }

    public void Alert(Vector2 alertLocation)
    {
        if (
            _goal != null 
            && (alertLocation - (Vector2)gameObject.transform.position).magnitude > ((Vector2)_goal.transform.position - (Vector2)gameObject.transform.position).magnitude
            )
        {
            return;
        }

        GameObject alertGoal = Instantiate(_dummyGoal,alertLocation, new Quaternion(0,0,0,0));
        _goal = alertGoal;
    }
    

    public void ChangeState(int index)
    {
        _currentState.OnExit();
        _currentState = _states[index];
        _currentState.OnEnter();
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
        transform.localPosition = _originalPosition;

        if (DebugMode == true) RandomizeGoal();

    }

    private void RandomizeGoal()
    {
        Debug.Log("Randomizing");
        float[] vals = { -8.5f, 0, 8.5f };

        float x = vals[Random.Range(0, 3)];
        float y = vals[Random.Range(0, 3)];

        Vector2 goalPosition = new Vector2(Environment.transform.position.x, Environment.transform.position.y) + new Vector2(x, y);

        _goal.transform.position = goalPosition;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (_goal == null)

        {
            for (int i = 0; i < 4; i++) // 4 is the number of observations, we're doing dummy observations so no errors
            {
                sensor.AddObservation(0f);
            }
            return;
        }
        ;
        float goalPosX_normalized = _goal.transform.localPosition.x / 5f;
        float goalPosY_normalized = _goal.transform.localPosition.y / 5f;

        float biterPosX_normalized = transform.localPosition.x / 5f;
        float biterPosY_normalized = transform.localPosition.y / 5f;

        sensor.AddObservation(goalPosX_normalized);
        sensor.AddObservation(goalPosY_normalized);
        sensor.AddObservation(biterPosX_normalized);
        sensor.AddObservation(biterPosY_normalized);
  

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        discreteActionsOut[0] = 0;

        if (!DebugMode) { return; }

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

        float dt = _moveSpeed * Time.deltaTime;

        switch (actions)
        {
            case 1:
                _rigidBody.AddForce(new Vector2(0,1) * dt, ForceMode2D.Force);
                
                break;
            case 2:

                _rigidBody.AddForce(new Vector2(-1,0) * dt, ForceMode2D.Force);

                break;
            case 3:

                _rigidBody.AddForce(new Vector2(1, 0) * dt, ForceMode2D.Force);

                break;
            case 4:
                _rigidBody.AddForce(new Vector2(0, -1) * dt, ForceMode2D.Force);

                break;

        }

    }

    /*
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Goal"))
        {
            GoalReached();
        }
    }
    */
    private void GoalReached()
    {
        AddReward(1.0f);
        CumulativeReward = GetCumulativeReward();

        RandomizeGoal();

        if (!DebugMode) { 
            Destroy(_goal);
            _goal = null;
            ChangeState(0);
        }

        //EndEpisode();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!DebugMode) return;

        if (collision.gameObject.CompareTag("Wall"))
        {
           // Debug.Log("Collision");
            AddReward(-0.05f);

            if (_renderer != null)
            {

                _renderer.color = Color.red;
            }
        }
        else if (collision.gameObject.CompareTag("Goal"))
        {
            GoalReached();
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
