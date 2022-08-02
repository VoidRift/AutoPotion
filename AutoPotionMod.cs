using System;
using System.Linq;
using Terraria;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.UI.Chat;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Input;

namespace AutoPotion
{
    enum AutoPotionInventory
    {
        PlayerInventory,
        PiggyBank,
        Safe,
        DefendersForge
    }

    public class AutoPotionMod : Mod
    {
        private static string TOGGLE_AUTO_POTION = "Toggle auto potion";
        private static string USE_POTION = "Single use potion";

        private bool _toggleActive = false;

        private Player _player => Main.player[Main.myPlayer];
        private List<Item> _activatedPotion = new List<Item>();
        private List<int> _flaskBuffType = new List<int>() { 71, 73, 74, 75, 76, 77, 78, 79 };
        private List<int> _foodBuffType = new List<int>() { 26, 206, 207 };
        private List<int> _calamityBuffType1 = new List<int>() { 437, 438 };
        private List<int> _calamityBuffType2 = new List<int>() { 452, 115 };
        private List<int> _calamityBuffType3 = new List<int>() { 448, 117 };
        private List<int> _calamityBuffType4 = new List<int>() { 443, 2, 113 };

        private static AutoPotionMod _instance;
        public static AutoPotionMod Instance => _instance ?? (_instance = new AutoPotionMod());

        public override uint ExtraPlayerBuffSlots => AutoPotionConfigServer.Instance.ExtraPlayerBuffSlots;

        public ModKeybind ToggleAutoPotionKeybind { get; private set; }
        public ModKeybind UseAutoPotionKeybind { get; private set; }

        public AutoPotionMod() : base()
        {
            _instance = this;
        }

        public override void Load()
        {
            base.Load();
            if (!Main.dedServ)
            {
                ToggleAutoPotionKeybind = KeybindLoader.RegisterKeybind(this, TOGGLE_AUTO_POTION, Keys.None);
                UseAutoPotionKeybind = KeybindLoader.RegisterKeybind(this, USE_POTION, Keys.None);
                On.Terraria.Player.UpdateDead += OnDeath;
                On.Terraria.Player.DelBuff += OnDelBuff;
                On.Terraria.Player.Spawn += OnSpawn;
            }
        }

        public override void Unload()
        {
            base.Unload();
            if (!Main.dedServ)
            {
                On.Terraria.Player.UpdateDead -= OnDeath;
                On.Terraria.Player.DelBuff -= OnDelBuff;
                On.Terraria.Player.Spawn -= OnSpawn;
            }
            _activatedPotion.Clear();
            _flaskBuffType.Clear();
            _foodBuffType.Clear();
            _calamityBuffType1.Clear();
            _calamityBuffType2.Clear();
            _calamityBuffType3.Clear();
            _calamityBuffType4.Clear();
            _instance = null;
            _toggleActive = false;
            ToggleAutoPotionKeybind = UseAutoPotionKeybind = null;
        }

        private void OnDelBuff(On.Terraria.Player.orig_DelBuff orig, global::Terraria.Player self, int b)
        {
            int buffType = _player.buffType[b];
            //Why does this fix the bug
            if ((buffType == 575) || (buffType == 592))
                return;
            orig(self, b);
            if (self == _player)
            {
                if (_toggleActive && !_activatedPotion.Any(it => it.buffType == buffType))
                    Logger.Info($"Potion with buff: {buffType} was not in activatedPotion list.");
                if (_toggleActive && _activatedPotion.Any(it => it.buffType == buffType))
                {
                    _activatedPotion.RemoveAll(it => it.buffType == buffType);
                    ConsumePotions();
                    if (_activatedPotion.Count == 0)
                        ToggleAutoPotion();
                }
                else
                {
                    _activatedPotion.RemoveAll(it => it.buffType == buffType);
                }
            }
        }

        private void OnDeath(On.Terraria.Player.orig_UpdateDead orig, global::Terraria.Player self)
        {
            orig(self);
            if (_toggleActive && self == _player && AutoPotionConfig.Instance.DisableOnDeath)
                ToggleAutoPotion();
        }

        private void OnSpawn(On.Terraria.Player.orig_Spawn orig, global::Terraria.Player self, PlayerSpawnContext context)
        {
            orig(self, context);
            if (_toggleActive && self == _player)
            {
                if (!AutoPotionConfig.Instance.DisableOnDeath)
                    ConsumePotions();
                else
                    ToggleAutoPotion(false);
            }
        }

        public void ToggleAutoPotion(bool sendChat = true)
        {
            _toggleActive = !_toggleActive;
            if (_toggleActive)
            {
                ConsumePotions();
                if (_activatedPotion.Count != 0)
                    SendMultiChat(GetAutomaticToggleText());
                else
                {
                    SendMultiChat(new List<TextSnippet>() { new TextSnippet("Automatic potion drinking ", AutoPotionConfig.Instance.PrintColor),
                        new TextSnippet("Not Enabled", new Color(255, 0, 0)) });
                    _toggleActive = !_toggleActive;
                }
            }
            else
            {
                if (sendChat)
                    SendMultiChat(GetAutomaticToggleText());
                _activatedPotion.Clear();
            }
        }

        private List<TextSnippet> GetAutomaticToggleText()
        {
            return new List<TextSnippet>() { new TextSnippet("Automatic potion drinking ", AutoPotionConfig.Instance.PrintColor),
                new TextSnippet(_toggleActive ? "Enabled" : "Disabled", _toggleActive ? new Color(0, 255, 0) : new Color(255, 0, 0))};
        }

        public void ConsumePotions()
        {
            Item[] items = new Item[0];
            switch (AutoPotionConfig.Instance.AutoPotionInventory)
            {
                case AutoPotionInventory.PlayerInventory:
                    items = _player.inventory;
                    break;
                case AutoPotionInventory.PiggyBank:
                    items = _player.bank.item;
                    break;
                case AutoPotionInventory.Safe:
                    items = _player.bank2.item;
                    break;
                case AutoPotionInventory.DefendersForge:
                    items = _player.bank3.item;
                    break;
            }

            List<Item> emptyPotions = new List<Item>();
            for (int i = 0; i < items.Length; i++)
            {
                bool bottomlessPotion = items[i].ModItem?.Mod.Name == "BottomlessPotions";
                if ((items[i].potion || items[i].consumable || bottomlessPotion) && items[i].buffTime != 0)
                {
                    if (_player.buffType.Contains(0))
                    {
                        if ((_flaskBuffType.Contains(items[i].buffType) && _player.buffType.Intersect(_flaskBuffType).Count() > 0)
                            || (_foodBuffType.Contains(items[i].buffType) && _player.buffType.Intersect(_foodBuffType).Count() > 0)
                            || (_calamityBuffType1.Contains(items[i].buffType) && _player.buffType.Intersect(_calamityBuffType1).Count() > 0)
                            || (_calamityBuffType2.Contains(items[i].buffType) && _player.buffType.Intersect(_calamityBuffType2).Count() > 0)
                            || (_calamityBuffType3.Contains(items[i].buffType) && _player.buffType.Intersect(_calamityBuffType3).Count() > 0)
                            || (_calamityBuffType4.Contains(items[i].buffType) && _player.buffType.Intersect(_calamityBuffType4).Count() > 0)
                            || _player.buffType.Contains(items[i].buffType))
                        {
                            if (!_activatedPotion.Any(it => it.buffType == items[i].buffType))
                                _activatedPotion.Add(items[i]);
                            if (!_player.buffType.Contains(items[i].buffType))
                                Logger.Info($"Potion with buff: {items[i].buffType} not added as it would cause an infinite loop.");
                            continue;
                        }

                        if (!_player.buffType.Contains(items[i].buffType))
                        {
                            if (!AutoPotionConfig.Instance.InfinitePotions && items[i].stack <= 1 && !AutoPotionConfig.Instance.UseLastPotion && !bottomlessPotion)
                            {
                                _activatedPotion.Remove(items[i]);
                                emptyPotions.Add(items[i]);
                                continue;
                            }

                            _player.AddBuff(items[i].buffType, AutoPotionConfig.Instance.InfinitePotions ? Int32.MaxValue : items[i].buffTime);

                            if (!_activatedPotion.Contains(items[i]))
                                _activatedPotion.Add(items[i]);

                            if (!AutoPotionConfig.Instance.InfinitePotions)
                            {
                                if (!bottomlessPotion)
                                    items[i].stack -= 1;
                            }
                            else
                            {
                                emptyPotions.Clear();
                            }

                            if (items[i].stack <= 0)
                            {
                                _activatedPotion.Remove(items[i]);
                                emptyPotions.Add(items[i]);
                                items[i] = new Item();
                                items[i].SetDefaults();
                                items[i].TurnToAir();
                            }
                        }
                        if (_player.buffType.Contains(items[i].buffType) && !_activatedPotion.Contains(items[i]))
                            _activatedPotion.Add(items[i]);
                    }
                    else
                    {
                        if (!_activatedPotion.Any(it => it.buffType == items[i].buffType))
                            _activatedPotion.Add(items[i]);
                        Logger.Info("No remaining buff slots available.");
                    }
                }
            }
            PrintEmptyPotions(emptyPotions);
        }

        private void PrintEmptyPotions(List<Item> emptyPotions)
        {
            List<TextSnippet> emptyPotionsSnippets = new List<TextSnippet>();

            foreach (Item potion in emptyPotions)
                emptyPotionsSnippets.Add(new TextSnippet($"{potion.Name}{(emptyPotions.IndexOf(potion) < emptyPotions.Count - 1 ? ", " : "")}", GetColorFromRare(potion.rare)));

            if (emptyPotionsSnippets.Count != 0 && AutoPotionConfig.Instance.PrintEmptyPotions)
            {
                emptyPotionsSnippets.Insert(0, new TextSnippet("No potions left of: ", AutoPotionConfig.Instance.PrintColor));
                int splitCounter = 0;
                int stringCounter = 0;
                List<List<TextSnippet>> newEmptyPotionsSnippets = new List<List<TextSnippet>>();
                foreach (TextSnippet snippet in emptyPotionsSnippets)
                {
                    if (stringCounter + snippet.Text.Length >= (splitCounter + 1) * 80)
                    {
                        splitCounter++;
                        stringCounter += snippet.Text.Length;
                    }
                    else
                    {
                        stringCounter += snippet.Text.Length;
                    }
                    if (newEmptyPotionsSnippets.Count <= (splitCounter))
                        newEmptyPotionsSnippets.Add(new List<TextSnippet>());
                    newEmptyPotionsSnippets.ElementAt(splitCounter).Add(snippet);
                }
                foreach (List<TextSnippet> snippets in newEmptyPotionsSnippets)
                    SendMultiChat(snippets);
            }
        }

        private void SendChat(string text, Color color = default)
        {
            if (color == default(Color))
                color = Color.White;
            foreach (string line in text.Split('\n'))
                Main.NewText(line, color);
        }

        private void SendMultiChat(List<TextSnippet> textSnippets)
        {
            string newText = "";
            foreach (TextSnippet snippet in textSnippets)
                newText += $"[c/{Utils.Hex3(snippet.Color)}: {snippet.Text}]";
            Main.NewText(newText);
        }

        private Color GetColorFromRare(int rare)
        {
            switch (rare)
            {
                case -12:
                    return new Color(255, 255, 255);
                case -11:
                    return new Color(100, 75, 0);
                case -1:
                    return new Color(123, 123, 123);
                case 0:
                    return new Color(255, 255, 255);
                case 1:
                    return new Color(154, 154, 255);
                case 2:
                    return new Color(154, 255, 154);
                case 3:
                    return new Color(246, 193, 146);
                case 4:
                    return new Color(253, 149, 149);
                case 5:
                    return new Color(255, 152, 255);
                case 6:
                    return new Color(191, 146, 233);
                case 7:
                    return new Color(143, 243, 2);
                case 8:
                    return new Color(243, 243, 2);
                case 9:
                    return new Color(0, 200, 255);
                case 10:
                    return new Color(230, 30, 88);
                case 11:
                    return new Color(146, 25, 206);
            }
            return new Color(255, 255, 255);
        }
    }
}