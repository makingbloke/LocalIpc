// Copyright ©2021 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using System;

namespace DotDoc.LocalIpc
{
    public class ReceivedEventArgs: EventArgs
    {
        public ReceivedEventArgs(object value)
        {
            Value = value;
        }

        public object Value { get; }
    }
}
