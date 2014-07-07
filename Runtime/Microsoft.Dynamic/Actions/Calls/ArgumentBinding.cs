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
using System.Linq;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>�������Ɖ������̊Ԃ̊֘A�t����\���܂��B</summary>
	public struct ArgumentBinding : IEquatable<ArgumentBinding>
	{
		static readonly int[] EmptyBinding = new int[0];

		int[] _binding; // immutable

		/// <summary>�ʒu�����肳��Ă�������̐����g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.ArgumentBinding"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="positionalArgCount">�ʒu�����肳��Ă�������̐����w�肵�܂��B</param>
		internal ArgumentBinding(int positionalArgCount) : this(positionalArgCount, EmptyBinding) { }

		/// <summary>�ʒu�����肳��Ă�������̐��Ɩ��O�t�������̊֘A�t�����g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.ArgumentBinding"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="positionalArgCount">�ʒu�����肳��Ă�������̐����w�肵�܂��B</param>
		/// <param name="binding">���O�t�������̊֘A�t�����w�肵�܂��B</param>
		internal ArgumentBinding(int positionalArgCount, int[] binding) : this()
		{
			Assert.NotNull(binding);
			_binding = binding;
			PositionalArgCount = positionalArgCount;
		}

		/// <summary>�ʒu�����肳��Ă�������̐����擾���܂��B</summary>
		public int PositionalArgCount { get; private set; }

		/// <summary>�w�肳�ꂽ�������̃C���f�b�N�X�ɑ΂��鉼�����̃C���f�b�N�X���擾���܂��B</summary>
		/// <param name="argumentIndex">�������̈������X�g���̏ꏊ�������C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�������̈������X�g���̏ꏊ�������C���f�b�N�X�B</returns>
		public int ArgumentToParameter(int argumentIndex) { return argumentIndex < PositionalArgCount ? argumentIndex : PositionalArgCount + _binding[argumentIndex - PositionalArgCount]; }

		/// <summary>�w�肳�ꂽ <see cref="ArgumentBinding"/> ������ <see cref="ArgumentBinding"/> �Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="other">��r���� <see cref="ArgumentBinding"/>�B</param>
		/// <returns>���� <see cref="ArgumentBinding"/> ���w�肳�ꂽ <see cref="ArgumentBinding"/> �Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool Equals(ArgumentBinding other) { return PositionalArgCount == other.PositionalArgCount && _binding.SequenceEqual(other._binding); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�����̃I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">��r����I�u�W�F�N�g�B</param>
		/// <returns>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public override bool Equals(object obj) { return obj is ArgumentBinding && Equals((ArgumentBinding)obj); }

		/// <summary>���̃I�u�W�F�N�g�̃n�b�V���l���v�Z���܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g�ɑ΂���n�b�V���l�B</returns>
		public override int GetHashCode() { return PositionalArgCount.GetHashCode() ^ _binding.GetValueHashCode(); }

		/// <summary>2 �� <see cref="ArgumentBinding"/> �����������ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �Ԗڂ� <see cref="ArgumentBinding"/>�B</param>
		/// <param name="right">��r���� 2 �Ԗڂ� <see cref="ArgumentBinding"/>�B</param>
		/// <returns>2 �� <see cref="ArgumentBinding"/> ���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator ==(ArgumentBinding left, ArgumentBinding right) { return left.Equals(right); }

		/// <summary>2 �� <see cref="ArgumentBinding"/> ���������Ȃ����ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �Ԗڂ� <see cref="ArgumentBinding"/>�B</param>
		/// <param name="right">��r���� 2 �Ԗڂ� <see cref="ArgumentBinding"/>�B</param>
		/// <returns>2 �� <see cref="ArgumentBinding"/> ���������Ȃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator !=(ArgumentBinding left, ArgumentBinding right) { return !left.Equals(right); }
	}
}
