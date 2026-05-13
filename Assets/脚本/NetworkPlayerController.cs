using Photon.Pun;
using UnityEngine;

public class NetworkPlayerController : MonoBehaviourPun, IPunObservable
{
    [Header("Movement")]
    public float moveSpeed = 400f;
    public float leftLimit = -250f;
    public float rightLimit = 150f;

    [Header("Shooting")]
    public string networkBulletPrefabName = "NetworkPlayerBullet";
    public Transform firePoint;
    public float fireRate = 0.2f;

    private float nextFireTime;
    private Vector3 networkPosition;
    private Quaternion networkRotation;

    void Start()
    {
        networkPosition = transform.position;
        networkRotation = transform.rotation;

        if (!photonView.IsMine)
        {
            DisableRemoteInputComponents();
        }
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            MoveLocalPlayer();
            TryShoot();
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 12f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 12f);
        }
    }

    void MoveLocalPlayer()
    {
        float input = Input.GetAxis("Horizontal");
        float newX = transform.position.x + input * moveSpeed * Time.deltaTime;
        newX = Mathf.Clamp(newX, leftLimit, rightLimit);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    void TryShoot()
    {
        if (!Input.GetKey(KeyCode.Space) || Time.time < nextFireTime)
            return;

        Transform origin = firePoint != null ? firePoint : transform;
        PhotonNetwork.Instantiate(networkBulletPrefabName, origin.position, origin.rotation);
        CombatEffects.SpawnMuzzleFlash(origin.position, origin.rotation);
        nextFireTime = Time.time + fireRate;
    }

    void DisableRemoteInputComponents()
    {
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        FireLogic fireLogic = GetComponent<FireLogic>();
        if (fireLogic != null)
        {
            fireLogic.enabled = false;
        }

        PlayerSkill playerSkill = GetComponent<PlayerSkill>();
        if (playerSkill != null)
        {
            playerSkill.enabled = false;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }
}
