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
using System.Runtime.Remoting;
using System.Security.Permissions;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary><see cref="Microsoft.Scripting.ScriptCode"/> �ɑ΂������ 1 �̃z�X�e�B���O API ��\���܂��B</summary>
	public sealed class CompiledCode : MarshalByRefObject
	{
		/// <summary>
		/// �R�[�h���R���p�C������ <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ����сA���ۂ̃R�[�h��\�� <see cref="Microsoft.Scripting.ScriptCode"/> ���g�p���āA
		/// <see cref="Microsoft.Scripting.Hosting.CompiledCode"/> �N���X�̐V�����C���X�^���X�����������܂��B
		/// </summary>
		/// <param name="engine">�R�[�h���R���p�C������ <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���w�肵�܂��B</param>
		/// <param name="code">���ۂ̃R�[�h��\�� <see cref="Microsoft.Scripting.ScriptCode"/> ���w�肵�܂��B</param>
		internal CompiledCode(ScriptEngine engine, ScriptCode code)
		{
			Assert.NotNull(engine);
			Assert.NotNull(code);
			Engine = engine;
			ScriptCode = code;
		}
		
		ScriptScope _defaultScope;

		/// <summary>���̃R�[�h���R���p�C������ <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���擾���܂��B</summary>
		public ScriptEngine Engine { get; private set; }
		
		/// <summary>���ۂ̃R�[�h��\�� <see cref="Microsoft.Scripting.ScriptCode"/> ���擾���܂��B</summary>
		internal ScriptCode ScriptCode { get; private set; }

		/// <summary>���̃R�[�h�̊���̃X�R�[�v���擾���܂��B</summary>
		public ScriptScope DefaultScope
		{
			get
			{
				if (_defaultScope == null)
					Interlocked.CompareExchange(ref _defaultScope, new ScriptScope(Engine, ScriptCode.CreateScope()), null);
				return _defaultScope;
			}
		}

		/// <summary>�R�[�h������̃X�R�[�v�Ŏ��s���܂��B </summary>
		public dynamic Execute() { return ScriptCode.Run(DefaultScope.Scope); }

		/// <summary>�w�肳�ꂽ�X�R�[�v�ŃR�[�h�����s���A���ʂ�Ԃ��܂��B</summary>
		/// <param name="scope">�R�[�h�����s����X�R�[�v���w�肵�܂��B</param>
		public dynamic Execute(ScriptScope scope)
		{
			ContractUtils.RequiresNotNull(scope, "scope");
			return ScriptCode.Run(scope.Scope);
		}

		/// <summary>����̃X�R�[�v�ŃR�[�h�����s���A���ʂ��w�肳�ꂽ�^�ɕϊ����܂��B</summary>
		public T Execute<T>() { return Engine.Operations.ConvertTo<T>((object)Execute()); }

		/// <summary>�w�肳�ꂽ�X�R�[�v�ŃR�[�h�����s���A���ʂ��w�肳�ꂽ�^�ɕϊ����܂��B</summary>
		/// <param name="scope">�R�[�h�����s����X�R�[�v���w�肵�܂��B</param>
		public T Execute<T>(ScriptScope scope) { return Engine.Operations.ConvertTo<T>((object)Execute(scope)); }

		/// <summary>����̃X�R�[�v�ŃR�[�h�����s���A���ʂ� <see cref="System.Runtime.Remoting.ObjectHandle"/> ��p���ă��b�v���܂��B</summary>
		public ObjectHandle ExecuteAndWrap() { return new ObjectHandle((object)Execute()); }

		/// <summary>�w�肳�ꂽ�X�R�[�v�ŃR�[�h�����s���A���ʂ� <see cref="System.Runtime.Remoting.ObjectHandle"/> ��p���ă��b�v���܂��B</summary>
		/// <param name="scope">�R�[�h�����s����X�R�[�v���w�肵�܂��B</param>
		public ObjectHandle ExecuteAndWrap(ScriptScope scope) { return new ObjectHandle((object)Execute(scope)); }

		/// <summary>����̃X�R�[�v�ŃR�[�h�����s���A���ʂ���є���������O�� <see cref="System.Runtime.Remoting.ObjectHandle"/> ��p���ă��b�v���܂��B</summary>
		/// <param name="exception">����������O�����b�v���ꂽ <see cref="System.Runtime.Remoting.ObjectHandle"/> ���i�[����ϐ����w�肵�܂��B</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public ObjectHandle ExecuteAndWrap(out ObjectHandle exception)
		{
			exception = null;
			try { return new ObjectHandle((object)Execute()); }
			catch (Exception e)
			{
				exception = new ObjectHandle(e);
				return null;
			}
		}

		/// <summary>�w�肳�ꂽ�X�R�[�v�ŃR�[�h�����s���A���ʂ���є���������O�� <see cref="System.Runtime.Remoting.ObjectHandle"/> ��p���ă��b�v���܂��B</summary>
		/// <param name="scope">�R�[�h�����s����X�R�[�v���w�肵�܂��B</param>
		/// <param name="exception">����������O�����b�v���ꂽ <see cref="System.Runtime.Remoting.ObjectHandle"/> ���i�[����ϐ����w�肵�܂��B</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public ObjectHandle ExecuteAndWrap(ScriptScope scope, out ObjectHandle exception)
		{
			exception = null;
			try { return new ObjectHandle((object)Execute(scope)); }
			catch (Exception e)
			{
				exception = new ObjectHandle(e);
				return null;
			}
		}

		/// <summary>�Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��A�L�����ԃT�[�r�X �I�u�W�F�N�g���擾���܂��B</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; } // TODO: Figure out what is the right lifetime
	}
}
