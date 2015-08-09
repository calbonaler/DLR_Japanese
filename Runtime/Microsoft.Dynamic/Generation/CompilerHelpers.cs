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
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Interpreter;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Generation
{
	// TODO: keep this?
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
	delegate void ActionRef<T0, T1>(ref T0 arg0, ref T1 arg1);

	/// <summary>コンパイラに必要なヘルパー メソッドを公開します。</summary>
	public static class CompilerHelpers
	{
		/// <summary>public static であるメソッドの属性を表します。</summary>
		public static readonly MethodAttributes PublicStatic = MethodAttributes.Public | MethodAttributes.Static;
		static readonly MethodInfo _CreateInstanceMethod = new Func<int>(ScriptingRuntimeHelpers.CreateInstance<int>).Method.GetGenericMethodDefinition();
		static int _Counter; // ラムダメソッドに一意な名前を生成するため

		/// <summary>指定された型の存在しないことを表す値を取得します。</summary>
		/// <param name="type">存在しないことを表す値の型を指定します。</param>
		/// <returns>指定された型の存在しないことを表す値。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		public static object GetMissingValue(Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			if (type.IsByRef)
				type = type.GetElementType();
			if (type.IsEnum)
				return Activator.CreateInstance(type);
			switch (Type.GetTypeCode(type))
			{
				default:
				case TypeCode.Object:
					// struct
					if (type.IsSealed && type.IsValueType)
						return Activator.CreateInstance(type);
					else if (type == typeof(object))
						return Missing.Value; // object 型の引数は本当の Missing 値を受け付ける
					else if (!type.IsValueType)
						return null;
					else
						throw Error.CantCreateDefaultTypeFor(type);
				case TypeCode.Empty:
				case TypeCode.DBNull:
				case TypeCode.String:
					return null;
				case TypeCode.Boolean: return false;
				case TypeCode.Char: return '\0';
				case TypeCode.SByte: return (sbyte)0;
				case TypeCode.Byte: return (byte)0;
				case TypeCode.Int16: return (short)0;
				case TypeCode.UInt16: return (ushort)0;
				case TypeCode.Int32: return (int)0;
				case TypeCode.UInt32: return (uint)0;
				case TypeCode.Int64: return 0L;
				case TypeCode.UInt64: return 0UL;
				case TypeCode.Single: return 0.0f;
				case TypeCode.Double: return 0.0D;
				case TypeCode.Decimal: return (decimal)0;
				case TypeCode.DateTime: return DateTime.MinValue;
			}
		}

		/// <summary>指定されたメソッドがインスタンス参照の必要なく呼び出すことができるかどうかを判断します。</summary>
		/// <param name="mi">判断するメソッドを指定します。</param>
		/// <returns>メソッドにインスタンスを与えることなく呼び出すことができる場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsStatic(MethodBase mi) { return mi.IsConstructor || mi.IsStatic; }

		/// <summary>指定されたメソッドがオブジェクトを構築するメソッドであるかどうかを判断します。</summary>
		/// <param name="mb">判断するメソッドを指定します。</param>
		/// <returns>メソッドがオブジェクトを構築するメソッドである場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsConstructor(MethodBase mb) { return mb.IsConstructor || mb.IsGenericMethod && ((MethodInfo)mb).GetGenericMethodDefinition() == _CreateInstanceMethod; }

		/// <summary>指定された式ツリー ノード型が比較演算子であるかどうかを判断します。</summary>
		/// <param name="op">判断する式ツリー ノード型を指定します。</param>
		/// <returns>式ツリー ノード型が比較演算子を表す場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsComparisonOperator(ExpressionType op)
		{
			switch (op)
			{
				case ExpressionType.LessThan: return true;
				case ExpressionType.LessThanOrEqual: return true;
				case ExpressionType.GreaterThan: return true;
				case ExpressionType.GreaterThanOrEqual: return true;
				case ExpressionType.Equal: return true;
				case ExpressionType.NotEqual: return true;
			}
			return false;
		}

		/// <summary><c>null</c> を含むすべてのオブジェクトに対する型を返します。</summary>
		/// <param name="obj">型を返すオブジェクトを指定します。<c>null</c> も指定することができます。</param>
		/// <returns>指定されたオブジェクトの型。<c>null</c> の場合は <see cref="DynamicNull"/> の型が返されます。</returns>
		public static Type GetType(object obj) { return obj == null ? typeof(DynamicNull) : obj.GetType(); }

		/// <summary>指定されたリストのそれぞれの要素の型が指定された型と等しいかどうかを判断します。比較はリスト内の指定されたインデックスから開始されます。</summary>
		/// <param name="args">型が比較されるリストを指定します。</param>
		/// <param name="start">型の比較が開始されるリスト内の位置を示す 0 から始まるインデックスを指定します。</param>
		/// <param name="types">要素の型と比較する型のリストを指定します。</param>
		/// <returns>指定されたリスト内のすべての要素の型が指定された型と等しければ <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool TypesEqual(IEnumerable args, int start, IEnumerable<Type> types) { return types.Zip(args.Cast<object>().Skip(start), (x, y) => Tuple.Create(x, y)).All(x => x.Item1 == (x.Item2 != null ? x.Item2.GetType() : null)); }

		/// <summary>指定されたメソッドが最適化可能かどうかを判断します。</summary>
		/// <param name="method">調べるメソッドを指定します。</param>
		/// <returns>メソッドが最適化可能であれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool CanOptimizeMethod(MethodBase method) { return !method.ContainsGenericParameters && !method.IsProtected() && !method.IsPrivate && method.DeclaringType.IsVisible; }

		/// <summary>このメソッドにディスパッチするパブリックである型で宣言されているメソッドを取得します。</summary>
		/// <param name="method">パブリックである型で宣言されているメソッドを取得するメソッドを指定します。</param>
		/// <returns>
		/// 指定されたメソッドにディスパッチするパブリックである型で宣言されているメソッドが見つかった場合はそのメソッド。
		/// それ以外の場合は元のメソッド。
		/// </returns>
		public static MethodInfo TryGetCallableMethod(MethodInfo method) { return TryGetCallableMethod(method.ReflectedType, method); }

		/// <summary>このメソッドにディスパッチするパブリックである型で宣言されているメソッドを取得します。</summary>
		/// <param name="targetType">メソッドを宣言または継承する型を指定します。</param>
		/// <param name="method">パブリックである型で宣言されているメソッドを取得するメソッドを指定します。</param>
		/// <returns>
		/// 指定されたメソッドにディスパッチするパブリックである型で宣言されているメソッドが見つかった場合はそのメソッド。
		/// それ以外の場合は元のメソッド。
		/// </returns>
		public static MethodInfo TryGetCallableMethod(Type targetType, MethodInfo method)
		{
			if (method.DeclaringType == null || method.DeclaringType.IsVisible)
				return method;
			// 最初にオーバーライドしている元の型から取得してみる
			var baseMethod = method.GetBaseDefinition();
			if (baseMethod.DeclaringType.IsVisible || baseMethod.DeclaringType.IsInterface)
				return baseMethod;
			// 多分このメソッドが来ている型のインターフェイスから取得できる...
			return targetType.GetInterfaces().Select(x => targetType.GetInterfaceMap(x))
				.SelectMany(x =>
					x.InterfaceMethods.Zip(x.TargetMethods, (a, b) => new { Interface = a, Target = b })
					.Where(y => y.Target != null && y.Target.MethodHandle == method.MethodHandle)
					.Take(1).Select(y => y.Interface)
				).FirstOrDefault() ?? baseMethod;
		}

		/// <summary>指定された型のメンバから適切な可視であるメンバを取得することで不可視なメンバを除外したメンバの配列を返します。</summary>
		/// <param name="type">メンバを検索した型を指定します。</param>
		/// <param name="foundMembers">見つかったメンバを指定します。</param>
		/// <returns>不可視なメンバが除外されたメンバの配列。</returns>
		public static MemberInfo[] FilterNonVisibleMembers(Type type, MemberInfo[] foundMembers)
		{
			if (!type.IsVisible && foundMembers.Length > 0)
				// 他の方法で取得できないあらゆるメンバを削除する必要がある
				foundMembers = foundMembers.Select(x => TryGetVisibleMember(x)).Where(x => x != null).ToArray();
			return foundMembers;
		}

		/// <summary>指定されたメンバに関連づけられたメソッドから可視であるメソッドを検索することにより、可視であるメンバを取得します。</summary>
		/// <param name="curMember">可視であるメンバを取得するメンバを指定します。</param>
		/// <returns>指定されたメンバに関連する可視であるメンバ。可視であるメンバが見つからなかった場合は <c>null</c> を返します。</returns>
		public static MemberInfo TryGetVisibleMember(MemberInfo curMember)
		{
			MethodInfo mi;
			MemberInfo visible = null;
			switch (curMember.MemberType)
			{
				case MemberTypes.Method:
					mi = TryGetCallableMethod((MethodInfo)curMember);
					if (IsVisible(mi))
						visible = mi;
					break;
				case MemberTypes.Property:
					var pi = (PropertyInfo)curMember;
					mi = TryGetCallableMethod(pi.GetGetMethod() ?? pi.GetSetMethod());
					if (IsVisible(mi))
						visible = mi.DeclaringType.GetProperty(pi.Name);
					break;
				case MemberTypes.Event:
					var ei = (EventInfo)curMember;
					mi = TryGetCallableMethod(ei.GetAddMethod() ?? ei.GetRemoveMethod() ?? ei.GetRaiseMethod());
					if (IsVisible(mi))
						visible = mi.DeclaringType.GetEvent(ei.Name);
					break;
				// この方法ではこれ以外は公開されない
			}
			return visible;
		}

		/// <summary>
		/// 指定された 2 つのメンバが IL で同じ構成を示しているかどうかを判断します。
		/// このメソッドは同じメンバであっても直接比較の結果を <c>false</c> にさせる <see cref="MemberInfo.ReflectedType"/> プロパティを無視します。
		/// </summary>
		/// <param name="self">比較する 1 番目のメンバを指定します。</param>
		/// <param name="other">比較する 2 番目のメンバを指定します。</param>
		/// <returns>指定された 2 つのメンバが IL で同じ構成を示している場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool MemberEquals(this MemberInfo self, MemberInfo other)
		{
			if ((self == null) != (other == null))
				return false; // 1 つが null だが他は違う
			if (self == null)
				return true; // 両方とも null
			if (self.MemberType != other.MemberType)
				return false;
			switch (self.MemberType)
			{
				case MemberTypes.Field:
					return ((FieldInfo)self).FieldHandle.Equals(((FieldInfo)other).FieldHandle);
				case MemberTypes.Method:
					return ((MethodInfo)self).MethodHandle.Equals(((MethodInfo)other).MethodHandle);
				case MemberTypes.Constructor:
					return ((ConstructorInfo)self).MethodHandle.Equals(((ConstructorInfo)other).MethodHandle);
				case MemberTypes.NestedType:
				case MemberTypes.TypeInfo:
					return ((Type)self).TypeHandle.Equals(((Type)other).TypeHandle);
				case MemberTypes.Event:
				case MemberTypes.Property:
				default:
					return ((MemberInfo)self).Module == ((MemberInfo)other).Module && ((MemberInfo)self).MetadataToken == ((MemberInfo)other).MetadataToken;
			}
		}

		/// <summary>このメソッドにディスパッチするパブリックである型で宣言されているメソッドを取得します。</summary>
		/// <param name="method">パブリックである型で宣言されているメソッドを取得するメソッドを指定します。</param>
		/// <param name="privateBinding">パブリックでないメソッドを呼び出すことができるかどうかを示す値を指定します。</param>
		/// <returns>指定されたメソッドにディスパッチするパブリックである型で宣言されているメソッド。</returns>
		/// <exception cref="System.InvalidOperationException"><paramref name="privateBinding"/> が <c>false</c> ですがパブリックであるメソッドが見つかりませんでした。</exception>
		public static MethodInfo GetCallableMethod(MethodInfo method, bool privateBinding)
		{
			var callable = TryGetCallableMethod(method);
			if (privateBinding || IsVisible(callable))
				return callable;
			throw Error.NoCallableMethods(method.DeclaringType, method.Name);
		}

		/// <summary>指定されたメソッドが可視であるかどうかを判断します。</summary>
		/// <param name="info">判断するメソッドを指定します。</param>
		/// <returns>メソッドが可視である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsVisible(MethodBase info) { return info.IsPublic && (info.DeclaringType == null || info.DeclaringType.IsVisible); }

		/// <summary>指定されたフィールドが可視であるかどうかを判断します。</summary>
		/// <param name="info">判断するフィールドを指定します。</param>
		/// <returns>フィールドが可視である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsVisible(FieldInfo info) { return info.IsPublic && (info.DeclaringType == null || info.DeclaringType.IsVisible); }

		/// <summary>指定されたメソッドが protected であるかどうかを判断します。</summary>
		/// <param name="info">判断するメソッドを指定します。</param>
		/// <returns>メソッドが protected である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsProtected(this MethodBase info) { return info.IsFamily || info.IsFamilyOrAssembly; }

		/// <summary>指定されたフィールドが protected であるかどうかを判断します。</summary>
		/// <param name="info">判断するフィールドを指定します。</param>
		/// <returns>フィールドが protected である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsProtected(this FieldInfo info) { return info.IsFamily || info.IsFamilyOrAssembly; }

		/// <summary>指定された型が protected であるかどうかを判断します。</summary>
		/// <param name="type">判断する型を指定します。</param>
		/// <returns>型が protected である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsProtected(this Type type) { return type.IsNestedFamily || type.IsNestedFamORAssem; }

		/// <summary>指定されたオブジェクトの型の継承階層の中で可視である型を取得します。</summary>
		/// <param name="value">可視である型を取得するオブジェクトを指定します。</param>
		/// <returns>オブジェクトの型の継承階層の中で可視である型。</returns>
		public static Type GetVisibleType(object value) { return GetVisibleType(GetType(value)); }

		/// <summary>指定された型の継承階層の中で可視である型を取得します。</summary>
		/// <param name="t">可視である型を取得する型を指定します。</param>
		/// <returns>型の継承階層の中で可視である型。</returns>
		public static Type GetVisibleType(Type t)
		{
			while (!t.IsVisible)
				t = t.BaseType;
			return t;
		}

		/// <summary>指定された型のコンストラクタを取得します。</summary>
		/// <param name="t">コンストラクタを取得する型を指定します。</param>
		/// <param name="privateBinding">プライベートなコンストラクタを呼び出すことができるかどうかを示す値を指定します。</param>
		/// <returns>指定された型に定義されているコンストラクタの配列。</returns>
		public static MethodBase[] GetConstructors(Type t, bool privateBinding) { return GetConstructors(t, privateBinding, false); }

		/// <summary>指定された型のコンストラクタを取得します。</summary>
		/// <param name="t">コンストラクタを取得する型を指定します。</param>
		/// <param name="privateBinding">プライベートなコンストラクタを呼び出すことができるかどうかを示す値を指定します。</param>
		/// <param name="includeProtected">protected なコンストラクタを含めるかどうかを示す値を指定します。</param>
		/// <returns>指定された型に定義されているコンストラクタの配列。</returns>
		public static MethodBase[] GetConstructors(Type t, bool privateBinding, bool includeProtected)
		{
			if (t.IsArray)
				// コンストラクタのように見えますが、JIT 検証は new int[](3) を好みません。
				// 将来より良い新しい配列の作成を返します。
				return new[] { new Func<int, int[]>(ScriptingRuntimeHelpers.CreateArray<int>).Method.GetGenericMethodDefinition().MakeGenericMethod(t.GetElementType()) };
			var bf = BindingFlags.Instance | BindingFlags.Public;
			if (privateBinding || includeProtected)
				bf |= BindingFlags.NonPublic;
			var ci = t.GetConstructors(bf);
			// プライベートバインディングでなくとも protected コンストラクタは残します。
			if (!privateBinding && includeProtected)
				ci = FilterConstructorsToPublicAndProtected(ci);
			if (t.IsValueType && t != typeof(ArgIterator))
				// 構造体は引数のないコンストラクタは定義しないので、ジェネリックメソッドを追加します。
				return ArrayUtils.Insert<MethodBase>(_CreateInstanceMethod.MakeGenericMethod(t), ci);
			return ci;
		}

		/// <summary>指定されたコンストラクタからパブリックまたは protected として定義されているコンストラクタのみを抽出します。</summary>
		/// <param name="ci">元のコンストラクタを指定します。</param>
		/// <returns>抽出されたコンストラクタの配列。</returns>
		public static ConstructorInfo[] FilterConstructorsToPublicAndProtected(IEnumerable<ConstructorInfo> ci) { return ci.Where(x => x.IsPublic || x.IsProtected()).ToArray(); }

		#region Type Conversions

		/// <summary>指定された型の間のユーザー定義の暗黙的な変換メソッドを取得します。</summary>
		/// <param name="fromType">変換元の型を指定します。</param>
		/// <param name="toType">変換先の型を指定します。</param>
		/// <returns>型の間のユーザー定義の暗黙的な変換メソッド。</returns>
		public static MethodInfo GetImplicitConverter(Type fromType, Type toType) { return GetConverter(fromType, toType, "op_Implicit"); }

		/// <summary>指定された型の間のユーザー定義の明示的な変換メソッドを取得します。</summary>
		/// <param name="fromType">変換元の型を指定します。</param>
		/// <param name="toType">変換先の型を指定します。</param>
		/// <returns>型の間のユーザー定義の明示的な変換メソッド。</returns>
		public static MethodInfo GetExplicitConverter(Type fromType, Type toType) { return GetConverter(fromType, toType, "op_Explicit"); }

		static MethodInfo GetConverter(Type fromType, Type toType, string opMethodName)
		{
			return fromType.GetMember(opMethodName, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static)
			.Concat(toType.GetMember(opMethodName, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static))
			.Cast<MethodInfo>().FirstOrDefault(x => (x.DeclaringType == null || x.DeclaringType.IsVisible) && x.ReturnType == toType && x.GetParameters()[0].ParameterType.IsAssignableFrom(fromType));
		}

		/// <summary>指定された値から指定された型への暗黙的な変換を試みます。</summary>
		/// <param name="value">変換元の値を指定します。</param>
		/// <param name="to">値が変換される型を指定します。</param>
		/// <param name="result">暗黙的変換の結果を格納する変数を指定します。</param>
		/// <returns>暗黙的変換が成功した場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool TryImplicitConversion(object value, Type to, out object result)
		{
			if (TryImplicitConvert(value, to, to.GetMember("op_Implicit"), out result))
				return true;
			for (var curType = GetType(value); curType != null; curType = curType.BaseType)
			{
				if (TryImplicitConvert(value, to, curType.GetMember("op_Implicit"), out result))
					return true;
			}
			return false;
		}

		static bool TryImplicitConvert(object value, Type to, IEnumerable<MemberInfo> implicitConv, out object result)
		{
			var method = implicitConv.Cast<MethodInfo>().FirstOrDefault(x => to.IsValueType == x.ReturnType.IsValueType && to.IsAssignableFrom(x.ReturnType));
			if (method != null)
			{
				result = method.IsStatic ? method.Invoke(null, new object[] { value }) : method.Invoke(value, ArrayUtils.EmptyObjects);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>指定されたオブジェクトの型が <see cref="StrongBox&lt;T&gt;"/> であるかどうかを判断します。</summary>
		/// <param name="target">判断するオブジェクトを指定します。</param>
		/// <returns>オブジェクトの型が <see cref="StrongBox&lt;T&gt;"/> である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsStrongBox(object target) { return IsStrongBox(GetType(target)); }

		/// <summary>指定された型が <see cref="StrongBox&lt;T&gt;"/> であるかどうかを判断します。</summary>
		/// <param name="t">判断する型を指定します。</param>
		/// <returns>型が <see cref="StrongBox&lt;T&gt;"/> である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsStrongBox(Type t) { return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(StrongBox<>); }

		/// <summary>変換が失敗した場合の値を表す <see cref="Expression"/> を作成します。</summary>
		/// <param name="type">変換先の型を指定します。</param>
		/// <returns>変換が失敗した場合の値を表す <see cref="Expression"/>。</returns>
		public static Expression GetTryConvertReturnValue(Type type) { return type.IsInterface || type.IsClass || TypeUtils.IsNullableType(type) ? AstUtils.Constant(null, type) : AstUtils.Constant(Activator.CreateInstance(type)); }

		/// <summary>指定された型の間で変換を行うことができる <see cref="TypeConverter"/> が存在するかどうかを調べます。</summary>
		/// <param name="fromType">変換元の型を指定します。</param>
		/// <param name="toType">変換先の <see cref="TypeConverterAttribute"/> が定義されている型を指定します。</param>
		/// <returns>指定された型の間で変換を行うことができる <see cref="TypeConverter"/> が存在すれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool HasTypeConverter(Type fromType, Type toType)
		{
			TypeConverter _;
			return TryGetTypeConverter(fromType, toType, out _);
		}

		/// <summary>指定された型の間で <see cref="TypeConverter"/> を適用して変換を試みます。</summary>
		/// <param name="value">変換される値を指定します。</param>
		/// <param name="toType">変換先の型を指定します。</param>
		/// <param name="result">変換の結果が格納される変数を指定します。</param>
		/// <returns>指定された型の間での変換が成功した場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool TryApplyTypeConverter(object value, Type toType, out object result)
		{
			TypeConverter converter;
			if (value != null && TryGetTypeConverter(value.GetType(), toType, out converter))
			{
				result = converter.ConvertFrom(value);
				return true;
			}
			else
			{
				result = value;
				return false;
			}
		}

		/// <summary>指定された型の間で変換を行うことができる <see cref="TypeConverter"/> の取得を試みます。</summary>
		/// <param name="fromType">変換元の型を指定します。</param>
		/// <param name="toType">変換先の <see cref="TypeConverterAttribute"/> が定義されている型を指定します。</param>
		/// <param name="converter">変換を行うことができる <see cref="TypeConverter"/> が格納される変数を指定します。</param>
		/// <returns>指定された型の間で変換を行うことができる <see cref="TypeConverter"/> が存在する場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static bool TryGetTypeConverter(Type fromType, Type toType, out TypeConverter converter)
		{
			ContractUtils.RequiresNotNull(fromType, "fromType");
			ContractUtils.RequiresNotNull(toType, "toType");
			// 利用可能な型変換を試す...
			foreach (var tca in toType.GetCustomAttributes<TypeConverterAttribute>(true))
			{
				try { converter = Activator.CreateInstance(Type.GetType(tca.ConverterTypeName)) as TypeConverter; }
				catch (Exception) { converter = null; }
				if (converter != null && converter.CanConvertFrom(fromType))
					return true;
			}
			converter = null;
			return false;
		}

		#endregion

		/// <summary>指定されたオブジェクトを呼び出すことができるメソッドを取得します。</summary>
		/// <param name="obj">呼び出すメソッドを取得するオブジェクトを指定します。</param>
		/// <returns>オブジェクトを呼び出すメソッドの配列。</returns>
		public static MethodBase[] GetMethodTargets(object obj)
		{
			var t = GetType(obj);
			if (typeof(Delegate).IsAssignableFrom(t))
				return new MethodBase[] { t.GetMethod("Invoke") };
			else if (typeof(BoundMemberTracker).IsAssignableFrom(t))
			{
				if (((BoundMemberTracker)obj).BoundTo.MemberType == TrackerTypes.Method) { }
			}
			else if (typeof(MethodGroup).IsAssignableFrom(t))
			{
			}
			else if (typeof(MemberGroup).IsAssignableFrom(t))
			{
			}
			else
				return t.GetMember("Call", MemberTypes.Method, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Cast<MethodInfo>().Where(x => x.IsSpecialName).ToArray();
			return null;
		}

		/// <summary>指定された型の引数および戻り値のデリゲートを型引数に持つ <see cref="CallSite&lt;T&gt;"/> を作成します。</summary>
		/// <param name="types">デリゲートの引数および戻り値の型を指定します。</param>
		/// <returns>指定された型の引数および戻り値のデリゲートを型引数に持つ <see cref="CallSite&lt;T&gt;"/>。</returns>
		public static Type MakeCallSiteType(params Type[] types) { return typeof(CallSite<>).MakeGenericType(Expression.GetDelegateType(types)); }

		/// <summary>指定されたラムダ式に対して翻訳されるデリゲートを作成します。</summary>
		/// <param name="lambda">コンパイルするラムダ式を指定します。</param>
		/// <returns>ラムダ式を翻訳できるデリゲート。</returns>
		public static Delegate LightCompile(this LambdaExpression lambda) { return LightCompile(lambda, -1); }

		/// <summary>指定されたラムダ式に対して翻訳されるデリゲートを作成します。</summary>
		/// <param name="lambda">コンパイルするラムダ式を指定します。</param>
		/// <param name="compilationThreshold">インタプリタがコンパイルを開始する繰り返し数を指定します。</param>
		/// <returns>ラムダ式を翻訳できるデリゲート。</returns>
		public static Delegate LightCompile(this LambdaExpression lambda, int compilationThreshold) { return new LightCompiler(compilationThreshold).CompileTop(lambda).CreateDelegate(); }

		/// <summary>指定されたラムダ式に対して翻訳されるデリゲートを作成します。</summary>
		/// <typeparam name="TDelegate">ラムダ式のデリゲート型を指定します。</typeparam>
		/// <param name="lambda">コンパイルするラムダ式を指定します。</param>
		/// <returns>ラムダ式を翻訳できるデリゲート。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static TDelegate LightCompile<TDelegate>(this Expression<TDelegate> lambda) where TDelegate : class { return (TDelegate)(object)LightCompile((LambdaExpression)lambda); }

		/// <summary>指定されたラムダ式に対して翻訳されるデリゲートを作成します。</summary>
		/// <typeparam name="TDelegate">ラムダ式のデリゲート型を指定します。</typeparam>
		/// <param name="lambda">コンパイルするラムダ式を指定します。</param>
		/// <param name="compilationThreshold">インタプリタがコンパイルを開始する繰り返し数を指定します。</param>
		/// <returns>ラムダ式を翻訳できるデリゲート。</returns>
		public static TDelegate LightCompile<TDelegate>(this Expression<TDelegate> lambda, int compilationThreshold) where TDelegate : class { return (TDelegate)(object)LightCompile((LambdaExpression)lambda, compilationThreshold); }

		/// <summary>ラムダ式をメソッド定義にコンパイルします。</summary>
		/// <param name="lambda">コンパイルするラムダ式を指定します。</param>
		/// <param name="method">ラムダ式の IL を保持するために使用される <see cref="MethodBuilder"/> を指定します。</param>
		/// <param name="emitDebugSymbols">PDB シンボルストアにデバッグ情報が出力されるかどうかを示す値を指定します。</param>
		public static void CompileToMethod(this LambdaExpression lambda, MethodBuilder method, bool emitDebugSymbols)
		{
			if (emitDebugSymbols)
			{
				ContractUtils.Requires(method.Module is ModuleBuilder, "method", "MethodBuilder は有効な ModuleBuilder を保持していません。");
				lambda.CompileToMethod(method, DebugInfoGenerator.CreatePdbGenerator());
			}
			else
				lambda.CompileToMethod(method);
		}

		/// <summary>
		/// ラムダ式をコンパイルします。
		/// <paramref name="emitDebugSymbols"/> が <c>true</c> の場合、ラムダ式は <see cref="TypeBuilder"/> 内にコンパイルされます。
		/// それ以外の場合は、このメソッドは単に <see cref="Expression&lt;TDelegate&gt;.Compile()"/> を呼び出すことと等価です。
		/// この回避策は動的メソッドがデバッグ情報を持つことができないという CLR の制限によるものです。
		/// </summary>
		/// <typeparam name="TDelegate">ラムダ式のデリゲート型を指定します。</typeparam>
		/// <param name="lambda">コンパイルするラムダ式を指定します。</param>
		/// <param name="emitDebugSymbols">デバッグ シンボル (PDB) が <see cref="DebugInfoGenerator"/> によって出力されるかどうかを示す値を指定します。</param>
		/// <returns>コンパイルされたデリゲート。</returns>
		public static TDelegate Compile<TDelegate>(this Expression<TDelegate> lambda, bool emitDebugSymbols) where TDelegate : class { return emitDebugSymbols ? CompileToMethod(lambda, DebugInfoGenerator.CreatePdbGenerator(), true) : lambda.Compile(); }

		/// <summary>
		/// ラムダ式を新しい型に出力することでコンパイルします。オプションでデバッグ可能であることをマークできます。
		/// この回避策は動的メソッドがデバッグ情報を持つことができないという CLR の制限によるものです。
		/// </summary>
		/// <typeparam name="TDelegate">ラムダ式のデリゲート型を指定します。</typeparam>
		/// <param name="lambda">コンパイルするラムダ式を指定します。</param>
		/// <param name="debugInfoGenerator">コンパイラによってシーケンス ポイントのマークやローカル変数の注釈に使用される <see cref="DebugInfoGenerator"/> を指定します。</param>
		/// <param name="emitDebugSymbols">デバッグ シンボル (PDB) が <paramref name="debugInfoGenerator"/> によって出力されるかどうかを示す値を指定します。</param>
		/// <returns>コンパイルされたデリゲート。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static TDelegate CompileToMethod<TDelegate>(this Expression<TDelegate> lambda, DebugInfoGenerator debugInfoGenerator, bool emitDebugSymbols) where TDelegate : class { return (TDelegate)(object)CompileToMethod((LambdaExpression)lambda, debugInfoGenerator, emitDebugSymbols); }

		/// <summary>
		/// ラムダ式を新しい型に出力することでコンパイルします。オプションでデバッグ可能であることをマークできます。
		/// この回避策は動的メソッドがデバッグ情報を持つことができないという CLR の制限によるものです。
		/// </summary>
		/// <param name="lambda">コンパイルするラムダ式を指定します。</param>
		/// <param name="debugInfoGenerator">コンパイラによってシーケンス ポイントのマークやローカル変数の注釈に使用される <see cref="DebugInfoGenerator"/> を指定します。</param>
		/// <param name="emitDebugSymbols">デバッグ シンボル (PDB) が <paramref name="debugInfoGenerator"/> によって出力されるかどうかを示す値を指定します。</param>
		/// <returns>コンパイルされたデリゲート。</returns>
		public static Delegate CompileToMethod(this LambdaExpression lambda, DebugInfoGenerator debugInfoGenerator, bool emitDebugSymbols)
		{
			// ラムダ式が名前を持っていない場合、一意なメソッド名を作成する。
			var methodName = string.IsNullOrEmpty(lambda.Name) ? "lambda_method$" + System.Threading.Interlocked.Increment(ref _Counter) : lambda.Name;
			var type = Snippets.DefinePublicType(methodName, typeof(object), false, emitDebugSymbols);
			var rewriter = new BoundConstantsRewriter(type);
			lambda = (LambdaExpression)rewriter.Visit(lambda);
			var method = type.DefineMethod(methodName, PublicStatic);
			lambda.CompileToMethod(method, debugInfoGenerator);
			var finished = type.CreateType();
			rewriter.InitializeFields(finished);
			return Delegate.CreateDelegate(lambda.Type, finished.GetMethod(method.Name));
		}

		// Matches ILGen.TryEmitConstant
		/// <summary>IL に指定された型の定数値を出力できるかどうかを判断します。</summary>
		/// <param name="value">調べる値を指定します。</param>
		/// <param name="type">調べる型を指定します。</param>
		/// <returns>指定された型の定数値を IL に出力できる場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool CanEmitConstant(object value, Type type)
		{
			if (value == null)
				return true;
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Char:
				case TypeCode.Byte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Decimal:
				case TypeCode.String:
					return true;
			}
			var t = value as Type;
			if (t != null && ILGeneratorExtensions.ShouldLdtoken(t))
				return true;
			var mb = value as MethodBase;
			if (mb != null && ILGeneratorExtensions.ShouldLdtoken(mb))
				return true;
			return false;
		}

		/// <summary><see cref="DynamicExpression"/> を site.Target(site, *args) の形に縮退します。</summary>
		/// <param name="node">縮退するノードを指定します。</param>
		/// <returns>縮退されたノード。</returns>
		public static Expression Reduce(DynamicExpression node)
		{
			// Store the callsite as a constant
			var siteConstant = AstUtils.Constant(CallSite.Create(node.DelegateType, node.Binder));
			// ($site = siteExpr).Target.Invoke($site, *args)
			var site = Expression.Variable(siteConstant.Type, "$site");
			return Expression.Block(new[] { site },
				Expression.Call(Expression.Field(Expression.Assign(site, siteConstant), siteConstant.Type.GetField("Target")),
					node.DelegateType.GetMethod("Invoke"),
					ArrayUtils.Insert(site, node.Arguments)
				)
			);
		}

		/// <summary>すべての生存しているオブジェクトを取り除き、型の静的フィールドに配置するリライターを表します。</summary>
		sealed class BoundConstantsRewriter : ExpressionVisitor
		{
			sealed class ReferenceEqualityComparer : EqualityComparer<object>
			{
				internal static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

				ReferenceEqualityComparer() { }

				public override bool Equals(object x, object y) { return ReferenceEquals(x, y); }

				public override int GetHashCode(object obj) { return RuntimeHelpers.GetHashCode(obj); }
			}

			readonly Dictionary<object, FieldBuilder> _fields = new Dictionary<object, FieldBuilder>(ReferenceEqualityComparer.Instance);
			readonly TypeBuilder _type;

			internal BoundConstantsRewriter(TypeBuilder type) { _type = type; }

			internal void InitializeFields(Type type)
			{
				foreach (var pair in _fields)
					type.GetField(pair.Value.Name).SetValue(null, pair.Key);
			}

			protected override Expression VisitConstant(ConstantExpression node)
			{
				if (CanEmitConstant(node.Value, node.Type))
					return node;
				FieldBuilder field;
				if (!_fields.TryGetValue(node.Value, out field))
				{
					field = _type.DefineField("$constant" + _fields.Count, GetVisibleType(node.Value.GetType()), FieldAttributes.Public | FieldAttributes.Static);
					_fields.Add(node.Value, field);
				}
				Expression result = Expression.Field(null, field);
				if (result.Type != node.Type)
					result = Expression.Convert(result, node.Type);
				return result;
			}

			protected override Expression VisitDynamic(DynamicExpression node) { return Visit(Reduce(node)); }
		}
	}
}
