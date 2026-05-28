using Photon.Pun;
using UnityEngine;

public class EnemyBulletLogic : MonoBehaviour, IPooledObject
{
    [Header("Bullet")]
    public float speed = 20f;
    public float lifeTime = 3f;
    public int damage = 5;

    private bool hitApplied;
    private float collisionRadius = 2f;
    private bool spawnedFromPool;

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
        ClearTrail();
    }

    private void ResetBulletState()
    {
        hitApplied = false;
        EnsureTriggerPhysics();
        collisionRadius = CalculateCollisionRadius();
        CombatEffects.AttachBulletTrail(gameObject, new Color(0.95f, 0.16f, 0.04f, 0.65f), 1.35f, 0.08f, 1f, 0.55f);
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
        Vector3 previousPosition = transform.position;
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
        TryHitLocalNetworkPlayer(previousPosition, transform.position);
    }

    void OnTriggerEnter(Collider other)
    {
        TryApplyHit(other);
    }

    private bool TryApplyHit(Collider other)
    {
        if (hitApplied || other == null)
            return false;

        if (!other.CompareTag("Player"))
            return false;

        Vector3 hitPosition = other.ClosestPoint(transform.position);
        bool blockedByShield = IsBlockedByShield(other);

        if (NetworkCoopGameRuntime.IsActive)
        {
            PhotonView targetView = other.GetComponentInParent<PhotonView>();
            if (targetView == null || !targetView.IsMine)
                return false;

            hitApplied = true;
            RuntimeObjectPool.Release(gameObject);
            if (NetworkCoopGameRuntime.ReportPlayerDamaged(damage, hitPosition, -transform.forward, blockedByShield))
                return true;
        }

        hitApplied = true;
        RuntimeObjectPool.Release(gameObject);
        CombatEffects.SpawnEnemyHit(hitPosition, -transform.forward);

        if (blockedByShield)
            return true;

        BloodManager.blood = Mathf.Max(0, BloodManager.blood - damage);
        return true;
    }

    private void TryHitLocalNetworkPlayer(Vector3 previousPosition, Vector3 currentPosition)
    {
        if (!NetworkCoopGameRuntime.IsActive || hitApplied || NetworkPlayerController.LocalPlayer == null)
            return;

        Vector3 travel = currentPosition - previousPosition;
        float distance = travel.magnitude;
        if (distance > 0.001f)
        {
            RaycastHit[] hits = Physics.SphereCastAll(
                previousPosition,
                collisionRadius,
                travel / distance,
                distance + collisionRadius,
                ~0,
                QueryTriggerInteraction.Collide
            );

            foreach (RaycastHit hit in hits)
            {
                if (IsLocalNetworkPlayerCollider(hit.collider) && TryApplyHit(hit.collider))
                    return;
            }
        }

        Collider[] overlaps = Physics.OverlapSphere(currentPosition, collisionRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (Collider overlap in overlaps)
        {
            if (IsLocalNetworkPlayerCollider(overlap) && TryApplyHit(overlap))
                return;
        }
    }

    private bool IsLocalNetworkPlayerCollider(Collider other)
    {
        if (other == null || NetworkPlayerController.LocalPlayer == null)
            return false;

        return other.GetComponentInParent<NetworkPlayerController>() == NetworkPlayerController.LocalPlayer;
    }

    private void EnsureTriggerPhysics()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider bulletCollider in colliders)
        {
            if (bulletCollider != null)
            {
                bulletCollider.isTrigger = true;
            }
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.useGravity = false;
        body.isKinematic = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private float CalculateCollisionRadius()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        float radius = 1.5f;
        foreach (Collider bulletCollider in colliders)
        {
            if (bulletCollider == null || !bulletCollider.enabled)
                continue;

            Vector3 extents = bulletCollider.bounds.extents;
            radius = Mathf.Max(radius, extents.x, extents.y, extents.z);
        }

        return radius;
    }

    private bool IsBlockedByShield(Collider other)
    {
        NetworkPlayerController networkPlayer = other.GetComponentInParent<NetworkPlayerController>();
        if (networkPlayer != null)
            return networkPlayer.IsShieldActive;

        if (PlayerSkill.Instance != null && PlayerSkill.Instance.IsShieldActive)
            return true;

        return false;
    }
}
