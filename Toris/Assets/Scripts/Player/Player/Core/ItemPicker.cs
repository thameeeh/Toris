using OutlandHaven.UIToolkit;
using UnityEngine;

public class ItemPicker : MonoBehaviour
{
    [SerializeField] private ItemPickEventSO _itemPickerSO;
    [SerializeField] private InventoryContainerSO _myInventorySO;

    [Header("Detection Settings")]
    [SerializeField] private Transform _interactionPoint;
    [SerializeField] private float _radius = 1.5f;
    [SerializeField] private LayerMask _layerMask;

    [Header("UI Settings")]
    [SerializeField] private InteractionPromptUI _interactionUI;

    private IContainerInteractable _currentSelection;

    private void OnValidate()
    {
        if (_itemPickerSO == null) 
        {
            Debug.LogError($"<b><color=red>[ItemPicker]</color></b> is missing ItemPickEventSO on GameObject: <b>{name}<b>", this);
        }
        if (_myInventorySO == null)
        {
            Debug.LogError($"<b><color=red>[ItemPicker]</color></b> is missing InventoryContainerSO on GameObject: <b>{name}<b>", this);
        }
        if (_interactionUI == null)
        {
            Debug.LogError($"<b><color=red>[ItemPicker]</color></b> is missing InteractionPromptUI on GameObject: <b>{name}<b>", this);
        }
    }

    private void OnEnable()
    {
        _itemPickerSO.OnItemPick += PickItem;
    }

    private void OnDisable()
    {
        _itemPickerSO.OnItemPick -= PickItem;
    }

    void Update()
    {
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
        _currentSelection.Interact(_myInventorySO);
    }

    private void FindBestInteractable()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(_interactionPoint.position, _radius, _layerMask);

        IContainerInteractable closest = null;
        float minDst = float.MaxValue;

        foreach (var hit in hits)
        {
            // TryGetComponent does not create garbage like GetComponent does
            if (hit.TryGetComponent(out IContainerInteractable found))
            {
                //use Vector2 since V3 have depth for sorting layers
                float dst = Vector2.Distance(transform.position, hit.transform.position);

                if (dst < minDst)
                {
                    closest = found;
                    minDst = dst;
                }
            }
        }

        _currentSelection = closest;
    }
}
