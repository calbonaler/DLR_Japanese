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
using Microsoft.Contracts;

namespace Microsoft.Scripting
{
	/// <summary>�g�[�N���Ɋւ�������i�[���܂��B</summary>
	[Serializable]
	public struct TokenInfo : IEquatable<TokenInfo>
	{
		/// <summary>���̃g�[�N���̎�ނ��擾�܂��͐ݒ肵�܂��B</summary>
		public TokenCategory Category { get; set; }

		/// <summary>���̃g�[�N���Ɋ֘A�t�����Ă���g���K���擾�܂��͐ݒ肵�܂��B</summary>
		public TokenTriggers Trigger { get; set; }

		/// <summary>���̃g�[�N���̃\�[�X�R�[�h��̏ꏊ���擾�܂��͐ݒ肵�܂��B</summary>
		public SourceSpan SourceSpan { get; set; }

		/// <summary>�\�[�X�R�[�h���͈̔́A�g�[�N���̎�ށA�g���K���g�p���āA<see cref="Microsoft.Scripting.TokenInfo"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="span">�g�[�N���̃\�[�X�R�[�h��̏ꏊ���w�肵�܂��B</param>
		/// <param name="category">�g�[�N���̎�ނ��w�肵�܂��B</param>
		/// <param name="trigger">�g�[�N���Ɋ֘A�t�����Ă���g���K���w�肵�܂��B</param>
		public TokenInfo(SourceSpan span, TokenCategory category, TokenTriggers trigger) : this()
		{
			Category = category;
			Trigger = trigger;
			SourceSpan = span;
		}

		/// <summary>���̃g�[�N���Ǝw�肳�ꂽ�g�[�N�������������ǂ����𔻒f���܂��B</summary>
		/// <param name="other">��r����g�[�N�����w�肵�܂��B</param>
		/// <returns>2 �̃g�[�N�����������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[StateIndependent]
		public bool Equals(TokenInfo other) { return Category == other.Category && Trigger == other.Trigger && SourceSpan == other.SourceSpan; }

		/// <summary>���̃I�u�W�F�N�g�Ǝw�肳�ꂽ�I�u�W�F�N�g�����������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">��r����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>2 �̃I�u�W�F�N�g���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public override bool Equals(object obj) { return obj is TokenInfo && Equals((TokenInfo)obj); }

		/// <summary>���̃I�u�W�F�N�g�̃n�b�V���l���v�Z���܂��B</summary>
		/// <returns>�v�Z���ꂽ�n�b�V���l�B</returns>
		public override int GetHashCode() { return Category.GetHashCode() ^ Trigger.GetHashCode() ^ SourceSpan.GetHashCode(); }

		/// <summary>2 �̃g�[�N�������������ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �ڂ̃g�[�N���B</param>
		/// <param name="right">��r���� 2 �ڂ̃g�[�N���B</param>
		/// <returns>2 �̃g�[�N�����������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator ==(TokenInfo left, TokenInfo right) { return left.Equals(right); }

		/// <summary>2 �̃g�[�N�����������Ȃ����ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �ڂ̃g�[�N���B</param>
		/// <param name="right">��r���� 2 �ڂ̃g�[�N���B</param>
		/// <returns>2 �̃g�[�N�����������Ȃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator !=(TokenInfo left, TokenInfo right) { return !left.Equals(right); }
	}
}
