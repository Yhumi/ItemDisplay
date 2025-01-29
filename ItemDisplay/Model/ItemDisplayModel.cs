using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemDisplay.Model
{
    public class ItemDisplayModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ItemName { get; set; } = string.Empty;
        public uint ItemId { get; set; }

        public string IconReference { get; set; } = string.Empty;

        public ItemDisplayType Type { get; set; }

        public uint IconId { get; set; } = 0;

        public int ItemCount { get { return InventoryCount + SaddlebagCount; } }
        public int InventoryCount { get; set; } = 0;
        public int SaddlebagCount { get; set; } = 0;
        public bool ShowCount { get; set; } = true;

        public string? TextCommand { get; set; }
        public bool ShowDisplay { get; set; } = true;
        public float Scale { get; set; } = 1f;
        public float Opacity { get; set; } = 1f;

        public List<uint> Instances { get; set; } = new();

        public int X { get; set; }
        public int Y { get; set; }
    }

    public enum ItemDisplayType
    {
        Item,
        Icon
    }
}
