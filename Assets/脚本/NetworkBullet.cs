using Photon.Pun;
using UnityEngine;

public class NetworkBullet : MonoBehaviourPun, IPooledObject
{
    public float speed = 260f;
    public float lifeTime = 3f;
    public int scoreValue = 1;
    public int healValue = 5;
    public bool grantsSkillCharge = true;

    private bool isDestroyed;
    private bool isLocalSimulation;
    private int ownerActorNumber = -1;
    private bool spawnedFromPool;
    private bool defaultsCaptured;
    private bool defaultGrantsSkillCharge;
    private PlayerShipVisualTheme visualTheme;

    void Awake()
    {
        BulletLogic legacyBulletLogic = GetComponent<BulletLogic>();
        if (legacyBulletLogic != null)
        {
            scoreValue = legacyBulletLogic.scoreValue;
            healValue = legacyBulletLogic.healValue;
            grantsSkillCharge = legacyBulletLogic.grantsSkillCharge;
            legacyBulletLogic.enabled = false;
        }

        CaptureDefaults();
    }

    public void InitializeLocal(int bulletOwnerActorNumber, bool skillGrantsCharge)
    {
        InitializeLocal(
            bulletOwnerActorNumber,
            skillGrantsCharge,
            NetworkPlayerController.GetNetworkThemeForActor(bulletOwnerActorNumber));
    }

    public void InitializeLocal(int bulletOwnerActorNumber, bool skillGrantsCharge, PlayerShipVisualTheme theme)
    {
        isLocalSimulation = true;
        ownerActorNumber = bulletOwnerActorNumber;
        grantsSkillCharge = skillGrantsCharge;
        visualTheme = theme;
        ApplyTrail();
    }

    void Start()
    {
        if (!spawnedFromPool)
        {
            ResetBulletStateFromRuntime();
        }
    }

    public void OnSpawnedFromPool()
    {
        spawnedFromPool = true;
        ResetBulletStateFromRuntime();
    }

    public void OnReturnedToPool()
    {
        CancelInvoke(nameof(DestroyNetworkBullet));
        isDestroyed = false;
        isLocalSimulation = false;
        ownerActorNumber = -1;
        grantsSkillCharge = defaultGrantsSkillCharge;
        visualTheme = PlayerShipColorSelection.BlueTheme;
        ClearTrail();
    }

    private void CaptureDefaults()
    {
        if (defaultsCaptured)
            return;

        defaultGrantsSkillCharge = grantsSkillCharge;
        defaultsCaptured = true;
    }

    private void ResetBulletStateFromRuntime()
    {
        CaptureDefaults();
        isDestroyed = false;
        isLocalSimulation = false;
        ownerActorNumber = -1;
        grantsSkillCharge = defaultGrantsSkillCharge;
        visualTheme = PlayerShipColorSelection.BlueTheme;

        object[] instantiateData = !isLocalSimulation && photonView != null ? photonView.InstantiationData : null;
        if (instantiateData != null && instantiateData.Length > 0 && instantiateData[0] is bool skillGrantsCharge)
        {
            grantsSkillCharge = skillGrantsCharge;
        }

        ApplyTrail();

        if (isLocalSimulation || photonView == null || photonView.ViewID == 0 || photonView.IsMine)
        {
            CancelInvoke(nameof(DestroyNetworkBullet));
            Invoke(nameof(DestroyNetworkBullet), lifeTime);
        }
    }

    private void ClearTrail()
    {
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();
        }
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void DestroyNetworkBullet()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;

        if (isLocalSimulation || photonView == null || photonView.ViewID == 0)
        {
            RuntimeObjectPool.Release(gameObject);
            return;
        }

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDestroyed || !CanApplyDamage())
            return;

        BossController boss = other.GetComponentInParent<BossController>();
        if (boss != null)
        {
            NetworkCoopBossIdentity bossIdentity = boss.GetComponent<NetworkCoopBossIdentity>();
            Vector3 bossHitPosition = other.ClosestPoint(transform.position);
            bool bossHandled = NetworkCoopGameRuntime.ReportBossDamaged(bossIdentity, 1, bossHitPosition, ownerActorNumber);
            if (bossHandled)
            {
                DestroyNetworkBullet();
            }

            return;
        }

        if (!other.CompareTag("Enemy"))
            return;

        NetworkCoopEnemyIdentity enemy = other.GetComponentInParent<NetworkCoopEnemyIdentity>();
        if (enemy == null)
            return;

        Vector3 hitPosition = other.ClosestPoint(transform.position);
        bool handled = NetworkCoopGameRuntime.ReportEnemyDestroyed(
            enemy,
            hitPosition,
            -transform.forward,
            scoreValue,
            healValue,
            grantsSkillCharge
        );

        if (handled)
        {
            DestroyNetworkBullet();
        }
    }

    private bool CanApplyDamage()
    {
        if (isLocalSimulation)
        {
            return ownerActorNumber <= 0
                || PhotonNetwork.LocalPlayer == null
                || PhotonNetwork.LocalPlayer.ActorNumber == ownerActorNumber;
        }

        return photonView != null && photonView.IsMine;
    }

    private void ApplyTrail()
    {
        CombatEffects.AttachBulletTrail(gameObject, visualTheme.BulletTrail, 5f, 0.12f);
        ClearTrail();
    }
}
