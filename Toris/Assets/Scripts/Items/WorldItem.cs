using UnityEngine;

namespace OutlandHaven.Inventory
{

    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class WorldItem : MonoBehaviour, IContainerInteractable
    {
        [Header("Data")]
        [SerializeField] private InventoryItemSO _itemData;
        [SerializeField] private int _quantity = 1;

        [Header("Quest Facts")]
        [Tooltip("Enable to override the generic PickUp fact with custom quest details. Successful pickups always report a quest fact.")]
        [SerializeField] private bool _reportQuestPickUpFact = false;
        [Tooltip("Custom fact type reported after the item is actually added to inventory. Use PickUp for simple pickups or Collect for collect objectives.")]
        [SerializeField] private QuestFactType _questItemFactType = QuestFactType.PickUp;
        [Tooltip("Stable exact item ID reported to quest rules. Example: LostRelic. Do not rely on GameObject names.")]
        [SerializeField] private string _questItemExactId = string.Empty;
        [Tooltip("Optional item group/type reported to quest rules. Example: Potion, Herb, QuestItem.")]
        [SerializeField] private string _questItemTypeOrTag = string.Empty;
        [Tooltip("Optional pickup context for quest rules. Example: Plains, MainArea, Tutorial.")]
        [SerializeField] private string _questItemContextId = string.Empty;

        public Vector3 InteractionPosition => transform.position + Vector3.up * 1.0f;

        [Header("Visuals")]
        private SpriteRenderer _renderer;
        private Collider2D _collider;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
            ApplyVisuals();
        }

        public void Initialize(InventoryItemSO itemData, int quantity)
        {
            _itemData = itemData;
            _quantity = Mathf.Max(1, quantity);
            ApplyVisuals();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (_itemData == null)
                Debug.LogWarning("<color=red>WorldItem</color> has no item data assigned!", this);
        }
#endif

        public bool Interact(InventoryManager targetContainer)
        {
            if (targetContainer == null) return false;
            if (_itemData == null) return false;

            ItemInstance item = new ItemInstance(_itemData);
            // Attempt to add the item to the container passed in
            bool success = targetContainer.AddItem(item, _quantity);

            if (success)
            {
                // Visual feedback, sound effects go here
                ReportQuestPickUpFactIfNeeded();
                Destroy(gameObject);
                return true;
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("Inventory is full!", this);
#endif
                return false;
            }
        }

        public string GetInteractionPrompt()
        {
            return "E";
        }

        private void ApplyVisuals()
        {
            if (_renderer == null || _itemData == null)
                return;

            _renderer.sprite = _itemData.Icon;
            name = $"WorldItem_{_itemData.ItemName}";
        }

        private void ReportQuestPickUpFactIfNeeded()
        {
            // Quest bridge: item facts are reported only after inventory accepts the item, never on failed pickup attempts.
            PixelCrushersQuestFactReporter.Report(new QuestFact(
                ResolveQuestItemFactType(),
                ResolveQuestItemExactId(),
                ResolveQuestItemTypeOrTag(),
                _quantity,
                _questItemContextId));
        }

        private QuestFactType ResolveQuestItemFactType()
        {
            return _reportQuestPickUpFact ? _questItemFactType : QuestFactType.PickUp;
        }

        private string ResolveQuestItemExactId()
        {
            if (_reportQuestPickUpFact && !string.IsNullOrWhiteSpace(_questItemExactId))
                return _questItemExactId;

            return _itemData != null ? _itemData.name : string.Empty;
        }

        private string ResolveQuestItemTypeOrTag()
        {
            if (_reportQuestPickUpFact && !string.IsNullOrWhiteSpace(_questItemTypeOrTag))
                return _questItemTypeOrTag;

            if (_itemData == null)
                return string.Empty;

            return !string.IsNullOrWhiteSpace(_itemData.ItemName)
                ? _itemData.ItemName
                : _itemData.name;
        }
    }

}
