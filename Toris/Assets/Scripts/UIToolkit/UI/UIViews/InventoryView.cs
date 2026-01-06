using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{

    public class InventoryView : GameView
    {
        public override ScreenType ID => ScreenType.Inventory;
        public InventoryView(VisualElement topElement) : base(topElement) { }

        public override void Dispose() 
        {
        
        }

        protected override void SetVisualElements()
        {
            Debug.Log("InventoryView: SetVisualElements called");
        }
    }
}