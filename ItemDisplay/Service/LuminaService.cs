using System.Collections.Generic;
using Lumina.Excel.Sheets;
using ECommons.DalamudServices;
using System.Linq;

namespace ItemDisplay.Service
{
    internal class LuminaService
    {
        public static Dictionary<uint, Item>? ItemSheet;
        public static Dictionary<uint, Marker>? MarkerSheet;
        public static Dictionary<uint, MacroIcon>? MacroIconSheet;
        public static Dictionary<uint, ContentFinderCondition>? ContentFinderConditionSheet;

        private static readonly List<uint> AcceptedContentTypes =
        [
            2, 3, 4, 5, 6, 21, 28, 30, 37
        ];

        public static void Init()
        {
            ItemSheet = Svc.Data?.GetExcelSheet<Item>()?
                       .ToDictionary(i => i.RowId, i => i);

            MarkerSheet = Svc.Data?.GetExcelSheet<Marker>()?
                       .ToDictionary(i => i.RowId, i => i);

            MacroIconSheet = Svc.Data?.GetExcelSheet<MacroIcon>()?
                       .ToDictionary(i => i.RowId, i => i);

            ContentFinderConditionSheet = Svc.Data?.GetExcelSheet<ContentFinderCondition>()?
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

        public static List<IconSelectorChoice> GetItemIconList()
        {
            return ItemSheet?.Values.Where(x => x.Icon != 0).Select(x => new IconSelectorChoice(x)).ToList() ?? [];
        }

        public static List<IconSelectorChoice> GetMacroIconList()
        {
            return MacroIconSheet?.Values.Where(x => x.Icon != 0).Select(x => new IconSelectorChoice(x)).ToList() ?? [];
        }

        public static List<IconSelectorChoice> GetMarkerIconList()
        {
            return MarkerSheet?.Values.Where(x => x.Icon != 0).Select(x => new IconSelectorChoice(x)).ToList() ?? [];
        }

        public static List<ContentSelection> GetContent()
        {
            return ContentFinderConditionSheet?.Values.Where(x => AcceptedContentTypes.Contains(x.ContentType.RowId)).Select(x => new ContentSelection(x)).ToList() ?? [];
        }
    }

    internal struct ContentSelection
    {
        internal ContentSelection(ContentFinderCondition cfc)
        {
            ContentId = cfc.RowId;
            ContentName = cfc.Name.ExtractText();
            ContentType = cfc.ContentType.Value.Name.ExtractText();
        }

        internal uint ContentId;
        internal string ContentName;
        internal string ContentType;
    }

    internal struct IconSelectorChoice
    {
        internal IconSelectorChoice(Item item)
        {
            ItemId = item.RowId;
            IconId = item.Icon;
            ItemName = item.Name.ExtractText();
        }

        internal IconSelectorChoice(Marker marker)
        {
            IconId = (uint)marker.Icon;
        }

        internal IconSelectorChoice(MacroIcon mIcon)
        {
            IconId = (uint)mIcon.Icon;
        }

        internal uint ItemId;
        internal uint IconId;
        internal string ItemName = string.Empty;
    }
}
