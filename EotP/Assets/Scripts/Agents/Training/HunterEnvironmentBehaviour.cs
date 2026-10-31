using UnityEngine;
using Random = UnityEngine.Random;

public class HunterEnvironmentBehaviour : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject Platform;
    void Start()
    {
        GenerateEnvironment();
    }

    void GenerateEnvironment()
    {
        

        for (int i = 0; i < 3; i++)
        {
            float x = -20 + (i * 20);

            int rand1 = Random.Range(3, 6);

            for (int g = 0; g < rand1; g++)
            {
                float sizeOffset = Random.Range(1f, 1.8f);
                float posOffsetX = Random.Range(-2f, 2f);
                float posOffsetY = Random.Range(0f, 2f);

                GameObject platform = Instantiate(Platform,gameObject.transform.Find("Walls").transform);
                float y = -14 + ((28 / rand1) * g);
                platform.transform.position = new Vector2(x, y) + new Vector2(posOffsetX, posOffsetY);
                platform.transform.localScale = new Vector2(platform.transform.localScale.x * sizeOffset, 1);


            }
        }
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
