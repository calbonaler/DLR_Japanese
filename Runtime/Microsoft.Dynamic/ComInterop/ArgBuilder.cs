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

using System.Linq.Expressions;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>
	/// ���\�b�h�o�C���_�[�ɂ���Ďg�p����������񋟂��܂��B
	/// ���\�b�h�ɒ�`����Ă��邻�ꂼ��̕����������ɑ΂��� 1 �� <see cref="ArgBuilder"/> �����݂��܂��B
	/// ���\�b�h�ɓn�����_����������\�� <see cref="Microsoft.Scripting.Actions.Calls.ParameterWrapper"/> �Ƃ͑ΏƓI�ł��B
	/// </summary>
	abstract class ArgBuilder
	{
		/// <summary>�����ɓn�����l��񋟂��� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		internal abstract Expression Marshal(Expression parameter);

		/// <summary>
		/// �����ɓn�����l��񋟂��� <see cref="Expression"/> ��Ԃ��܂��B
		/// ���̃��\�b�h�͌��ʂ��Q�Ɠn���ɗ��p�����Ƒz�肳���ꍇ�ɌĂ΂�܂��B
		/// </summary>
		internal virtual Expression MarshalToRef(Expression parameter) { return Marshal(parameter); }

		/// <summary>
		/// ���\�b�h�Ăяo���̌�Ŏw�肳�ꂽ�l���X�V���� <see cref="Expression"/> ��Ԃ��܂��B
		/// �X�V���K�v�Ȃ��ꍇ�� <c>null</c> ��Ԃ��\��������܂��B
		/// </summary>
		internal virtual Expression UnmarshalFromRef(Expression newValue) { return newValue; }
	}
}