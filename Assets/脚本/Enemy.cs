using Photon.Pun;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    [Tooltip("Fire interval")]
    public float fireRate = 1.5f;

    private float nextFireTime = 0f;
    private Transform player;
    private bool hasCrashedIntoPlayer;
    private float nextTargetRefreshTime;
    private Collider[] enemyColliders;
    private const float TargetRefreshInterval = 0.25f;

    void Start()
    {
        ApplyDifficulty();
        EnsurePhysicsCallbacks();
        enemyColliders = GetComponentsInChildren<Collider>();
        CombatEffects.ApplyEnemyShipVisuals(gameObject);

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("Enemy: scene has no GameObject tagged Player.");
        }
    }

    void ApplyDifficulty()
    {
        moveSpeed *= DifficultyManager.EnemySpeedMultiplier;
        fireRate *= DifficultyManager.EnemyFireIntervalMultiplier;
    }

    void Update()
    {
        if (NetworkCoopGameRuntime.IsActive)
        {
            CheckLocalPlayerCrash();
        }

        if (NetworkCoopGameRuntime.IsActive && !PhotonNetwork.IsMasterClient)
            return;

        if (player == null || Time.time >= nextTargetRefreshTime)
        {
            RefreshTargetPlayer();
            nextTargetRefreshTime = Time.time + TargetRefreshInterval;
        }

        MoveTowardPlayer();
        TryShoot();
    }

    void RefreshTargetPlayer()
    {
        GameObject[] players;
        try
        {
            players = GameObject.FindGameObjectsWithTag("Player");
        }
        catch (UnityException)
        {
            return;
        }

        float bestDistance = float.MaxValue;
        Transform bestTarget = null;
        foreach (GameObject playerObject in players)
        {
            if (playerObject == null || !playerObject.activeInHierarchy)
                continue;

            float distance = Vector3.SqrMagnitude(playerObject.transform.position - transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = playerObject.transform;
            }
        }

        if (bestTarget != null)
        {
            player = bestTarget;
        }
    }

    void MoveTowardPlayer()
    {
        if (player == null)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            player.position,
            moveSpeed * Time.deltaTime
        );
    }

    void TryShoot()
    {
        if (Time.time < nextFireTime)
            return;

        Shoot();
        nextFireTime = Time.time + fireRate;
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
            return;

        if (NetworkCoopGameRuntime.SpawnEnemyBullet(firePoint.position, firePoint.rotation, bulletPrefab))
            return;

        RuntimeObjectPool.Spawn(bulletPrefab, firePoint.position, firePoint.rotation);
        CombatEffects.SpawnEnemyMuzzleFlash(firePoint.position, firePoint.rotation);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CrashIntoPlayer(other.transform);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            CrashIntoPlayer(collision.transform);
        }
    }

    private void CrashIntoPlayer(Transform playerTransform)
    {
        if (hasCrashedIntoPlayer)
            return;

        if (NetworkCoopGameRuntime.IsActive)
        {
            PhotonView targetView = playerTransform != null ? playerTransform.GetComponentInParent<PhotonView>() : null;
            if (targetView == null || !targetView.IsMine)
                return;
        }

        hasCrashedIntoPlayer = true;

        Vector3 crashPoint = playerTransform != null
            ? Vector3.Lerp(transform.position, playerTransform.position, 0.5f)
            : transform.position;

        if (NetworkCoopGameRuntime.ReportPlayerCrashed(100, crashPoint))
        {
            NetworkCoopEnemyIdentity identity = GetComponent<NetworkCoopEnemyIdentity>();
            if (identity != null)
            {
                NetworkCoopGameRuntime.ReportEnemyDestroyed(identity, crashPoint, -transform.forward, 0, 0, false);
            }
            return;
        }

        CombatEffects.SpawnExplosion(crashPoint);
        BloodManager.blood = 0;
        Destroy(gameObject);
    }

    private void CheckLocalPlayerCrash()
    {
        if (hasCrashedIntoPlayer || NetworkPlayerController.LocalPlayer == null)
            return;

        if (IsTouchingLocalPlayer(NetworkPlayerController.LocalPlayer))
        {
            CrashIntoPlayer(NetworkPlayerController.LocalPlayer.transform);
        }
    }

    private bool IsTouchingLocalPlayer(NetworkPlayerController localPlayer)
    {
        if (localPlayer == null)
            return false;

        Collider[] playerColliders = localPlayer.GetComponentsInChildren<Collider>();
        foreach (Collider enemyCollider in enemyColliders)
        {
            if (enemyCollider == null || !enemyCollider.enabled)
                continue;

            foreach (Collider playerCollider in playerColliders)
            {
                if (playerCollider == null || !playerCollider.enabled)
                    continue;

                if (enemyCollider.bounds.Intersects(playerCollider.bounds))
                    return true;

                Vector3 enemyPoint = enemyCollider.ClosestPoint(playerCollider.bounds.center);
                Vector3 playerPoint = playerCollider.ClosestPoint(enemyPoint);
                if (Vector3.SqrMagnitude(enemyPoint - playerPoint) <= 4f)
                    return true;
            }
        }

        return false;
    }

    private void EnsurePhysicsCallbacks()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.useGravity = false;
        body.isKinematic = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }
}
