using Microsoft.Xna.Framework;
using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace AutoPotion
{
    public class AutoPotionConfig : ModConfig
    {
        public static AutoPotionConfig Instance => ModContent.GetInstance<AutoPotionConfig>();
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(AutoPotionInventory.PiggyBank)]
        public AutoPotionInventory AutoPotionInventory { get; set; }

        [DefaultValue(false)]
        public bool InfinitePotions { get; set; }

        [DefaultValue(false)]
        public bool RemovePotionSickness { get; set; }

        [DefaultValue(false)]
        public bool UseLastPotion { get; set; }

        [DefaultValue(true)]
        public bool DisableOnDeath { get; set; }

        [DefaultValue(false)]
        public bool WhitelistMode { get; set; }

        [DefaultValue(true)]
        public bool PrintEmptyPotions { get; set; }

        [DefaultValue(typeof(Color), "255, 255, 255, 255")]
        public Color PrintColor { get; set; }

        public override void OnChanged()
        {
            base.OnChanged();
            if (InfinitePotions && AutoPotionConfigServer.Instance != null && !AutoPotionConfigServer.Instance.InfinitePotionsAllowed)
            {
                InfinitePotions = false;
            }
            if (RemovePotionSickness && AutoPotionConfigServer.Instance != null && !AutoPotionConfigServer.Instance.RemovePotionSicknessAllowed)
            {
                RemovePotionSickness = false;
            }
        }
    }

    class AutoPotionConfigServer : ModConfig
    {
        public static AutoPotionConfigServer Instance => ModContent.GetInstance<AutoPotionConfigServer>();
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Range(0, 100)]
        [Increment(1)]
        [DefaultValue(0)]
        [ReloadRequired]
        public uint ExtraPlayerBuffSlots { get; set; }

        [DefaultValue(false)]
        [ReloadRequired]
        public bool InfinitePotionsAllowed { get; set; }

        [DefaultValue(false)]
        [ReloadRequired]
        public bool RemovePotionSicknessAllowed { get; set; }
    }
}