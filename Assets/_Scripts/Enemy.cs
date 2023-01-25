using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour
{

    [SerializeField] Animator anim;
    [SerializeField] Rigidbody rb;
    int life = 3;
    float range = 6;
    bool onHit = false;

    [SerializeField] Player target;
    [SerializeField] ParticleSystem counterParticle, hitParticle;
    [SerializeField] AudioSource audio;
    [SerializeField] GameObject character;


    public enum States {Idle, Walk, Circle, Punch, Hit, Death};
    public States currentState { get; private set; }

    public Vector3 walkTo;

    // Debug
    [SerializeField] TextMeshPro state;


    void Start()
    {
        counterParticle.Clear();
        counterParticle.Stop();
        //yield return new WaitUntil(() => target != null);
        GoToState(States.Idle);
    }    

    private void Update()
    {
        UpdateFSM();       
    }

    #region State Machine

    void UpdateFSM()
    {
        switch(currentState)
        {
            case States.Idle: Idle(); break;
            case States.Walk: break;
            case States.Circle: break;
            case States.Punch: break;
            case States.Hit: break;
            case States.Death: break;
        }
    }

    public void GoToState(States nextState)
    {
        //state.text = nextState.ToString();
        // On State Exit
        switch (currentState)
        {
            case States.Idle: break;
            case States.Walk: break;
            case States.Circle: break;
            case States.Punch: OnExitPunch(); break;
            case States.Hit: break;
        }        
        currentState = nextState;
        // On State Enter
        switch (currentState)
        {
            case States.Idle: StartCoroutine(OnEnterIdle()); break;
            case States.Walk: StartCoroutine(OnEnterWalk()); break;
            case States.Circle: break;
            case States.Punch: StartCoroutine(OnEnterPunch()); break;
            case States.Hit: StartCoroutine(OnEnterHit()); break;
            case States.Death: OnEnterDeath(); break;
        }
    }

    #region OnEnter

    IEnumerator OnEnterIdle()
    {
        anim.SetFloat("Speed", 0);
        rb.velocity = Vector3.zero;
        while(currentState == States.Idle)
        {
            yield return new WaitForSeconds(Random.value * 2);
            if (Vector3.Distance(target.transform.position, transform.position) > range)
            {
                walkTo = target.transform.position + (transform.position - target.transform.position).normalized * Random.Range(range * 0.85f, range);
                // Other approaches

                //walkTo = target.transform.position + target.transform.rotation * new Vector3(5 * Random.Range(-1, 1), 0, 5 * Random.value);

                //walkTo = target.transform.position + Random.onUnitSphere * 5;
                //walkTo.y = 0;

                GoToState(States.Walk);
            }
        }
    }

    IEnumerator OnEnterWalk()
    {
        while (Vector3.Distance(walkTo, transform.position) > 0.1f)
        {
            if (currentState != States.Walk) yield break;
            rb.velocity = (walkTo - transform.position).normalized * 6;
            anim.SetFloat("Speed", rb.velocity.magnitude);
            rb.transform.LookAt(walkTo);
            yield return null;
        }
        if (currentState == States.Walk) GoToState(States.Idle);
    }

    IEnumerator OnEnterPunch()
    {
        counterParticle.Play();
        //Time.timeScale = 0.75f;
        anim.SetFloat("Speed", 0);
        rb.velocity = Vector3.zero;
        
        anim.SetTrigger("Punch 1");
        yield return new WaitForSeconds(2f);
        if (currentState == States.Punch) GoToState(States.Idle);
        
    }

    IEnumerator OnEnterHit()
    {        
        anim.SetFloat("Speed", 0);
        anim.SetTrigger("Idle");
        rb.velocity = Vector3.zero;
        rb.transform.LookAt(target.transform);
        if (life == 1 && EnemyAI.Instance.enemies.Count == 1)
            GameManager.Instance.StartCutscene();

        yield return new WaitUntil(() => onHit);
        audio.clip = AudioManager.Instance.GetRandomPunch();
        audio.Play();
        hitParticle.Play();
        onHit = false;
        life--;
        if (life <= 0)
        {
            GoToState(States.Death);
            yield break;
        }
        anim.SetTrigger("Hit");
        yield return new WaitForSeconds(0.6f);
        if (currentState == States.Hit) GoToState(States.Idle);
    }

    void OnEnterDeath()
    {
        EnemyAI.Instance.enemies.Remove(this);
        anim.SetTrigger("Death");
        SetLayer(0);
        enabled = false;
    }

    #endregion

    #region Update
    void Idle()
    {        
        rb.transform.LookAt(target.transform);
    }
    #endregion

    #region On Exit
    void OnExitPunch()
    {
        counterParticle.Clear();
        counterParticle.Stop();
        //Time.timeScale = 1;
    }

    #endregion

    void HitTarget()
    {
        if(Vector3.Distance(target.transform.position, transform.position) < 3)
            target.OnHit();
    }

    public void OnHit() => onHit = true;

    #endregion

    public void SetLayer(int layer) => character.layer = layer;
}
