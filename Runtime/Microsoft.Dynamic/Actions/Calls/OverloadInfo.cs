/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * ironruby@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
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
using System.Reflection;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>
	/// オーバーロード解決のためのメソッドオーバーロードの要約を定義します。
	/// このクラスは <see cref="OverloadResolver"/> に解決の実行に必要となるメタデータを提供します。
	/// </summary>
	/// <remarks>警告: このクラスは一時的な API であり、将来のバージョンで破壊的変更を受ける可能性があります。</remarks>
	[DebuggerDisplay("{(object)ReflectionInfo ?? Name}")]
	public abstract class OverloadInfo
	{
		/// <summary>派生クラスでオーバーライドされた場合は、このメソッドオーバーロードの名前を取得します。</summary>
		public abstract string Name { get; }

		/// <summary>派生クラスでオーバーライドされた場合は、このメソッドオーバーロードの仮引数のリストを取得します。</summary>
		public abstract IList<ParameterInfo> Parameters { get; }

		/// <summary>このメソッドオーバーロードの仮引数の数を取得します。</summary>
		public virtual int ParameterCount { get { return Parameters.Count; } }

		/// <summary>派生クラスでオーバーライドされた場合は、戻り値に対する <see cref="System.Reflection.ParameterInfo"/> を取得します。コンストラクタの場合は <c>null</c> となります。</summary>
		public abstract ParameterInfo ReturnParameter { get; }

		/// <summary>このメソッドオーバーロードの指定されたインデックスにある仮引数が <c>null</c> を許容しないかどうかを示す値を返します。</summary>
		/// <param name="parameterIndex"><c>null</c> 非許容かどうかを判断する仮引数のインデックスを指定します。</param>
		/// <returns>指定された仮引数が <c>null</c> 非許容であれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public virtual bool ProhibitsNull(int parameterIndex) { return Parameters[parameterIndex].ProhibitsNull(); }

		/// <summary>このメソッドオーバーロードの指定されたインデックスにある仮引数が <c>null</c> である要素を許容しないかどうかを示す値を返します。</summary>
		/// <param name="parameterIndex"><c>null</c> 要素非許容かどうかを判断する仮引数のインデックスを指定します。</param>
		/// <returns>指定された仮引数が <c>null</c> 要素を許容しなければ <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public virtual bool ProhibitsNullItems(int parameterIndex) { return Parameters[parameterIndex].ProhibitsNullItems(); }

		/// <summary>このメソッドオーバーロードの指定されたインデックスにある仮引数が配列引数であるかどうかを示す値を返します。</summary>
		/// <param name="parameterIndex">配列引数かどうかを判断する仮引数のインデックスを指定します。</param>
		/// <returns>指定された仮引数が配列引数であれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public virtual bool IsParamArray(int parameterIndex) { return Parameters[parameterIndex].IsParamArray(); }

		/// <summary>このメソッドオーバーロードの指定されたインデックスにある仮引数が辞書引数であるかどうかを示す値を返します。</summary>
		/// <param name="parameterIndex">辞書引数かどうかを判断する仮引数のインデックスを指定します。</param>
		/// <returns>指定された仮引数が辞書引数であれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public virtual bool IsParamDictionary(int parameterIndex) { return Parameters[parameterIndex].IsParamDictionary(); }

		/// <summary>派生クラスでオーバーライドされた場合は、このメソッドオーバーロードが宣言されている型を取得します。</summary>
		public abstract Type DeclaringType { get; }

		/// <summary>派生クラスでオーバーライドされた場合は、このメソッドオーバーロードの戻り値の型を取得します。コンストラクタの場合は <c>null</c> になります。</summary>
		public abstract Type ReturnType { get; }

		/// <summary>派生クラスでオーバーライドされた場合は、このメソッドオーバーロードの属性を示すフラグを取得します。</summary>
		public abstract MethodAttributes Attributes { get; }

		/// <summary>派生クラスでオーバーライドされた場合は、このメソッドオーバーロードがコンストラクタであるかどうかを示す値を取得します。</summary>
		public abstract bool IsConstructor { get; }

		/// <summary>派生クラスでオーバーライドされた場合は、このメソッドオーバーロードが拡張メソッドであるかどうかを示す値を取得します。</summary>
		public abstract bool IsExtension { get; }

		/// <summary>
		/// 派生クラスでオーバーライドされた場合は、このメソッドオーバーロードの引数の数が可変であるかどうかを示す値を取得します。
		/// 配列引数や辞書引数の場合は、引数の数は変化します。
		/// </summary>
		public abstract bool IsVariadic { get; }

		/// <summary>派生クラスでオーバーライドされた場合は、このメソッドオーバーロードがジェネリックメソッドの定義かどうかを示す値を取得します。</summary>
		public abstract bool IsGenericMethodDefinition { get; }

		/// <summary>派生クラスでオーバーライドされた場合は、このメソッドオーバーロードがジェネリックメソッドかどうかを示す値を取得します。</summary>
		public abstract bool IsGenericMethod { get; }

		/// <summary>派生クラスでオーバーライドされた場合は、このメソッドオーバーロードが割り当てられていないジェネリック型引数を含んでいるかどうかを示す値を取得します。</summary>
		public abstract bool ContainsGenericParameters { get; }

		/// <summary>派生クラスでオーバーライドされた場合は、このメソッドオーバーロードのジェネリック型引数を取得します。</summary>
		public abstract IList<Type> GenericArguments { get; }

		/// <summary>派生クラスでオーバーライドされた場合は、このメソッドオーバーロードのジェネリック型引数に指定された型を割り当てて、ジェネリックメソッドを作成します。</summary>
		/// <param name="genericArguments">ジェネリックメソッドの型引数に割り当てる型を指定します。</param>
		/// <returns>指定された型が割り当てられたジェネリックメソッドを示す <see cref="OverloadInfo"/>。</returns>
		public abstract OverloadInfo MakeGenericMethod(Type[] genericArguments);

		/// <summary>このメソッドオーバーロードに対して有効な呼び出し規約を取得します。</summary>
		public virtual CallingConventions CallingConvention { get { return CallingConventions.Standard; } }

		/// <summary>このメソッドオーバーロードに対する <see cref="MethodBase"/> を取得します。</summary>
		public virtual MethodBase ReflectionInfo { get { return null; } }

		// TODO: remove
		/// <summary>このメソッドオーバーロードがインスタンスを作成できるかどうかを示す値を取得します。</summary>
		public virtual bool IsInstanceFactory { get { return IsConstructor; } }

		/// <summary>このメソッドオーバーロードが定義されている型からしかアクセスできないかどうかを示す値を取得します。</summary>
		public bool IsPrivate { get { return ((Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private); } }

		/// <summary>このメソッドオーバーロードがすべてのオブジェクトからアクセス可能であるかどうかを示す値を取得します。</summary>
		public bool IsPublic { get { return ((Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public); } }

		/// <summary>このメソッドオーバーロードが定義されているアセンブリ内からしかアクセスできないかどうかを示す値を取得します。</summary>
		public bool IsAssembly { get { return ((Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly); } }

		/// <summary>このメソッドオーバーロードが定義されているクラスとすべての派生クラスからアクセス可能であるかどうかを示す値を取得します。</summary>
		public bool IsFamily { get { return ((Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family); } }

		/// <summary>このメソッドオーバーロードが定義されているアセンブリ内と任意の派生クラスからアクセス可能であるかどうかを示す値を取得します。</summary>
		public bool IsFamilyOrAssembly { get { return ((Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem); } }

		/// <summary>このメソッドオーバーロードが定義されているクラスとアセンブリ内にある派生クラスからしかアクセスできないかどうかを示す値を取得します。</summary>
		public bool IsFamilyAndAssembly { get { return ((Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem); } }

		/// <summary>このメソッドオーバーロードが派生クラスからアクセス可能であるかどうかを示す値を取得します。</summary>
		public bool IsProtected { get { return IsFamily || IsFamilyOrAssembly; } }

		/// <summary>このメソッドオーバーロードを静的に呼び出すことが可能であるかどうかを示す値を取得します。</summary>
		public bool IsStatic { get { return IsConstructor || (Attributes & MethodAttributes.Static) != 0; } }

		/// <summary>このメソッドオーバーロードが仮想メソッドであるかどうかを示す値を取得します。</summary>
		public bool IsVirtual { get { return (Attributes & MethodAttributes.Virtual) != 0; } }

		/// <summary>このメソッドオーバーロードが特別であるかどうかを示す値を取得します。</summary>
		public bool IsSpecialName { get { return (Attributes & MethodAttributes.SpecialName) != 0; } }

		/// <summary>このメソッドオーバーロードをオーバーライドできないかどうかを示す値を取得します。</summary>
		public bool IsFinal { get { return (Attributes & MethodAttributes.Final) != 0; } }
	}

	/// <summary><see cref="System.Reflection.MethodBase"/> に関連付けられたメソッドオーバーロードを表します。</summary>
	/// <remarks>
	/// このクラスはスレッドセーフではありません。
	/// 警告: このクラスは一時的な API であり、将来のバージョンで破壊的変更を受ける可能性があります。
	/// </remarks>
	public class ReflectionOverloadInfo : OverloadInfo
	{
		[Flags]
		enum _Flags
		{
			None = 0,
			IsVariadic = 1,
			KnownVariadic = 2,
			ContainsGenericParameters = 4,
			KnownContainsGenericParameters = 8,
			IsExtension = 16,
			KnownExtension = 32,
		}

		MethodBase _method;
		ReadOnlyCollection<ParameterInfo> _parameters; // lazy
		ReadOnlyCollection<Type> _genericArguments; // lazy
		_Flags _flags; // lazy

		/// <summary>指定されたメソッドまたはコンストラクタを使用して、<see cref="Microsoft.Scripting.Actions.Calls.ReflectionOverloadInfo"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="method">基になるメソッドまたはコンストラクタを指定します。</param>
		public ReflectionOverloadInfo(MethodBase method) { _method = method; }

		/// <summary>このメソッドオーバーロードに対する <see cref="MethodBase"/> を取得します。</summary>
		public override MethodBase ReflectionInfo { get { return _method; } }

		/// <summary>このメソッドオーバーロードの名前を取得します。</summary>
		public override string Name { get { return _method.Name; } }

		/// <summary>このメソッドオーバーロードの仮引数のリストを取得します。</summary>
		public override IList<ParameterInfo> Parameters { get { return _parameters ?? (_parameters = new ReadOnlyCollection<ParameterInfo>(_method.GetParameters())); } }

		/// <summary>戻り値に対する <see cref="System.Reflection.ParameterInfo"/> を取得します。コンストラクタの場合は <c>null</c> となります。</summary>
		public override ParameterInfo ReturnParameter
		{
			get
			{
				MethodInfo method = _method as MethodInfo;
				return method != null ? method.ReturnParameter : null;
			}
		}

		/// <summary>このメソッドオーバーロードのジェネリック型引数を取得します。</summary>
		public override IList<Type> GenericArguments { get { return _genericArguments ?? (_genericArguments = new ReadOnlyCollection<Type>(_method.GetGenericArguments())); } }

		/// <summary>このメソッドオーバーロードが宣言されている型を取得します。</summary>
		public override Type DeclaringType { get { return _method.DeclaringType; } }

		/// <summary>このメソッドオーバーロードの戻り値の型を取得します。コンストラクタの場合は <c>null</c> になります。</summary>
		public override Type ReturnType { get { return _method.GetReturnType(); } }

		/// <summary>このメソッドオーバーロードに対して有効な呼び出し規約を取得します。</summary>
		public override CallingConventions CallingConvention { get { return _method.CallingConvention; } }

		/// <summary>このメソッドオーバーロードの属性を示すフラグを取得します。</summary>
		public override MethodAttributes Attributes { get { return _method.Attributes; } }

		/// <summary>このメソッドオーバーロードがインスタンスを作成できるかどうかを示す値を取得します。</summary>
		public override bool IsInstanceFactory { get { return CompilerHelpers.IsConstructor(_method); } }

		/// <summary>このメソッドオーバーロードがコンストラクタであるかどうかを示す値を取得します。</summary>
		public override bool IsConstructor { get { return _method.IsConstructor; } }

		/// <summary>このメソッドオーバーロードが拡張メソッドであるかどうかを示す値を取得します。</summary>
		public override bool IsExtension
		{
			get
			{
				if ((_flags & _Flags.KnownExtension) == 0)
					_flags |= _Flags.KnownExtension | (_method.IsExtension() ? _Flags.IsExtension : 0);
				return (_flags & _Flags.IsExtension) != 0;
			}
		}

		/// <summary>このメソッドオーバーロードの引数の数が可変であるかどうかを示す値を取得します。配列引数や辞書引数の場合は、引数の数は変化します。</summary>
		public override bool IsVariadic
		{
			get
			{
				if ((_flags & _Flags.KnownVariadic) == 0)
					_flags |= _Flags.KnownVariadic | (IsVariadicInternal() ? _Flags.IsVariadic : 0);
				return (_flags & _Flags.IsVariadic) != 0;
			}
		}

		bool IsVariadicInternal()
		{
			var ps = Parameters;
			for (int i = ps.Count - 1; i >= 0; i--)
			{
				if (ps[i].IsParamArray() || ps[i].IsParamDictionary())
					return true;
			}
			return false;
		}

		/// <summary>このメソッドオーバーロードがジェネリックメソッドかどうかを示す値を取得します。</summary>
		public override bool IsGenericMethod { get { return _method.IsGenericMethod; } }

		/// <summary>このメソッドオーバーロードがジェネリックメソッドの定義かどうかを示す値を取得します。</summary>
		public override bool IsGenericMethodDefinition { get { return _method.IsGenericMethodDefinition; } }

		/// <summary>このメソッドオーバーロードが割り当てられていないジェネリック型引数を含んでいるかどうかを示す値を取得します。</summary>
		public override bool ContainsGenericParameters
		{
			get
			{
				if ((_flags & _Flags.KnownContainsGenericParameters) == 0)
					_flags |= _Flags.KnownContainsGenericParameters | (_method.ContainsGenericParameters ? _Flags.ContainsGenericParameters : 0);
				return (_flags & _Flags.ContainsGenericParameters) != 0;
			}
		}

		/// <summary>このメソッドオーバーロードのジェネリック型引数に指定された型を割り当てて、ジェネリックメソッドを作成します。</summary>
		/// <param name="genericArguments">ジェネリックメソッドの型引数に割り当てる型を指定します。</param>
		/// <returns>指定された型が割り当てられたジェネリックメソッドを示す <see cref="OverloadInfo"/>。</returns>
		public override OverloadInfo MakeGenericMethod(Type[] genericArguments) { return new ReflectionOverloadInfo(((MethodInfo)_method).MakeGenericMethod(genericArguments)); }

		/// <summary>指定されたメソッドの配列から対応する <see cref="OverloadInfo"/> を作成します。</summary>
		/// <param name="methods"><see cref="OverloadInfo"/> の基になるメソッドの配列を指定します。</param>
		/// <returns>指定されたメソッドに対応する <see cref="OverloadInfo"/> の配列。</returns>
		public static OverloadInfo[] CreateArray(MemberInfo[] methods) { return Array.ConvertAll(methods, m => new ReflectionOverloadInfo((MethodBase)m)); }
	}
}
