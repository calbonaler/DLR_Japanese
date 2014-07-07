/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * ironruby@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>
	/// �I�[�o�[���[�h�����̂��߂̃��\�b�h�I�[�o�[���[�h�̗v����`���܂��B
	/// ���̃N���X�� <see cref="OverloadResolver"/> �ɉ����̎��s�ɕK�v�ƂȂ郁�^�f�[�^��񋟂��܂��B
	/// </summary>
	/// <remarks>�x��: ���̃N���X�͈ꎞ�I�� API �ł���A�����̃o�[�W�����Ŕj��I�ύX���󂯂�\��������܂��B</remarks>
	[DebuggerDisplay("{(object)ReflectionInfo ?? Name}")]
	public abstract class OverloadInfo
	{
		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A���̃��\�b�h�I�[�o�[���[�h�̖��O���擾���܂��B</summary>
		public abstract string Name { get; }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A���̃��\�b�h�I�[�o�[���[�h�̉������̃��X�g���擾���܂��B</summary>
		public abstract IList<ParameterInfo> Parameters { get; }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�̉������̐����擾���܂��B</summary>
		public virtual int ParameterCount { get { return Parameters.Count; } }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A�߂�l�ɑ΂��� <see cref="System.Reflection.ParameterInfo"/> ���擾���܂��B�R���X�g���N�^�̏ꍇ�� <c>null</c> �ƂȂ�܂��B</summary>
		public abstract ParameterInfo ReturnParameter { get; }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�̎w�肳�ꂽ�C���f�b�N�X�ɂ��鉼������ <c>null</c> �����e���Ȃ����ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="parameterIndex"><c>null</c> �񋖗e���ǂ����𔻒f���鉼�����̃C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�������� <c>null</c> �񋖗e�ł���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public virtual bool ProhibitsNull(int parameterIndex) { return Parameters[parameterIndex].ProhibitsNull(); }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�̎w�肳�ꂽ�C���f�b�N�X�ɂ��鉼������ <c>null</c> �ł���v�f�����e���Ȃ����ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="parameterIndex"><c>null</c> �v�f�񋖗e���ǂ����𔻒f���鉼�����̃C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�������� <c>null</c> �v�f�����e���Ȃ���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public virtual bool ProhibitsNullItems(int parameterIndex) { return Parameters[parameterIndex].ProhibitsNullItems(); }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�̎w�肳�ꂽ�C���f�b�N�X�ɂ��鉼�������z������ł��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="parameterIndex">�z��������ǂ����𔻒f���鉼�����̃C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���������z������ł���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public virtual bool IsParamArray(int parameterIndex) { return Parameters[parameterIndex].IsParamArray(); }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�̎w�肳�ꂽ�C���f�b�N�X�ɂ��鉼���������������ł��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="parameterIndex">�����������ǂ����𔻒f���鉼�����̃C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�����������������ł���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public virtual bool IsParamDictionary(int parameterIndex) { return Parameters[parameterIndex].IsParamDictionary(); }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A���̃��\�b�h�I�[�o�[���[�h���錾����Ă���^���擾���܂��B</summary>
		public abstract Type DeclaringType { get; }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A���̃��\�b�h�I�[�o�[���[�h�̖߂�l�̌^���擾���܂��B�R���X�g���N�^�̏ꍇ�� <c>null</c> �ɂȂ�܂��B</summary>
		public abstract Type ReturnType { get; }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A���̃��\�b�h�I�[�o�[���[�h�̑����������t���O���擾���܂��B</summary>
		public abstract MethodAttributes Attributes { get; }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A���̃��\�b�h�I�[�o�[���[�h���R���X�g���N�^�ł��邩�ǂ����������l���擾���܂��B</summary>
		public abstract bool IsConstructor { get; }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A���̃��\�b�h�I�[�o�[���[�h���g�����\�b�h�ł��邩�ǂ����������l���擾���܂��B</summary>
		public abstract bool IsExtension { get; }

		/// <summary>
		/// �h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A���̃��\�b�h�I�[�o�[���[�h�̈����̐����ςł��邩�ǂ����������l���擾���܂��B
		/// �z������⎫�������̏ꍇ�́A�����̐��͕ω����܂��B
		/// </summary>
		public abstract bool IsVariadic { get; }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A���̃��\�b�h�I�[�o�[���[�h���W�F�l���b�N���\�b�h�̒�`���ǂ����������l���擾���܂��B</summary>
		public abstract bool IsGenericMethodDefinition { get; }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A���̃��\�b�h�I�[�o�[���[�h���W�F�l���b�N���\�b�h���ǂ����������l���擾���܂��B</summary>
		public abstract bool IsGenericMethod { get; }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A���̃��\�b�h�I�[�o�[���[�h�����蓖�Ă��Ă��Ȃ��W�F�l���b�N�^�������܂�ł��邩�ǂ����������l���擾���܂��B</summary>
		public abstract bool ContainsGenericParameters { get; }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A���̃��\�b�h�I�[�o�[���[�h�̃W�F�l���b�N�^�������擾���܂��B</summary>
		public abstract IList<Type> GenericArguments { get; }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�́A���̃��\�b�h�I�[�o�[���[�h�̃W�F�l���b�N�^�����Ɏw�肳�ꂽ�^�����蓖�ĂāA�W�F�l���b�N���\�b�h���쐬���܂��B</summary>
		/// <param name="genericArguments">�W�F�l���b�N���\�b�h�̌^�����Ɋ��蓖�Ă�^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�����蓖�Ă�ꂽ�W�F�l���b�N���\�b�h������ <see cref="OverloadInfo"/>�B</returns>
		public abstract OverloadInfo MakeGenericMethod(Type[] genericArguments);

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�ɑ΂��ėL���ȌĂяo���K����擾���܂��B</summary>
		public virtual CallingConventions CallingConvention { get { return CallingConventions.Standard; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�ɑ΂��� <see cref="MethodBase"/> ���擾���܂��B</summary>
		public virtual MethodBase ReflectionInfo { get { return null; } }

		// TODO: remove
		/// <summary>���̃��\�b�h�I�[�o�[���[�h���C���X�^���X���쐬�ł��邩�ǂ����������l���擾���܂��B</summary>
		public virtual bool IsInstanceFactory { get { return IsConstructor; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h����`����Ă���^���炵���A�N�Z�X�ł��Ȃ����ǂ����������l���擾���܂��B</summary>
		public bool IsPrivate { get { return ((Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private); } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�����ׂẴI�u�W�F�N�g����A�N�Z�X�\�ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsPublic { get { return ((Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public); } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h����`����Ă���A�Z���u�������炵���A�N�Z�X�ł��Ȃ����ǂ����������l���擾���܂��B</summary>
		public bool IsAssembly { get { return ((Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly); } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h����`����Ă���N���X�Ƃ��ׂĂ̔h���N���X����A�N�Z�X�\�ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsFamily { get { return ((Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family); } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h����`����Ă���A�Z���u�����ƔC�ӂ̔h���N���X����A�N�Z�X�\�ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsFamilyOrAssembly { get { return ((Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem); } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h����`����Ă���N���X�ƃA�Z���u�����ɂ���h���N���X���炵���A�N�Z�X�ł��Ȃ����ǂ����������l���擾���܂��B</summary>
		public bool IsFamilyAndAssembly { get { return ((Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem); } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h���h���N���X����A�N�Z�X�\�ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsProtected { get { return IsFamily || IsFamilyOrAssembly; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h��ÓI�ɌĂяo�����Ƃ��\�ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsStatic { get { return IsConstructor || (Attributes & MethodAttributes.Static) != 0; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�����z���\�b�h�ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsVirtual { get { return (Attributes & MethodAttributes.Virtual) != 0; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�����ʂł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsSpecialName { get { return (Attributes & MethodAttributes.SpecialName) != 0; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h���I�[�o�[���C�h�ł��Ȃ����ǂ����������l���擾���܂��B</summary>
		public bool IsFinal { get { return (Attributes & MethodAttributes.Final) != 0; } }
	}

	/// <summary><see cref="System.Reflection.MethodBase"/> �Ɋ֘A�t����ꂽ���\�b�h�I�[�o�[���[�h��\���܂��B</summary>
	/// <remarks>
	/// ���̃N���X�̓X���b�h�Z�[�t�ł͂���܂���B
	/// �x��: ���̃N���X�͈ꎞ�I�� API �ł���A�����̃o�[�W�����Ŕj��I�ύX���󂯂�\��������܂��B
	/// </remarks>
	public class ReflectionOverloadInfo : OverloadInfo
	{
		[Flags]
		enum _Flags
		{
			None = 0,
			IsVariadic = 1,
			KnownVariadic = 2,
			ContainsGenericParameters = 4,
			KnownContainsGenericParameters = 8,
			IsExtension = 16,
			KnownExtension = 32,
		}

		MethodBase _method;
		ReadOnlyCollection<ParameterInfo> _parameters; // lazy
		ReadOnlyCollection<Type> _genericArguments; // lazy
		_Flags _flags; // lazy

		/// <summary>�w�肳�ꂽ���\�b�h�܂��̓R���X�g���N�^���g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.ReflectionOverloadInfo"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="method">��ɂȂ郁�\�b�h�܂��̓R���X�g���N�^���w�肵�܂��B</param>
		public ReflectionOverloadInfo(MethodBase method) { _method = method; }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�ɑ΂��� <see cref="MethodBase"/> ���擾���܂��B</summary>
		public override MethodBase ReflectionInfo { get { return _method; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�̖��O���擾���܂��B</summary>
		public override string Name { get { return _method.Name; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�̉������̃��X�g���擾���܂��B</summary>
		public override IList<ParameterInfo> Parameters { get { return _parameters ?? (_parameters = new ReadOnlyCollection<ParameterInfo>(_method.GetParameters())); } }

		/// <summary>�߂�l�ɑ΂��� <see cref="System.Reflection.ParameterInfo"/> ���擾���܂��B�R���X�g���N�^�̏ꍇ�� <c>null</c> �ƂȂ�܂��B</summary>
		public override ParameterInfo ReturnParameter
		{
			get
			{
				MethodInfo method = _method as MethodInfo;
				return method != null ? method.ReturnParameter : null;
			}
		}

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�̃W�F�l���b�N�^�������擾���܂��B</summary>
		public override IList<Type> GenericArguments { get { return _genericArguments ?? (_genericArguments = new ReadOnlyCollection<Type>(_method.GetGenericArguments())); } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h���錾����Ă���^���擾���܂��B</summary>
		public override Type DeclaringType { get { return _method.DeclaringType; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�̖߂�l�̌^���擾���܂��B�R���X�g���N�^�̏ꍇ�� <c>null</c> �ɂȂ�܂��B</summary>
		public override Type ReturnType { get { return _method.GetReturnType(); } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�ɑ΂��ėL���ȌĂяo���K����擾���܂��B</summary>
		public override CallingConventions CallingConvention { get { return _method.CallingConvention; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�̑����������t���O���擾���܂��B</summary>
		public override MethodAttributes Attributes { get { return _method.Attributes; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h���C���X�^���X���쐬�ł��邩�ǂ����������l���擾���܂��B</summary>
		public override bool IsInstanceFactory { get { return CompilerHelpers.IsConstructor(_method); } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h���R���X�g���N�^�ł��邩�ǂ����������l���擾���܂��B</summary>
		public override bool IsConstructor { get { return _method.IsConstructor; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h���g�����\�b�h�ł��邩�ǂ����������l���擾���܂��B</summary>
		public override bool IsExtension
		{
			get
			{
				if ((_flags & _Flags.KnownExtension) == 0)
					_flags |= _Flags.KnownExtension | (_method.IsExtension() ? _Flags.IsExtension : 0);
				return (_flags & _Flags.IsExtension) != 0;
			}
		}

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�̈����̐����ςł��邩�ǂ����������l���擾���܂��B�z������⎫�������̏ꍇ�́A�����̐��͕ω����܂��B</summary>
		public override bool IsVariadic
		{
			get
			{
				if ((_flags & _Flags.KnownVariadic) == 0)
					_flags |= _Flags.KnownVariadic | (IsVariadicInternal() ? _Flags.IsVariadic : 0);
				return (_flags & _Flags.IsVariadic) != 0;
			}
		}

		bool IsVariadicInternal()
		{
			var ps = Parameters;
			for (int i = ps.Count - 1; i >= 0; i--)
			{
				if (ps[i].IsParamArray() || ps[i].IsParamDictionary())
					return true;
			}
			return false;
		}

		/// <summary>���̃��\�b�h�I�[�o�[���[�h���W�F�l���b�N���\�b�h���ǂ����������l���擾���܂��B</summary>
		public override bool IsGenericMethod { get { return _method.IsGenericMethod; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h���W�F�l���b�N���\�b�h�̒�`���ǂ����������l���擾���܂��B</summary>
		public override bool IsGenericMethodDefinition { get { return _method.IsGenericMethodDefinition; } }

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�����蓖�Ă��Ă��Ȃ��W�F�l���b�N�^�������܂�ł��邩�ǂ����������l���擾���܂��B</summary>
		public override bool ContainsGenericParameters
		{
			get
			{
				if ((_flags & _Flags.KnownContainsGenericParameters) == 0)
					_flags |= _Flags.KnownContainsGenericParameters | (_method.ContainsGenericParameters ? _Flags.ContainsGenericParameters : 0);
				return (_flags & _Flags.ContainsGenericParameters) != 0;
			}
		}

		/// <summary>���̃��\�b�h�I�[�o�[���[�h�̃W�F�l���b�N�^�����Ɏw�肳�ꂽ�^�����蓖�ĂāA�W�F�l���b�N���\�b�h���쐬���܂��B</summary>
		/// <param name="genericArguments">�W�F�l���b�N���\�b�h�̌^�����Ɋ��蓖�Ă�^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�����蓖�Ă�ꂽ�W�F�l���b�N���\�b�h������ <see cref="OverloadInfo"/>�B</returns>
		public override OverloadInfo MakeGenericMethod(Type[] genericArguments) { return new ReflectionOverloadInfo(((MethodInfo)_method).MakeGenericMethod(genericArguments)); }

		/// <summary>�w�肳�ꂽ���\�b�h�̔z�񂩂�Ή����� <see cref="OverloadInfo"/> ���쐬���܂��B</summary>
		/// <param name="methods"><see cref="OverloadInfo"/> �̊�ɂȂ郁�\�b�h�̔z����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���\�b�h�ɑΉ����� <see cref="OverloadInfo"/> �̔z��B</returns>
		public static OverloadInfo[] CreateArray(MemberInfo[] methods) { return Array.ConvertAll(methods, m => new ReflectionOverloadInfo((MethodBase)m)); }
	}
}
