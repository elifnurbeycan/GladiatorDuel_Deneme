using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyAgent : MonoBehaviour
{
    [Header("References")]
    public Gladiator enemy;  // Agent (Kendisi)
    public Gladiator player; // Hedef (Rakibi)
    private QLearningBrain brain;

    [Header("AI Settings")]
    public float decisionDelay = 0.2f; 

    // Hasar takibi
    private float hpAtStartOfTurn_Player;
    private float hpAtStartOfTurn_Enemy;

    void Start()
    {
        brain = GetComponent<QLearningBrain>();

        if(player != null && enemy != null)
        {
            hpAtStartOfTurn_Player = player.currentHP;
            hpAtStartOfTurn_Enemy = enemy.currentHP;
        }

        // Aksiyonları Kaydet
        brain.RegisterAction("MoveForward",  _ => AttemptAction(0), 0);
        brain.RegisterAction("MoveBackward", _ => AttemptAction(1), 0);
        brain.RegisterAction("RangedAttack", _ => AttemptAction(2), 0);
        brain.RegisterAction("MeleeAttack",  _ => AttemptAction(3), 0);
        brain.RegisterAction("Sleep",        _ => AttemptAction(4), 0);
        brain.RegisterAction("ArmorUp",      _ => AttemptAction(5), 0);
    }

    public void StartEnemyTurn()
    {
        hpAtStartOfTurn_Player = player.currentHP;
        hpAtStartOfTurn_Enemy = enemy.currentHP;
        StartCoroutine(ThinkAndAct());
    }

    private IEnumerator ThinkAndAct()
    {
        yield return new WaitForSeconds(decisionDelay);

        //SENSÖRLER
        float distState = (float)GameManager.Instance.currentDistance; 
        float myManaState = enemy.currentMana > 20 ? 1f : 0f;          
        float myAmmoState = enemy.currentAmmo > 0 ? 1f : 0f;           
        float myHPState = Mathf.Round(enemy.currentHP / 20f);          

        List<float> sensors = new List<float> { distState, myManaState, myAmmoState, myHPState };
        brain.SetInputs(sensors);

        //KARAR
        int actionIndex = brain.DecideAction();

        //MANTIK KONTROLÜ
        bool isLogicValid = CheckActionLogic(actionIndex);

        if (!isLogicValid)
        {
            brain.Punish(10f);
            ForceRandomValidMove();
        }
        else
        {
            brain.ExecuteAction(actionIndex);
        }

        //SONUÇ VE TUR BİTİRME
        yield return new WaitForSeconds(0.8f); 
        EvaluateResult();

        if (GameManager.Instance.isTrainingMode)
        {
            if (GameManager.Instance.isPlayerTurn) GameManager.Instance.EndPlayerTurn(); 
            else GameManager.Instance.EndEnemyTurn();  
        }
        else
        {
            GameManager.Instance.EndEnemyTurn();
        }
    }

    
    private bool CheckActionLogic(int actionCode)
    {
        switch (actionCode)
        {
            case 0: return GameManager.Instance.currentDistance != DistanceLevel.Close && enemy.currentMana >= 4;
            case 1: return GameManager.Instance.currentDistance != DistanceLevel.Far && enemy.currentMana >= 4;
            case 2: return GameManager.Instance.currentDistance != DistanceLevel.Close && enemy.currentAmmo > 0 && enemy.currentMana >= 20;
            case 3: return GameManager.Instance.currentDistance == DistanceLevel.Close && enemy.currentMana >= 10;
            
            // ESKİSİ: case 4: return enemy.currentHP < enemy.maxHP;
            // YENİSİ: Canım eksikse VEYA Manam eksikse uyuyabilirim.
            case 4: return enemy.currentHP < enemy.maxHP || enemy.currentMana < enemy.maxMana;
            
            case 5: return enemy.currentMana >= 25;
        }
        return false;
    }

    private void ForceRandomValidMove()
    {
        List<int> validMoves = new List<int>();
        for (int i = 0; i <= 5; i++)
        {
            if (CheckActionLogic(i)) validMoves.Add(i);
        }

        if (validMoves.Count > 0)
        {
            int randomValid = validMoves[Random.Range(0, validMoves.Count)];
            AttemptAction(randomValid);
        }
        else
        {
            // Eğer buraya düşüyorsa hem canın hem manan full ama yapacak hamlen yok demektir (Çok nadir)
            GameManager.Instance.uiManager.UpdateBattleLog("Agent Pas Geçti");
        }
    }

    private void AttemptAction(int actionCode)
    {
        bool amIPlayerSide = (GameManager.Instance.player == enemy);

        switch (actionCode)
        {
            case 0: 
                GameManager.Instance.uiManager.UpdateBattleLog("Agent İleri Gitti"); 
                GameManager.Instance.MoveCloser(amIPlayerSide); 
                enemy.SpendMana(4); 
                break;

            case 1: 
                GameManager.Instance.uiManager.UpdateBattleLog("Agent Geri Çekildi"); 
                GameManager.Instance.MoveAway(amIPlayerSide); 
                enemy.SpendMana(4); 
                break;

            case 2: 
                GameManager.Instance.uiManager.UpdateBattleLog("Agent Ok Attı"); 
                enemy.currentAmmo--; enemy.SpendMana(20); 
                enemy.ShootProjectile("Player", Random.Range(15, 21)); 
                break;

            case 3:
                GameManager.Instance.uiManager.UpdateBattleLog("Agent Kılıç Vurdu");
                enemy.SpendMana(10); enemy.TriggerAttack();
                if (Random.value > 0.15f) player.TakeDamage(Random.Range(10, 15));
                else GameManager.Instance.uiManager.UpdateBattleLog("Agent Iskaladı!");
                break;

            case 4:
                GameManager.Instance.uiManager.UpdateBattleLog("Agent İyileşiyor");
                enemy.RestoreMana(40); enemy.RestoreHP(15);
                break;

            case 5:
                GameManager.Instance.uiManager.UpdateBattleLog("Agent Savunma Aldı");
                enemy.SpendMana(25); enemy.ActivateArmorUp(2);
                break;
        }
    }

    private void EvaluateResult()
    {
        if (player.currentHP <= 0) { brain.Reward(150f); return; }
        if (enemy.currentHP <= 0) { brain.Punish(150f); return; }

        float damageDealt = hpAtStartOfTurn_Player - player.currentHP;
        float damageTaken = hpAtStartOfTurn_Enemy - enemy.currentHP;

        float turnReward = 0f;
        if (damageDealt > 0) turnReward += damageDealt * 2.0f;
        
        if (damageTaken > 0)
        {
            float survivalFactor = (enemy.currentHP < 30) ? 4.0f : 2.0f;
            turnReward -= damageTaken * survivalFactor;
        }
        else if (damageTaken < 0) 
        {
            turnReward += Mathf.Abs(damageTaken) * 1.5f;
        }

        float hpGap = enemy.currentHP - player.currentHP;
        turnReward += hpGap * 0.2f;

        if (turnReward > 0) brain.Reward(turnReward);
        else 
        {
            if (turnReward == 0) turnReward = -1f;
            brain.Punish(Mathf.Abs(turnReward));
        }
    }
}