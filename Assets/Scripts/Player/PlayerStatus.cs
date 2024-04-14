using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatus : MonoBehaviour
{
    [Header("# Status")]
    public float currentHealth;
    public float maxHealth;
    public float currentStamina;
    public float maxStamina;
    public int level;
    public float exp;
    public float[] nextExp;

    [Header("# Default Value")]
    public float attack;
    public float powerAttack;

    [Header("# Additional Value")]
    public float increaseAttack; // ���ط� ����
    public float increasePowerAttack; // ������ ���ط� ����
    public float increaseSpeed; // �̵��ӵ� ����
    public float increaseHealth; // ����� ����
    public float increaseStamina; // ���¹̳� ����

    [Header("# Enforce Value")]
    public float enforceAttack;
    public float enforcePowerAttack;
    public float enforceSpeed;
    public float enforceHealth;
    public float enforceStamina;

    [Header("# UI")]
    public Image healthBar;
    public Image steminaBar;
    public Image expBar;
    public Text healthText;
    public Text levelText;

    public static PlayerStatus instance;

    void Awake()
    {
        if (PlayerStatus.instance == null)
            PlayerStatus.instance = this;

        Load();

        currentHealth = maxHealth * (enforceHealth + increaseHealth);
        currentStamina = maxStamina * (enforceStamina + increaseStamina);
    }

    void Update()
    {
        LevelUp();
    }

    void LateUpdate()
    {
        StatusSetting();
        UISetting();
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey("Attack"))
            return;

        enforceAttack = PlayerPrefs.GetFloat("Attack");
        instance.enforcePowerAttack = PlayerPrefs.GetFloat("PowerAttack");
        instance.enforceHealth = PlayerPrefs.GetFloat("Health");
        instance.enforceStamina = PlayerPrefs.GetFloat("Stamina");
        instance.enforceSpeed = PlayerPrefs.GetFloat("Speed");
    }

    void StatusSetting()
    {
        if (currentHealth > maxHealth * (increaseHealth + enforceHealth))
            currentHealth = maxHealth * (increaseHealth + enforceHealth);

        if (currentStamina > maxStamina * (increaseStamina + enforceStamina))
            currentStamina = maxStamina * (increaseStamina + enforceStamina);
    }

    void UISetting()
    {
        healthBar.fillAmount = currentHealth / (maxHealth * (increaseHealth + enforceHealth));
        steminaBar.fillAmount = currentStamina / (maxStamina * (increaseStamina + enforceStamina));
        expBar.fillAmount = exp / nextExp[level];

        healthText.text = currentHealth.ToString("F0") + " / " + (maxHealth * (increaseHealth + enforceHealth)).ToString("F0");
        levelText.text = "Lv." + (level + 1).ToString();

        if (currentHealth < 0)
            healthText.text = "0 / " + (maxHealth * (increaseHealth + enforceHealth)).ToString("F0");
        else
            healthText.text = currentHealth.ToString("F0") + " / " + (maxHealth * (increaseHealth + enforceHealth)).ToString("F0");
    }

    void LevelUp()
    {
        if (level >= nextExp.Length)
            return;

        if (exp >= nextExp[level])
        {
            exp -= nextExp[level];
            level++;
            GameManager.instance.LevelUpSystem();
        }
    }
}
