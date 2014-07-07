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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Scripting.Ast;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Generation
{
	/// <summary>コードをディスクに保存できるように、定数および動的サイトをシリアライズします。</summary>
	sealed class ToDiskRewriter : ExpressionVisitor
	{
		static int _uniqueNameId;
		List<Expression> _constants;
		Dictionary<object, Expression> _constantCache;
		ParameterExpression _constantPool;
		Dictionary<Type, Type> _delegateTypes;
		int _depth;
		readonly TypeGen _typeGen;
		TypeGen _symbolGen;
		static int _id;
		readonly Dictionary<SymbolId, FieldBuilderExpression> _indirectSymbolIds = new Dictionary<SymbolId, FieldBuilderExpression>();

		internal ToDiskRewriter(TypeGen typeGen) { _typeGen = typeGen; }

		/// <summary>指定されたラムダ式をリライトします。</summary>
		/// <param name="lambda">リライトするラムダ式を指定します。</param>
		/// <returns>リライトされたラムダ式。</returns>
		public LambdaExpression RewriteLambda(LambdaExpression lambda)
		{
			var res = (LambdaExpression)Visit(lambda);
			if (_symbolGen != null)
				_symbolGen.FinishType();
			return res;
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			_depth++;
			try
			{
				// 最初にラムダを訪問し、ツリーを探索してリライトする必要のあるあらゆる定数を検索
				node = (Expression<T>)base.VisitLambda(node);
				if (_depth != 1)
					return node;
				var body = node.Body;
				if (_constants != null)
				{
					// CodeContextExpression を含んでいる可能性のある定数をリライト
					for (int i = 0; i < _constants.Count; i++)
						_constants[i] = Visit(_constants[i]);
					// 定数プール変数を最上位のラムダに追加
					body = AstUtils.AddScopedVariable(body, _constantPool, Expression.NewArrayInit(typeof(object), _constants));
				}
				// ラムダをリライト
				return Expression.Lambda<T>(body, node.Name + "$" + Interlocked.Increment(ref _uniqueNameId), node.TailCall, node.Parameters);
			}
			finally { _depth--; }
		}

		protected override Expression VisitExtension(Expression node)
		{
			if (node.NodeType == ExpressionType.Dynamic)
				return VisitDynamic((DynamicExpression)node); // ノードは動的であったので、動的ノードは取り除かれ、現在は呼び出しサイトをリライトする必要が生じた
			var symbol = node as SymbolConstantExpression;
			if (symbol != null)
			{
				if (symbol.Value == SymbolId.Empty)
					return Expression.Field(null, typeof(SymbolId).GetField("Empty"));
				FieldBuilderExpression value;
				if (!_indirectSymbolIds.TryGetValue(symbol.Value, out value))
				{
					// フィールドを作成し、修正を出力する
					if (_symbolGen == null)
						_symbolGen = new TypeGen(_typeGen.AssemblyGen, ((ModuleBuilder)_typeGen.TypeBuilder.Module).DefineType("Symbols" + Interlocked.Increment(ref _id)));
					if (_indirectSymbolIds.Count == 0)
					{
						_symbolGen.TypeInitializer.EmitType(_symbolGen.TypeBuilder);
						_symbolGen.TypeInitializer.EmitCall(new Action<Type>(ScriptingRuntimeHelpers.InitializeSymbols).Method);
					}
					_indirectSymbolIds[symbol.Value] = value = new FieldBuilderExpression(_symbolGen.AddStaticField(typeof(SymbolId), FieldAttributes.Public, SymbolTable.IdToString(symbol.Value)));
				}
				Debug.Assert(value != null);
				return value;
			}
			return Visit(node.Reduce());
		}

		protected override Expression VisitConstant(ConstantExpression node)
		{
			var site = node.Value as CallSite;
			if (site != null)
				return RewriteCallSite(site);
			var exprSerializable = node.Value as IExpressionSerializable;
			if (exprSerializable != null)
			{
				EnsureConstantPool();
				Expression res;
				if (!_constantCache.TryGetValue(node.Value, out res))
				{
					var serialized = exprSerializable.CreateExpression();
					_constants.Add(serialized);
					_constantCache[node.Value] = res = AstUtils.Convert(Expression.ArrayAccess(_constantPool, AstUtils.Constant(_constants.Count - 1)), serialized.Type);
				}
				return res;
			}
			var symbols = node.Value as SymbolId[];
			if (symbols != null)
				return Expression.NewArrayInit(typeof(SymbolId), symbols.Select(s => VisitExtension(new SymbolConstantExpression(s))).ToArray());
			var strings = node.Value as string[];
			if (strings != null)
			{
				if (strings.Length == 0)
					return Expression.Field(null, typeof(ArrayUtils).GetField("EmptyStrings"));
				_constants.Add(Expression.NewArrayInit(typeof(string), strings.Select(s => Expression.Constant(s, typeof(string))).ToArray()));
				return AstUtils.Convert(Expression.ArrayAccess(_constantPool, AstUtils.Constant(_constants.Count - 1)), typeof(string[]));
			}
			return base.VisitConstant(node);
		}

		// DynamicExpression がそのデリゲートに一時的な (メモリ内の) 型を使用する場合、ディスクに保存することができる新しいデリゲート型に置換する必要がある
		protected override Expression VisitDynamic(DynamicExpression node)
		{
			Type delegateType;
			if (RewriteDelegate(node.DelegateType, out delegateType))
				node = Expression.MakeDynamic(delegateType, node.Binder, node.Arguments);
			return Visit(CompilerHelpers.Reduce(node)); // ラムダを動的でないメソッドとして出力できるようにするために、動的式は縮退する
		}

		bool RewriteDelegate(Type delegateType, out Type newDelegateType)
		{
			if (!ShouldRewriteDelegate(delegateType))
			{
				newDelegateType = null;
				return false;
			}
			if (_delegateTypes == null)
				_delegateTypes = new Dictionary<Type, Type>();
			// TODO: キャッシュを AssemblyGen に移動するべき?
			if (!_delegateTypes.TryGetValue(delegateType, out newDelegateType))
			{
				MethodInfo invoke = delegateType.GetMethod("Invoke");
				newDelegateType = _typeGen.AssemblyGen.MakeDelegateType(delegateType.Name, invoke.GetParameters().Select(p => p.ParameterType).ToArray(), invoke.ReturnType);
				_delegateTypes[delegateType] = newDelegateType;
			}
			return true;
		}

		bool ShouldRewriteDelegate(Type delegateType)
		{
			// 一時的なデリゲート型をディスクに保存するアセンブリに保持されたものに置換する必要がある。
			//
			// 厄介な問題:
			// SaveAssemblies モードはモジュールが一時的であることを検出できないようにする。
			// オプションがオンの場合、もう 1 つの AssemblyBuilder に存在しているデリゲートも常に置換する
			var module = delegateType.Module as ModuleBuilder;
			return module != null && (module.IsTransient() || Snippets.SaveSnippets && module.Assembly != _typeGen.AssemblyGen.AssemblyBuilder);
		}

		Expression RewriteCallSite(CallSite site)
		{
			var serializer = site.Binder as IExpressionSerializable;
			if (serializer == null)
				throw Error.GenNonSerializableBinder();
			EnsureConstantPool();
			var siteType = site.GetType();
			_constants.Add(Expression.Call(siteType.GetMethod("Create"), serializer.CreateExpression()));
			// ノードをリライト
			return Visit(AstUtils.Convert(Expression.ArrayAccess(_constantPool, AstUtils.Constant(_constants.Count - 1)), siteType));
		}

		void EnsureConstantPool()
		{
			// あとで生成する初期化コードを最も外側のラムダに追加して、作成している配列にインデックスを返す。
			if (_constantPool == null)
			{
				_constantPool = Expression.Variable(typeof(object[]), "$constantPool");
				_constants = new List<Expression>();
				_constantCache = new Dictionary<object, Expression>();
			}
		}
	}
}
