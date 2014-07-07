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

using System.Diagnostics;
using System.Reflection;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>�ÓI�t�B�[���h�̒l���X�^�b�N�ɓǂݍ��ޖ��߂�\���܂��B</summary>
	sealed class LoadStaticFieldInstruction : Instruction
	{
		readonly FieldInfo _field;

		/// <summary>�l��ǂݍ��ސÓI�t�B�[���h���g�p���āA<see cref="Microsoft.Scripting.Interpreter.LoadStaticFieldInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="field">�l��ǂݍ��ސÓI�t�B�[���h��\�� <see cref="FieldInfo"/> ���w�肵�܂��B</param>
		public LoadStaticFieldInstruction(FieldInfo field)
		{
			Debug.Assert(field.IsStatic);
			_field = field;
		}

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(_field.GetValue(null));
			return +1;
		}
	}

	/// <summary>�C���X�^���X�t�B�[���h�̒l���X�^�b�N�ɓǂݍ��ޖ��߂�\���܂��B</summary>
	sealed class LoadFieldInstruction : Instruction
	{
		readonly FieldInfo _field;

		/// <summary>�l��ǂݍ��ރC���X�^���X�t�B�[���h���g�p���āA<see cref="Microsoft.Scripting.Interpreter.LoadFieldInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="field">�l��ǂݍ��ރC���X�^���X�t�B�[���h��\�� <see cref="FieldInfo"/> ���w�肵�܂��B</param>
		public LoadFieldInstruction(FieldInfo field)
		{
			Assert.NotNull(field);
			_field = field;
		}

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(_field.GetValue(frame.Pop()));
			return +1;
		}
	}

	/// <summary>�C���X�^���X�t�B�[���h�̒l��ݒ肷�閽�߂�\���܂��B</summary>
	sealed class StoreFieldInstruction : Instruction
	{
		readonly FieldInfo _field;

		/// <summary>�l��ݒ肷��C���X�^���X�t�B�[���h���g�p���āA<see cref="Microsoft.Scripting.Interpreter.StoreFieldInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="field">�l��ݒ肷��C���X�^���X�t�B�[���h��\�� <see cref="FieldInfo"/> ���w�肵�܂��B</param>
		public StoreFieldInstruction(FieldInfo field)
		{
			Assert.NotNull(field);
			_field = field;
		}

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 2; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 0; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			var value = frame.Pop();
			var self = frame.Pop();
			_field.SetValue(self, value);
			return +1;
		}
	}

	/// <summary>�ÓI�t�B�[���h�̒l��ݒ肷�閽�߂�\���܂��B</summary>
	sealed class StoreStaticFieldInstruction : Instruction
	{
		readonly FieldInfo _field;

		/// <summary>�l��ݒ肷��ÓI�t�B�[���h���g�p���āA<see cref="Microsoft.Scripting.Interpreter.StoreStaticFieldInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="field">�l��ݒ肷��ÓI�t�B�[���h��\�� <see cref="FieldInfo"/> ���w�肵�܂��B</param>
		public StoreStaticFieldInstruction(FieldInfo field)
		{
			Assert.NotNull(field);
			_field = field;
		}

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 0; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			_field.SetValue(null, frame.Pop());
			return +1;
		}
	}
}
