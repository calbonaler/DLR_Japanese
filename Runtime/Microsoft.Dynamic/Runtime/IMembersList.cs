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

using System.Collections.Generic;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>�C���X�^���X�̂��ׂẴ����o�̖��O�̃��X�g��񋟂��܂��B</summary>
	public interface IMembersList
	{
		/// <summary>���̃I�u�W�F�N�g�̂��ׂẴ����o�̖��O���擾���܂��B</summary>
		/// <returns>���ׂẴ����o�̖��O��񋓂��� <see cref="System.Collections.Generic.IEnumerable&lt;String&gt;"/>�B</returns>
		IEnumerable<string> GetMemberNames();
	}
}
