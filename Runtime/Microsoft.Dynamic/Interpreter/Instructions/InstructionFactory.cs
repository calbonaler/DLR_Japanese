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
using System.Collections.Generic;
using Microsoft.Scripting.Numerics;
using BigInt = System.Numerics.BigInteger;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>�^�ɑΉ����閽�߂̍쐬���x�����܂��B</summary>
	public abstract class InstructionFactory
	{
		// TODO: weak table for types in a collectible assembly?
		static Dictionary<Type, InstructionFactory> _factories;

		/// <summary>�w�肳�ꂽ�^�Ɋ�Â����߂��쐬�ł��� <see cref="InstructionFactory"/> ��Ԃ��܂��B</summary>
		/// <param name="type">���߂Ɏg�p����^���w�肵�܂��B</param>
		/// <returns>�^�Ɋ�Â����߂��쐬�ł��� <see cref="InstructionFactory"/>�B</returns>
		internal static InstructionFactory GetFactory(Type type)
		{
			if (_factories == null)
			{
				_factories = new Dictionary<Type, InstructionFactory>() {
                    { typeof(object), InstructionFactory<object>.Factory },
                    { typeof(bool), InstructionFactory<bool>.Factory },
                    { typeof(byte), InstructionFactory<byte>.Factory },
                    { typeof(sbyte), InstructionFactory<sbyte>.Factory },
                    { typeof(short), InstructionFactory<short>.Factory },
                    { typeof(ushort), InstructionFactory<ushort>.Factory },
                    { typeof(int), InstructionFactory<int>.Factory },
                    { typeof(uint), InstructionFactory<uint>.Factory },
                    { typeof(long), InstructionFactory<long>.Factory },
                    { typeof(ulong), InstructionFactory<ulong>.Factory },
                    { typeof(float), InstructionFactory<float>.Factory },
                    { typeof(double), InstructionFactory<double>.Factory },
                    { typeof(char), InstructionFactory<char>.Factory },
                    { typeof(string), InstructionFactory<string>.Factory },
                    { typeof(BigInt), InstructionFactory<BigInt>.Factory },
                    { typeof(BigInteger), InstructionFactory<BigInteger>.Factory },
                    { typeof(SymbolId), InstructionFactory<SymbolId>.Factory },     
                };
			}
			lock (_factories)
			{
				InstructionFactory factory;
				if (!_factories.TryGetValue(type, out factory))
					_factories[type] = factory = (InstructionFactory)typeof(InstructionFactory<>).MakeGenericType(type).GetField("Factory").GetValue(null);
				return factory;
			}
		}

		/// <summary>�z��̗v�f���擾���閽�߂��쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal abstract Instruction GetArrayItem();

		/// <summary>�z��̗v�f��ݒ肷�閽�߂��쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal abstract Instruction SetArrayItem();

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ��\�ł��邩�ǂ����𔻒f���閽�߂��쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal abstract Instruction TypeIs();

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�^�ւ̕ϊ������݁A���s�����ꍇ�� <c>null</c> ��Ԃ����߂��쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal abstract Instruction TypeAs();

		/// <summary>�^�̊���l���擾���閽�߂��쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal abstract Instruction DefaultValue();

		/// <summary>�w�肳�ꂽ���������z����쐬���閽�߂��쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal abstract Instruction NewArray();

		/// <summary>�w�肳�ꂽ���̗v�f�ɂ���ĐV�����쐬���ꂽ�z������������閽�߂��쐬���܂��B</summary>
		/// <param name="elementCount">�z��̏������Ɏg�p����v�f�̐����w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal abstract Instruction NewArrayInit(int elementCount);
	}

	/// <summary>�^�ɑΉ����閽�߂̍쐬���x�����܂��B<see cref="InstructionFactory"/> �N���X�̊���̎�����񋟂��܂��B</summary>
	/// <typeparam name="T">���߂Ɏg�p�����^���w�肵�܂��B</typeparam>
	public sealed class InstructionFactory<T> : InstructionFactory
	{
		/// <summary>���̃N���X�̃C���X�^���X�������܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly InstructionFactory Factory = new InstructionFactory<T>();

		Instruction _getArrayItem;
		Instruction _setArrayItem;
		Instruction _typeIs;
		Instruction _defaultValue;
		Instruction _newArray;
		Instruction _typeAs;

		InstructionFactory() { }

		/// <summary>�z��̗v�f���擾���閽�߂��쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal override Instruction GetArrayItem() { return _getArrayItem ?? (_getArrayItem = new GetArrayItemInstruction<T>()); }

		/// <summary>�z��̗v�f��ݒ肷�閽�߂��쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal override Instruction SetArrayItem() { return _setArrayItem ?? (_setArrayItem = new SetArrayItemInstruction<T>()); }

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ��\�ł��邩�ǂ����𔻒f���閽�߂��쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal override Instruction TypeIs() { return _typeIs ?? (_typeIs = new TypeIsInstruction<T>()); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�^�ւ̕ϊ������݁A���s�����ꍇ�� <c>null</c> ��Ԃ����߂��쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal override Instruction TypeAs() { return _typeAs ?? (_typeAs = new TypeAsInstruction<T>()); }

		/// <summary>�^�̊���l���擾���閽�߂��쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal override Instruction DefaultValue() { return _defaultValue ?? (_defaultValue = new DefaultValueInstruction<T>()); }

		/// <summary>�w�肳�ꂽ���������z����쐬���閽�߂��쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal override Instruction NewArray() { return _newArray ?? (_newArray = new NewArrayInstruction<T>()); }

		/// <summary>�w�肳�ꂽ���̗v�f�ɂ���ĐV�����쐬���ꂽ�z������������閽�߂��쐬���܂��B</summary>
		/// <param name="elementCount">�z��̏������Ɏg�p����v�f�̐����w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ���߂�\�� <see cref="Instruction"/>�B</returns>
		protected internal override Instruction NewArrayInit(int elementCount) { return new NewArrayInitInstruction<T>(elementCount); }
	}
}
