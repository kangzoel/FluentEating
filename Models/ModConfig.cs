using StardewModdingAPI;

namespace FluentEating.Models
{
    internal class ModConfig
    {
        public SButton KeyBind_ToggleEnabled { get; set; } = SButton.F10;
        public bool Enabled { get; set; } = true;

        public int AutoEatStaminaThreshold { get; set; } = 0;
        public int AutoEatHealthThreshold { get; set; } = 20;

        public SButton KeyBind_ToggleMaintainBuff { get; set; } = SButton.F11;
        public bool MaintainBuff { get; set; } = false;
    }
}
