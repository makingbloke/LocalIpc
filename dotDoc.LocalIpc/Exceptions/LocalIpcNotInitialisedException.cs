// Copyright ©2021 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using System;
using System.Runtime.Serialization;

namespace DotDoc.LocalIpc.Exceptions
{
    [Serializable]
    public class LocalIpcNotInitialisedException : Exception
    {
        public LocalIpcNotInitialisedException()
        {
        }

        public LocalIpcNotInitialisedException(string message)
            : base(message)
        {
        }

        public LocalIpcNotInitialisedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected LocalIpcNotInitialisedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
