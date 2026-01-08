using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // Sahne resetlemek için gerekli

public enum DistanceLevel
{
    Close,
    Mid,
    Far
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    public Gladiator player; // Kırmızı Gladyatör (Agent_Red)
    public Gladiator enemy;  // Mavi Gladyatör (Agent_Blue)
    public UIManager uiManager;
    
    // 🔥 YENİ EKLENEN REFERANS: Kırmızı Ajanın Beyni
    public EnemyAgent playerAgent; // Agent_Red içindeki EnemyAgent scripti
    public EnemyAgent enemyAgent;  // Agent_Blue içindeki EnemyAgent scripti

    [Header("Transforms")]
    public Transform playerTransform;
    public Transform enemyTransform;

    [Header("Turn / State")]
    public bool isPlayerTurn = true;
    public DistanceLevel currentDistance = DistanceLevel.Far;

    //YENİ EKLENEN: EĞİTİM MODU AYARI
    [Header("Eğitim Ayarları (Training)")]
    public bool isTrainingMode = false;

    [Header("Audio Settings")]
    public AudioSource musicSource; 

    // Hareket Ayarları
    private float stepSize = 2.0f; 
    private float mapBoundary = 7.5f;
    private float minDistanceBetween = 1.5f; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        isPlayerTurn = true;
        
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        AudioListener.volume = musicVol;

        if (musicSource != null)
        {
            musicSource.loop = true;
            if (!musicSource.isPlaying) musicSource.Play();
        }

        InitPositions();
        UpdateDistanceState();
        uiManager.UpdateAllUI();

        //
        StartPlayerTurn(); 
    }

    private void InitPositions()
    {
        if (playerTransform == null || enemyTransform == null) return;
        playerTransform.position = new Vector3(-mapBoundary, playerTransform.position.y, playerTransform.position.z);
        enemyTransform.position  = new Vector3(mapBoundary, enemyTransform.position.y, enemyTransform.position.z);
    }

    // =======================================================
    // SIRA YÖNETİMİ 
    // =======================================================

    //YENİ FONKSİYON: Oyuncu sırasını başlatır
    public void StartPlayerTurn()
    {
        isPlayerTurn = true;
        uiManager.SetTurnText("Kırmızı Sırası");

        if (isTrainingMode)
        {
            // EĞİTİM MODU: Butonları kapat, Kırmızı Ajanı çalıştır
            uiManager.UpdateActionButtonsInteractable(false);
            if (playerAgent != null) playerAgent.StartEnemyTurn();
        }
        else
        {
            // NORMAL MOD: Butonları aç, sen oyna
            uiManager.UpdateActionButtonsInteractable(true);
        }
    }

    // Player hamlesini bitirince çağrılır
    public void EndPlayerTurn() 
    { 
        player.OnTurnEnd(); 
        uiManager.UpdateAllUI(); 
        
        if (CheckGameEnd()) return; // Oyun bitti mi kontrol et

        isPlayerTurn = false; 
        uiManager.SetTurnText("Mavi Sırası"); 
        uiManager.UpdateActionButtonsInteractable(false); 
        
        // Mavi Ajanı çalıştır
        if (enemyAgent != null) enemyAgent.StartEnemyTurn(); 
    }

    // Enemy hamlesini bitirince çağrılır
    public void EndEnemyTurn() 
    { 
        enemy.OnTurnEnd(); 
        uiManager.UpdateAllUI(); 
        
        if (CheckGameEnd()) return; // Oyun bitti mi kontrol et

        // Sırayı tekrar Player'a (Kırmızıya) ver
        StartPlayerTurn(); 
    }

    // =======================================================
    // OYUN BİTİŞİ VE RESET
    // =======================================================
    private bool CheckGameEnd() 
    { 
        if (player.currentHP <= 0) 
        { 
            uiManager.SetTurnText("Mavi Kazandı!"); 
            uiManager.UpdateActionButtonsInteractable(false); 
            if(isTrainingMode) StartCoroutine(RestartGame()); // Otomatik Reset
            return true; 
        } 
        else if (enemy.currentHP <= 0) 
        { 
            uiManager.SetTurnText("Kırmızı Kazandı!"); 
            uiManager.UpdateActionButtonsInteractable(false); 
            if(isTrainingMode) StartCoroutine(RestartGame()); // Otomatik Reset
            return true; 
        } 
        return false;
    }

    IEnumerator RestartGame()
    {
        yield return new WaitForSeconds(2f); // Sonucu görmek için bekle
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Sahneyi yeniden başlat
    }

    // =======================================================
    // HAREKET MANTIĞI
    // =======================================================

    public void MoveCloser(bool actorIsPlayer)
    {
        float currentX = actorIsPlayer ? playerTransform.position.x : enemyTransform.position.x;
        float targetX;

        if (actorIsPlayer)
        {
            targetX = currentX + stepSize;
            float limit = enemyTransform.position.x - minDistanceBetween;
            if (targetX > limit) targetX = limit;
        }
        else
        {
            targetX = currentX - stepSize;
            float limit = playerTransform.position.x + minDistanceBetween;
            if (targetX < limit) targetX = limit;
        }
        StartCoroutine(SmoothMoveRoutine(actorIsPlayer, targetX));
    }

    public void MoveAway(bool actorIsPlayer)
    {
        float currentX = actorIsPlayer ? playerTransform.position.x : enemyTransform.position.x;
        float targetX;

        if (actorIsPlayer)
        {
            targetX = currentX - stepSize;
            if (targetX < -mapBoundary) targetX = -mapBoundary;
        }
        else
        {
            targetX = currentX + stepSize;
            if (targetX > mapBoundary) targetX = mapBoundary;
        }
        StartCoroutine(SmoothMoveRoutine(actorIsPlayer, targetX));
    }

    private IEnumerator SmoothMoveRoutine(bool actorIsPlayer, float targetX)
    {
        if (actorIsPlayer) { player.SetMoveAnimation(true); player.ToggleWalkSound(true); }
        else { enemy.SetMoveAnimation(true); enemy.ToggleWalkSound(true); }

        Transform movingTransform = actorIsPlayer ? playerTransform : enemyTransform;
        Vector3 startPos = movingTransform.position;
        Vector3 endPos = new Vector3(targetX, startPos.y, startPos.z);

        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
            movingTransform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        movingTransform.position = endPos;

        if (actorIsPlayer) { player.SetMoveAnimation(false); player.ToggleWalkSound(false); }
        else { enemy.SetMoveAnimation(false); enemy.ToggleWalkSound(false); }

        UpdateDistanceState();
    }

    private void UpdateDistanceState()
    {
        float dist = Vector3.Distance(playerTransform.position, enemyTransform.position);
        
        if (dist <= 2.5f) currentDistance = DistanceLevel.Close;
        else if (dist > 2.5f && dist <= 7.0f) currentDistance = DistanceLevel.Mid;
        else currentDistance = DistanceLevel.Far;

        uiManager.UpdateDistanceText(currentDistance);
    }
}