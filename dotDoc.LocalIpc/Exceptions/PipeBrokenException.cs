// Copyright ©2021 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using System;
using System.Runtime.Serialization;

namespace DotDoc.LocalIpc.Exceptions
{
    [Serializable]
    public class PipeBrokenException : Exception
    {
        public PipeBrokenException()
        {
        }

        public PipeBrokenException(string message)
            : base(message)
        {
        }

        public PipeBrokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected PipeBrokenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
