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

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>�K�p�\�ȃ��\�b�h�Ƃ��̖��O�t�������̊֘A�t�����i�[���܂��B</summary>
	public sealed class ApplicableCandidate
	{
		/// <summary>�K�p�\�ȃ��\�b�h���擾���܂��B</summary>
		public MethodCandidate Method { get; private set; }

		/// <summary>�K�p�\�ȃ��\�b�h�ɑ΂��閼�O�t�������̊֘A�t�����擾���܂��B</summary>
		public ArgumentBinding ArgumentBinding { get; private set; }

		/// <summary>�w�肳�ꂽ���\�b�h�Ɩ��O�t�������̊֘A�t�����g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.ApplicableCandidate"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="method">�K�p�\�ȃ��\�b�h���w�肵�܂��B</param>
		/// <param name="argBinding">�K�p�\�ȃ��\�b�h�ɑ΂��閼�O�t�������̊֘A�t�����w�肵�܂��B</param>
		internal ApplicableCandidate(MethodCandidate method, ArgumentBinding argBinding)
		{
			Assert.NotNull(method, argBinding);
			Method = method;
			ArgumentBinding = argBinding;
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�ɑΉ����鉼�������擾���܂��B</summary>
		/// <param name="argumentIndex">�������ɑΉ�����C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɑΉ����鉼�����B</returns>
		public ParameterWrapper GetParameter(int argumentIndex) { return Method.Parameters[ArgumentBinding.ArgumentToParameter(argumentIndex)]; }

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g�ɑ΂��镶����\���B</returns>
		public override string ToString() { return Method.ToString(); }
	}
}
