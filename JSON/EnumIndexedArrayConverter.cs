using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OVERKILL.Upgrades.Cybergrind;

namespace OVERKILL.JSON;

public class EnumIndexedArrayConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        OK.Log($"Reading: {objectType.Name}");

        List <double> l = new List <double>();

        int i = 0;
        if(reader.TokenType == JsonToken.StartArray)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                {
                    break;
                }
                double item = serializer.Deserialize<double>(reader);

                l.Add(item);
                
                i++;
             
                /*
                if (l.Count > Constants.DataSourceMaxItems)
                {
                    BadDataSourceValidationResult validationResult = new()
                    {
                        MaxListSize = new()
                        {
                            MaxSize = Constants.DataSourceMaxItems,
                            Recieved = items.Count
                        }
                    };
                    throw new BadDataSourceException(validationResult);
                }
                */
            }
        }
        if(!l.Any())
        {
            OK.Log("AAAAHAHAH!");
            throw new Exception("Atleast 1 entry in list Needed");
        }



        var arr = l.ToArray();

        var v = Activator.CreateInstance(objectType, new object[] {arr});
        
        OK.Log($"Read enumindexed: {reader.Path}, count: {arr.Length}");

        return v;
    }

    public override bool CanConvert(Type objectType)
    {
        return true;
    }
}
