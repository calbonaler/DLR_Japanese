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

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>�I�[�o�[���[�h�����̍ۂ̑I�����ꂽ����\���܂��B</summary>
	public enum Candidate
	{
		/// <summary>2 �̌��͓����ł��B</summary>
		Equivalent = 0,
		/// <summary>1 �Ԗڂ̌�₪�I������܂����B</summary>
		One = +1,
		/// <summary>2 �Ԗڂ̌�₪�I������܂����B</summary>
		Two = -1,
		/// <summary>2 �̌��͂����܂��ł���I���ł��܂���B</summary>
		Ambiguous = 2
	}

	/// <summary><see cref="Microsoft.Scripting.Actions.Calls.Candidate"/> �ɑ΂���g�����\�b�h��񋟂��܂��B</summary>
	internal static class CandidateExtension
	{
		/// <summary>���݂� <see cref="Candidate"/> �ɂ����đI���ς݂ł��邩�ǂ����������l���擾���܂��B</summary>
		/// <param name="candidate">���f���� <see cref="Candidate"/> ���w�肵�܂��B</param>
		/// <returns><see cref="Candidate"/> ���I���ς݂ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool Chosen(this Candidate candidate) { return candidate == Candidate.One || candidate == Candidate.Two; }

		/// <summary>���݂� <see cref="Candidate"/> �̂����Е���\�� <see cref="Candidate"/> ���擾���܂��B</summary>
		/// <param name="candidate">�����Е����擾���� <see cref="Candidate"/> ���w�肵�܂��B</param>
		/// <returns>�����Е���\�� <see cref="Candidate"/>�B</returns>
		public static Candidate TheOther(this Candidate candidate)
		{
			if (candidate == Candidate.One)
				return Candidate.Two;
			if (candidate == Candidate.Two)
				return Candidate.One;
			return candidate;
		}
	}
}
