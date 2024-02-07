using System;
using System.Collections.Generic;
using Nautilus.Handlers;

namespace DeathrunRemade.Objects
{
    /// <summary>
    /// At the time of writing Nautilus does not include an easy way to undo any registered changes the way e.g.
    /// Harmony does. This man-in-the-middle caching class stores the game state before we register our changes and
    /// allows for restoring it at a later date, enabling e.g. resetting recipe changes between different save games.
    /// <br />
    /// It's not perfect, but it's about the best I could come up with.
    /// </summary>
    internal class NautilusShell<TKey, TValue>
    {
        private readonly Action<TKey, TValue> _editFunction;
        private readonly Func<TKey, TValue> _currentValueFunction;
        private readonly Dictionary<TKey, TValue> _cache;
        public IReadOnlyCollection<TKey> ActiveChanges => _cache.Keys;
        public bool IsEmpty => _cache.Count == 0;
        
        /// <param name="editFunction">The function that will be used to submit changes to the game, such as
        /// <see cref="CraftDataHandler.SetRecipeData(TechType,ITechData)"/></param>
        /// <param name="currentValueFunction">The function that will be used to query the current state of the thing
        /// that is about to be modified, such as <see cref="CraftDataHandler.GetRecipeData"/></param>
        public NautilusShell(Action<TKey, TValue> editFunction, Func<TKey, TValue> currentValueFunction)
        {
            _editFunction = editFunction;
            _currentValueFunction = currentValueFunction;
            _cache = new Dictionary<TKey, TValue>();
        }

        /// <summary>
        /// Cache the current state and submit our changes.
        /// </summary>
        public void SendChanges(TKey key, TValue value)
        {
            // Do not overwrite anything. Prefer resetting to the earliest known state.
            if (!_cache.ContainsKey(key))
                _cache.Add(key, _currentValueFunction(key));
            _editFunction(key, value);
        }

        /// <summary>
        /// Undo all changes by resetting to the cached state. Clears the contents of the cache after reset.
        /// </summary>
        public void UndoChanges()
        {
            foreach (var kvpair in _cache)
            {
                _editFunction(kvpair.Key, kvpair.Value);
            }

            _cache.Clear();
        }

        /// <summary>
        /// Try to find the original value for a key. Returns false if this key was never overridden.
        /// </summary>
        public bool TryGetOriginalValue(TKey key, out TValue value)
        {
            return _cache.TryGetValue(key, out value);
        }
        
        /// <summary>
        /// Override the existing cached value for a specific key.
        /// </summary>
        /// <exception cref="KeyNotFoundException">If the key does not exist in the cache.</exception>
        public void OverrideCachedValue(TKey key, TValue newValue)
        {
            if (!_cache.ContainsKey(key))
                throw new KeyNotFoundException($"Key '{key}' has never been cached.");

            _cache[key] = newValue;
        }
    }
}