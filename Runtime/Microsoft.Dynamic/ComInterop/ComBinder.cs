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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "System.Dynamic")]

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>���I�� COM �I�u�W�F�N�g�Ƀo�C���h���邽�߂̃w���p�[ ���\�b�h��񋟂��܂��B</summary>
	public static class ComBinder
	{
		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�� COM �I�u�W�F�N�g���ǂ����𔻒f���܂��B</summary>
		/// <param name="value">���ׂ�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�I�u�W�F�N�g�� COM �I�u�W�F�N�g�̏ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsComObject(object value) { return ComObject.IsComObject(value); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂��� COM �o�C���f�B���O���\���ǂ����𔻒f���܂��B</summary>
		/// <param name="value">���ׂ�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�I�u�W�F�N�g�� COM �o�C���f�B���O�\�ȏꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool CanComBind(object value) { return IsComObject(value) || value is IPseudoComObject; }

		/// <summary>���I�����o�擾����̃o�C���f�B���O�̎��s�����݂܂��B</summary>
		/// <param name="binder">���I����̏ڍׂ�\�� <see cref="GetMemberBinder"/> �̃C���X�^���X���w�肵�܂��B</param>
		/// <param name="instance">���I����̃^�[�Q�b�g���w�肵�܂��B</param>
		/// <param name="result">�o�C���f�B���O�̌��ʂ�\���V���� <see cref="DynamicMetaObject"/> ���i�[�����ϐ����w�肵�܂��B</param>
		/// <param name="delayInvocation">�����o�]���̒x�����������ǂ����������l���w�肵�܂��B</param>
		/// <returns>���삪����Ƀo�C���h���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool TryBindGetMember(GetMemberBinder binder, DynamicMetaObject instance, out DynamicMetaObject result, bool delayInvocation)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			if (TryGetMetaObject(ref instance))
			{
				var comGetMember = new ComGetMemberBinder(binder, delayInvocation);
				result = instance.BindGetMember(comGetMember);
				if (result.Expression.Type.IsValueType)
					result = new DynamicMetaObject(Expression.Convert(result.Expression, typeof(object)), result.Restrictions);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>���I�����o�擾����̃o�C���f�B���O�̎��s�����݂܂��B</summary>
		/// <param name="binder">���I����̏ڍׂ�\�� <see cref="GetMemberBinder"/> �̃C���X�^���X���w�肵�܂��B</param>
		/// <param name="instance">���I����̃^�[�Q�b�g���w�肵�܂��B</param>
		/// <param name="result">�o�C���f�B���O�̌��ʂ�\���V���� <see cref="DynamicMetaObject"/> ���i�[�����ϐ����w�肵�܂��B</param>
		/// <returns>���삪����Ƀo�C���h���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool TryBindGetMember(GetMemberBinder binder, DynamicMetaObject instance, out DynamicMetaObject result) { return TryBindGetMember(binder, instance, out result, false); }

		/// <summary>���I�����o�ݒ葀��̃o�C���f�B���O�̎��s�����݂܂��B</summary>
		/// <param name="binder">���I����̏ڍׂ�\�� <see cref="SetMemberBinder"/> �̃C���X�^���X���w�肵�܂��B</param>
		/// <param name="instance">���I����̃^�[�Q�b�g���w�肵�܂��B</param>
		/// <param name="value">�����o�ݒ葀��̒l��\�� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="result">�o�C���f�B���O�̌��ʂ�\���V���� <see cref="DynamicMetaObject"/> ���i�[�����ϐ����w�肵�܂��B</param>
		/// <returns>���삪����Ƀo�C���h���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool TryBindSetMember(SetMemberBinder binder, DynamicMetaObject instance, DynamicMetaObject value, out DynamicMetaObject result)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(value, "value");
			if (TryGetMetaObject(ref instance))
			{
				result = instance.BindSetMember(binder, value);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>���I�Ăяo������̃o�C���f�B���O�̎��s�����݂܂��B</summary>    
		/// <param name="binder">���I����̏ڍׂ�\�� <see cref="InvokeBinder"/> �̃C���X�^���X���w�肵�܂��B</param>
		/// <param name="instance">���I����̃^�[�Q�b�g���w�肵�܂��B</param>
		/// <param name="args">�Ăяo������̈�����\�� <see cref="DynamicMetaObject"/> �C���X�^���X�̔z����w�肵�܂��B</param>
		/// <param name="result">�o�C���f�B���O�̌��ʂ�\���V���� <see cref="DynamicMetaObject"/> ���i�[�����ϐ����w�肵�܂��B</param>
		/// <returns>���삪����Ƀo�C���h���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool TryBindInvoke(InvokeBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, out DynamicMetaObject result)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(args, "args");
			if (TryGetMetaObjectInvoke(ref instance))
			{
				result = instance.BindInvoke(binder, args);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>���I�����o�Ăяo������̃o�C���f�B���O�̎��s�����݂܂��B</summary>
		/// <param name="binder">���I����̏ڍׂ�\�� <see cref="InvokeMemberBinder"/> �̃C���X�^���X���w�肵�܂��B</param>
		/// <param name="instance">���I����̃^�[�Q�b�g���w�肵�܂��B</param>
		/// <param name="args">�����o�Ăяo������̈�����\�� <see cref="DynamicMetaObject"/> �C���X�^���X�̔z����w�肵�܂��B</param>
		/// <param name="result">�o�C���f�B���O�̌��ʂ�\���V���� <see cref="DynamicMetaObject"/> ���i�[�����ϐ����w�肵�܂��B</param>
		/// <returns>���삪����Ƀo�C���h���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool TryBindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, out DynamicMetaObject result)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(args, "args");
			if (TryGetMetaObject(ref instance))
			{
				result = instance.BindInvokeMember(binder, args);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>���I�C���f�b�N�X�擾����̃o�C���f�B���O�̎��s�����݂܂��B</summary>
		/// <param name="binder">���I����̏ڍׂ�\�� <see cref="GetIndexBinder"/> �̃C���X�^���X���w�肵�܂��B</param>
		/// <param name="instance">���I����̃^�[�Q�b�g���w�肵�܂��B</param>
		/// <param name="args">�C���f�b�N�X�擾����̈�����\�� <see cref="DynamicMetaObject"/> �C���X�^���X�̔z����w�肵�܂��B</param>
		/// <param name="result">�o�C���f�B���O�̌��ʂ�\���V���� <see cref="DynamicMetaObject"/> ���i�[�����ϐ����w�肵�܂��B</param>
		/// <returns>���삪����Ƀo�C���h���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool TryBindGetIndex(GetIndexBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, out DynamicMetaObject result)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(args, "args");
			if (TryGetMetaObjectInvoke(ref instance))
			{
				result = instance.BindGetIndex(binder, args);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>���I�C���f�b�N�X�ݒ葀��̃o�C���f�B���O�̎��s�����݂܂��B</summary>
		/// <param name="binder">���I����̏ڍׂ�\�� <see cref="SetIndexBinder"/> �̃C���X�^���X���w�肵�܂��B</param>
		/// <param name="instance">���I����̃^�[�Q�b�g���w�肵�܂��B</param>
		/// <param name="args">�C���f�b�N�X�ݒ葀��̈�����\�� <see cref="DynamicMetaObject"/> �C���X�^���X�̔z����w�肵�܂��B</param>
		/// <param name="value">�C���f�b�N�X�ݒ葀��̒l��\�� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="result">�o�C���f�B���O�̌��ʂ�\���V���� <see cref="DynamicMetaObject"/> ���i�[�����ϐ����w�肵�܂��B</param>
		/// <returns>���삪����Ƀo�C���h���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool TryBindSetIndex(SetIndexBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, DynamicMetaObject value, out DynamicMetaObject result)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(args, "args");
			ContractUtils.RequiresNotNull(value, "value");
			if (TryGetMetaObjectInvoke(ref instance))
			{
				result = instance.BindSetIndex(binder, args, value);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>���I�ϊ�����̃o�C���f�B���O�̎��s�����݂܂��B</summary>
		/// <param name="binder">���I����̏ڍׂ�\�� <see cref="ConvertBinder"/> �̃C���X�^���X���w�肵�܂��B</param>
		/// <param name="instance">���I����̃^�[�Q�b�g���w�肵�܂��B</param>
		/// <param name="result">�o�C���f�B���O�̌��ʂ�\���V���� <see cref="DynamicMetaObject"/> ���i�[�����ϐ����w�肵�܂��B</param>
		/// <returns>���삪����Ƀo�C���h���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool TryConvert(ConvertBinder binder, DynamicMetaObject instance, out DynamicMetaObject result)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			// COM �I�u�W�F�N�g�̂�����C���^�[�t�F�C�X�ւ̕ϊ��͏�ɉ\���ƍl���܂��B���s���� QueryInterface �����ʂɂȂ�܂��B
			if (IsComObject(instance.Value) && binder.Type.IsInterface)
			{
				result = new DynamicMetaObject(Expression.Convert(instance.Expression, binder.Type),
					BindingRestrictions.GetExpressionRestriction(
						Expression.Call(
							typeof(ComObject).GetMethod("IsComObject", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic),
							Ast.Utils.Convert(instance.Expression, typeof(object))
						)
					)
				);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>
		/// �w�肳�ꂽ�I�u�W�F�N�g�Ɋ֘A�t����ꂽ�����o�����擾���܂��B
		/// ���̃��\�b�h�� <see cref="IsComObject"/> �� <c>true</c> ��Ԃ��I�u�W�F�N�g�ɑ΂��Ă̂ݓ��삵�܂��B
		/// </summary>
		/// <param name="value">�����o����v������I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�����o���̃R���N�V�����B</returns>
		public static IEnumerable<string> GetDynamicMemberNames(object value)
		{
			ContractUtils.RequiresNotNull(value, "value");
			ContractUtils.Requires(IsComObject(value), "value", Strings.ComObjectExpected);
			return ComObject.ObjectToComObject(value).GetMemberNames(false);
		}

		/// <summary>
		/// �w�肳�ꂽ�I�u�W�F�N�g�Ɋ֘A�t����ꂽ�f�[�^�`���̃����o�����擾���܂��B
		/// ���̃��\�b�h�� <see cref="IsComObject"/> �� <c>true</c> ��Ԃ��I�u�W�F�N�g�ɑ΂��Ă̂ݓ��삵�܂��B
		/// </summary>
		/// <param name="value">�����o����v������I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�����o���̃R���N�V�����B</returns>
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static IList<string> GetDynamicDataMemberNames(object value)
		{
			ContractUtils.RequiresNotNull(value, "value");
			ContractUtils.Requires(IsComObject(value), "value", Strings.ComObjectExpected);
			return ComObject.ObjectToComObject(value).GetMemberNames(true);
		}

		/// <summary>
		/// �I�u�W�F�N�g�ɑ΂���f�[�^�`���̃����o�Ɗ֘A�t����ꂽ�I�u�W�F�N�g��Ԃ��܂��B
		/// ���̃��\�b�h�� <see cref="IsComObject"/> �� <c>true</c> ��Ԃ��I�u�W�F�N�g�ɑ΂��Ă̂ݓ��삵�܂��B
		/// </summary>
		/// <param name="value">�f�[�^�����o��v������I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="names">�l���擾����f�[�^�����o�̖��O���w�肵�܂��B</param>
		/// <returns>�f�[�^�����o�̖��O�ƒl�̃y�A�̃R���N�V�����B</returns>
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static IList<KeyValuePair<string, object>> GetDynamicDataMembers(object value, IEnumerable<string> names)
		{
			ContractUtils.RequiresNotNull(value, "value");
			ContractUtils.Requires(IsComObject(value), "value", Strings.ComObjectExpected);
			return ComObject.ObjectToComObject(value).GetMembers(names);
		}

		static bool TryGetMetaObject(ref DynamicMetaObject instance)
		{
			// ���ł� COM DynamicMetaObject �̏ꍇ�͐V�������̂����Ȃ��B
			// (COM ����̃t�H�[���o�b�N���Ăяo���ꍇ�ɁA�ċA��h�����߂ɂ�����s���B)
			if (instance is ComUnwrappedMetaObject)
				return false;
			if (IsComObject(instance.Value))
			{
				instance = new ComMetaObject(instance.Expression, instance.Restrictions, instance.Value);
				return true;
			}
			return false;
		}

		static bool TryGetMetaObjectInvoke(ref DynamicMetaObject instance)
		{
			// ���ł� COM DynamicMetaObject �̏ꍇ�͐V�������̂����Ȃ��B
			// (COM ����̃t�H�[���o�b�N���Ăяo���ꍇ�ɁA�ċA��h�����߂ɂ�����s���B)
			if (TryGetMetaObject(ref instance))
				return true;
			if (instance.Value is IPseudoComObject)
			{
				instance = ((IPseudoComObject)instance.Value).GetMetaObject(instance.Expression);
				return true;
			}
			return false;
		}

		/// <summary>COM �����o�擾����̓��ʂȃZ�}���e�B�N�X�������o�C���_�[�ł��B</summary>
		internal class ComGetMemberBinder : GetMemberBinder
		{
			readonly GetMemberBinder _originalBinder;
			internal bool _CanReturnCallables;

			internal ComGetMemberBinder(GetMemberBinder originalBinder, bool CanReturnCallables) : base(originalBinder.Name, originalBinder.IgnoreCase)
			{
				_originalBinder = originalBinder;
				_CanReturnCallables = CanReturnCallables;
			}

			public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion) { return _originalBinder.FallbackGetMember(target, errorSuggestion); }

			public override int GetHashCode() { return _originalBinder.GetHashCode() ^ (_CanReturnCallables ? 1 : 0); }

			public override bool Equals(object obj)
			{
				var other = obj as ComGetMemberBinder;
				return other != null && _CanReturnCallables == other._CanReturnCallables && _originalBinder.Equals(other._originalBinder);
			}
		}
	}
}