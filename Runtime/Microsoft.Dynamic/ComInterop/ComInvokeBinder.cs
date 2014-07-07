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
using System.Runtime.InteropServices;
using Microsoft.Scripting.Utils;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Scripting.ComInterop
{
	sealed class ComInvokeBinder
	{
		readonly ComMethodDesc _methodDesc;
		readonly Expression _method;        // 呼び出される ComMethodDesc
		readonly Expression _dispatch;      // IDispatch

		readonly CallInfo _callInfo;
		readonly DynamicMetaObject[] _args;
		readonly bool[] _isByRef;
		readonly Expression _instance;

		BindingRestrictions _restrictions;

		VarEnumSelector _varEnumSelector;
		string[] _keywordArgNames;
		int _totalExplicitArgs; // 存在すれば、ArgumentKind.Dictionary の個別の要素を含む。

		ParameterExpression _dispatchObject;
		ParameterExpression _dispatchPointer;
		ParameterExpression _dispId;
		ParameterExpression _dispParams;
		ParameterExpression _paramVariants;
		ParameterExpression _invokeResult;
		ParameterExpression _returnValue;
		ParameterExpression _dispIdsOfKeywordArgsPinned;
		ParameterExpression _propertyPutDispId;

		internal ComInvokeBinder(CallInfo callInfo, DynamicMetaObject[] args, bool[] isByRef, BindingRestrictions restrictions, Expression method, Expression dispatch, ComMethodDesc methodDesc)
		{
			Debug.Assert(callInfo != null, "arguments");
			Debug.Assert(args != null, "args");
			Debug.Assert(isByRef != null, "isByRef");
			Debug.Assert(method != null, "method");
			Debug.Assert(dispatch != null, "dispatch");
			Debug.Assert(Utils.TypeUtils.AreReferenceAssignable(typeof(ComMethodDesc), method.Type), "method");
			Debug.Assert(Utils.TypeUtils.AreReferenceAssignable(typeof(IDispatch), dispatch.Type), "dispatch");
			_method = method;
			_dispatch = dispatch;
			_methodDesc = methodDesc;
			_callInfo = callInfo;
			_args = args;
			_isByRef = isByRef;
			_restrictions = restrictions;
			// CallBinderHelper は動作する引数の正しい個数を持っているので、何かの値のインスタンスを設定する。
			_instance = dispatch;
		}

		ParameterExpression DispatchObjectVariable { get { return EnsureVariable(ref _dispatchObject, typeof(IDispatch), "dispatchObject"); } }

		ParameterExpression DispatchPointerVariable { get { return EnsureVariable(ref _dispatchPointer, typeof(IntPtr), "dispatchPointer"); } }

		ParameterExpression DispIdVariable { get { return EnsureVariable(ref _dispId, typeof(int), "dispId"); } }

		ParameterExpression DispParamsVariable { get { return EnsureVariable(ref _dispParams, typeof(ComTypes.DISPPARAMS), "dispParams"); } }

		ParameterExpression InvokeResultVariable { get { return EnsureVariable(ref _invokeResult, typeof(Variant), "invokeResult"); } }

		ParameterExpression ReturnValueVariable { get { return EnsureVariable(ref _returnValue, typeof(object), "returnValue"); } }

		ParameterExpression DispIdsOfKeywordArgsPinnedVariable { get { return EnsureVariable(ref _dispIdsOfKeywordArgsPinned, typeof(GCHandle), "dispIdsOfKeywordArgsPinned"); } }

		ParameterExpression PropertyPutDispIdVariable { get { return EnsureVariable(ref _propertyPutDispId, typeof(int), "propertyPutDispId"); } }

		ParameterExpression ParamVariantsVariable { get { return EnsureVariable(ref _paramVariants, VariantArray.GetStructType(_args.Length), "paramVariants"); } }

		static ParameterExpression EnsureVariable(ref ParameterExpression var, Type type, string name) { return var ?? (var = Expression.Variable(type, name)); }

		static Type MarshalType(DynamicMetaObject mo, bool isByRef)
		{
			var marshalType = mo.Value == null && mo.HasValue && !mo.LimitType.IsValueType ? null : mo.LimitType;
			// mo.Expression が書き込みか、またはその評価が副作用を持たないかをチェックしません。
			// 仮定は ByRef 引数情報とそれを一致させるものがすべてこれを処理するということです。
			// null はただ null が渡されたことを意味します。
			return isByRef ? (marshalType ?? (marshalType = mo.Expression.Type)).MakeByRefType() : marshalType;
		}

		internal DynamicMetaObject Invoke()
		{
			_keywordArgNames = _callInfo.ArgumentNames.ToArray();
			_totalExplicitArgs = _args.Length;
			// インスタンスはテストしたので、もう一度テストする必要はない。
			_restrictions = _args.Aggregate(_restrictions, (x, y) => x.Merge(ComBinderHelpers.GetTypeRestrictionForDynamicMetaObject(y)));
			_varEnumSelector = new VarEnumSelector(_args.Select((x, i) => MarshalType(x, _isByRef[i])).ToArray());
			return new DynamicMetaObject(CreateScope(MakeIDispatchInvokeTarget()), BindingRestrictions.Combine(_args).Merge(_restrictions));
		}

		static void AddNotNull(List<ParameterExpression> list, ParameterExpression var)
		{
			if (var != null)
				list.Add(var);
		}

		Expression CreateScope(Expression expression)
		{
			List<ParameterExpression> vars = new List<ParameterExpression>();
			AddNotNull(vars, _dispatchObject);
			AddNotNull(vars, _dispatchPointer);
			AddNotNull(vars, _dispId);
			AddNotNull(vars, _dispParams);
			AddNotNull(vars, _paramVariants);
			AddNotNull(vars, _invokeResult);
			AddNotNull(vars, _returnValue);
			AddNotNull(vars, _dispIdsOfKeywordArgsPinned);
			AddNotNull(vars, _propertyPutDispId);
			return vars.Count > 0 ? Expression.Block(vars, expression) : expression;
		}

		Expression GenerateTryBlock() {
			// 変数宣言
            var excepInfo = Expression.Variable(typeof(ExcepInfo), "excepInfo");
            var argErr = Expression.Variable(typeof(uint), "argErr");
            var hresult = Expression.Variable(typeof(int), "hresult");
            List<Expression> tryStatements = new List<Expression>();
            if (_keywordArgNames.Length > 0)
                tryStatements.Add(
                    Expression.Assign(
                        Expression.Field(DispParamsVariable, typeof(ComTypes.DISPPARAMS).GetField("rgdispidNamedArgs")),
                        Expression.Call(typeof(UnsafeMethods).GetMethod("GetIdsOfNamedParameters"),
                            DispatchObjectVariable,
                            Expression.Constant(ArrayUtils.Insert(_methodDesc.Name, _keywordArgNames)),
                            DispIdVariable,
                            DispIdsOfKeywordArgsPinnedVariable
                        )
                    )
                );
			// Variant に引数をマーシャリング
            var positionalArgs = _varEnumSelector.VariantBuilders.Count - _keywordArgNames.Length; // 引数は名前ではなく位置によって渡される
			tryStatements.AddRange(_varEnumSelector.VariantBuilders.Select((x, i) => x.InitializeArgumentVariant(
				VariantArray.GetStructField(ParamVariantsVariable, i >= positionalArgs ? i - positionalArgs : _varEnumSelector.VariantBuilders.Count - 1 - i),
				_args[i].Expression
			)).Where(x => x != null));
            // Call Invoke
			// INVOKE_PROPERTYGET はプロパティをメソッドとして扱わなければならないので、型情報のない COM オブジェクトでのみ必要とされるべきです。
            tryStatements.Add(
				Expression.Assign(hresult,
					Expression.Call(typeof(UnsafeMethods).GetMethod("IDispatchInvoke"),
					    DispatchPointerVariable,
					    DispIdVariable,
					    Expression.Constant(_methodDesc.IsPropertyPut ?
							(_methodDesc.IsPropertyPutRef ? ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF : ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT) :
							ComTypes.INVOKEKIND.INVOKE_FUNC | ComTypes.INVOKEKIND.INVOKE_PROPERTYGET
						),
					    DispParamsVariable,
					    InvokeResultVariable,
					    excepInfo,
					    argErr
					)
				)
			);
            // ComRuntimeHelpers.CheckThrowException(hresult, excepInfo, argErr, ThisParameter);
            tryStatements.Add(Expression.Call(typeof(ComRuntimeHelpers).GetMethod("CheckThrowException"), hresult, excepInfo, argErr, Expression.Constant(_methodDesc.Name, typeof(string))));
            // _returnValue = (ReturnType)_invokeResult.ToObject();
            tryStatements.Add(Expression.Assign(ReturnValueVariable, Expression.Call(InvokeResultVariable, typeof(Variant).GetMethod("ToObject"))));
			tryStatements.AddRange(_varEnumSelector.VariantBuilders.Select((x, i) => x.UpdateFromReturn(_args[i].Expression)).Where(x => x != null));
            tryStatements.Add(Expression.Empty());
            return Expression.Block(new[] { excepInfo, argErr, hresult }, tryStatements);
        }

		Expression GenerateFinallyBlock()
		{
			List<Expression> exprs = new List<Expression>();
			// UnsafeMethods.IUnknownRelease(dispatchPointer);
			exprs.Add(Expression.Call(new Func<IntPtr, int>(UnsafeMethods.IUnknownRelease).Method, DispatchPointerVariable));
			// Clear memory allocated for marshalling
			exprs.AddRange(_varEnumSelector.VariantBuilders.Select(x => x.Clear()).Where(x => x != null));
			// _invokeResult.Clear()
			exprs.Add(Expression.Call(InvokeResultVariable, typeof(Variant).GetMethod("Clear")));
			// _dispIdsOfKeywordArgsPinned.Free()
			if (_dispIdsOfKeywordArgsPinned != null)
				exprs.Add(Expression.Call(DispIdsOfKeywordArgsPinnedVariable, typeof(GCHandle).GetMethod("Free")));
			exprs.Add(Expression.Empty());
			return Expression.Block(exprs);
		}

		/// <summary>最適化された呼び出しのターゲットに対するすタブを生成します。</summary>
		Expression MakeIDispatchInvokeTarget()
		{
			Debug.Assert(_varEnumSelector.VariantBuilders.Count == _totalExplicitArgs);
			List<Expression> exprs = new List<Expression>();
			// _dispId = ((DispCallable)this).ComMethodDesc.DispId;
			exprs.Add(Expression.Assign(DispIdVariable, Expression.Property(_method, typeof(ComMethodDesc).GetProperty("DispId"))));
			// _dispParams.rgvararg = RuntimeHelpers.UnsafeMethods.ConvertVariantByrefToPtr(ref _paramVariants._element0)
			if (_totalExplicitArgs != 0)
				exprs.Add(
					Expression.Assign(Expression.Field(DispParamsVariable, typeof(ComTypes.DISPPARAMS).GetField("rgvarg")),
						Expression.Call(
							new UnsafeMethods.ConvertByrefToPtrDelegate<Variant>(UnsafeMethods.ConvertVariantByrefToPtr).Method,
							VariantArray.GetStructField(ParamVariantsVariable, 0)
						)
					)
				);
			// _dispParams.cArgs = <number_of_params>;
			exprs.Add(Expression.Assign(Expression.Field(DispParamsVariable, typeof(ComTypes.DISPPARAMS).GetField("cArgs")), Expression.Constant(_totalExplicitArgs)));
			if (_methodDesc.IsPropertyPut)
			{
				// dispParams.cNamedArgs = 1;
				// dispParams.rgdispidNamedArgs = RuntimeHelpers.UnsafeMethods.GetNamedArgsForPropertyPut()
				exprs.Add(Expression.Assign(Expression.Field(DispParamsVariable, typeof(ComTypes.DISPPARAMS).GetField("cNamedArgs")), Expression.Constant(1)));
				exprs.Add(Expression.Assign(PropertyPutDispIdVariable, Expression.Constant(ComDispIds.DISPID_PROPERTYPUT)));
				exprs.Add(Expression.Assign(Expression.Field(DispParamsVariable, typeof(ComTypes.DISPPARAMS).GetField("rgdispidNamedArgs")),
						Expression.Call(new UnsafeMethods.ConvertByrefToPtrDelegate<int>(UnsafeMethods.ConvertInt32ByrefToPtr).Method, PropertyPutDispIdVariable)
					)
				);
			}
			else // _dispParams.cNamedArgs = N;
				exprs.Add(Expression.Assign(Expression.Field(DispParamsVariable, typeof(ComTypes.DISPPARAMS).GetField("cNamedArgs")), Expression.Constant(_keywordArgNames.Length)));
			// _dispatchObject = _dispatch
			// _dispatchPointer = Marshal.GetIDispatchForObject(_dispatchObject);
			exprs.Add(Expression.Assign(DispatchObjectVariable, _dispatch));
			exprs.Add(Expression.Assign(DispatchPointerVariable, Expression.Call(new Func<object, IntPtr>(Marshal.GetIDispatchForObject).Method, DispatchObjectVariable)));
			var tryStatements = GenerateTryBlock();
			var finallyStatements = GenerateFinallyBlock();
			exprs.Add(Expression.TryFinally(tryStatements, finallyStatements));
			exprs.Add(ReturnValueVariable);
			return Expression.Block(_varEnumSelector.VariantBuilders.Where(x => x.TempVariable != null).Select(x => x.TempVariable), exprs);
		}
	}
}