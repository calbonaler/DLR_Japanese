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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast
{
	/// <summary>
	/// <see cref="GeneratorExpression"/> �� YieldBreak �܂��� YieldReturn ��\���܂��B
	/// <see cref="Value"/> �� <c>null</c> �łȂ��ꍇ�� YieldReturn�A����ȊO�̏ꍇ�� YieldBreak ��\���܂��B
	/// </summary>
	public sealed class YieldExpression : Expression
	{
		/// <summary>���x���A�n�����l�A�}�[�J�[���g�p���āA<see cref="Microsoft.Scripting.Ast.YieldExpression"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="target">���̃W�F�l���[�^��������郉�x�����w�肵�܂��B</param>
		/// <param name="value">���x���œn�����l���w�肵�܂��B</param>
		/// <param name="yieldMarker">�f�o�b�O�p�̃}�[�J�[���w�肵�܂��B</param>
		internal YieldExpression(LabelTarget target, Expression value, int yieldMarker)
		{
			Target = target;
			Value = value;
			YieldMarker = yieldMarker;
		}

		/// <summary>
		/// �m�[�h�����P���ȃm�[�h�ɕό`�ł��邱�Ƃ������܂��B
		/// ���ꂪ <c>true</c> ��Ԃ��ꍇ�A<see cref="Expression.Reduce"/> ���Ăяo���ĒP�������ꂽ�`���𐶐��ł��܂��B
		/// </summary>
		public override bool CanReduce { get { return false; } }

		/// <summary>���� <see cref="System.Linq.Expressions.Expression"/> ���\�����̐ÓI�Ȍ^���擾���܂��B</summary>
		public sealed override Type Type { get { return typeof(void); } }

		/// <summary>
		/// ���̎��̃m�[�h�^��Ԃ��܂��B
		/// �g���m�[�h�́A���̃��\�b�h���I�[�o�[���C�h����Ƃ��A<see cref="System.Linq.Expressions.ExpressionType.Extension"/> ��Ԃ��K�v������܂��B
		/// </summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>���̎����������l���擾���܂��B</summary>
		public Expression Value { get; private set; }

		/// <summary>���̃W�F�l���[�^��������郉�x�����擾���܂��B</summary>
		public LabelTarget Target { get; private set; }

		/// <summary>�f�o�b�O�p�̃}�[�J�[���擾���܂��B</summary>
		public int YieldMarker { get; private set; }

		/// <summary>
		/// �m�[�h��P�������A�P�������ꂽ���� <paramref name="visitor"/> �f���Q�[�g���Ăяo���܂��B
		/// �m�[�h��P�����ł��Ȃ��ꍇ�A���̃��\�b�h�͗�O���X���[���܂��B
		/// </summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> �̃C���X�^���X�B</param>
		/// <returns>�������̎��A�܂��̓c���[���ő������̎��ƒu�������鎮</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor)
		{
			var v = visitor.Visit(Value);
			return v == Value ? this : Utils.MakeYield(Target, v, YieldMarker);
		}
	}

	public partial class Utils
	{
		/// <summary>�w�肳�ꂽ���x���ɏ��������� YieldBreak �X�e�[�g�����g���쐬���܂��B</summary>
		/// <param name="target">�����������郉�x�����w�肵�܂��B</param>
		/// <returns>YieldBreak �X�e�[�g�����g��\�� <see cref="YieldExpression"/>�B</returns>
		public static YieldExpression YieldBreak(LabelTarget target) { return MakeYield(target, null, -1); }

		/// <summary>�w�肳�ꂽ���x���ɏ��������� YieldReturn �X�e�[�g�����g���쐬���܂��B</summary>
		/// <param name="target">�����������郉�x�����w�肵�܂��B</param>
		/// <param name="value">�n�����l���w�肵�܂��B</param>
		/// <returns>YieldReturn �X�e�[�g�����g��\�� <see cref="YieldExpression"/>�B</returns>
		public static YieldExpression YieldReturn(LabelTarget target, Expression value) { return MakeYield(target, value, -1); }

		/// <summary>�w�肳�ꂽ���x���ɏ��������� YieldReturn �X�e�[�g�����g���쐬���܂��B</summary>
		/// <param name="target">�����������郉�x�����w�肵�܂��B</param>
		/// <param name="value">�n�����l���w�肵�܂��B</param>
		/// <param name="yieldMarker">�f�o�b�O�p�̃}�[�J�[���w�肵�܂��B</param>
		/// <returns>YieldReturn �X�e�[�g�����g��\�� <see cref="YieldExpression"/>�B</returns>
		public static YieldExpression YieldReturn(LabelTarget target, Expression value, int yieldMarker)
		{
			ContractUtils.RequiresNotNull(value, "value");
			return MakeYield(target, value, yieldMarker);
		}

		/// <summary>�w�肳�ꂽ���x���ɏ��������� Yield �X�e�[�g�����g���쐬���܂��B</summary>
		/// <param name="target">�����������郉�x�����w�肵�܂��B</param>
		/// <param name="value">�n�����l���w�肵�܂��B</param>
		/// <param name="yieldMarker">�f�o�b�O�p�̃}�[�J�[���w�肵�܂��B</param>
		/// <returns>YieldReturn �X�e�[�g�����g�܂��� YieldBreak �X�e�[�g�����g��\�� <see cref="YieldExpression"/>�B</returns>
		public static YieldExpression MakeYield(LabelTarget target, Expression value, int yieldMarker)
		{
			ContractUtils.RequiresNotNull(target, "target");
			ContractUtils.Requires(target.Type != typeof(void), "target", "�W�F�l���[�^�̃��x���͔� void �^�ł���K�v������܂��B");
			if (value != null && !TypeUtils.AreReferenceAssignable(target.Type, value.Type))
			{
				// C# �͎����I�ɃW�F�l���[�^�̖߂�l�����p���܂�
				if (target.Type.IsSubclassOf(typeof(Expression)) && TypeUtils.AreAssignable(target.Type, value.GetType()))
					value = Expression.Quote(value);
				throw new ArgumentException(string.Format("�^ '{0}' �̎����^ '{1}' �̃W�F�l���[�^���x���ɏ��邱�Ƃ͂ł��܂���B", value.Type, target.Type));
			}
			return new YieldExpression(target, value, yieldMarker);
		}
	}
}
