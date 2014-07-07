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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast
{
	/// <summary>
	/// <see cref="IEnumerable"/>�A<see cref="IEnumerable&lt;T&gt;"/>�A<see cref="IEnumerator"/>�A�܂��� <see cref="IEnumerator&lt;T&gt;"/> �^�̃p�����[�^�̂Ȃ��W�F�l���[�^��\���܂��B
	/// ���̖{�͈̂�A�� <see cref="YieldExpression"/> ���i�[���邱�Ƃ��ł��܂��B
	/// �񋓎q��ł̂��ꂼ��� MoveNext �ւ̌Ăяo���ŃW�F�l���[�^�ɓ���AYieldReturn �܂��� YieldBreak �ɓ�����܂Ŏ������s���܂��B
	/// </summary>
	public sealed class GeneratorExpression : Expression
	{
		Expression _reduced;
		readonly Type _type;

		/// <summary>�w�肳�ꂽ���O�A�^�A���x���A�{�̂��g�p���āA<see cref="Microsoft.Scripting.Ast.GeneratorExpression"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">���̃W�F�l���[�^�̖��O���w�肵�܂��B</param>
		/// <param name="type">���̃W�F�l���[�^�̌^���w�肵�܂��B</param>
		/// <param name="label">���̃W�F�l���[�^���珈�������郉�x�����w�肵�܂��B</param>
		/// <param name="body">���̃W�F�l���[�^�̖{�̂��w�肵�܂��B</param>
		internal GeneratorExpression(string name, Type type, LabelTarget label, Expression body)
		{
			Target = label;
			Body = body;
			_type = type;
			Name = name;
		}

		/// <summary>�m�[�h�����P���ȃm�[�h�ɕό`�ł��邱�Ƃ������܂��B</summary>
		public override bool CanReduce { get { return true; } }

		/// <summary>���� <see cref="System.Linq.Expressions.Expression"/> ���\�����̐ÓI�Ȍ^���擾���܂��B</summary>
		public sealed override Type Type { get { return _type; } }

		/// <summary>���� <see cref="System.Linq.Expressions.Expression"/> �̃m�[�h�^���擾���܂��B</summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>���̃W�F�l���[�^�̖��O���擾���܂��B</summary>
		public string Name { get; private set; }

		/// <summary>YieldBreak �܂��� YieldReturn ���ɂ���Ďg�p����邱�̃W�F�l���[�^���珈�������郉�x�����擾���܂��B</summary>
		public LabelTarget Target { get; private set; }

		/// <summary>���̃W�F�l���[�^�̖{�̂��擾���܂��B�{�̂ɂ� YieldBreak �܂��� YieldReturn �����܂߂邱�Ƃ��ł��܂��B</summary>
		public Expression Body { get; private set; }

		/// <summary>���̃m�[�h�����P���Ȏ��ɕό`���܂��B</summary>
		/// <returns>�P�������ꂽ���B</returns>
		public override Expression Reduce() { return _reduced ?? (_reduced = new GeneratorRewriter(this).Reduce()); }

		/// <summary>�m�[�h��P�������A�P�������ꂽ���� <paramref name="visitor"/> �f���Q�[�g���Ăяo���܂��B</summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> �̃C���X�^���X�B</param>
		/// <returns>�������̎��A�܂��̓c���[���ő������̎��ƒu�������鎮</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor)
		{
			var b = visitor.Visit(Body);
			if (b == Body)
				return this;
			return Utils.Generator(Name, Target, b, Type);
		}

		/// <summary>���̃W�F�l���[�^�̌^�� <see cref="IEnumerable"/> �܂��� <see cref="IEnumerable&lt;T&gt;"/> �Ɠ��������ǂ����������l���擾���܂��B</summary>
		internal bool IsEnumerable { get { return Utils.IsEnumerableType(Type); } }
	}

	public partial class Utils
	{
		/// <summary>�w�肳�ꂽ���x���Ɩ{�̂��g�p���āA<see cref="IEnumerable&lt;T&gt;"/> �^�̃W�F�l���[�^���쐬���܂��BT �� <paramref name="label"/> �̌^�Ɠ������Ȃ�܂��B</summary>
		/// <param name="label">�W�F�l���[�^���珈�������郉�x�����w�肵�܂��B</param>
		/// <param name="body">�W�F�l���[�^�̖{�̂��w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ�W�F�l���[�^�B</returns>
		public static GeneratorExpression Generator(LabelTarget label, Expression body)
		{
			ContractUtils.RequiresNotNull(label, "label");
			ContractUtils.RequiresNotNull(body, "body");
			ContractUtils.Requires(label.Type != typeof(void), "label", "label must have a non-void type");
			return new GeneratorExpression("generator", typeof(IEnumerable<>).MakeGenericType(label.Type), label, body);
		}

		/// <summary>�w�肳�ꂽ���x���Ɩ{�̂��g�p���āA<paramref name="type"/> �^�̃W�F�l���[�^���쐬���܂��BT �� <paramref name="label"/> �̌^�Ɠ������Ȃ�܂��B</summary>
		/// <param name="label">�W�F�l���[�^���珈�������郉�x�����w�肵�܂��B</param>
		/// <param name="body">�W�F�l���[�^�̖{�̂��w�肵�܂��B</param>
		/// <param name="type">�W�F�l���[�^�̌^���w�肵�܂��B�W�F�l���[�^�̌^�� <see cref="IEnumerable"/>�A<see cref="IEnumerable&lt;T&gt;"/>�A<see cref="IEnumerator"/>�A�܂��� <see cref="IEnumerator&lt;T&gt;"/> �̂����ꂩ�ł���K�v������܂��B</param>
		/// <returns>�V�����쐬���ꂽ�W�F�l���[�^�B</returns>
		public static GeneratorExpression Generator(LabelTarget label, Expression body, Type type) { return Generator("generator", label, body, type); }

		/// <summary>�w�肳�ꂽ���O�A���x���Ɩ{�̂��g�p���āA<paramref name="type"/> �^�̃W�F�l���[�^���쐬���܂��BT �� <paramref name="label"/> �̌^�Ɠ������Ȃ�܂��B</summary>
		/// <param name="name">�W�F�l���[�^�̖��O���w�肵�܂��B</param>
		/// <param name="label">�W�F�l���[�^���珈�������郉�x�����w�肵�܂��B</param>
		/// <param name="body">�W�F�l���[�^�̖{�̂��w�肵�܂��B</param>
		/// <param name="type">�W�F�l���[�^�̌^���w�肵�܂��B�W�F�l���[�^�̌^�� <see cref="IEnumerable"/>�A<see cref="IEnumerable&lt;T&gt;"/>�A<see cref="IEnumerator"/>�A�܂��� <see cref="IEnumerator&lt;T&gt;"/> �̂����ꂩ�ł���K�v������܂��B</param>
		/// <returns>�V�����쐬���ꂽ�W�F�l���[�^�B</returns>
		public static GeneratorExpression Generator(string name, LabelTarget label, Expression body, Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(body, "body");
			ContractUtils.RequiresNotNull(label, "label");
			ContractUtils.Requires(label.Type != typeof(void), "label", "label must have a non-void type");
			ContractUtils.Requires(body.Type == typeof(void), "body", "generator body must have a void type");
			// Generator type must be one of: IEnumerable, IEnumerator,
			// IEnumerable<T>, or IEnumerator<T>, where T is label.Ttpe
			if (type.IsGenericType)
			{
				var genType = type.GetGenericTypeDefinition();
				if (genType != typeof(IEnumerable<>) && genType != typeof(IEnumerator<>) || type.GetGenericArguments()[0] != label.Type)
					throw GeneratorTypeMustBeEnumerableOfT(label.Type);
			}
			else if (type != typeof(IEnumerable) && type != typeof(IEnumerator))
				throw GeneratorTypeMustBeEnumerableOfT(label.Type);
			return new GeneratorExpression(name, type, label, body);
		}

		static ArgumentException GeneratorTypeMustBeEnumerableOfT(Type type) { return new ArgumentException(string.Format("Generator must be of type IEnumerable<T>, IEnumerator<T>, IEnumerable, or IEnumerator, where T is '{0}'", type)); }

		/// <summary>�w�肳�ꂽ�^�� <see cref="IEnumerable"/> �� <see cref="IEnumerable&lt;T&gt;"/> �ł��邩�ǂ����𒲂ׂ܂��B</summary>
		/// <param name="type">���ׂ�^���w�肵�܂��B</param>
		/// <returns>�^���񋓌^�ł���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		internal static bool IsEnumerableType(Type type) { return type == typeof(IEnumerable) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>); }

		#region Generator lambda factories

		/// <summary>
		/// �p�����[�^�̂Ȃ��W�F�l���[�^���܂ރ����_�����쐬���܂��B
		/// IEnumerator ��Ԃ��ꍇ�ƂĂ��P���ƂȂ�A�萔���Ԃ̍\�z�ɂȂ�܂��B
		/// �������Ȃ���A���ʂ� IEnumerable �ł���ꍇ�A���ꂼ��� GetEnumerator() �ւ̌Ăяo�����p�����[�^�Ɠ����l�� IEnumerator ��Ԃ����Ƃ��m�F���邽�߂ɁA�c���[�S�̂̒T����K�v�Ƃ��܂��B
		/// </summary>
		/// <param name="label">�����̃W�F�l���[�^���珈�������郉�x�����w�肵�܂��B</param>
		/// <param name="body">�����̃W�F�l���[�^�̖{�̂��w�肵�܂��B</param>
		/// <param name="parameters">�����_���̃p�����[�^���w�肵�܂��B</param>
		/// <returns>�p�����[�^�̂Ȃ��W�F�l���[�^���܂ރ����_���B</returns>
		public static Expression<T> GeneratorLambda<T>(LabelTarget label, Expression body, params ParameterExpression[] parameters) { return (Expression<T>)GeneratorLambda(typeof(T), label, body, null, parameters); }

		/// <summary>
		/// �p�����[�^�̂Ȃ��W�F�l���[�^���܂ރ����_�����쐬���܂��B
		/// IEnumerator ��Ԃ��ꍇ�ƂĂ��P���ƂȂ�A�萔���Ԃ̍\�z�ɂȂ�܂��B
		/// �������Ȃ���A���ʂ� IEnumerable �ł���ꍇ�A���ꂼ��� GetEnumerator() �ւ̌Ăяo�����p�����[�^�Ɠ����l�� IEnumerator ��Ԃ����Ƃ��m�F���邽�߂ɁA�c���[�S�̂̒T����K�v�Ƃ��܂��B
		/// </summary>
		/// <param name="label">�����̃W�F�l���[�^���珈�������郉�x�����w�肵�܂��B</param>
		/// <param name="body">�����̃W�F�l���[�^�̖{�̂��w�肵�܂��B</param>
		/// <param name="name">�����̃W�F�l���[�^�̖��O���w�肵�܂��B</param>
		/// <param name="parameters">�����_���̃p�����[�^���w�肵�܂��B</param>
		/// <returns>�p�����[�^�̂Ȃ��W�F�l���[�^���܂ރ����_���B</returns>
		public static Expression<T> GeneratorLambda<T>(LabelTarget label, Expression body, string name, params ParameterExpression[] parameters) { return (Expression<T>)GeneratorLambda(typeof(T), label, body, name, parameters); }

		/// <summary>
		/// �p�����[�^�̂Ȃ��W�F�l���[�^���܂ރ����_�����쐬���܂��B
		/// IEnumerator ��Ԃ��ꍇ�ƂĂ��P���ƂȂ�A�萔���Ԃ̍\�z�ɂȂ�܂��B
		/// �������Ȃ���A���ʂ� IEnumerable �ł���ꍇ�A���ꂼ��� GetEnumerator() �ւ̌Ăяo�����p�����[�^�Ɠ����l�� IEnumerator ��Ԃ����Ƃ��m�F���邽�߂ɁA�c���[�S�̂̒T����K�v�Ƃ��܂��B
		/// </summary>
		/// <param name="label">�����̃W�F�l���[�^���珈�������郉�x�����w�肵�܂��B</param>
		/// <param name="body">�����̃W�F�l���[�^�̖{�̂��w�肵�܂��B</param>
		/// <param name="name">�����̃W�F�l���[�^�̖��O���w�肵�܂��B</param>
		/// <param name="parameters">�����_���̃p�����[�^���w�肵�܂��B</param>
		/// <returns>�p�����[�^�̂Ȃ��W�F�l���[�^���܂ރ����_���B</returns>
		public static Expression<T> GeneratorLambda<T>(LabelTarget label, Expression body, string name, IEnumerable<ParameterExpression> parameters) { return (Expression<T>)GeneratorLambda(typeof(T), label, body, name, parameters); }

		/// <summary>
		/// �p�����[�^�̂Ȃ��W�F�l���[�^���܂ރ����_�����쐬���܂��B
		/// IEnumerator ��Ԃ��ꍇ�ƂĂ��P���ƂȂ�A�萔���Ԃ̍\�z�ɂȂ�܂��B
		/// �������Ȃ���A���ʂ� IEnumerable �ł���ꍇ�A���ꂼ��� GetEnumerator() �ւ̌Ăяo�����p�����[�^�Ɠ����l�� IEnumerator ��Ԃ����Ƃ��m�F���邽�߂ɁA�c���[�S�̂̒T����K�v�Ƃ��܂��B
		/// </summary>
		/// <param name="delegateType">�Ԃ���郉���_���̌^���w�肵�܂��B</param>
		/// <param name="label">�����̃W�F�l���[�^���珈�������郉�x�����w�肵�܂��B</param>
		/// <param name="body">�����̃W�F�l���[�^�̖{�̂��w�肵�܂��B</param>
		/// <param name="parameters">�����_���̃p�����[�^���w�肵�܂��B</param>
		/// <returns>�p�����[�^�̂Ȃ��W�F�l���[�^���܂ރ����_���B</returns>
		public static LambdaExpression GeneratorLambda(Type delegateType, LabelTarget label, Expression body, params ParameterExpression[] parameters) { return GeneratorLambda(delegateType, label, body, null, parameters); }

		/// <summary>
		/// �p�����[�^�̂Ȃ��W�F�l���[�^���܂ރ����_�����쐬���܂��B
		/// IEnumerator ��Ԃ��ꍇ�ƂĂ��P���ƂȂ�A�萔���Ԃ̍\�z�ɂȂ�܂��B
		/// �������Ȃ���A���ʂ� IEnumerable �ł���ꍇ�A���ꂼ��� GetEnumerator() �ւ̌Ăяo�����p�����[�^�Ɠ����l�� IEnumerator ��Ԃ����Ƃ��m�F���邽�߂ɁA�c���[�S�̂̒T����K�v�Ƃ��܂��B
		/// </summary>
		/// <param name="delegateType">�Ԃ���郉���_���̌^���w�肵�܂��B</param>
		/// <param name="label">�����̃W�F�l���[�^���珈�������郉�x�����w�肵�܂��B</param>
		/// <param name="body">�����̃W�F�l���[�^�̖{�̂��w�肵�܂��B</param>
		/// <param name="name">�����̃W�F�l���[�^�̖��O���w�肵�܂��B</param>
		/// <param name="parameters">�����_���̃p�����[�^���w�肵�܂��B</param>
		/// <returns>�p�����[�^�̂Ȃ��W�F�l���[�^���܂ރ����_���B</returns>
		public static LambdaExpression GeneratorLambda(Type delegateType, LabelTarget label, Expression body, string name, params ParameterExpression[] parameters) { return GeneratorLambda(delegateType, label, body, name, (IEnumerable<ParameterExpression>)parameters); }

		/// <summary>
		/// �p�����[�^�̂Ȃ��W�F�l���[�^���܂ރ����_�����쐬���܂��B
		/// IEnumerator ��Ԃ��ꍇ�ƂĂ��P���ƂȂ�A�萔���Ԃ̍\�z�ɂȂ�܂��B
		/// �������Ȃ���A���ʂ� IEnumerable �ł���ꍇ�A���ꂼ��� GetEnumerator() �ւ̌Ăяo�����p�����[�^�Ɠ����l�� IEnumerator ��Ԃ����Ƃ��m�F���邽�߂ɁA�c���[�S�̂̒T����K�v�Ƃ��܂��B
		/// </summary>
		/// <param name="delegateType">�Ԃ���郉���_���̌^���w�肵�܂��B</param>
		/// <param name="label">�����̃W�F�l���[�^���珈�������郉�x�����w�肵�܂��B</param>
		/// <param name="body">�����̃W�F�l���[�^�̖{�̂��w�肵�܂��B</param>
		/// <param name="name">�����̃W�F�l���[�^�̖��O���w�肵�܂��B</param>
		/// <param name="parameters">�����_���̃p�����[�^���w�肵�܂��B</param>
		/// <returns>�p�����[�^�̂Ȃ��W�F�l���[�^���܂ރ����_���B</returns>
		public static LambdaExpression GeneratorLambda(Type delegateType, LabelTarget label, Expression body, string name, IEnumerable<ParameterExpression> parameters)
		{
			ContractUtils.RequiresNotNull(delegateType, "delegateType");
			ContractUtils.Requires(typeof(Delegate).IsAssignableFrom(delegateType) && !delegateType.IsAbstract, "Lambda type parameter must be derived from System.Delegate");
			var generatorType = delegateType.GetMethod("Invoke").GetReturnType();
			if (IsEnumerableType(generatorType))
				body = TransformEnumerable(body, parameters); // rewrite body
			return Expression.Lambda(delegateType, Generator(name, label, body, generatorType), name, parameters);
		}

		// Creates a GeneratorLambda as a lambda containing a parameterless
		// generator. Because we want parameters to be captured by value and
		// not as variables, we have to do a transformation more like this:
		//    static IEnumerable<int> Foo(int count) {
		//        count *= 2;
		//        for (int i = 0; i < count; i++) {
		//            yield return i;
		//        }
		//    }
		//
		// Becomes:
		//
		//    static IEnumerable<int> Foo(int count) {
		//        return generator {
		//            int __count = count;
		//            __count *= 2;
		//            for (int i = 0; i < __count; i++) {
		//                yield return i;
		//            }
		//        }
		//    }
		//
		// This involves a full rewrite, unfortunately.
		static Expression TransformEnumerable(Expression body, IEnumerable<ParameterExpression> parameters)
		{
			var paramList = parameters.ToArray();
			if (paramList.Length == 0)
				return body;
			var vars = new ParameterExpression[paramList.Length];
			var map = new Dictionary<ParameterExpression, ParameterExpression>(paramList.Length);
			var block = new Expression[paramList.Length + 1];
			for (int i = 0; i < paramList.Length; i++)
			{
				map.Add(paramList[i], vars[i] = Expression.Variable(paramList[i].Type, paramList[i].Name));
				block[i] = Expression.Assign(vars[i], paramList[i]);
			}
			block[paramList.Length] = new LambdaParameterRewriter(map).Visit(body);
			return Expression.Block(vars, block);
		}
		#endregion
	}
}
