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

using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>
	/// ���ׂĂ̏������o�C���_�ɈϏ����� <see cref="DynamicMetaObject"/> ��\���܂��B
	/// �o�C���_�͎��ۂ̃I�u�W�F�N�g (�ʏ� RCW) ��œ��삷��̂ŁAComObject �� FallBack �����s����O�ɁA���b�v��������K�v������܂��B
	/// ComBinder �ŃT�|�[�g����ȊO�̂����鑀��ɑ΂��Ă�������������K�v�͂���܂���B
	/// </summary>
	class ComFallbackMetaObject : DynamicMetaObject
	{
		internal ComFallbackMetaObject(Expression expression, BindingRestrictions restrictions, object arg) : base(expression, restrictions, arg) { }

		public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return binder.FallbackGetIndex(UnwrapSelf(), indexes);
		}

		public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return binder.FallbackSetIndex(UnwrapSelf(), indexes, value);
		}

		public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return binder.FallbackGetMember(UnwrapSelf());
		}

		public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return binder.FallbackInvokeMember(UnwrapSelf(), args);
		}

		public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return binder.FallbackSetMember(UnwrapSelf(), value);
		}

		protected virtual ComUnwrappedMetaObject UnwrapSelf()
		{
			return new ComUnwrappedMetaObject(
				ComObject.RcwFromComObject(Expression),
				Restrictions.Merge(ComBinderHelpers.GetTypeRestrictionForDynamicMetaObject(this)),
				((ComObject)Value).RuntimeCallableWrapper
			);
		}
	}

	/// <summary>���̌^�͒P��̌^�Ƃ��đ��݂��Ă��邽�߁AComBinder �̓t�H�[���o�b�N�����݂�ۂɁA�ēx�̃o�C���h���s���܂���B</summary>
	sealed class ComUnwrappedMetaObject : DynamicMetaObject
	{
		internal ComUnwrappedMetaObject(Expression expression, BindingRestrictions restrictions, object value) : base(expression, restrictions, value) { }
	}
}