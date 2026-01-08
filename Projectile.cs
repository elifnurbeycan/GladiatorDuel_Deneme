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
        // 1. Kendi sahibimize çarptıysak yoksay
        if (!string.IsNullOrEmpty(targetTag) && !hitObj.CompareTag(targetTag))
        {
            Debug.Log("-> Kendi sahibime çarptım, yoksayıyorum.");
            return; 
        }

        // 2. Hedefi bulduysak hasar ver
        Gladiator target = hitObj.GetComponent<Gladiator>();
        if (target == null) target = hitObj.GetComponentInParent<Gladiator>();

        if (target != null)
        {
            Debug.Log("-> Hedef (Gladiator) bulundu! Hasar veriliyor.");
            target.TakeDamage(damage);
            Destroy(gameObject); // Oku yok et
        }
        else
        {
            Debug.Log("-> Çarptığım şeyde 'Gladiator' scripti yok.");
        }
    }
}