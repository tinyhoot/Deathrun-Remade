using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DeathrunRemade.Handlers;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    /// <summary>
    /// The game is very unreliable when it comes to using the proper damage types and will often just use Normal.
    /// Set an override where necessary so we still capture the causes accurately.
    /// </summary>
    [HarmonyPatch]
    internal class CauseOfDeathPatcher
    {
        /// <summary>
        /// Suffocation does not have its own damage type and uses Normal instead, for whatever reason.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.SuffocationDie))]
        private static void PatchSuffocation(Player __instance)
        {
            string cause = __instance.IsSwimming() ? "Drowning" : "Asphyxiation";
            DeathrunInit._RunHandler.SetCauseOfDeathOverride(cause);
        }

        /// <summary>
        /// Melee attacks mostly use Normal damage with no way to tell who did the attacking.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MeleeAttack), nameof(MeleeAttack.OnTouch))]
        private static void PatchMeleeAttack(MeleeAttack __instance, Collider collider)
        {
            if (collider == null || collider.GetComponent<Player>() == null)
                return;
            DeathrunInit._RunHandler.SetCauseOfDeathOverride(__instance.gameObject);
        }
        
        /// <summary>
        /// Not a subclass of melee attack for some reason and thus needs its own patch.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(JuvenileEmperorMeleeAttack), nameof(JuvenileEmperorMeleeAttack.OnClawTouch))]
        private static void PatchJuvenileMeleeAttack(JuvenileEmperorMeleeAttack __instance, Collider collider)
        {
            if (collider == null || collider.GetComponent<Player>() == null)
                return;
            DeathrunInit._RunHandler.SetCauseOfDeathOverride(__instance.gameObject);
        }
        
        /// <summary>
        /// Not a subclass of melee attack for some reason and thus needs its own patch.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SeaTreaderMeleeAttack), nameof(SeaTreaderMeleeAttack.OnLegTouch))]
        private static void PatchSeaTreaderMeleeAttack(SeaTreaderMeleeAttack __instance, Collider collider)
        {
            if (collider == null || collider.GetComponent<Player>() == null)
                return;
            DeathrunInit._RunHandler.SetCauseOfDeathOverride(__instance.gameObject);
        }
        
        /// <summary>
        /// Has extra methods that need patching to account for all the ways you can die to a dragon.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SeaDragonMeleeAttack), nameof(SeaDragonMeleeAttack.OnTouchFront))]
        private static void PatchSeaDragonMeleeAttackBite(SeaDragonMeleeAttack __instance, Collider collider)
        {
            if (collider == null || collider.GetComponent<Player>() == null)
                return;
            DeathrunInit._RunHandler.SetCauseOfDeathOverride(__instance.gameObject);
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SeaDragonMeleeAttack), nameof(SeaDragonMeleeAttack.SwatAttack))]
        private static void PatchSeaDragonMeleeAttackGrab(SeaDragonMeleeAttack __instance, GameObject target)
        {
            if (target == null || target.GetComponent<Player>() == null)
                return;
            DeathrunInit._RunHandler.SetCauseOfDeathOverride(__instance.gameObject);
        }

        /// <summary>
        /// Patch any and all projectiles in the game since quite a lot of those deal normal damage too.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnCollisionEnter))]
        private static void PatchProjectile(Projectile __instance, Collision collision)
        {
            if (collision?.gameObject == null || collision.gameObject.GetComponent<Player>() == null)
                return;
            DeathrunInit._RunHandler.SetCauseOfDeathOverride(__instance.gameObject);
        }

        /// <summary>
        /// Anything that can attach to the player uses normal damage, so patch it.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AttachAndSuck), nameof(AttachAndSuck.SuckBlood))]
        private static void PatchBloodSucker(AttachAndSuck __instance)
        {
            var target = __instance.targetLiveMixin.gameObject;
            if (target.GetComponent<Player>() == null)
                return;
            DeathrunInit._RunHandler.SetCauseOfDeathOverride(target);
        }

        /// <summary>
        /// Being inside the Cyclops when it explodes is instant death, but that uses normal damage.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CyclopsDestructionEvent), nameof(CyclopsDestructionEvent.DestroyCyclops))]
        private static void PatchCyclopsDestruction()
        {
            DeathrunInit._RunHandler.SetCauseOfDeathOverride("Cyclops Destruction");
        }
        
        /// <summary>
        /// On pickup uses normal damage.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DamageOnPickup), nameof(DamageOnPickup.OnPickedUp))]
        private static void PatchPickupDamage(DamageOnPickup __instance)
        {
            DeathrunInit._RunHandler.SetCauseOfDeathOverride(__instance.gameObject);
        }
        
        /// <summary>
        /// Can be any damage type but there's a good chance it's normal, so patch this just in case.
        /// Attaches to whatever it damages, so it's a bit of a special case either way.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DamageOverTime), nameof(DamageOverTime.DoDamage))]
        private static void PatchDamageOverTime(DamageOverTime __instance)
        {
            if (__instance.GetComponent<Player>() == null)
                return;
            DeathrunInit._RunHandler.SetCauseOfDeathOverride(__instance.doer);
        }
        
        /// <summary>
        /// To my knowledge this is exclusively used for the Aurora's radiation damage, but I might have missed
        /// something. Doesn't hurt to be safe.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DamagePlayerInRadius), nameof(DamagePlayerInRadius.DoDamage))]
        private static IEnumerable<CodeInstruction> PatchDamageInRadius(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            // There's a branching label on the beginning of this method call which makes it annoying to deal with.
            // Instead of messing with that, we'll insert our method call in the midst of things.
            // Find the call to LiveMixin.TakeDamage().
            matcher.MatchForward(true, 
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldnull),
                    new CodeMatch(i => ((MethodInfo)i.operand).Name.Equals("TakeDamage")))
                // Insert our own call to set the cause of death in between the call and its arguments.
                .Insert(
                    CodeInstruction.LoadField(typeof(DeathrunInit), nameof(DeathrunInit._RunHandler)),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(Component), nameof(Component.gameObject)).GetMethod),
                    CodeInstruction.Call(typeof(RunHandler), nameof(RunHandler.SetCauseOfDeathOverride), new[] { typeof(GameObject) }));
            return matcher.InstructionEnumeration();
        }

        /// <summary>
        /// Used for many spherical hitboxes. Variable damage type, so can be normal.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DamageSphere), nameof(DamageSphere.ApplyDamageEffects))]
        private static void PatchDamageSphere(DamageSphere __instance)
        {
            if (__instance.tracker.Get().All(go => go == null || go.GetComponent<Player>() == null))
                return;
            DeathrunInit._RunHandler.SetCauseOfDeathOverride(__instance.gameObject);
        }

        // I do not understand what is going on with this, but the GetComponent call is causing NullRefs all over.
        // Since it's very unlikely to happen anyway I simply removed the patch altogether.
        
        // /// <summary>
        // /// I don't think anyone has ever died to this, but floaters can technically cause normal damage if touched
        // /// while they are not attached to something. If they attach to the player, this causes a tiny bit of damage.
        // /// </summary>
        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(Floater), nameof(Floater.OnCollisionEnter))]
        // private static void PatchFloater(Floater __instance, Collider collisionInfo)
        // {
        //     if (collisionInfo is null || collisionInfo.GetComponent<Player>() is null)
        //         return;
        //     DeathrunInit._RunHandler.SetCauseOfDeathOverride(__instance.gameObject);
        // }

        /// <summary>
        /// Lava does normal damage???
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Lava), nameof(Lava.OnTriggerStay))]
        private static void PatchLava(Collider collider)
        {
            if (collider == null || collider.GetComponent<Player>() == null)
                return;
            DeathrunInit._RunHandler.SetCauseOfDeathOverride("Lava");
        }
        
        /// <summary>
        /// Magma does normal damage???
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MagmaBlob), nameof(MagmaBlob.OnTriggerStay))]
        private static void PatchMagma(Collider other)
        {
            if (other == null || other.GetComponent<Player>() == null)
                return;
            DeathrunInit._RunHandler.SetCauseOfDeathOverride("Chunk of Magma");
        }

        /// <summary>
        /// Getting warp-pulled out of your vehicle does normal damage.
        /// </summary>
        /// <param name="target"></param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WarpBall), nameof(WarpBall.Warp))]
        private static void PatchWarpBall(GameObject target)
        {
            if (target == null || target.GetComponent<Player>() == null)
                return;
            DeathrunInit._RunHandler.SetCauseOfDeathOverride(TechType.Warper);
        }

        /// <summary>
        /// These both deal normal damage. Fair, all things considered.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.OnConsoleCommand_kill))]
        [HarmonyPatch(typeof(Player), nameof(Player.OnConsoleCommand_takedamage))]
        private static void PatchConsoleCommands()
        {
            DeathrunInit._RunHandler.SetCauseOfDeathOverride("Deus Ex Machina");
        }

        /// <summary>
        /// The escape rocket's elevator can actually crush you if you get caught under it. Patch a short notifying
        /// call to this mod into the method in case that happens.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Rocket), nameof(Rocket.Update))]
        private static IEnumerable<CodeInstruction> PatchRocketElevator(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            // Find the call to Player.main.OnKill().
            matcher.MatchForward(false, 
                    new CodeMatch(OpCodes.Ldsfld),
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    new CodeMatch(i => ((MethodInfo)i.operand).Name.Equals("OnKill")))
                // Insert our own call to set the cause of death just before it.
                .Insert(
                    CodeInstruction.LoadField(typeof(DeathrunInit), nameof(DeathrunInit._RunHandler)),
                    new CodeInstruction(OpCodes.Ldstr, "Rocket Elevator"),
                    CodeInstruction.Call(typeof(RunHandler), nameof(RunHandler.SetCauseOfDeathOverride), new[] { typeof(string) }));
            return matcher.InstructionEnumeration();
        }

        /// <summary>
        /// The game checks whether any stats are too low and returns a damage value if so. This damage is always dealt
        /// as Starve, but we want a bit more nuance.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Survival), nameof(Survival.UpdateStats))]
        private static void PatchHunger(Survival __instance, float __result)
        {
            // Don't do anything if we're not about to take damage.
            if (__result <= 0f)
                return;
            string cause = __instance.food <= 0f ? "Starvation" : "Dehydration";
            DeathrunInit._RunHandler.SetCauseOfDeathOverride(cause);
        }
    }
}