using UnityEngine;

/// <summary>
/// Classic flappy-bird physics: continuous gravity fall, each tap sets
/// velocity directly to a fixed upward flap strength (not an accumulating
/// force) - matching the reference implementation's feel exactly. No ground,
/// no coyote time, no jump cooldown; death is any collision (pipe/ground/
/// ceiling) while alive.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Flap Physics")]
    [SerializeField] private float flapVelocity = 5.5f;
    [SerializeField] private float gravityScale = 2.6f;
    [SerializeField] private float maxFallSpeed = -9f;
    [SerializeField] private float maxRiseSpeed = 6.5f;
    [SerializeField] private float tiltPerVelocity = 6f;
    [SerializeField] private float maxTiltUp = 25f;
    [SerializeField] private float maxTiltDown = -70f;

    [Header("Visual Settings")]
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private ParticleSystem deathParticles;
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;

    public event System.Action OnJump;
    public event System.Action OnDeath;

    public bool IsAlive { get; private set; } = true;
    public Vector2 Velocity => rb.velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = gravityScale;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Update()
    {
        if (!IsAlive) return;

        bool tapped = Input.GetMouseButtonDown(0);
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            tapped = true;

        if (tapped)
        {
            Flap();
        }

        UpdateTilt();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (!IsAlive) return;

        var v = rb.velocity;
        v.y = Mathf.Clamp(v.y, maxFallSpeed, maxRiseSpeed);
        rb.velocity = v;
    }

    private void Flap()
    {
        rb.velocity = new Vector2(0f, flapVelocity);

        if (jumpParticles != null) jumpParticles.Play();
        AudioManager.Instance?.PlayJumpSound();
        HapticManager.Instance?.VibrateLight();

        OnJump?.Invoke();
    }

    private void UpdateTilt()
    {
        float targetAngle = Mathf.Clamp(rb.velocity.y * tiltPerVelocity, maxTiltDown, maxTiltUp);
        transform.rotation = Quaternion.Euler(0, 0, targetAngle);
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat("verticalSpeed", rb.velocity.y);
        animator.SetBool("isAlive", IsAlive);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsAlive) return;

        if (other.CompareTag("Obstacle") || other.CompareTag("Ground") || other.CompareTag("Ceiling"))
        {
            Die();
        }
    }

    private void Die()
    {
        if (!IsAlive) return;

        IsAlive = false;

        if (deathParticles != null)
        {
            deathParticles.transform.position = transform.position;
            deathParticles.Play();
        }

        HapticManager.Instance?.VibrateStrong();
        AudioManager.Instance?.PlayGameOverSound();

        OnDeath?.Invoke();
        GameManager.Instance?.GameOver();
    }

    public void ResetPlayer(Vector3 position)
    {
        transform.position = position;
        transform.rotation = Quaternion.identity;
        IsAlive = true;
        rb.velocity = Vector2.zero;

        if (animator != null) animator.SetBool("isAlive", true);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.32f);
    }
}
