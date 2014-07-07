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
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>
	/// ファイナライザを備えた <see cref="ComEventSink"/> のコレクションを表します。
	/// このリストは通常 RCW オブジェクトにカスタムデータを付加し、RCW がファイナライズされればいつでもファイナライズされます。
	/// </summary>
	sealed class ComEventSinksContainer : Collection<ComEventSink>, IDisposable
	{
		ComEventSinksContainer() { }

		static readonly object _ComObjectEventSinksKey = new object();

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
		public static ComEventSinksContainer FromRuntimeCallableWrapper(object rcw, bool createIfNotFound)
		{
			// !!! Marshal.Get/SetComObjectData has a LinkDemand for UnmanagedCode which will turn into a full demand. We need to avoid this by making this method SecurityCritical
			var data = Marshal.GetComObjectData(rcw, _ComObjectEventSinksKey);
			if (data != null || createIfNotFound == false)
				return (ComEventSinksContainer)data;
			lock (_ComObjectEventSinksKey)
			{
				data = Marshal.GetComObjectData(rcw, _ComObjectEventSinksKey);
				if (data != null)
					return (ComEventSinksContainer)data;
				var comEventSinks = new ComEventSinksContainer();
				if (!Marshal.SetComObjectData(rcw, _ComObjectEventSinksKey, comEventSinks))
					throw Error.SetComObjectDataFailed();
				return comEventSinks;
			}
		}

		public void Dispose()
		{
			DisposeAll();
			GC.SuppressFinalize(this);
		}

		void DisposeAll()
		{
			foreach (var sink in this)
				sink.Dispose();
		}

		~ComEventSinksContainer() { DisposeAll(); }
	}
}