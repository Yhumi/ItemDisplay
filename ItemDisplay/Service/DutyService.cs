using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemDisplay.Service
{
    internal static unsafe class DutyService
    {
        internal static unsafe uint GetCurrentInstanceId()
        {
            if(GameMain.Instance()->IsInInstanceArea())
                return GameMain.Instance()->CurrentContentFinderConditionId;
            return 0;
        }
    }
}
