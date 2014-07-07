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
using System.Diagnostics;
using System.Reflection;
using Microsoft.Scripting.Generation;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// ランタイムで一般に使用されるメソッドを提供します。
	/// このクラスには一般に使用されるプリミティブ型のキャッシュされたボックス化表現を共有できるように提供するメソッドが含まれます。
	/// これは <see cref="System.Object"/> を普遍的な型として使用するほとんどの動的言語で有用です。
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
	public static partial class ScriptingRuntimeHelpers
	{
		const int MIN_CACHE = -100;
		const int MAX_CACHE = 1000;
		static readonly object[] cache = MakeCache();

		/// <summary>ボックス化されたブール値 <c>true</c> を表します。</summary>
		public static readonly object True = true;
		/// <summary>ボックス化されたブール値 <c>false</c> を表します。</summary>
		public static readonly object False = false;

		/// <summary><see cref="BooleanToObject"/> メソッドを示します。</summary>
		internal static readonly MethodInfo BooleanToObjectMethod = typeof(ScriptingRuntimeHelpers).GetMethod("BooleanToObject");
		/// <summary><see cref="Int32ToObject"/> メソッドを示します。</summary>
		internal static readonly MethodInfo Int32ToObjectMethod = typeof(ScriptingRuntimeHelpers).GetMethod("Int32ToObject");

		static object[] MakeCache()
		{
			var result = new object[MAX_CACHE - MIN_CACHE];
			for (int i = 0; i < result.Length; i++)
				result[i] = i + MIN_CACHE;
			return result;
		}

		/// <summary>指定された 32 ビット符号付き整数のキャッシュされたボックス化表現を利用できる場合はそれを返します。それ以外の場合は引数をボックス化します。</summary>
		/// <param name="value">ボックス化する 32 ビット符号付き整数を指定します。</param>
		/// <returns>ボックス化された値。</returns>
		public static object Int32ToObject(int value)
		{
			// キャッシュは pystone スコアを整数を多用するアプリケーションの場合、MS .NET 1.1 で 5-10% 向上させます。
			// TODO: これがまだパフォーマンスに貢献しているかを検証すること。.NET 3.5 および 4.0 で有害であるという証拠があります。
			if (value < MAX_CACHE && value >= MIN_CACHE)
				return cache[value - MIN_CACHE];
			return (object)value;
		}

		static readonly string[] chars = MakeSingleCharStrings();

		static string[] MakeSingleCharStrings()
		{
			string[] result = new string[255];
			for (char ch = (char)0; ch < result.Length; ch++)
				result[ch] = new string(ch, 1);
			return result;
		}

		/// <summary>指定されたブール値に対応するボックス化表現を返します。</summary>
		/// <param name="value">ボックス化するブール値を指定します。</param>
		/// <returns>ボックス化された値。</returns>
		public static object BooleanToObject(bool value) { return value ? True : False; }

		/// <summary>指定された文字を 1 文字のみを含む文字列に変換します。キャッシュが使用できる場合はキャッシュを返します。</summary>
		/// <param name="ch">文字列に変換する文字を指定します。</param>
		/// <returns>変換された文字列。</returns>
		public static string CharToString(char ch)
		{
			if (ch < chars.Length)
				return chars[ch];
			return new string(ch, 1);
		}

		/// <summary>指定されたプリミティブ型の既定値を返します。</summary>
		/// <param name="type">既定値を返すプリミティブ型を指定します。</param>
		/// <returns>プリミティブ型の既定値。プリミティブ型以外が指定された場合は <c>null</c> を返します。</returns>
		internal static object GetPrimitiveDefaultValue(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean: return ScriptingRuntimeHelpers.False;
				case TypeCode.SByte: return default(SByte);
				case TypeCode.Byte: return default(Byte);
				case TypeCode.Char: return default(Char);
				case TypeCode.Int16: return default(Int16);
				case TypeCode.Int32: return ScriptingRuntimeHelpers.Int32ToObject(0);
				case TypeCode.Int64: return default(Int64);
				case TypeCode.UInt16: return default(UInt16);
				case TypeCode.UInt32: return default(UInt32);
				case TypeCode.UInt64: return default(UInt64);
				case TypeCode.Single: return default(Single);
				case TypeCode.Double: return default(Double);
				case TypeCode.DBNull: return default(DBNull);
				case TypeCode.DateTime: return default(DateTime);
				case TypeCode.Decimal: return default(Decimal);
				default: return null;
			}
		}

		/// <summary>指定されたメッセージを使用して、新しい <see cref="ArgumentTypeException"/> を作成します。</summary>
		/// <param name="message">例外を説明するメッセージを指定します。</param>
		/// <returns>新しく作成された <see cref="ArgumentTypeException"/>。</returns>
		public static ArgumentTypeException SimpleTypeError(string message) { return new ArgumentTypeException(message); }

		/// <summary>指定された型への変換が失敗したことを示す例外を返します。</summary>
		/// <param name="toType">変換先の型を指定します。</param>
		/// <param name="value">変換を試みた値を指定します。</param>
		/// <returns>変換が失敗したことを示す <see cref="ArgumentTypeException"/>。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")] // TODO: fix
		public static ArgumentTypeException CannotConvertError(Type toType, object value) { return SimpleTypeError(string.Format("Cannot convert {0}({1}) to {2}", CompilerHelpers.GetType(value).Name, value, toType.Name)); }

		/// <summary>指定された属性が見つからないことを示す例外を返します。</summary>
		/// <param name="message">例外を説明するメッセージを指定します。</param>
		/// <returns>属性が見つからないことを示す <see cref="MissingMemberException"/>。</returns>
		public static MissingMemberException SimpleAttributeError(string message) { return new MissingMemberException(message); } //TODO: localize

		/// <summary>指定されたフィールドまたはプロパティが読み取り専用であるのに代入を試みた場合にエラーを発生させます。</summary>
		/// <param name="field">フィールドへの代入であるかどうかを示す値を指定します。</param>
		/// <param name="name">代入を試みたメンバの名前を指定します。</param>
		/// <returns>なし。</returns>
		/// <exception cref="MissingMemberException">フィールドまたはプロパティは読み取り専用です。</exception>
		public static object ReadOnlyAssignError(bool field, string name)
		{
			if (field)
				throw Error.FieldReadonly(name);
			else
				throw Error.PropertyReadonly(name);
		}

		/// <summary>指定された型のインスタンスを作成します。</summary>
		/// <typeparam name="T">インスタンスを作成する型を指定します。</typeparam>
		/// <returns>指定された型のインスタンス。</returns>
		public static T CreateInstance<T>() { return default(T); }

		/// <summary>指定された型の配列を作成します。</summary>
		/// <typeparam name="T">配列の要素の型を指定します。</typeparam>
		/// <param name="args">配列の要素数を指定します。</param>
		/// <returns>新しく作成された指定された型の配列。</returns>
		public static T[] CreateArray<T>(int args) { return new T[args]; }

		/// <summary><see cref="System.Runtime.CompilerServices.StrongBox&lt;T&gt;"/> が予期されたにもかかわらず別の型を受け取ったことを示す例外を返します。</summary>
		/// <param name="type">予期していた <see cref="System.Runtime.CompilerServices.StrongBox&lt;T&gt;"/> の型引数を指定します。</param>
		/// <param name="received">実際に受け取った値を指定します。</param>
		/// <returns><see cref="System.Runtime.CompilerServices.StrongBox&lt;T&gt;"/> が予期されたにもかかわらず別の型を受け取ったことを示す <see cref="ArgumentTypeException"/>。</returns>
		public static Exception MakeIncorrectBoxTypeError(Type type, object received) { return Error.UnexpectedType("StrongBox<" + type.Name + ">", CompilerHelpers.GetType(received).Name); }

		/// <summary>指定された型の <see cref="SymbolId"/> 型の静的フィールドにそのフィールドの名前を表す <see cref="SymbolId"/> を設定します。</summary>
		/// <param name="t"><see cref="SymbolId"/> 型の静的フィールドを名前で初期化する型を指定します。</param>
		public static void InitializeSymbols(Type t)
		{
			foreach (var fi in t.GetFields())
			{
				if (fi.FieldType == typeof(SymbolId))
				{
					Debug.Assert((SymbolId)fi.GetValue(null) == SymbolId.Empty);
					fi.SetValue(null, SymbolTable.StringToId(fi.Name));
				}
			}
		}
	}
}
