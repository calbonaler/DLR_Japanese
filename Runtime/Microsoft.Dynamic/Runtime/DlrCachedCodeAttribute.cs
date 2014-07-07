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
using System.Collections.ObjectModel;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>�ۑ������ <see cref="ScriptCode"/> �ɓK�p����A�f�B�X�N����� <see cref="ScriptCode"/> �̍č쐬�Ɏg�p����鑮����\���܂��B</summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class DlrCachedCodeAttribute : Attribute { }

	/// <summary>�L���b�V������œK������Ă��郁�\�b�h���}�[�N���܂��B</summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class CachedOptimizedCodeAttribute : Attribute
	{
		readonly ReadOnlyCollection<string> _names;

		/// <summary><see cref="Microsoft.Scripting.Runtime.CachedOptimizedCodeAttribute"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public CachedOptimizedCodeAttribute() { _names = EmptyReadOnlyCollection<string>.Instance; }

		/// <summary>�X�R�[�v���̖��O���g�p���āA<see cref="Microsoft.Scripting.Runtime.CachedOptimizedCodeAttribute"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="names">�œK�����ꂽ�X�R�[�v�Ɋi�[����Ă��閼�O���w�肵�܂��B</param>
		public CachedOptimizedCodeAttribute(string[] names)
		{
			ContractUtils.RequiresNotNull(names, "names");
			_names = names.ToReadOnly();
		}

		/// <summary>�œK�����ꂽ�X�R�[�v�Ɋi�[����Ă��閼�O���擾���܂��B</summary>
		public ReadOnlyCollection<string> Names { get { return _names; } }
	}
}
