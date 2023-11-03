using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace AutoPotion
{
    public enum AutoPotionInventory
    {
        PlayerInventory,
        PiggyBank,
        Safe,
        DefendersForge,
        VoidVault
    }

    public class AutoPotionMod : Mod
    {
        private Player _player => Main.player[Main.myPlayer];
        private List<Item> _activatedPotion = new List<Item>();
        private List<int> _flaskBuffType = new List<int>() { BuffID.WeaponImbueVenom, BuffID.WeaponImbueCursedFlames, BuffID.WeaponImbueFire, BuffID.WeaponImbueGold, BuffID.WeaponImbueIchor, BuffID.WeaponImbueNanites, BuffID.WeaponImbueConfetti, BuffID.WeaponImbuePoison };
        private List<int> _foodBuffType = new List<int>() { BuffID.WellFed, BuffID.WellFed2, BuffID.WellFed3 };
        private List<int> _calamityFlaskBuffType = new List<int>();
        private List<int> _calamityBuffType1 = new List<int>();
        private List<int> _calamityBuffType2 = new List<int>();
        private List<int> _calamityBuffType3 = new List<int>();
        private Dictionary<int, int> _calamityBuffType4 = new Dictionary<int, int>();
        private List<int> _calamityBrokenTypes = new List<int>();
        private bool _toggleActive = false;
        private int? _previousPotionDelay;

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
                ToggleAutoPotionKeybind = KeybindLoader.RegisterKeybind(this, "ToggleAutoPotion", Keys.None);
                UseAutoPotionKeybind = KeybindLoader.RegisterKeybind(this, "UsePotion", Keys.None);
                On_Player.UpdateDead += OnDeath;
                On_Player.DelBuff += OnDelBuff;
                On_Player.AddBuff += OnAddBuff;
                On_Player.Spawn += OnSpawn;
                if (AutoPotionConfig.Instance.RemovePotionSickness)
                {
                    _previousPotionDelay = Item.potionDelay;
                    Item.potionDelay = 0;
                }
            }
        }

        public override void PostSetupContent()
        {
            base.PostSetupContent();
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamityMod))
            {
                List<ModItem> calamityModItems = calamityMod.GetContent<ModItem>().ToList();
                List<ModBuff> calamityModBuffs = calamityMod.GetContent<ModBuff>().ToList();

                foreach (ModBuff modBuff in calamityModBuffs)
                {
                    if (modBuff.Name == "HotE")
                    {
                        _calamityBrokenTypes.Add(modBuff.Type); //Heart of the Elements (https://calamitymod.wiki.gg/wiki/Heart_of_the_Elements)
                    }
                    else if (modBuff.Name == "ProfanedSoulGuardians" || modBuff.Name == "ProfanedBabs")
                    {
                        _calamityBrokenTypes.Add(modBuff.Type); //Profaned Soul Guardians (https://calamitymod.wiki.gg/wiki/Profaned_Soul_Artifact)
                    }
                }
                foreach (ModItem modItem in calamityModItems)
                {
                    if (modItem.Name == "FlaskOfCrumbling" || modItem.Name == "CrumblingPotion") //Flask of Crumbling (https://calamitymod.wiki.gg/wiki/Flask_of_Crumbling)
                    {
                        _calamityBuffType1.Add(modItem.Item.buffType);
                        _calamityFlaskBuffType.Add(modItem.Item.buffType);
                    }
                    else if (modItem.Name == "FlaskOfBrimstone") //Flask of Brimstone (https://calamitymod.wiki.gg/wiki/Flask_of_Brimstone)
                    {
                        _calamityFlaskBuffType.Add(modItem.Item.buffType);
                    }
                    else if (modItem.Name == "FlaskOfHolyFlames" || modItem.Name == "HolyWrathPotion") //Flask of Holy Flames (https://calamitymod.wiki.gg/wiki/Flask_of_Holy_Flames)
                    {
                        _calamityFlaskBuffType.Add(modItem.Item.buffType);
                        _calamityBuffType3.Add(modItem.Item.buffType);
                        _calamityBuffType3.Add(BuffID.Wrath);
                    }
                    else if (modItem.Name == "ShatteringPotion") //Shattering Potion (https://calamitymod.wiki.gg/wiki/Shattering_Potion) (2.0.2.001: Removed)
                    {
                        _calamityBuffType1.Add(modItem.Item.buffType);
                    }
                    else if (modItem.Name == "ProfanedRagePotion") //Profaned Rage Potion (https://calamitymod.wiki.gg/wiki/Profaned_Rage_Potion) (2.0.2.001: Removed)
                    {
                        _calamityBuffType2.Add(modItem.Item.buffType);
                        _calamityBuffType2.Add(BuffID.Rage);
                    }
                    else if (modItem.Name == "CadancePotion") //Cadance Potion (https://calamitymod.wiki.gg/wiki/Cadance_Potion) (2.0.2.001: Removed)
                    {
                        _calamityBuffType4.Add(modItem.Item.buffType, BuffID.Regeneration);
                        _calamityBuffType4.Add(modItem.Item.buffType, BuffID.Lifeforce);
                    }
                }
            }
        }

        public override void Unload()
        {
            base.Unload();
            if (!Main.dedServ)
            {
                On_Player.UpdateDead -= OnDeath;
                On_Player.DelBuff -= OnDelBuff;
                On_Player.AddBuff -= OnAddBuff;
                On_Player.Spawn -= OnSpawn;
                if (_previousPotionDelay.HasValue)
                {
                    Item.potionDelay = _previousPotionDelay.Value;
                }
            }
            _activatedPotion.Clear();
            _flaskBuffType.Clear();
            _foodBuffType.Clear();
            _calamityFlaskBuffType.Clear();
            _calamityBuffType1.Clear();
            _calamityBuffType2.Clear();
            _calamityBuffType3.Clear();
            _calamityBuffType4.Clear();
            _calamityBrokenTypes.Clear();
            _instance = null;
            _toggleActive = false;
            ToggleAutoPotionKeybind = UseAutoPotionKeybind = null;
        }

        private void OnDelBuff(On_Player.orig_DelBuff orig, Player self, int b)
        {
            int buffType = _player.buffType[b];
            //Why does this fix a bug with calamity buffs
            if (_calamityBrokenTypes.Contains(buffType))
            {
                return;
            }
            orig(self, b);
            if (self == _player)
            {
                if (_toggleActive && !_activatedPotion.Any(it => it.buffType == buffType))
                {
                    Logger.Info($"Potion with buff: {buffType} was not in activatedPotion list.");
                }
                if (_toggleActive && _activatedPotion.Any(it => it.buffType == buffType))
                {
                    _activatedPotion.RemoveAll(it => it.buffType == buffType);
                    ConsumePotions();
                    if (_activatedPotion.Count == 0)
                    {
                        ToggleAutoPotion();
                    }
                }
                else
                {
                    _activatedPotion.RemoveAll(it => it.buffType == buffType);
                }
            }
        }
        
        private void OnAddBuff(On_Player.orig_AddBuff orig, Player self, int type, int timeToAdd, bool quiet, bool foodHack)
        {
            if (!AutoPotionConfig.Instance.RemovePotionSickness || type != BuffID.PotionSickness)
            {
                orig(self, type, timeToAdd, quiet, foodHack);
            }
        }

        private void OnDeath(On_Player.orig_UpdateDead orig, Player self)
        {
            orig(self);
            if (_toggleActive && self == _player && AutoPotionConfig.Instance.DisableOnDeath)
            {
                ToggleAutoPotion();
            }
        }

        private void OnSpawn(On_Player.orig_Spawn orig, Player self, PlayerSpawnContext context)
        {
            orig(self, context);
            if (_toggleActive && self == _player)
            {
                if (!AutoPotionConfig.Instance.DisableOnDeath)
                {
                    ConsumePotions();
                }
                else
                {
                    ToggleAutoPotion(false);
                }
            }
        }

        public void ToggleAutoPotion(bool sendChat = true)
        {
            _toggleActive = !_toggleActive;
            if (_toggleActive)
            {
                ConsumePotions();
                if (_activatedPotion.Count != 0)
                {
                    SendMultiChat(GetAutomaticToggleText());
                }
                else
                {
                    SendMultiChat(new List<TextSnippet>() { new TextSnippet("Automatic potion drinking ", AutoPotionConfig.Instance.PrintColor), new TextSnippet("Not Enabled", new Color(255, 0, 0)) });
                    _toggleActive = !_toggleActive;
                }
            }
            else
            {
                if (sendChat)
                {
                    SendMultiChat(GetAutomaticToggleText());
                }
                _activatedPotion.Clear();
            }
        }

        private List<TextSnippet> GetAutomaticToggleText()
        {
            return new List<TextSnippet>() { new TextSnippet("Automatic potion drinking ", AutoPotionConfig.Instance.PrintColor),
                new TextSnippet(_toggleActive ? "Enabled" : "Disabled", _toggleActive ? new Color(0, 255, 0) : new Color(255, 0, 0))};
        }

        public bool ConsumePotions()
        {
            bool usedPostion = false;
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
                case AutoPotionInventory.VoidVault:
                    items = _player.bank4.item;
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
                            || (_calamityFlaskBuffType.Contains(items[i].buffType) && _player.buffType.Intersect(_calamityFlaskBuffType).Count() > 0)
                            || (_calamityBuffType1.Contains(items[i].buffType) && _player.buffType.Intersect(_calamityBuffType1).Count() > 0)
                            || (_calamityBuffType2.Contains(items[i].buffType) && _player.buffType.Intersect(_calamityBuffType2).Count() > 0)
                            || (_calamityBuffType3.Contains(items[i].buffType) && _player.buffType.Intersect(_calamityBuffType3).Count() > 0)
                            || (_calamityBuffType4.ContainsKey(items[i].buffType) && _player.buffType.Intersect(_calamityBuffType4.Values).Count() > 0)
                            || _player.buffType.Contains(items[i].buffType))
                        {
                            if (!_activatedPotion.Any(it => it.buffType == items[i].buffType))
                            {
                                _activatedPotion.Add(items[i]);
                            }
                            if (!_player.buffType.Contains(items[i].buffType))
                            {
                                Logger.Info($"Potion with buff: {items[i].buffType} not added as it would cause an infinite loop.");
                            }
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

                            _player.AddBuff(items[i].buffType, AutoPotionConfig.Instance.InfinitePotions ? int.MaxValue : items[i].buffTime);

                            usedPostion = true;

                            if (!_activatedPotion.Contains(items[i]))
                            {
                                _activatedPotion.Add(items[i]);
                            }

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
                        {
                            _activatedPotion.Add(items[i]);
                        }
                    }
                    else
                    {
                        if (!_activatedPotion.Any(it => it.buffType == items[i].buffType))
                        {
                            _activatedPotion.Add(items[i]);
                        }
                        Logger.Info("No remaining buff slots available.");
                        break;
                    }
                }
            }
            PrintEmptyPotions(emptyPotions);
            return usedPostion;
        }

        private void PrintEmptyPotions(List<Item> emptyPotions)
        {
            List<TextSnippet> emptyPotionsSnippets = new List<TextSnippet>();
            foreach (Item potion in emptyPotions)
            {
                emptyPotionsSnippets.Add(new TextSnippet($"{potion.Name}{(emptyPotions.IndexOf(potion) < emptyPotions.Count - 1 ? ", " : "")}", GetColorFromRare(potion.rare)));
            }
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
                    {
                        newEmptyPotionsSnippets.Add(new List<TextSnippet>());
                    }
                    newEmptyPotionsSnippets.ElementAt(splitCounter).Add(snippet);
                }
                foreach (List<TextSnippet> snippets in newEmptyPotionsSnippets)
                {
                    SendMultiChat(snippets);
                }
            }
        }

        private void SendChat(string text, Color color = default)
        {
            if (color == default(Color))
            {
                color = Color.White;
            }
            foreach (string line in text.Split('\n'))
            {
                Main.NewText(line, color);
            }
        }

        private void SendMultiChat(List<TextSnippet> textSnippets)
        {
            string newText = "";
            foreach (TextSnippet snippet in textSnippets)
            {
                newText += $"[c/{Utils.Hex3(snippet.Color)}: {snippet.Text}]";
            }
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