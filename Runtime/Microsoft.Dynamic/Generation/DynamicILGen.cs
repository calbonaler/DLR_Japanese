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
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation
{
	/// <summary>���I�ȃ��\�b�h�̖{�̂̍\�z���x�����A���\�b�h�܂��̓f���Q�[�g���擾�ł���悤�ɂ��܂��B</summary>
	public abstract class DynamicILGen
	{
		/// <summary>���I���\�b�h�̖{�̂��\�z�ł��� <see cref="ILGenerator"/> ���g�p���āA<see cref="Microsoft.Scripting.Generation.DynamicILGen"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="il">���I���\�b�h�̖{�̂��\�z�ł��� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		internal DynamicILGen(ILGenerator il) { Generator = il; }

		/// <summary>���̓��I���\�b�h���\�z���� <see cref="ILGenerator"/> ���擾���܂��B</summary>
		public ILGenerator Generator { get; private set; }

		/// <summary>���I���\�b�h���������āA�쐬���ꂽ���\�b�h��\���w�肳�ꂽ�^�̃f���Q�[�g���쐬���܂��B</summary>
		/// <typeparam name="TDelegate">�擾����f���Q�[�g�̌^���w�肵�܂��B</typeparam>
		/// <returns>�쐬���ꂽ���\�b�h��\���f���Q�[�g�B</returns>
		public TDelegate CreateDelegate<TDelegate>() where TDelegate : class
		{
			MethodInfo mi;
			return CreateDelegate<TDelegate>(out mi);
		}

		/// <summary>���I���\�b�h���������āA�쐬���ꂽ���\�b�h��\���w�肳�ꂽ�^�̃f���Q�[�g����у��\�b�h�̏����i�[���� <see cref="MethodInfo"/> ���擾���܂��B</summary>
		/// <typeparam name="TDelegate">�擾����f���Q�[�g�̌^���w�肵�܂��B</typeparam>
		/// <param name="mi">�쐬���ꂽ���\�b�h�̏�񂪊i�[����܂��B</param>
		/// <returns>�쐬���ꂽ���\�b�h��\���f���Q�[�g�B</returns>
		public TDelegate CreateDelegate<TDelegate>(out MethodInfo mi) where TDelegate : class
		{
			ContractUtils.Requires(typeof(TDelegate).IsSubclassOf(typeof(Delegate)), "T");
			return (TDelegate)(object)(mi = Finish()).CreateDelegate(typeof(TDelegate));
		}

		/// <summary>���I���\�b�h���������āA�쐬���ꂽ���\�b�h�̏����i�[���� <see cref="MethodInfo"/> ���擾���܂��B</summary>
		/// <returns>�쐬���ꂽ���\�b�h�̏���\�� <see cref="MethodInfo"/>�B</returns>
		public abstract MethodInfo Finish();
	}

	class DynamicILGenMethod : DynamicILGen
	{
		readonly DynamicMethod _dm;

		internal DynamicILGenMethod(DynamicMethod dm) : base(dm.GetILGenerator()) { _dm = dm; }

		public override MethodInfo Finish() { return _dm; }
	}

	class DynamicILGenType : DynamicILGen
	{
		readonly TypeBuilder _tb;
		readonly MethodBuilder _mb;

		internal DynamicILGenType(TypeBuilder tb, MethodBuilder mb) : base(mb.GetILGenerator())
		{
			_tb = tb;
			_mb = mb;
		}

		public override MethodInfo Finish() { return _tb.CreateType().GetMethod(_mb.Name); }
	}
}
