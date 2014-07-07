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
using System.Linq.Expressions;
using System.Reflection;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Ast
{
	/// <summary>
	/// <see cref="SymbolId"/> �̒萔��\���܂��B
	/// ���̃m�[�h�͏k�މ\�ł���AGlobalOptimizedRewriter �ɂ���ă����C�g����܂��B
	/// TODO: ���̃m�[�h�� GlobalOptimizedRewriter �������Ɍ^�w�肳�ꂽ�m�[�h��F�����A�����C�g�ł���悤�ɂ��邽�߂ɑ��݂��܂��B
	/// �@�\���K�v�Ȃ��Ȃ�΁A���̃N���X����菜����܂��B
	/// ���̌^����菜���ꂽ�ꍇ�A<see cref="Microsoft.Scripting.Ast.Utils.Constant(object)"/> �̖߂�l�̌^�� <see cref="Expression"/> ���� <see cref="ConstantExpression"/> �ɕύX���܂��B
	/// </summary>
	sealed class SymbolConstantExpression : Expression
	{
		/// <summary>�w�肳�ꂽ <see cref="SymbolId"/> ���g�p���āA<see cref="Microsoft.Scripting.Ast.SymbolConstantExpression"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="value">���̃m�[�h�Ɋi�[���� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		internal SymbolConstantExpression(SymbolId value) { Value = value; }

		/// <summary>
		/// �m�[�h�����P���ȃm�[�h�ɕό`�ł��邱�Ƃ������܂��B
		/// ���ꂪ <c>true</c> ��Ԃ��ꍇ�A<see cref="Reduce"/> ���Ăяo���ĒP�������ꂽ�`���𐶐��ł��܂��B
		/// </summary>
		public override bool CanReduce { get { return true; } }

		/// <summary>���� <see cref="System.Linq.Expressions.Expression"/> ���\�����̐ÓI�Ȍ^���擾���܂��B</summary>
		public sealed override Type Type { get { return typeof(SymbolId); } }

		/// <summary>
		/// ���̎��̃m�[�h�^��Ԃ��܂��B
		/// �g���m�[�h�́A���̃��\�b�h���I�[�o�[���C�h����Ƃ��A<see cref="System.Linq.Expressions.ExpressionType.Extension"/> ��Ԃ��K�v������܂��B
		/// </summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>���̃m�[�h�Ɋi�[����Ă��� <see cref="SymbolId"/> ���擾���܂��B</summary>
		public SymbolId Value { get; private set; }

		static readonly Expression _SymbolIdEmpty = Expression.Field(null, typeof(SymbolId).GetField("Empty"));
		static readonly Expression _SymbolIdInvalid = Expression.Field(null, typeof(SymbolId).GetField("Invalid"));
		static readonly ConstructorInfo _SymbolIdCtor = typeof(SymbolId).GetConstructor(new[] { typeof(int) });

		/// <summary>
		/// ���̃m�[�h�����P���Ȏ��ɕό`���܂��B
		/// <see cref="CanReduce"/> �� <c>true</c> ��Ԃ��ꍇ�A����͗L���Ȏ���Ԃ��܂��B
		/// ���̃��\�b�h�́A���ꎩ�̂��P��������K�v������ʂ̃m�[�h��Ԃ��ꍇ������܂��B
		/// </summary>
		/// <returns>�P�������ꂽ���B</returns>
		public override Expression Reduce() { return GetExpression(Value); }

		static Expression GetExpression(SymbolId value)
		{
			if (value == SymbolId.Empty)
				return _SymbolIdEmpty;
			else if (value == SymbolId.Invalid)
				return _SymbolIdInvalid;
			else
				return Expression.New(_SymbolIdCtor, AstUtils.Constant(value.Id));
		}

		/// <summary>
		/// �m�[�h��P�������A�P�������ꂽ���� <paramref name="visitor"/> �f���Q�[�g���Ăяo���܂��B
		/// �m�[�h��P�����ł��Ȃ��ꍇ�A���̃��\�b�h�͗�O���X���[���܂��B
		/// </summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> �̃C���X�^���X�B</param>
		/// <returns>�������̎��A�܂��̓c���[���ő������̎��ƒu�������鎮</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor) { return this; }
	}
}
