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

    void Start()
    {
        ApplyDifficulty();
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
        MoveTowardPlayer();
        TryShoot();
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

        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        CombatEffects.SpawnMuzzleFlash(firePoint.position, firePoint.rotation);
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

        hasCrashedIntoPlayer = true;

        Vector3 crashPoint = playerTransform != null
            ? Vector3.Lerp(transform.position, playerTransform.position, 0.5f)
            : transform.position;

        CombatEffects.SpawnExplosion(crashPoint);
        BloodManager.blood = 0;
        Destroy(gameObject);
    }
}
