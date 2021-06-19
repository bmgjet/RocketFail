using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Oxide.Plugins
{
    [Info("RocketFail", "bmgjet", "1.0.1")]
    [Description("Player takes damage when launcher breaks.")]
    public class RocketFail : RustPlugin
    {
        #region Vars
        private const string permUse = "RocketFail.use";
        private PluginConfig config;
        #endregion

        #region Language
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
            {"exploded", "<color=red>Weapon Exploded!</color>"}
            }, this);
        }

        private void message(BasePlayer chatplayer, string key, params object[] args)
        {
            if (chatplayer == null) { return; }
            var message = string.Format(lang.GetMessage(key, this, chatplayer.UserIDString), args);
            chatplayer.ChatMessage(message);
        }
        #endregion

        #region Configuration
        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Explode min condition : ")] public float MinHealth { get; set; }
            [JsonProperty(PropertyName = "HV hurt ammount : ")] public int HVHurt { get; set; }
            [JsonProperty(PropertyName = "Fire hurt ammount : ")] public int FireHurt { get; set; }
            [JsonProperty(PropertyName = "Exposive hurt ammount : ")] public int ExpHurt { get; set; }
            [JsonProperty(PropertyName = "HE hurt ammount : ")] public int HeHurt { get; set; }
            [JsonProperty(PropertyName = "Smoke hurt ammount : ")] public int SmokeHurt { get; set; }
            [JsonProperty(PropertyName = "Bleed ammount : ")] public int BleedHurt { get; set; }
            [JsonProperty(PropertyName = "Randomise launcher break : ")] public bool Randomise { get; set; }
            [JsonProperty(PropertyName = "Condition to trigger randomise : ")] public float RandomiseTrigger { get; set; }
            [JsonProperty(PropertyName = "Randomise 1 out of X chance to explode : ")] public int RandomiseChance { get; set; }
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                MinHealth = 5f,
                HVHurt = 40,
                FireHurt = 35,
                ExpHurt = 70,
                HeHurt = 50,
                SmokeHurt = 15,
                BleedHurt = 10,
                Randomise = false,
                RandomiseTrigger = 20,
                RandomiseChance = 50
            };
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config.WriteObject(GetDefaultConfig(), true);
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }
        #endregion

        #region Oxide Hooks
        private void Init()
        {
            permission.RegisterPermission(permUse, this);
            config = Config.ReadObject<PluginConfig>();
            if (config == null) { LoadDefaultConfig(); }
        }

        private void OnRocketLaunched(BasePlayer player, BaseEntity baseEntity2)
        {
            if (player.IPlayer.HasPermission(permUse))
            {
                var weapon = player.GetActiveItem().GetHeldEntity() as BaseProjectile;
                if (weapon == null || baseEntity2 == null || player == null) return;
                float weaponcondition = player.GetActiveItem().condition;
                string Ammotype = baseEntity2.ShortPrefabName;
                if (config.Randomise)
                {
                    if (weaponcondition <= config.RandomiseTrigger)
                    {
                        Random random = new Random();
                        if (random.Next(0, config.RandomiseChance) == random.Next(0, config.RandomiseChance)) { weaponcondition = 0; }
                    }
                }
                if (weaponcondition <= config.MinHealth)
                {
                    int GiveDamage = 0;
                    switch (Ammotype)
                    {
                        case "rocket_hv": GiveDamage = config.HVHurt; break;
                        case "rocket_fire": GiveDamage = config.FireHurt; break;
                        case "rocket_basic": GiveDamage = config.ExpHurt; break;
                        case "40mm_grenade_he": GiveDamage = config.HeHurt; break;
                        case "40mm_grenade_smoke": GiveDamage = config.SmokeHurt; break;
                    }
                    var finaleffectExternal = new Effect("assets/bundled/prefabs/fx/gas_explosion_small.prefab", player, 0, Vector3.zero, Vector3.forward);
                    var finaleffect = new Effect("assets/bundled/prefabs/fx/explosions/explosion_01.prefab", player, 0, Vector3.zero, Vector3.forward);
                    EffectNetwork.Send(finaleffect, player.net.connection);
                    EffectNetwork.Send(finaleffectExternal, player.net.connection);
                    List<BasePlayer> ClosePlayers = new List<BasePlayer>();
                    Vis.Entities<BasePlayer>(player.transform.position, 30f, ClosePlayers); // Get nearby players to play effect to.
                    foreach (BasePlayer EffectPlayer in ClosePlayers)
                    {
                        EffectNetwork.Send(finaleffect, EffectPlayer.net.connection);
                        EffectNetwork.Send(finaleffectExternal, EffectPlayer.net.connection);
                    }
                    message(player, "exploded");
                    player.GetActiveItem().condition = 0;
                    weapon.UpdateItemCondition();
                    player.Hurt(GiveDamage);
                    player.metabolism.bleeding.SetValue(config.BleedHurt);
                }
            }
        }
        #endregion
    }
}