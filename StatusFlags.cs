using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemindMe {
    [Flags]
    public enum StatusFlags : byte {
        None = 0,
        Hostile = 1 << 0,
        InCombat = 1 << 1,
        WeaponOut = 1 << 2,
        PartyMember = 1 << 4,
        AllianceMember = 1 << 5,
        Friend = 1 << 6,
        Casting = 1 << 7
    }
}
