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
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>�����̈������g�p���铮�I�Ăяo�����������܂��B������ <see cref="ArgumentArray"/> �Ń��b�v����܂��B</summary>
	sealed class DynamicSplatInstruction : Instruction
	{
		readonly CallSite<Func<CallSite, ArgumentArray, object>> _site;
		readonly int _argumentCount;

		/// <summary>�w�肳�ꂽ�����̐��Ɠ��I�Ăяo���T�C�g���g�p���āA<see cref="Microsoft.Scripting.Interpreter.DynamicSplatInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="argumentCount">���I�Ăяo���̈����̐����w�肵�܂��B</param>
		/// <param name="site">���I�Ăяo���Ɏg�p�����Ăяo���T�C�g���w�肵�܂��B</param>
		internal DynamicSplatInstruction(int argumentCount, CallSite<Func<CallSite, ArgumentArray, object>> site)
		{
			_site = site;
			_argumentCount = argumentCount;
		}

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return _argumentCount; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			int first = frame.StackIndex - _argumentCount;
			frame.Data[first] = _site.Target(_site, new ArgumentArray(frame.Data, first, _argumentCount));
			frame.StackIndex = first + 1;
			return 1;
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return "DynamicSplatInstruction(" + _site + ")"; }
	}
}
