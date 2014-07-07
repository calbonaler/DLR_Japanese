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
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	/// <summary><see cref="OverloadResolverFactory"/> の既定の実装を表します。</summary>
	sealed class DefaultOverloadResolverFactory : OverloadResolverFactory
	{
		DefaultBinder _binder;

		/// <summary>指定された <see cref="DefaultBinder"/> を使用して、<see cref="Microsoft.Scripting.Actions.DefaultOverloadResolverFactory"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binder">作成される <see cref="DefaultOverloadResolver"/> に適用される <see cref="DefaultBinder"/> を指定します。</param>
		public DefaultOverloadResolverFactory(DefaultBinder binder)
		{
			Assert.NotNull(binder);
			_binder = binder;
		}

		/// <summary>指定された引数および呼び出しシグネチャを使用して新しい <see cref="Microsoft.Scripting.Actions.DefaultOverloadResolver"/> を作成します。</summary>
		/// <param name="args">オーバーロード解決の対象となる引数のリストを指定します。</param>
		/// <param name="signature">オーバーロードを呼び出すシグネチャを指定します。</param>
		/// <param name="callType">オーバーロードを呼び出す方法を指定します。</param>
		/// <returns>指定された引数およびシグネチャに対するオーバーロードを解決する <see cref="DefaultOverloadResolver"/>。</returns>
		public override DefaultOverloadResolver CreateOverloadResolver(IList<DynamicMetaObject> args, CallSignature signature, CallTypes callType) { return new DefaultOverloadResolver(_binder, args, signature, callType); }
	}

	/// <summary><see cref="OverloadResolver"/> に対する既定の実装を表します。</summary>
	public class DefaultOverloadResolver : OverloadResolver
	{
		// the first argument is "self" if CallType is ImplicitInstance
		// (TODO: it might be better to change the signature)
		DynamicMetaObject _invalidSplattee;
		static readonly DefaultOverloadResolverFactory _factory = new DefaultOverloadResolverFactory(DefaultBinder.Instance);

		// instance method call:
		/// <summary>インスタンスメソッド呼び出しに対する <see cref="Microsoft.Scripting.Actions.DefaultOverloadResolver"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binder">バインディングを実行する <see cref="ActionBinder"/> を指定します。</param>
		/// <param name="instance">メソッド呼び出しのインスタンスを表す <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="args">メソッド呼び出しの実引数を表す <see cref="DynamicMetaObject"/> のリストを指定します。</param>
		/// <param name="signature">オーバーロードのシグネチャを指定します。</param>
		public DefaultOverloadResolver(ActionBinder binder, DynamicMetaObject instance, IList<DynamicMetaObject> args, CallSignature signature) : this(binder, ArrayUtils.Insert(instance, args), signature, CallTypes.ImplicitInstance) { }

		// method call:
		/// <summary>静的メソッド呼び出しに対する <see cref="Microsoft.Scripting.Actions.DefaultOverloadResolver"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binder">バインディングを実行する <see cref="ActionBinder"/> を指定します。</param>
		/// <param name="args">メソッド呼び出しの実引数を表す <see cref="DynamicMetaObject"/> のリストを指定します。</param>
		/// <param name="signature">オーバーロードのシグネチャを指定します。</param>
		public DefaultOverloadResolver(ActionBinder binder, IList<DynamicMetaObject> args, CallSignature signature) : this(binder, args, signature, CallTypes.None) { }

		/// <summary>一般のメソッド呼び出しに対する <see cref="Microsoft.Scripting.Actions.DefaultOverloadResolver"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binder">バインディングを実行する <see cref="ActionBinder"/> を指定します。</param>
		/// <param name="args">メソッド呼び出しの実引数を表す <see cref="DynamicMetaObject"/> のリストを指定します。</param>
		/// <param name="signature">オーバーロードのシグネチャを指定します。</param>
		/// <param name="callType">オーバーロードを呼び出す方法を指定します。</param>
		public DefaultOverloadResolver(ActionBinder binder, IList<DynamicMetaObject> args, CallSignature signature, CallTypes callType) : base(binder)
		{
			ContractUtils.RequiresNotNullItems(args, "args");
			Debug.Assert((callType == CallTypes.ImplicitInstance ? 1 : 0) + signature.ArgumentCount == args.Count);
			Arguments = args;
			Signature = signature;
			CallType = callType;
		}

		/// <summary><see cref="DefaultOverloadResolver"/> を作成する方法を表す <see cref="OverloadResolverFactory"/> を取得します。</summary>
		public static OverloadResolverFactory Factory { get { return _factory; } }

		/// <summary>この <see cref="DefaultOverloadResolver"/> の対象となるオーバーロードのシグネチャを取得します。</summary>
		public CallSignature Signature { get; private set; }

		/// <summary>この <see cref="DefaultOverloadResolver"/> が呼び出すオーバーロードに渡す実引数のリストを取得します。</summary>
		public IList<DynamicMetaObject> Arguments { get; private set; }

		/// <summary>この <see cref="DefaultOverloadResolver"/> がオーバーロードを呼び出す方法を取得します。</summary>
		public CallTypes CallType { get; private set; }

		/// <summary>引数のバインディングの前に呼び出されます。</summary>
		/// <param name="mapping">マッピングの対象となる <see cref="ParameterMapping"/> オブジェクト。</param>
		/// <returns>
		/// 仮引数がこのメソッドによってマッピングされたかどうかを示すビットマスク。
		/// 既定のビットマスクは残りの仮引数に対して構築されます。(ビットはクリアされています。)
		/// </returns>
		protected internal override BitArray MapSpecialParameters(ParameterMapping mapping)
		{
			//  CallType        call-site   m static                  m instance         m operator/extension
			//  implicit inst.  T.m(a,b)    Ast.Call(null, [a, b])    Ast.Call(a, [b])   Ast.Call(null, [a, b])   
			//  none            a.m(b)      Ast.Call(null, [b])       Ast.Call(a, [b])   Ast.Call(null, [a, b])
			if (!mapping.Overload.IsStatic)
			{
				mapping.AddParameter(new ParameterWrapper(null, mapping.Overload.DeclaringType, null, ParameterBindingFlags.ProhibitNull | (CallType == CallTypes.ImplicitInstance ? ParameterBindingFlags.IsHidden : 0)));
				mapping.AddInstanceBuilder(new InstanceBuilder(mapping.ArgIndex));
			}
			return null;
		}

		/// <summary>仮引数が等価である 2 つの候補を比較します。</summary>
		/// <param name="one">比較する 1 番目の適用可能な候補を指定します。</param>
		/// <param name="two">比較する 2 番目の適用可能な候補を指定します。</param>
		/// <returns>どちらの候補が選択されたか、あるいは完全に等価かを示す <see cref="Candidate"/>。</returns>
		protected override Candidate CompareEquivalentCandidates(ApplicableCandidate one, ApplicableCandidate two)
		{
			var result = base.CompareEquivalentCandidates(one, two);
			if (result.Chosen())
				return result;
			if (one.Method.Overload.IsStatic && !two.Method.Overload.IsStatic)
				return CallType == CallTypes.ImplicitInstance ? Candidate.Two : Candidate.One;
			else if (!one.Method.Overload.IsStatic && two.Method.Overload.IsStatic)
				return CallType == CallTypes.ImplicitInstance ? Candidate.One : Candidate.Two;
			return Candidate.Equivalent;
		}

		#region Actual Arguments

		/// <summary>指定されたインデックスにある実引数の値を取得します。インスタンスメソッド呼び出しに対する暗黙の引数が考慮されます。</summary>
		/// <param name="index">取得する実引数の位置を示す 0 から始まるインデックスを指定します。</param>
		/// <returns>指定されたインデックスにある実引数の値。</returns>
		DynamicMetaObject GetArgument(int index) { return Arguments[(CallType == CallTypes.ImplicitInstance ? 1 : 0) + index]; }

		/// <summary>この <see cref="OverloadResolver"/> に渡される名前付き引数を取得します。</summary>
		/// <param name="namedArgs">名前付き引数の値を格納するリスト。</param>
		/// <param name="argNames">名前付き引数の名前を格納するリスト。</param>
		protected override void GetNamedArguments(out IList<DynamicMetaObject> namedArgs, out IList<string> argNames)
		{
			if (Signature.HasNamedArgument() || Signature.HasDictionaryArgument())
			{
				var objects = new List<DynamicMetaObject>();
				var names = new List<string>();
				for (int i = 0; i < Signature.ArgumentCount; i++)
				{
					if (Signature.GetArgumentKind(i) == ArgumentType.Named)
					{
						objects.Add(GetArgument(i));
						names.Add(Signature.GetArgumentName(i));
					}
				}
				if (Signature.HasDictionaryArgument())
				{
					if (objects == null)
					{
						objects = new List<DynamicMetaObject>();
						names = new List<string>();
					}
					SplatDictionaryArgument(names, objects);
				}
				names.TrimExcess();
				objects.TrimExcess();
				argNames = names;
				namedArgs = objects;
			}
			else
			{
				argNames = ArrayUtils.EmptyStrings;
				namedArgs = DynamicMetaObject.EmptyMetaObjects;
			}
		}

		/// <summary>指定された名前付き引数と展開された引数に関する情報から <see cref="ActualArguments"/> を作成します。</summary>
		/// <param name="namedArgs">名前付き引数の値を格納するリストを指定します。</param>
		/// <param name="argNames">名前付き引数の名前を格納するリストを指定します。</param>
		/// <param name="preSplatLimit">実際の引数内で展開記号に先行して存在しなければならない引数の最小数を指定します。</param>
		/// <param name="postSplatLimit">実際の引数内で展開記号に後続して存在しなければならない引数の最小数を指定します。</param>
		/// <returns>作成された <see cref="ActualArguments"/>。引数が構築されないかオーバーロード解決がエラーを生成した場合は <c>null</c> が返されます。</returns>
		protected override ActualArguments CreateActualArguments(IList<DynamicMetaObject> namedArgs, IList<string> argNames, int preSplatLimit, int postSplatLimit)
		{
			var res = new List<DynamicMetaObject>();
			if (CallType == CallTypes.ImplicitInstance)
				res.Add(Arguments[0]);
			for (int i = 0; i < Signature.ArgumentCount; i++)
			{
				var arg = GetArgument(i);
				switch (Signature.GetArgumentKind(i))
				{
					case ArgumentType.Simple:
					case ArgumentType.Instance:
						res.Add(arg);
						break;
					case ArgumentType.List:
						// TODO: lazy splat
						IList<object> list = arg.Value as IList<object>;
						if (list == null)
						{
							_invalidSplattee = arg;
							return null;
						}
						for (int j = 0; j < list.Count; j++)
							res.Add(DynamicMetaObject.Create(list[j],
								Ast.Call(
									Ast.Convert(
										arg.Expression,
										typeof(IList<object>)
									),
									typeof(IList<object>).GetMethod("get_Item"),
									AstUtils.Constant(j)
								)
							));
						break;
					case ArgumentType.Named:
					case ArgumentType.Dictionary:
						// already processed
						break;
					default:
						throw new NotImplementedException();
				}
			}
			res.TrimExcess();
			return new ActualArguments(res, namedArgs, argNames, CallType == CallTypes.ImplicitInstance ? 1 : 0, 0, -1, -1);
		}

		void SplatDictionaryArgument(IList<string> splattedNames, IList<DynamicMetaObject> splattedArgs)
		{
			Assert.NotNull(splattedNames, splattedArgs);
			DynamicMetaObject dictMo = GetArgument(Signature.ArgumentCount - 1);
			foreach (DictionaryEntry de in (IDictionary)dictMo.Value)
			{
				if (de.Key is string)
				{
					splattedNames.Add((string)de.Key);
					splattedArgs.Add(
						DynamicMetaObject.Create(de.Value,
							Ast.Call(
								AstUtils.Convert(dictMo.Expression, typeof(IDictionary)),
								typeof(IDictionary).GetMethod("get_Item"),
								AstUtils.Constant((string)de.Key)
							)
						)
					);
				}
			}
		}

		/// <summary>遅延展開された引数を示す <see cref="Expression"/> を取得します。</summary>
		/// <returns>遅延展開された引数を示す <see cref="Expression"/>。</returns>
		protected override Expression GetSplattedExpression() { throw Assert.Unreachable; } // lazy splatting not used:

		/// <summary>指定されたインデックスにある遅延展開された引数の値を取得します。</summary>
		/// <param name="index">取得する遅延展開された引数の位置を示す 0 から始まるインデックスを指定します。</param>
		/// <returns>指定されたインデックスにある遅延展開された引数の値。</returns>
		protected override object GetSplattedItem(int index) { throw Assert.Unreachable; } // lazy splatting not used:

		#endregion

		/// <summary>指定された <see cref="BindingTarget"/> から正しくない引数に関するエラーを表す <see cref="ErrorInfo"/> を作成します。</summary>
		/// <param name="target">失敗したバインディングを表す <see cref="BindingTarget"/> を指定します。</param>
		/// <returns>正しくない引数に関するエラーを表す <see cref="ErrorInfo"/>。</returns>
		public override ErrorInfo MakeInvalidParametersError(BindingTarget target)
		{
			if (target.Result == BindingResult.InvalidArguments && _invalidSplattee != null)
				return ErrorInfo.FromException(Ast.Call(new Func<string, string, ArgumentTypeException>(BinderOps.InvalidSplatteeError).Method,
					AstUtils.Constant(target.Name),
					AstUtils.Constant(Binder.GetTypeName(_invalidSplattee.GetLimitType()))
				));
			return base.MakeInvalidParametersError(target);
		}
	}
}
