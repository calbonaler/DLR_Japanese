/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * ironruby@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	using AstUtils = Microsoft.Scripting.Ast.Utils;

	/// <summary>
	/// ���I�T�C�g�ɓn���ꂽ���ׂĂ̈����� Func/Action �f���Q�[�g���󂯓���邱�Ƃ��ł��邻���葽���̈����Ń��b�v���܂��B
	/// ���̂悤�ȃT�C�g�ɑ΂���K���𐶐�����o�C���_�[�͂܂����������b�v�������āA�����ɑ΂���o�C���f�B���O�����s����K�v������܂��B
	/// </summary>
	public sealed class ArgumentArray
	{
		readonly object[] _arguments;

		// the index of the first item _arguments that represents an argument:
		readonly int _first;

		/// <summary>�����S�̂Ǝ��ۂɗ��p����͈͂��g�p���āA<see cref="Microsoft.Scripting.Runtime.ArgumentArray"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="arguments">�����S�̂��w�肵�܂��B</param>
		/// <param name="first"><paramref name="arguments"/> �̒��ň����Ƃ��Ďg�p�����ŏ��̈ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="count">���ۂɈ����Ƃ��Ďg�p����v�f�̐����w�肵�܂��B</param>
		internal ArgumentArray(object[] arguments, int first, int count)
		{
			_arguments = arguments;
			_first = first;
			Count = count;
		}

		/// <summary>������\�����X�g������ۂɎg�p�����v�f�̐����擾���܂��B</summary>
		public int Count { get; private set; }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�ɂ���������擾���܂��B</summary>
		/// <param name="index">�擾��������̈ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɂ�������B</returns>
		public object this[int index]
		{
			get
			{
				ContractUtils.RequiresArrayIndex(_arguments, index, "index");
				return _arguments[_first + index];
			}
		}

		/// <summary>�w�肳�ꂽ <see cref="ArgumentArray"/> ��\���C���X�^���X�̎w�肳�ꂽ�C���f�b�N�X�ɂ���������擾���� <see cref="DynamicMetaObject"/> ��Ԃ��܂��B</summary>
		/// <param name="parameter">�������擾���� <see cref="ArgumentArray"/> �̃C���X�^���X��\�������w�肵�܂��B</param>
		/// <param name="index">�擾��������̈ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�ʒu�ɂ������������ <see cref="DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject GetMetaObject(Expression parameter, int index) { return DynamicMetaObject.Create(this[index], Expression.Property(AstUtils.Convert(parameter, typeof(ArgumentArray)), "Item", AstUtils.Constant(index))); }
	}
}
