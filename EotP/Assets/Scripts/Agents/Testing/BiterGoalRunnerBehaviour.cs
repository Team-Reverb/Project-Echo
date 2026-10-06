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
        int[] vals = {-7, 0, 7 };

        int x = vals[Random.Range(0, 2)];
        int y = vals[Random.Range(0, 2)];

        return new Vector2(x, y);
    }
}
