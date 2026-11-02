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
using System.Runtime.CompilerServices;

public class HunterAgent : Agent
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private SpriteRenderer _renderer;
    private Rigidbody2D _rigidBody;

    private GameObject _environment;

    private bool _playerRevealed;
    public int CurrentEpisode = 0;
    public float CumulativeReward = 0f;

    public float _moveSpeed = 1.5f;

    public GameObject Player;


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void Initialize()
    {
        Debug.Log("Initialize()");

        _renderer = GetComponent<SpriteRenderer>();
        _rigidBody = GetComponent<Rigidbody2D>();
        CurrentEpisode = 0;
        CumulativeReward = 0f;


    }

    public override void OnEpisodeBegin()
    {

        Debug.Log("OnEpisodeBegin()");

        CurrentEpisode++;
        CumulativeReward = 0f;
        _renderer.color = Color.blue;

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float agentPosX_normalized = transform.localPosition.x / 5f;
        float agentPosY_normalized = transform.localPosition.y / 5f;
        float playerPosX_normalized = (Player.transform.position - _environment.transform.position).x / 5f; // relative
        float playerPosY_normalized = (Player.transform.position - _environment.transform.position).y / 5f; // relative
        float playerPulseCooldown_normalized = Player.GetComponent<PlayerController>().PulseCooldownNormalized;
        _playerRevealed = IsRevealed();

        sensor.AddObservation(agentPosX_normalized);
        sensor.AddObservation(agentPosY_normalized);
        sensor.AddObservation(playerPosX_normalized);
        sensor.AddObservation(playerPosY_normalized);
        sensor.AddObservation(playerPulseCooldown_normalized);
        sensor.AddObservation(_playerRevealed);
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

        if (Vector3.Magnitude(gameObject.transform.position -Player.gameObject.transform.position) > 2f)
        {
            AddReward(-2f / MaxStep);
        }

        if (_playerRevealed)
        {
            // wip
        }

        CumulativeReward = GetCumulativeReward();
    }

    bool IsRevealed()
    {

        Vector3 direction = (Player.gameObject.transform.position - gameObject.transform.position);
        float distance = direction.magnitude;
        direction = direction.normalized;

        RaycastHit hit;
        if (
            Physics.Raycast(gameObject.transform.position, direction, out hit, Mathf.Infinity, LayerMask.GetMask("Wall", "Character"))
            ||
            distance > Player.GetComponent<PlayerController>().echoPulsePrefab.GetComponent<EchoPulse>().maxRadius
           )
        {
            return false;
        }


        return true;

    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var actions = act[0];

        float dt = _moveSpeed * Time.deltaTime;

        switch (actions)
        {
            case 1:
                _rigidBody.AddForce(new Vector2(0, 1) * dt, ForceMode2D.Force);

                break;
            case 2:

                _rigidBody.AddForce(new Vector2(-1, 0) * dt, ForceMode2D.Force);

                break;
            case 3:

                _rigidBody.AddForce(new Vector2(1, 0) * dt, ForceMode2D.Force);

                break;
            case 4:
                _rigidBody.AddForce(new Vector2(0, -1) * dt, ForceMode2D.Force);

                break;

        }

    }
}
