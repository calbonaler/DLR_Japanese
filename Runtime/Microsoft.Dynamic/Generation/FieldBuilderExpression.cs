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
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.Scripting.Generation
{
	/// <summary>�^�̍쐬���������Ă��Ȃ��Ƃ��� <see cref="FieldBuilder"/> �𒊏ۍ\���� (AST) �ɖ��ߍ��ނ��Ƃ��ł���P���Ȏ���\���܂��B</summary>
	public class FieldBuilderExpression : Expression
	{
		readonly FieldBuilder _builder;

		/// <summary>���ߍ��� <see cref="FieldBuilder"/> ���g�p���āA<see cref="Microsoft.Scripting.Generation.FieldBuilderExpression"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="builder">���ۍ\���� (AST) �ɖ��ߍ��� <see cref="FieldBuilder"/> ���w�肵�܂��B</param>
		public FieldBuilderExpression(FieldBuilder builder) { _builder = builder; }

		/// <summary>
		/// �m�[�h�����P���ȃm�[�h�ɕό`�ł��邱�Ƃ������܂��B
		/// ���ꂪ <c>true</c> ��Ԃ��ꍇ�A<see cref="Reduce"/> ���Ăяo���ĒP�������ꂽ�`���𐶐��ł��܂��B
		/// </summary>
		public override bool CanReduce { get { return true; } }

		/// <summary>���̎��̃m�[�h�^��Ԃ��܂��B �g���m�[�h�́A���̃��\�b�h���I�[�o�[���C�h����Ƃ��A<see cref="System.Linq.Expressions.ExpressionType.Extension"/> ��Ԃ��K�v������܂��B</summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>���� <see cref="System.Linq.Expressions.Expression"/> ���\�����̐ÓI�Ȍ^���擾���܂��B</summary>
		public sealed override Type Type { get { return _builder.FieldType; } }

		/// <summary>
		/// ���̃m�[�h�����P���Ȏ��ɕό`���܂��B
		/// <see cref="CanReduce"/> �� <c>true</c> ��Ԃ��ꍇ�A����͗L���Ȏ���Ԃ��܂��B
		/// ���̃��\�b�h�́A���ꎩ�̂��P��������K�v������ʂ̃m�[�h��Ԃ��ꍇ������܂��B
		/// </summary>
		/// <returns>�P�������ꂽ���B</returns>
		public override Expression Reduce()
		{
			FieldInfo fi = GetFieldInfo();
			Debug.Assert(fi.Name == _builder.Name);
			return Expression.Field(null, fi);
		}

		FieldInfo GetFieldInfo() { return _builder.DeclaringType.Module.ResolveField(_builder.GetToken().Token); } // FieldBuilder ���쐬���ꂽ�t�B�[���h�ɕϊ�

		/// <summary>�m�[�h��P�������A�P�������ꂽ���� <paramref name="visitor"/> �f���Q�[�g���Ăяo���܂��B</summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> �̃C���X�^���X�B</param>
		/// <returns>�������̎��A�܂��̓c���[���ő������̎��ƒu�������鎮</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor) { return this; }
	}
}
