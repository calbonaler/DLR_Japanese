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
using System.Runtime.Serialization;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Providers
{
	/// <summary>
	/// �z�X�e�B���O API �v���o�C�_�ɑ΂��鍂�x�� API ��񋟂��܂��B�����̃��\�b�h�̓z�X�g����g�p������̂ł͂���܂���B
	/// �����͊����̃z�X�e�B���O API �ɉe�����y�ڂ����茾��ŗL�̋@�\�Ŋg�������肵�����ƍl���鑼�̃z�X�e�B���O API �����҂ɑ΂��Ē񋟂���܂��B
	/// </summary>
	public static class HostingHelpers
	{
		/// <summary>�w�肳�ꂽ <see cref="ScriptRuntime"/> ���� <see cref="ScriptDomainManager"/> ���擾���܂��B</summary>
		/// <param name="runtime"><see cref="ScriptDomainManager"/> ���擾���� <see cref="ScriptRuntime"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="runtime"/> �� <c>null</c> �Q�Ƃł��B</exception>
		/// <exception cref="SerializationException"><paramref name="runtime"/> �̓����[�g�ł��B</exception>
		public static ScriptDomainManager GetDomainManager(ScriptRuntime runtime)
		{
			ContractUtils.RequiresNotNull(runtime, "runtime");
			return runtime.Manager;
		}

		/// <summary>�w�肳�ꂽ <see cref="ScriptEngine"/> ���� <see cref="LanguageContext"/> ���擾���܂��B</summary>
		/// <param name="engine"><see cref="LanguageContext"/> ���擾���� <see cref="ScriptEngine"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="engine"/> �� <c>null</c> �Q�Ƃł��B</exception>
		/// <exception cref="SerializationException"><paramref name="engine"/> �̓����[�g�ł��B</exception>
		public static LanguageContext GetLanguageContext(ScriptEngine engine)
		{
			ContractUtils.RequiresNotNull(engine, "engine");
			return engine.LanguageContext;
		}

		/// <summary>�w�肳�ꂽ <see cref="ScriptSource"/> ���� <see cref="SourceUnit"/> ���擾���܂��B</summary>
		/// <param name="scriptSource"><see cref="SourceUnit"/> ���擾���� <see cref="ScriptSource"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="scriptSource"/> �� <c>null</c> �Q�Ƃł��B</exception>
		/// <exception cref="SerializationException"><paramref name="scriptSource"/> �̓����[�g�ł��B</exception>
		public static SourceUnit GetSourceUnit(ScriptSource scriptSource)
		{
			ContractUtils.RequiresNotNull(scriptSource, "scriptSource");
			return scriptSource.SourceUnit;
		}

		/// <summary>�w�肳�ꂽ <see cref="CompiledCode"/> ���� <see cref="ScriptCode"/> ���擾���܂��B</summary>
		/// <param name="compiledCode"><see cref="ScriptCode"/> ���擾���� <see cref="CompiledCode"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="compiledCode"/> �� <c>null</c> �Q�Ƃł��B</exception>
		/// <exception cref="SerializationException"><paramref name="compiledCode"/> �̓����[�g�ł��B</exception>
		public static ScriptCode GetScriptCode(CompiledCode compiledCode)
		{
			ContractUtils.RequiresNotNull(compiledCode, "compiledCode");
			return compiledCode.ScriptCode;
		}

		/// <summary>�w�肳�ꂽ <see cref="ScriptIO"/> ���� <see cref="SharedIO"/> ���擾���܂��B</summary>
		/// <param name="io"><see cref="SharedIO"/> ���擾���� <see cref="ScriptIO"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="io"/> �� <c>null</c> �Q�Ƃł��B</exception>
		/// <exception cref="SerializationException"><paramref name="io"/> �̓����[�g�ł��B</exception>
		public static SharedIO GetSharedIO(ScriptIO io)
		{
			ContractUtils.RequiresNotNull(io, "io");
			return io.SharedIO;
		}

		/// <summary>�w�肳�ꂽ <see cref="ScriptScope"/> ���� <see cref="Scope"/> ���擾���܂��B</summary>
		/// <param name="scriptScope"><see cref="Scope"/> ���擾���� <see cref="ScriptScope"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="scriptScope"/> �� <c>null</c> �Q�Ƃł��B</exception>
		/// <exception cref="SerializationException"><paramref name="scriptScope"/> �̓����[�g�ł��B</exception>
		public static Scope GetScope(ScriptScope scriptScope)
		{
			ContractUtils.RequiresNotNull(scriptScope, "scriptScope");
			return scriptScope.Scope;
		}

		/// <summary>�w�肳�ꂽ <see cref="ScriptEngine"/> ����� <see cref="Scope"/> ����V���� <see cref="ScriptScope"/> ���쐬���܂��B</summary>
		/// <param name="engine">�V���� <see cref="ScriptScope"/> �̊�ɂȂ�G���W�����w�肵�܂��B</param>
		/// <param name="scope">�V���� <see cref="ScriptScope"/> �̊�ɂȂ� <see cref="Scope"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="engine"/> �� <c>null</c> �Q�Ƃł��B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="scope"/> �� <c>null</c> �Q�Ƃł��B</exception>
		/// <exception cref="ArgumentException"><paramref name="engine"/> �͓��߃v���L�V�ł��B</exception>
		public static ScriptScope CreateScriptScope(ScriptEngine engine, Scope scope)
		{
			ContractUtils.RequiresNotNull(engine, "engine");
			ContractUtils.RequiresNotNull(scope, "scope");
			ContractUtils.Requires(!RemotingServices.IsTransparentProxy(engine), "engine", "The engine cannot be a transparent proxy");
			return new ScriptScope(engine, scope);
		}

		/// <summary><see cref="ScriptEngine"/> �̃A�v���P�[�V�����h���C�����̃R�[���o�b�N�����s���A���ʂ�Ԃ��܂��B</summary>
		[Obsolete("LanguageContext ��p���ăT�[�r�X�������� ScriptEngine.GetService ���Ăяo�����Ƃ𐄏����܂��B")]
		public static TRet CallEngine<T, TRet>(ScriptEngine engine, Func<LanguageContext, T, TRet> f, T arg) { return engine.Call(f, arg); }

		/// <summary>�w�肳�ꂽ <see cref="DocumentationProvider"/> ����V���� <see cref="DocumentationOperations"/> ���쐬���܂��B</summary>
		public static DocumentationOperations CreateDocumentationOperations(DocumentationProvider provider) { return new DocumentationOperations(provider); }
	}
}
