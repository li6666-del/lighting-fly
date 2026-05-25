using Photon.Pun;
using UnityEngine;

public class NetworkBullet : MonoBehaviourPun
{
    public float speed = 260f;
    public float lifeTime = 3f;
    public int scoreValue = 1;
    public int healValue = 5;
    public bool grantsSkillCharge = true;

    private bool isDestroyed;
    private bool isLocalSimulation;
    private int ownerActorNumber = -1;

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
    }

    public void InitializeLocal(int bulletOwnerActorNumber, bool skillGrantsCharge)
    {
        isLocalSimulation = true;
        ownerActorNumber = bulletOwnerActorNumber;
        grantsSkillCharge = skillGrantsCharge;
    }

    void Start()
    {
        object[] instantiateData = !isLocalSimulation && photonView != null ? photonView.InstantiationData : null;
        if (instantiateData != null && instantiateData.Length > 0 && instantiateData[0] is bool skillGrantsCharge)
        {
            grantsSkillCharge = skillGrantsCharge;
        }

        CombatEffects.AttachBulletTrail(gameObject, new Color(0.15f, 0.95f, 1f, 1f), 5f, 0.12f);

        if (isLocalSimulation || photonView == null || photonView.ViewID == 0 || photonView.IsMine)
        {
            Invoke(nameof(DestroyNetworkBullet), lifeTime);
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
            Destroy(gameObject);
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
            bool bossHandled = NetworkCoopGameRuntime.ReportBossDamaged(bossIdentity, 1, bossHitPosition);
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
}
