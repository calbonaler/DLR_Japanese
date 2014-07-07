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
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>�C���^�v���^�ɂ���ĉ��߂����f���Q�[�g�̍쐬���Ǘ����܂��B�����̃f���Q�[�g�͕p�ɂɎ��s�����ꍇ�ɂ̂݃R���p�C������܂��B</summary>
	sealed class LightDelegateCreator
	{
		// null if we are forced to compile
		readonly LambdaExpression _lambda;

		// Adaptive compilation support:
		Type _compiledDelegateType;
		Delegate _compiled;
		readonly object _compileLock = new object();

		/// <summary>�f���Q�[�g�����߂���C���^�v���^�ƑΏۂ̃����_�����w�肵�āA<see cref="Microsoft.Scripting.Interpreter.LightDelegateCreator"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="interpreter">�쐬�����f���Q�[�g�����߂���C���^�v���^���w�肵�܂��B</param>
		/// <param name="lambda">�쐬�����f���Q�[�g�̑ΏۂƂȂ郉���_�����w�肵�܂��B</param>
		internal LightDelegateCreator(Interpreter interpreter, LambdaExpression lambda)
		{
			Assert.NotNull(lambda);
			Interpreter = interpreter;
			_lambda = lambda;
		}

		/// <summary>�쐬�����f���Q�[�g�����߂���C���^�v���^���擾���܂��B</summary>
		internal Interpreter Interpreter { get; private set; }

		bool HasClosure { get { return Interpreter != null && Interpreter.ClosureSize > 0; } }

		/// <summary>�f���Q�[�g�� JIT �R�[�h�ɃR���p�C�����ꂽ���ǂ����������l���擾���܂��B</summary>
		internal bool HasCompiled { get { return _compiled != null; } }

		/// <summary>�R���p�C�����ꂽ�f���Q�[�g�������_���Ɠ����^�������Ă��邩�ǂ����������l���擾���܂��B<c>false</c> �̏ꍇ�A�^�͉��߂̂��߂ɕύX����Ă��܂��B</summary>
		internal bool SameDelegateType { get { return _compiledDelegateType == _lambda.Type; } }

		/// <summary>���̃����_����ΏۂƂ���f���Q�[�g���쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ�f���Q�[�g�B</returns>
		internal Delegate CreateDelegate() { return CreateDelegate(null); }

		/// <summary>���̃����_����ΏۂƂ���f���Q�[�g���N���[�W���ϐ����w�肵�č쐬���܂��B</summary>
		/// <param name="closure">�ΏۂƂȂ�f���Q�[�g�̎��s���������N���[�W���ϐ����w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ�f���Q�[�g�B</returns>
		internal Delegate CreateDelegate(StrongBox<object>[] closure)
		{
			if (_compiled != null)
			{
				// If the delegate type we want is not a Func/Action, we can't use the compiled code directly.
				// So instead just fall through and create an interpreted LightLambda, which will pick up the compiled delegate on its first run.
				//
				// Ideally, we would just rebind the compiled delegate using Delegate.CreateDelegate.
				// Unfortunately, it doesn't work on dynamic methods.
				if (SameDelegateType)
					return CreateCompiledDelegate(closure);
			}
			if (Interpreter == null)
			{
				// We can't interpret, so force a compile
				Compile(null);
				var compiled = CreateCompiledDelegate(closure);
				Debug.Assert(compiled.GetType() == _lambda.Type);
				return compiled;
			}
			// Otherwise, we'll create an interpreted LightLambda
			return new LightLambda(this, closure, Interpreter._compilationThreshold).MakeDelegate(_lambda.Type);
		}

		/// <summary>���̃����_����ΏۂƂ���R���p�C���ς݂̃f���Q�[�g���N���[�W���ϐ����w�肵�Ď擾���܂��B</summary>
		/// <param name="closure">�ΏۂƂȂ�f���Q�[�g�̎��s���������N���[�W���ϐ����w�肵�܂��B</param>
		/// <returns>�R���p�C���ς݂̃f���Q�[�g�B</returns>
		internal Delegate CreateCompiledDelegate(StrongBox<object>[] closure)
		{
			Debug.Assert(HasClosure == (closure != null));
			if (HasClosure)
				// We need to apply the closure to get the actual delegate.
				return ((Func<StrongBox<object>[], Delegate>)_compiled)(closure);
			return _compiled;
		}

		/// <summary>�y�ʃ����_���ɑ΂���R���p�C���ς݂̃f���Q�[�g���쐬���āA����ȍ~�̌Ăяo���ŃC���^�v���^�����s�������ɃR���p�C�����ꂽ�R�[�h�����s����悤�ɕۑ����܂��B</summary>
		/// <param name="state"><see cref="M:ThreadPool.QueueUserWorkItem(WaitCallback)"/> �ɂ��̃��\�b�h��n�����߂̃_�~�[�����ł��B</param>
		internal void Compile(object state)
		{
			if (_compiled != null)
				return;
			// Compilation is expensive, we only want to do it once.
			lock (_compileLock)
			{
				if (_compiled != null)
					return;
				PerfTrack.NoteEvent(PerfTrack.Category.Compiler, "Interpreted lambda compiled");
				// Interpreter needs a standard delegate type.
				// So change the lambda's delegate type to Func<...> or Action<...> so it can be called from the LightLambda.Run methods.
				var lambda = _lambda;
				if (Interpreter != null)
				{
					_compiledDelegateType = GetFuncOrAction(lambda);
					lambda = Expression.Lambda(_compiledDelegateType, lambda.Body, lambda.Name, lambda.Parameters);
				}
				_compiled = HasClosure ? LightLambdaClosureVisitor.BindLambda(lambda, Interpreter.ClosureVariables) : lambda.Compile();
			}
		}

		static Type GetFuncOrAction(LambdaExpression lambda)
		{
			Type delegateType;
			var isVoid = lambda.ReturnType == typeof(void);
			if (isVoid && lambda.Parameters.Count == 2 && lambda.Parameters[0].IsByRef && lambda.Parameters[1].IsByRef)
				return typeof(ActionRef<,>).MakeGenericType(lambda.Parameters.Select(p => p.Type).ToArray());
			else
			{
				var types = lambda.Parameters.Select(p => p.IsByRef ? p.Type.MakeByRefType() : p.Type).ToArray();
				if (isVoid)
				{
					if (Expression.TryGetActionType(types, out delegateType))
						return delegateType;
				}
				else if (Expression.TryGetFuncType(ArrayUtils.Append(types, lambda.ReturnType), out delegateType))
					return delegateType;
				return lambda.Type;
			}
		}
	}
}
