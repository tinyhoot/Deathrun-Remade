using System;
using System.Collections.Generic;
using System.Reflection;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using HootLib;

namespace DeathrunRemade.Objects.Attributes
{
    /// <summary>
    /// A helper attribute to make it easier to harmony patch specific types / methods all at once. This functionality
    /// exists in HarmonyX from v2.11.0 onwards but at the time of writing that's a hot new release and does not support
    /// unpatching.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class PatchCategory : Attribute
    {
        public readonly string Category;
        
        public PatchCategory(string category)
        {
            Category = category;
        }

        public PatchCategory(ApplyPatch category)
        {
            Category = category.AsString();
        }
    }

    internal static class PatchCategoryHarmonyExtensions
    {
        /// <inheritdoc cref="PatchTypesWithCategory(HarmonyLib.Harmony,string)"/>
        public static void PatchTypesWithCategory(this Harmony harmony, ApplyPatch category)
        {
            PatchTypesWithCategory(harmony, category.AsString());
        }
        
        /// <summary>
        /// Finds and patches everything in all types annotated with a <see cref="PatchCategory"/> attribute of the
        /// specified category.
        /// </summary>
        /// <param name="harmony">The harmony instance.</param>
        /// <param name="category">The category to patch.</param>
        public static void PatchTypesWithCategory(this Harmony harmony, string category)
        {
            List<(Type, PatchCategory)> attributes = Hootils.GetOwnedTypesWithAttribute<PatchCategory>();
            if (attributes is null || attributes.Count == 0)
                return;

            foreach ((Type type, PatchCategory attribute) in attributes)
            {
                if (attribute.Category != category)
                    continue;
                harmony.PatchAll(type);
            }
        }

        /// <inheritdoc cref="UnpatchTypesWithCategory(HarmonyLib.Harmony,string)"/>
        public static void UnpatchTypesWithCategory(this Harmony harmony, ApplyPatch category)
        {
            UnpatchTypesWithCategory(harmony, category.AsString());
        }
        
        /// <summary>
        /// Finds and unpatches everything in all types annotated with a <see cref="PatchCategory"/> attribute of the
        /// specified category.<br />
        /// Caution: This only works reliably for methods where the target type and target method name are
        /// both provided in the <em>same</em> <see cref="HarmonyPatch"/> attribute.
        /// </summary>
        /// <param name="harmony">The harmony instance.</param>
        /// <param name="category">The category to unpatch.</param>
        public static void UnpatchTypesWithCategory(this Harmony harmony, string category)
        {
            List<(Type, PatchCategory)> attributes = Hootils.GetOwnedTypesWithAttribute<PatchCategory>();
            if (attributes is null || attributes.Count == 0)
                return;

            foreach ((Type type, PatchCategory attribute) in attributes)
            {
                if (attribute.Category != category)
                    continue;
                
                int unpatched = 0;
                foreach (MethodInfo patchMethod in AccessTools.GetDeclaredMethods(type))
                {
                    // DeathrunInit._Log.Debug($"Looking at method {patchMethod.Name}");
                    
                    foreach (MethodInfo targetMethod in ExtractPatchTargets(patchMethod))
                    {
                        // DeathrunInit._Log.Debug($"Unpatching target {targetMethod.DeclaringType}.{targetMethod.Name}()");
                        harmony.Unpatch(targetMethod, patchMethod);
                        unpatched++;
                    }
                }
                DeathrunInit._Log.Debug($"Unpatched {unpatched} methods in {type}.");
            }
        }

        /// <summary>
        /// Get all targeted methods from the HarmonyPatch attributes of a method.
        /// This only really works if both type and method are defined in the same attribute, but that is the style
        /// used by this project anyway so we're good.
        /// </summary>
        private static List<MethodInfo> ExtractPatchTargets(MethodInfo method)
        {
            List<HarmonyMethod> attributes = HarmonyMethodExtensions.GetFromMethod(method);
            List<MethodInfo> targets = new List<MethodInfo>();

            foreach (HarmonyMethod attribute in attributes)
            {
                MethodInfo target = AccessTools.Method(attribute.declaringType, attribute.methodName, attribute.argumentTypes);
                if (target != null)
                    targets.Add(target);
            }

            return targets;
        }
    }
}