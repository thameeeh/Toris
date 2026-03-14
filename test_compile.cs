using System;
using System.Collections.Generic;
using UnityEngine;
using OutlandHaven.UIToolkit;

public class TestCompile {
    public void Test() {
        InventoryItemSO item = ScriptableObject.CreateInstance<InventoryItemSO>();
        var comp = item.GetComponent<UpgradeableComponent>();
    }
}
