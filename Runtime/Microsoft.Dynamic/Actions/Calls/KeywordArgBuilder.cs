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
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>�L�[���[�h�����ɑ΂���l��񋟂��� <see cref="ArgBuilder"/> �ł��B
	/// 
	/// <see cref="KeywordArgBuilder"/> �̓G�~�b�g���ɂ�����ʒu�����[�U�[����n���ꂽ�L�[���[�h�������ɂ����鏉���I�t�Z�b�g�A�L�[���[�h�����̐��A�����̑�������v�Z���܂��B
	/// ���̌�A�P��̐��m�Ȉ����݂̂��󂯓�����ɂȂ� <see cref="ArgBuilder"/> �ɏ������Ϗ����܂��B
	/// �G�~�b�g���܂ňʒu�̌v�Z��x�������邱�ƂŁA���[�U�[����n���ꂽ���m�Ȉ����̐���m��Ȃ��Ă����\�b�h�o�C���f�B���O�������ł���悤�ɂȂ�܂��B
	/// ���������āA���\�b�h�o�C���_�̓��\�b�h�I�[�o�[���[�h�Z�b�g�ƃL�[���[�h���ɂ݈̂ˑ����邱�ƂɂȂ�A���[�U�[�����ւ̈ˑ��͂Ȃ��Ȃ�܂��B
	/// ���[�U�[�����̐��͎��O�Ɍ���ł��܂����A���݂̃��\�b�h�o�C���_�͂��̌`�����Ƃ��Ă��܂���B
	/// </summary>
	sealed class KeywordArgBuilder : ArgBuilder
	{
		readonly int _kwArgCount, _kwArgIndex;
		readonly ArgBuilder _builder;

		/// <summary>��ɂȂ� <see cref="ArgBuilder"/>�A�L�[���[�h�����̐�����уL�[���[�h�������̈ʒu���g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.KeywordArgBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="builder">��ɂȂ� <see cref="ArgBuilder"/> ���w�肵�܂��B</param>
		/// <param name="kwArgCount">�L�[���[�h�����̐����w�肵�܂��B</param>
		/// <param name="kwArgIndex">�L�[���[�h�������̌��݂̈����̈ʒu���w�肵�܂��B</param>
		public KeywordArgBuilder(ArgBuilder builder, int kwArgCount, int kwArgIndex) : base(builder.ParameterInfo)
		{
			Debug.Assert(BuilderExpectsSingleParameter(builder));
			Debug.Assert(builder.ConsumedArgumentCount == 1);
			_builder = builder;
			Debug.Assert(kwArgIndex < kwArgCount);
			_kwArgCount = kwArgCount;
			_kwArgIndex = kwArgIndex;
		}

		/// <summary>���̈����̗D�揇�ʂ��擾���܂��B</summary>
		public override int Priority { get { return _builder.Priority; } }

		/// <summary>���̃r���_�ɂ���ď�������ۂ̈����̐����擾���܂��B</summary>
		public override int ConsumedArgumentCount { get { return 1; } }

		/// <summary>�w�肳�ꂽ <see cref="ArgBuilder"/> ���P��̈����݂̂����Ƃ�ۏ؂��܂��B</summary>
		/// <param name="builder">���f���� <see cref="ArgBuilder"/> ���w�肵�܂��B</param>
		internal static bool BuilderExpectsSingleParameter(ArgBuilder builder) { return ((SimpleArgBuilder)builder).Index == 0; }

		/// <summary>�����ɓn�����l��񋟂��� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>�����ɓn�����l��񋟂��� <see cref="Expression"/>�B�������X�L�b�v���ꂽ�ꍇ (�܂�A�Ăяo����ɓn����Ȃ��ꍇ) <c>null</c> ��Ԃ��܂��B</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			Debug.Assert(BuilderExpectsSingleParameter(_builder));
			int index = GetKeywordIndex(args.Length);
			Debug.Assert(!hasBeenUsed[index]);
			hasBeenUsed[index] = true;
			return _builder.ToExpression(resolver, MakeRestrictedArg(args, index), new bool[1]);
		}

		/// <summary>�����ɑ΂��ėv�������^���擾���܂��B<see cref="ArgBuilder"/> ������������Ȃ��ꍇ�� <c>null</c> ���Ԃ���܂��B</summary>
		public override Type Type { get { return _builder.Type; } }

		/// <summary>�������Ԗߒl�𐶐����� (ref ���邢�� out �̂悤��) �ꍇ�A�Ăяo�����ɒǉ��ŕԂ����l��񋟂��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <returns>�Ăяo����Œǉ��ŕԂ����l��񋟂��� <see cref="Expression"/>�B</returns>
		internal override Expression ToReturnExpression(OverloadResolver resolver) { return _builder.ToReturnExpression(resolver); }

		/// <summary>���\�b�h�Ăяo���̌�ɒ񋟂��ꂽ�l���X�V���� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <returns>�񋟂��ꂽ�l���X�V���� <see cref="Expression"/>�B�X�V���s�v�ȏꍇ�� <c>null</c> ��Ԃ��܂��B</returns>
		internal override Expression UpdateFromReturn(OverloadResolver resolver, RestrictedArguments args) { return _builder.UpdateFromReturn(resolver, MakeRestrictedArg(args, GetKeywordIndex(args.Length))); }

		static RestrictedArguments MakeRestrictedArg(RestrictedArguments args, int index) { return new RestrictedArguments(new[] { args.Objects[index] }, new[] { args.Types[index] }, false); }

		int GetKeywordIndex(int paramCount) { return paramCount - _kwArgCount + _kwArgIndex; }

		/// <summary>�Q�Ǝ��̈����ɂ���ēn��������\�Ȓl���擾���܂��B�Ăяo����͍X�V���ꂽ�l���i�[����܂��B</summary>
		internal override Expression ByRefArgument { get { return _builder.ByRefArgument; } }

		/// <summary>�w�肳�ꂽ�����ɑ΂��邱�� <see cref="ArgBuilder"/> �̃R�s�[�𐶐����܂��B</summary>
		/// <param name="newType">�R�s�[����ɂ��鉼�������w�肵�܂��B</param>
		/// <returns>�R�s�[���ꂽ <see cref="ArgBuilder"/>�B</returns>
		public override ArgBuilder Clone(ParameterInfo newType) { return new KeywordArgBuilder(_builder.Clone(newType), _kwArgCount, _kwArgIndex); }
	}
}
