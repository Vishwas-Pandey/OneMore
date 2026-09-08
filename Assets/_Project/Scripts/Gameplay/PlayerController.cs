using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravityScale = 2f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Visual Settings")]
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private ParticleSystem trailParticles;
    [SerializeField] private ParticleSystem deathParticles;
    [SerializeField] private Animator animator;

    [Header("Jump Settings")]
    [SerializeField] private float jumpCooldown = 0.15f;
    [SerializeField] private float coyoteTime = 0.1f;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool canJump = true;
    private float lastGroundedTime;
    private float jumpTimer;

    public event System.Action OnJump;
    public event System.Action OnLand;
    public event System.Action OnDeath;

    public bool IsGrounded => isGrounded;
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

        if (trailParticles != null) trailParticles.Stop();
    }

    private void Update()
    {
        if (!IsAlive) return;

        UpdateGroundCheck();
        HandleInput();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (!IsAlive) return;
        UpdatePhysics();
    }

    private void UpdateGroundCheck()
    {
        bool wasGrounded = isGrounded;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);
        isGrounded = colliders.Length > 0;

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            if (!wasGrounded)
            {
                OnLand?.Invoke();
                if (trailParticles != null) trailParticles.Stop();
            }
        }

        if (jumpTimer > 0)
            jumpTimer -= Time.deltaTime;
        else
            canJump = true;
    }

    private void HandleInput()
    {
        // Single tap anywhere on screen. Works identically for touch (device)
        // and mouse (editor/simulator) via Unity's input abstraction.
        bool tapped = Input.GetMouseButtonDown(0);

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            tapped = true;

        if (tapped)
        {
            bool canCoyoteJump = canJump && (isGrounded || Time.time - lastGroundedTime <= coyoteTime);
            if (canCoyoteJump)
            {
                PerformJump();
            }
        }
    }

    private void PerformJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);

        canJump = false;
        jumpTimer = jumpCooldown;

        if (jumpParticles != null) jumpParticles.Play();

        AudioManager.Instance?.PlayJumpSound();
        HapticManager.Instance?.VibrateLight();

        OnJump?.Invoke();
    }

    private void UpdatePhysics()
    {
        if (rb.velocity.y < 0)
        {
            rb.gravityScale = gravityScale * 1.5f;
        }
        else if (rb.velocity.y > 0 && !Input.GetMouseButton(0))
        {
            rb.gravityScale = gravityScale * 0.8f;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }

        rb.velocity = new Vector2(Mathf.Clamp(rb.velocity.x, -maxSpeed, maxSpeed), rb.velocity.y);
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("verticalSpeed", rb.velocity.y);
        animator.SetBool("isAlive", IsAlive);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsAlive) return;

        if (other.CompareTag("Obstacle") || other.CompareTag("Ground"))
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
        IsAlive = true;
        canJump = true;
        jumpTimer = 0f;
        rb.velocity = Vector2.zero;

        if (trailParticles != null) trailParticles.Stop();
        if (animator != null) animator.SetBool("isAlive", true);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
