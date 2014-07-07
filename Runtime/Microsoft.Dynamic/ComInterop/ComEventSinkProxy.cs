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
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>
	/// <see cref="ComEventSink"/> のインスタンスで sourceIid に対する QueryInterface のハンドルに責任を負います。
	/// 
	/// 背景: COM イベント シンクがコネクション ポイントに Advise する場合、dispinterface が渡されることが仮定されます。
	/// 現在、COM クライアントが正しいポインタを渡すと信頼しているホストもありますが、そうでないものもあります。
	/// 例えば、Excel のコネクションポイントの実装は渡されたポインタに対して QueryInterface を呼び出しませんが、Word は呼び出します。
	/// 
	/// <see cref="ComEventSink"/> は強く言えば、実装を要求されているインターフェイスを実装しません。<see cref="IReflect"/> を使用して「改竄」しているだけです。
	/// そのため、IConnectionPoint.Advise に渡されたポインタに対する Word の QueryInterface は失敗します。
	/// これを防ぐために、他のクラスのように「着飾る」ことができる <see cref="RealProxy"/> の利点を利用して、実際はサポートしないインターフェイスに対する QueryInterface を成功させます。
	/// (イベント シンクへの呼び出しの場合、共通のプラクティスは IDispatch.Invoke を使用することなので「私はそのインターフェイスを実装します」と言えれば十分。)
	/// </summary>
	sealed class ComEventSinkProxy : RealProxy
	{
		Guid _sinkIid;
		ComEventSink _sink;
		static readonly MethodInfo _methodInfoInvokeMember = typeof(ComEventSink).GetMethod("InvokeMember", BindingFlags.Instance | BindingFlags.Public);

		public ComEventSinkProxy(ComEventSink sink, Guid sinkIid) : base(typeof(ComEventSink))
		{
			_sink = sink;
			_sinkIid = sinkIid;
		}

		// iid がシンクのものである場合、基底クラスに IDispatch する RCW を求める
		public override IntPtr SupportsInterface(ref Guid iid) { return iid == _sinkIid ? Marshal.GetIDispatchForObject(_sink) : base.SupportsInterface(ref iid); }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public override IMessage Invoke(IMessage msg)
		{
			ContractUtils.RequiresNotNull(msg, "msg");
			// メソッド呼び出しをハンドルする方法だけ知っている (プロパティやフィールドアクセサはメソッドとみなされる)
			var mcm = msg as IMethodCallMessage;
			if (mcm == null)
				throw new NotSupportedException();
			// ComEventSink.InvokeMember は特別にハンドルします。
			// その必要がある理由は、RealProxy.Invoke を通して呼ばれた場合に、
			// どのように namedParameters 引数 (IMethodCallMessage.Args 配列の 7 番目の要素) がマーシャリングされるかによるためです。
			// RealProxy.Invoke では namedParameters は object[] 型ですが、InvokeMember は string[] 型を期待しています。
			// 単純にこの呼び出しに (RemotingServices.ExecuteMessage を使用して) そのまま渡す場合、
			// リモートが namedParameter (object[]) を (namedParameters として string[] を予期する) InvokeMember に渡そうとした場合に InvalidCastException を得ることになります。
			// そのため、ComEventSink.InvokeMember では namedParameters は使用しません。つまり、単純に引数を無視して null を渡します。
			if (((MethodInfo)mcm.MethodBase) == _methodInfoInvokeMember)
			{
				object retVal = null;
				try
				{
					retVal = ((IReflect)_sink).InvokeMember(mcm.Args[0] as string, (BindingFlags)mcm.Args[1], mcm.Args[2] as Binder, null, mcm.Args[4] as object[], mcm.Args[5] as ParameterModifier[], mcm.Args[6] as CultureInfo, null);
				}
				catch (Exception ex) { return new ReturnMessage(ex.InnerException, mcm); }
				return new ReturnMessage(retVal, mcm.Args, mcm.ArgCount, null, mcm);
			}
			return RemotingServices.ExecuteMessage(_sink, mcm);
		}
	}
}