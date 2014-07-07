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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Contracts;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// ジェネリック型引数の数により区別された型のグループを表します。
	/// このグループが単一の型として扱われた場合は、グループに含まれるジェネリックでない型を表すことになります。
	/// </summary>
	public sealed class TypeGroup : TypeTracker
	{
		readonly string _name;

		TypeGroup(Type t1, int arity1, Type t2, int arity2)
		{
			// TODO: types of different arities might be inherited, but we don't support that yet:
			Debug.Assert(t1.DeclaringType == t2.DeclaringType);
			Debug.Assert(arity1 != arity2);
			TypesByArity = new ReadOnlyDictionary<int, Type>(new Dictionary<int, System.Type>() { { arity1, t1 }, { arity2, t2 } });
			_name = ReflectionUtils.GetNormalizedTypeName(t1);
			Debug.Assert(_name == ReflectionUtils.GetNormalizedTypeName(t2));
		}

		TypeGroup(Type t1, TypeGroup existingTypes)
		{
			// TODO: types of different arities might be inherited, but we don't support that yet:
			Debug.Assert(t1.DeclaringType == existingTypes.DeclaringType);
			Debug.Assert(ReflectionUtils.GetNormalizedTypeName(t1) == existingTypes.Name);
			var typesByArity = new Dictionary<int, Type>(existingTypes.TypesByArity);
			typesByArity[GetGenericArity(t1)] = t1;
			TypesByArity = new ReadOnlyDictionary<int, Type>(typesByArity);
			_name = existingTypes.Name;
		}

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		[Confined]
		public override string ToString() { return base.ToString() + ":" + Name + "(" + string.Join(", ", Types.Select(x => x.Name)) + ")"; }

		/// <summary>この <see cref="TypeGroup"/> に含まれているすべての型のメンバの名前を取得します。</summary>
		/// <returns><see cref="TypeGroup"/> 内のすべての型のメンバの名前。</returns>
		public override IEnumerable<string> GetMemberNames()
		{
			HashSet<string> members = new HashSet<string>();
			foreach (var t in Types)
				CollectMembers(members, t);
			return members;
		}

		/// <summary>ジェネリック型引数を指定された個数もつ型を取得します。</summary>
		/// <param name="arity">取得する型のもつジェネリック型引数の個数を指定します。</param>
		/// <returns>ジェネリック型引数を指定された個数持つ型を表す <see cref="TypeTracker"/>。対象の型がこの <see cref="TypeGroup"/> に存在しない場合は <c>null</c> を返します。</returns>
		public TypeTracker GetTypeForArity(int arity)
		{
			Type typeWithMatchingArity;
			if (!TypesByArity.TryGetValue(arity, out typeWithMatchingArity))
				return null;
			return ReflectionCache.GetTypeTracker(typeWithMatchingArity);
		}

		/// <summary>既存の <see cref="TypeTracker"/> に新しい <see cref="TypeTracker"/> をマージします。</summary>
		/// <param name="existingType">マージされる <see cref="TypeTracker"/> を指定します。<c>null</c> にすることもできます。</param>
		/// <param name="newType">マージされたリストに追加される新しい型を表す <see cref="TypeTracker"/> を指定します。</param>
		/// <returns>マージされたリスト。</returns>
		public static TypeTracker Merge(TypeTracker existingType, TypeTracker newType)
		{
			ContractUtils.RequiresNotNull(newType, "newType");
			if (existingType == null)
				return newType;
			var simpleType = existingType as ReflectedTypeTracker;
			if (simpleType != null)
			{
				var existingArity = GetGenericArity(simpleType.Type);
				var newArity = GetGenericArity(newType.Type);
				if (existingArity == newArity)
					return newType;
				return new TypeGroup(simpleType.Type, existingArity, newType.Type, newArity);
			}
			return new TypeGroup(newType.Type, existingType as TypeGroup);
		}

		/// <summary>ジェネリック型引数の個数を取得します。</summary>
		static int GetGenericArity(Type type)
		{
			if (!type.IsGenericType)
				return 0;
			Debug.Assert(type.IsGenericTypeDefinition);
			return type.GetGenericArguments().Length;
		}

		/// <summary>
		/// この <see cref="TypeGroup"/> に含まれている非ジェネリック型を取得します。
		/// <see cref="TypeGroup"/> 内に非ジェネリック型が存在しない場合は例外をスローします。
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")] // TODO: fix
		public Type NonGenericType
		{
			get
			{
				Type nonGenericType;
				if (TryGetNonGenericType(out nonGenericType))
					return nonGenericType;
				throw Error.NonGenericWithGenericGroup(Name);
			}
		}

		/// <summary>この <see cref="TypeGroup"/> に含まれている非ジェネリック型の取得を試みます。</summary>
		/// <param name="nonGenericType">取得された非ジェネリック型が格納される変数を指定します。</param>
		/// <returns><see cref="TypeGroup"/> 内に非ジェネリック型が存在して取得された場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool TryGetNonGenericType(out Type nonGenericType) { return TypesByArity.TryGetValue(0, out nonGenericType); }

		/// <summary>この <see cref="TypeGroup"/> に含まれているすべての型を取得します。</summary>
		public IEnumerable<Type> Types { get { return TypesByArity.Values; } }

		/// <summary>この <see cref="TypeGroup"/> 内から提供されたジェネリック型引数の数に応じた型を返すディクショナリを取得します。</summary>
		public ReadOnlyDictionary<int, Type> TypesByArity { get; private set; }

		#region MemberTracker overrides

		/// <summary><see cref="MemberTracker"/> の種類を取得します。</summary>
		public override sealed TrackerTypes MemberType { get { return TrackerTypes.TypeGroup; } }

		/// <summary>この <see cref="TypeGroup"/> に含まれているすべての型を宣言する型を取得します。</summary>
		public override Type DeclaringType { get { return Types.First().DeclaringType; } }

		/// <summary><see cref="TypeGroup"/> の基本名を取得します。この名前は型引数の数を除いてすべての型で共有されています。</summary>
		public override string Name { get { return _name; } }

		/// <summary>
		/// この <see cref="TypeGroup"/> に含まれている非ジェネリック型を取得します。
		/// <see cref="TypeGroup"/> 内に非ジェネリック型が存在しない場合は例外をスローします。
		/// </summary>
		public override Type Type { get { return NonGenericType; } }

		/// <summary>この <see cref="TypeGroup"/> に非ジェネリック型が含まれているかどうかを示す値を取得します。</summary>
		public override bool IsGenericType { get { return TypesByArity.Keys.Any(x => x > 0); } }

		/// <summary>
		/// この <see cref="TypeGroup"/> に含まれている非ジェネリック型がパブリックとして宣言されているかどうかを示す値を取得します。
		/// <see cref="TypeGroup"/> 内に非ジェネリック型が含まれていない場合は例外をスローします。
		/// </summary>
		public override bool IsPublic { get { return NonGenericType.IsPublic; } }

		#endregion
	}
}
