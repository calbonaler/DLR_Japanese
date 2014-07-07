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

namespace Microsoft.Scripting.Actions.Calls {
    /// <summary>�I�[�o�[���[�h�̉����ȂǂɎg�p�����k���ϊ��̃��x����\���܂��B�ϊ��̃��x���͂��ꂼ��̌��ꂪ��`���܂��B</summary>
    public enum NarrowingLevel {
        /// <summary>���̃��x���̕ϊ��͂ǂ̂悤�ȏk�����s���܂���B</summary>
        None,
        /// <summary>����͑� 1 ���x���̏k���ϊ����s���܂��B</summary>
        One,
        /// <summary>����͑� 2 ���x���Ƃ��� <see cref="One"/> �ȏ�̏k���ϊ����s���܂��B</summary>
        Two,
        /// <summary>����͑� 3 ���x���Ƃ��� <see cref="Two"/> �ȏ�̏k���ϊ����s���܂��B</summary>
        Three,
        /// <summary>�����Ӗ��̂���ϊ��ł���\���͂���܂����A��񂪑�������\��������܂��B</summary>
        All
    }
}
