// Copyright ©2021-2022 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using System.Runtime.Serialization;

namespace DotDoc.LocalIpc.Exceptions;

/// <summary>
/// Pipe Broken Exception.
/// </summary>
[Serializable]
public class PipeBrokenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipeBrokenException"/> class.
    /// </summary>
    public PipeBrokenException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PipeBrokenException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">Error message.</param>
    public PipeBrokenException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PipeBrokenException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public PipeBrokenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PipeBrokenException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The serialization info <see cref="SerializationInfo"/>.</param>
    /// <param name="context">The streaming context <see cref="StreamingContext"/>.</param>
    protected PipeBrokenException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
