using UnityEngine;

namespace OutlandHaven.Inventory
{
    /// <summary>
    /// Component added to dropped WorldItems that automatically pulls them toward the player when in range,
    /// and automatically deposits them in the inventory once they arrive.
    /// </summary>
    [RequireComponent(typeof(WorldItem), typeof(Collider2D))]
    public class WorldItemMagnet : MonoBehaviour
    {
        [Header("Magnet Settings")]
        [Tooltip("The maximum distance from the player before the item starts being attracted.")]
        [SerializeField] private float _detectRange = 5f;

        [Tooltip("The starting speed of the item's movement toward the player.")]
        [SerializeField] private float _initialSpeed = 1.5f;

        [Tooltip("How fast the item accelerates as it moves closer to the player.")]
        [SerializeField] private float _acceleration = 6f;

        [Tooltip("The distance from the player at which the item is automatically picked up and added to inventory.")]
        [SerializeField] private float _collectDistance = 0.25f;

        private WorldItem _worldItem;
        private Collider2D _collider;
        private Transform _playerTransform;
        private InventoryManager _playerInventory;
        private float _currentSpeed;
        private bool _isAttracted;

        private void Awake()
        {
            _worldItem = GetComponent<WorldItem>();
            _collider = GetComponent<Collider2D>();
            _currentSpeed = _initialSpeed;
        }

        private void Update()
        {
            // Do not pull if the Loot Magnet feature is disabled in settings
            if (!LootMagnetSettings.LootMagnetEnabled)
                return;

            // Do not pull the item if it has not landed yet (WorldItemDropPresentation disables the collider while traveling)
            if (_collider != null && !_collider.enabled)
                return;

            // Resolve player references dynamically
            if (_playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _playerTransform = player.transform;
                    player.TryGetComponent(out _playerInventory);
                    
                    // Fallback to the central resolver if player inventory is not directly on the root
                    if (_playerInventory == null)
                    {
                        _playerInventory = PlayerInventorySceneResolver.ResolvePlayerInventory(null, null);
                    }
                }
            }

            if (_playerTransform == null)
                return;

            // Check distance to player
            Vector3 playerPosition = _playerTransform.position;
            float sqrDistance = (playerPosition - transform.position).sqrMagnitude;
            float sqrDetectRange = _detectRange * _detectRange;

            if (!_isAttracted)
            {
                if (sqrDistance <= sqrDetectRange)
                {
                    _isAttracted = true;
                }
            }

            if (_isAttracted)
            {
                MoveTowardPlayer(playerPosition);
                CheckAutoCollect(sqrDistance);
            }
        }

        private void MoveTowardPlayer(Vector3 playerPosition)
        {
            // Accelerate speed over time
            _currentSpeed += _acceleration * Time.deltaTime;

            // Move the object toward the player
            Vector3 direction = (playerPosition - transform.position).normalized;
            transform.position += direction * _currentSpeed * Time.deltaTime;
        }

        private void CheckAutoCollect(float sqrDistance)
        {
            float sqrCollectDistance = _collectDistance * _collectDistance;
            if (sqrDistance <= sqrCollectDistance)
            {
                if (_playerInventory != null)
                {
                    // Attempt to add the item directly to the player's inventory
                    bool success = _worldItem.Interact(_playerInventory);
                    
                    if (success)
                    {
                        // Interact handles audio, facts, and calls Destroy(gameObject) on success.
                        // We destroy the magnet component as well to avoid multi-pickup frames.
                        enabled = false;
                    }
                    else
                    {
                        // If inventory is full, stop pulling the item to prevent clipping/floating behind player forever
                        _isAttracted = false;
                        _currentSpeed = _initialSpeed;
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw visual detection range inside Unity editor when selected
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _detectRange);
        }
#endif
    }
}
