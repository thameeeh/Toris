using NUnit.Framework;
using UnityEngine;
using OutlandHaven.Inventory;
using System.Reflection;
using System.Collections.Generic;

namespace OutlandHaven.UIToolkit.Tests
{
    [TestFixture]
    public class PlayerEquipmentControllerTests
    {
        private GameObject _gameObject;
        private PlayerEquipmentController _controller;
        private InventoryManager _equipmentInventory;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("TestObject");
            _controller = _gameObject.AddComponent<PlayerEquipmentController>();
            _equipmentInventory = _gameObject.AddComponent<InventoryManager>();

            // Use reflection to set private field _equipmentInventory
            var fieldInfo = typeof(PlayerEquipmentController).GetField("_equipmentInventory", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                Assert.Fail("Could not find private field _equipmentInventory in PlayerEquipmentController");
            }
            fieldInfo.SetValue(_controller, _equipmentInventory);

            // Initialize LiveSlots in InventoryManager
            _equipmentInventory.LiveSlots = new List<InventorySlot>();
            for (int i = 0; i < 5; i++)
            {
                _equipmentInventory.LiveSlots.Add(new InventorySlot());
            }
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void IsSlotOccupied_WhenSlotIsEmpty_ReturnsFalse()
        {
            // Arrange
            _controller.RefreshEquipmentState();

            // Act
            bool isOccupied = _controller.IsSlotOccupied(EquipmentSlot.Head);

            // Assert
            Assert.IsFalse(isOccupied);
        }

        [Test]
        public void IsSlotOccupied_WhenSlotIsOccupied_ReturnsTrue()
        {
            // Arrange
            var item = new ItemInstance();
            item.BaseItem = ScriptableObject.CreateInstance<InventoryItemSO>();
            _equipmentInventory.LiveSlots[0].SetItem(item, 1); // 0 is Head
            _controller.RefreshEquipmentState();

            // Act
            bool isOccupied = _controller.IsSlotOccupied(EquipmentSlot.Head);

            // Assert
            Assert.IsTrue(isOccupied);
        }

        [Test]
        public void IsSlotOccupied_WhenSlotIsCleared_ReturnsFalse()
        {
            // Arrange
            var item = new ItemInstance();
            item.BaseItem = ScriptableObject.CreateInstance<InventoryItemSO>();
            _equipmentInventory.LiveSlots[0].SetItem(item, 1); // 0 is Head
            _controller.RefreshEquipmentState();
            Assert.IsTrue(_controller.IsSlotOccupied(EquipmentSlot.Head));

            // Act
            _equipmentInventory.LiveSlots[0].Clear();
            _controller.RefreshEquipmentState();

            // Assert
            bool isOccupied = _controller.IsSlotOccupied(EquipmentSlot.Head);
            Assert.IsFalse(isOccupied);
        }
    }
}
