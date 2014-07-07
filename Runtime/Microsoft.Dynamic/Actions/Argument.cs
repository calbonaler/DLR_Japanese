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
using System.Linq.Expressions;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	// TODO: ����ɁAthis, list, dict, block �̃C���f�b�N�X���L�����邾���ŏ\���ɂ���ׂ��ł��B
	/// <summary>�֐��ɓn����������\���܂��B</summary>
	public struct Argument : IEquatable<Argument>
	{
		/// <summary>�P���Ȗ��O�̂Ȃ��ʒu�����肳��Ă��������\���܂��B</summary>
		public static readonly Argument Simple = new Argument(ArgumentType.Simple, null);

		/// <summary>���̈����̎�ނ��擾���܂��B</summary>
		public ArgumentType Kind { get; private set; }

		/// <summary>���̈��������O�t�������ł���΁A���̈����̖��O���擾���܂��B����ȊO�̏ꍇ�� <c>null</c> ��Ԃ��܂��B</summary>
		public string Name { get; private set; }

		/// <summary>�w�肳�ꂽ���O���g�p���āA<see cref="Microsoft.Scripting.Actions.Argument"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">���̖��O�t�������̖��O���w�肵�܂��B</param>
		public Argument(string name) : this(ArgumentType.Named, name) { }

		/// <summary>�����̎�ނ��g�p���āA<see cref="Microsoft.Scripting.Actions.Argument"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="kind">���̈����̎�ނ��w�肵�܂��B<see cref="ArgumentType.Named"/> ���w�肷�邱�Ƃ͂ł��܂���B</param>
		public Argument(ArgumentType kind) : this(kind, null) { }

		/// <summary>�����̖��O����ю�ނ��g�p���āA<see cref="Microsoft.Scripting.Actions.Argument"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="kind">���̈����̎�ނ��w�肵�܂��B</param>
		/// <param name="name">�����̎�ނ����O�t�������̏ꍇ�A�����̖��O���w�肵�܂��B</param>
		public Argument(ArgumentType kind, string name) : this()
		{
			ContractUtils.Requires((kind == ArgumentType.Named) ^ (name == null), "kind");
			Kind = kind;
			Name = name;
		}

		/// <summary>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">�������𔻒f����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public override bool Equals(object obj) { return obj is Argument && Equals((Argument)obj); }

		/// <summary>���̈������w�肳�ꂽ�����Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="other">���������ǂ����𔻒f���� <see cref="Argument"/> �I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>���̈����Ǝw�肳�ꂽ�������������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[StateIndependent]
		public bool Equals(Argument other) { return Kind == other.Kind && Name == other.Name; }

		/// <summary>�w�肳�ꂽ 2 �� <see cref="Argument"/> �I�u�W�F�N�g�����������ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �Ԗڂ� <see cref="Argument"/>�B</param>
		/// <param name="right">��r���� 2 �Ԗڂ� <see cref="Argument"/>�B</param>
		/// <returns>2 �� <see cref="Argument"/> �I�u�W�F�N�g���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator ==(Argument left, Argument right) { return left.Equals(right); }

		/// <summary>�w�肳�ꂽ 2 �� <see cref="Argument"/> �I�u�W�F�N�g���������Ȃ����ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �Ԗڂ� <see cref="Argument"/>�B</param>
		/// <param name="right">��r���� 2 �Ԗڂ� <see cref="Argument"/>�B</param>
		/// <returns>2 �� <see cref="Argument"/> �I�u�W�F�N�g���������Ȃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator !=(Argument left, Argument right) { return !left.Equals(right); }

		/// <summary>���̃I�u�W�F�N�g�̃n�b�V���l���v�Z���܂��B</summary>
		/// <returns>�v�Z���ꂽ�n�b�V���l�B</returns>
		public override int GetHashCode() { return Name != null ? Name.GetHashCode() ^ (int)Kind : (int)Kind; }

		/// <summary>���̈������P���Ȉ������ǂ����������l���擾���܂��B</summary>
		public bool IsSimple { get { return Equals(Simple); } }
		
		/// <summary>���̃I�u�W�F�N�g�̕�����\����Ԃ��܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return Name == null ? Kind.ToString() : Kind.ToString() + ":" + Name; }

		/// <summary>���̃I�u�W�F�N�g��\�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g��\�� <see cref="Expression"/>�B</returns>
		internal Expression CreateExpression()
		{
			return Expression.New(typeof(Argument).GetConstructor(new Type[] { typeof(ArgumentType), typeof(string) }),
				AstUtils.Constant(Kind),
				AstUtils.Constant(Name, typeof(string))
			);
		}
	}
}


