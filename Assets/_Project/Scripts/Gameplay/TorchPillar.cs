using UnityEngine;

/// <summary>
/// One torch pillar (top-hanging or bottom-standing half of a pipe pair).
/// The shaft is a child sprite stretched via localScale to reach whatever
/// height this spawn needs; the flame cap is a fixed-size child repositioned
/// to sit at the open end of the shaft. Moves left at the spawner-assigned
/// speed and returns itself to the pool once off-screen.
/// </summary>
[RequireComponent(typeof(Poolable))]
public class TorchPillar : MonoBehaviour
{
    [SerializeField] private Transform shaft;
    [SerializeField] private Transform flameCap;
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private float despawnX = -8f;

    private float speed;
    private ObjectPool ownerPool;
    private bool isTopPillar;
    private float shaftNativeHeight = 1f;

    public void SetOwnerPool(ObjectPool pool) => ownerPool = pool;
    public void SetSpeed(float value) => speed = value;

    private void Awake()
    {
        // localScale is a multiplier on the sprite's own native size, not a
        // direct world-unit length - cache the native height once so Configure
        // can scale to an exact world-unit target instead of silently
        // under/over-shooting by whatever the source sprite's PPU happens to be.
        var sr = shaft.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            shaftNativeHeight = sr.sprite.bounds.size.y;
        }
    }

    /// <summary>
    /// height: world-unit length of the shaft. isTop: true if this pillar
    /// hangs from the ceiling (shaft grows downward, flame at the bottom
    /// edge); false if it stands from the floor (shaft grows upward, flame
    /// at the top edge). anchorY: world Y of the screen edge this pillar
    /// mounts to (ceiling or floor line).
    /// </summary>
    public void Configure(float height, bool isTop, float anchorY)
    {
        isTopPillar = isTop;
        float dir = isTop ? -1f : 1f;

        float scaleY = shaftNativeHeight > 0f ? height / shaftNativeHeight : height;
        shaft.localScale = new Vector3(shaft.localScale.x, scaleY, shaft.localScale.z);
        shaft.localPosition = new Vector3(0f, dir * height * 0.5f, 0f);

        if (flameCap != null)
        {
            flameCap.localPosition = new Vector3(0f, dir * height, 0f);

            var flameSr = flameCap.GetComponent<SpriteRenderer>();
            if (flameSr != null) flameSr.flipY = isTop;
        }

        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(boxCollider.size.x, height);
            boxCollider.offset = new Vector2(0f, dir * height * 0.5f);
        }

        transform.position = new Vector3(transform.position.x, anchorY, transform.position.z);
    }

    private void Update()
    {
        transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);

        if (transform.position.x < despawnX)
        {
            if (ownerPool != null)
                ownerPool.ReturnObject(gameObject);
            else
                gameObject.SetActive(false);
        }
    }

    public float GetX() => transform.position.x;
}
