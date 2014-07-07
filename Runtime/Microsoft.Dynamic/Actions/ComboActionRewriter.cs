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
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// ���I�T�C�g������铮�I�T�C�g�������A�P��̃R���{���I�T�C�g�ɕό`���鎮�c���[�̃����C�^�[��\���܂��B
	/// �R���{���I�T�C�g�͌ʂ̃��^�o�C���_�[�����s���A�P��̓��I�T�C�g�Ō��ʂ̃R�[�h�𐶐����܂��B
	/// </summary>
	public class ComboActionRewriter : ExpressionVisitor
	{
		/// <summary>
		/// �R���{���I�T�C�g�̐����Ɏg�p����k�މ\�ȃm�[�h�ł��B
		/// ���I�T�C�g�𔭌����邽�тɁA������ <see cref="ComboDynamicSiteExpression"/> �ɒu�����܂��B
		/// ���I�T�C�g�̎q�� <see cref="ComboDynamicSiteExpression"/> �ƂȂ�ꍇ�́A�o�C���f�B���O�}�b�s���O�����X�V���Ďq��e�ƃ}�[�W���܂��B
		/// ���͂̂��� 1 �ł�����p�𔭐�������ꍇ�́A�������~���܂��B
		/// </summary>
		class ComboDynamicSiteExpression : Expression
		{
			readonly Type _type;

			public ComboDynamicSiteExpression(Type type, List<BinderMappingInfo> binders, Expression[] inputs)
			{
				Binders = binders;
				Inputs = inputs;
				_type = type;
			}

			public override bool CanReduce { get { return true; } }

			public sealed override Type Type { get { return _type; } }

			public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

			public Expression[] Inputs { get; private set; }

			public List<BinderMappingInfo> Binders { get; private set; }

			public override Expression Reduce() { return Expression.Dynamic(new ComboBinder(Binders), Type, Inputs); } // we just reduce to a simple DynamicExpression
		}

		/// <summary><see cref="System.Linq.Expressions.DynamicExpression"/> �̎q�𑖍����܂��B</summary>
		/// <param name="node">�������鎮�B</param>
		/// <returns>���܂��͂����ꂩ�̕��������ύX���ꂽ�ꍇ�͕ύX���ꂽ���B����ȊO�̏ꍇ�͌��̎��B</returns>
		protected override Expression VisitDynamic(DynamicExpression node)
		{
			var metaBinder = node.Binder as DynamicMetaObjectBinder;
			if (metaBinder == null)
				return node; // �g�ݍ��킹�邱�Ƃ��ł��Ȃ��̂ŁADynamicMetaObjectBinder �ȊO�̃o�C���_�[�ɂ��m�[�h�̓����C�g���܂���B
			// �V�������I�T�C�g�m�[�h�̂��߂Ɏ����������W���܂��B
			bool foundSideEffectingArgs = false;
			List<Expression> inputs = new List<Expression>();
			// �����}�b�s���O�͂��ꂼ��̃��^�o�C���_�[�ɑ΂��� 1 �� List<ComboParameterMappingInfo> �ŁA�������X�g�͂��ꂼ��̓���̃o�C���_�[�ɑ΂���}�b�s���O���܂݂܂��B
			List<BinderMappingInfo> binders = new List<BinderMappingInfo>();
			List<ParameterMappingInfo> myInfo = new List<ParameterMappingInfo>();
			int actionCount = 0;
			foreach (var e in node.Arguments)
			{
				if (!foundSideEffectingArgs)
				{
					// �����̌����������܂�...
					var rewritten = Visit(e);
					var combo = rewritten as ComboDynamicSiteExpression;
					ConstantExpression ce;
					if (combo != null)
					{
						// ����玩�g�̎��Ƒg�ݍ��킹��A�N�V�������͂���܂ł����̃A�N�V�����������������L�����܂��B
						// �����̎q���A�N�V�����������ꍇ�A�����̃I�t�Z�b�g�͉����グ���邽�߁B
						int baseActionCount = actionCount;
						binders.AddRange(combo.Binders.Select(x => new BinderMappingInfo(x.Binder, x.MappingInfo.Select(y =>
						{
							if (y.IsParameter)
							{
								y = ParameterMappingInfo.Parameter(inputs.Count); // �q����̂��ׂĂ̓��͂͂����Ŏ��������̂��̂�
								inputs.Add(combo.Inputs[y.ParameterIndex]);
							}
							else if (y.IsAction)
							{
								y = ParameterMappingInfo.Action(y.ActionIndex + baseActionCount);
								actionCount++;
							}
							else
								Debug.Assert(y.Constant != null); // �萔�͂��̂܂ܗ���
							return y;
						}).ToArray())));
						myInfo.Add(ParameterMappingInfo.Action(actionCount++));
					}
					else if ((ce = rewritten as ConstantExpression) != null)
						myInfo.Add(ParameterMappingInfo.Fixed(ce)); // �萔�̓R���{�ɒ�����
					else if (IsSideEffectFree(rewritten))
					{
						// ����͓��͈����Ƃ��Ĉ�����
						myInfo.Add(ParameterMappingInfo.Parameter(inputs.Count));
						inputs.Add(rewritten);
					}
					else
					{
						// ���̈����͗����ł��Ȃ����Ƃ����Ă��邽�߂��̂܂܂ɂ��Ȃ���΂Ȃ炸�A
						// ���ׂĂ̎c��̈����͂��ꂪ����p��^���邩�̂悤�ɒʏ�ʂ�]�������K�v������B
						foundSideEffectingArgs = true;
						myInfo.Add(ParameterMappingInfo.Parameter(inputs.Count));
						inputs.Add(e);
					}
				}
				else
				{
					// ����p�����邩������Ȃ������ɏo����Ă��܂������߁A����ȏ�̌����͂ł��Ȃ�
					myInfo.Add(ParameterMappingInfo.Parameter(inputs.Count));
					inputs.Add(e);
				}
			}
			binders.Add(new BinderMappingInfo(metaBinder, myInfo));
			// TODO: ������d�����Ă�����͂���菜�� (�Ⴆ�Ε�����^�����郍�[�J���Ȃ�)
			return new ComboDynamicSiteExpression(node.Type, binders, inputs.ToArray());
		}

		bool IsSideEffectFree(Expression rewritten)
		{
			if (rewritten is ParameterExpression)
				return true;
			if (rewritten.NodeType == ExpressionType.TypeIs)
				return IsSideEffectFree(((UnaryExpression)rewritten).Operand);
			var be = rewritten as BinaryExpression;
			if (be != null && be.Method == null && IsSideEffectFree(be.Left) && IsSideEffectFree(be.Right))
				return true;
			var mc = rewritten as MethodCallExpression;
			if (mc != null && mc.Method != null)
				return mc.Method.IsDefined(typeof(NoSideEffectsAttribute), false);
			var ce = rewritten as ConditionalExpression;
			if (ce != null)
				return IsSideEffectFree(ce.Test) && IsSideEffectFree(ce.IfTrue) && IsSideEffectFree(ce.IfFalse);
			var me = rewritten as MemberExpression;
			if (me != null && me.Member is System.Reflection.FieldInfo)
				return false;
			return false;
		}
	}
}
