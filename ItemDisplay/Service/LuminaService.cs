using System.Collections.Generic;
using Lumina.Excel.Sheets;
using ECommons.DalamudServices;
using System.Linq;

namespace ItemDisplay.Service
{
    internal class LuminaService
    {
        public static Dictionary<uint, Item>? ItemSheet;

        public static void Init()
        {
            ItemSheet = Svc.Data?.GetExcelSheet<Item>()?
                       .ToDictionary(i => i.RowId, i => i);
        }

        public static uint GetItemIdByItemName(string itemName)
        {
            var item = ItemSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == itemName.ToLower());
            return item.HasValue ? item.Value.Key : 0;
        }

        public static ushort GetIconId(uint itemId)
        {
            var item = ItemSheet?.FirstOrDefault(x => x.Key == itemId);
            return item.HasValue ? item.Value.Value.Icon : (ushort)0;
        }
    }
}
