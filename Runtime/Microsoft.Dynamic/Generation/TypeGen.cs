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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation
{
	/// <summary>�^�̍\�z���x�����܂��B</summary>
	public sealed class TypeGen
	{
		ILGenerator _initGen; // The IL generator for the .cctor()

		/// <summary>�^�������q (cctor) �̖{�̂��\�z���� <see cref="ILGenerator"/> ���擾���܂��B</summary>
		public ILGenerator TypeInitializer
		{
			get
			{
				if (_initGen == null)
					_initGen = TypeBuilder.DefineTypeInitializer().GetILGenerator();
				return _initGen;
			}
		}

		/// <summary>���̌^�������Ă���A�Z���u�����\�z���Ă��� <see cref="AssemblyGen"/> ���擾���܂��B</summary>
		internal AssemblyGen AssemblyGen { get; private set; }

		/// <summary>�^�����ڍׂɒ�`�ł��� <see cref="TypeBuilder"/> ���擾���܂��B</summary>
		public TypeBuilder TypeBuilder { get; private set; }

		/// <summary>�w�肳�ꂽ <see cref="AssemblyGen"/> ����� <see cref="TypeBuilder"/> ���g�p���āA<see cref="Microsoft.Scripting.Generation.TypeGen"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="myAssembly">���̌^�������Ă���A�Z���u�����\�z���Ă��� <see cref="AssemblyGen"/> ���w�肵�܂��B</param>
		/// <param name="myType">���̌^���\�z���� <see cref="TypeBuilder"/> ���w�肵�܂��B</param>
		public TypeGen(AssemblyGen myAssembly, TypeBuilder myType)
		{
			Assert.NotNull(myAssembly, myType);
			AssemblyGen = myAssembly;
			TypeBuilder = myType;
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return TypeBuilder.ToString(); }

		/// <summary>�^�̍\�z���������āA���̌^�� <see cref="System.Type"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <returns>���̌^��\�� <see cref="System.Type"/> �I�u�W�F�N�g�B</returns>
		public Type FinishType()
		{
			if (_initGen != null)
				_initGen.Emit(OpCodes.Ret);
			var ret = TypeBuilder.CreateType();
			Debug.WriteLine("finished: " + ret.FullName);
			return ret;
		}

		/// <summary>���̌^�Ɏw�肳�ꂽ�^����і��O�����p�u���b�N�ȐÓI�t�B�[���h��ǉ����܂��B</summary>
		/// <param name="fieldType">�ÓI�t�B�[���h�̌^���w�肵�܂��B</param>
		/// <param name="name">�ÓI�t�B�[���h�̖��O���w�肵�܂��B</param>
		/// <returns>�ǉ����ꂽ�ÓI�t�B�[���h���\�z���� <see cref="FieldBuilder"/>�B</returns>
		public FieldBuilder AddStaticField(Type fieldType, string name) { return TypeBuilder.DefineField(name, fieldType, FieldAttributes.Public | FieldAttributes.Static); }

		/// <summary>���̌^�Ɏw�肳�ꂽ�^����і��O�����ÓI�t�B�[���h��ǉ����܂��B</summary>
		/// <param name="fieldType">�ÓI�t�B�[���h�̌^���w�肵�܂��B</param>
		/// <param name="attributes">�ÓI�t�B�[���h�̖��O���w�肵�܂��B</param>
		/// <param name="name">�ÓI�t�B�[���h�̑�����\�� <see cref="FieldAttributes"/> ���w�肵�܂��B</param>
		/// <returns>�ǉ����ꂽ�ÓI�t�B�[���h���\�z���� <see cref="FieldBuilder"/>�B</returns>
		public FieldBuilder AddStaticField(Type fieldType, FieldAttributes attributes, string name) { return TypeBuilder.DefineField(name, fieldType, attributes | FieldAttributes.Static); }

		/// <summary>���̌^�Ɏw�肳�ꂽ�C���^�[�t�F�C�X ���\�b�h�̖����������`���܂��B</summary>
		/// <param name="baseMethod">��������C���^�[�t�F�C�X�̃��\�b�h��\�� <see cref="MethodInfo"/> ���w�肵�܂��B</param>
		/// <returns>�C���^�[�t�F�C�X�̖����������\�b�h�{�̂��\�z�ł��� <see cref="ILGenerator"/>�B</returns>
		public ILGenerator DefineExplicitInterfaceImplementation(MethodInfo baseMethod)
		{
			ContractUtils.RequiresNotNull(baseMethod, "baseMethod");
			var mb = TypeBuilder.DefineMethod(
				baseMethod.DeclaringType.Name + "." + baseMethod.Name,
				baseMethod.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.Public) | MethodAttributes.NewSlot | MethodAttributes.Final,
				baseMethod.ReturnType,
				baseMethod.GetParameters().Select(p => p.ParameterType).ToArray()
			);
			TypeBuilder.DefineMethodOverride(mb, baseMethod);
			return mb.GetILGenerator();
		}

		/// <summary>���̌^�Ŏw�肳�ꂽ���N���X�̃��\�b�h���I�[�o�[���C�h���܂��B</summary>
		/// <param name="baseMethod">�I�[�o�[���C�h������N���X�̃��\�b�h��\�� <see cref="MethodInfo"/> ���w�肵�܂��B</param>
		/// <returns>���\�b�h �I�[�o�[���C�h�̖{�̂��\�z�ł��� <see cref="ILGenerator"/>�B</returns>
		public ILGenerator DefineMethodOverride(MethodInfo baseMethod)
		{
			var mb = TypeBuilder.DefineMethod(baseMethod.Name, baseMethod.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.ReservedMask), baseMethod.ReturnType, baseMethod.GetParameters().Select(p => p.ParameterType).ToArray());
			TypeBuilder.DefineMethodOverride(mb, baseMethod);
			return mb.GetILGenerator();
		}
	}
}
