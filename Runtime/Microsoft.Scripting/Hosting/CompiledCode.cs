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
	/// <summary><see cref="Microsoft.Scripting.ScriptCode"/> に対するもう 1 つのホスティング API を表します。</summary>
	public sealed class CompiledCode : MarshalByRefObject
	{
		/// <summary>
		/// コードをコンパイルした <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> および、実際のコードを表す <see cref="Microsoft.Scripting.ScriptCode"/> を使用して、
		/// <see cref="Microsoft.Scripting.Hosting.CompiledCode"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="engine">コードをコンパイルした <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を指定します。</param>
		/// <param name="code">実際のコードを表す <see cref="Microsoft.Scripting.ScriptCode"/> を指定します。</param>
		internal CompiledCode(ScriptEngine engine, ScriptCode code)
		{
			Assert.NotNull(engine);
			Assert.NotNull(code);
			Engine = engine;
			ScriptCode = code;
		}
		
		ScriptScope _defaultScope;

		/// <summary>このコードをコンパイルした <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を取得します。</summary>
		public ScriptEngine Engine { get; private set; }
		
		/// <summary>実際のコードを表す <see cref="Microsoft.Scripting.ScriptCode"/> を取得します。</summary>
		internal ScriptCode ScriptCode { get; private set; }

		/// <summary>このコードの既定のスコープを取得します。</summary>
		public ScriptScope DefaultScope
		{
			get
			{
				if (_defaultScope == null)
					Interlocked.CompareExchange(ref _defaultScope, new ScriptScope(Engine, ScriptCode.CreateScope()), null);
				return _defaultScope;
			}
		}

		/// <summary>コードを既定のスコープで実行します。 </summary>
		public dynamic Execute() { return ScriptCode.Run(DefaultScope.Scope); }

		/// <summary>指定されたスコープでコードを実行し、結果を返します。</summary>
		/// <param name="scope">コードを実行するスコープを指定します。</param>
		public dynamic Execute(ScriptScope scope)
		{
			ContractUtils.RequiresNotNull(scope, "scope");
			return ScriptCode.Run(scope.Scope);
		}

		/// <summary>既定のスコープでコードを実行し、結果を指定された型に変換します。</summary>
		public T Execute<T>() { return Engine.Operations.ConvertTo<T>((object)Execute()); }

		/// <summary>指定されたスコープでコードを実行し、結果を指定された型に変換します。</summary>
		/// <param name="scope">コードを実行するスコープを指定します。</param>
		public T Execute<T>(ScriptScope scope) { return Engine.Operations.ConvertTo<T>((object)Execute(scope)); }

		/// <summary>既定のスコープでコードを実行し、結果を <see cref="System.Runtime.Remoting.ObjectHandle"/> を用いてラップします。</summary>
		public ObjectHandle ExecuteAndWrap() { return new ObjectHandle((object)Execute()); }

		/// <summary>指定されたスコープでコードを実行し、結果を <see cref="System.Runtime.Remoting.ObjectHandle"/> を用いてラップします。</summary>
		/// <param name="scope">コードを実行するスコープを指定します。</param>
		public ObjectHandle ExecuteAndWrap(ScriptScope scope) { return new ObjectHandle((object)Execute(scope)); }

		/// <summary>既定のスコープでコードを実行し、結果および発生した例外を <see cref="System.Runtime.Remoting.ObjectHandle"/> を用いてラップします。</summary>
		/// <param name="exception">発生した例外がラップされた <see cref="System.Runtime.Remoting.ObjectHandle"/> を格納する変数を指定します。</param>
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

		/// <summary>指定されたスコープでコードを実行し、結果および発生した例外を <see cref="System.Runtime.Remoting.ObjectHandle"/> を用いてラップします。</summary>
		/// <param name="scope">コードを実行するスコープを指定します。</param>
		/// <param name="exception">発生した例外がラップされた <see cref="System.Runtime.Remoting.ObjectHandle"/> を格納する変数を指定します。</param>
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

		/// <summary>対象のインスタンスの有効期間ポリシーを制御する、有効期間サービス オブジェクトを取得します。</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; } // TODO: Figure out what is the right lifetime
	}
}
