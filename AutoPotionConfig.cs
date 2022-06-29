using Microsoft.Xna.Framework;
using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace AutoPotion
{
    class AutoPotionConfig : ModConfig
    {
        public static AutoPotionConfig Instance => ModContent.GetInstance<AutoPotionConfig>();
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(AutoPotionInventory.PiggyBank)]
        [Label("Auto Potion Inventory")]
        [Tooltip("This is where the mod will look for potions to use.")]
        public AutoPotionInventory AutoPotionInventory { get; set; }

        [DefaultValue(false)]
        [Label("Infinite Potions")]
        [Tooltip("If enabled no potions will be used but the buff will be applied.")]
        public bool InfinitePotions { get; set; }

        [DefaultValue(false)]
        [Label("Use Last Potion")]
        [Tooltip("If the potion count is 1 and it is enabled the last potion will not be used.")]
        public bool UseLastPotion { get; set; }

        [DefaultValue(true)]
        [Label("Disable On Death")]
        [Tooltip("If the player dies or leaves the server auto potion will be disabled.")]
        public bool DisableOnDeath { get; set; }

        [DefaultValue(true)]
        [Label("Print Empty Potions")]
        [Tooltip("If no more potions are left of a type it will post it into the local chat.")]
        public bool PrintEmptyPotions { get; set; }

        [DefaultValue(typeof(Color), "255, 255, 255, 255")]
        [Label("Print Color")]
        [Tooltip("Text color of text in chat.")]
        public Color PrintColor { get; set; }

        public override void OnChanged()
        {
            base.OnChanged();
            if (InfinitePotions && AutoPotionConfigServer.Instance != null && !AutoPotionConfigServer.Instance.InfinitePotionsAllowed)
                InfinitePotions = false;
        }
    }

    class AutoPotionConfigServer : ModConfig
    {
        public static AutoPotionConfigServer Instance => ModContent.GetInstance<AutoPotionConfigServer>();
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Range(0, 100)]
        [Increment(1)]
        [DefaultValue(0)]
        [Label("Extra Player Buff Slots")]
        [Tooltip("Increases the maximum number of buff slots available.")]
        [ReloadRequired]
        public uint ExtraPlayerBuffSlots { get; set; }

        [DefaultValue(false)]
        [Label("Infinite Potions Allowed")]
        [Tooltip("Allows a user to toggle InfinitePotions")]
        [ReloadRequired]
        public bool InfinitePotionsAllowed { get; set; }
    }
}