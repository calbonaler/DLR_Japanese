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

using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// �C���^�[�Z�v�^�[�̃v���g�^�C�v�ł��B
	/// �C���^�[�Z�v�^�[�͎��ۂ� <see cref="CallSiteBinder"/> �����b�v���� <see cref="CallSiteBinder"/> �ŁA���b�v���ꂽ�o�C���_�[���������鎮�c���[��ł̔C�ӂ̑�������s�ł��܂��B
	/// </summary>
	/// <remarks>
	/// ���̂悤�ȖړI�ɑ΂��ēK�p�ł��܂��B
	/// * ���c���[�̃_���v
	/// * �ǉ��̏�������
	/// * �ÓI�R���p�C��
	/// </remarks>
	public static class Interceptor
	{
		/// <summary>�w�肳�ꂽ���c���[���C���^�[�Z�v�g���܂��B</summary>
		/// <param name="expression">�C���^�[�Z�v�g���鎮�c���[���w�肵�܂��B</param>
		/// <returns>����������ꂽ���c���[�B</returns>
		public static Expression Intercept(Expression expression) { return new InterceptorWalker().Visit(expression); }

		/// <summary>�w�肳�ꂽ�����_�����C���^�[�Z�v�g���܂��B</summary>
		/// <param name="lambda">�C���^�[�Z�v�g���郉���_�����w�肵�܂��B</param>
		/// <returns>����������ꂽ�����_���B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static LambdaExpression Intercept(LambdaExpression lambda) { return new InterceptorWalker().Visit(lambda) as LambdaExpression; }

		class InterceptorSiteBinder : CallSiteBinder
		{
			readonly CallSiteBinder _binder;

			internal InterceptorSiteBinder(CallSiteBinder binder) { _binder = binder; }

			public override int GetHashCode() { return _binder.GetHashCode(); }

			public override bool Equals(object obj) { return obj != null && obj.Equals(_binder); }

			public override Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel)
			{
				var binding = _binder.Bind(args, parameters, returnLabel);

				//
				// TODO: Implement interceptor action here
				//

				//
				// Call interceptor recursively to continue intercepting on rules
				//
				return Interceptor.Intercept(binding);
			}
		}

		class InterceptorWalker : ExpressionVisitor
		{
			protected override Expression VisitDynamic(DynamicExpression node) { return node.Binder is InterceptorSiteBinder ? node : Expression.MakeDynamic(node.DelegateType, new InterceptorSiteBinder(node.Binder), node.Arguments); }
		}
	}
}
