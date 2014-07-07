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
    /// <summary>オーバーロードの解決などに使用される縮小変換のレベルを表します。変換のレベルはそれぞれの言語が定義します。</summary>
    public enum NarrowingLevel {
        /// <summary>このレベルの変換はどのような縮小も行いません。</summary>
        None,
        /// <summary>言語は第 1 レベルの縮小変換を行います。</summary>
        One,
        /// <summary>言語は第 2 レベルとして <see cref="One"/> 以上の縮小変換を行います。</summary>
        Two,
        /// <summary>言語は第 3 レベルとして <see cref="Two"/> 以上の縮小変換を行います。</summary>
        Three,
        /// <summary>多少意味のある変換である可能性はありますが、情報が損失する可能性があります。</summary>
        All
    }
}
