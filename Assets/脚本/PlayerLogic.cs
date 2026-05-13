using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 400f;
    public float leftLimit = -250f;
    public float rightLimit = 150f;

    void Awake()
    {
        if (GetComponent<PlayerSkill>() == null)
        {
            gameObject.AddComponent<PlayerSkill>();
        }
    }

    void Start()
    {
        FireLogic fireLogic = GetComponent<FireLogic>();
        Transform firePoint = fireLogic != null ? fireLogic.firePoint : null;
        CombatEffects.AttachPlayerEngineJet(gameObject, firePoint);
    }

    void Update()
    {
        HorizontalMove();
    }

    void HorizontalMove()
    {
        float input = Input.GetAxis("Horizontal");
        float newX = transform.position.x + input * moveSpeed * Time.deltaTime;
        newX = Mathf.Clamp(newX, leftLimit, rightLimit);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            new Vector3(leftLimit, transform.position.y - 1f, 0),
            new Vector3(leftLimit, transform.position.y + 1f, 0)
        );
        Gizmos.DrawLine(
            new Vector3(rightLimit, transform.position.y - 1f, 0),
            new Vector3(rightLimit, transform.position.y + 1f, 0)
        );
    }
}
