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
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Microsoft.Contracts;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>リフレクションメンバのキャッシュを提供します。1 つの要求に対して常に 1 つの値のセットが返されます。</summary>
	public static class ReflectionCache
	{
		static readonly ConcurrentDictionary<MethodBaseCache, MethodGroup> _functions = new ConcurrentDictionary<MethodBaseCache, MethodGroup>();
		static readonly ConcurrentDictionary<Type, TypeTracker> _typeCache = new ConcurrentDictionary<Type, TypeTracker>();

		/// <summary>
		/// 指定された型からメソッドグループを取得します。
		/// 返されるメソッドグループは型/名前の組ではなく、定義されたメソッドに基づいて一意です。
		/// 言い換えると、基本クラスと指定された名前で新しいメソッドを定義しない派生クラスに対する GetMethodGroup 呼び出しは、両方の型に対して同じインスタンスを返します。
		/// </summary>
		/// <param name="type">メソッドグループを取得する型を指定します。</param>
		/// <param name="name">取得するメソッドグループの名前を指定します。</param>
		/// <returns>取得されたメソッドグループ。</returns>
		public static MethodGroup GetMethodGroup(Type type, string name) { return GetMethodGroup(type, name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.InvokeMethod, null); }

		/// <summary>
		/// 指定された型からメソッドグループを取得します。
		/// 返されるメソッドグループは型/名前の組ではなく、定義されたメソッドに基づいて一意です。
		/// 言い換えると、基本クラスと指定された名前で新しいメソッドを定義しない派生クラスに対する GetMethodGroup 呼び出しは、両方の型に対して同じインスタンスを返します。
		/// </summary>
		/// <param name="type">メソッドグループを取得する型を指定します。</param>
		/// <param name="name">取得するメソッドグループの名前を指定します。</param>
		/// <param name="bindingFlags">検索方法を制御する <see cref="BindingFlags"/> を指定します。</param>
		/// <param name="filter">検索結果の配列に適用されるフィルターを指定します。この引数には <c>null</c> を指定できます。</param>
		/// <returns>取得されたメソッドグループ。</returns>
		public static MethodGroup GetMethodGroup(Type type, string name, BindingFlags bindingFlags, MemberFilter filter)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(name, "name");
			var mems = type.FindMembers(MemberTypes.Method, bindingFlags, filter ?? ((x, _) => x.Name == name), null);
			return mems.Length > 0 ? GetMethodGroup(name, Array.ConvertAll(mems, x => (MethodInfo)x)) : null;
		}

		/// <summary>指定されたメソッド配列からメソッドグループを取得します。</summary>
		/// <param name="name">取得するメソッドグループの名前を指定します。</param>
		/// <param name="methods">メソッドグループを作成するメソッドの配列を指定します。</param>
		/// <returns>取得されたメソッドグループ。</returns>
		public static MethodGroup GetMethodGroup(string name, MethodBase[] methods) { return _functions.GetOrAdd(new MethodBaseCache(name, methods), _ => new MethodGroup(Array.ConvertAll(methods, x => (MethodTracker)MemberTracker.FromMemberInfo(x)))); }

		/// <summary>指定されたメンバグループからメソッドグループを取得します。メンバグループにはメソッドのみが含まれている必要があります。</summary>
		/// <param name="name">取得するメソッドグループの名前を指定します。</param>
		/// <param name="mems">メソッドグループを作成するメンバグループを指定します。</param>
		/// <returns>取得されたメソッドグループ。</returns>
		public static MethodGroup GetMethodGroup(string name, MemberGroup mems)
		{
			if (mems.Count <= 0)
				return null;
			var bases = new MethodBase[mems.Count];
			var trackers = new MethodTracker[mems.Count];
			for (int i = 0; i < bases.Length; i++)
				bases[i] = (trackers[i] = (MethodTracker)mems[i]).Method;
			return _functions.GetOrAdd(new MethodBaseCache(name, bases), _ => new MethodGroup(trackers));
		}

		/// <summary>指定された型に対する <see cref="TypeTracker"/> を返します。</summary>
		/// <param name="type"><see cref="TypeTracker"/> を取得する型を指定します。</param>
		/// <returns>型に対する <see cref="TypeTracker"/>。</returns>
		public static TypeTracker GetTypeTracker(Type type) { return _typeCache.GetOrAdd(type, x => new ReflectedTypeTracker(x)); }

		// TODO: Make me private again
		/// <summary>メソッドの配列および名前を格納して、メソッドグループの等価性を定義します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")] // TODO: fix
		public class MethodBaseCache
		{
			readonly MethodBase[] _members;
			readonly string _name;

			/// <summary>指定された名前とメソッドの配列を使用して、<see cref="Microsoft.Scripting.Runtime.ReflectionCache.MethodBaseCache"/> クラスの新しいインスタンスを初期化します。</summary>
			/// <param name="name">メソッドグループの名前を指定します。</param>
			/// <param name="members">メソッドグループに含まれるメソッドを表す配列を指定します。</param>
			public MethodBaseCache(string name, MethodBase[] members)
			{
				// sort by module ID / token so that the Equals / GetHashCode doesn't have to line up members if reflection returns them in different orders.
				Array.Sort(members, (x, y) => x.Module == y.Module ? x.MetadataToken.CompareTo(y.MetadataToken) : x.Module.ModuleVersionId.CompareTo(y.Module.ModuleVersionId));
				_name = name;
				_members = members;
			}

			/// <summary>このオブジェクトと指定されたオブジェクトが等しいかどうかを判断します。</summary>
			/// <param name="obj">等しいかどうかを調べるオブジェクトを指定します。</param>
			/// <returns>このオブジェクトと指定されたオブジェクトが等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
			[Confined]
			public override bool Equals(object obj)
			{
				var other = obj as MethodBaseCache;
				if (other == null || _members.Length != other._members.Length || other._name != _name)
					return false;
				return _members.Zip(other._members,
					(x, y) => x.DeclaringType == y.DeclaringType && x.MetadataToken == y.MetadataToken && x.IsGenericMethod == y.IsGenericMethod &&
						(!x.IsGenericMethod || x.GetGenericArguments().SequenceEqual(y.GetGenericArguments()))
				).All(x => x);
			}

			/// <summary>このオブジェクトのハッシュ値を計算します。</summary>
			/// <returns>オブジェクトのハッシュ値。</returns>
			[Confined]
			public override int GetHashCode() { return _members.Aggregate(6551, (x, y) => x ^ (x << 5 ^ y.DeclaringType.GetHashCode() ^ y.MetadataToken)) ^ _name.GetHashCode(); }
		}
	}
}
