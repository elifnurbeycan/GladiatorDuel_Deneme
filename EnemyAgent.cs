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
    public float decisionDelay = 0.1f; // Daha hızlı karar versin

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

        // 1. SENSÖRLER
        float distState = (float)GameManager.Instance.currentDistance; 
        float myManaState = enemy.currentMana > 20 ? 1f : 0f;          
        float myAmmoState = enemy.currentAmmo > 0 ? 1f : 0f;           
        float myHPState = Mathf.Round(enemy.currentHP / 20f);          

        List<float> sensors = new List<float> { distState, myManaState, myAmmoState, myHPState };
        brain.SetInputs(sensors);

        // 2. KARAR
        int actionIndex = brain.DecideAction();

        // 3. MANTIK KONTROLÜ
        bool isLogicValid = CheckActionLogic(actionIndex);

        if (!isLogicValid)
        {
            brain.Punish(10f); // Saçma hamleye ceza
            ForceRandomValidMove();
        }
        else
        {
            brain.ExecuteAction(actionIndex);
        }

        // 4. SONUÇ VE TUR BİTİRME
        yield return new WaitForSeconds(1.5f); // Animasyon bekleme süresini kıstım
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
            case 4: return enemy.currentHP < enemy.maxHP || enemy.currentMana < enemy.maxMana; // Mana veya Can eksikse uyuyabilir
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
            GameManager.Instance.uiManager.UpdateBattleLog("Agent Pas Geçti");
        }
    }

    // 🔥 BURASI ÇOK ÖNEMLİ: STRATEJİK YÖNLENDİRME 🔥
    private void AttemptAction(int actionCode)
    {
        bool amIPlayerSide = (GameManager.Instance.player == enemy);
        bool isLowHP = enemy.currentHP < (enemy.maxHP * 0.4f); // Canım %40'ın altında mı?

        switch (actionCode)
        {
            case 0: // Move Forward (İleri Git)
                GameManager.Instance.uiManager.UpdateBattleLog("Agent İleri Gitti"); 
                GameManager.Instance.MoveCloser(amIPlayerSide); 
                enemy.SpendMana(4); 
                
                // Canım çoksa ve yaklaşıyorsam -> AFERİN (Cesaret Ödülü)
                if (!isLowHP) brain.Reward(0.2f);
                // Canım azsa ve düşmana koşuyorsam -> HAYIR (Risk Cezası)
                else brain.Punish(0.2f);
                break;

            case 1: // Move Backward (Geri Git)
                GameManager.Instance.uiManager.UpdateBattleLog("Agent Geri Çekildi"); 
                GameManager.Instance.MoveAway(amIPlayerSide); 
                enemy.SpendMana(4); 
                
                // Canım azsa ve kaçıyorsam -> AFERİN (Hayatta Kalma Ödülü)
                if (isLowHP) brain.Reward(0.5f);
                // Canım full ve kaçıyorsam -> HAYIR (Korkaklık Cezası)
                else brain.Punish(0.2f);
                break;

            case 2: // Ranged Attack
                GameManager.Instance.uiManager.UpdateBattleLog("Agent Ok Attı"); 
                enemy.currentAmmo--; enemy.SpendMana(20); 
                enemy.ShootProjectile("Player", Random.Range(15, 21)); 
                // Saldırıya her zaman ufak bir teşvik verelim
                brain.Reward(0.1f);
                break;

            case 3: // Melee Attack
                GameManager.Instance.uiManager.UpdateBattleLog("Agent Kılıç Vurdu");
                enemy.SpendMana(10); 
                enemy.TriggerAttack();

                // ARTIK ISKA YOK! HER ZAMAN VURUR ⚔️
                // Hasar aralığı sabit kalsın (10-15 arası)
                int damage = Random.Range(10, 16); 
                player.TakeDamage(damage);

                // Vurduğu için ödül veriyoruz
                brain.Reward(0.2f); 
                break; 

            case 4: // Sleep - İyileşme
                GameManager.Instance.uiManager.UpdateBattleLog("Agent İyileşiyor");
    
                // Değerleri düşür:
                enemy.RestoreMana(20); // Manası yavaş dolsun
                enemy.RestoreHP(5);    // Canı çok az dolsun (Rakip 10 vurursa, uyusa bile 5 zarar eder)

                // Sadece gerçekten zordaysa ödül ver (Canı %40 altındaysa)
                if (isLowHP || enemy.currentMana < 20) brain.Reward(0.5f);
                else brain.Punish(0.1f); // Keyfi uyuyorsa hafif ceza ver
                break;

            case 5: // Armor
                GameManager.Instance.uiManager.UpdateBattleLog("Agent Savunma Aldı");
                enemy.SpendMana(25); enemy.ActivateArmorUp(2);
                if (isLowHP) brain.Reward(0.3f); // Can azken defans mantıklı
                break;
        }
    }

    // 🔥 ÖDÜL SİSTEMİ: SONUCA GÖRE BÜYÜK PUAN 🔥
    private void EvaluateResult()
    {
        if (player.currentHP <= 0) { brain.Reward(150f); return; } // KAZANMA
        if (enemy.currentHP <= 0) { brain.Punish(150f); return; }  // KAYBETME

        float damageDealt = hpAtStartOfTurn_Player - player.currentHP;
        float damageTaken = hpAtStartOfTurn_Enemy - enemy.currentHP;

        float turnReward = 0f;

        // 1. VURMA PUANI (Daha yüksek yaptık ki saldırmayı sevsin)
        if (damageDealt > 0) turnReward += damageDealt * 3.0f;
        
        // 2. HASAR YEME CEZASI
        if (damageTaken > 0)
        {
            // Canım azsa hasar yemek felakettir (4 kat ceza)
            // Canım çoksa o kadar dert değil (2 kat ceza)
            float survivalFactor = (enemy.currentHP < 30) ? 4.0f : 2.0f;
            turnReward -= damageTaken * survivalFactor;
        }
        else if (damageTaken < 0) 
        {
            // İyileştiysem (Negatif hasar) ödül ver
            turnReward += Mathf.Abs(damageTaken) * 1.5f;
        }

        // 3. CAN FARKI AVANTAJI
        float hpGap = enemy.currentHP - player.currentHP;
        turnReward += hpGap * 0.2f;

        // Ödülü uygula
        if (turnReward > 0) brain.Reward(turnReward);
        else 
        {
            if (turnReward == 0) turnReward = -0.5f; // Boş geçmek hafif kötüdür
            brain.Punish(Mathf.Abs(turnReward));
        }
    }
}
