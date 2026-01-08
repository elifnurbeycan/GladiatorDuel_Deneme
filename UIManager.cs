using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Player UI")]
    public Slider playerHPSlider;
    public Slider playerManaSlider;
    public TextMeshProUGUI playerAmmoText;

    [Header("Enemy UI")]
    public Slider enemyHPSlider;
    public Slider enemyManaSlider;
    public TextMeshProUGUI enemyAmmoText;

    [Header("General UI")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI battleLogText;

    [Header("Action Buttons")]
    public Button moveForwardButton;
    public Button moveBackwardButton;
    public Button rangedButton;
    public Button meleeButton;
    public Button sleepButton;
    public Button armorUpButton;

    [Header("Melee Panel")]
    public GameObject meleeChoicePanel;

    [Header("Gladiator References")]
    public Gladiator player;
    public Gladiator enemy;

    public void UpdateAllUI()
    {
        // Player
        playerHPSlider.maxValue = player.maxHP;
        playerHPSlider.value    = player.currentHP;

        playerManaSlider.maxValue = player.maxMana;
        playerManaSlider.value    = player.currentMana;

        playerAmmoText.text = "Ammo: " + player.currentAmmo;

        // Enemy
        enemyHPSlider.maxValue = enemy.maxHP;
        enemyHPSlider.value    = enemy.currentHP;

        enemyManaSlider.maxValue = enemy.maxMana;
        enemyManaSlider.value    = enemy.currentMana;

        enemyAmmoText.text = "Ammo: " + enemy.currentAmmo;

        // Sleep butonu sadece mana < 50 iken aktif olsun (ve oyuncu sırasıysa)
        bool canSleep = player.currentMana < 50 && GameManager.Instance.isPlayerTurn;
        sleepButton.interactable = canSleep;
    }

    public void SetTurnText(string txt)
    {
        turnText.text = txt;
    }

    public void UpdateDistanceText(DistanceLevel dist)
    {
        distanceText.text = "Distance: " + dist.ToString();
    }

    public void UpdateBattleLog(string message)
    {
        if (battleLogText != null)
        {
            battleLogText.text = message;
            
            // İstersen 2 saniye sonra silinmesi için Coroutine yazabilirsin
            // ama şimdilik kalıcı dursun, yeni mesaj gelince değişsin.
        }
    }

    public void UpdateActionButtonsInteractable(bool enable)
    {
        moveForwardButton.interactable  = enable;
        moveBackwardButton.interactable = enable;
        rangedButton.interactable       = enable;
        meleeButton.interactable        = enable;
        armorUpButton.interactable      = enable;

        if (!enable)
        {
            // Tur rakipteyken sleep'i de kapat
            sleepButton.interactable = false;
        }
        else
        {
            // Oyuncu turu başladığında tekrar güncellensin
            UpdateAllUI();
        }
    }

    public void ShowMeleeChoicePanel(bool show)
    {
        meleeChoicePanel.SetActive(show);
    }
}
