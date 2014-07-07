/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * ironpy@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>���\�b�h���Ăяo�����߂�\���܂��B</summary>
	public abstract partial class CallInstruction : Instruction
	{
		/// <summary>�Ăяo�����\�b�h��\�� <see cref="MethodInfo"/> ���擾���܂��B</summary>
		public abstract MethodInfo Info { get; }

		/// <summary>�C���X�^���X���\�b�h�ł� "this" ���܂ރ��\�b�h�̈����̌����擾���܂��B</summary>
		public abstract int ArgumentCount { get; }

		/// <summary><see cref="Microsoft.Scripting.Interpreter.CallInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		internal CallInstruction() { }

		static readonly ConcurrentDictionary<MethodInfo, CallInstruction> _cache = new ConcurrentDictionary<MethodInfo, CallInstruction>();

		/// <summary>�w�肳�ꂽ���\�b�h���Ăяo���K�؂� <see cref="CallInstruction"/> �N���X�̔h���N���X���쐬���܂��B</summary>
		/// <param name="info">�Ăяo�����\�b�h���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���\�b�h���Ăяo�� <see cref="CallInstruction"/> �N���X�̔h���N���X�B</returns>
		public static CallInstruction Create(MethodInfo info) { return Create(info, info.GetParameters()); }

		/// <summary>�w�肳�ꂽ���\�b�h���Ăяo���K�؂� <see cref="CallInstruction"/> �N���X�̔h���N���X���쐬���܂��B</summary>
		/// <param name="info">�Ăяo�����\�b�h���w�肵�܂��B</param>
		/// <param name="parameters">���\�b�h�̉�������\�� <see cref="ParameterInfo"/> �̔z����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���\�b�h���Ăяo�� <see cref="CallInstruction"/> �N���X�̔h���N���X�B</returns>
		public static CallInstruction Create(MethodInfo info, ParameterInfo[] parameters)
		{
			int argumentCount = parameters.Length;
			if (!info.IsStatic)
				argumentCount++;
			// CLR �o�O #796414 (Array.Get/Set �ɑ΂���f���Q�[�g���쐬�ł��Ȃ�) �ɑ΂�������:
			// T[]::Address - �߂�l T& �̂��ߎ��c���[�ł̓T�|�[�g����Ȃ�
			if (info.DeclaringType != null && info.DeclaringType.IsArray && (info.Name == "Get" || info.Name == "Set"))
				return GetArrayAccessor(info, argumentCount);
			if (info is DynamicMethod || !info.IsStatic && info.DeclaringType.IsValueType || argumentCount >= MaxHelpers || Array.Exists(parameters, x => x.ParameterType.IsByRef))
				return new MethodInfoCallInstruction(info, argumentCount);
			return ShouldCache(info) ? _cache.GetOrAdd(info, x => CreateWorker(x, argumentCount, parameters)) : CreateWorker(info, argumentCount, parameters);
		}

		static CallInstruction CreateWorker(MethodInfo info, int argumentCount, ParameterInfo[] parameters)
		{
			try { return argumentCount < MaxArgs ? FastCreate(info, parameters) : SlowCreate(info, parameters); }
			catch (TargetInvocationException tie)
			{
				if (!(tie.InnerException is NotSupportedException))
					throw;
				return new MethodInfoCallInstruction(info, argumentCount);
			}
			catch (NotSupportedException)
			{
				// Delegate.CreateDelegate �����\�b�h���n���h���ł��Ȃ��ꍇ�A�x�����t���N�V�����o�[�W�����Ƀt�H�[���o�b�N����
				// �Ⴆ�΂���̓C���^�[�t�F�C�X�ɒ�`����ăN���X�Ɏ�������Ă���W�F�l���b�N���\�b�h�̏ꍇ�ɔ�������\��������
				return new MethodInfoCallInstruction(info, argumentCount);
			}
		}

		static CallInstruction GetArrayAccessor(MethodInfo info, int argumentCount)
		{
			var isGetter = info.Name == "Get";
			switch (info.DeclaringType.GetArrayRank())
			{
				case 1:
					return Create(isGetter ? info.DeclaringType.GetMethod("GetValue", new[] { typeof(int) }) : new Action<Array, int, object>(ArrayItemSetter1).Method);
				case 2:
					return Create(isGetter ? info.DeclaringType.GetMethod("GetValue", new[] { typeof(int), typeof(int) }) : new Action<Array, int, int, object>(ArrayItemSetter2).Method);
				case 3:
					return Create(isGetter ? info.DeclaringType.GetMethod("GetValue", new[] { typeof(int), typeof(int), typeof(int) }) : new Action<Array, int, int, int, object>(ArrayItemSetter3).Method);
				default:
					return new MethodInfoCallInstruction(info, argumentCount);
			}
		}

		static void ArrayItemSetter1(Array array, int index0, object value) { array.SetValue(value, index0); }

		static void ArrayItemSetter2(Array array, int index0, int index1, object value) { array.SetValue(value, index0, index1); }

		static void ArrayItemSetter3(Array array, int index0, int index1, int index2, object value) { array.SetValue(value, index0, index1, index2); }

		static bool ShouldCache(MethodInfo info) { return !(info is DynamicMethod); }

		/// <summary>���̌^�܂��͂���ȏ�^�����p�ł��Ȃ��ꍇ��\�� <c>null</c> ���擾���܂��B</summary>
		static Type TryGetParameterOrReturnType(MethodInfo target, ParameterInfo[] pi, int index)
		{
			if (!target.IsStatic && --index < 0)
				return target.DeclaringType;
			if (index < pi.Length)
				return pi[index].ParameterType; // next in signature
			if (target.ReturnType == typeof(void) || index > pi.Length)
				return null; // no more parameters
			return target.ReturnType; // last parameter on Invoke is return type
		}

		static bool IndexIsNotReturnType(int index, MethodInfo target, ParameterInfo[] pi) { return pi.Length != index || (pi.Length == index && !target.IsStatic); }

		/// <summary>�K�؂� <see cref="CallInstruction"/> �h���N���X�̃C���X�^���X���쐬���邽�߂Ƀ��t���N�V�������g�p���܂��B</summary>
		static CallInstruction SlowCreate(MethodInfo info, ParameterInfo[] pis)
		{
			List<Type> types = new List<Type>();
			if (!info.IsStatic)
				types.Add(info.DeclaringType);
			foreach (var pi in pis)
				types.Add(pi.ParameterType);
			if (info.ReturnType != typeof(void))
				types.Add(info.ReturnType);
			Type[] arrTypes = types.ToArray();
			return (CallInstruction)Activator.CreateInstance(GetHelperType(info, arrTypes), info);
		}

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public sealed override int ProducedStack { get { return Info.ReturnType == typeof(void) ? 0 : 1; } }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public sealed override int ConsumedStack { get { return ArgumentCount; } }
		
		/// <summary>���̖��߂̖��O���擾���܂��B</summary>
		public sealed override string InstructionName { get { return "Call"; } }

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return "Call(" + Info + ")"; }
	}

	sealed class MethodInfoCallInstruction : CallInstruction
	{
		readonly MethodInfo _target;
		readonly int _argumentCount;

		public override MethodInfo Info { get { return _target; } }

		public override int ArgumentCount { get { return _argumentCount; } }

		internal MethodInfoCallInstruction(MethodInfo target, int argumentCount)
		{
			_target = target;
			_argumentCount = argumentCount;
		}

		public override object Invoke(params object[] args) { return InvokeWorker(args); }

		public override object InvokeInstance(object instance, params object[] args)
		{
			if (_target.IsStatic)
			{
				try { return _target.Invoke(null, args); }
				catch (TargetInvocationException ex) { throw ExceptionHelpers.UpdateForRethrow(ex.InnerException); }
			}
			try { return _target.Invoke(instance, args); }
			catch (TargetInvocationException ex) { throw ExceptionHelpers.UpdateForRethrow(ex.InnerException); }
		}
		
		public override object Invoke() { return InvokeWorker(); }

		public override object Invoke(object arg0) { return InvokeWorker(arg0); }

		public override object Invoke(object arg0, object arg1) { return InvokeWorker(arg0, arg1); }

		public override object Invoke(object arg0, object arg1, object arg2) { return InvokeWorker(arg0, arg1, arg2); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3) { return InvokeWorker(arg0, arg1, arg2, arg3); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4) { return InvokeWorker(arg0, arg1, arg2, arg3, arg4); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5) { return InvokeWorker(arg0, arg1, arg2, arg3, arg4, arg5); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6) { return InvokeWorker(arg0, arg1, arg2, arg3, arg4, arg5, arg6); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7) { return InvokeWorker(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8) { return InvokeWorker(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8); }
		
		object InvokeWorker(params object[] args)
		{
			if (_target.IsStatic)
			{
				try { return _target.Invoke(null, args); }
				catch (TargetInvocationException ex) { throw ExceptionHelpers.UpdateForRethrow(ex.InnerException); }
			}
			try { return _target.Invoke(args[0], Utils.ArrayUtils.RemoveFirst(args)); }
			catch (TargetInvocationException ex) { throw ExceptionHelpers.UpdateForRethrow(ex.InnerException); }
		}

		public sealed override int Run(InterpretedFrame frame)
		{
			var args = new object[ArgumentCount];
			for (int i = ArgumentCount - 1; i >= 0; i--)
				args[i] = frame.Pop();
			var ret = Invoke(args);
			if (_target.ReturnType != typeof(void))
				frame.Push(ret);
			return 1;
		}
	}
}
