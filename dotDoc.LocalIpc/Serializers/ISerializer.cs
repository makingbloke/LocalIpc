// Copyright ©2021 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace DotDoc.LocalIpc.Serializers
{
    public interface ISerializer
    {
        public byte[] Serialize(object value);
        public T Deserialize<T>(byte[] bytes);
    }
}
