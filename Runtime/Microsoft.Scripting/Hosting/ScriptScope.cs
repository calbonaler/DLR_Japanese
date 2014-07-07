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
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>コードに対する実行単位を表します。<see cref="Microsoft.Scripting.Runtime.Scope"/> に対する、もう 1 つのホスティング API です。</summary>
	/// <remarks>
	/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> には、すべてのコードが実行されるグローバルの <see cref="Microsoft.Scripting.Runtime.Scope"/> が含まれ、
	/// 任意のイニシャライザやリローダも含めることもできます。
	/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> はスレッドセーフではないため、
	/// ホストは複数スレッドが同じモジュールにアクセスする際にロックするか、スレッドごとにコピーをとるかを選択する必要があります。
	/// </remarks>
	[DebuggerTypeProxy(typeof(ScriptScope.DebugView))]
	public sealed class ScriptScope : MarshalByRefObject, IDynamicMetaObjectProvider
	{
		/// <summary>基になるスコープおよびエンジンを使用して、<see cref="Microsoft.Scripting.Hosting.ScriptScope"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="engine">このスコープに関連付けられるエンジンを指定します。</param>
		/// <param name="scope">このスコープの基になる <see cref="Microsoft.Scripting.Runtime.Scope"/> を指定します。</param>
		internal ScriptScope(ScriptEngine engine, Scope scope)
		{
			Assert.NotNull(engine, scope);
			Scope = scope;
			Engine = engine;
		}

		/// <summary>このスコープの基になる <see cref="Microsoft.Scripting.Runtime.Scope"/> を取得します。</summary>
		internal Scope Scope { get; private set; }

		/// <summary>このスコープに関連付けられている言語に対するエンジンを取得します。スコープが言語に関連付けられていない場合、インバリアントエンジンを返します。</summary>
		public ScriptEngine Engine { get; private set; }

		/// <summary>指定された名前でスコープに格納されている値を取得します。</summary>
		/// <param name="name">取得する値に関連付けられている名前を指定します。</param>
		/// <exception cref="MissingMemberException">指定された名前はスコープでは定義されていません。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> は <c>null</c> 参照です。</exception>
		public dynamic GetVariable(string name) { return Engine.LanguageContext.ScopeGetVariable(Scope, name); }

		/// <summary>
		/// 指定された名前でスコープに格納されている値を取得します。
		/// 結果はスコープに関連付けられている言語が定義する変換を使用して指定された型に変換されます。
		/// スコープにどの言語も関連付けられていない場合、既定の変換が実行されます。
		/// </summary>
		/// <param name="name">取得する値に関連付けられている名前を指定します。</param>
		/// <exception cref="MissingMemberException">指定された名前はスコープでは定義されていません。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> は <c>null</c> 参照です。</exception>
		public T GetVariable<T>(string name) { return Engine.LanguageContext.ScopeGetVariable<T>(Scope, name); }

		/// <summary>指定された名前でスコープに格納されている値の取得を試みます。取得が成功した場合 <c>true</c> を返します。</summary>
		/// <param name="name">取得する値に関連付けられている名前を指定します。</param>
		/// <param name="value">取得した値を格納する変数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> は <c>null</c> 参照です。</exception>
		public bool TryGetVariable(string name, out dynamic value) { return Engine.LanguageContext.ScopeTryGetVariable(Scope, name, out value); }

		/// <summary>
		/// 指定された名前でスコープに格納されている値の取得を試みます。
		/// 結果はスコープに関連付けられている言語が定義する変換を使用して指定された型に変換されます。
		/// スコープにどの言語も関連付けられていない場合、既定の変換が実行されます。
		/// 取得が成功した場合 <c>true</c> を返します。
		/// </summary>
		/// <param name="name">取得する値に関連付けられている名前を指定します。</param>
		/// <param name="value">取得した値を格納する変数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> は <c>null</c> 参照です。</exception>
		public bool TryGetVariable<T>(string name, out T value)
		{
			object result;
			if (Engine.LanguageContext.ScopeTryGetVariable(Scope, name, out result))
			{
				value = Engine.Operations.ConvertTo<T>(result);
				return true;
			}
			value = default(T);
			return false;
		}

		/// <summary>指定された値を指定された名前でこのスコープに格納します。</summary>
		/// <param name="name">値が関連付けられる名前を指定します。</param>
		/// <param name="value">スコープに格納する値を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> は <c>null</c> 参照です。</exception>
		public void SetVariable(string name, object value) { Engine.LanguageContext.ScopeSetVariable(Scope, name, value); }

		/// <summary>指定された名前でスコープに格納されている値に対するハンドルを取得します。</summary>
		/// <param name="name">取得する値に関連付けられている名前を指定します。</param>
		/// <exception cref="MissingMemberException">指定された名前はスコープでは定義されていません。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> は <c>null</c> 参照です。</exception>
		public ObjectHandle GetVariableHandle(string name) { return new ObjectHandle((object)GetVariable(name)); }

		/// <summary>指定された名前でスコープに格納されている値に対するハンドルの取得を試みます。取得が成功した場合 <c>true</c> を返します。</summary>
		/// <param name="name">取得する値に関連付けられている名前を指定します。</param>
		/// <param name="handle">取得した値に対するハンドルを格納する変数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> は <c>null</c> 参照です。</exception>
		public bool TryGetVariableHandle(string name, out ObjectHandle handle)
		{
			object value;
			if (TryGetVariable(name, out value))
			{
				handle = new ObjectHandle(value);
				return true;
			}
			else
			{
				handle = null;
				return false;
			}
		}

		/// <summary>指定された値を指定された名前でこのスコープに格納します。</summary>
		/// <param name="name">値が関連付けられる名前を指定します。</param>
		/// <param name="handle">スコープに格納する値に対するハンドルを指定します。</param>
		/// <exception cref="SerializationException">
		/// ハンドルによって保持されている値はスコープのアプリケーションドメインのものではなく、また、シリアライズ可能でも <see cref="MarshalByRefObject"/> でもありません。
		/// </exception>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> または <paramref name="handle"/> は <c>null</c> 参照です。</exception>
		public void SetVariable(string name, ObjectHandle handle)
		{
			ContractUtils.RequiresNotNull(handle, "handle");
			SetVariable(name, handle.Unwrap());
		}

		/// <summary>このコンテキストまたは外側のスコープに指定された名前が定義されているかどうかを調べます。</summary>
		/// <param name="name">定義されているかどうかを調べる名前を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> は <c>null</c> 参照です。</exception>
		public bool ContainsVariable(string name)
		{
			object dummy;
			return TryGetVariable(name, out dummy);
		}

		/// <summary>このスコープから指定された名前の変数を削除します。</summary>
		/// <param name="name">削除する変数の名前を指定します。</param>
		/// <returns>削除される前に値がこのスコープに存在した場合は <c>true</c> を返します。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> は <c>null</c> 参照です。</exception>
		public bool RemoveVariable(string name)
		{
			if (Engine.Operations.ContainsMember(Scope, name))
			{
				Engine.Operations.RemoveMember(Scope, name);
				return true;
			}
			return false;
		}

		/// <summary>このスコープに格納されている変数を取得します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public IEnumerable<string> GetVariableNames() { return Engine.Operations.GetMemberNames((object)Scope.Storage); } // Remoting: we eagerly enumerate all variables to avoid cross domain calls for each item.

		/// <summary>このスコープに格納されている名前と値の組を取得します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public IEnumerable<KeyValuePair<string, object>> GetItems() { return GetVariableNames().Select(name => new KeyValuePair<string, object>(name, (object)Engine.Operations.GetMember((object)Scope.Storage, name))); } // Remoting: we eagerly enumerate all variables to avoid cross domain calls for each item.

		#region DebugView
		sealed class DebugView
		{
			readonly ScriptScope _scope;

			public DebugView(ScriptScope scope)
			{
				Assert.NotNull(scope);
				_scope = scope;
			}

			public ScriptEngine Language { get { return _scope.Engine; } }

			public System.Collections.Hashtable Variables
			{
				get
				{
					System.Collections.Hashtable result = new System.Collections.Hashtable();
					foreach (var variable in _scope.GetItems())
						result[variable.Key] = variable.Value;
					return result;
				}
			}
		}
		#endregion

		#region IDynamicMetaObjectProvider implementation

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) { return new Meta(parameter, this); }

		sealed class Meta : DynamicMetaObject
		{
			internal Meta(Expression parameter, ScriptScope scope) : base(parameter, BindingRestrictions.Empty, scope) { }

			// TODO: support for IgnoreCase in underlying ScriptScope APIs
			public override DynamicMetaObject BindGetMember(GetMemberBinder action)
			{
				var result = Expression.Variable(typeof(object), "result");
				var fallback = action.FallbackGetMember(this);
				return new DynamicMetaObject(
					Expression.Block(
						new [] { result },
						Expression.Condition(
							Expression.Call(
								Expression.Convert(Expression, typeof(ScriptScope)),
								typeof(ScriptScope).GetMethod("TryGetVariable", new[] { typeof(string), typeof(object).MakeByRefType() }),
								Expression.Constant(action.Name),
								result
							),
							result,
							Expression.Convert(fallback.Expression, typeof(object))
						)
					),
					BindingRestrictions.GetTypeRestriction(Expression, typeof(ScriptScope)).Merge(fallback.Restrictions)
				);
			}

			// TODO: support for IgnoreCase in underlying ScriptScope APIs
			public override DynamicMetaObject BindSetMember(SetMemberBinder action, DynamicMetaObject value)
			{
				var objValue = Expression.Convert(value.Expression, typeof(object));
				return new DynamicMetaObject(
					Expression.Block(
						Expression.Call(
							Expression.Convert(Expression, typeof(ScriptScope)),
							typeof(ScriptScope).GetMethod("SetVariable", new[] { typeof(string), typeof(object) }),
							Expression.Constant(action.Name),
							objValue
						),
						objValue
					),
					Restrictions.Merge(value.Restrictions).Merge(BindingRestrictions.GetTypeRestriction(Expression, typeof(ScriptScope)))
				);
			}

			// TODO: support for IgnoreCase in underlying ScriptScope APIs
			public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder action)
			{
				var fallback = action.FallbackDeleteMember(this);
				return new DynamicMetaObject(
					Expression.IfThenElse(
						Expression.Call(
							Expression.Convert(Expression, typeof(ScriptScope)),
							typeof(ScriptScope).GetMethod("RemoveVariable"),
							Expression.Constant(action.Name)
						),
						Expression.Empty(),
						fallback.Expression
					),
					Restrictions.Merge(BindingRestrictions.GetTypeRestriction(Expression, typeof(ScriptScope))).Merge(fallback.Restrictions)
				);
			}

			// TODO: support for IgnoreCase in underlying ScriptScope APIs
			public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder action, DynamicMetaObject[] args)
			{
				var fallback = action.FallbackInvokeMember(this, args);
				var result = Expression.Variable(typeof(object), "result");
				return new DynamicMetaObject(
					Expression.Block(
						new [] { result },
						Expression.Condition(
							Expression.Call(
								Expression.Convert(Expression, typeof(ScriptScope)),
								typeof(ScriptScope).GetMethod("TryGetVariable", new[] { typeof(string), typeof(object).MakeByRefType() }),
								Expression.Constant(action.Name),
								result
							),
							Expression.Convert(action.FallbackInvoke(new DynamicMetaObject(result, BindingRestrictions.Empty), args, null).Expression, typeof(object)),
							Expression.Convert(fallback.Expression, typeof(object))
						)
					),
					BindingRestrictions.Combine(args).Merge(BindingRestrictions.GetTypeRestriction(Expression, typeof(ScriptScope))).Merge(fallback.Restrictions)
				);
			}

			public override IEnumerable<string> GetDynamicMemberNames() { return ((ScriptScope)Value).GetVariableNames(); }
		}

		#endregion

		// TODO: Figure out what is the right lifetime
		/// <summary>対象のインスタンスの有効期間ポリシーを制御する、有効期間サービス オブジェクトを取得します。</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
