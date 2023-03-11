using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Nodes;

public class MessageParser
{
    static MessageParser()
    {
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
        };
    }

    public static dynamic ParseMessage(string msg)
        => RenameFields((JToken)JsonConvert.DeserializeObject<dynamic>(msg), ToPascalCase);

    private static JToken RenameFields(JToken token, Func<string, string> renamer)
    {
        var jObject = token as JObject;
        if (jObject != null)
        {
            var newObj = new JObject();
            foreach (var (field, value) in jObject)
            {
                newObj[renamer(field)] = RenameFields(value, renamer);
            }

            return newObj;
        }

        var jArray = token as JArray;
        if (jArray != null)
        {
            var newArr = new JArray();
            foreach (var value in jArray)
            {
                newArr.Add(RenameFields(value, renamer));
            }

            return newArr;
        }

        var jValue = token as JValue;
        if (jValue != null)
        {
            return new JValue(jValue);
        }

        throw new Exception("this should not happen! good luck :)");
    }

    private static string ToPascalCase(string s)
    {
        var spacedLowercase = s.ToLower().Replace("_", " ");
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(spacedLowercase).Replace(" ", "");
    }
}