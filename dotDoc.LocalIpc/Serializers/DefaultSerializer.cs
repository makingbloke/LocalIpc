// Copyright ©2021 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using System;
using System.Text;
using System.Text.Json;

namespace DotDoc.LocalIpc.Serializers
{
    /// <inheritdoc/>
    public class DefaultSerializer : ISerializer
    {
        /// <inheritdoc/>
        public byte[] Serialize<T>(T value)
        {
            // If the value is not null then use the type of the value for serialization.
            // For example if we pass an object that contains an int, then use int as the serialization type.
            Type type = value?.GetType() ?? typeof(T);

            // Create an anonymous type containing the type of the value and the value and serialize this.
            var valueWrapper = new { TypeName = type.AssemblyQualifiedName, Value = value };
            string json = JsonSerializer.Serialize(valueWrapper);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <inheritdoc/>
        public T Deserialize<T>(byte[] bytes)
        {
            using JsonDocument jsonDocument = JsonDocument.Parse(bytes);

            string typeName = jsonDocument.RootElement.GetProperty("TypeName").GetString();
            Type type = Type.GetType(typeName, true, false);

            // Get the raw text of the value property and deserialize this as the type specified in TypeName.
            string valueJson = jsonDocument.RootElement.GetProperty("Value").GetRawText();
            return (T)JsonSerializer.Deserialize(valueJson, type);
        }
    }
}
