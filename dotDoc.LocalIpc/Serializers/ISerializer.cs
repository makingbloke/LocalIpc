// Copyright ©2021 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace DotDoc.LocalIpc.Serializers
{
    /// <summary>
    /// Local Ipc Serializer Interface.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serialize the specified object into a byte array.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <returns>A byte array containing the serialized object.</returns>
        public byte[] Serialize<T>(T value);

        /// <summary>
        /// Deserialize the specified byte array into an object.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize.</typeparam>
        /// <param name="bytes">The byte array to deserialize.</param>
        /// <returns>A instance of <see cref="{T}"/>.</returns>
        public T Deserialize<T>(byte[] bytes);
    }
}
