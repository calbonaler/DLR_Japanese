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
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.Remoting;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>�z�X�e�B���O API �ɂ����錾���\���܂��B<see cref="Microsoft.Scripting.Runtime.LanguageContext"/> �ɑ΂������ 1 �̃z�X�e�B���O API �ł��B</summary>
	[DebuggerDisplay("{Setup.DisplayName}")]
	public sealed class ScriptEngine : MarshalByRefObject
	{
		LanguageSetup _config;
		ObjectOperations _operations;

		/// <summary>
		/// �w�肳�ꂽ�����^�C������� <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> ���g�p���āA
		/// <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> �N���X�̐V�����C���X�^���X�����������܂��B
		/// </summary>
		/// <param name="runtime">���̃G���W���Ɋ֘A�t���郉���^�C�����w�肵�܂��B</param>
		/// <param name="context">���̃G���W���̊�ɂȂ� <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> ���w�肵�܂��B</param>
		internal ScriptEngine(ScriptRuntime runtime, LanguageContext context)
		{
			Debug.Assert(runtime != null);
			Debug.Assert(context != null);
			Runtime = runtime;
			LanguageContext = context;
		}

		#region Object Operations

		/// <summary>�G���W���ɑ΂������� <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> �I�u�W�F�N�g���擾���܂��B</summary>
		/// <remarks>
		/// <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> �I�u�W�F�N�g�̓I�u�W�F�N�g�̌^�ɑ΂���K���⏈������������L���b�V�����邽�߁A
		/// ����� <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> �ɑ΂��镡���̃I�u�W�F�N�g�̎g�p�̓L���b�V��������ቺ�����܂��B
		/// �₪�āA�������̑���ɑ΂���L���b�V���� <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> �̓L���b�V�����~������܂łɐ��\��ቺ�����A
		/// �w�肳�ꂽ�I�u�W�F�N�g�ɑ΂���v�����ꂽ����̎�����S�T������悤�ɂȂ�܂��B
		/// 
		/// �V���� <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> �C���X�^���X���쐬������� 1 �̗��R�́A
		/// �C���X�^���X�� <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> �̓���̋@�\���֘A�t����Ƃ������Ƃł��B
		/// ����͌��ꂲ�Ƃ̐U�镑���𑀍삪�ǂ̂悤�Ɏ��s�����̂���ύX�ł��� <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> �ɈϏ����邱�Ƃ��ł��܂��B
		/// 
		/// �P���ȃz�X�e�B���O�ɂ����ẮA����͏\���ȐU�镑���ƂȂ�܂��B
		/// </remarks>
		public ObjectOperations Operations
		{
			get
			{
				if (_operations == null)
					Interlocked.CompareExchange(ref _operations, CreateOperations(), null);
				return _operations;
			}
		}

		/// <summary>�V���� <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		public ObjectOperations CreateOperations() { return new ObjectOperations(new DynamicOperations(LanguageContext), this); }

		/// <summary>
		/// �w�肳�ꂽ <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> �ɓ��L�̂�����Z�}���e�B�N�X���p������
		/// �V���� <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> �I�u�W�F�N�g���쐬���܂��B
		/// </summary>
		/// <param name="scope">�쐬���� <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> �̊�ƂȂ� <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> �I�u�W�F�N�g���w�肵�܂��B</param>
		public ObjectOperations CreateOperations(ScriptScope scope)
		{
			ContractUtils.RequiresNotNull(scope, "scope");
			return new ObjectOperations(LanguageContext.Operations, this);
		}

		#endregion

		#region Code Execution (for convenience)

		/// <summary>�������s���܂��B���s�͓��ɂǂ̃X�R�[�v�ɂ��֘A�t�����܂���B</summary>
		/// <param name="expression">���s���鎮���w�肵�܂��B</param>
		/// <exception cref="NotSupportedException">�G���W���̓R�[�h�̎��s���T�|�[�g���Ă��܂���B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="expression"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public dynamic Execute(string expression)
		{
			// �z�X�g�̓X�R�[�v��K�v�Ƃ��Ă��Ȃ��̂ŁA�����ł͍쐬���Ȃ��B
			// ����̓R�[�h���ǂ� DLR �X�R�[�v�ɂ��֘A�t�����Ă��Ȃ��Ƃ��Ĉ������߁A�K�X�O���[�o�������̃Z�}���e�B�N�X��ύX���܂��B
			return CreateScriptSourceFromString(expression).Execute();
		}

		/// <summary>�w�肳�ꂽ�X�R�[�v�Ŏ������s���܂��B</summary>
		/// <param name="expression">���s���鎮���w�肵�܂��B</param>
		/// <param name="scope">�������s����X�R�[�v���w�肵�܂��B</param>
		/// <exception cref="NotSupportedException">�G���W���̓R�[�h�̎��s���T�|�[�g���Ă��܂���B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="expression"/> �� <c>null</c> �Q�Ƃł��B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="scope"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public dynamic Execute(string expression, ScriptScope scope) { return CreateScriptSourceFromString(expression).Execute(scope); }

		/// <summary>����V�����X�R�[�v�Ŏ��s���A���ʂ��w�肳�ꂽ�^�ɕϊ����܂��B</summary>
		/// <param name="expression">���s���鎮���w�肵�܂��B</param>
		/// <exception cref="NotSupportedException">�G���W���̓R�[�h�̎��s���T�|�[�g���Ă��܂���B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="expression"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public T Execute<T>(string expression) { return Operations.ConvertTo<T>((object)Execute(expression)); }

		/// <summary>�����w�肳�ꂽ�X�R�[�v�Ŏ��s���A���ʂ��w�肳�ꂽ�^�ɕϊ����܂��B</summary>
		/// <param name="expression">���s���鎮���w�肵�܂��B</param>
		/// <param name="scope">�������s����X�R�[�v���w�肵�܂��B</param>
		/// <exception cref="NotSupportedException">�G���W���̓R�[�h�̎��s���T�|�[�g���Ă��܂���B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="expression"/> �� <c>null</c> �Q�Ƃł��B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="scope"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public T Execute<T>(string expression, ScriptScope scope) { return Operations.ConvertTo<T>((object)Execute(expression, scope)); }

		/// <summary>�w�肳�ꂽ�t�@�C���̓��e��V�����X�R�[�v�Ŏ��s���A���̃X�R�[�v��Ԃ��܂��B</summary>
		/// <param name="path">���s����t�@�C���̃p�X���w�肵�܂��B</param>
		/// <exception cref="NotSupportedException">�G���W���̓R�[�h�̎��s���T�|�[�g���Ă��܂���B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="path"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public ScriptScope ExecuteFile(string path) { return ExecuteFile(path, CreateScope()); }

		/// <summary>�w�肳�ꂽ�t�@�C���̓��e���w�肳�ꂽ�X�R�[�v�Ŏ��s���܂��B</summary>
		/// <param name="path">���s����t�@�C���̃p�X���w�肵�܂��B</param>
		/// <param name="scope">�t�@�C���̓��e�����s����X�R�[�v���w�肵�܂��B</param>
		/// <exception cref="NotSupportedException">�G���W���̓R�[�h�̎��s���T�|�[�g���Ă��܂���B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="path"/> �� <c>null</c> �Q�Ƃł��B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="scope"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public ScriptScope ExecuteFile(string path, ScriptScope scope)
		{
			CreateScriptSourceFromFile(path).Execute(scope);
			return scope;
		}

		/// <summary>�w�肳�ꂽ�X�R�[�v�Ŏ������s���A���ʂ� <see cref="System.Runtime.Remoting.ObjectHandle"/> �Ń��b�v���ĕԂ��܂��B</summary>
		/// <param name="expression">���s���鎮���w�肵�܂��B</param>
		/// <param name="scope">�������s����X�R�[�v���w�肵�܂��B</param>
		public ObjectHandle ExecuteAndWrap(string expression, ScriptScope scope) { return new ObjectHandle((object)Execute(expression, scope)); }

		/// <summary>��̃X�R�[�v�Ŏ������s���A���ʂ� <see cref="System.Runtime.Remoting.ObjectHandle"/> �Ń��b�v���ĕԂ��܂��B</summary>
		/// <param name="expression">���s���鎮���w�肵�܂��B</param>
		public ObjectHandle ExecuteAndWrap(string expression) { return new ObjectHandle((object)Execute(expression)); }

		/// <summary>
		/// �w�肳�ꂽ�X�R�[�v�Ŏ������s���A���ʂ� <see cref="System.Runtime.Remoting.ObjectHandle"/> �Ń��b�v���ĕԂ��܂��B
		/// ��O�����������ꍇ�͗�O��ߑ����A���� <see cref="System.Runtime.Remoting.ObjectHandle"/> ���񋟂���܂��B
		/// </summary>
		/// <param name="expression">���s���鎮���w�肵�܂��B</param>
		/// <param name="scope">�������s����X�R�[�v���w�肵�܂��B</param>
		/// <param name="exception">����������O�ɑ΂��� <see cref="System.Runtime.Remoting.ObjectHandle"/> ���i�[�����ϐ����w�肵�܂��B</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public ObjectHandle ExecuteAndWrap(string expression, ScriptScope scope, out ObjectHandle exception)
		{
			exception = null;
			try { return new ObjectHandle((object)Execute(expression, scope)); }
			catch (Exception e)
			{
				exception = new ObjectHandle(e);
				return null;
			}
		}

		/// <summary>
		/// ��̃X�R�[�v�Ŏ������s���A���ʂ� <see cref="System.Runtime.Remoting.ObjectHandle"/> �Ń��b�v���ĕԂ��܂��B
		/// ��O�����������ꍇ�͗�O��ߑ����A���� <see cref="System.Runtime.Remoting.ObjectHandle"/> ���񋟂���܂��B
		/// </summary>
		/// <param name="expression">���s���鎮���w�肵�܂��B</param>
		/// <param name="exception">����������O�ɑ΂��� <see cref="System.Runtime.Remoting.ObjectHandle"/> ���i�[�����ϐ����w�肵�܂��B</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public ObjectHandle ExecuteAndWrap(string expression, out ObjectHandle exception)
		{
			exception = null;
			try { return new ObjectHandle((object)Execute(expression)); }
			catch (Exception e)
			{
				exception = new ObjectHandle(e);
				return null;
			}
		}

		#endregion

		#region Scopes

		/// <summary>�V������� <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> ���쐬���܂��B</summary>
		public ScriptScope CreateScope() { return new ScriptScope(this, new Scope()); }

		[Obsolete("IAttributesCollection is obsolete, use CreateScope(IDynamicMetaObjectProvider) instead")]
		public ScriptScope CreateScope(IAttributesCollection dictionary)
		{
			ContractUtils.RequiresNotNull(dictionary, "dictionary");
			return new ScriptScope(this, new Scope(dictionary));
		}

		/// <summary>
		/// �X�g���[�W�Ƃ��ĔC�ӂ̃I�u�W�F�N�g��p����V���� <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> ���쐬���܂��B
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> �ɑ΂���A�N�Z�X�̓I�u�W�F�N�g�ɑ΂��郁���o�̎擾�A�ݒ�A�폜�ɂȂ�܂��B
		/// </summary>
		/// <param name="storage"><see cref="Microsoft.Scripting.Hosting.ScriptScope"/> �̃X�g���[�W�ƂȂ�I�u�W�F�N�g���w�肵�܂��B</param>
		public ScriptScope CreateScope(IDynamicMetaObjectProvider storage)
		{
			ContractUtils.RequiresNotNull(storage, "storage");
			return new ScriptScope(this, new Scope(storage));
		}

		/// <summary>
		/// �w�肳�ꂽ�p�X�ɑ΂��� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �����s���ꂽ
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> ���擾���܂��B
		/// </summary>
		/// <remarks>
		/// <see cref="Microsoft.Scripting.Hosting.ScriptSource.Path"/> �v���p�e�B��
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> �̌����̃L�[�ƂȂ�܂��B
		/// �z�X�g�� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> ���쐬��
		/// <see cref="Microsoft.Scripting.Hosting.ScriptSource.Path"/> �v���p�e�B��K�؂ɐݒ肷�邱�Ƃ��m�F����K�v������܂��B
		/// 
		/// <see cref="GetScope"/> �̓t�@�C���Ƃ��̎��s�X�R�[�v���}�b�s���O����K�v������悤�ȃc�[���ɂƂ��Ĕ��ɖ��ɗ����܂��B
		/// ���Ƃ��΁A�G�f�B�^��C���^�v���^�Ƃ������c�[���̓t�@�C�� Bar ���C���|�[�g������K�v�Ƃ����肵�Ă���t�@�C�� Foo �����s����\��������܂��B
		/// 
		/// �G�f�B�^�̃��[�U�[�͌�Ƀt�@�C�� Bar ���J���A���̃R���e�L�X�g���ɂ��鎮�����s�������Ǝv����������܂���B
		/// �c�[���� Bar �̃C���^�v���^�E�B���h�E���̓K�؂ȃR���e�L�X�g��ݒ肷�邱�ƂŁA
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> ��������K�v������ł��傤�B
		/// ���̃��\�b�h�͂��̂悤�ȃV�i���I�ɑ΂��ėL���ƂȂ�܂��B
		/// </remarks>
		public ScriptScope GetScope(string path)
		{
			ContractUtils.RequiresNotNull(path, "path");
			Scope scope = LanguageContext.GetScope(path);
			return scope != null ? new ScriptScope(this, scope) : null;
		}

		#endregion

		#region Source Unit Creation

		/// <summary>����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA�����񂩂� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <param name="expression">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ镶������w�肵�܂��B</param>
		public ScriptSource CreateScriptSourceFromString(string expression)
		{
			ContractUtils.RequiresNotNull(expression, "expression");
			return CreateScriptSource(new SourceStringContentProvider(expression), null, SourceCodeKind.AutoDetect);
		}

		/// <summary>����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA�����񂩂� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <param name="code">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ镶������w�肵�܂��B</param>
		/// <param name="kind">�\�[�X�R�[�h�̎�ނ����� <see cref="Microsoft.Scripting.SourceCodeKind"/> ���w�肵�܂��B</param>
		public ScriptSource CreateScriptSourceFromString(string code, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(code, "code");
			ContractUtils.Requires(kind.IsValid(), "kind");
			return CreateScriptSource(new SourceStringContentProvider(code), null, kind);
		}

		/// <summary>����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA�����񂩂� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <param name="expression">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ镶������w�肵�܂��B</param>
		/// <param name="path">�\�[�X�R�[�h�̃p�X���w�肵�܂��B</param>
		public ScriptSource CreateScriptSourceFromString(string expression, string path)
		{
			ContractUtils.RequiresNotNull(expression, "expression");
			return CreateScriptSource(new SourceStringContentProvider(expression), path, SourceCodeKind.AutoDetect);
		}

		/// <summary>����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA�����񂩂� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <param name="code">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ镶������w�肵�܂��B</param>
		/// <param name="path">�\�[�X�R�[�h�̃p�X���w�肵�܂��B</param>
		/// <param name="kind">�\�[�X�R�[�h�̎�ނ����� <see cref="Microsoft.Scripting.SourceCodeKind"/> ���w�肵�܂��B</param>
		public ScriptSource CreateScriptSourceFromString(string code, string path, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(code, "code");
			ContractUtils.Requires(kind.IsValid(), "kind");
			return CreateScriptSource(new SourceStringContentProvider(code), path, kind);
		}

		/// <summary>����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA�t�@�C������ <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <param name="path"><see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ�t�@�C���������p�X���w�肵�܂��B</param>
		/// <remarks>
		/// �p�X�̊g���q�� <see cref="Microsoft.Scripting.Hosting.ScriptRuntime.GetEngineByFileExtension"/> �ł��̌���G���W���Ɋ֘A�t�����Ă���K�v�͂���܂���B
		/// </remarks>
		public ScriptSource CreateScriptSourceFromFile(string path) { return CreateScriptSourceFromFile(path, Encoding.Default, SourceCodeKind.File); }

		/// <summary>����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA�t�@�C������ <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <param name="path"><see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ�t�@�C���������p�X���w�肵�܂��B</param>
		/// <param name="encoding">�\�[�X�R�[�h�̃G���R�[�f�B���O���w�肵�܂��B</param>
		/// <remarks>
		/// �p�X�̊g���q�� <see cref="Microsoft.Scripting.Hosting.ScriptRuntime.GetEngineByFileExtension"/> �ł��̌���G���W���Ɋ֘A�t�����Ă���K�v�͂���܂���B
		/// </remarks>
		public ScriptSource CreateScriptSourceFromFile(string path, Encoding encoding) { return CreateScriptSourceFromFile(path, encoding, SourceCodeKind.File); }

		/// <summary>����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA�t�@�C������ <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <param name="path"><see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ�t�@�C���������p�X���w�肵�܂��B</param>
		/// <param name="encoding">�\�[�X�R�[�h�̃G���R�[�f�B���O���w�肵�܂��B</param>
		/// <param name="kind">�\�[�X�R�[�h�̎�ނ����� <see cref="Microsoft.Scripting.SourceCodeKind"/> ���w�肵�܂��B</param>
		/// <remarks>
		/// �p�X�̊g���q�� <see cref="Microsoft.Scripting.Hosting.ScriptRuntime.GetEngineByFileExtension"/> �ł��̌���G���W���Ɋ֘A�t�����Ă���K�v�͂���܂���B
		/// </remarks>
		public ScriptSource CreateScriptSourceFromFile(string path, Encoding encoding, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(path, "path");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			ContractUtils.Requires(kind.IsValid(), "kind");
			if (!LanguageContext.CanCreateSourceCode)
				throw new NotSupportedException("Invariant engine cannot create scripts");
			return new ScriptSource(this, LanguageContext.CreateFileUnit(path, encoding, kind));
		}

		/// <summary>
		/// ����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA<see cref="System.CodeDom.CodeObject"/> ����
		/// <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B
		/// </summary>
		/// <param name="content">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ� <see cref="System.CodeDom.CodeObject"/> ���w�肵�܂��B</param>
		/// <remarks>
		/// ���̃��\�b�h�͍\���Ɨ��̃Z�}���e�B�N�X���ɑ΂��čŏ����� CodeDom �T�|�[�g�����s���܂���B
		/// ����͂�葽���̂��Ƃ��s���܂����A�z�X�g�� <see cref="System.CodeDom.CodeMemberMethod"/> ����сA���L�̃T�u�m�[�h�݂̂�F�߂܂��B
		///     <see cref="System.CodeDom.CodeSnippetStatement"/>
		///     <see cref="System.CodeDom.CodeSnippetExpression"/>
		///     <see cref="System.CodeDom.CodePrimitiveExpression"/>
		///     <see cref="System.CodeDom.CodeMethodInvokeExpression"/>
		///     <see cref="System.CodeDom.CodeExpressionStatement"/> (MethodInvoke �̕ێ��̂���)
		/// </remarks>
		public ScriptSource CreateScriptSource(CodeObject content) { return CreateScriptSource(content, null, SourceCodeKind.File); }

		/// <summary>
		/// ����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA<see cref="System.CodeDom.CodeObject"/> ����
		/// <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B
		/// </summary>
		/// <param name="content">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ� <see cref="System.CodeDom.CodeObject"/> ���w�肵�܂��B</param>
		/// <param name="path">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �ɑ΂��Đݒ肳���p�X���w�肵�܂��B</param>
		/// <remarks>
		/// ���̃��\�b�h�͍\���Ɨ��̃Z�}���e�B�N�X���ɑ΂��čŏ����� CodeDom �T�|�[�g�����s���܂���B
		/// ����͂�葽���̂��Ƃ��s���܂����A�z�X�g�� <see cref="System.CodeDom.CodeMemberMethod"/> ����сA���L�̃T�u�m�[�h�݂̂�F�߂܂��B
		///     <see cref="System.CodeDom.CodeSnippetStatement"/>
		///     <see cref="System.CodeDom.CodeSnippetExpression"/>
		///     <see cref="System.CodeDom.CodePrimitiveExpression"/>
		///     <see cref="System.CodeDom.CodeMethodInvokeExpression"/>
		///     <see cref="System.CodeDom.CodeExpressionStatement"/> (MethodInvoke �̕ێ��̂���)
		/// </remarks>
		public ScriptSource CreateScriptSource(CodeObject content, string path) { return CreateScriptSource(content, path, SourceCodeKind.File); }

		/// <summary>
		/// ����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA<see cref="System.CodeDom.CodeObject"/> ����
		/// <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B
		/// </summary>
		/// <param name="content">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ� <see cref="System.CodeDom.CodeObject"/> ���w�肵�܂��B</param>
		/// <param name="kind">�\�[�X�R�[�h�̎�ނ����� <see cref="Microsoft.Scripting.SourceCodeKind"/> ���w�肵�܂��B</param>
		/// <remarks>
		/// ���̃��\�b�h�͍\���Ɨ��̃Z�}���e�B�N�X���ɑ΂��čŏ����� CodeDom �T�|�[�g�����s���܂���B
		/// ����͂�葽���̂��Ƃ��s���܂����A�z�X�g�� <see cref="System.CodeDom.CodeMemberMethod"/> ����сA���L�̃T�u�m�[�h�݂̂�F�߂܂��B
		///     <see cref="System.CodeDom.CodeSnippetStatement"/>
		///     <see cref="System.CodeDom.CodeSnippetExpression"/>
		///     <see cref="System.CodeDom.CodePrimitiveExpression"/>
		///     <see cref="System.CodeDom.CodeMethodInvokeExpression"/>
		///     <see cref="System.CodeDom.CodeExpressionStatement"/> (MethodInvoke �̕ێ��̂���)
		/// </remarks>
		public ScriptSource CreateScriptSource(CodeObject content, SourceCodeKind kind) { return CreateScriptSource(content, null, kind); }

		/// <summary>
		/// ����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA<see cref="System.CodeDom.CodeObject"/> ����
		/// <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B
		/// </summary>
		/// <param name="content">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ� <see cref="System.CodeDom.CodeObject"/> ���w�肵�܂��B</param>
		/// <param name="path">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �ɑ΂��Đݒ肳���p�X���w�肵�܂��B</param>
		/// <param name="kind">�\�[�X�R�[�h�̎�ނ����� <see cref="Microsoft.Scripting.SourceCodeKind"/> ���w�肵�܂��B</param>
		/// <remarks>
		/// ���̃��\�b�h�͍\���Ɨ��̃Z�}���e�B�N�X���ɑ΂��čŏ����� CodeDom �T�|�[�g�����s���܂���B
		/// ����͂�葽���̂��Ƃ��s���܂����A�z�X�g�� <see cref="System.CodeDom.CodeMemberMethod"/> ����сA���L�̃T�u�m�[�h�݂̂�F�߂܂��B
		///     <see cref="System.CodeDom.CodeSnippetStatement"/>
		///     <see cref="System.CodeDom.CodeSnippetExpression"/>
		///     <see cref="System.CodeDom.CodePrimitiveExpression"/>
		///     <see cref="System.CodeDom.CodeMethodInvokeExpression"/>
		///     <see cref="System.CodeDom.CodeExpressionStatement"/> (MethodInvoke �̕ێ��̂���)
		/// </remarks>
		public ScriptSource CreateScriptSource(CodeObject content, string path, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(content, "content");
			if (!LanguageContext.CanCreateSourceCode)
				throw new NotSupportedException("Invariant engine cannot create scripts");
			return new ScriptSource(this, LanguageContext.GenerateSourceCode(content, path, kind));
		}

		/// <summary>����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA�X�g���[������ <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <param name="content">
		/// �쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ�X�g���[����ێ����Ă���
		/// <see cref="Microsoft.Scripting.StreamContentProvider"/> ���w�肵�܂��B
		/// </param>
		/// <param name="path">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �ɑ΂��Đݒ肳���p�X���w�肵�܂��B</param>
		public ScriptSource CreateScriptSource(StreamContentProvider content, string path)
		{
			ContractUtils.RequiresNotNull(content, "content");
			return CreateScriptSource(content, path, Encoding.Default, SourceCodeKind.File);
		}

		/// <summary>����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA�X�g���[������ <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <param name="content">
		/// �쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ�X�g���[����ێ����Ă���
		/// <see cref="Microsoft.Scripting.StreamContentProvider"/> ���w�肵�܂��B
		/// </param>
		/// <param name="path">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �ɑ΂��Đݒ肳���p�X���w�肵�܂��B</param>
		/// <param name="encoding">�\�[�X�R�[�h�̃G���R�[�f�B���O���w�肵�܂��B</param>
		public ScriptSource CreateScriptSource(StreamContentProvider content, string path, Encoding encoding)
		{
			ContractUtils.RequiresNotNull(content, "content");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			return CreateScriptSource(content, path, encoding, SourceCodeKind.File);
		}

		/// <summary>����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA�X�g���[������ <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <param name="content">
		/// �쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ�X�g���[����ێ����Ă���
		/// <see cref="Microsoft.Scripting.StreamContentProvider"/> ���w�肵�܂��B
		/// </param>
		/// <param name="path">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �ɑ΂��Đݒ肳���p�X���w�肵�܂��B</param>
		/// <param name="encoding">�\�[�X�R�[�h�̃G���R�[�f�B���O���w�肵�܂��B</param>
		/// <param name="kind">�\�[�X�R�[�h�̎�ނ����� <see cref="Microsoft.Scripting.SourceCodeKind"/> ���w�肵�܂��B</param>
		public ScriptSource CreateScriptSource(StreamContentProvider content, string path, Encoding encoding, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(content, "content");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			ContractUtils.Requires(kind.IsValid(), "kind");
			return CreateScriptSource(new LanguageBoundTextContentProvider(LanguageContext, content, encoding, path), path, kind);
		}

		/// <summary>����o�C���f�B���O�Ƃ��Č��݂̃G���W�����g�p���āA�R���e���c�v���o�C�_���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <param name="contentProvider">
		/// �쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g�̊�ɂȂ� <see cref="Microsoft.Scripting.TextContentProvider"/> ���w�肵�܂��B
		/// </param>
		/// <param name="path">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �ɑ΂��Đݒ肳���p�X���w�肵�܂��B</param>
		/// <param name="kind">�\�[�X�R�[�h�̎�ނ����� <see cref="Microsoft.Scripting.SourceCodeKind"/> ���w�肵�܂��B</param>
		/// <remarks>
		/// ���̃��\�b�h�̓��[�U�[���R���e���c�v���o�C�_�����L�ł���悤�ɂ��邱�ƂŁA
		/// �G�f�B�^�̃e�L�X�g�\���Ƃ������z�X�g�����̃f�[�^�\�������b�v����X�g���[���������ł���悤�ɂ��܂��B
		/// </remarks>
		public ScriptSource CreateScriptSource(TextContentProvider contentProvider, string path, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(contentProvider, "contentProvider");
			ContractUtils.Requires(kind.IsValid(), "kind");
			if (!LanguageContext.CanCreateSourceCode)
				throw new NotSupportedException("Invariant engine cannot create scripts");
			return new ScriptSource(this, LanguageContext.CreateSourceUnit(contentProvider, path, kind));
		}

		#endregion

		/// <summary>����ŗL�̃T�[�r�X��Ԃ��܂��B</summary>
		/// <param name="args">�T�[�r�X�̎擾�Ɏg�p����������w�肵�܂��B</param>
		/// <remarks>
		/// ���ʂɗ��p�\�ȃT�[�r�X�����Ɏ����܂��B
		///     TokenCategorizer
		///         �W���̃\�[�X�R�[�h�g�[�N������񋟂��܂��B
		///     ExceptionOperations
		///         ��O�I�u�W�F�N�g�̃t�H�[�}�b�g����񋟂��܂��B
		///     DocumentationProvider
		///         �������Ԓ��̃I�u�W�F�N�g�ɑ΂���h�L�������g��񋟂��܂��B
		/// </remarks>
		public TService GetService<TService>(params object[] args) where TService : class
		{
			if (typeof(TService) == typeof(TokenCategorizer))
			{
				var service = LanguageContext.GetService<TokenizerService>(Enumerable.Repeat<object>(LanguageContext, 1).Concat(args).ToArray());
				return service != null ? (TService)(object)new TokenCategorizer(service) : null;
			}
			else if (typeof(TService) == typeof(ExceptionOperations))
			{
				var service = LanguageContext.GetService<ExceptionOperations>();
				return service != null ? (TService)(object)service : (TService)(object)new ExceptionOperations(LanguageContext);
			}
			else if (typeof(TService) == typeof(DocumentationOperations))
			{
				var service = LanguageContext.GetService<DocumentationProvider>(args);
				return service != null ? (TService)(object)new DocumentationOperations(service) : null;
			}
			return LanguageContext.GetService<TService>(args);
		}

		#region Misc. engine information

		/// <summary>���̃G���W�����g�p���Ă���ǂݎ���p�̌���I�v�V�������擾���܂��B</summary>
		/// <remarks>
		/// �l�̓����^�C���̏��������ɓǂݎ���p�̌�Ō��肳��܂��B
		/// �\���t�@�C����ݒ肵����A�����I�� <see cref="Microsoft.Scripting.Hosting.ScriptRuntimeSetup"/> ���g�p�����肷�邱�ƂŐݒ��ύX���邱�Ƃ��ł��܂��B
		/// </remarks>
		public LanguageSetup Setup
		{
			get
			{
				if (_config == null)
				{
					// ���[�U�[�̓C���o���A���g�ȃG���W�����擾�ł��Ă͂Ȃ�Ȃ��B
					Debug.Assert(!(LanguageContext is InvariantContext));
					//��v���錾��\��������
					var config = Runtime.Manager.Configuration.GetLanguageConfig(LanguageContext);
					Debug.Assert(config != null);
					return _config = Runtime.Setup.LanguageSetups.FirstOrDefault(x => new AssemblyQualifiedTypeName(x.TypeName) == config.ProviderName);
				}
				return _config;
			}
		}

		/// <summary>�G���W�������s�����R���e�L�X�g�ɑ΂��� <see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> ���擾���܂��B</summary>
		public ScriptRuntime Runtime { get; private set; }

		/// <summary>�G���W���̃o�[�W�������擾���܂��B</summary>
		public Version LanguageVersion { get { return LanguageContext.LanguageVersion; } }

		#endregion

		/// <summary>�R���p�C���R�[�h�̂ǂ̃X�R�[�v�ɂ��֘A�t�����Ă��Ȃ�����ŗL�� <see cref="Microsoft.Scripting.CompilerOptions"/> ���擾���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public CompilerOptions GetCompilerOptions() { return LanguageContext.GetCompilerOptions(); }

		/// <summary>�w�肳�ꂽ�X�R�[�v�Ɋ֘A�t�����Ă��錾��ŗL�� <see cref="Microsoft.Scripting.CompilerOptions"/> ���擾���܂��B</summary>
		/// <param name="scope">�擾���� <see cref="Microsoft.Scripting.CompilerOptions"/> ���֘A�t�����Ă���X�R�[�v���w�肵�܂��B</param>
		public CompilerOptions GetCompilerOptions(ScriptScope scope) { return LanguageContext.GetCompilerOptions(scope.Scope); }

		/// <summary>�X�N���v�g���ʂ̃t�@�C����R�[�h���C���|�[�g�܂��͗v�������Ƃ��ɁA�t�@�C���̃��[�h�ɃG���W���ɂ���Ďg�p����錟���p�X���擾�܂��͐ݒ肵�܂��B</summary>
		public ICollection<string> SearchPaths
		{
			get { return LanguageContext.SearchPaths; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				ContractUtils.RequiresNotNullItems(value, "value");
				LanguageContext.SearchPaths = value;
			}
		}

		#region Internal API Surface

		/// <summary>���̃G���W���̊�ɂȂ� <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> ���擾���܂��B</summary>
		internal LanguageContext LanguageContext { get; private set; }

		/// <summary>�w�肳�ꂽ <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> �ɑ΂���f���Q�[�g�����̃C���X�^���X�ŌĂяo���܂��B</summary>
		/// <typeparam name="T">�����̌^���w�肵�܂��B</typeparam>
		/// <typeparam name="TRet">�߂�l�̌^���w�肵�܂��B</typeparam>
		/// <param name="f">���̃C���X�^���X�ŌĂяo���f���Q�[�g���w�肵�܂��B</param>
		/// <param name="arg">�f���Q�[�g�Ɏw�肷��������w�肵�܂��B</param>
		internal TRet Call<T, TRet>(Func<LanguageContext, T, TRet> f, T arg) { return f(LanguageContext, arg); }

		#endregion

		// TODO: Figure out what is the right lifetime
		/// <summary>�Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��A�L�����ԃT�[�r�X �I�u�W�F�N�g���擾���܂��B</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
