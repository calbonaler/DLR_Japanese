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
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Interpreter;

namespace Microsoft.Scripting.Utils
{
	using AstUtils = Microsoft.Scripting.Ast.Utils;

	/// <summary>���I����� <see cref="DynamicMetaObject"/> �Ɋւ��郆�[�e�B���e�B ���\�b�h��񋟂��܂��B</summary>
	public static class DynamicUtils
	{
		/// <summary>�����^�C���l����т��� <see cref="DynamicMetaObject"/> ���o�C���f�B���O�v���Z�X���ɕ\�����ɑ΂��� <see cref="DynamicMetaObject"/> �̃C���X�^���X���쐬���܂��B</summary>
		/// <param name="argValue"><see cref="DynamicMetaObject"/> �ɂ���ĕ\����郉���^�C���l���w�肵�܂��B</param>
		/// <param name="parameterExpression">���� <see cref="DynamicMetaObject"/> ���o�C���f�B���O�v���Z�X���ɕ\�������w�肵�܂��B</param>
		/// <returns><see cref="DynamicMetaObject"/> �̐V�����C���X�^���X�B</returns>
		public static DynamicMetaObject ObjectToMetaObject(object argValue, Expression parameterExpression)
		{
			var ido = argValue as IDynamicMetaObjectProvider;
			return ido != null ? ido.GetMetaObject(parameterExpression) : new DynamicMetaObject(parameterExpression, BindingRestrictions.Empty, argValue);
		}

		/// <summary>�����ɑ΂���o�C���f�B���O���s�� <see cref="CallSite&lt;T&gt;"/> �̃^�[�Q�b�g���X�V���܂��B</summary>
		/// <typeparam name="TDelegate"><see cref="CallSite&lt;T&gt;"/> �̃^�[�Q�b�g�̌^���w�肵�܂��B</typeparam>
		/// <param name="binder">���I��������ۂɃo�C���h���� <see cref="DynamicMetaObjectBinder"/> ���w�肵�܂��B</param>
		/// <param name="site">����̃o�C���h�Ώۂł��� <see cref="CallSite&lt;T&gt;"/> ���w�肵�܂��B</param>
		/// <param name="args">���I����̈����̔z����w�肵�܂��B</param>
		/// <param name="compilationThreshold">�C���^�v���^���R���p�C�����J�n����܂ł̌J��Ԃ������w�肵�܂��B</param>
		/// <returns><see cref="CallSite&lt;T&gt;"/> �̃^�[�Q�b�g��u��������V�����f���Q�[�g�B</returns>
		public static TDelegate LightBind<TDelegate>(this DynamicMetaObjectBinder binder, CallSite<TDelegate> site, object[] args, int compilationThreshold) where TDelegate : class
		{
			var d = Bind<TDelegate>(binder, args).LightCompile(compilationThreshold);
			var lambda = ((Delegate)(object)d).Target as LightLambda;
			if (lambda != null)
				lambda.Compile += (_, e) =>
				{
					// site.Target ���ł܂��K�����g�p����Ă���ꍇ�R���p�C�����ꂽ�f���Q�[�g�Œu�������܂��B
					// site.Target �̓R���p�C�������f���Q�[�g���������ޑO�ɕʂ̃X���b�h�ɂ���čX�V����邱�Ƃ��ł��܂��B
					// ���̂悤�ȏꍇ�A�R���p�C�����ꂽ�K�������s���āA�K�p�ł��Ȃ����Ƃ����m���Ă��烋�[���L���b�V���ɂ���Ēu�������܂��B
					// TODO: ���߂����f���Q�[�g�� L1 ����� L2 �L���b�V�����u��������?
					if (site.Target == d)
						site.Target = (TDelegate)(object)e.Compiled;
				};
			else
				PerfTrack.NoteEvent(PerfTrack.Category.Rules, "Rule not interpreted");
			return d;
		}

		/// <summary>�w�肳�ꂽ�����ɑ΂��ē��I����̃o�C���f�B���O�����s���܂��B</summary>
		/// <typeparam name="TDelegate">�o�C���f�B���O�Ő��������f���Q�[�g�̌^���w�肵�܂��B</typeparam>
		/// <param name="binder">�o�C���f�B���O�����s���� <see cref="DynamicMetaObjectBinder"/> ���w�肵�܂��B</param>
		/// <param name="args">���I����̈����̔z����w�肵�܂��B</param>
		/// <returns>�o�C���f�B���O�̌��ʐ������ꂽ�f���Q�[�g�B</returns>
		public static Expression<TDelegate>/*!*/ Bind<TDelegate>(this DynamicMetaObjectBinder binder, object[] args) where TDelegate : class
		{
			var returnLabel = LambdaSignature<TDelegate>.Instance.ReturnLabel.Type == typeof(object) && binder.ReturnType != typeof(void) && binder.ReturnType != typeof(object) ? Expression.Label(binder.ReturnType) : LambdaSignature<TDelegate>.Instance.ReturnLabel;
			var binding = binder.Bind(args, LambdaSignature<TDelegate>.Instance.Parameters, returnLabel);
			if (binding == null)
				throw new InvalidOperationException("CallSiteBinder.Bind �� null �łȂ�����Ԃ��K�v������܂��B");
			return Stitch<TDelegate>(binding, returnLabel);
		}

		// TODO: This should be merged into CallSiteBinder.
		static Expression<TDelegate>/*!*/ Stitch<TDelegate>(Expression binding, LabelTarget returnLabel) where TDelegate : class
		{
			var updLabel = Expression.Label(CallSiteBinder.UpdateLabel);
			var site = Expression.Parameter(typeof(CallSite), "$site");
			var @params = ArrayUtils.Insert(site, LambdaSignature<TDelegate>.Instance.Parameters);
			Expression body;
			if (returnLabel != LambdaSignature<TDelegate>.Instance.ReturnLabel)
			{
				// TODO:
				// This allows the binder to produce a strongly typed binding expression that gets boxed if the call site's return value is of type object. 
				// The current implementation of CallSiteBinder is too strict as it requires the two types to be reference-assignable.
				var tmp = Expression.Parameter(typeof(object));
				body = Expression.Convert(
					Expression.Block(new[] { tmp },
						binding,
						updLabel,
						Expression.Label(returnLabel,
							Expression.Condition(Expression.NotEqual(Expression.Assign(tmp, Expression.Invoke(Expression.Property(Expression.Convert(site, typeof(CallSite<TDelegate>)), "Update"), @params)), AstUtils.Constant(null)),
								Expression.Convert(tmp, returnLabel.Type),
								Expression.Default(returnLabel.Type)
							)
						)
					), typeof(object)
				);
			}
			else
				body = Expression.Block(
					binding,
					updLabel,
					Expression.Label(returnLabel, Expression.Invoke(Expression.Property(Expression.Convert(site, typeof(CallSite<TDelegate>)), "Update"), @params))
				);
			return Expression.Lambda<TDelegate>(body, "CallSite.Target", true, @params);
		}

		// TODO: This should be merged into CallSiteBinder.
		sealed class LambdaSignature<TDelegate> where TDelegate : class
		{
			internal static readonly LambdaSignature<TDelegate> Instance = new LambdaSignature<TDelegate>();
			internal readonly ReadOnlyCollection<ParameterExpression> Parameters;
			internal readonly LabelTarget ReturnLabel;

			LambdaSignature()
			{
				if (!typeof(Delegate).IsAssignableFrom(typeof(TDelegate)))
					throw new InvalidOperationException();
				var invoke = typeof(TDelegate).GetMethod("Invoke");
				var pis = invoke.GetParameters();
				if (pis[0].ParameterType != typeof(CallSite))
					throw new InvalidOperationException();
				Parameters = new ReadOnlyCollection<ParameterExpression>(pis.Skip(1).Select((x, i) => Expression.Parameter(x.ParameterType, "$arg" + i)).ToArray());
				ReturnLabel = Expression.Label(invoke.ReturnType);
			}
		}
	}
}
