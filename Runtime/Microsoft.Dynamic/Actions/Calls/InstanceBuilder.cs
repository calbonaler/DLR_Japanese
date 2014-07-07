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
using System.Reflection;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>���\�b�h���Ăяo�����Ƃ̂ł���C���^�X�^���X��񋟂��܂��B</summary>
	public class InstanceBuilder
	{
		// Index of actual argument expression or -1 if the instance is null.
		int _index;

		/// <summary>�C���X�^���X��\���������̃C���f�b�N�X���g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.InstanceBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�C���X�^���X��\���������X�g���̎������̃C���f�b�N�X���w�肵�܂��B</param>
		public InstanceBuilder(int index)
		{
			ContractUtils.Requires(index >= -1, "index");
			_index = index;
		}

		/// <summary>���̃C���X�^���X�� <c>null</c> �ł��邩�ǂ����������l���擾���܂��B</summary>
		public virtual bool HasValue { get { return _index != -1; } }

		/// <summary>���̃r���_�ɂ���ď�������ۂ̈����̐����擾���܂��B</summary>
		public virtual int ConsumedArgumentCount { get { return 1; } }

		/// <summary>�C���X�^���X�̒l��񋟂��� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="method">�Ăяo�����\�b�h������ <see cref="MethodInfo"/> ���w�肵�܂��B</param>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>�C���X�^���X�̒l��񋟂��� <see cref="Expression"/>�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")] // TODO
		protected internal virtual Expression ToExpression(ref MethodInfo method, OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			if (_index == -1)
				return AstUtils.Constant(null);
			ContractUtils.Requires(hasBeenUsed.Length == args.Length, "hasBeenUsed");
			ContractUtils.Requires(_index < args.Length, "args");
			ContractUtils.Requires(!hasBeenUsed[_index], "hasBeenUsed");
			hasBeenUsed[_index] = true;
			return resolver.Convert(args.Objects[_index], args.Types[_index], null, (method = GetCallableMethod(args, method)).DeclaringType);
		}

		/// <summary>�C���X�^���X�̒l��񋟂���f���Q�[�g��Ԃ��܂��B</summary>
		/// <param name="method">�Ăяo�����\�b�h������ <see cref="MethodInfo"/> ���w�肵�܂��B</param>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>�C���X�^���X�̒l��񋟂���f���Q�[�g�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")] // TODO
		protected internal virtual Func<object[], object> ToDelegate(ref MethodInfo method, OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			if (_index == -1)
				return _ => null;
			var conv = resolver.GetConvertor(_index + 1, args.Objects[_index], null, (method = GetCallableMethod(args, method)).DeclaringType);
			if (conv != null)
				return conv;
			return (Func<object[], object>)Delegate.CreateDelegate(typeof(Func<object[], object>), _index + 1, new Func<object, object[], object>(ArgBuilder.ArgumentRead).Method);
		}

		MethodInfo GetCallableMethod(RestrictedArguments args, MethodInfo method)
		{
			// �s���̃��\�b�h���Q�Ƃ��Ă���Ȃ�΁A�������̂��Ăяo�����Ƃ��ł�����ł�����悢���\�b�h�̌��������݂܂��B
			// ���ꂪ���s�����ꍇ�A�Ƃɂ����o�C���h���s���܂��B�܂�A�s���̃��\�b�h���t�B���^����̂͌Ăяo�����̐ӔC�ƂȂ�܂��B
			// �^�Ɏ������ꂽ�C���^�[�t�F�C�X��ʂ��ăA�N�Z�X���\�Ƃ��ꂽ���\�b�h�ɃA�N�Z�X�ł���悤�A����̓��^�C���X�^���X�̐����^���g�p���܂��B
			// ���̑��̏ꍇ�A�^�������^�ł�������A���\�b�h���A�N�Z�X�s�\�ł������肷��\��������܂��B
			return CompilerHelpers.TryGetCallableMethod(args.Objects[_index].LimitType, method);
		}
	}
}
