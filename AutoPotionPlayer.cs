using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace AutoPotion
{
    public class AutoPotionPlayer : ModPlayer
	{
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            base.ProcessTriggers(triggersSet);
            if (Mod is AutoPotionMod AutoPotionMod)
            {
                if (AutoPotionMod.ToggleAutoPotionKeybind.JustPressed)
                {
                    AutoPotionMod.ToggleAutoPotion();
                }
                else if (AutoPotionMod.UseAutoPotionKeybind.JustPressed)
                {
                    if (AutoPotionMod.ConsumePotions())
                    {
                        SoundEngine.PlaySound(SoundID.Item3, Player.position);
                    }
                }
            }
        }
    }
}