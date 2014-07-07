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
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.Contracts;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// �񋟂��ꂽ <see cref="LanguageContext"/> �ɂ���ė��p�\�ɂȂ� DLR �o�C���_�[���g�p����I�u�W�F�N�g����f���Q�[�g�ւ̕ϊ��̃T�|�[�g��񋟂��܂��B
	/// ��ɂ���� <see cref="IDynamicMetaObjectProvider"/> ����������I�u�W�F�N�g����K�؂ȃf���Q�[�g�^�ւ̕ϊ����T�|�[�g���܂��B
	/// �񋟂��ꂽ�I�u�W�F�N�g�����łɓK�؂Ȍ^�̃f���Q�[�g�ł���ꍇ�́A�P���Ƀf���Q�[�g���Ԃ���܂��B
	/// </summary>
	public class DynamicDelegateCreator
	{
		readonly LanguageContext _languageContext;

		/// <summary><see cref="LanguageContext"/> ���g�p���āA<see cref="Microsoft.Scripting.Runtime.DynamicDelegateCreator"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="languageContext">�f���Q�[�g�ւ̕ϊ��Ɏg�p����� DLR �o�C���_�[��񋟂��� <see cref="LanguageContext"/> ���w�肵�܂��B</param>
		public DynamicDelegateCreator(LanguageContext languageContext)
		{
			ContractUtils.RequiresNotNull(languageContext, "languageContext");
			_languageContext = languageContext;
		}

		/// <summary>���\�b�h�V�O�l�`���Ɋ�Â��ċ��L����铮�I�ɐ������ꂽ�f���Q�[�g�̃e�[�u���ł��B</summary>
		ConcurrentDictionary<SignatureInfo, DelegateInfo> _cache = new ConcurrentDictionary<SignatureInfo, DelegateInfo>();

		/// <summary>
		/// ���̃I�u�W�F�N�g��񓮓I�R�[�h���� (�R�[�h�R���e�L�X�g�Ȃ���) �Ăяo�����߂Ɏg�p�����\���̂���w�肳�ꂽ�V�O�l�`���̃f���Q�[�g���쐬���܂��B
		/// �X�^�u�͓K�؂ȕϊ�/�{�b�N�X�������s������A�I�u�W�F�N�g���Ăяo�����肷�邽�߂ɍ쐬����܂��B
		/// �X�^�u�͂��̃I�u�W�F�N�g�̌���R���e�L�X�g���Ŏ��s�����K�v������܂��B
		/// </summary>
		/// <param name="callableObject">�f���Q�[�g�ɕϊ������I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="delegateType">�ϊ���̃f���Q�[�g�^���w�肵�܂��B</param>
		/// <returns>�ϊ����ꂽ�f���Q�[�g�B</returns>
		/// <exception cref="Microsoft.Scripting.ArgumentTypeException">
		/// �I�u�W�F�N�g�� <see cref="Delegate"/> �^�̃T�u�N���X�ł����A�w�肳�ꂽ�^�ł͂���܂���B
		/// �܂��́A�I�u�W�F�N�g�� <see cref="IDynamicMetaObjectProvider"/> ���������Ă��܂���B
		/// </exception>
		public Delegate GetDelegate(object callableObject, Type delegateType)
		{
			ContractUtils.RequiresNotNull(delegateType, "delegateType");
			var result = callableObject as Delegate;
			if (result != null)
			{
				if (!delegateType.IsAssignableFrom(result.GetType()))
					throw ScriptingRuntimeHelpers.SimpleTypeError(string.Format("Cannot cast {0} to {1}.", result.GetType(), delegateType));
				return result;
			}
			var provider = callableObject as IDynamicMetaObjectProvider;
			if (provider != null)
			{
				MethodInfo invoke;
				if (!typeof(Delegate).IsAssignableFrom(delegateType) || (invoke = delegateType.GetMethod("Invoke")) == null)
					throw ScriptingRuntimeHelpers.SimpleTypeError("A specific delegate type is required.");
				if ((result = _cache.GetOrAdd(new SignatureInfo(invoke.ReturnType, invoke.GetParameters()), key => key.CreateDelegateInfo(_languageContext)).CreateDelegate(delegateType, provider)) != null)
					return result;
			}
			throw ScriptingRuntimeHelpers.SimpleTypeError("Object is not callable.");
		}

		sealed class SignatureInfo
		{
			readonly Type _returnType;
			readonly ParameterInfo[] _parameters;

			internal SignatureInfo(Type returnType, ParameterInfo[] parameters)
			{
				Assert.NotNull(returnType);
				Assert.NotNullItems(parameters);
				_parameters = parameters;
				_returnType = returnType;
			}

			[Confined]
			public override bool Equals(object obj)
			{
				var dsi = obj as SignatureInfo;
				return dsi != null && dsi._returnType == _returnType && _parameters.SequenceEqual(dsi._parameters);
			}

			[Confined]
			public override int GetHashCode() { return _parameters.Aggregate(5331, (x, y) => x ^ y.GetHashCode()) ^ _returnType.GetHashCode(); }

			[Confined]
			public override string ToString() { return _returnType.ToString() + "(" + string.Join(", ", _parameters.Select(x => x.ParameterType.ToString())) + ")"; }

			internal DelegateInfo CreateDelegateInfo(LanguageContext context) { return new DelegateInfo(context, _returnType, _parameters); }
		}

		sealed class DelegateInfo
		{
			readonly Type _returnType;
			readonly ParameterInfo[] _parameters;
			readonly MethodInfo _method;
			readonly object[] _constants;
			WeakDictionary<object, WeakReference<object[]>> _constantMap = new WeakDictionary<object, WeakReference<object[]>>();
			readonly InvokeBinder _invokeBinder;
			readonly ConvertBinder _convertBinder;

			static readonly object TargetPlaceHolder = new object();
			static readonly object CallSitePlaceHolder = new object();
			static readonly object ConvertSitePlaceHolder = new object();

			internal DelegateInfo(LanguageContext context, Type returnType, ParameterInfo[] parameters)
			{
				Assert.NotNull(returnType);
				Assert.NotNullItems(parameters);
				_returnType = returnType;
				_parameters = parameters;
				PerfTrack.NoteEvent(PerfTrack.Category.DelegateCreate, ToString());
				if (_returnType != typeof(void))
					_convertBinder = context.CreateConvertBinder(_returnType, true);
				_invokeBinder = context.CreateInvokeBinder(new CallInfo(_parameters.Length));
				// Create the method with a special name so the langauge compiler knows that method's stack frame is not visible
				var cg = Snippets.CreateDynamicMethod("_Scripting_", _returnType, ArrayUtils.Insert(typeof(object[]), Array.ConvertAll(_parameters, x => x.ParameterType)), false);
				// Emit the stub
				_constants = EmitClrCallStub(cg.Generator);
				_method = cg.Finish();
			}

			internal Delegate CreateDelegate(Type delegateType, object target)
			{
				Assert.NotNull(delegateType, target);

				// to enable:
				// function x() { }
				// someClass.someEvent += delegateType(x) 
				// someClass.someEvent -= delegateType(x) 
				//
				// we need to avoid re-creating the object array because they won't be compare equal when removing the delegate if they're difference instances.
				// Therefore we use a weak hashtable to get back the original object array.
				// The values also need to be weak to avoid creating a circular reference from the constants target back to the target.
				// This is fine because as long as the delegate is referenced the object array will stay alive.
				// Once the delegate is gone it's not wired up anywhere and -= will never be used again.

				object[] clone;
				lock (_constantMap)
				{
					WeakReference<object[]> cloneRef;
					if (!_constantMap.TryGetValue(target, out cloneRef) || !cloneRef.TryGetTarget(out clone))
					{
						_constantMap[target] = new WeakReference<object[]>(clone = ArrayUtils.Copy(_constants));
						Debug.Assert(clone[0] == TargetPlaceHolder);
						Debug.Assert(clone[1] == CallSitePlaceHolder);
						Debug.Assert(clone[2] == ConvertSitePlaceHolder);
						clone[0] = target;
						clone[1] = MakeCallSite();
						clone[2] = _returnType != typeof(void) ? CallSite.Create(Expression.GetDelegateType(typeof(CallSite), typeof(object), _returnType), _convertBinder) : null;
					}
				}
				return _method.CreateDelegate(delegateType, clone);
			}

			/// <summary> CLR �Ăяo������M���āA���I����R�[�h���Ăяo���X�^�u�𐶐����܂��B</summary>
			object[] EmitClrCallStub(ILGenerator il)
			{
				List<ReturnFixer> fixers = new List<ReturnFixer>(0);
				// Create strongly typed return type from the site.
				// This will, among other things, generate tighter code.
				var siteType = MakeCallSite().GetType();
				var convertSiteType = _returnType != typeof(void) ? CallSite.Create(Expression.GetDelegateType(typeof(CallSite), typeof(object), _returnType), _convertBinder).GetType() : null;
				const int TargetIndex = 0, CallSiteIndex = 1, ConvertSiteIndex = 2;
				FieldInfo convertTarget = null;
				if (_returnType != typeof(void))
				{
					// load up the conversion logic on the stack
					var convertSiteLocal = il.DeclareLocal(convertSiteType);
					EmitConstantGet(il, ConvertSiteIndex, convertSiteType);
					il.Emit(OpCodes.Dup);
					il.Emit(OpCodes.Stloc, convertSiteLocal);
					il.EmitFieldGet(convertTarget = convertSiteType.GetField("Target"));
					il.Emit(OpCodes.Ldloc, convertSiteLocal);
				}
				// load up the invoke logic on the stack
				var siteLocal = il.DeclareLocal(siteType);
				EmitConstantGet(il, CallSiteIndex, siteType);
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Stloc, siteLocal);
				var target = siteType.GetField("Target");
				il.EmitFieldGet(target);
				il.Emit(OpCodes.Ldloc, siteLocal);
				EmitConstantGet(il, TargetIndex, typeof(object));
				for (int i = 0; i < _parameters.Length; i++)
				{
					if (_parameters[i].ParameterType.IsByRef)
					{
						var rf = ReturnFixer.EmitArgument(il, i + 1, _parameters[i].ParameterType);
						if (rf != null)
							fixers.Add(rf);
					}
					else
						il.EmitLoadArg(i + 1);
				}
				// emit the invoke for the call
				il.EmitCall(target.FieldType, "Invoke");
				// emit the invoke for the convert
				if (_returnType == typeof(void))
					il.Emit(OpCodes.Pop);
				else
					il.EmitCall(convertTarget.FieldType, "Invoke");
				// fixup any references
				foreach (var rf in fixers)
					rf.FixReturn(il);
				il.Emit(OpCodes.Ret);
				// build up constants array
				return new object[] { TargetPlaceHolder, CallSitePlaceHolder, ConvertSitePlaceHolder };
			}

			static void EmitConstantGet(ILGenerator il, int index, Type type)
			{
				il.Emit(OpCodes.Ldarg_0);
				il.EmitInt32(index);
				il.Emit(OpCodes.Ldelem_Ref);
				if (type != typeof(object))
					il.Emit(OpCodes.Castclass, type);
			}

			CallSite MakeCallSite()
			{
				Type[] sig = new Type[_parameters.Length + 3];
				// call site
				sig[0] = typeof(CallSite);
				// target object
				sig[1] = typeof(object);
				// arguments
				for (int i = 0; i < _parameters.Length; i++)
					sig[i + 2] = _parameters[i].ParameterType.IsByRef ? typeof(object) : _parameters[i].ParameterType;
				// return type
				sig[sig.Length - 1] = typeof(object);
				return CallSite.Create(Expression.GetDelegateType(sig), _invokeBinder);
			}
		}
	}
}
