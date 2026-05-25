using UnityEngine;

public class BulletLogic : MonoBehaviour
{
    [Header("Bullet")]
    public float speed = 20f;
    public float lifeTime = 3f;
    public int scoreValue = 1;
    public int healValue = 5;
    public bool grantsSkillCharge = true;

    private bool hitApplied;

    void Start()
    {
        CombatEffects.AttachBulletTrail(gameObject, new Color(0.15f, 0.95f, 1f, 1f), 5f, 0.12f);
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hitApplied || other == null)
            return;

        BossController boss = other.GetComponentInParent<BossController>();
        if (boss != null)
        {
            hitApplied = true;
            Vector3 bossHitPosition = other.ClosestPoint(transform.position);
            boss.TakeDamage(1, bossHitPosition);
            Destroy(gameObject);
            return;
        }

        if (!other.CompareTag("Enemy"))
            return;

        hitApplied = true;

        Vector3 hitPosition = other.ClosestPoint(transform.position);
        CombatEffects.SpawnHit(hitPosition, -transform.forward);
        CombatEffects.SpawnExplosion(other.transform.position);

        Destroy(other.gameObject);
        Destroy(gameObject);

        ScoreManager.score += scoreValue;
        BloodManager.blood = Mathf.Min(100, BloodManager.blood + DifficultyManager.GetKillHealAmount(healValue));

        if (grantsSkillCharge && PlayerSkill.Instance != null)
        {
            PlayerSkill.Instance.RegisterEnemyKill();
        }
    }
}
