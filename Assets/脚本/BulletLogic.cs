using UnityEngine;

public class BulletLogic : MonoBehaviour, IPooledObject
{
    [Header("Bullet")]
    public float speed = 20f;
    public float lifeTime = 3f;
    public int scoreValue = 1;
    public int healValue = 5;
    public bool grantsSkillCharge = true;

    private bool hitApplied;
    private bool spawnedFromPool;
    private bool defaultsCaptured;
    private bool defaultGrantsSkillCharge;

    void Awake()
    {
        CaptureDefaults();
    }

    void Start()
    {
        if (!spawnedFromPool)
        {
            ResetBulletState();
        }
    }

    public void OnSpawnedFromPool()
    {
        spawnedFromPool = true;
        ResetBulletState();
    }

    public void OnReturnedToPool()
    {
        CancelInvoke(nameof(Expire));
        hitApplied = false;
        grantsSkillCharge = defaultGrantsSkillCharge;
        ClearTrail();
    }

    private void CaptureDefaults()
    {
        if (defaultsCaptured)
            return;

        defaultGrantsSkillCharge = grantsSkillCharge;
        defaultsCaptured = true;
    }

    private void ResetBulletState()
    {
        CaptureDefaults();
        hitApplied = false;
        grantsSkillCharge = defaultGrantsSkillCharge;
        CombatEffects.AttachBulletTrail(gameObject, PlayerShipColorSelection.CurrentTheme.BulletTrail, 5f, 0.12f);
        ClearTrail();
        CancelInvoke(nameof(Expire));
        Invoke(nameof(Expire), Mathf.Max(0.05f, lifeTime));
    }

    private void ClearTrail()
    {
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();
        }
    }

    private void Expire()
    {
        RuntimeObjectPool.Release(gameObject);
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
            RuntimeObjectPool.Release(gameObject);
            return;
        }

        if (!other.CompareTag("Enemy"))
            return;

        hitApplied = true;

        Vector3 hitPosition = other.ClosestPoint(transform.position);
        CombatEffects.SpawnHit(hitPosition, -transform.forward, PlayerShipColorSelection.CurrentTheme);
        CombatEffects.SpawnExplosion(other.transform.position);

        Destroy(other.gameObject);
        RuntimeObjectPool.Release(gameObject);

        ScoreManager.score += scoreValue;
        BloodManager.blood = Mathf.Min(100, BloodManager.blood + DifficultyManager.GetKillHealAmount(healValue));

        if (grantsSkillCharge && PlayerSkill.Instance != null)
        {
            PlayerSkill.Instance.RegisterEnemyKill();
        }
    }
}
