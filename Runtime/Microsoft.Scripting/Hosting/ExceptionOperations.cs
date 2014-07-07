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
using System.Runtime.Remoting;
using System.Security.Permissions;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>������Ŕ���������O�Ɋւ��鑀���񋟂��܂��B</summary>
	public sealed class ExceptionOperations : MarshalByRefObject
	{
		readonly LanguageContext _context;

		/// <summary>
		/// ����Ɋւ������\�� <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> ���g�p���āA
		/// <see cref="Microsoft.Scripting.Hosting.ExceptionOperations"/> �N���X�̐V�����C���X�^���X�����������܂��B
		/// </summary>
		/// <param name="context">����Ɋւ������\�� <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> ���w�肵�܂��B</param>
		internal ExceptionOperations(LanguageContext context) { _context = context; }

		/// <summary>�w�肳�ꂽ��O��\����������擾���܂��B</summary>
		/// <param name="exception">��������擾�����O���w�肵�܂��B</param>
		public string FormatException(Exception exception) { return _context.FormatException(exception); }

		/// <summary>�w�肳�ꂽ��O�ɑ΂��郁�b�Z�[�W����ї�O�̌^���擾���܂��B</summary>
		/// <param name="exception">���b�Z�[�W����ї�O�̌^���擾�����O���w�肵�܂��B</param>
		/// <param name="message">�擾���郁�b�Z�[�W���i�[����ϐ����w�肵�܂��B</param>
		/// <param name="errorTypeName">�擾�����O�̌^���i�[����ϐ����w�肵�܂��B</param>
		public void GetExceptionMessage(Exception exception, out string message, out string errorTypeName) { _context.GetExceptionMessage(exception, out message, out errorTypeName); }

		/// <summary>�w�肳�ꂽ��O���n���h�����A�n���h���ɐ����������ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="exception">�n���h�������O���w�肵�܂��B</param>
		public bool HandleException(Exception exception)
		{
			ContractUtils.RequiresNotNull(exception, "exception");
			return false;
		}

		/// <summary>��O�ɑ΂���X�^�b�N�t���[����Ԃ��܂��B</summary>
		/// <param name="exception">�X�^�b�N�t���[�����擾�����O���w�肵�܂��B</param>
		public IList<DynamicStackFrame> GetStackFrames(Exception exception)
		{
			ContractUtils.RequiresNotNull(exception, "exception");
			return _context.GetStackFrames(exception);
		}

		/// <summary>�w�肳�ꂽ��O��\����������擾���܂��B</summary>
		/// <param name="exception">��������擾�����O�����b�v���Ă��� <see cref="System.Runtime.Remoting.ObjectHandle"/> ���w�肵�܂��B</param>
		public string FormatException(ObjectHandle exception)
		{
			ContractUtils.RequiresNotNull(exception, "exception");
			var exceptionObj = exception.Unwrap() as Exception;
			ContractUtils.Requires(exceptionObj != null, "exception", "ObjectHandle must be to Exception object");
			return _context.FormatException(exceptionObj);
		}

		/// <summary>�w�肳�ꂽ��O�ɑ΂��郁�b�Z�[�W����ї�O�̌^���擾���܂��B</summary>
		/// <param name="exception">���b�Z�[�W����ї�O�̌^���擾�����O�����b�v���Ă��� <see cref="System.Runtime.Remoting.ObjectHandle"/> ���w�肵�܂��B</param>
		/// <param name="message">�擾���郁�b�Z�[�W���i�[����ϐ����w�肵�܂��B</param>
		/// <param name="errorTypeName">�擾�����O�̌^���i�[����ϐ����w�肵�܂��B</param>
		public void GetExceptionMessage(ObjectHandle exception, out string message, out string errorTypeName)
		{
			ContractUtils.RequiresNotNull(exception, "exception");
			var exceptionObj = exception.Unwrap() as Exception;
			ContractUtils.Requires(exceptionObj != null, "exception", "ObjectHandle must be to Exception object");
			_context.GetExceptionMessage(exceptionObj, out message, out errorTypeName);
		}

		/// <summary>�w�肳�ꂽ��O���n���h�����A�n���h���ɐ����������ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="exception">�n���h�������O�����b�v���Ă��� <see cref="System.Runtime.Remoting.ObjectHandle"/> ���w�肵�܂��B</param>
		public bool HandleException(ObjectHandle exception)
		{
			ContractUtils.RequiresNotNull(exception, "exception");
			var exceptionObj = exception.Unwrap() as Exception;
			ContractUtils.Requires(exceptionObj != null, "exception", "ObjectHandle must be to Exception object");
			return false;
		}

		/// <summary>��O�ɑ΂���X�^�b�N�t���[����Ԃ��܂��B</summary>
		/// <param name="exception">�X�^�b�N�t���[�����擾�����O�����b�v���Ă��� <see cref="System.Runtime.Remoting.ObjectHandle"/> ���w�肵�܂��B</param>
		public IList<DynamicStackFrame> GetStackFrames(ObjectHandle exception)
		{
			ContractUtils.RequiresNotNull(exception, "exception");
			var exceptionObj = exception.Unwrap() as Exception;
			ContractUtils.Requires(exceptionObj != null, "exception", "ObjectHandle must be to Exception object");
			return _context.GetStackFrames(exceptionObj);
		}

		/// <summary>�Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��A�L�����ԃT�[�r�X �I�u�W�F�N�g���擾���܂��B</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
