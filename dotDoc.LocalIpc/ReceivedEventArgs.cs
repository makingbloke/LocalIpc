// Copyright ©2021 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using System;

namespace DotDoc.LocalIpc
{
    public class ReceivedEventArgs: EventArgs
    {
        private readonly object _value;

        public ReceivedEventArgs(object value)
        {
            _value = value;
        }

        public object GetValue()
        {
            return _value;
        }

        public T GetValue<T>()
        {
            return (T)Convert.ChangeType(_value, typeof(T));
        }
    }
}
