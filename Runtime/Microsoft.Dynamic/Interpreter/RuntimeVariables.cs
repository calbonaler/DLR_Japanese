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

using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary><see cref="IRuntimeVariables"/> �̎�����\���܂��B</summary>
	sealed class RuntimeVariables : IRuntimeVariables
	{
		readonly IStrongBox[] _boxes;

		RuntimeVariables(IStrongBox[] boxes) { _boxes = boxes; }

		int IRuntimeVariables.Count { get { return _boxes.Length; } }

		object IRuntimeVariables.this[int index]
		{
			get { return _boxes[index].Value; }
			set { _boxes[index].Value = value; }
		}

		/// <summary>�w�肳�ꂽ�Q�Ƃɑ΂��� <see cref="RuntimeVariables"/> ���쐬���܂��B</summary>
		/// <param name="boxes"><see cref="RuntimeVariables"/> ������������Q�Ƃ��w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ <see cref="RuntimeVariables"/></returns>
		internal static IRuntimeVariables Create(IStrongBox[] boxes) { return new RuntimeVariables(boxes); }
	}
}
