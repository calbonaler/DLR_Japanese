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

using System.ComponentModel;

namespace Microsoft.Scripting
{
	/// <summary>�\�[�X�R�[�h�̎�ނ��`���܂��B�p�[�T�[�͓K�X������Ԃ�ݒ肵�܂��B</summary>
	public enum SourceCodeKind
	{
		/// <summary>��ނ͎w�肳��Ă��܂���B</summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		Unspecified = 0,
		/// <summary>�\�[�X�R�[�h�͎���\���Ă��܂��B</summary>
		Expression = 1,
		/// <summary>�\�[�X�R�[�h�͕����̃X�e�[�g�����g��\���Ă��܂��B</summary>
		Statements = 2,
		/// <summary>�\�[�X�R�[�h�͒P��̃X�e�[�g�����g��\���Ă��܂��B</summary>
		SingleStatement = 3,
		/// <summary>�\�[�X�R�[�h�̓t�@�C���̓��e�ł��B</summary>
		File = 4,
		/// <summary>�\�[�X�R�[�h�͑Θb�R�}���h�ł��B </summary>
		InteractiveCode = 5,
		/// <summary>����p�[�T�[�������I�Ɏ�ނ����肵�܂��B����ł��Ȃ������ꍇ�͍\���G���[���񍐂���܂��B</summary>
		AutoDetect = 6
	}
}

namespace Microsoft.Scripting.Utils
{
	/// <summary>�񋓑͈̂̔͂Ɋւ��郁�\�b�h��񋟂��܂��B</summary>
	public static partial class EnumBounds
	{
		/// <summary>�w�肳�ꂽ <see cref="SourceCodeKind"/> �񋓑̂��L���Ȓl�������Ă��邩�ǂ�����Ԃ��܂��B</summary>
		/// <param name="value">�L�����ǂ����𒲂ׂ� <see cref="SourceCodeKind"/> �񋓑̂̒l���w�肵�܂��B</param>
		/// <returns>�L���Ȓl�ł���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsValid(this SourceCodeKind value) { return value > SourceCodeKind.Unspecified && value <= SourceCodeKind.AutoDetect; }
	}
}
