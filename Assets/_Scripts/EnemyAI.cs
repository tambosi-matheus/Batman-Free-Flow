using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public static EnemyAI Instance { get; private set; }
    public List<Enemy> enemies = new List<Enemy>();
    public Enemy enemyPunching = null;
    [SerializeField] Player player;

    private void Awake()
    {
        Instance = this;
    }

    IEnumerator Start()
    {
        foreach(GameObject e in GameObject.FindGameObjectsWithTag("Enemy"))        
            enemies.Add(e.GetComponent<Enemy>());
        yield return new WaitUntil(() => player != null);
        StartCoroutine(AIUpdate());
    }

    IEnumerator AIUpdate()
    {
        while (true)
        {
            // Get a new enemy
            enemyPunching = null;
            while (enemyPunching == null)
            {
                if (enemies.Count == 0) break;
                var enemy = enemies[Random.Range(0, enemies.Count)];
                if (enemy.currentState != Enemy.States.Hit)
                    enemyPunching = enemy;
                else if (enemies.Count == 1)
                    yield return new WaitForSeconds(0.5f);
            }
            if (enemies.Count == 0) break;
            enemyPunching.walkTo = player.transform.position - enemyPunching.transform.forward;
            enemyPunching.GoToState(Enemy.States.Walk);
            yield return new WaitUntil(() => enemyPunching.currentState != Enemy.States.Walk);
            enemyPunching.GoToState(Enemy.States.Punch);
            yield return new WaitUntil(() => enemyPunching.currentState != Enemy.States.Punch);
            yield return new WaitForSeconds(Random.value * 3);
        }
    }
}
