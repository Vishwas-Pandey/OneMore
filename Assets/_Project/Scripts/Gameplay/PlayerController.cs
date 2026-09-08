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
    [SerializeField] private float flapVelocity = 5f;
    [SerializeField] private float gravityScale = 1.3f;
    [SerializeField] private float maxFallSpeed = -5.5f;
    [SerializeField] private float maxRiseSpeed = 6f;
    [SerializeField] private float tiltPerVelocity = 6f;
    [SerializeField] private float maxTiltUp = 20f;
    [SerializeField] private float maxTiltDown = -55f;
    [SerializeField] private float tiltSmoothSpeed = 7f;

    [Header("Visual Settings")]
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private ParticleSystem deathParticles;
    [SerializeField] private Animator animator;

    [Header("Scene References")]
    [SerializeField] private ScoreManager scoreManager;

    private Rigidbody2D rb;

    public event System.Action OnJump;
    public event System.Action OnDeath;

    public bool IsAlive { get; private set; } = true;
    public Vector2 Velocity => rb.linearVelocity;

    /// <summary>
    /// True only once BeginFlight() has been called (i.e. the countdown has
    /// finished). Gravity/input/death are all gated on this so the bird just
    /// hovers in place during the pre-game countdown instead of immediately
    /// falling into the ground/ceiling before the player can react.
    /// </summary>
    private bool canFly;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Update()
    {
        if (!IsAlive || !canFly) return;
        if (GameManager.Instance != null && GameManager.Instance.IsPaused()) return;

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
        if (!IsAlive || !canFly) return;

        var v = rb.linearVelocity;
        v.y = Mathf.Clamp(v.y, maxFallSpeed, maxRiseSpeed);
        rb.linearVelocity = v;
    }

    /// <summary>Called once the countdown finishes and gameplay actually begins.</summary>
    public void BeginFlight()
    {
        canFly = true;
        rb.gravityScale = gravityScale;
    }

    private void Flap()
    {
        rb.linearVelocity = new Vector2(0f, flapVelocity);

        if (jumpParticles != null) jumpParticles.Play();
        AudioManager.Instance?.PlayJumpSound();
        HapticManager.Instance?.VibrateLight();

        OnJump?.Invoke();
    }

    private void UpdateTilt()
    {
        // Slerp toward the target angle instead of snapping to it instantly -
        // an instant snap is what reads as a rigid "ball bounce"; easing
        // toward the target each frame gives the smoother, flowing rotation
        // of an actual gliding bird.
        float targetAngle = Mathf.Clamp(rb.linearVelocity.y * tiltPerVelocity, maxTiltDown, maxTiltUp);
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, tiltSmoothSpeed * Time.deltaTime);
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat("verticalSpeed", rb.linearVelocity.y);
        animator.SetBool("isAlive", IsAlive);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsAlive || !canFly) return;

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

        int finalScore = scoreManager != null ? scoreManager.GetCurrentScore() : 0;
        bool isNewBest = scoreManager != null && scoreManager.IsNewBest();
        GameManager.Instance?.GameOver(finalScore, isNewBest);
    }

    public void ResetPlayer(Vector3 position)
    {
        transform.position = position;
        transform.rotation = Quaternion.identity;
        IsAlive = true;
        canFly = false;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        if (animator != null) animator.SetBool("isAlive", true);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.32f);
    }
}
