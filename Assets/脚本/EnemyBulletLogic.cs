using UnityEngine;

public class EnemyBulletLogic : MonoBehaviour
{
    [Header("Bullet")]
    public float speed = 20f;
    public float lifeTime = 3f;
    public int damage = 5;

    void Start()
    {
        CombatEffects.AttachBulletTrail(gameObject, new Color(1f, 0.25f, 0.05f, 1f), 4f, 0.12f);
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Vector3 hitPosition = other.ClosestPoint(transform.position);
        CombatEffects.SpawnHit(hitPosition, -transform.forward);

        Destroy(gameObject);
        if (PlayerSkill.Instance != null && PlayerSkill.Instance.IsShieldActive)
            return;

        BloodManager.blood = Mathf.Max(0, BloodManager.blood - damage);
    }
}
