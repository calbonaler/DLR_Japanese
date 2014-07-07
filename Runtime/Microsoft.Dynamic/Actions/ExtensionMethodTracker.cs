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

using System;
using System.Reflection;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>�g�����\�b�h��\���܂��B</summary>
	public class ExtensionMethodTracker : MethodTracker
	{
		readonly Type _declaringType;

		/// <summary>���\�b�h�A�ÓI���A�g�����\�b�h���g������^���g�p���āA<see cref="Microsoft.Scripting.Actions.ExtensionMethodTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="method">�g�����\�b�h��\�� <see cref="MethodInfo"/> ���w�肵�܂��B</param>
		/// <param name="isStatic">�w�肳�ꂽ�g�����\�b�h���ÓI���ǂ����������l���w�肵�܂��B</param>
		/// <param name="declaringType">�w�肳�ꂽ�g�����\�b�h���g������^���w�肵�܂��B</param>
		internal ExtensionMethodTracker(MethodInfo method, bool isStatic, Type declaringType)
			: base(method, isStatic)
		{
			ContractUtils.RequiresNotNull(declaringType, "declaringType");
			_declaringType = declaringType;
		}

		/// <summary>
		/// �g�����\�b�h�̐錾����^���擾���܂��B
		/// ���̃��\�b�h�͊g�����\�b�h�Ȃ̂ŁA�錾����^�͎��ۂɂ͎q�̊g�����\�b�h���g������^�ł���A���ۂɐ錾���ꂽ�^�Ƃ͈قȂ�܂��B
		/// </summary>
		public override Type DeclaringType { get { return _declaringType; } }
	}
}
