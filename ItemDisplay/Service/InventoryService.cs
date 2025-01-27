using ECommons.DalamudServices;
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
        internal static unsafe int GetInventoryItemCount(uint itemId)
        {
            int itemCount = 0;
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory1), itemId);
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory2), itemId);
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory3), itemId);
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory4), itemId);
            return itemCount;
        }

        internal static unsafe int GetSaddlebagItemCount(uint itemId)
        {
            int itemCount = 0;
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.SaddleBag1), itemId);
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.SaddleBag2), itemId);
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.PremiumSaddleBag1), itemId);
            itemCount += GetItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.PremiumSaddleBag2), itemId);
            return itemCount;
        }

        internal static unsafe bool InventoryAccess()
        {
            return 
                InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory1) != null;
        }

        internal static unsafe bool SaddlebagAccess()
        {
            return
                InventoryManager.Instance()->GetInventoryContainer(InventoryType.SaddleBag1) != null;
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

        internal static unsafe void DebugSaddlebag()
        {
            var saddleBag = InventoryManager.Instance()->GetInventoryContainer(InventoryType.SaddleBag1);
            Svc.Log.Debug($"[DEBUG SADDLEBAG] {saddleBag->Loaded}");
            Svc.Log.Debug($"[DEBUG SADDLEBAG] {saddleBag->Size}");
        }
    }
}
