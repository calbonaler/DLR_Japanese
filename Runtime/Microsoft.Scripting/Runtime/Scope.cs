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
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// 実行コードに対するホストにより提供される変数群を表します。
	/// スコープ拡張子を用いて言語ごとの情報をコンテキストに関連づけることもできます。
	/// このクラスは複数実行にわたって使用される状態の追跡や、カスタムストレージの提供
	/// (たとえば、オブジェクトをキーとするアクセスなど)、その他の言語固有のセマンティクスに使用することができます。
	/// </summary>
	/// <remarks>
	/// スコープオブジェクトは基になるストレージがスレッドセーフである限りスレッドセーフです。
	/// スクリプトホストはスレッドセーフなモジュールを用いるかどうかを選択できますが、
	/// スレッドセーフでないストレージを使用する場合はコードが単一スレッドであることを制約しなければなりません。
	/// </remarks>
	public sealed class Scope : IDynamicMetaObjectProvider
	{
		ScopeExtension[] _extensions; // resizable
		IDynamicMetaObjectProvider _storage;

		/// <summary>新しい空のスレッドセーフなディクショナリを使用して、<see cref="Microsoft.Scripting.Runtime.Scope"/> クラスの新しいインスタンスを初期化します。</summary>
		public Scope()
		{
			_extensions = ScopeExtension.EmptyArray;
			_storage = new ScopeStorage();
		}

		/// <summary>指定されたディクショナリを使用して <see cref="Microsoft.Scripting.Runtime.Scope"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="dictionary">作成されるスコープの基になるディクショナリを指定します。</param>
		[Obsolete("Scope(IDynamicMetaObjectProvider) オーバーロードを代わりに使用してください。")]
		public Scope(IAttributesCollection dictionary)
		{
			_extensions = ScopeExtension.EmptyArray;
			_storage = new AttributesAdapter(dictionary);
		}

		/// <summary>ストレージとして任意のオブジェクトを使用する <see cref="Microsoft.Scripting.Runtime.Scope"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="storage">ストレージとして使用される任意のオブジェクトを指定します。</param>
		public Scope(IDynamicMetaObjectProvider storage)
		{
			_extensions = ScopeExtension.EmptyArray;
			_storage = storage;
		}

		/// <summary>指定された言語コンテキストを表す <see cref="ContextId"/> に関連付けられたスコープ拡張子を取得します。</summary>
		/// <param name="languageContextId">取得するスコープ拡張子が関連付けられている言語コンテキストを表す <see cref="ContextId"/> を指定します。</param>
		public ScopeExtension GetExtension(ContextId languageContextId) { return languageContextId.Id < _extensions.Length ? _extensions[languageContextId.Id] : null; }

		/// <summary>スコープ拡張子を指定された言語コンテキストを表す <see cref="ContextId"/> に関連付けてこのオブジェクトに設定します。拡張子は 1 回しか設定できません。</summary>
		/// <param name="languageContextId">スコープ拡張子を関連付ける言語コンテキストを表す <see cref="ContextId"/> を指定します。</param>
		/// <param name="extension">設定するスコープ拡張子を指定します。</param>
		/// <returns>以前に同じ言語コンテキストにスコープ拡張子が関連付けられていた場合は以前の値。それ以外の場合は新しく設定された <see cref="ScopeExtension"/>。</returns>
		public ScopeExtension SetExtension(ContextId languageContextId, ScopeExtension extension)
		{
			ContractUtils.RequiresNotNull(extension, "extension");
			lock (_extensions)
			{
				if (languageContextId.Id >= _extensions.Length)
					Array.Resize(ref _extensions, languageContextId.Id + 1);
				return _extensions[languageContextId.Id] ?? (_extensions[languageContextId.Id] = extension);
			}
		}

		/// <summary>このオブジェクトの基になっているストレージを取得します。</summary>
		public dynamic Storage { get { return _storage; } }

		sealed class MetaScope : DynamicMetaObject
		{
			public MetaScope(Expression parameter, Scope scope) : base(parameter, BindingRestrictions.Empty, scope) { }

			public override DynamicMetaObject BindGetMember(GetMemberBinder binder) { return Restrict(binder.Bind(StorageMetaObject, DynamicMetaObject.EmptyMetaObjects)); }

			public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args) { return Restrict(binder.Bind(StorageMetaObject, args)); }

			public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value) { return Restrict(binder.Bind(StorageMetaObject, new[] { value })); }

			public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder) { return Restrict(binder.Bind(StorageMetaObject, DynamicMetaObject.EmptyMetaObjects)); }

			DynamicMetaObject Restrict(DynamicMetaObject result)
			{
				if (Expression.Type == typeof(Scope))
					return result; // ideal binding, we add no new restrictions if we're binding against a strongly typed Scope
				// Un-ideal binding: we add restrictions.
				return new DynamicMetaObject(result.Expression, BindingRestrictions.GetTypeRestriction(Expression, typeof(Scope)).Merge(result.Restrictions));
			}

			DynamicMetaObject StorageMetaObject { get { return DynamicMetaObject.Create(((Scope)Value)._storage, Expression.Property(Expression.Convert(Expression, typeof(Scope)), typeof(Scope).GetProperty("Storage"))); } }

			public override IEnumerable<string> GetDynamicMemberNames() { return StorageMetaObject.GetDynamicMemberNames(); }
		}

		#region IDynamicMetaObjectProvider Members

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) { return new MetaScope(parameter, this); }

		#endregion

		sealed class AttributesAdapter : IDynamicMetaObjectProvider
		{
			static readonly object _getFailed = new object();
			IAttributesCollection _data;

			public AttributesAdapter(IAttributesCollection data) { _data = data; }

			static object TryGetMember(object adapter, SymbolId name)
			{
				object result;
				if (((AttributesAdapter)adapter)._data.TryGetValue(name, out result))
					return result;
				return _getFailed;
			}

			static void TrySetMember(object adapter, SymbolId name, object value) { ((AttributesAdapter)adapter)._data[name] = value; }

			static bool TryDeleteMember(object adapter, SymbolId name) { return ((AttributesAdapter)adapter)._data.Remove(name); }

			DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) { return new Meta(parameter, this); }

			sealed class Meta : DynamicMetaObject
			{
				public Meta(Expression parameter, AttributesAdapter storage) : base(parameter, BindingRestrictions.Empty, storage) { }

				public override DynamicMetaObject BindGetMember(GetMemberBinder binder) { return DynamicTryGetMember(binder.Name, binder.FallbackGetMember(this).Expression, _ => _); }

				public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
				{
					return DynamicTryGetMember(binder.Name,
						binder.FallbackInvokeMember(this, args).Expression,
						_ => binder.FallbackInvoke(new DynamicMetaObject(_, BindingRestrictions.Empty), args, null).Expression
					);
				}

				DynamicMetaObject DynamicTryGetMember(string name, Expression fallback, Func<Expression, Expression> resultOp)
				{
					var tmp = Expression.Parameter(typeof(object));
					return new DynamicMetaObject(
						Expression.Block(
							new[] { tmp },
							Expression.Condition(
								Expression.NotEqual(
									Expression.Assign(
										tmp,
										Expression.Invoke(
											Expression.Constant(new Func<object, SymbolId, object>(AttributesAdapter.TryGetMember)),
											Expression,
											Expression.Constant(SymbolTable.StringToId(name))
										)
									),
									Expression.Constant(_getFailed)
								),
								ExpressionUtils.Convert(resultOp(tmp), typeof(object)),
								ExpressionUtils.Convert(fallback, typeof(object))
							)
						),
						GetRestrictions()
					);
				}

				BindingRestrictions GetRestrictions() { return BindingRestrictions.GetTypeRestriction(Expression, typeof(AttributesAdapter)); }

				public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
				{
					return new DynamicMetaObject(
						Expression.Block(
							Expression.Invoke(
								Expression.Constant(new Action<object, SymbolId, object>(AttributesAdapter.TrySetMember)),
								Expression,
								Expression.Constant(SymbolTable.StringToId(binder.Name)),
								Expression.Convert(
									value.Expression,
									typeof(object)
								)
							),
							value.Expression
						),
						GetRestrictions()
					);
				}

				public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
				{
					return new DynamicMetaObject(
						Expression.Condition(
							Expression.Invoke(
								Expression.Constant(new Func<object, SymbolId, bool>(AttributesAdapter.TryDeleteMember)),
								Expression,
								Expression.Constant(SymbolTable.StringToId(binder.Name))
							),
							Expression.Default(binder.ReturnType),
							binder.FallbackDeleteMember(this).Expression
						),
						GetRestrictions()
					);
				}

				public override IEnumerable<string> GetDynamicMemberNames() { return ((AttributesAdapter)Value)._data.Keys.OfType<string>(); }
			}
		}
	}
}
