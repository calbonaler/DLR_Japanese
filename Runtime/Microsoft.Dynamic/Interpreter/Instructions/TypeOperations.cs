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
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>�w�肳�ꂽ <see cref="LightDelegateCreator"/> ���g�p����f���Q�[�g�̍쐬���s�����߂�\���܂��B</summary>
	sealed class CreateDelegateInstruction : Instruction
	{
		readonly LightDelegateCreator _creator;

		/// <summary>�w�肳�ꂽ <see cref="LightDelegateCreator"/> ���g�p���āA<see cref="Microsoft.Scripting.Interpreter.CreateDelegateInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="delegateCreator">�f���Q�[�g�̍쐬���Ǘ����� <see cref="LightDelegateCreator"/> ���w�肵�܂��B</param>
		internal CreateDelegateInstruction(LightDelegateCreator delegateCreator) { _creator = delegateCreator; }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return _creator.Interpreter.ClosureSize; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			StrongBox<object>[] closure;
			if (_creator.Interpreter.ClosureSize > 0)
			{
				closure = new StrongBox<object>[_creator.Interpreter.ClosureSize];
				for (int i = closure.Length - 1; i >= 0; i--)
					closure[i] = (StrongBox<object>)frame.Pop();
			}
			else
				closure = null;
			frame.Push(_creator.CreateDelegate(closure));
			return +1;
		}
	}

	/// <summary>�w�肳�ꂽ�R���X�g���N�^�����s���邱�Ƃɂ��C���X�^���X�̍쐬���s�����߂�\���܂��B</summary>
	sealed class NewInstruction : Instruction
	{
		readonly ConstructorInfo _constructor;
		readonly int _argCount;

		/// <summary>�w�肳�ꂽ�R���X�g���N�^���g�p���āA<see cref="Microsoft.Scripting.Interpreter.NewInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="constructor">�쐬���ꂽ�C���X�^���X�̏������Ɏg�p�����R���X�g���N�^���w�肵�܂��B</param>
		public NewInstruction(ConstructorInfo constructor)
		{
			_constructor = constructor;
			_argCount = constructor.GetParameters().Length;
		}

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return _argCount; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			object[] args = new object[_argCount];
			for (int i = _argCount - 1; i >= 0; i--)
				args[i] = frame.Pop();
			object ret;
			try { ret = _constructor.Invoke(args); }
			catch (TargetInvocationException ex)
			{
				ExceptionHelpers.UpdateForRethrow(ex.InnerException);
				throw ex.InnerException;
			}
			frame.Push(ret);
			return +1;
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return "New " + _constructor.DeclaringType.Name + "(" + _constructor + ")"; }
	}

	/// <summary>�^�̊���l��]���X�^�b�N�ɓǂݍ��ޖ��߂�\���܂��B</summary>
	/// <typeparam name="T">����l���擾����^���w�肵�܂��B</typeparam>
	sealed class DefaultValueInstruction<T> : Instruction
	{
		/// <summary><see cref="Microsoft.Scripting.Interpreter.DefaultValueInstruction&lt;T&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		internal DefaultValueInstruction() { }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(default(T));
			return +1;
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return "New " + typeof(T); }
	}

	/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ��\���ǂ����𔻒f���閽�߂�\���܂��B</summary>
	/// <typeparam name="T">�I�u�W�F�N�g���ϊ������^���w�肵�܂��B</typeparam>
	sealed class TypeIsInstruction<T> : Instruction
	{
		/// <summary><see cref="Microsoft.Scripting.Interpreter.TypeIsInstruction&lt;T&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		internal TypeIsInstruction() { }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			// unfortunately Type.IsInstanceOfType() is 35-times slower than "is T" so we use generic code:
			frame.Push(frame.Pop() is T);
			return +1;
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return "TypeIs " + typeof(T).Name; }
	}

	/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�^�ւ̕ϊ������݁A���s�����ꍇ�� <c>null</c> ��Ԃ����߂�\���܂��B</summary>
	/// <typeparam name="T"></typeparam>
	sealed class TypeAsInstruction<T> : Instruction
	{
		/// <summary><see cref="Microsoft.Scripting.Interpreter.TypeAsInstruction&lt;T&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		internal TypeAsInstruction() { }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			// can't use as w/o generic constraint
			var value = frame.Pop();
			if (value is T)
				frame.Push(value);
			else
				frame.Push(null);
			return +1;
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return "TypeAs " + typeof(T).Name; }
	}

	/// <summary>�I�u�W�F�N�g�̌^���w�肳�ꂽ�^�Ɠ��������ǂ����𔻒f���閽�߂�\���܂��B</summary>
	sealed class TypeEqualsInstruction : Instruction
	{
		/// <summary>���̖��߂̗B��̃C���X�^���X�������܂��B</summary>
		public static readonly TypeEqualsInstruction Instance = new TypeEqualsInstruction();

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 2; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		TypeEqualsInstruction() { }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			var type = frame.Pop();
			var obj = frame.Pop();
			frame.Push(ScriptingRuntimeHelpers.BooleanToObject(obj != null && (object)obj.GetType() == type));
			return +1;
		}
	}
}
