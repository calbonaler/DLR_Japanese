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

namespace Microsoft.Scripting
{
	/// <summary>
	/// �ʏ�̈����ɑ�������Ȃ�������L�[���[�h�������󂯕t���邱�Ƃ��ł���������}�[�N���邽�߂Ɏg�p����܂��B
	/// ���̓��ʂȃL�[���[�h�����͌Ăяo���ō쐬�����f�B�N�V���i�����œn����܂��B
	/// </summary>
	/// <remarks>
	/// �����f�B�N�V���i�����T�|�[�g����قƂ�ǂ̌���͉��L�̌^���g�p�ł��܂��B
	/// <list type="bullet">
	///		<item><description>IDictionary&lt;string, anything&gt;</description></item>
	///		<item><description>IDictionary&lt;object, anything&gt;</description></item>
	///		<item><description>Dictionary&lt;string, anything&gt;</description></item>
	///		<item><description>Dictionary&lt;object, anything&gt;</description></item>
	///		<item><description>IDictionary</description></item>
	///		<item><description>IAttributeCollection (����)</description></item>
	/// </list>
	/// 
	/// ���ꃌ�x���ł̃T�|�[�g�̂Ȃ�����ł́A���[�U�[�������Ńf�B�N�V���i�����쐬���A�A�C�e�����i�[���Ȃ���΂Ȃ�܂���B
	/// ���̑����̓f�B�N�V���i���Ƃ��� <see cref="System.ParamArrayAttribute"/> �Ɠ��l�ł��B
	/// </remarks>
	/// <example>
	/// public static void KeywordArgFunction([ParamsDictionary]IDictionary&lt;string, object&gt; dict) {
	///     foreach (var v in dict) {
	///         Console.WriteLine("Key: {0} Value: {1}", v.Key, v.Value);
	///     }
	/// }
	/// 
	/// Python ����͈ȉ��̂悤�ɌĂяo����܂��B
	/// 
	/// KeywordArgFunction(a = 2, b = "abc")
	/// 
	/// will print:
	///     Key: a Value = 2
	///     Key: b Value = abc
	/// </example>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class ParamDictionaryAttribute : Attribute { }
}
