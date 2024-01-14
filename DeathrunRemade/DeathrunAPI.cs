using System;
using System.Collections.Generic;
using DeathrunRemade.Handlers;

namespace DeathrunRemade
{
    /// <summary>
    /// A public API for other mods to interact with Deathrun.
    /// <br />
    /// To ensure this API actually exists at the time you try to access it you must either:
    /// <list type="bullet">
    /// <item>Add a <see cref="BepInEx.BepInDependency"/> attribute to your mod to ensure Deathrun loads first, or</item>
    /// <item>Avoid calling the API from your plugin's Awake() method and use Start() instead.</item>
    /// </list>
    /// </summary>
    public static class DeathrunAPI
    {
        /// <inheritdoc cref="AddSuitCrushDepth(TechType,IEnumerable{float})"/>
        public static void AddSuitCrushDepth(TechType suit, float crushDepth)
        {
            AddSuitCrushDepth(suit, new[] { crushDepth });
        }
        
        /// <summary>
        /// Register a custom suit with its associated crush depth values.<br />
        /// Crush depth values of suits may need to change depending on difficulty. If you pass more than one value
        /// they are associated with the difficulty in ascending order. The first value is associated with HARD, the
        /// next with DEATHRUN, and so on. If you pass only one value or the difficulty is higher than the number of
        /// values you passed, the <em>last</em> value you passed will be used (i.e. the one associated with the highest
        /// difficulty).
        /// </summary>
        /// <param name="suit">The <see cref="TechType"/> of the suit you are registering.</param>
        /// <param name="crushDepth">The depth(s) to which the player can dive with this suit equipped without taking
        /// damage.</param>
        /// <example>
        /// Adding a suit with a crush depth that is always 800m regardless of settings:
        /// <code>AddSuitCrushDepth(MyAwesomeSuit, 800f);</code>
        /// Adding a suit with a crush depth that is lower on more difficult settings:
        /// <code>AddSuitCrushDepth(MyAwesomeSuit, new float[] { 800f, 500f });</code>
        /// </example>
        public static void AddSuitCrushDepth(TechType suit, IEnumerable<float> crushDepth)
        {
            CrushDepthHandler.AddSuitCrushDepth(suit, crushDepth);
        }

        /// <summary>
        /// Try to get the existing crush depth values for a <see cref="TechType"/>. This is useful if you want to add
        /// a custom suit with values that match e.g. the vanilla reinforced suit.
        /// </summary>
        /// <param name="suit">The <see cref="TechType"/> of the suit you want to find values of.</param>
        /// <param name="crushDepths">The custom crush depth values of the suit in ascending order. The first element
        /// will be for HARD difficulty, the next for DEATHRUN, and so on.</param>
        /// <returns>True if the suit has a crush depth entry, false if it does not.</returns>
        public static bool TryGetSuitCrushDepth(TechType suit, out float[] crushDepths)
        {
            return CrushDepthHandler.TryGetSuitCrushDepth(suit, out crushDepths);
        }

        /// <summary>
        /// Get the number of distinct difficulty levels for personal crush depth. This value determines how many
        /// different depths can be added to <see cref="AddSuitCrushDepth(TechType,IEnumerable{float})"/>.
        /// </summary>
        public static int GetNumCrushDepthDifficulties()
        {
            return Enum.GetValues(DeathrunInit._Config.PersonalCrushDepth.Value.GetType()).Length - 1;
        }

        /// <inheritdoc cref="AddNitrogenModifier(TechType,IEnumerable{float})"/>
        public static void AddNitrogenModifier(TechType suit, float modifier)
        {
            AddNitrogenModifier(suit, new[] { modifier });
        }

        /// <summary>
        /// Register a custom equipment item with its associated nitrogen modifier values.<br />
        /// Nitrogen modifier values of items may need to change depending on difficulty. If you pass more than one
        /// value they are associated with the difficulty in ascending order. The first value is associated with HARD,
        /// the next with DEATHRUN, and so on. If you pass only one value or the difficulty is higher than the number of
        /// values you passed, the <em>last</em> value you passed will be used (i.e. the one associated with the highest
        /// difficulty).
        /// Note: These values are <em>multipliers</em> for the nitrogen accumulation rate. A value of 0.5 will halve the
        /// accumulation rate, 1.0 will completely negate it, and -1.0 will double it.
        /// </summary>
        public static void AddNitrogenModifier(TechType suit, IEnumerable<float> modifier)
        {
            NitrogenHandler.AddNitrogenModifier(suit, modifier);
        }

        /// <summary>
        /// Try to get the existing nitrogen modifier values for a <see cref="TechType"/>. This is useful if you want to
        /// add a custom equipment item with values that match e.g. the vanilla reinforced suit.
        /// </summary>
        /// <param name="suit">The <see cref="TechType"/> of the equipment item you want to find values of.</param>
        /// <param name="modifier"> The custom nitrogen modifier values of the equipment item in ascending order. The
        /// first element will be for HARD difficulty, the next for DEATHRUN, and so on.</param>
        /// <returns> True if the equipment item has a nitrogen modifier entry, false if it does not.</returns>
        public static bool TryGetNitrogenModifier(TechType suit, out float[] modifier)
        {
            return NitrogenHandler.TryGetNitrogenModifier(suit, out modifier);
        }

        /// <summary>
        /// Get the number of distinct difficulty levels for nitrogen modifier. This value determines how many
        /// different modifiers can be added to <see cref="AddNitrogenModifier(TechType,IEnumerable{float})"/>.
        /// </summary>
        public static int GetNumNitrogenDifficulties()
        {
            return Enum.GetValues(DeathrunInit._Config.NitrogenBends.Value.GetType()).Length - 1;
        }
    }
}