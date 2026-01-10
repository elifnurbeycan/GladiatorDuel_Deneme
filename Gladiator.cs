using UnityEngine;

public class Gladiator : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator; 

    [Header("Base Stats")]
    public int maxHP = 100;
    public int currentHP;

    public int maxMana = 120;
    public int startMana = 80;
    public int currentMana;

    [Header("Ranged")]
    public int maxAmmo = 10;
    public int currentAmmo;

    [Header("Armor Up")]
    public bool armorUpActive = false;
    public int armorUpTurnsRemaining = 0; 

    [Header("Audio")]
    public AudioSource audioSource;   // Karakterin üzerindeki Audio Source
    public AudioClip attackSound;     // Vuruş Sesi
    public AudioClip hitSound;        // Hasar/Acı Sesi
    public AudioClip walkSound;       // Yürüme Sesi (Loop)

    [Header("Projectile Settings")]
    public GameObject arrowPrefab;    // Fırlatılacak Ok Prefab'ı
    public Transform firePoint;       // Okun çıkacağı nokta (Namlu)

    private void Awake()
    {
        currentHP = maxHP;
        currentMana = Mathf.Clamp(startMana, 0, maxMana);
        currentAmmo = maxAmmo;
    }

    // Awake'ten hemen sonra çalışır
    private void Start()
    {
        // 🔥 SFX AYARINI HAFIZADAN ÇEK 🔥
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

        if (audioSource != null)
        {
            audioSource.volume = sfxVol; // Karakterin sesini ayarla
        }
    }

    // --- FIRLATMA (PROJECTILE) SİSTEMİ ---
    
    public void ShootProjectile(string targetTag, float damage)
    {
        // DÜZELTME 1: 'projectilePrefab' yerine 'arrowPrefab' kullanıldı.
        GameObject projectile = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);

        // 2. Hedefi Bul (Rakip nerede?)
        GameObject target = GameObject.FindGameObjectWithTag(targetTag);

        if (target != null)
        {
            // 3. Yönü Hesapla: (Hedef Konumu - Namlu Konumu)
            Vector2 direction = (target.transform.position - firePoint.position).normalized;

            // 4. Mermiyi o yöne fırlat
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Hızı burada ayarlıyoruz (15f hızında)
                // Not: Her Unity sürümünde çalışması için 'velocity' kullanıldı.
                rb.linearVelocity = direction * 15f; 
            }

            // 5. (İsteğe Bağlı) Okun görsel açısını da hedefe döndür
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

            // 6. Hasar bilgisini mermiye yükle
            Projectile projScript = projectile.GetComponent<Projectile>();
            if (projScript != null)
            {
                // DÜZELTME 2: (int) ekleyerek float'ı int'e çevirdik.
                projScript.damage = (int)damage; 
                projScript.targetTag = targetTag; // Kimi vuracağını söyle
            }
        }
        else
        {
            // Hedef yoksa (Yedek plan) FirePoint yönüne gitsin
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                 if (transform.localScale.x < 0) rb.linearVelocity = Vector2.left * 15f;
                 else rb.linearVelocity = Vector2.right * 15f;
            }
        }
    }

    // --- ANİMASYON VE SES FONKSİYONLARI ---

    // 1. Yürüme Animasyonu ve Sesi (Aç/Kapa)
    public void SetMoveAnimation(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
        }
    }

    public void ToggleWalkSound(bool isWalking)
    {
        if (audioSource == null || walkSound == null) return;

        if (isWalking)
        {
            // Eğer zaten çalıyorsa tekrar başlatma (Sesin üst üste binmesini engeller)
            if (!audioSource.isPlaying || audioSource.clip != walkSound)
            {
                audioSource.clip = walkSound;
                audioSource.loop = true; // Yürüdüğü sürece döngüde kalsın
                audioSource.Play();
            }
        }
        else
        {
            // Yürüme bittiyse durdur
            if (audioSource.clip == walkSound)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
        }
    }

    // 2. Saldırı Animasyonu ve Sesi (Tetikleyici)
    public void TriggerAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }

    // 3. Hasar ve Ölüm Mantığı
    public void TakeDamage(int amount)
    {
        // Zaten öldüyse tepki verme
        if (currentHP <= 0) return;

        float finalDamage = amount;

        if (armorUpActive)
        {
            finalDamage *= 0.8f; // %20 hasar azaltma
        }

        currentHP -= Mathf.RoundToInt(finalDamage);
        if (currentHP < 0) currentHP = 0;

        if (animator != null)
        {
            if (currentHP <= 0)
            {
                // ÖLÜM
                animator.SetTrigger("Death");
            }
            else
            {
                // HASAR ALMA
                animator.SetTrigger("Hit");

                if (audioSource != null && hitSound != null)
                {
                    audioSource.PlayOneShot(hitSound);
                }
            }
        }
    }

    // --- YARDIMCI FONKSİYONLAR ---

    public bool SpendMana(int amount)
    {
        if (currentMana < amount) return false;
        currentMana -= amount;
        return true;
    }

    public void RestoreMana(int amount)
    {
        currentMana += amount;
        if (currentMana > maxMana) currentMana = maxMana;
    }

    public void RestoreHP(int amount)
    {
        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;
    }

    public void ActivateArmorUp(int turns)
    {
        armorUpActive = true;
        armorUpTurnsRemaining = turns;
    }

    public void OnTurnEnd()
    {
        if (armorUpActive)
        {
            armorUpTurnsRemaining--;
            if (armorUpTurnsRemaining <= 0)
            {
                armorUpActive = false;
            }
        }
    }
}
