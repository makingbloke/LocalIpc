// Copyright ©2021 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using Newtonsoft.Json;
using System.Text;

namespace DotDoc.LocalIpc.Serializers
{
    /// <inheritdoc/>
    public class DefaultSerializer : ISerializer
    {
        private static readonly JsonSerializerSettings settings = new ()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        /// <inheritdoc/>
        public byte[] Serialize(object value)
        {
            string json = JsonConvert.SerializeObject(value, settings);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            return bytes;
        }

        /// <inheritdoc/>
        public T Deserialize<T>(byte[] bytes)
        {
            string json = Encoding.UTF8.GetString(bytes);
            T value = JsonConvert.DeserializeObject<T>(json, settings);
            return value;            
        }
    }
}
