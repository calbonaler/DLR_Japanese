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
using System.Linq.Expressions;

namespace Microsoft.Scripting.Actions.Calls
{
	public delegate object OptimizingCallDelegate(object[] args, out bool shouldOptimize);

	/// <summary><see cref="OverloadResolver"/> ���g�p���� 1 �ȏ�̃��\�b�h�ւ̃o�C���f�B���O�̌��ʂ��J�v�Z�������܂��B</summary>
	/// <remarks>
	/// ���[�U�[�͍ŏ��� <see cref="Result"/> �v���p�e�B����o�C���f�B���O�������������A����̃G���[�������������𔻒f����K�v������܂��B
	/// �o�C���f�B���O�����������ꍇ�A<see cref="MakeExpression"/> ���烁�\�b�h���Ăяo�������쐬�ł��܂��B
	/// �o�C���f�B���O�����s�����ꍇ�A�Ăяo�����͎��s�̗��R�Ɋ�Â����J�X�^���G���[���b�Z�[�W���쐬�ł��܂��B
	/// </remarks>
	public sealed class BindingTarget
	{
		/// <summary>���\�b�h�o�C���f�B���O�������������Ƃ����� <see cref="Microsoft.Scripting.Actions.Calls.BindingTarget"/> ���쐬���܂��B</summary>
		/// <param name="name">���\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="actualArgumentCount">���\�b�h�Ɏ��ۂɓn���ꂽ�����̐����w�肵�܂��B</param>
		/// <param name="candidate">�ΏۂƂȂ郁�\�b�h���w�肵�܂��B</param>
		/// <param name="level">���\�b�h�� <see cref="NarrowingLevel"/> ���w�肵�܂��B</param>
		/// <param name="restrictedArgs">�{���o�C���f�B���O�����s���� <see cref="System.Dynamic.DynamicMetaObject"/> ���w�肵�܂��B</param>
		internal BindingTarget(string name, int actualArgumentCount, MethodCandidate candidate, NarrowingLevel level, RestrictedArguments restrictedArgs)
		{
			Name = name;
			MethodCandidate = candidate;
			RestrictedArguments = restrictedArgs;
			NarrowingLevel = level;
			ActualArgumentCount = actualArgumentCount;
		}

		/// <summary>�����̐����������Ȃ����߂Ƀo�C���f�B���O�����s�������Ƃ����� <see cref="Microsoft.Scripting.Actions.Calls.BindingTarget"/> ���쐬���܂��B</summary>
		/// <param name="name">���\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="actualArgumentCount">���\�b�h�Ɏ��ۂɓn���ꂽ�����̐����w�肵�܂��B</param>
		/// <param name="expectedArgCount">���\�b�h���󂯓���\�Ȉ����̐����w�肵�܂��B</param>
		internal BindingTarget(string name, int actualArgumentCount, int[] expectedArgCount)
		{
			Name = name;
			Result = BindingResult.IncorrectArgumentCount;
			ExpectedArgumentCount = expectedArgCount;
			ActualArgumentCount = actualArgumentCount;
		}

		/// <summary>1 �ȏ�̈������ϊ��ł��Ȃ����߂Ƀo�C���f�B���O�����s�������Ƃ����� <see cref="Microsoft.Scripting.Actions.Calls.BindingTarget"/> ���쐬���܂��B</summary>
		/// <param name="name">���\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="actualArgumentCount">���\�b�h�Ɏ��ۂɓn���ꂽ�����̐����w�肵�܂��B</param>
		/// <param name="failures">���\�b�h�Ƃ���Ɋ֘A�t����ꂽ�G���[���w�肵�܂��B</param>
		internal BindingTarget(string name, int actualArgumentCount, CallFailure[] failures)
		{
			Name = name;
			Result = BindingResult.CallFailure;
			CallFailures = failures;
			ActualArgumentCount = actualArgumentCount;
		}

		/// <summary>��v�������܂��ł��邽�߂Ƀo�C���f�B���O�����s�������Ƃ����� <see cref="Microsoft.Scripting.Actions.Calls.BindingTarget"/> ���쐬���܂��B</summary>
		/// <param name="name">���\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="actualArgumentCount">���\�b�h�Ɏ��ۂɓn���ꂽ�����̐����w�肵�܂��B</param>
		/// <param name="ambiguousMatches">��v���������������̃��\�b�h���w�肵�܂��B</param>
		internal BindingTarget(string name, int actualArgumentCount, MethodCandidate[] ambiguousMatches)
		{
			Name = name;
			Result = BindingResult.AmbiguousMatch;
			AmbiguousMatches = ambiguousMatches;
			ActualArgumentCount = actualArgumentCount;
		}

		/// <summary>���̎��s������ <see cref="Microsoft.Scripting.Actions.Calls.BindingTarget"/> ���쐬���܂��B</summary>
		/// <param name="name">���\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="result">���̎��s������ <see cref="BindingResult"/> ���w�肵�܂��B</param>
		internal BindingTarget(string name, BindingResult result)
		{
			Name = name;
			Result = result;
		}

		/// <summary>�o�C���f�B���O�̌��ʂ��擾���܂��B</summary>
		public BindingResult Result { get; private set; }

		/// <summary>�o�C���f�B���O�^�[�Q�b�g���Ăяo�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <returns>�o�C���f�B���O�^�[�Q�b�g���Ăяo�� <see cref="Expression"/>�B</returns>
		/// <exception cref="System.InvalidOperationException">�o�C���f�B���O�����s���Ă��܂��B�܂��́A<see cref="System.Dynamic.DynamicMetaObject"/> �ɑ΂���o�C���f�B���O���������Ă��܂���B</exception>
		public Expression MakeExpression()
		{
			if (MethodCandidate == null)
				throw new InvalidOperationException("An expression cannot be produced because the method binding was unsuccessful.");
			if (RestrictedArguments == null)
				throw new InvalidOperationException("An expression cannot be produced because the method binding was done with Expressions, not MetaObject's");
			return MethodCandidate.MakeExpression(RestrictedArguments);
		}

		/// <summary>�o�C���f�B���O�^�[�Q�b�g���Ăяo���f���Q�[�g���쐬���܂��B</summary>
		/// <returns>�o�C���f�B���O�^�[�Q�b�g���Ăяo���f���Q�[�g�B</returns>
		/// <exception cref="System.InvalidOperationException">�o�C���f�B���O�����s���Ă��܂��B�܂��́A<see cref="System.Dynamic.DynamicMetaObject"/> �ɑ΂���o�C���f�B���O���������Ă��܂���B</exception>
		public OptimizingCallDelegate MakeDelegate()
		{
			if (MethodCandidate == null)
				throw new InvalidOperationException("An expression cannot be produced because the method binding was unsuccessful.");
			if (RestrictedArguments == null)
				throw new InvalidOperationException("An expression cannot be produced because the method binding was done with Expressions, not MetaObject's");
			return MethodCandidate.MakeDelegate(RestrictedArguments);
		}

		/// <summary>�o�C���f�B���O�����������ꍇ�́A�I�����ꂽ�I�[�o�[���[�h���擾���܂��B���s�����ꍇ�́A<c>null</c> ��Ԃ��܂��B</summary>
		public OverloadInfo Overload { get { return MethodCandidate != null ? MethodCandidate.Overload : null; } }

		/// <summary><see cref="OverloadResolver"/> �ɒ񋟂���郁�\�b�h�̖��O���w�肵�܂��B</summary>
		public string Name { get; private set; }

		/// <summary>�o�C���f�B���O�����������ꍇ�́A�ΏۂƂȂ郁�\�b�h���擾���܂��B���s�����ꍇ�́A<c>null</c> ��Ԃ��܂��B</summary>
		public MethodCandidate MethodCandidate { get; private set; }

		/// <summary><see cref="Result"/> �� <see cref="BindingResult.AmbiguousMatch"/> �̏ꍇ�ɁA��v���������������̃��\�b�h���擾���܂��B</summary>
		public IEnumerable<MethodCandidate> AmbiguousMatches { get; private set; }

		/// <summary><see cref="Result"/> �� <see cref="BindingResult.CallFailure"/> �̏ꍇ�ɁA���\�b�h�Ƃ���Ɋ֘A�t����ꂽ�ϊ��G���[���擾���܂��B</summary>
		public ICollection<CallFailure> CallFailures { get; private set; }

		/// <summary><see cref="Result"/> �� <see cref="BindingResult.IncorrectArgumentCount"/> �̏ꍇ�ɁA���\�b�h���󂯓���\�Ȉ����̐����擾���܂��B</summary>
		public IList<int> ExpectedArgumentCount { get; private set; }

		/// <summary>���\�b�h�Ɏ��ۂɓn���ꂽ�����̑������擾���܂��B</summary>
		public int ActualArgumentCount { get; private set; }

		/// <summary>
		/// �{���o�C���f�B���O�����s���� <see cref="System.Dynamic.DynamicMetaObject"/> �𐧖񂳂ꂽ��ԂŕԂ��܂��B
		/// �z��̃����o�͂��ꂼ��̈����ɑΉ����Ă��܂��B���ׂẴ����o�ɂ͒l�����݂��܂��B
		/// </summary>
		public RestrictedArguments RestrictedArguments { get; private set; }

		/// <summary>�o�C���f�B���O�̌��ʂ̌^���擾���܂��B�ǂ̃��\�b�h���K�p�ł��Ȃ��ꍇ�� <c>null</c> ��Ԃ��܂��B</summary>
		public Type ReturnType { get { return MethodCandidate != null ? MethodCandidate.ReturnType : null; } }

		/// <summary>�Ăяo�������������ꍇ�́A���\�b�h�� <see cref="NarrowingLevel"/> ���擾���܂��B���s�����ꍇ�� <see cref="Microsoft.Scripting.Actions.Calls.NarrowingLevel.None"/> ���Ԃ���܂��B</summary>
		public NarrowingLevel NarrowingLevel { get; private set; }

		/// <summary>�o�C���f�B���O�������������ǂ����������l���擾���܂��B</summary>
		public bool Success { get { return Result == BindingResult.Success; } }
	}
}
