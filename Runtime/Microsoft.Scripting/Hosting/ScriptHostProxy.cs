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

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>DLR �ɑ΂��ăz�X�e�B���O��񋟂��܂��BDLR�ɑ΂���v���� <see cref="Microsoft.Scripting.Hosting.ScriptHost"/> �ɓ]������܂��B</summary>
	sealed class ScriptHostProxy : DynamicRuntimeHostingProvider
	{
		readonly ScriptHost _host;

		/// <summary>�w�肳�ꂽ�z�X�g���g�p���āA<see cref="Microsoft.Scripting.Hosting.ScriptHostProxy"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="host">�v����]������z�X�g���w�肵�܂��B</param>
		public ScriptHostProxy(ScriptHost host)
		{
			Assert.NotNull(host);
			_host = host;
		}

		/// <summary>�z�X�g�Ɋ֘A�t����ꂽ <see cref="Microsoft.Scripting.PlatformAdaptationLayer"/> ���擾���܂��B</summary>
		public override PlatformAdaptationLayer PlatformAdaptationLayer { get { return _host.PlatformAdaptationLayer; } }
	}
}
