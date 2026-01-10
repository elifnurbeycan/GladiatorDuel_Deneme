using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 20;
    public string targetTag; 

    private float lifeTime = 3f;

    void Start()
    {
        Destroy(gameObject, lifeTime); // 3 saniye sonra yok ol
        
        // Çarpışmayı aç
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
    }

    void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    // Hem Trigger hem Collision dinliyoruz
    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        Debug.Log("OK BİR ŞEYE DEĞDİ (Trigger): " + hitInfo.name);
        HandleHit(hitInfo.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("OK BİR ŞEYE DEĞDİ (Collision): " + collision.gameObject.name);
        HandleHit(collision.gameObject);
    }

    void HandleHit(GameObject hitObj)
    {
        // 1. Önce çarptığım şey canlı bir "Gladiator" mı diye bak
        Gladiator hitGladiator = hitObj.GetComponent<Gladiator>();

        // Eğer çarptığım şey Gladyatör DEĞİLSE (Arka plan, duvar vs.) -> SESSİZCE ÇIK
        if (hitGladiator == null) return;

        // 2. Gladyatör ama benim HEDEFİM değilse (Yani oku atan kişi kendisine çarptıysa)
        if (!hitObj.CompareTag(targetTag))
        {
            // İstersen buraya log koyabilirsin ama gerek yok, sessizce geçsin.
            // Debug.Log("Kendi kendime çarptım, yoksayıyorum.");
            return; 
        }

        // 3. Buraya geldiysek HEDEFİ VURDUK demektir!
        Debug.Log("-> TAM İSABET! Hasar veriliyor: " + hitObj.name);
        hitGladiator.TakeDamage(damage);
        
        // Oku yok et
        Destroy(gameObject); 
    }
}
