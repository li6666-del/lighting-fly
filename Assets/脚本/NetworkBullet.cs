using Photon.Pun;
using UnityEngine;

public class NetworkBullet : MonoBehaviourPun
{
    public float speed = 260f;
    public float lifeTime = 3f;

    void Start()
    {
        CombatEffects.AttachBulletTrail(gameObject, new Color(0.15f, 0.95f, 1f, 1f), 5f, 0.12f);

        if (photonView.IsMine)
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
        if (photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
