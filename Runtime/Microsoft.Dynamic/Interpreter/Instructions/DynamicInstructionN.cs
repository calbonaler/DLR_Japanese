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
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>�C�ӂ̃f���Q�[�g�^�ɑ΂��铮�I�Ăяo�����s�����߂�\���܂��B</summary>
	sealed partial class DynamicInstructionN : Instruction
	{
		readonly CallInstruction _target;
		readonly object _targetDelegate;
		readonly CallSite _site;
		readonly int _argumentCount;
		readonly bool _isVoid;

		/// <summary>�w�肳�ꂽ�f���Q�[�g�^�ƌĂяo���T�C�g���g�p���āA<see cref="Microsoft.Scripting.Interpreter.DynamicInstructionN"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="delegateType">���I�Ăяo���Ɏg�p����f���Q�[�g�^���w�肵�܂��B</param>
		/// <param name="site">���I�Ăяo���T�C�g���w�肵�܂��B</param>
		public DynamicInstructionN(Type delegateType, CallSite site)
		{
			var methodInfo = delegateType.GetMethod("Invoke");
			var parameters = methodInfo.GetParameters();
			_target = CallInstruction.Create(methodInfo, parameters);
			_site = site;
			_argumentCount = parameters.Length - 1;
			_targetDelegate = site.GetType().GetField("Target").GetValue(site);
		}

		/// <summary>�w�肳�ꂽ�f���Q�[�g�^�A�Ăяo���T�C�g����ђl��Ԃ��Ȃ����ǂ����������l���g�p���āA<see cref="Microsoft.Scripting.Interpreter.DynamicInstructionN"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="delegateType">���I�Ăяo���Ɏg�p����f���Q�[�g�^���w�肵�܂��B</param>
		/// <param name="site">���I�Ăяo���T�C�g���w�肵�܂��B</param>
		/// <param name="isVoid">���̓��I�Ăяo�����l��Ԃ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		public DynamicInstructionN(Type delegateType, CallSite site, bool isVoid) : this(delegateType, site) { _isVoid = isVoid; }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return _isVoid ? 0 : 1; } }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return _argumentCount; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			object[] args = new object[_argumentCount + 1];
			args[0] = _site;
			for (int i = _argumentCount - 1; i >= 0; i--)
				args[i + 1] = frame.Pop();
			var ret = _target.InvokeInstance(_targetDelegate, args);
			if (!_isVoid)
				frame.Push(ret);
			return 1;
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return "DynamicInstructionN(" + _site + ")"; }
	}
}
