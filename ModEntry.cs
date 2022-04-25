using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using GenericModConfigMenu;
using Object = StardewValley.Object;
using FluentEating.Models;

namespace FluentEating
{
    public class ModEntry : Mod
    {
        private ModConfig _config;
        private enum EatingPriority { Health, Stamina };
        private bool _ateFluently = false;

        const int _messageID_Eating = 1;
        const int _messageID_ConfigChanged = 2;

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>();

            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            IGenericModConfigMenuApi configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

            if (configMenu is null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => _config = new ModConfig(),
                save: () => Helper.WriteConfig(_config));

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => "Threshold");

            configMenu.AddKeybind(
                mod: ModManifest,
                getValue: () => _config.KeyBind_ToggleEnabled,
                setValue: value => _config.KeyBind_ToggleEnabled = value,
                name: () => "Toggle Enabled");

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => _config.AutoEatStaminaThreshold,
                setValue: value => _config.AutoEatStaminaThreshold = value,
                name: () => "Stamina");

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => _config.AutoEatHealthThreshold,
                setValue: value => _config.AutoEatHealthThreshold = value,
                name: () => "Health");

            configMenu.AddParagraph(
                mod: ModManifest,
                text: () => "If you lose more health/stamina than your current state in one shot, the auto-eat won't be triggered. Instead, You'll pass out as per usual.");

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => "Advanced");

            configMenu.AddKeybind(
                mod: ModManifest,
                getValue: () => _config.KeyBind_ToggleMaintainBuff,
                setValue: value => _config.KeyBind_ToggleMaintainBuff = value,
                name: () => "Toggle Maintain Buff");

            configMenu.AddBoolOption(
                mod: ModManifest,
                getValue: () => _config.MaintainBuff,
                setValue: value => _config.MaintainBuff = value,
                name: () => "Maintain buff");

            configMenu.AddParagraph(
                mod: ModManifest,
                text: () => "If enabled, player will eat the first buff food and/or drink (from top-left of the backpack) to maintain their buffs. Otherwise, any buff food won't be eaten.");

            configMenu.AddBoolOption(
                mod: ModManifest,
                getValue: () => _config.InstantEat,
                setValue: value => _config.InstantEat = value,
                name: () => "Skip Eating Animation");

        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button == _config.KeyBind_ToggleEnabled)
            {
                _config.Enabled = !_config.Enabled;

                var status = _config.Enabled ? "enabled" : "disabled";

                ShowMessage($"Fluent Eating is {status}", _messageID_ConfigChanged);
            }

            if (e.Button == _config.KeyBind_ToggleMaintainBuff)
            {
                _config.MaintainBuff = !_config.MaintainBuff;

                var status = _config.MaintainBuff ? "maintained" : "not maintained";

                ShowMessage($"Buffs are {status}", _messageID_ConfigChanged);
            }

            Helper.WriteConfig(_config);
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!_config.Enabled)
                return;

            if (_ateFluently)
            {
                static bool predicate(HUDMessage x) => x.whatType == HUDMessage.health_type || x.whatType == HUDMessage.stamina_type;

                if (Game1.hudMessages.FindAll(predicate).Count > 0)
                {
                    Game1.hudMessages.RemoveAll(predicate);
                    _ateFluently = false;
                }
            }

            if (!e.IsMultipleOf(50))
                return;

            if (!Context.IsPlayerFree)
                return;

            EatFluently();
        }

        private void EatFluently()
        {
            if (_config.MaintainBuff)
                MaintainBuff();

            EatingPriority priority;

            if (Math.Floor((double)Game1.player.health) <= _config.AutoEatHealthThreshold)
                priority = EatingPriority.Health;
            else if (Math.Floor((double)Game1.player.stamina) <= _config.AutoEatStaminaThreshold)
                priority = EatingPriority.Stamina;
            else return;

            Object leastValueConsumable = GetLeastValueConsumable(priority);

            if (leastValueConsumable is null)
                return;

            if (!Context.CanPlayerMove)
                return;

            if (priority switch
            {
                EatingPriority.Health => Game1.player.health == Game1.player.maxHealth,
                EatingPriority.Stamina => Game1.player.stamina == Game1.player.MaxStamina,
                _ => throw new Exception("Eating Priority is Invalid")
            })
                return;

            Eat(leastValueConsumable);
        }

        private void MaintainBuff()
        {
            Object firstBuffingFood = null;
            Object firstBuffingDrink = null;

            foreach (Item item in Game1.player.Items)
            {
                if (firstBuffingFood is not null && firstBuffingDrink is not null)
                    break;

                if (item is not Object @object)
                    continue;

                if (@object.Edibility <= 0)
                    continue;

                var consumable = new Consumable(@object);

                if (!consumable.HasBuff)
                    continue;

                if (firstBuffingFood is null && consumable.Type == Consumable.ItemType.Food)
                    firstBuffingFood = @object;

                if (firstBuffingDrink is null && consumable.Type == Consumable.ItemType.Drink)
                    firstBuffingDrink = @object;
            }

            if (Game1.buffsDisplay.food is null && firstBuffingFood is not null)
                Eat(firstBuffingFood);

            if (Game1.buffsDisplay.drink is null && firstBuffingDrink is not null)
                Eat(firstBuffingDrink);
        }

        private void Eat(Object @object)
        {
            if (!Context.CanPlayerMove)
                return;

            Farmer player = Game1.player;

            if (_config.InstantEat)
            {
                player.health = Math.Min(player.maxHealth, player.health + @object.healthRecoveredOnConsumption());
                player.stamina = Math.Min(player.MaxStamina, player.stamina + @object.staminaRecoveredOnConsumption());

                var consumable = new Consumable(@object);

                if (consumable.HasBuff)
                    consumable.ApplyBuff();
            }
            else
            {
                Game1.player.eatObject(@object);
            }

            ShowMessage($"You consumed {@object.Name} fluently.", _messageID_Eating);

            @object.Stack--;

            if (@object.Stack == 0)
                Game1.player.removeItemFromInventory(@object);

            _ateFluently = true;
        }

        private static Object GetLeastValueConsumable(EatingPriority priority)
        {
            Object leastValueConsumable = null;
            float leastValue = float.NegativeInfinity;

            foreach (Item item in Game1.player.Items)
            {
                if (item is not Object @object || @object.Edibility <= 0)
                    continue;

                if ((new Consumable(@object)).HasBuff)
                    continue;

                float recoveredAmount = priority == EatingPriority.Health ? item.healthRecoveredOnConsumption() : item.staminaRecoveredOnConsumption();

                float realRecoveredAmount = priority switch
                {
                    EatingPriority.Health => Game1.player.maxHealth - Game1.player.health < recoveredAmount ? Game1.player.maxHealth - Game1.player.health : recoveredAmount,
                    EatingPriority.Stamina => Game1.player.MaxStamina - Game1.player.stamina < recoveredAmount ? Game1.player.MaxStamina - Game1.player.stamina : recoveredAmount,
                    _ => throw new Exception("Eating Priority is Invalid")
                };

                float sellingPrice = @object.sellToStorePrice(Game1.player.UniqueMultiplayerID);

                float recoveryPerGold = realRecoveredAmount / sellingPrice;

                if (leastValueConsumable is null || recoveryPerGold > leastValue)
                {
                    leastValueConsumable = @object;
                    leastValue = recoveryPerGold;
                }
            }

            return leastValueConsumable;
        }

        private static void ShowMessage(string text, int messageID = -1)
        {
            Game1.hudMessages.RemoveAll(x => x.number == messageID || x.whatType == HUDMessage.health_type || x.whatType == HUDMessage.stamina_type);
            Game1.addHUDMessage(new HUDMessage(text, HUDMessage.error_type) { noIcon = true, number = messageID });
        }
    }
}
