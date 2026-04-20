using OutlandHaven.UIToolkit;
using UnityEngine;

namespace OutlandHaven.Inventory
{

    public class ItemPicker : MonoBehaviour
    {
        [SerializeField] private ItemPickEventSO _itemPickerSO;
        [Tooltip("The inventory this picker will send items to.")]
        [SerializeField] private InventoryManager _myInventoryManager;

        [Header("Detection Settings")]
        [SerializeField] private Transform _interactionPoint;
        [SerializeField] private float _radius = 1.5f;
        [SerializeField] private LayerMask _layerMask;

        [Header("UI Settings")]
        [SerializeField] private InteractionPromptUI _interactionUI;

        private IContainerInteractable _currentSelection;

        private void Awake()
        {
            ResolveRuntimeReferences(logErrors: true);
        }

        private void OnValidate()
        {
            if (_itemPickerSO == null)
            {
                Debug.LogError($"<b><color=red>[ItemPicker]</color></b> is missing ItemPickEventSO on GameObject: <b>{name}<b>", this);
            }

            if (!gameObject.scene.IsValid())
                return;

            if (_myInventoryManager == null)
            {
                Debug.LogWarning($"<b><color=yellow>[InventoryManager]</color></b> is not assigned on GameObject: <b>{name}</b>. ItemPicker will try to auto-resolve the player inventory at runtime.", this);
            }
            if (_interactionUI == null)
            {
                Debug.LogWarning($"<b><color=teal>[InteractionUI]</color></b> is not assigned on GameObject: <b>{name}</b>. ItemPicker will try to auto-resolve the prompt UI at runtime.", this);
            }
        }

        private void OnEnable()
        {
            if (_itemPickerSO != null)
                _itemPickerSO.OnItemPick += PickItem;
        }

        private void OnDisable()
        {
            if (_itemPickerSO != null)
                _itemPickerSO.OnItemPick -= PickItem;
        }

        void Update()
        {
            ResolveRuntimeReferences();

            if (_interactionUI == null)
                return;

            // CONSTANTLY scan items for UI
            FindBestInteractable();

            if (_currentSelection != null)
            {
                // PASS DATA TO THE UI
                _interactionUI.DisplayPrompt(
                    _currentSelection.GetInteractionPrompt(), // Text
                    _currentSelection.InteractionPosition     // Location
                );
            }
            else
            {
                _interactionUI.Hide();
            }
        }

        void PickItem()
        {
            if (_currentSelection == null) return;

            ResolveRuntimeReferences(logErrors: true);
            if (_myInventoryManager == null)
                return;

            bool picked = _currentSelection.Interact(_myInventoryManager);

            if (picked)
            {
                _currentSelection = null;
                _interactionUI?.Hide();
            }
        }

        private void FindBestInteractable()
        {
            if (_interactionPoint == null)
            {
                _currentSelection = null;
                return;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(_interactionPoint.position, _radius, _layerMask);

            IContainerInteractable closest = null;
            float minSqrDst = float.MaxValue;
            Vector2 position2D = transform.position;

            foreach (var hit in hits)
            {
                // TryGetComponent does not create garbage like GetComponent does
                if (hit.TryGetComponent(out IContainerInteractable found))
                {
                    //use Vector2 since V3 have depth for sorting layers
                    float sqrDst = (position2D - (Vector2)hit.transform.position).sqrMagnitude;

                    if (sqrDst < minSqrDst)
                    {
                        closest = found;
                        minSqrDst = sqrDst;
                    }
                }
            }

            _currentSelection = closest;
        }

        private void ResolveRuntimeReferences(bool logErrors = false)
        {
            _myInventoryManager = PlayerInventorySceneResolver.ResolvePlayerInventory(this, _myInventoryManager);
            _interactionUI = PlayerInventorySceneResolver.ResolveInteractionPrompt(_interactionUI);

            if (!logErrors)
                return;

            if (_myInventoryManager == null)
            {
                Debug.LogError("ItemPicker cannot resolve a player InventoryManager in the current scene.", this);
            }

            if (_interactionUI == null)
            {
                Debug.LogError("ItemPicker cannot resolve an InteractionPromptUI in the current scene.", this);
            }
        }
    }

}
