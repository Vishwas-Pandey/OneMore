using UnityEngine;

/// <summary>
/// Attached to every obstacle prefab. Handles per-instance difficulty scaling
/// and optional horizontal movement; returns itself to the ObjectPool when it
/// scrolls off-screen instead of being destroyed.
/// </summary>
[RequireComponent(typeof(Poolable))]
public class Obstacle : MonoBehaviour
{
    [SerializeField] private float baseSpeed = 3f;
    [SerializeField] private float despawnX = -12f;

    private float difficulty = 1f;
    private bool isMoving;
    private float moveSpeed;
    private ObjectPool ownerPool;

    public void SetDifficulty(float value) => difficulty = value;

    public void SetMovement(bool moving, float speed)
    {
        isMoving = moving;
        moveSpeed = speed;
    }

    public void SetOwnerPool(ObjectPool pool) => ownerPool = pool;

    private void Update()
    {
        float speed = baseSpeed * difficulty + (isMoving ? moveSpeed : 0f);
        transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);

        if (transform.position.x < despawnX)
        {
            if (ownerPool != null)
                ownerPool.ReturnObject(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}
