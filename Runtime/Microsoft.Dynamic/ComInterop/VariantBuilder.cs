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
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>IDispatch.Invoke への呼び出しに対する Variant への引数のパッケージングをハンドルします。</summary>
	class VariantBuilder
	{
		MemberExpression _variant;
		readonly ArgBuilder _argBuilder;
		readonly VarEnum _targetComType;
		internal ParameterExpression TempVariable { get; private set; }

		internal VariantBuilder(VarEnum targetComType, ArgBuilder builder)
		{
			_targetComType = targetComType;
			_argBuilder = builder;
		}

		internal bool IsByRef { get { return (_targetComType & VarEnum.VT_BYREF) != 0; } }

		internal Expression InitializeArgumentVariant(MemberExpression variant, Expression parameter)
		{
			// NOTE: Variant を記憶しなければなりません。
			// 理由は引数の順序が呼び出しに対して Variant の順序に正確にマッピングしないということと、
			// クリーンアップ時に初期化した Variant をクリーニングすることを保証するためです。
			_variant = variant;
			if (IsByRef)
			{
				// temp = argument
				// paramVariants._elementN.SetAsByrefT(ref temp)
				Debug.Assert(TempVariable == null);
				var argExpr = _argBuilder.MarshalToRef(parameter);
				TempVariable = Expression.Variable(argExpr.Type, null);
				return Expression.Block(Expression.Assign(TempVariable, argExpr), Expression.Call(variant, Variant.GetByrefSetter(_targetComType & ~VarEnum.VT_BYREF), TempVariable));
			}
			var argument = _argBuilder.Marshal(parameter);
			// ConvertArgBuilder が対応する _targetComType を持っていないため、特別な扱いを強制されます。
			if (_argBuilder is ConvertibleArgBuilder)
				return Expression.Call(variant, typeof(Variant).GetMethod("SetAsIConvertible"), argument);
			if (Variant.IsPrimitiveType(_targetComType) || _targetComType == VarEnum.VT_DISPATCH || _targetComType == VarEnum.VT_UNKNOWN ||
			   _targetComType == VarEnum.VT_VARIANT || _targetComType == VarEnum.VT_RECORD || _targetComType == VarEnum.VT_ARRAY)
				return Expression.Assign(Expression.Property(variant, Variant.GetAccessor(_targetComType)), argument); // paramVariants._elementN.AsT = (cast)argN
			switch (_targetComType)
			{
				case VarEnum.VT_EMPTY:
					return null;
				case VarEnum.VT_NULL:
					return Expression.Call(variant, typeof(Variant).GetMethod("SetAsNull")); // paramVariants._elementN.SetAsNull();
				default:
					Debug.Assert(false, "Unexpected VarEnum");
					return null;
			}
		}

		static Expression Release(Expression pUnk) { return Expression.Call(new Action<IntPtr>(UnsafeMethods.IUnknownReleaseNotZero).Method, pUnk); }

		internal Expression Clear()
		{
			if (IsByRef)
			{
				if (_argBuilder is StringArgBuilder)
				{
					Debug.Assert(TempVariable != null);
					return Expression.Call(new Action<IntPtr>(Marshal.FreeBSTR).Method, TempVariable);
				}
				else if (_argBuilder is DispatchArgBuilder)
				{
					Debug.Assert(TempVariable != null);
					return Release(TempVariable);
				}
				else if (_argBuilder is UnknownArgBuilder)
				{
					Debug.Assert(TempVariable != null);
					return Release(TempVariable);
				}
				else if (_argBuilder is VariantArgBuilder)
				{
					Debug.Assert(TempVariable != null);
					return Expression.Call(TempVariable, typeof(Variant).GetMethod("Clear"));
				}
				return null;
			}
			switch (_targetComType)
			{
				case VarEnum.VT_EMPTY:
				case VarEnum.VT_NULL:
					return null;
				case VarEnum.VT_BSTR:
				case VarEnum.VT_UNKNOWN:
				case VarEnum.VT_DISPATCH:
				case VarEnum.VT_ARRAY:
				case VarEnum.VT_RECORD:
				case VarEnum.VT_VARIANT:
					return Expression.Call(_variant, typeof(Variant).GetMethod("Clear")); // paramVariants._elementN.Clear()
				default:
					Debug.Assert(Variant.IsPrimitiveType(_targetComType), "Unexpected VarEnum");
					return null;
			}
		}

		internal Expression UpdateFromReturn(Expression parameter) { return TempVariable == null ? null : Expression.Assign(parameter, Ast.Utils.Convert(_argBuilder.UnmarshalFromRef(TempVariable), parameter.Type)); }
	}
}