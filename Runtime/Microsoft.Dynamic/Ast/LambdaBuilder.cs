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
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;
using RuntimeHelpers = Microsoft.Scripting.Runtime.ScriptingRuntimeHelpers;

namespace Microsoft.Scripting.Ast
{
	// TODO: これは削除され、自身のローカルスコープをハンドルする言語に置き換えられる CodeContext に関連する機能を含んでいます。
	/// <summary>
	/// <see cref="LambdaExpression"/> を作成するためのビルダーを表します。
	/// 式ツリーは引数および変数が事前に作成された上で、<see cref="LambdaExpression"/> を作成するファクトリに渡されることを要求するので、
	/// このビルダーはラムダ式の構成に関するあらゆる情報を追跡し、<see cref="LambdaExpression"/> を作成します。
	/// </summary>
	public class LambdaBuilder
	{
		readonly List<KeyValuePair<ParameterExpression, bool>> _visibleVars = new List<KeyValuePair<ParameterExpression, bool>>();
		string _name;
		Type _returnType;
		Expression _body;
		bool _completed;
		static int _lambdaId; // ラムダの一意な名前を生成するため

		internal LambdaBuilder(string name, Type returnType)
		{
			Locals = new List<ParameterExpression>();
			Parameters = new List<ParameterExpression>();
			Visible = true;
			_name = name;
			_returnType = returnType;
		}

		/// <summary>ラムダ式の名前を取得または設定します。現在匿名あるいは無名のラムダ式は許可されていません。</summary>
		public string Name
		{
			get { return _name; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_name = value;
			}
		}

		/// <summary>作成されるラムダ式の戻り値の型を取得または設定します。</summary>
		public Type ReturnType
		{
			get { return _returnType; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_returnType = value;
			}
		}

		/// <summary>ラムダ式のローカル変数を直接操作できるリストを取得します。</summary>
		public List<ParameterExpression> Locals { get; private set; }

		/// <summary>ラムダ式の仮引数を直接操作できるリストを取得します。</summary>
		public List<ParameterExpression> Parameters { get; private set; }

		/// <summary>存在する場合は配列引数を取得します。</summary>
		public ParameterExpression ParamsArray { get; private set; }

		/// <summary>ラムダ式の本体を取得します。これは <c>null</c> でない必要があります。</summary>
		public Expression Body
		{
			get { return _body; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_body = value;
			}
		}

		/// <summary>生成されるラムダ式がローカル変数を直接 CLR スタックに確保する代わりにそれらを格納するディクショナリを持つかどうかを示す値を取得または設定します。</summary>
		public bool Dictionary { get; set; }

		/// <summary>スコープが可視かどうかを示す値を取得または設定します。既定ではスコープは可視です。</summary>
		public bool Visible { get; set; }

		/// <summary>可視である変数のリストを取得します。</summary>
		/// <returns>可視である変数のリスト。</returns>
		public IEnumerable<ParameterExpression> GetVisibleVariables() { return _visibleVars.Where(x => Dictionary || x.Value).Select(x => x.Key); }

		/// <summary>
		/// 指定された名前と型を使用して、ラムダ式の仮引数を作成します。
		/// <see cref="Parameters"/> は作成された順序を保持しますが、直接 <see cref="Parameters"/> にアクセスすることで順序を変更することも可能です。
		/// </summary>
		/// <param name="type">作成される仮引数の型を指定します。</param>
		/// <param name="name">作成される仮引数の名前を指定します。</param>
		/// <returns>作成された仮引数を表す <see cref="ParameterExpression"/>。</returns>
		public ParameterExpression Parameter(Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			var result = Expression.Parameter(type, name);
			Parameters.Add(result);
			_visibleVars.Add(new KeyValuePair<ParameterExpression, bool>(result, false));
			return result;
		}

		/// <summary>
		/// 指定された名前と型を使用して、ラムダ式の仮引数を作成します。
		/// <see cref="Parameters"/> は作成された順序を保持しますが、直接 <see cref="Parameters"/> にアクセスすることで順序を変更することも可能です。
		/// </summary>
		/// <param name="type">作成される仮引数の型を指定します。</param>
		/// <param name="name">作成される仮引数の名前を指定します。</param>
		/// <returns>作成された仮引数を表す <see cref="ParameterExpression"/>。</returns>
		public ParameterExpression ClosedOverParameter(Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			var result = Expression.Parameter(type, name);
			Parameters.Add(result);
			_visibleVars.Add(new KeyValuePair<ParameterExpression, bool>(result, true));
			return result;
		}

		/// <summary>
		/// 指定された名前と型を使用して、ラムダ式の隠れた仮引数を作成します。
		/// <see cref="Parameters"/> は作成された順序を保持しますが、直接 <see cref="Parameters"/> にアクセスすることで順序を変更することも可能です。
		/// </summary>
		/// <param name="type">作成される仮引数の型を指定します。</param>
		/// <param name="name">作成される仮引数の名前を指定します。</param>
		/// <returns>作成された仮引数を表す <see cref="ParameterExpression"/>。</returns>
		public ParameterExpression CreateHiddenParameter(Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			var result = Expression.Parameter(type, name);
			Parameters.Add(result);
			return result;
		}

		/// <summary>
		/// 指定された名前と型を使用して、ラムダ式の配列引数を作成します。
		/// 配列引数はシグネチャに即時に追加されます。
		/// ラムダが作成される前に、(呼び出し元は明示的にリストを操作することで順序を変更できますが) ビルダーはこの引数が最後であるかどうかを確認します。
		/// </summary>
		/// <param name="type">作成される仮引数の型を指定します。</param>
		/// <param name="name">作成される仮引数の名前を指定します。</param>
		/// <returns>作成された仮引数を表す <see cref="ParameterExpression"/>。</returns>
		public ParameterExpression CreateParamsArray(Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.Requires(type.IsArray, "type");
			ContractUtils.Requires(type.GetArrayRank() == 1, "type");
			ContractUtils.Requires(ParamsArray == null, "type", "すでに配列引数が存在します。");
			return ParamsArray = Parameter(type, name);
		}

		/// <summary>指定された名前と型を使用して、ローカル変数を作成します。</summary>
		/// <param name="type">作成されるローカル変数の型を指定します。</param>
		/// <param name="name">作成されるローカル変数の名前を指定します。</param>
		/// <returns>作成されたローカル変数を表す <see cref="ParameterExpression"/>。</returns>
		public ParameterExpression ClosedOverVariable(Type type, string name)
		{
			var result = Expression.Variable(type, name);
			Locals.Add(result);
			_visibleVars.Add(new KeyValuePair<ParameterExpression, bool>(result, true));
			return result;
		}

		/// <summary>指定された名前と型を使用して、ローカル変数を作成します。</summary>
		/// <param name="type">作成されるローカル変数の型を指定します。</param>
		/// <param name="name">作成されるローカル変数の名前を指定します。</param>
		/// <returns>作成されたローカル変数を表す <see cref="ParameterExpression"/>。</returns>
		public ParameterExpression Variable(Type type, string name)
		{
			var result = Expression.Variable(type, name);
			Locals.Add(result);
			_visibleVars.Add(new KeyValuePair<ParameterExpression, bool>(result, false));
			return result;
		}

		/// <summary>指定された名前と型を使用して、一時変数を作成します。</summary>
		/// <param name="type">作成されるローカル変数の型を指定します。</param>
		/// <param name="name">作成されるローカル変数の名前を指定します。</param>
		/// <returns>作成されたローカル変数を表す <see cref="ParameterExpression"/>。</returns>
		public ParameterExpression HiddenVariable(Type type, string name)
		{
			var result = Expression.Variable(type, name);
			Locals.Add(result);
			return result;
		}

		/// <summary>
		/// 一時変数をビルダーによって保持される変数リストに追加します。
		/// これは変数がビルダーの外で作成された場合に便利です。
		/// </summary>
		/// <param name="temp">追加されるローカル変数を表す <see cref="ParameterExpression"/> を指定します。</param>
		public void AddHiddenVariable(ParameterExpression temp)
		{
			ContractUtils.RequiresNotNull(temp, "temp");
			Locals.Add(temp);
		}

		/// <summary>
		/// このビルダーから <see cref="LambdaExpression"/> を作成します。
		/// この操作の後は、このビルダーは他のインスタンスの作成に使用できなくなります。
		/// </summary>
		/// <param name="lambdaType">作成されるラムダ式の型を指定します。</param>
		/// <returns>新しく作成された <see cref="LambdaExpression"/>。</returns>
		public LambdaExpression MakeLambda(Type lambdaType)
		{
			Validate();
			EnsureSignature(lambdaType);
			var lambda = Expression.Lambda(lambdaType, AddDefaultReturn(MakeBody()), _name + "$" + Interlocked.Increment(ref _lambdaId), Parameters);
			// このビルダーは今完了した
			_completed = true;
			return lambda;
		}

		/// <summary>
		/// このビルダーから <see cref="LambdaExpression"/> を作成します。
		/// この操作の後は、このビルダーは他のインスタンスの作成に使用できなくなります。
		/// </summary>
		/// <returns>新しく作成された <see cref="LambdaExpression"/>。</returns>
		public LambdaExpression MakeLambda()
		{
			ContractUtils.Requires(ParamsArray == null, "配列引数のラムダには明示的なデリゲート型が必要です。");
			Validate();
			var lambda = Expression.Lambda(
				GetLambdaType(_returnType, Parameters),
				AddDefaultReturn(MakeBody()),
				_name + "$" + Interlocked.Increment(ref _lambdaId),
				Parameters
			);
			// このビルダーは今完了した
			_completed = true;
			return lambda;
		}

		/// <summary>
		/// このビルダーからジェネレータを含む <see cref="LambdaExpression"/> を作成します。
		/// この操作の後は、このビルダーは他のインスタンスの作成に使用できなくなります。
		/// </summary>
		/// <param name="label">内部のジェネレータから処理を譲るラベルを指定します。</param>
		/// <param name="lambdaType">返されるラムダ式の型を指定します。</param>
		/// <returns>新しく作成された <see cref="LambdaExpression"/>。</returns>
		public LambdaExpression MakeGenerator(LabelTarget label, Type lambdaType)
		{
			Validate();
			EnsureSignature(lambdaType);
			var lambda = Utils.GeneratorLambda(lambdaType, label, MakeBody(), _name + "$" + Interlocked.Increment(ref _lambdaId), Parameters);
			// このビルダーは今完了した
			_completed = true;
			return lambda;
		}

		/// <summary>必要であればラムダ式の本体および指定されたデリゲートのシグネチャに一致する仮引数を構築します。</summary>
		void EnsureSignature(Type delegateType)
		{
			System.Diagnostics.Debug.Assert(Parameters != null, "ここでは仮引数リストが必要です。");
			// paramMapping はキーが引数、値はリダイレクトされるべき式で、どのように引数を割り当てるかを格納するディクショナリです。
			// 現在、引数は (どのような変更も必要ないことを示す) それ自身か、
			// 元の引数がデリゲートシグネチャに対応する直接引数を持たない場合に、ラムダ式に追加される合成変数にリダイレクトされます。
			// 例:
			//     デリゲートのシグネチャ    del(x, params y[])
			//     ラムダ式のシグネチャ      lambda(a, b, param n[])
			// この状況では上記の割り当ては <a, x>, <b, V1>, <n, V2> のようになります。
			// ここで、V1 および V2 は合成変数で、次のように初期化されます。 V1 = y[0], V2 = { y[1], y[2], ... y[n] }
			var delegateParams = delegateType.GetMethod("Invoke").GetParameters();
			var delegateHasParamarray = delegateParams.Any() && delegateParams.Last().IsDefined(typeof(ParamArrayAttribute), false);
			if (ParamsArray != null && !delegateHasParamarray)
				throw new ArgumentException("配列引数のラムダには配列引数のデリゲート型が必要です。");
			var copy = delegateHasParamarray ? delegateParams.Length - 1 : delegateParams.Length;
			var unwrap = Parameters.Count - copy - (ParamsArray != null ? 1 : 0);
			// ラムダ式には配列引数を除いて少なくともデリゲートと同数の仮引数がなくてはなりません。
			if (unwrap < 0)
				throw new ArgumentException("ラムダに十分な仮引数がありません。");
			// リライトが必要なければ短絡する
			if (!delegateHasParamarray && Enumerable.Range(0, copy).All(x => Parameters[x].Type == delegateParams[x].ParameterType))
				return;
			List<ParameterExpression> newParams = new List<ParameterExpression>(delegateParams.Length);
			Dictionary<ParameterExpression, ParameterExpression> paramMapping = new Dictionary<ParameterExpression, ParameterExpression>();
			List<Tuple<ParameterExpression, Expression>> backings = new List<Tuple<ParameterExpression, Expression>>();
			for (int i = 0; i < copy; i++)
			{
				if (Parameters[i].Type != delegateParams[i].ParameterType)
				{
					// 変換された引数に割り当て
					var newParameter = Expression.Parameter(delegateParams[i].ParameterType, delegateParams[i].Name);
					var backingVariable = Expression.Variable(Parameters[i].Type, Parameters[i].Name);
					newParams.Add(newParameter);
					paramMapping.Add(Parameters[i], backingVariable);
					backings.Add(new Tuple<ParameterExpression, Expression>(backingVariable, newParameter));
				}
				else
				{
					// 同じ仮引数を使用
					newParams.Add(Parameters[i]);
					paramMapping.Add(Parameters[i], Parameters[i]);
				}
			}
			if (delegateHasParamarray)
			{
				var delegateParamarray = Expression.Parameter(delegateParams.Last().ParameterType, delegateParams.Last().Name);
				newParams.Add(delegateParamarray);
				// デリゲートの配列引数を変数へラップ解除して、仮引数を変数へ割り当て
				for (int i = 0; i < unwrap; i++)
				{
					var backingVariable = Expression.Variable(Parameters[copy + i].Type, Parameters[copy + i].Name);
					paramMapping.Add(Parameters[copy + i], backingVariable);
					backings.Add(new Tuple<ParameterExpression, Expression>(backingVariable, Expression.ArrayAccess(delegateParamarray, AstUtils.Constant(i))));
				}
				// ラムダ式の配列引数はデリゲートの配列引数からラップ解除した要素をスキップして、残りの要素を取得するべき。
				if (ParamsArray != null)
				{
					var backingVariable = Expression.Variable(ParamsArray.Type, ParamsArray.Name);
					paramMapping.Add(ParamsArray, backingVariable);
					// ヘルパー呼び出し
					backings.Add(new Tuple<ParameterExpression, Expression>(backingVariable, Expression.Call(
						new Func<int[], int, int[]>(RuntimeHelpers.ShiftParamsArray).Method.GetGenericMethodDefinition()
						.MakeGenericMethod(delegateParamarray.Type.GetElementType()),
						delegateParamarray,
						AstUtils.Constant(unwrap)
					)));
				}
			}
			_body = Expression.Block(
				backings.Select(x => Expression.Assign(x.Item1, AstUtils.Convert(x.Item2, x.Item1.Type)))
				.Concat(Enumerable.Repeat(new LambdaParameterRewriter(paramMapping).Visit(_body), 1))
			);
			ParamsArray = null;
			Locals.AddRange(backings.Select(x => x.Item1));
			Parameters = newParams;
			for (int i = 0; i < _visibleVars.Count; i++)
			{
				var p = _visibleVars[i].Key as ParameterExpression;
				ParameterExpression v;
				if (p != null && paramMapping.TryGetValue(p, out v))
					_visibleVars[i] = new KeyValuePair<ParameterExpression, bool>(v, _visibleVars[i].Value);
			}
		}

		/// <summary>ラムダを作成するのに十分な情報をビルダーが保持しているかどうかを検証します。</summary>
		void Validate()
		{
			if (_completed)
				throw new InvalidOperationException("ビルダーはクローズされています。");
			if (_returnType == null)
				throw new InvalidOperationException("戻り値の型が指定されていません。");
			if (_name == null)
				throw new InvalidOperationException("名前が指定されていません。");
			if (_body == null)
				throw new InvalidOperationException("本体が指定されていません。");
			if (ParamsArray != null && (Parameters.Count == 0 || Parameters[Parameters.Count - 1] != ParamsArray))
				throw new InvalidOperationException("配列引数の仮引数が仮引数リストの最後にありません。");
		}

		// 必要であればスコープをラップします。
		Expression MakeBody() { return Locals != null && Locals.Count > 0 ? Expression.Block(Locals, _body) : _body; }

		// 必要であれば既定の戻り値を追加します。
		Expression AddDefaultReturn(Expression body) { return body.Type == typeof(void) && _returnType != typeof(void) ? Expression.Block(body, Utils.Default(_returnType)) : body; }

		static Type GetLambdaType(Type returnType, IEnumerable<ParameterExpression> parameterList)
		{
			parameterList = parameterList ?? Enumerable.Empty<ParameterExpression>();
			ContractUtils.RequiresNotNull(returnType, "returnType");
			ContractUtils.RequiresNotNullItems(parameterList, "parameter");
			return Expression.GetDelegateType(ArrayUtils.Append(parameterList.Select(x => x.Type).ToArray(), returnType));
		}
	}

	public static partial class Utils
	{
		/// <summary>指定された名前と戻り値の型を使用して、<see cref="LambdaBuilder"/> の新しいインスタンスを作成します。</summary>
		/// <param name="returnType">構築されるラムダ式の戻り値の型を指定します。</param>
		/// <param name="name">構築されるラムダ式の名前を指定します。</param>
		/// <returns>新しい <see cref="LambdaBuilder"/> のインスタンス。</returns>
		public static LambdaBuilder Lambda(Type returnType, string name) { return new LambdaBuilder(name, returnType); }
	}
}

namespace Microsoft.Scripting.Runtime
{
	public static partial class ScriptingRuntimeHelpers
	{
		/// <summary>指定された配列引数を指定された数左にシフトした残りを返します。</summary>
		/// <param name="array">シフトする配列を指定します。</param>
		/// <param name="count">シフトする個数を指定します。</param>
		/// <returns><paramref name="count"/> 分左にシフトされた配列。シフト量が範囲を超えている場合は空の配列を返します。</returns>
		public static T[] ShiftParamsArray<T>(T[] array, int count) { return array != null && array.Length > count ? ArrayUtils.ShiftLeft(array, count) : new T[0]; }
	}
}
