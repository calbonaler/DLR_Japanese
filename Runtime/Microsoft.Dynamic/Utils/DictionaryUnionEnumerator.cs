/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Scripting.Utils
{
	class DictionaryUnionEnumerator : CheckedDictionaryEnumerator
	{
		IList<IDictionaryEnumerator> _enums;
		int _current = 0;

		public DictionaryUnionEnumerator(IList<IDictionaryEnumerator> enums) { _enums = enums; }

		protected override object KeyCore { get { return _enums[_current].Key; } }

		protected override object ValueCore { get { return _enums[_current].Value; } }

		protected override bool MoveNextCore()
		{
			for (; _current < _enums.Count; _current++)
			{
				if (_enums[_current].MoveNext())
					return true;
			}
			return false;
		}

		protected override void ResetCore()
		{
			foreach (var e in _enums)
				e.Reset();
			_current = 0;
		}
	}
}
