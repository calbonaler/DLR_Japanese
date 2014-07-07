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
using System.IO;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>�R�[�h��\����������i�[���� <see cref="TextContentProvider"/> ��\���܂��B</summary>
	[Serializable]
	sealed class SourceStringContentProvider : TextContentProvider
	{
		readonly string _code;

		/// <summary>�w�肳�ꂽ�R�[�h���g�p���āA<see cref="Microsoft.Scripting.SourceStringContentProvider"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="code">��ɂȂ�R�[�h��\����������w�肵�܂��B</param>
		internal SourceStringContentProvider(string code)
		{
			ContractUtils.RequiresNotNull(code, "code");
			_code = code;
		}

		/// <summary><see cref="TextContentProvider"/> ���쐬���ꂽ�R���e���c����ɂ���V���� <see cref="TextReader"/> ���쐬���܂��B</summary>
		public override SourceCodeReader GetReader() { return new SourceCodeReader(new StringReader(_code), null); }
	}
}
