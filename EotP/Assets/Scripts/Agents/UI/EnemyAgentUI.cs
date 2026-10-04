using TMPro;
using UnityEngine;

public class EnemyAgentUI : MonoBehaviour
{

    [SerializeField] private EnemyAgent _enemyAgent;
    [SerializeField] private GameObject _episodeNo;
    [SerializeField] private GameObject _rewardNo;


    private void OnGUI()
    {
        _episodeNo.GetComponent<TMP_Text>().text = "Episode : " + _enemyAgent.CurrentEpisode + " - Step : " + _enemyAgent.StepCount;
        _rewardNo.GetComponent<TMP_Text>().text = "Reward : " + _enemyAgent.CumulativeReward.ToString();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
