using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    float maxSpeed = 7;
    [SerializeField] float attackRange = 10;
    [SerializeField] Transform cameraParent;
    [SerializeField] Animator anim;
    [SerializeField] Transform body;
    [SerializeField] CinemachineImpulseSource cameraInpulse;

    Enemy lookingTarget, target = null;

    List<Enemy> enemies;

    bool inCombo;
    bool countered;     

    //State Machine
    private enum States {Free, Punch, Hit, Counter };
    private States currentState = States.Free;

    string[] closeAttackAnimations = {
        "Punch 1", "Ground Kick 1", "Ground Kick 2"};
    string[] longAttackAnimations = {        
        "Air Kick 1", "Air Kick 2" , "Air Kick 3", "Air Kick 4", "Air Punch 1"};
    
    // Start is called before the first frame update
    void Start()
    {
        enemies = EnemyAI.Instance.enemies;
        lookingTarget = enemies[Random.Range(0, enemies.Count)];
        lookingTarget.SetLayer(3);
    }

    void Update()
    {
        enemies = EnemyAI.Instance.enemies;
        UpdateLookingTarget();
        FSMUpdate();
    }

    void UpdateLookingTarget()
    {
        Enemy cameraTarget = GetTargetEnemy();
        
        if (cameraTarget && cameraTarget != lookingTarget)
        {
            lookingTarget.SetLayer(0);
            lookingTarget = cameraTarget;
            lookingTarget.SetLayer(3);
        }
    }

    Enemy GetTargetEnemy()
    {
        Enemy enemy = null;
        var bestScore = 0f;
        
        foreach(Enemy e in enemies)
        {
            var dist = Vector3.Distance(e.transform.position, transform.position);
            if (dist < attackRange)
            {
                var enemyDir = e.transform.position - transform.position;
                var angle = Vector3.Angle(Camera.main.transform.forward, enemyDir);
                var score = 2 / dist + 90 / angle;
                if (score > bestScore)
                {
                    bestScore = score;
                    enemy = e;
                }
            }
        }
        return enemy;
    }

    #region State Machine
    void FSMUpdate()
    {        
        switch (currentState)
        {
            case States.Free: Free(); break;
            case States.Punch: Punch();  break;
            case States.Hit: break;
            case States.Counter: break;
        }
    }

    void GoToState(States newState)
    {
        // On State Exit
        switch(currentState)
        {
            case States.Free: ExitFree(); break;
            case States.Punch: break;
            case States.Hit: break;
            case States.Counter: break;
        }
        currentState = newState;

        // On State Enter
        switch (newState)
        {
            case States.Free: EnterFree(); break;
            case States.Punch: StartCoroutine(EnterPunch()); break;
            case States.Hit: StartCoroutine(EnterHit()); break;
            case States.Counter: StartCoroutine(EnterCounter()); break;
        }
    }

    #region On Enter

    void EnterFree()
    {
        anim.SetFloat("Speed", rb.velocity.magnitude);
    }    

    IEnumerator EnterPunch()
    {
        var duration = 0.6f;
        var animation = "Punch 1";
        
        target = GetTargetEnemy();
        if(target)
        {
            if (Vector3.Distance(target.transform.position, transform.position) > 3)
                animation = longAttackAnimations[Random.Range(0, longAttackAnimations.Length)];
            else
                animation = closeAttackAnimations[Random.Range(0, closeAttackAnimations.Length)];

            target.GoToState(Enemy.States.Hit);

            transform.DOMove(target.transform.position + target.transform.forward, duration);
            transform.DOLookAt(target.transform.position, duration / 2);
        }
        anim.SetTrigger(animation);
        yield return new WaitForSeconds(duration);
        if (currentState == States.Punch) GoToState(States.Free);
    }

    IEnumerator EnterCounter()
    {
        DOTween.Pause(transform);
        DOTween.Kill(transform);
        var animation = "Punch 1";
        if(target != null) target.GoToState(Enemy.States.Idle);

        float duration = 0.6f;
        // Get enemy to counter
        target = null;
        if (enemies.Count > 0)
        {
            var inRange = enemies.FindAll(e => Vector3.Distance(e.transform.position, transform.position) < attackRange);
            if (inRange.Count > 0)
            {
                // Get enemy that is attacking
                foreach (Enemy e in inRange)
                {
                    if (e.currentState == Enemy.States.Punch)
                    {
                        target = e;
                        target.GoToState(Enemy.States.Hit);
                        break;
                    }
                }
                // Get animation based on distance to target
                if(target != null)
                {
                    if (Vector3.Distance(target.transform.position, transform.position) < 3)
                        animation = longAttackAnimations[Random.Range(0, longAttackAnimations.Length)];
                    else
                        animation = closeAttackAnimations[Random.Range(0, closeAttackAnimations.Length)];
                    transform.DOMove(target.transform.position + target.transform.forward, duration);
                    transform.DOLookAt(target.transform.position, duration/2);
                }
            }
        }
        anim.SetTrigger(animation);
        yield return new WaitForSeconds(duration);
        if(currentState == States.Counter) GoToState(States.Free);
    }

    IEnumerator EnterHit()
    {        
        anim.SetTrigger("Hit");
        DOTween.KillAll(transform);
        yield return new WaitForSeconds(0.6f);
        GoToState(States.Free);
    }

    #endregion

    #region Update
    void Free()
    {        
        var cameraT = Camera.main.transform;
        var vel = rb.velocity;
        vel = Vector3.Lerp(vel, Vector3.zero, 0.2f);

        var forward = cameraT.forward;
        var right = cameraT.right;
        forward.y = 0;
        right.y = 0;        

        vel += Input.GetAxis("Vertical") * 100 * Time.deltaTime * forward.normalized + Input.GetAxis("Horizontal") * 100 * Time.deltaTime * right.normalized;
        vel = Vector3.ClampMagnitude(vel, maxSpeed);
        rb.velocity = vel;
        anim.SetFloat("Speed", vel.magnitude);
        if (vel.sqrMagnitude > 0.1f)
        {
            anim.speed = Mathf.Max(1, vel.magnitude / (0.65f * maxSpeed));
            body.forward = vel.normalized;
        }

        if (Input.GetMouseButtonDown(0))
        {
            GoToState(States.Punch); 
            return;
        }
        if (Input.GetMouseButtonDown(1))
        {
            GoToState(States.Counter);
            return;
        }
    }

    void Punch()
    {
        if (Input.GetMouseButtonDown(1))
        {
            // Check if there's an enemy attacking
            if (enemies.Count > 0)
            {
                var inRange = enemies.FindAll(e => Vector3.Distance(e.transform.position, transform.position) < attackRange);
                if (inRange.Count > 0)
                {
                    foreach (Enemy e in inRange)
                    {
                        if (e.currentState == Enemy.States.Punch)
                        {
                            GoToState(States.Counter);
                            return;
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region On Exit

    void ExitFree()
    {
        rb.velocity = Vector3.zero;
        anim.SetFloat("Speed", 0);
    }

    #endregion

    void HitTarget()
    {
        if (target != null)
        {
            cameraInpulse.GenerateImpulseWithForce(0.2f);
            target.OnHit();
            GameManager.Instance.AddScore();
            target = null;
        }
        else
            GameManager.Instance.CancelCombo();
    }
    public void OnHit()
    {
        if (countered)
        {
            countered = false;
            return;
        }
        GoToState(States.Hit);
    }

    #endregion    

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        var camForward = Camera.main.transform.forward;
        camForward.y = 0;
        camForward.Normalize();
        camForward *= 3;
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + camForward);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;       
        if(target != null) Gizmos.DrawSphere(target.transform.position + Vector3.up, 0.3f);
    }    
}
