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

using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>�ǉ��̓񍀉��Z�ɂ��Ẵo�C���f�B���O�����s�ł��� <see cref="BinaryOperationBinder"/> ��\���܂��B</summary>
	public abstract class ExtensionBinaryOperationBinder : BinaryOperationBinder
	{
		/// <summary>���Z�̎�ނ�\����������g�p���āA<see cref="Microsoft.Scripting.Actions.ExtensionBinaryOperationBinder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="operation">���Z�̎�ނ�\����������w�肵�܂��B</param>
		protected ExtensionBinaryOperationBinder(string operation) : base(ExpressionType.Extension)
		{
			ContractUtils.RequiresNotNull(operation, "operation");
			ExtensionOperation = operation;
		}

		/// <summary>���Z�̎�ނ�\����������擾���܂��B</summary>
		public string ExtensionOperation { get; private set; }

		/// <summary>���̃I�u�W�F�N�g�ɂ��Ẵn�b�V���l���v�Z���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̃n�b�V���l�B</returns>
		public override int GetHashCode() { return base.GetHashCode() ^ ExtensionOperation.GetHashCode(); }

		/// <summary>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">��r����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public override bool Equals(object obj)
		{
			var ebob = obj as ExtensionBinaryOperationBinder;
			return ebob != null && base.Equals(obj) && ExtensionOperation == ebob.ExtensionOperation;
		}
	}
}
