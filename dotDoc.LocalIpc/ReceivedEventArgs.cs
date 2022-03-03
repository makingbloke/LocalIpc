// Copyright ©2021-2022 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

namespace DotDoc.LocalIpc;

/// <summary>
/// Received Event Args.
/// </summary>
public class ReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReceivedEventArgs"/> class.
    /// </summary>
    /// <param name="value">The value received.</param>
    public ReceivedEventArgs(object value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the value received.
    /// </summary>
    public object Value { get; }
}
