using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemDisplay.Service
{
    internal class InventoryService
    {
        internal static unsafe async Task<int> GetItemCount(uint itemId)
        {
            int itemCount = 0;
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory1), itemId);
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory2), itemId);
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory3), itemId);
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory4), itemId);
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.SaddleBag1), itemId);
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.SaddleBag2), itemId);
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.PremiumSaddleBag1), itemId);
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.PremiumSaddleBag2), itemId);
            return itemCount;
        }

        internal static unsafe int GetItemInInventory(InventoryContainer* inventoryPointer, uint itemId)
        {
            int itemQ = 0;
            for (int i = 0; i < inventoryPointer->Size; i++)
            {
                var item = inventoryPointer->Items[i];
                if (item.GetBaseItemId() == itemId)
                {
                    itemQ += item.Quantity;
                }
            }
            return itemQ;
        }
    }
}
