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

using Microsoft.Contracts;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>
	/// ����� <see cref="LanguageContext"/> �Ɋ֘A�t����ꂽ�R���p�C�����ꂽ�R�[�h�̃C���X�^���X��\���܂��B
	/// �R�[�h�͈قȂ�X�R�[�v�ŕ�������s�ł��܂��B
	/// ���̃N���X�ɑ΂������ 1 �̃z�X�e�B���O API �� <see cref="Microsoft.Scripting.Hosting.CompiledCode"/> �ł��B
	/// </summary>
	public abstract class ScriptCode
	{
		/// <summary>�|��P�ʂ��g�p���āA<see cref="Microsoft.Scripting.ScriptCode"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="sourceUnit">���̃N���X�Ɋ֘A�Â�����<see cref="LanguageContext"/> ��ێ����Ă��� <see cref="SourceUnit"/> �I�u�W�F�N�g���w�肵�܂��B</param>
		protected ScriptCode(SourceUnit sourceUnit)
		{
			ContractUtils.RequiresNotNull(sourceUnit, "sourceUnit");
			SourceUnit = sourceUnit;
		}

		/// <summary>���̃N���X�Ɋ֘A�t�����Ă��� <see cref="LanguageContext"/> ���擾���܂��B</summary>
		public LanguageContext LanguageContext { get { return SourceUnit.LanguageContext; } }

		/// <summary>���̃N���X�̃R�[�h��ێ����Ă��� <see cref="SourceUnit"/> ���擾���܂��B</summary>
		public SourceUnit SourceUnit { get; private set; }

		/// <summary>�V���� <see cref="Scope"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <returns>�V���� <see cref="Scope"/> �I�u�W�F�N�g�B</returns>
		public virtual Scope CreateScope() { return new Scope(); }

		/// <summary>�V�����X�R�[�v�ł��̃R�[�h�����s���܂��B</summary>
		/// <returns>���̃R�[�h�̎��s���ʁB</returns>
		public virtual object Run() { return Run(CreateScope()); }

		/// <summary>�w�肳�ꂽ�X�R�[�v�ł��̃R�[�h�����s���܂��B</summary>
		/// <param name="scope">���̃R�[�h�����s����X�R�[�v���w�肵�܂��B</param>
		/// <returns>���̃R�[�h�̎��s���ʁB</returns>
		public abstract object Run(Scope scope);

		/// <summary>���̃I�u�W�F�N�g�̕�����\����Ԃ��܂��B</summary>
		/// <returns>������\���B</returns>
		[Confined]
		public override string ToString() { return string.Format("ScriptCode '{0}' from {1}", SourceUnit.Path, LanguageContext.GetType().Name); }
	}
}
