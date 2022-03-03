// Copyright ©2021-2022 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using System.Text;
using System.Text.Json;

namespace DotDoc.LocalIpc.Serializers;

/// <inheritdoc/>
public class DefaultSerializer : ISerializer
{
    /// <inheritdoc/>
    public byte[] Serialize(object value)
    {
        Type type = value?.GetType() ?? typeof(object);

        var valueWrapper = new { TypeName = type.AssemblyQualifiedName, Value = value };
        string json = JsonSerializer.Serialize(valueWrapper);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <inheritdoc/>
    public object Deserialize(byte[] bytes)
    {
        using JsonDocument jsonDocument = JsonDocument.Parse(bytes);

        string typeName = jsonDocument.RootElement.GetProperty("TypeName").GetString();
        Type type = Type.GetType(typeName, true, false);

        string valueJson = jsonDocument.RootElement.GetProperty("Value").GetRawText();
        return JsonSerializer.Deserialize(valueJson, type);
    }
}
