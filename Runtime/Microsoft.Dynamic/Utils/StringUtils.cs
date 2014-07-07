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
using System.Text;

namespace Microsoft.Scripting.Utils
{
	/// <summary>������Ɋւ��郆�[�e�B���e�B ���\�b�h�����J���܂��B</summary>
	public static class StringUtils
	{
		/// <summary>�w�肳�ꂽ�e�L�X�g���w�肳�ꂽ���ŒP��P�ʂŎ����I�ɉ��s���A�S�̂��C���f���g���܂��B</summary>
		/// <param name="text">�P��P�ʂŎ������s���ăC���f���g���s���e�L�X�g���w�肵�܂��B</param>
		/// <param name="indentFirst">�e�L�X�g�̐擪�s���C���f���g���邩�ǂ����������l���w�肵�܂��B</param>
		/// <param name="lineWidth">�e�L�X�g�̎������s�̕����������������w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���Ŏ����I�ɉ��s����S�̂��C���f���g���ꂽ������B</returns>
		public static string SplitWords(string text, bool indentFirst, int lineWidth)
		{
			ContractUtils.RequiresNotNull(text, "text");
			const string indent = "    ";
			if (text.Length <= lineWidth || lineWidth <= 0)
				return indentFirst ? indent + text : text;
			StringBuilder res = new StringBuilder();
			for (int start = 0, length = lineWidth; start < text.Length; length = Math.Min(text.Length - start, lineWidth))
			{
				if (length >= lineWidth)
				{
					// find last space to break on
					while (length > 0 && !char.IsWhiteSpace(text[start + length - 1]))
						length--;
				}
				if (indentFirst || res.Length > 0)
					res.Append(indent);
				var copying = length > 0 ? length : Math.Min(text.Length - start, lineWidth);
				res.Append(text, start, copying);
				start += copying;
				res.AppendLine();
			}
			return res.ToString();
		}

		/// <summary>�w�肳�ꂽ�e�L�X�g�Ɋ܂܂��擪�Ƀo�b�N�X���b�V�����t�������悤�Ȑ��䕶�����G�X�P�[�v���܂��B</summary>
		/// <param name="value">���䕶�����G�X�P�[�v����e�L�X�g���w�肵�܂��B</param>
		/// <returns>���䕶�����G�X�P�[�v���ꂽ�e�L�X�g�B�Ⴆ�� CR �� "\r" �ɃG�X�P�[�v����܂��B</returns>
		public static string AddSlashes(string value)
		{
			ContractUtils.RequiresNotNull(value, "value");
			// TODO: optimize
			StringBuilder result = new StringBuilder(value.Length);
			for (int i = 0; i < value.Length; i++)
			{
				switch (value[i])
				{
					case '\a': result.Append("\\a"); break;
					case '\b': result.Append("\\b"); break;
					case '\f': result.Append("\\f"); break;
					case '\n': result.Append("\\n"); break;
					case '\r': result.Append("\\r"); break;
					case '\t': result.Append("\\t"); break;
					case '\v': result.Append("\\v"); break;
					default: result.Append(value[i]); break;
				}
			}
			return result.ToString();
		}
	}
}
