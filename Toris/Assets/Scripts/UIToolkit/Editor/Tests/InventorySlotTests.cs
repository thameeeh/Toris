using NUnit.Framework;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.UIToolkit.Tests
{
    public class InventorySlotTests
    {
        [Test]
        public void SetItem_WithValidItemAndPositiveAmount_SetsPropertiesCorrectly()
        {
            // Arrange
            var slot = new InventorySlot();
            var item = new ItemInstance();
            var amount = 5;

            // Act
            slot.SetItem(item, amount);

            // Assert
            Assert.AreSame(item, slot.HeldItem);
            Assert.AreEqual(amount, slot.Count);
        }

        [Test]
        public void SetItem_WithNullItem_SetsHeldItemToNullAndRetainsAmount()
        {
            // Arrange
            var slot = new InventorySlot();
            var amount = 10;

            // Act
            slot.SetItem(null, amount);

            // Assert
            Assert.IsNull(slot.HeldItem);
            Assert.AreEqual(amount, slot.Count);
        }

        [Test]
        public void SetItem_WithNegativeAmount_SetsAmountCorrectly()
        {
            // Arrange
            var slot = new InventorySlot();
            var item = new ItemInstance();
            var amount = -5;

            // Act
            slot.SetItem(item, amount);

            // Assert
            Assert.AreSame(item, slot.HeldItem);
            Assert.AreEqual(amount, slot.Count);
        }
    }
}
