using System;
using System.Collections.Generic;
using HootLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DeathrunRemade.Objects
{
    // Since these classes are just for serialising we really don't need warnings about "unused" fields.
#pragma warning disable CS0649
    
    /// <summary>
    /// A wrapper class to make JSON serialisation of recipes easier because UWE's classes contain getters/setters
    /// and that doesn't work too well.
    /// </summary>
    [Serializable]
    internal class SerialTechData
    {
        [JsonConverter(typeof(BetterStringEnumConverter))]
        public TechType techType;
        public int craftAmount = 1;
        public List<SerialIngredient> ingredients;

        public CraftData.TechData ToTechData()
        {
            var data = new CraftData.TechData
            {
                _craftAmount = craftAmount,
                _techType = techType,
                _ingredients = new CraftData.Ingredients()
            };
            ingredients.ForEach(i => data._ingredients.Add(i.techType, i.amount));

            return data;
        }
    }

    [Serializable]
    internal class SerialIngredient
    {
        [JsonConverter(typeof(BetterStringEnumConverter))]
        public TechType techType;
        public int amount;

        public SerialIngredient(TechType techType, int amount = 1)
        {
            this.techType = techType;
            this.amount = amount;
        }

        public CraftData.Ingredient ToIngredient()
        {
            return new CraftData.Ingredient(techType, amount);
        }

        public override string ToString()
        {
            return $"({amount} {techType})";
        }
    }

    [Serializable]
    internal class SerialScanData
    {
        [JsonConverter(typeof(BetterStringEnumConverter))]
        public TechType techType;
        public int amount;
        
        public SerialScanData(TechType techType, int amount)
        {
            this.techType = techType;
            this.amount = amount;
        }
    }

    /// <summary>
    /// The normal StringEnumConverter results in a nullref for some reason so here's an extremely basic replacement.
    /// </summary>
    internal class BetterStringEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (!objectType.IsEnum)
                return existingValue;

            return Hootils.ParseEnum(objectType, reader.Value?.ToString());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            string name = value.ToString();
            writer.WriteValue(name);
        }
    }
}