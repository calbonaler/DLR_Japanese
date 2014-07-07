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
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>
	/// 特定の RCW に対するイベント シンクを実装します。
	/// TlbImp'd のアセンブリのイベント実装と異なり、このクラスは各 RCW に対して 1 つしかイベント シンクを作成しません。
	/// (理論上 RCW には複数の <see cref="ComEventSink"/> が存在できます。しかしこれらはすべて実装しているインターフェイスが異なります。)
	/// </summary>
	/// <remarks>
	/// それぞれの <see cref="ComEventSink"/> は <see cref="ComEventSinkMethod"/> オブジェクトのリストを保持します。
	/// この <see cref="ComEventSinkMethod"/> はソース インターフェイス上の単一のメソッドを表し、呼び出しを転送するマルチキャスト デリゲートを保持します。
	/// 通告: 同じ <see cref="ComEventSinkMethod"/> が複数のイベント ハンドラを呼び出せるように、マルチキャスト デリゲートをチェインします。
	/// <see cref="ComEventSink"/> はコネクションポイントから Unadvise するために、<see cref="IDisposable"/> を実装しています。
	/// 通常、RCW がファイナライズされると、対応する <see cref="IDisposable.Dispose"/> が <see cref="ComEventSinksContainer"/> のファイナライザによってトリガーされます。
	/// 通告: <see cref="ComEventSinksContainer"/> の生存期間は RCW の生存期間に束縛されます。
	/// </remarks>
	sealed class ComEventSink : MarshalByRefObject, IReflect, IDisposable
	{
		Guid _sourceIid;
		ComTypes.IConnectionPoint _connectionPoint;
		int _adviseCookie;
		List<ComEventSinkMethod> _comEventSinkMethods;
		object _lockObject = new object(); // DoNotLockOnObjectsWithWeakIdentity 警告が発生するため、ComEventSink はロックできません。

		/// <summary>メソッドの ("[DISPID=N]" の形で文字列にフォーマットされた) DISPIDと呼び出すデリゲートのリストを格納します。</summary>
		class ComEventSinkMethod
		{
			public string _name;
			public Func<object[], object> _handlers;
		}

		ComEventSink(object rcw, Guid sourceIid) { Initialize(rcw, sourceIid); }

		void Initialize(object rcw, Guid sourceIid)
		{
			_sourceIid = sourceIid;
			_adviseCookie = -1;
			Debug.Assert(_connectionPoint == null, "re-initializing event sink w/o unadvising from connection point");
			var cpc = rcw as ComTypes.IConnectionPointContainer;
			if (cpc == null)
				throw Error.COMObjectDoesNotSupportEvents();
			cpc.FindConnectionPoint(ref _sourceIid, out _connectionPoint);
			if (_connectionPoint == null)
				throw Error.COMObjectDoesNotSupportSourceInterface();
			// なぜこうする必要があるのかについては ComEventSinkProxy のコメントを読んでください。
			_connectionPoint.Advise(new ComEventSinkProxy(this, _sourceIid).GetTransparentProxy(), out _adviseCookie);
		}

		public static ComEventSink FromRuntimeCallableWrapper(object rcw, Guid sourceIid, bool createIfNotFound)
		{
			var comEventSinks = ComEventSinksContainer.FromRuntimeCallableWrapper(rcw, createIfNotFound);
			if (comEventSinks == null)
				return null;
			ComEventSink comEventSink = null;
			lock (comEventSinks)
			{
				foreach (var sink in comEventSinks)
				{
					if (sink._sourceIid == sourceIid)
					{
						comEventSink = sink;
						break;
					}
					else if (sink._sourceIid == Guid.Empty)
					{
						// 以前に破棄された ComEventSink オブジェクトが見つかったので、再利用する。
						sink.Initialize(rcw, sourceIid);
						comEventSink = sink;
					}
				}
				if (comEventSink == null && createIfNotFound == true)
					comEventSinks.Add(comEventSink = new ComEventSink(rcw, sourceIid));
			}
			return comEventSink;
		}

		public void AddHandler(int dispid, object func)
		{
			var name = string.Format(CultureInfo.InvariantCulture, "[DISPID={0}]", dispid);
			lock (_lockObject)
			{
				var sinkMethod = FindSinkMethod(name);
				if (sinkMethod == null)
					(_comEventSinkMethods ?? (_comEventSinkMethods = new List<ComEventSinkMethod>())).Add(sinkMethod = new ComEventSinkMethod() { _name = name });
				sinkMethod._handlers += new SplatCallSite(func).Invoke;
			}
		}

		public void RemoveHandler(int dispid, object func)
		{
			var name = string.Format(CultureInfo.InvariantCulture, "[DISPID={0}]", dispid);
			lock (_lockObject)
			{
				var sinkEntry = FindSinkMethod(name);
				if (sinkEntry == null)
					return;
				// デリゲートをマルチキャストデリゲートチェインから取り除く
				// 削除したいハンドラに対応するデリゲートを見つける必要があります。
				// デリゲートオブジェクトの Target プロパティが ComEventCallContext オブジェクトなので、これは簡単です。
				foreach (var d in sinkEntry._handlers.GetInvocationList())
				{
					var callContext = d.Target as SplatCallSite;
					if (callContext != null && callContext._callable.Equals(func))
					{
						sinkEntry._handlers -= d as Func<object[], object>;
						break;
					}
				}
				// デリゲートチェインが空ならば、対応する ComEventSinkMethod を削除
				if (sinkEntry._handlers == null)
					_comEventSinkMethods.Remove(sinkEntry);
				// インターフェイスに要素が存在しなければ、コネクションポイントから Unadvise (Dispose 呼び出しは IConnectionPoint.Unadvise を呼び出します)
				if (_comEventSinkMethods.Count == 0)
					Dispose(); // 新しいハンドラがアタッチされた場合データ構造を再利用するので、リストからは削除しない
			}
		}

		public object ExecuteHandler(string name, object[] args)
		{
			var site = FindSinkMethod(name);
			return site != null && site._handlers != null ? site._handlers(args) : null;
		}

		#region Unimplemented members

		public FieldInfo GetField(string name, BindingFlags bindingAttr) { return null; }

		public FieldInfo[] GetFields(BindingFlags bindingAttr) { return new FieldInfo[0]; }

		public MemberInfo[] GetMember(string name, BindingFlags bindingAttr) { return new MemberInfo[0]; }

		public MemberInfo[] GetMembers(BindingFlags bindingAttr) { return new MemberInfo[0]; }

		public MethodInfo GetMethod(string name, BindingFlags bindingAttr) { return null; }

		public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) { return null; }

		public MethodInfo[] GetMethods(BindingFlags bindingAttr) { return new MethodInfo[0]; }

		public PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) { return null; }

		public PropertyInfo GetProperty(string name, BindingFlags bindingAttr) { return null; }

		public PropertyInfo[] GetProperties(BindingFlags bindingAttr) { return new PropertyInfo[0]; }

		#endregion

		public Type UnderlyingSystemType { get { return typeof(object); } }

		public object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) { return ExecuteHandler(name, args); }

		public void Dispose()
		{
			DisposeAll();
			GC.SuppressFinalize(this);
		}

		~ComEventSink() { DisposeAll(); }

		void DisposeAll()
		{
			if (_connectionPoint == null || _adviseCookie == -1)
				return;
			try
			{
				_connectionPoint.Unadvise(_adviseCookie);
				// _connectionPoint はこのオブジェクトのコンストラクタで CLR に入ったので、その参照カウンタはインクリメントされている。
				// _connectionPoint を他のコンポーネントに公開していないので、リンクしている他のオブジェクトに対する RCW をキルする心配無くリリースできる。
				Marshal.ReleaseComObject(_connectionPoint);
			}
			catch (Exception ex)
			{
				COMException exCOM = ex as COMException;
				if (exCOM != null && exCOM.ErrorCode == ComHresults.CONNECT_E_NOCONNECTION)
				{
					Debug.Assert(false, "IConnectionPoint::Unadvise returned CONNECT_E_NOCONNECTION.");
					throw;
				}
			}
			finally
			{
				_connectionPoint = null;
				_adviseCookie = -1;
				_sourceIid = Guid.Empty;
			}
		}

		ComEventSinkMethod FindSinkMethod(string name) { return _comEventSinkMethods == null ? null : _comEventSinkMethods.Find(element => element._name == name); }
	}
}