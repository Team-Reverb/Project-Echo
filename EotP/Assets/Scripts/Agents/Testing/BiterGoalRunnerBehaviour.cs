using NUnit.Framework;
using UnityEngine;

public class BiterGoalRunnerBehaviour : MonoBehaviour
{

    public float Speed;

    private Vector2 _acceleration;
    private Vector2 _goal;
    private Rigidbody2D _rigidBody;

    void Start()
    {
        _acceleration = Vector2.zero;
        _goal = RandomGoal();
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {

        Vector2 sub = _goal - new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);
        _acceleration = Vector2.Normalize(sub) * Speed;

        if (sub.magnitude < 0.25f) { _goal = RandomGoal(); }
    }

    void FixedUpdate()
    {
        _rigidBody.AddForce(_acceleration, ForceMode2D.Force);
    }

    private Vector2 RandomGoal()
    {
        int[] yvals = {-8, 0, 8 };
        float[] xvals = { -8.5f, 0, 8.5f };

        float x = xvals[Random.Range(0, 2)];
        int y = yvals[Random.Range(0, 2)];

        return new Vector2(x, y);
    }
}
