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
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Interpreter;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast
{
	/// <summary>�C���^�v���^�ɂ���ĔF������铮�I�����\���܂��B</summary>
	public abstract class LightDynamicExpression : Expression, IInstructionProvider
	{
		/// <summary>�w�肳�ꂽ�o�C���_�[���g�p���āA<see cref="Microsoft.Scripting.Ast.LightDynamicExpression"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		protected LightDynamicExpression(CallSiteBinder binder)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			Binder = binder;
		}

		/// <summary>
		/// �m�[�h�����P���ȃm�[�h�ɕό`�ł��邱�Ƃ������܂��B
		/// ���ꂪ <c>true</c> ��Ԃ��ꍇ�A<see cref="Reduce"/> ���Ăяo���ĒP�������ꂽ�`���𐶐��ł��܂��B
		/// </summary>
		public sealed override bool CanReduce { get { return true; } }

		/// <summary>���I�T�C�g�̎��s���̓�������肷�� <see cref="System.Runtime.CompilerServices.CallSiteBinder"/> ���擾���܂��B</summary>
		public CallSiteBinder Binder { get; private set; }

		/// <summary>���̎��̃m�[�h�^��Ԃ��܂��B �g���m�[�h�́A���̃��\�b�h���I�[�o�[���C�h����Ƃ��A<see cref="System.Linq.Expressions.ExpressionType.Extension"/> ��Ԃ��K�v������܂��B</summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>���� <see cref="System.Linq.Expressions.Expression"/> ���\�����̐ÓI�Ȍ^���擾���܂��B</summary>
		public override Type Type { get { return typeof(object); } }

		/// <summary>�w�肳�ꂽ�C���^�v���^�ɂ��̃I�u�W�F�N�g���\�����߂�ǉ����܂��B</summary>
		/// <param name="compiler">���߂�ǉ�����C���^�v���^���w�肵�܂��B</param>
		public void AddInstructions(LightCompiler compiler)
		{
			var instr = DynamicInstructionN.CreateUntypedInstruction(Binder, ArgumentCount);
			if (instr == null)
			{
				var lightBinder = Binder as ILightCallSiteBinder;
				if (lightBinder == null || !lightBinder.AcceptsArgumentArray)
				{
					compiler.Compile(Reduce());
					return;
				}
				Debug.Assert(Type == typeof(object));
				instr = new DynamicSplatInstruction(ArgumentCount, CallSite<Func<CallSite, ArgumentArray, object>>.Create(Binder));
			}
			for (int i = 0; i < ArgumentCount; i++)
				compiler.Compile(GetArgument(i));
			compiler.Instructions.Emit(instr);
		}

		/// <summary>
		/// ���̃m�[�h�����P���Ȏ��ɕό`���܂��B
		/// <see cref="CanReduce"/> �� <c>true</c> ��Ԃ��ꍇ�A����͗L���Ȏ���Ԃ��܂��B
		/// ���̃��\�b�h�́A���ꎩ�̂��P��������K�v������ʂ̃m�[�h��Ԃ��ꍇ������܂��B
		/// </summary>
		/// <returns>�P�������ꂽ���B</returns>
		public abstract override Expression Reduce();

		/// <summary>���̃m�[�h���\�����I����̈����̌����擾���܂��B</summary>
		protected abstract int ArgumentCount { get; }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�ɑ��݂��铮�I����̈������擾���܂��B</summary>
		/// <param name="index">���I����̈������擾���� 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɑ��݂��铮�I����̈����B</returns>
		protected abstract Expression GetArgument(int index);
	}

	#region Specialized Subclasses

	/// <summary>�C���^�v���^�ɂ���ĔF������� 1 �̈������Ƃ铮�I�����\���܂��B</summary>
	public class LightDynamicExpression1 : LightDynamicExpression
	{
		/// <summary>�w�肳�ꂽ�o�C���_�[�ƈ������g�p���āA<see cref="Microsoft.Scripting.Ast.LightDynamicExpression1"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="arg0">���I����� 1 �Ԗڂ̈������w�肵�܂��B</param>
		protected internal LightDynamicExpression1(CallSiteBinder binder, Expression arg0) : base(binder)
		{
			ContractUtils.RequiresNotNull(arg0, "arg0");
			Argument0 = arg0;
		}

		/// <summary>
		/// ���̃m�[�h�����P���Ȏ��ɕό`���܂��B
		/// <see cref="Expression.CanReduce"/> �� <c>true</c> ��Ԃ��ꍇ�A����͗L���Ȏ���Ԃ��܂��B
		/// ���̃��\�b�h�́A���ꎩ�̂��P��������K�v������ʂ̃m�[�h��Ԃ��ꍇ������܂��B
		/// </summary>
		/// <returns>�P�������ꂽ���B</returns>
		public override Expression Reduce() { return Expression.Dynamic(Binder, Type, Argument0); }

		/// <summary>���̃m�[�h���\�����I����̈����̌����擾���܂��B</summary>
		protected sealed override int ArgumentCount { get { return 1; } }

		/// <summary>���I����� 1 �Ԗڂ̈������擾���܂��B</summary>
		public Expression Argument0 { get; private set; }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�ɑ��݂��铮�I����̈������擾���܂��B</summary>
		/// <param name="index">���I����̈������擾���� 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɑ��݂��铮�I����̈����B</returns>
		protected sealed override Expression GetArgument(int index)
		{
			switch (index)
			{
				case 0: return Argument0;
				default: throw Assert.Unreachable;
			}
		}
	}

	class LightTypedDynamicExpression1 : LightDynamicExpression1
	{
		readonly Type _returnType;

		internal LightTypedDynamicExpression1(CallSiteBinder binder, Type returnType, Expression arg0) : base(binder, arg0)
		{
			ContractUtils.RequiresNotNull(returnType, "returnType");
			_returnType = returnType;
		}

		public sealed override Type Type { get { return _returnType; } }
	}

	/// <summary>�C���^�v���^�ɂ���ĔF������� 2 �̈������Ƃ铮�I�����\���܂��B</summary>
	public class LightDynamicExpression2 : LightDynamicExpression
	{
		/// <summary>�w�肳�ꂽ�o�C���_�[�ƈ������g�p���āA<see cref="Microsoft.Scripting.Ast.LightDynamicExpression2"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="arg0">���I����� 1 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg1">���I����� 2 �Ԗڂ̈������w�肵�܂��B</param>
		protected internal LightDynamicExpression2(CallSiteBinder binder, Expression arg0, Expression arg1) : base(binder)
		{
			ContractUtils.RequiresNotNull(arg0, "arg0");
			ContractUtils.RequiresNotNull(arg1, "arg1");
			Argument0 = arg0;
			Argument1 = arg1;
		}

		/// <summary>
		/// ���̃m�[�h�����P���Ȏ��ɕό`���܂��B
		/// <see cref="Expression.CanReduce"/> �� <c>true</c> ��Ԃ��ꍇ�A����͗L���Ȏ���Ԃ��܂��B
		/// ���̃��\�b�h�́A���ꎩ�̂��P��������K�v������ʂ̃m�[�h��Ԃ��ꍇ������܂��B
		/// </summary>
		/// <returns>�P�������ꂽ���B</returns>
		public override Expression Reduce() { return Expression.Dynamic(Binder, Type, Argument0, Argument1); }

		/// <summary>���̃m�[�h���\�����I����̈����̌����擾���܂��B</summary>
		protected override int ArgumentCount { get { return 2; } }

		/// <summary>���I����� 1 �Ԗڂ̈������擾���܂��B</summary>
		public Expression Argument0 { get; private set; }

		/// <summary>���I����� 2 �Ԗڂ̈������擾���܂��B</summary>
		public Expression Argument1 { get; private set; }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�ɑ��݂��铮�I����̈������擾���܂��B</summary>
		/// <param name="index">���I����̈������擾���� 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɑ��݂��铮�I����̈����B</returns>
		protected override Expression GetArgument(int index)
		{
			switch (index)
			{
				case 0: return Argument0;
				case 1: return Argument1;
				default: throw Assert.Unreachable;
			}
		}
	}

	class LightTypedDynamicExpression2 : LightDynamicExpression2
	{
		readonly Type _returnType;

		internal LightTypedDynamicExpression2(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1) : base(binder, arg0, arg1)
		{
			ContractUtils.RequiresNotNull(returnType, "returnType");
			_returnType = returnType;
		}

		public sealed override Type Type { get { return _returnType; } }
	}

	/// <summary>�C���^�v���^�ɂ���ĔF������� 3 �̈������Ƃ铮�I�����\���܂��B</summary>
	public class LightDynamicExpression3 : LightDynamicExpression
	{
		/// <summary>�w�肳�ꂽ�o�C���_�[�ƈ������g�p���āA<see cref="Microsoft.Scripting.Ast.LightDynamicExpression3"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="arg0">���I����� 1 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg1">���I����� 2 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg2">���I����� 3 �Ԗڂ̈������w�肵�܂��B</param>
		protected internal LightDynamicExpression3(CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2) : base(binder)
		{
			ContractUtils.RequiresNotNull(arg0, "arg0");
			ContractUtils.RequiresNotNull(arg1, "arg1");
			ContractUtils.RequiresNotNull(arg2, "arg2");
			Argument0 = arg0;
			Argument1 = arg1;
			Argument2 = arg2;
		}

		/// <summary>
		/// ���̃m�[�h�����P���Ȏ��ɕό`���܂��B
		/// <see cref="Expression.CanReduce"/> �� <c>true</c> ��Ԃ��ꍇ�A����͗L���Ȏ���Ԃ��܂��B
		/// ���̃��\�b�h�́A���ꎩ�̂��P��������K�v������ʂ̃m�[�h��Ԃ��ꍇ������܂��B
		/// </summary>
		/// <returns>�P�������ꂽ���B</returns>
		public override Expression Reduce() { return Expression.Dynamic(Binder, Type, Argument0, Argument1, Argument2); }

		/// <summary>���̃m�[�h���\�����I����̈����̌����擾���܂��B</summary>
		protected sealed override int ArgumentCount { get { return 3; } }

		/// <summary>���I����� 1 �Ԗڂ̈������擾���܂��B</summary>
		public Expression Argument0 { get; private set; }

		/// <summary>���I����� 2 �Ԗڂ̈������擾���܂��B</summary>
		public Expression Argument1 { get; private set; }

		/// <summary>���I����� 3 �Ԗڂ̈������擾���܂��B</summary>
		public Expression Argument2 { get; private set; }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�ɑ��݂��铮�I����̈������擾���܂��B</summary>
		/// <param name="index">���I����̈������擾���� 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɑ��݂��铮�I����̈����B</returns>
		protected sealed override Expression GetArgument(int index)
		{
			switch (index)
			{
				case 0: return Argument0;
				case 1: return Argument1;
				case 2: return Argument2;
				default: throw Assert.Unreachable;
			}
		}
	}

	class LightTypedDynamicExpression3 : LightDynamicExpression3
	{
		readonly Type _returnType;

		internal LightTypedDynamicExpression3(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2) : base(binder, arg0, arg1, arg2)
		{
			ContractUtils.RequiresNotNull(returnType, "returnType");
			_returnType = returnType;
		}

		public sealed override Type Type { get { return _returnType; } }
	}

	/// <summary>�C���^�v���^�ɂ���ĔF������� 4 �̈������Ƃ铮�I�����\���܂��B</summary>
	public class LightDynamicExpression4 : LightDynamicExpression
	{
		/// <summary>�w�肳�ꂽ�o�C���_�[�ƈ������g�p���āA<see cref="Microsoft.Scripting.Ast.LightDynamicExpression4"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="arg0">���I����� 1 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg1">���I����� 2 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg2">���I����� 3 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg3">���I����� 4 �Ԗڂ̈������w�肵�܂��B</param>
		protected internal LightDynamicExpression4(CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2, Expression arg3) : base(binder)
		{
			ContractUtils.RequiresNotNull(arg0, "arg0");
			ContractUtils.RequiresNotNull(arg1, "arg1");
			ContractUtils.RequiresNotNull(arg2, "arg2");
			ContractUtils.RequiresNotNull(arg3, "arg3");
			Argument0 = arg0;
			Argument1 = arg1;
			Argument2 = arg2;
			Argument3 = arg3;
		}

		/// <summary>
		/// ���̃m�[�h�����P���Ȏ��ɕό`���܂��B
		/// <see cref="Expression.CanReduce"/> �� <c>true</c> ��Ԃ��ꍇ�A����͗L���Ȏ���Ԃ��܂��B
		/// ���̃��\�b�h�́A���ꎩ�̂��P��������K�v������ʂ̃m�[�h��Ԃ��ꍇ������܂��B
		/// </summary>
		/// <returns>�P�������ꂽ���B</returns>
		public override Expression Reduce() { return Expression.Dynamic(Binder, Type, Argument0, Argument1, Argument2, Argument3); }

		/// <summary>���̃m�[�h���\�����I����̈����̌����擾���܂��B</summary>
		protected sealed override int ArgumentCount { get { return 4; } }

		/// <summary>���I����� 1 �Ԗڂ̈������擾���܂��B</summary>
		public Expression Argument0 { get; private set; }

		/// <summary>���I����� 2 �Ԗڂ̈������擾���܂��B</summary>
		public Expression Argument1 { get; private set; }

		/// <summary>���I����� 3 �Ԗڂ̈������擾���܂��B</summary>
		public Expression Argument2 { get; private set; }

		/// <summary>���I����� 4 �Ԗڂ̈������擾���܂��B</summary>
		public Expression Argument3 { get; private set; }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�ɑ��݂��铮�I����̈������擾���܂��B</summary>
		/// <param name="index">���I����̈������擾���� 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɑ��݂��铮�I����̈����B</returns>
		protected sealed override Expression GetArgument(int index)
		{
			switch (index)
			{
				case 0: return Argument0;
				case 1: return Argument1;
				case 2: return Argument2;
				case 3: return Argument3;
				default: throw Assert.Unreachable;
			}
		}
	}

	class LightTypedDynamicExpression4 : LightDynamicExpression4
	{
		readonly Type _returnType;

		internal LightTypedDynamicExpression4(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2, Expression arg3) : base(binder, arg0, arg1, arg2, arg3)
		{
			ContractUtils.RequiresNotNull(returnType, "returnType");
			_returnType = returnType;
		}

		public sealed override Type Type { get { return _returnType; } }
	}

	/// <summary>�C���^�v���^�ɂ���ĔF�������C�ӌ̈������Ƃ錋�ʌ^���w�肳�ꂽ���I�����\���܂��B</summary>
	public class LightTypedDynamicExpressionN : LightDynamicExpression
	{
		readonly Type _returnType;

		/// <summary>�w�肳�ꂽ�o�C���_�[�ƈ������g�p���āA<see cref="Microsoft.Scripting.Ast.LightTypedDynamicExpressionN"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="returnType">���I����̌��ʌ^���w�肵�܂��B</param>
		/// <param name="args">���I����̈������w�肵�܂��B</param>
		protected internal LightTypedDynamicExpressionN(CallSiteBinder binder, Type returnType, IList<Expression> args) : base(binder)
		{
			ContractUtils.RequiresNotNull(returnType, "returnType");
			ContractUtils.RequiresNotEmpty(args, "args");
			Arguments = args;
			_returnType = returnType;
		}

		/// <summary>
		/// ���̃m�[�h�����P���Ȏ��ɕό`���܂��B
		/// <see cref="Expression.CanReduce"/> �� <c>true</c> ��Ԃ��ꍇ�A����͗L���Ȏ���Ԃ��܂��B
		/// ���̃��\�b�h�́A���ꎩ�̂��P��������K�v������ʂ̃m�[�h��Ԃ��ꍇ������܂��B
		/// </summary>
		/// <returns>�P�������ꂽ���B</returns>
		public override Expression Reduce() { return Expression.Dynamic(Binder, Type, Arguments); }

		/// <summary>���̃m�[�h���\�����I����̈����̌����擾���܂��B</summary>
		protected sealed override int ArgumentCount { get { return Arguments.Count; } }

		/// <summary>���� <see cref="System.Linq.Expressions.Expression"/> ���\�����̐ÓI�Ȍ^���擾���܂��B</summary>
		public sealed override Type Type { get { return _returnType; } }

		/// <summary>���I����̈������擾���܂��B</summary>
		public IList<Expression> Arguments { get; private set; }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�ɑ��݂��铮�I����̈������擾���܂��B</summary>
		/// <param name="index">���I����̈������擾���� 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɑ��݂��铮�I����̈����B</returns>
		protected sealed override Expression GetArgument(int index) { return Arguments[index]; }
	}

	#endregion

	public static partial class Utils
	{
		/// <summary>���I����̎��s���o�C���_�[�ƈ������g�p���āA<see cref="LightDynamicExpression"/> ���쐬���܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="arg0">���I����� 1 �Ԗڂ̈������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="LightDynamicExpression"/>�B</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Expression arg0) { return LightDynamic(binder, typeof(object), arg0); }

		/// <summary>���I����̎��s���o�C���_�[�ƈ�������ё���̌��ʌ^���g�p���āA<see cref="LightDynamicExpression"/> ���쐬���܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="returnType">����̌��ʌ^���w�肵�܂��B</param>
		/// <param name="arg0">���I����� 1 �Ԗڂ̈������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="LightDynamicExpression"/>�B</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Type returnType, Expression arg0) { return returnType == typeof(object) ? new LightDynamicExpression1(binder, arg0) : new LightTypedDynamicExpression1(binder, returnType, arg0); }

		/// <summary>���I����̎��s���o�C���_�[�ƈ������g�p���āA<see cref="LightDynamicExpression"/> ���쐬���܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="arg0">���I����� 1 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg1">���I����� 2 �Ԗڂ̈������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="LightDynamicExpression"/>�B</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Expression arg0, Expression arg1) { return LightDynamic(binder, typeof(object), arg0, arg1); }

		/// <summary>���I����̎��s���o�C���_�[�ƈ�������ё���̌��ʌ^���g�p���āA<see cref="LightDynamicExpression"/> ���쐬���܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="returnType">����̌��ʌ^���w�肵�܂��B</param>
		/// <param name="arg0">���I����� 1 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg1">���I����� 2 �Ԗڂ̈������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="LightDynamicExpression"/>�B</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1) { return returnType == typeof(object) ? new LightDynamicExpression2(binder, arg0, arg1) : new LightTypedDynamicExpression2(binder, returnType, arg0, arg1); }

		/// <summary>���I����̎��s���o�C���_�[�ƈ������g�p���āA<see cref="LightDynamicExpression"/> ���쐬���܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="arg0">���I����� 1 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg1">���I����� 2 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg2">���I����� 3 �Ԗڂ̈������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="LightDynamicExpression"/>�B</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2) { return LightDynamic(binder, typeof(object), arg0, arg1, arg2); }

		/// <summary>���I����̎��s���o�C���_�[�ƈ�������ё���̌��ʌ^���g�p���āA<see cref="LightDynamicExpression"/> ���쐬���܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="returnType">����̌��ʌ^���w�肵�܂��B</param>
		/// <param name="arg0">���I����� 1 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg1">���I����� 2 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg2">���I����� 3 �Ԗڂ̈������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="LightDynamicExpression"/>�B</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2) { return returnType == typeof(object) ? new LightDynamicExpression3(binder, arg0, arg1, arg2) : new LightTypedDynamicExpression3(binder, returnType, arg0, arg1, arg2); }

		/// <summary>���I����̎��s���o�C���_�[�ƈ������g�p���āA<see cref="LightDynamicExpression"/> ���쐬���܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="arg0">���I����� 1 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg1">���I����� 2 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg2">���I����� 3 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg3">���I����� 4 �Ԗڂ̈������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="LightDynamicExpression"/>�B</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2, Expression arg3) { return LightDynamic(binder, typeof(object), arg0, arg1, arg2, arg3); }

		/// <summary>���I����̎��s���o�C���_�[�ƈ�������ё���̌��ʌ^���g�p���āA<see cref="LightDynamicExpression"/> ���쐬���܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="returnType">����̌��ʌ^���w�肵�܂��B</param>
		/// <param name="arg0">���I����� 1 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg1">���I����� 2 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg2">���I����� 3 �Ԗڂ̈������w�肵�܂��B</param>
		/// <param name="arg3">���I����� 4 �Ԗڂ̈������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="LightDynamicExpression"/>�B</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2, Expression arg3) { return returnType == typeof(object) ? new LightDynamicExpression4(binder, arg0, arg1, arg2, arg3) : new LightTypedDynamicExpression4(binder, returnType, arg0, arg1, arg2, arg3); }

		/// <summary>���I����̎��s���o�C���_�[�ƈ������g�p���āA<see cref="LightDynamicExpression"/> ���쐬���܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="arguments">���I����̈������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="LightDynamicExpression"/>�B</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, IEnumerable<Expression> arguments) { return LightDynamic(binder, typeof(object), arguments); }

		/// <summary>���I����̎��s���o�C���_�[�ƈ�������ё���̌��ʌ^���g�p���āA<see cref="LightDynamicExpression"/> ���쐬���܂��B</summary>
		/// <param name="binder">���I����̎��s���o�C���_�[���w�肵�܂��B</param>
		/// <param name="returnType">����̌��ʌ^���w�肵�܂��B</param>
		/// <param name="arguments">���I����̈������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="LightDynamicExpression"/>�B</returns>
		public static LightDynamicExpression LightDynamic(CallSiteBinder binder, Type returnType, IEnumerable<Expression> arguments)
		{
			ContractUtils.RequiresNotNull(arguments, "arguments");
			return LightDynamic(binder, returnType, arguments.ToReadOnly());
		}

		static LightDynamicExpression LightDynamic(CallSiteBinder binder, Type returnType, IList<Expression> arguments)
		{
			ContractUtils.RequiresNotNull(arguments, "arguments");
			switch (arguments.Count)
			{
				case 1: return LightDynamic(binder, returnType, arguments[0]);
				case 2: return LightDynamic(binder, returnType, arguments[0], arguments[1]);
				case 3: return LightDynamic(binder, returnType, arguments[0], arguments[1], arguments[2]);
				case 4: return LightDynamic(binder, returnType, arguments[0], arguments[1], arguments[2], arguments[3]);
				default: return new LightTypedDynamicExpressionN(binder, returnType, arguments);
			}
		}
	}
}
