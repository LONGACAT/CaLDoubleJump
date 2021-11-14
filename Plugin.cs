using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MonoMod.Cil;
using System;

namespace CaLDoubleJump
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<int> maxJumps;
        private int jumpsLeft;
        private void Awake()
        {
            maxJumps = Config.Bind("General", "MaxJumps", 2, "The max amount of jumps the player can do");
            jumpsLeft = maxJumps.Value;
            IL.Cat.CatControls.InputCheck += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdcI4(0),
                    x => x.MatchCallOrCallvirt<Cat.CatControls>("Jump")
                    );
                c.Index += 1;
                c.RemoveRange(2);
                c.EmitDelegate<Action<Cat.CatControls>>((self) =>
                {
                    // Player fell off the floor and should have one jump removed, so only one jump can be done mid-air
                    if (!(bool)AccessTools.Property(typeof(Cat.CatCollision), "CollidingBottom").GetValue((Cat.CatCollision)AccessTools.Field(typeof(Cat.CatControls), "catCollision").GetValue(self)) && jumpsLeft == maxJumps.Value)
                    {
                        jumpsLeft--;
                    }
                    if (jumpsLeft > 0)
                    {
                        AccessTools.Field(typeof(Cat.CatControls), "jumping").SetValue(self, false);
                        jumpsLeft--;
                    }
                    // Calling Jump(true) makes it ignore that the player is in mid-air
                    AccessTools.Method(typeof(Cat.CatControls), "Jump").Invoke(self, new object[] { true });
                });
            };

            // Reset the amount of jumps left
            On.Cat.CatCollision.UpdateCollidingStatus += (orig, self) =>
            {
                orig(self);
                if ((bool)AccessTools.Property(typeof(Cat.CatCollision), "CollidingBottom").GetValue(self))
                {
                    jumpsLeft = maxJumps.Value;
                }
            };
        }
    }
}
