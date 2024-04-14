using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("# Movement Values")]
    public float moveInput;
    public float moveSpeed;
    public float jumpPower;
    public float dashPower;
    public float knockBackPower;

    [Header("# Delay Check")]
    public float lastJump;
    public float jumpDelay;
    public float lastDash;
    public float dashDelay;
    public float lastAttack;
    public float attackDelay;
    public float powerAttackCharge;
    public float powerAttackFullCharge;
    public float lastHit;

    [Header("# Skill Costs")]
    public float guardCost;
    public float dashCost;
    public float powerAttackCost;

    [Header("# State Check")]
    public bool onFloor;
    public bool landing;
    public bool[] jumping;
    public bool guarding;
    public bool dashing;
    public bool attackCombo;
    public bool attacking;
    public bool powerAttacking;
    public bool flipCheck;
    public bool stuned;
    public bool invincibility;
    public bool die;
    public bool portal;
    public Vector2 playerPosition;

    [Header("# UI")]
    public Image blackBG;

    Rigidbody2D rigid;
    CapsuleCollider2D coll;
    SpriteRenderer spriter;
    Animator anim;

    public static PlayerController instance;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        coll = GetComponent<CapsuleCollider2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        if (PlayerController.instance == null)
            PlayerController.instance = this;
    }

    void Update()
    {
        Landing();
        KeyInput();
        FlipCheck();

        playerPosition = new Vector2(rigid.transform.position.x, rigid.transform.position.y);

        if (rigid.velocity.y < -20f)
            rigid.velocity = new Vector2(rigid.velocity.x, -20f);

        if (landing)
            Physics2D.IgnoreLayerCollision(3, 6, false);
        else
            Physics2D.IgnoreLayerCollision(3, 6, true);

        if (dashing)
            rigid.velocity = new Vector2(rigid.velocity.x, 0f);

        if (dashing)
            Physics2D.IgnoreLayerCollision(3, 7, true);
        else
            Physics2D.IgnoreLayerCollision(3, 7, false);

        if (powerAttacking)
        {
            moveSpeed = 5f;
            PlayerStatus.instance.currentStamina -= Time.deltaTime * powerAttackCost;
        }
        else
            moveSpeed = 10f;

        if (guarding)
            PlayerStatus.instance.currentStamina -= Time.deltaTime * guardCost;
    }

    void FixedUpdate()
    {
        Move();
    }

    void LateUpdate()
    {
        anim.SetFloat("Speed", Mathf.Abs(moveInput));

        Timer();
        FlipPlayer();

        if (PlayerStatus.instance.currentHealth <= 0)
            Dead();
    }

    void KeyInput()
    {
        if (portal || die)
        {
            moveInput = 0;
            return;
        }   

        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
            Jump();

        if (Input.GetButtonDown("Fire1"))
            Attack();

        if (Input.GetButton("Fire1"))
            PowerAttack();

        if (Input.GetButtonUp("Fire1"))
            PowerAttackExit();

        if (Input.GetButton("Fire2"))
            GuardUp();

        if (Input.GetButtonUp("Fire2"))
            GuardDown();

        if (Input.GetButtonDown("Fire3"))
            Dash();
    }

    void Move()
    {
        if (attacking || dashing || guarding || stuned)
            return;

        rigid.velocity = new Vector2(moveInput * moveSpeed * (PlayerStatus.instance.increaseSpeed + PlayerStatus.instance.enforceSpeed), rigid.velocity.y);
    }

    void Jump()
    {
        if (attacking || powerAttacking || dashing || guarding || stuned || lastJump < jumpDelay || GameManager.instance.paused || GameManager.instance.levelUp || GameManager.instance.enforce || GameManager.instance.delay < 0.1f)
            return;

        lastJump = 0f;

        if (Input.GetButton("Down"))
        {
            if (landing && !onFloor)
                DownJump();

            return;
        }

        if (!jumping[0])
        {
            SoundEffect.instance.Jump();
            jumping[0] = true;
            anim.SetBool("Jump0", true);
            rigid.velocity = Vector2.zero;
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        }
        else if (!jumping[1])
        {
            SoundEffect.instance.Jump();
            jumping[1] = true;
            anim.SetBool("Jump1", true);
            rigid.velocity = Vector2.zero;
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        }
    }

    void DownJump()
    {
        CancelInvoke("CancelDownJump");
        SoundEffect.instance.Jump();
        coll.isTrigger = true;
        rigid.velocity = Vector2.zero;
        rigid.AddForce(Vector2.down * 5f, ForceMode2D.Impulse);
        Invoke("CancelDownJump", 0.5f);
    }

    void CancelDownJump()
    {
        coll.isTrigger = false;
    }

    void Landing()
    {
        if (RaycastingL.instance.raycasting || RaycastingR.instance.raycasting)
        {
            if (!landing)
                landing = true;

            if (Mathf.Abs(rigid.velocity.y) < 0.2f)
            {
                anim.SetBool("Jump0", false);
                anim.SetBool("Jump1", false);
                jumping[0] = false;
                jumping[1] = false;
            }
        }
        else
        {
            if (landing)
                landing = false;

            anim.SetBool("Jump0", true);
            jumping[0] = true;
        }
    }

    void Attack()
    {
        if (guarding || dashing || stuned || GameManager.instance.paused || GameManager.instance.levelUp || GameManager.instance.enforce || GameManager.instance.delay < 0.1f)
            return;

        if (lastAttack > attackDelay)
        {
            lastAttack = 0f;
            attacking = true;
            anim.SetBool("Attack", true);
            PlayerAttack.instance.AttackOn();
            SoundEffect.instance.Attack();

            if (!attackCombo)
            {
                attackCombo = true;
                anim.SetTrigger("Attack0");
            }
            else
            {
                attackCombo = false;
                anim.SetTrigger("Attack1");
            }
        }
    }

    void PowerAttack()
    {
        if (dashing || guarding || stuned || GameManager.instance.paused || GameManager.instance.levelUp || GameManager.instance.enforce || GameManager.instance.delay < 0.1f)
            return;

        powerAttackCharge += Time.deltaTime;

        if (powerAttackCharge > powerAttackFullCharge && PlayerStatus.instance.currentStamina > powerAttackCost)
        {
            powerAttacking = true;
            anim.SetBool("PowerAttack", true);
        }

        if (PlayerStatus.instance.currentStamina <= 0f)
        {
            PowerAttackExit();
            return;
        }
    }

    void PowerAttackExit()
    {
        powerAttackCharge = 0;
        powerAttacking = false;
        anim.SetBool("PowerAttack", false);
    }

    void GuardUp()
    {
        if (dashing || powerAttacking || attacking || stuned || GameManager.instance.paused || GameManager.instance.levelUp || GameManager.instance.enforce || GameManager.instance.delay < 0.1f)
            return;

        if (PlayerStatus.instance.currentStamina > guardCost)
        {
            if (PlayerStatus.instance.currentStamina > 0f)
            {
                attacking = false;
                guarding = true;
                anim.SetBool("Guard", true);
            }
        }
        
        if (PlayerStatus.instance.currentStamina <= 0f)
        {
            GuardDown();
        }
    }

    void GuardDown()
    {
        guarding = false;
        anim.SetBool("Guard", false);
    }

    void Dash()
    {
        if (dashing || guarding || powerAttacking || stuned || GameManager.instance.paused || GameManager.instance.levelUp || GameManager.instance.enforce || GameManager.instance.delay < 0.1f)
            return;

        if (lastDash > dashDelay && PlayerStatus.instance.currentStamina >= dashCost)
        {
            SoundEffect.instance.Dash();
            PlayerStatus.instance.currentStamina -= dashCost;
            attacking = false;
            dashing = true;
            lastDash = 0;
            rigid.velocity = Vector2.zero;

            if (spriter.flipX)
                rigid.AddForce(Vector2.right * dashPower * (-1), ForceMode2D.Impulse);
            else
                rigid.AddForce(Vector2.right * dashPower, ForceMode2D.Impulse);

            anim.SetTrigger("Dash");

            Invoke("DashExit", 0.5f);
        }
    }
    
    void DashExit()
    {
        rigid.velocity = Vector2.zero;
        dashing = false;
    }

    void Timer()
    {
        lastJump += Time.deltaTime;
        lastDash += Time.deltaTime;
        lastAttack += Time.deltaTime;
        lastHit += Time.deltaTime;

        if (lastAttack > 1.5f)
            attackCombo = false;

        if (lastAttack > attackDelay + 0.125f)
        {
            attacking = false;
            anim.SetBool("Attack", false);
        }

        if (lastHit > 0.5f)
            stuned = false;

        if (lastHit > 2f)
            invincibility = false;

        if (!guarding && !dashing && !attacking && !powerAttacking)
            PlayerStatus.instance.currentStamina += Time.deltaTime;
    }

    void FlipPlayer()
    {
        if (portal || die || dashing || guarding || attacking || stuned)
            return;

        if (moveInput > 0)
            spriter.flipX = false;
        else if (moveInput < 0)
            spriter.flipX = true;
    }

    void FlipCheck()
    {
        if (spriter.flipX)
            flipCheck = true;
        else
            flipCheck = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
            onFloor = true;

        if (collision.gameObject.CompareTag("Enemy") && !stuned && !invincibility && !die)
        {
            if (guarding && ((spriter.flipX && gameObject.transform.position.x > collision.transform.position.x) || (!spriter.flipX && gameObject.transform.position.x < collision.transform.position.x)))
                return;

            lastHit = 0;
            SoundEffect.instance.Hit();
            anim.SetTrigger("Hit");
            stuned = true;
            invincibility = true;
            rigid.velocity = Vector2.zero;

            if (collision.transform.position.x > rigid.transform.position.x)
                rigid.AddForce(Vector2.left * knockBackPower, ForceMode2D.Impulse);
            else
                rigid.AddForce(Vector2.right * knockBackPower, ForceMode2D.Impulse);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
            onFloor = false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if ((collision.gameObject.CompareTag("EnemyAttack") && !stuned && !invincibility && !die))
        {
            if (guarding)
            {
                if ((collision.gameObject.transform.position.x > rigid.transform.position.x && !spriter.flipX) || collision.gameObject.transform.position.x < rigid.transform.position.x && spriter.flipX)
                {
                    SoundEffect.instance.Guard();
                    return;
                }   
            }

            anim.SetTrigger("Hit");
            lastHit = 0;
            stuned = true;
            invincibility = true;
            rigid.velocity = Vector2.zero;

            if (collision.gameObject.transform.position.x > rigid.transform.position.x)
                rigid.AddForce(Vector2.left * knockBackPower, ForceMode2D.Impulse);
            else
                rigid.AddForce(Vector2.right * knockBackPower, ForceMode2D.Impulse);
        }
            
    }

    void Dead()
    {
        if (!die)
        {
            GameManager.instance.Save();
            SoundEffect.instance.Die();
            Physics2D.IgnoreLayerCollision(3, 7, true);
            die = true;
            anim.SetBool("Death", true);
            anim.SetTrigger("Die");
            GameManager.instance.levelUp = false;
            Time.timeScale = 1;
            StartCoroutine(FadeOutDead());
        }
    }

    IEnumerator FadeOutDead()
    {
        yield return new WaitForSecondsRealtime(2f);

        Color alpha = blackBG.color;
        float time = 0f;

        while (alpha.a < 1f)
        {
            time += Time.deltaTime;
            alpha.a = Mathf.Lerp(0, 1, time);
            blackBG.color = alpha;

            yield return null;
        }

        time = 0;
        Revive();

        yield return new WaitForSecondsRealtime(1f);

        while (alpha.a > 0f)
        {
            time += Time.deltaTime;
            alpha.a = Mathf.Lerp(1, 0, time);
            blackBG.color = alpha;

            yield return null;
        }

        die = false;
        GameManager.instance.Save();

        yield return null;
    }

    void Revive()
    {
        gameObject.transform.position = new Vector3(0, -2, 0);
        anim.SetBool("Death", false);
        GameManager.instance.leftMonster = 0;
        GameManager.instance.floor = 0;
        PlayerStatus.instance.exp = 0;
        PlayerStatus.instance.level = 0;
        PlayerStatus.instance.increaseAttack = 1;
        PlayerStatus.instance.increasePowerAttack = 1;
        PlayerStatus.instance.increaseSpeed = 1;
        PlayerStatus.instance.increaseHealth = 1;
        PlayerStatus.instance.increaseStamina = 1;
        PlayerStatus.instance.currentHealth = PlayerStatus.instance.maxHealth;
        PlayerStatus.instance.currentStamina = PlayerStatus.instance.maxStamina;
        GameManager.instance.levelUp = false;
        guarding = false;
        attacking = false;
        powerAttacking = false;
        dashing = false;
        stuned = false;
        Physics2D.IgnoreLayerCollision(3, 7, false);

        for (int index = 0; index < GameManager.instance.dungeonMaps.Length; index++)
        {
            GameManager.instance.dungeonMaps[index].SetActive(false);
        }
        
        attacking = false;
        powerAttacking = false;
        guarding = false;
        dashing = false;
        Time.timeScale = 1;
    }

    public void Fade(Vector3 target)
    {
        StartCoroutine(FadeOut(target));
    }

    IEnumerator FadeOut(Vector3 target)
    {
        portal = true;
        Color alpha = blackBG.color;
        float time = 0f;

        while (alpha.a < 1f)
        {
            time += Time.deltaTime;
            alpha.a = Mathf.Lerp(0, 1, time);
            blackBG.color = alpha;

            yield return null;
        }

        time = 0;
        Teleport(target);

        yield return new WaitForSecondsRealtime(1f);

        while (alpha.a > 0f)
        {
            time += Time.deltaTime;
            alpha.a = Mathf.Lerp(1, 0, time);
            blackBG.color = alpha;

            yield return null;
        }

        portal = false;

        yield return null;
    }

    public void Teleport(Vector3 target)
    {
        GameManager.instance.DungeonSetting();
        gameObject.transform.position = target;
    }
}
