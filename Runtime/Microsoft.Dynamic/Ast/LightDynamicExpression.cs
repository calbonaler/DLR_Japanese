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
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Interpreter;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast
{
	/// <summary>インタプリタによって認識される動的操作を表します。</summary>
	public abstract class LightDynamicExpression : Expression, IInstructionProvider
	{
		/// <summary>指定されたバインダーを使用して、<see cref="Microsoft.Scripting.Ast.LightDynamicExpression"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		protected LightDynamicExpression(CallSiteBinder binder)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			Binder = binder;
		}

		/// <summary>
		/// ノードをより単純なノードに変形できることを示します。
		/// これが <c>true</c> を返す場合、<see cref="Reduce"/> を呼び出して単純化された形式を生成できます。
		/// </summary>
		public sealed override bool CanReduce { get { return true; } }

		/// <summary>動的サイトの実行時の動作を決定する <see cref="System.Runtime.CompilerServices.CallSiteBinder"/> を取得します。</summary>
		public CallSiteBinder Binder { get; private set; }

		/// <summary>この式のノード型を返します。 拡張ノードは、このメソッドをオーバーライドするとき、<see cref="System.Linq.Expressions.ExpressionType.Extension"/> を返す必要があります。</summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>この <see cref="System.Linq.Expressions.Expression"/> が表す式の静的な型を取得します。</summary>
		public override Type Type { get { return typeof(object); } }

		/// <summary>指定されたインタプリタにこのオブジェクトが表す命令を追加します。</summary>
		/// <param name="compiler">命令を追加するインタプリタを指定します。</param>
		public void AddInstructions(LightCompiler compiler)
		{
			var instr = DynamicInstructionN.CreateUntypedInstruction(Binder, ArgumentCount);
			if (instr == null)
			{
				var lightBinder = Binder as ILightCallSiteBinder;
				if (lightBinder == null || !lightBinder.AcceptsArgumentArray)
				{
					compiler.Compile(Reduce());
					return;
				}
				Debug.Assert(Type == typeof(object));
				instr = new DynamicSplatInstruction(ArgumentCount, CallSite<Func<CallSite, ArgumentArray, object>>.Create(Binder));
			}
			for (int i = 0; i < ArgumentCount; i++)
				compiler.Compile(GetArgument(i));
			compiler.Instructions.Emit(instr);
		}

		/// <summary>
		/// このノードをより単純な式に変形します。
		/// <see cref="CanReduce"/> が <c>true</c> を返す場合、これは有効な式を返します。
		/// このメソッドは、それ自体も単純化する必要がある別のノードを返す場合があります。
		/// </summary>
		/// <returns>単純化された式。</returns>
		public abstract override Expression Reduce();

		/// <summary>このノードが表す動的操作の引数の個数を取得します。</summary>
		protected abstract int ArgumentCount { get; }

		/// <summary>指定されたインデックスに存在する動的操作の引数を取得します。</summary>
		/// <param name="index">動的操作の引数を取得する 0 から始まるインデックスを指定します。</param>
		/// <returns>指定されたインデックスに存在する動的操作の引数。</returns>
		protected abstract Expression GetArgument(int index);
	}

	#region Specialized Subclasses

	/// <summary>インタプリタによって認識される 1 個の引数をとる動的操作を表します。</summary>
	public class LightDynamicExpression1 : LightDynamicExpression
	{
		/// <summary>指定されたバインダーと引数を使用して、<see cref="Microsoft.Scripting.Ast.LightDynamicExpression1"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="arg0">動的操作の 1 番目の引数を指定します。</param>
		protected internal LightDynamicExpression1(CallSiteBinder binder, Expression arg0) : base(binder)
		{
			ContractUtils.RequiresNotNull(arg0, "arg0");
			Argument0 = arg0;
		}

		/// <summary>
		/// このノードをより単純な式に変形します。
		/// <see cref="Expression.CanReduce"/> が <c>true</c> を返す場合、これは有効な式を返します。
		/// このメソッドは、それ自体も単純化する必要がある別のノードを返す場合があります。
		/// </summary>
		/// <returns>単純化された式。</returns>
		public override Expression Reduce() { return Expression.Dynamic(Binder, Type, Argument0); }

		/// <summary>このノードが表す動的操作の引数の個数を取得します。</summary>
		protected sealed override int ArgumentCount { get { return 1; } }

		/// <summary>動的操作の 1 番目の引数を取得します。</summary>
		public Expression Argument0 { get; private set; }

		/// <summary>指定されたインデックスに存在する動的操作の引数を取得します。</summary>
		/// <param name="index">動的操作の引数を取得する 0 から始まるインデックスを指定します。</param>
		/// <returns>指定されたインデックスに存在する動的操作の引数。</returns>
		protected sealed override Expression GetArgument(int index)
		{
			switch (index)
			{
				case 0: return Argument0;
				default: throw Assert.Unreachable;
			}
		}
	}

	class LightTypedDynamicExpression1 : LightDynamicExpression1
	{
		readonly Type _returnType;

		internal LightTypedDynamicExpression1(CallSiteBinder binder, Type returnType, Expression arg0) : base(binder, arg0)
		{
			ContractUtils.RequiresNotNull(returnType, "returnType");
			_returnType = returnType;
		}

		public sealed override Type Type { get { return _returnType; } }
	}

	/// <summary>インタプリタによって認識される 2 個の引数をとる動的操作を表します。</summary>
	public class LightDynamicExpression2 : LightDynamicExpression
	{
		/// <summary>指定されたバインダーと引数を使用して、<see cref="Microsoft.Scripting.Ast.LightDynamicExpression2"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="arg0">動的操作の 1 番目の引数を指定します。</param>
		/// <param name="arg1">動的操作の 2 番目の引数を指定します。</param>
		protected internal LightDynamicExpression2(CallSiteBinder binder, Expression arg0, Expression arg1) : base(binder)
		{
			ContractUtils.RequiresNotNull(arg0, "arg0");
			ContractUtils.RequiresNotNull(arg1, "arg1");
			Argument0 = arg0;
			Argument1 = arg1;
		}

		/// <summary>
		/// このノードをより単純な式に変形します。
		/// <see cref="Expression.CanReduce"/> が <c>true</c> を返す場合、これは有効な式を返します。
		/// このメソッドは、それ自体も単純化する必要がある別のノードを返す場合があります。
		/// </summary>
		/// <returns>単純化された式。</returns>
		public override Expression Reduce() { return Expression.Dynamic(Binder, Type, Argument0, Argument1); }

		/// <summary>このノードが表す動的操作の引数の個数を取得します。</summary>
		protected override int ArgumentCount { get { return 2; } }

		/// <summary>動的操作の 1 番目の引数を取得します。</summary>
		public Expression Argument0 { get; private set; }

		/// <summary>動的操作の 2 番目の引数を取得します。</summary>
		public Expression Argument1 { get; private set; }

		/// <summary>指定されたインデックスに存在する動的操作の引数を取得します。</summary>
		/// <param name="index">動的操作の引数を取得する 0 から始まるインデックスを指定します。</param>
		/// <returns>指定されたインデックスに存在する動的操作の引数。</returns>
		protected override Expression GetArgument(int index)
		{
			switch (index)
			{
				case 0: return Argument0;
				case 1: return Argument1;
				default: throw Assert.Unreachable;
			}
		}
	}

	class LightTypedDynamicExpression2 : LightDynamicExpression2
	{
		readonly Type _returnType;

		internal LightTypedDynamicExpression2(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1) : base(binder, arg0, arg1)
		{
			ContractUtils.RequiresNotNull(returnType, "returnType");
			_returnType = returnType;
		}

		public sealed override Type Type { get { return _returnType; } }
	}

	/// <summary>インタプリタによって認識される 3 個の引数をとる動的操作を表します。</summary>
	public class LightDynamicExpression3 : LightDynamicExpression
	{
		/// <summary>指定されたバインダーと引数を使用して、<see cref="Microsoft.Scripting.Ast.LightDynamicExpression3"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="arg0">動的操作の 1 番目の引数を指定します。</param>
		/// <param name="arg1">動的操作の 2 番目の引数を指定します。</param>
		/// <param name="arg2">動的操作の 3 番目の引数を指定します。</param>
		protected internal LightDynamicExpression3(CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2) : base(binder)
		{
			ContractUtils.RequiresNotNull(arg0, "arg0");
			ContractUtils.RequiresNotNull(arg1, "arg1");
			ContractUtils.RequiresNotNull(arg2, "arg2");
			Argument0 = arg0;
			Argument1 = arg1;
			Argument2 = arg2;
		}

		/// <summary>
		/// このノードをより単純な式に変形します。
		/// <see cref="Expression.CanReduce"/> が <c>true</c> を返す場合、これは有効な式を返します。
		/// このメソッドは、それ自体も単純化する必要がある別のノードを返す場合があります。
		/// </summary>
		/// <returns>単純化された式。</returns>
		public override Expression Reduce() { return Expression.Dynamic(Binder, Type, Argument0, Argument1, Argument2); }

		/// <summary>このノードが表す動的操作の引数の個数を取得します。</summary>
		protected sealed override int ArgumentCount { get { return 3; } }

		/// <summary>動的操作の 1 番目の引数を取得します。</summary>
		public Expression Argument0 { get; private set; }

		/// <summary>動的操作の 2 番目の引数を取得します。</summary>
		public Expression Argument1 { get; private set; }

		/// <summary>動的操作の 3 番目の引数を取得します。</summary>
		public Expression Argument2 { get; private set; }

		/// <summary>指定されたインデックスに存在する動的操作の引数を取得します。</summary>
		/// <param name="index">動的操作の引数を取得する 0 から始まるインデックスを指定します。</param>
		/// <returns>指定されたインデックスに存在する動的操作の引数。</returns>
		protected sealed override Expression GetArgument(int index)
		{
			switch (index)
			{
				case 0: return Argument0;
				case 1: return Argument1;
				case 2: return Argument2;
				default: throw Assert.Unreachable;
			}
		}
	}

	class LightTypedDynamicExpression3 : LightDynamicExpression3
	{
		readonly Type _returnType;

		internal LightTypedDynamicExpression3(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2) : base(binder, arg0, arg1, arg2)
		{
			ContractUtils.RequiresNotNull(returnType, "returnType");
			_returnType = returnType;
		}

		public sealed override Type Type { get { return _returnType; } }
	}

	/// <summary>インタプリタによって認識される 4 個の引数をとる動的操作を表します。</summary>
	public class LightDynamicExpression4 : LightDynamicExpression
	{
		/// <summary>指定されたバインダーと引数を使用して、<see cref="Microsoft.Scripting.Ast.LightDynamicExpression4"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="arg0">動的操作の 1 番目の引数を指定します。</param>
		/// <param name="arg1">動的操作の 2 番目の引数を指定します。</param>
		/// <param name="arg2">動的操作の 3 番目の引数を指定します。</param>
		/// <param name="arg3">動的操作の 4 番目の引数を指定します。</param>
		protected internal LightDynamicExpression4(CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2, Expression arg3) : base(binder)
		{
			ContractUtils.RequiresNotNull(arg0, "arg0");
			ContractUtils.RequiresNotNull(arg1, "arg1");
			ContractUtils.RequiresNotNull(arg2, "arg2");
			ContractUtils.RequiresNotNull(arg3, "arg3");
			Argument0 = arg0;
			Argument1 = arg1;
			Argument2 = arg2;
			Argument3 = arg3;
		}

		/// <summary>
		/// このノードをより単純な式に変形します。
		/// <see cref="Expression.CanReduce"/> が <c>true</c> を返す場合、これは有効な式を返します。
		/// このメソッドは、それ自体も単純化する必要がある別のノードを返す場合があります。
		/// </summary>
		/// <returns>単純化された式。</returns>
		public override Expression Reduce() { return Expression.Dynamic(Binder, Type, Argument0, Argument1, Argument2, Argument3); }

		/// <summary>このノードが表す動的操作の引数の個数を取得します。</summary>
		protected sealed override int ArgumentCount { get { return 4; } }

		/// <summary>動的操作の 1 番目の引数を取得します。</summary>
		public Expression Argument0 { get; private set; }

		/// <summary>動的操作の 2 番目の引数を取得します。</summary>
		public Expression Argument1 { get; private set; }

		/// <summary>動的操作の 3 番目の引数を取得します。</summary>
		public Expression Argument2 { get; private set; }

		/// <summary>動的操作の 4 番目の引数を取得します。</summary>
		public Expression Argument3 { get; private set; }

		/// <summary>指定されたインデックスに存在する動的操作の引数を取得します。</summary>
		/// <param name="index">動的操作の引数を取得する 0 から始まるインデックスを指定します。</param>
		/// <returns>指定されたインデックスに存在する動的操作の引数。</returns>
		protected sealed override Expression GetArgument(int index)
		{
			switch (index)
			{
				case 0: return Argument0;
				case 1: return Argument1;
				case 2: return Argument2;
				case 3: return Argument3;
				default: throw Assert.Unreachable;
			}
		}
	}

	class LightTypedDynamicExpression4 : LightDynamicExpression4
	{
		readonly Type _returnType;

		internal LightTypedDynamicExpression4(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2, Expression arg3) : base(binder, arg0, arg1, arg2, arg3)
		{
			ContractUtils.RequiresNotNull(returnType, "returnType");
			_returnType = returnType;
		}

		public sealed override Type Type { get { return _returnType; } }
	}

	/// <summary>インタプリタによって認識される任意個の引数をとる結果型が指定された動的操作を表します。</summary>
	public class LightTypedDynamicExpressionN : LightDynamicExpression
	{
		readonly Type _returnType;

		/// <summary>指定されたバインダーと引数を使用して、<see cref="Microsoft.Scripting.Ast.LightTypedDynamicExpressionN"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="returnType">動的操作の結果型を指定します。</param>
		/// <param name="args">動的操作の引数を指定します。</param>
		protected internal LightTypedDynamicExpressionN(CallSiteBinder binder, Type returnType, IList<Expression> args) : base(binder)
		{
			ContractUtils.RequiresNotNull(returnType, "returnType");
			ContractUtils.RequiresNotEmpty(args, "args");
			Arguments = args;
			_returnType = returnType;
		}

		/// <summary>
		/// このノードをより単純な式に変形します。
		/// <see cref="Expression.CanReduce"/> が <c>true</c> を返す場合、これは有効な式を返します。
		/// このメソッドは、それ自体も単純化する必要がある別のノードを返す場合があります。
		/// </summary>
		/// <returns>単純化された式。</returns>
		public override Expression Reduce() { return Expression.Dynamic(Binder, Type, Arguments); }

		/// <summary>このノードが表す動的操作の引数の個数を取得します。</summary>
		protected sealed override int ArgumentCount { get { return Arguments.Count; } }

		/// <summary>この <see cref="System.Linq.Expressions.Expression"/> が表す式の静的な型を取得します。</summary>
		public sealed override Type Type { get { return _returnType; } }

		/// <summary>動的操作の引数を取得します。</summary>
		public IList<Expression> Arguments { get; private set; }

		/// <summary>指定されたインデックスに存在する動的操作の引数を取得します。</summary>
		/// <param name="index">動的操作の引数を取得する 0 から始まるインデックスを指定します。</param>
		/// <returns>指定されたインデックスに存在する動的操作の引数。</returns>
		protected sealed override Expression GetArgument(int index) { return Arguments[index]; }
	}

	#endregion

	public static partial class Utils
	{
		/// <summary>動的操作の実行時バインダーと引数を使用して、<see cref="LightDynamicExpression"/> を作成します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="arg0">動的操作の 1 番目の引数を指定します。</param>
		/// <returns>新しく作成された <see cref="LightDynamicExpression"/>。</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Expression arg0) { return LightDynamic(binder, typeof(object), arg0); }

		/// <summary>動的操作の実行時バインダーと引数および操作の結果型を使用して、<see cref="LightDynamicExpression"/> を作成します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="returnType">操作の結果型を指定します。</param>
		/// <param name="arg0">動的操作の 1 番目の引数を指定します。</param>
		/// <returns>新しく作成された <see cref="LightDynamicExpression"/>。</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Type returnType, Expression arg0) { return returnType == typeof(object) ? new LightDynamicExpression1(binder, arg0) : new LightTypedDynamicExpression1(binder, returnType, arg0); }

		/// <summary>動的操作の実行時バインダーと引数を使用して、<see cref="LightDynamicExpression"/> を作成します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="arg0">動的操作の 1 番目の引数を指定します。</param>
		/// <param name="arg1">動的操作の 2 番目の引数を指定します。</param>
		/// <returns>新しく作成された <see cref="LightDynamicExpression"/>。</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Expression arg0, Expression arg1) { return LightDynamic(binder, typeof(object), arg0, arg1); }

		/// <summary>動的操作の実行時バインダーと引数および操作の結果型を使用して、<see cref="LightDynamicExpression"/> を作成します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="returnType">操作の結果型を指定します。</param>
		/// <param name="arg0">動的操作の 1 番目の引数を指定します。</param>
		/// <param name="arg1">動的操作の 2 番目の引数を指定します。</param>
		/// <returns>新しく作成された <see cref="LightDynamicExpression"/>。</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1) { return returnType == typeof(object) ? new LightDynamicExpression2(binder, arg0, arg1) : new LightTypedDynamicExpression2(binder, returnType, arg0, arg1); }

		/// <summary>動的操作の実行時バインダーと引数を使用して、<see cref="LightDynamicExpression"/> を作成します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="arg0">動的操作の 1 番目の引数を指定します。</param>
		/// <param name="arg1">動的操作の 2 番目の引数を指定します。</param>
		/// <param name="arg2">動的操作の 3 番目の引数を指定します。</param>
		/// <returns>新しく作成された <see cref="LightDynamicExpression"/>。</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2) { return LightDynamic(binder, typeof(object), arg0, arg1, arg2); }

		/// <summary>動的操作の実行時バインダーと引数および操作の結果型を使用して、<see cref="LightDynamicExpression"/> を作成します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="returnType">操作の結果型を指定します。</param>
		/// <param name="arg0">動的操作の 1 番目の引数を指定します。</param>
		/// <param name="arg1">動的操作の 2 番目の引数を指定します。</param>
		/// <param name="arg2">動的操作の 3 番目の引数を指定します。</param>
		/// <returns>新しく作成された <see cref="LightDynamicExpression"/>。</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2) { return returnType == typeof(object) ? new LightDynamicExpression3(binder, arg0, arg1, arg2) : new LightTypedDynamicExpression3(binder, returnType, arg0, arg1, arg2); }

		/// <summary>動的操作の実行時バインダーと引数を使用して、<see cref="LightDynamicExpression"/> を作成します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="arg0">動的操作の 1 番目の引数を指定します。</param>
		/// <param name="arg1">動的操作の 2 番目の引数を指定します。</param>
		/// <param name="arg2">動的操作の 3 番目の引数を指定します。</param>
		/// <param name="arg3">動的操作の 4 番目の引数を指定します。</param>
		/// <returns>新しく作成された <see cref="LightDynamicExpression"/>。</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2, Expression arg3) { return LightDynamic(binder, typeof(object), arg0, arg1, arg2, arg3); }

		/// <summary>動的操作の実行時バインダーと引数および操作の結果型を使用して、<see cref="LightDynamicExpression"/> を作成します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="returnType">操作の結果型を指定します。</param>
		/// <param name="arg0">動的操作の 1 番目の引数を指定します。</param>
		/// <param name="arg1">動的操作の 2 番目の引数を指定します。</param>
		/// <param name="arg2">動的操作の 3 番目の引数を指定します。</param>
		/// <param name="arg3">動的操作の 4 番目の引数を指定します。</param>
		/// <returns>新しく作成された <see cref="LightDynamicExpression"/>。</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2, Expression arg3) { return returnType == typeof(object) ? new LightDynamicExpression4(binder, arg0, arg1, arg2, arg3) : new LightTypedDynamicExpression4(binder, returnType, arg0, arg1, arg2, arg3); }

		/// <summary>動的操作の実行時バインダーと引数を使用して、<see cref="LightDynamicExpression"/> を作成します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="arguments">動的操作の引数を指定します。</param>
		/// <returns>新しく作成された <see cref="LightDynamicExpression"/>。</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, IEnumerable<Expression> arguments) { return LightDynamic(binder, typeof(object), arguments); }

		/// <summary>動的操作の実行時バインダーと引数および操作の結果型を使用して、<see cref="LightDynamicExpression"/> を作成します。</summary>
		/// <param name="binder">動的操作の実行時バインダーを指定します。</param>
		/// <param name="returnType">操作の結果型を指定します。</param>
		/// <param name="arguments">動的操作の引数を指定します。</param>
		/// <returns>新しく作成された <see cref="LightDynamicExpression"/>。</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Type returnType, IEnumerable<Expression> arguments)
		{
			ContractUtils.RequiresNotNull(arguments, "arguments");
			return LightDynamic(binder, returnType, arguments.ToReadOnly());
		}

		static LightDynamicExpression LightDynamic(CallSiteBinder binder, Type returnType, IList<Expression> arguments)
		{
			ContractUtils.RequiresNotNull(arguments, "arguments");
			switch (arguments.Count)
			{
				case 1: return LightDynamic(binder, returnType, arguments[0]);
				case 2: return LightDynamic(binder, returnType, arguments[0], arguments[1]);
				case 3: return LightDynamic(binder, returnType, arguments[0], arguments[1], arguments[2]);
				case 4: return LightDynamic(binder, returnType, arguments[0], arguments[1], arguments[2], arguments[3]);
				default: return new LightTypedDynamicExpressionN(binder, returnType, arguments);
			}
		}
	}
}
