// Copyright ©2021-2022 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

namespace DotDoc.LocalIpc.Serializers;

/// <summary>
/// Local Ipc Serializer Interface.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Serialize the specified object into a byte array.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <returns>A byte array containing the serialized object.</returns>
    public byte[] Serialize(object value);

    /// <summary>
    /// Deserialize the specified byte array into an object.
    /// </summary>
    /// <param name="bytes">The byte array to deserialize.</param>
    /// <returns>A <see cref="object"/> containing the deserialized item.</returns>
    public object Deserialize(byte[] bytes);
}
