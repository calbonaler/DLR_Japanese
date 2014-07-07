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
using System.IO;
using System.Security.Permissions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>ディスク上のファイルによって保持されるストリームコンテンツに対する <see cref="StreamContentProvider"/> を提供します。</summary>
	[Serializable]
	sealed class FileStreamContentProvider : StreamContentProvider
	{
		readonly PALHolder _pal;

		/// <summary>このコンテンツを保持しているファイルのパスを取得します。</summary>
		internal string Path { get; private set; }

		/// <summary><see cref="PlatformAdaptationLayer"/> および基になるファイルのパスを使用して、<see cref="Microsoft.Scripting.FileStreamContentProvider"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="pal">ファイルのオープンに使用する <see cref="PlatformAdaptationLayer"/> を指定します。</param>
		/// <param name="path">対象となるコンテンツを保持しているファイルのパスを指定します。</param>
		internal FileStreamContentProvider(PlatformAdaptationLayer pal, string path)
		{
			Assert.NotNull(pal, path);
			Path = path;
			_pal = new PALHolder(pal);
		}

		/// <summary><see cref="FileStreamContentProvider"/> が作成されたコンテンツを基にする新しい <see cref="Stream"/> を作成します。</summary>
		public override Stream GetStream() { return _pal.GetStream(Path); }

		[Serializable]
		class PALHolder : MarshalByRefObject
		{
			[NonSerialized]
			readonly PlatformAdaptationLayer _pal;

			internal PALHolder(PlatformAdaptationLayer pal) { _pal = pal; }

			internal Stream GetStream(string path) { return _pal.OpenInputFileStream(path); }

			// TODO: Figure out what is the right lifetime
			[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
			public override object InitializeLifetimeService() { return null; }
		}
	}
}
