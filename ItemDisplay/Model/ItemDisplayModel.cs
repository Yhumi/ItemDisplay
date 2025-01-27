using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemDisplay.Model
{
    public class ItemDisplayModel
    {
        public string ItemName { get; set; }
        public uint ItemId { get; set; }

        public int ItemCount { get { return InventoryCount + SaddlebagCount; } }
        public int InventoryCount { get; set; } = 0;
        public int SaddlebagCount { get; set; } = 0;

        public string? TextCommand { get; set; }
        public bool ShowDisplay { get; set; } = true;
        public float Scale { get; set; } = 1f;
    }
}
