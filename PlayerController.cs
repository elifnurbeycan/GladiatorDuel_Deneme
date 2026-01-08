using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public Gladiator player;
    public Gladiator enemy;

    // Tur sonunu gecikmeli çalıştırmak için coroutine
    private IEnumerator EndPlayerTurnWithDelay()
    {
        yield return new WaitForSeconds(2f);   // 2 saniye bekle
        GameManager.Instance.EndPlayerTurn();
    }

    // Oyuncu hamleyi seçtiği anda inputu kilitle
    private void LockPlayerTurn()
    {
        GameManager.Instance.isPlayerTurn = false;
        GameManager.Instance.uiManager.UpdateActionButtonsInteractable(false);
    }

    public void OnMoveForward()
    {
        if (!GameManager.Instance.isPlayerTurn) return;
        if (!player.SpendMana(4)) return;

        // 🔥 LOG EKLE: Oyuncu ne yaptı?
        GameManager.Instance.uiManager.UpdateBattleLog("Oyuncu İleri Atıldı");
        
        // 🔥 PANEL KAPAT: Eğer Melee paneli açıksa kapat
        GameManager.Instance.uiManager.ShowMeleeChoicePanel(false);

        LockPlayerTurn();

        GameManager.Instance.MoveCloser(true);
        StartCoroutine(EndPlayerTurnWithDelay());
    }

    public void OnMoveBackward()
    {
        if (!GameManager.Instance.isPlayerTurn) return;
        
        // Zaten Far ise gitme kontrolü
        if (GameManager.Instance.currentDistance == DistanceLevel.Far) 
        {
            GameManager.Instance.uiManager.UpdateBattleLog("Zaten En Uzak Mesafedesin!");
            return; 
        }

        if (!player.SpendMana(4)) return;

        // 🔥 LOG VE PANEL
        GameManager.Instance.uiManager.UpdateBattleLog("Oyuncu Geri Çekildi");
        GameManager.Instance.uiManager.ShowMeleeChoicePanel(false);

        LockPlayerTurn();

        GameManager.Instance.MoveAway(true); 
        StartCoroutine(EndPlayerTurnWithDelay());
    }

    public void OnRangedAttack()
    {
        if (!GameManager.Instance.isPlayerTurn) return;

        if (player.currentAmmo <= 0) 
        {
            GameManager.Instance.uiManager.UpdateBattleLog("Mermi Bitti!");
            return;
        }

        if (GameManager.Instance.currentDistance == DistanceLevel.Close)
        {
            GameManager.Instance.uiManager.UpdateBattleLog("Mesafe Çok Yakın! Ok Atılamaz.");
            return;
        }

        if (!player.SpendMana(20)) 
        {
            GameManager.Instance.uiManager.UpdateBattleLog("Mana Yetersiz!");
            return;
        }

        // 🔥 LOG VE PANEL
        GameManager.Instance.uiManager.UpdateBattleLog("Oyuncu Ok Fırlattı!");
        GameManager.Instance.uiManager.ShowMeleeChoicePanel(false);

        LockPlayerTurn();
        player.currentAmmo--;

        int damage = Random.Range(15, 21);
        
        // Ok fırlat
        player.ShootProjectile("Enemy", damage);

        StartCoroutine(EndPlayerTurnWithDelay());
    }

    public void OnMeleeButton()
    {
        if (!GameManager.Instance.isPlayerTurn) return;

        if (GameManager.Instance.currentDistance != DistanceLevel.Close)
        {
            GameManager.Instance.uiManager.UpdateBattleLog("Rakip Çok Uzakta! Yaklaşmalısın.");
            return;
        }

        // Paneli aç
        GameManager.Instance.uiManager.ShowMeleeChoicePanel(true);
    }

    public void OnQuickAttack()
    {
        if (!GameManager.Instance.isPlayerTurn) return;
        if (GameManager.Instance.currentDistance != DistanceLevel.Close) return;
        if (!player.SpendMana(10)) return;

        // 🔥 LOG
        GameManager.Instance.uiManager.UpdateBattleLog("Oyuncu Hızlı Saldırı Yaptı!");

        LockPlayerTurn();

        player.TriggerAttack();

        if (Random.value <= 0.85f)
        {
            int dmg = Random.Range(10, 13);
            enemy.TakeDamage(dmg);
        }
        else
        {
            GameManager.Instance.uiManager.UpdateBattleLog("Oyuncu Iskaladı!");
        }

        GameManager.Instance.uiManager.ShowMeleeChoicePanel(false);
        StartCoroutine(EndPlayerTurnWithDelay());
    }

    public void OnPowerAttack()
    {
        if (!GameManager.Instance.isPlayerTurn) return;
        if (GameManager.Instance.currentDistance != DistanceLevel.Close) return;
        if (!player.SpendMana(30)) return;

        // 🔥 LOG
        GameManager.Instance.uiManager.UpdateBattleLog("Oyuncu Güçlü Saldırı Yaptı!");

        LockPlayerTurn();

        player.TriggerAttack();

        if (Random.value <= 0.50f)
        {
            int dmg = Random.Range(25, 36);
            enemy.TakeDamage(dmg);
        }
        else
        {
            GameManager.Instance.uiManager.UpdateBattleLog("Oyuncu Iskaladı!");
        }

        GameManager.Instance.uiManager.ShowMeleeChoicePanel(false);
        StartCoroutine(EndPlayerTurnWithDelay());
    }

    public void OnSleep()
    {
        if (!GameManager.Instance.isPlayerTurn) return;
        
        if (player.currentMana >= 50) 
        {
            // Mana çoksa uyumaya gerek yok uyarısı (Opsiyonel)
            // Ama kural gereği "Mana < 50" şartı varsa buton zaten pasif olur.
            return;
        }

        // 🔥 LOG
        GameManager.Instance.uiManager.UpdateBattleLog("Oyuncu Dinleniyor...");
        GameManager.Instance.uiManager.ShowMeleeChoicePanel(false);

        LockPlayerTurn();

        player.RestoreMana(40);
        player.RestoreHP(15);

        StartCoroutine(EndPlayerTurnWithDelay());
    }

    public void OnArmorUp()
    {
        if (!GameManager.Instance.isPlayerTurn) return;
        if (!player.SpendMana(25)) return;

        // 🔥 LOG
        GameManager.Instance.uiManager.UpdateBattleLog("Oyuncu Savunmaya Geçti!");
        GameManager.Instance.uiManager.ShowMeleeChoicePanel(false);

        LockPlayerTurn();

        player.ActivateArmorUp(2);
        StartCoroutine(EndPlayerTurnWithDelay());
    }
}