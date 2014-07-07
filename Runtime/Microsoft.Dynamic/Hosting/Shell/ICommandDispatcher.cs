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

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary>
	/// �P��̑Θb�R�}���h���f�B�X�p�b�`���邽�߂Ɏg�p����܂��B
	/// ���̃C���^�[�t�F�C�X�̓R�}���h�����s�����X���b�h�A�R�}���h�ɋ��������s���ԂȂǂ𐧌䂷�邽�߂Ɏg�p����܂��B
	/// </summary>
	public interface ICommandDispatcher
	{
		/// <summary>�w�肳�ꂽ�R�[�h���w�肳�ꂽ�X�R�[�v�Ŏ��s�����悤�Ƀf�B�X�p�b�`���āA���ʂ�Ԃ��܂��B</summary>
		/// <param name="compiledCode">���s����R�[�h���w�肵�܂��B</param>
		/// <param name="scope">�R�[�h�����s����X�R�[�v���w�肵�܂��B</param>
		/// <returns>�R�[�h�����s���ꂽ���ʂ�Ԃ��܂��B</returns>
		object Execute(CompiledCode compiledCode, ScriptScope scope);
	}
}
