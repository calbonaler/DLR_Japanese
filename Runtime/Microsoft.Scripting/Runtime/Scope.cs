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
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// ���s�R�[�h�ɑ΂���z�X�g�ɂ��񋟂����ϐ��Q��\���܂��B
	/// �X�R�[�v�g���q��p���Č��ꂲ�Ƃ̏����R���e�L�X�g�Ɋ֘A�Â��邱�Ƃ��ł��܂��B
	/// ���̃N���X�͕������s�ɂ킽���Ďg�p������Ԃ̒ǐՂ�A�J�X�^���X�g���[�W�̒�
	/// (���Ƃ��΁A�I�u�W�F�N�g���L�[�Ƃ���A�N�Z�X�Ȃ�)�A���̑��̌���ŗL�̃Z�}���e�B�N�X�Ɏg�p���邱�Ƃ��ł��܂��B
	/// </summary>
	/// <remarks>
	/// �X�R�[�v�I�u�W�F�N�g�͊�ɂȂ�X�g���[�W���X���b�h�Z�[�t�ł������X���b�h�Z�[�t�ł��B
	/// �X�N���v�g�z�X�g�̓X���b�h�Z�[�t�ȃ��W���[����p���邩�ǂ�����I���ł��܂����A
	/// �X���b�h�Z�[�t�łȂ��X�g���[�W���g�p����ꍇ�̓R�[�h���P��X���b�h�ł��邱�Ƃ𐧖񂵂Ȃ���΂Ȃ�܂���B
	/// </remarks>
	public sealed class Scope : IDynamicMetaObjectProvider
	{
		ScopeExtension[] _extensions; // resizable
		IDynamicMetaObjectProvider _storage;

		/// <summary>�V������̃X���b�h�Z�[�t�ȃf�B�N�V���i�����g�p���āA<see cref="Microsoft.Scripting.Runtime.Scope"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public Scope()
		{
			_extensions = ScopeExtension.EmptyArray;
			_storage = new ScopeStorage();
		}

		/// <summary>�w�肳�ꂽ�f�B�N�V���i�����g�p���� <see cref="Microsoft.Scripting.Runtime.Scope"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="dictionary">�쐬�����X�R�[�v�̊�ɂȂ�f�B�N�V���i�����w�肵�܂��B</param>
		[Obsolete("Scope(IDynamicMetaObjectProvider) �I�[�o�[���[�h�����Ɏg�p���Ă��������B")]
		public Scope(IAttributesCollection dictionary)
		{
			_extensions = ScopeExtension.EmptyArray;
			_storage = new AttributesAdapter(dictionary);
		}

		/// <summary>�X�g���[�W�Ƃ��ĔC�ӂ̃I�u�W�F�N�g���g�p���� <see cref="Microsoft.Scripting.Runtime.Scope"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="storage">�X�g���[�W�Ƃ��Ďg�p�����C�ӂ̃I�u�W�F�N�g���w�肵�܂��B</param>
		public Scope(IDynamicMetaObjectProvider storage)
		{
			_extensions = ScopeExtension.EmptyArray;
			_storage = storage;
		}

		/// <summary>�w�肳�ꂽ����R���e�L�X�g��\�� <see cref="ContextId"/> �Ɋ֘A�t����ꂽ�X�R�[�v�g���q���擾���܂��B</summary>
		/// <param name="languageContextId">�擾����X�R�[�v�g���q���֘A�t�����Ă��錾��R���e�L�X�g��\�� <see cref="ContextId"/> ���w�肵�܂��B</param>
		public ScopeExtension GetExtension(ContextId languageContextId) { return languageContextId.Id < _extensions.Length ? _extensions[languageContextId.Id] : null; }

		/// <summary>�X�R�[�v�g���q���w�肳�ꂽ����R���e�L�X�g��\�� <see cref="ContextId"/> �Ɋ֘A�t���Ă��̃I�u�W�F�N�g�ɐݒ肵�܂��B�g���q�� 1 �񂵂��ݒ�ł��܂���B</summary>
		/// <param name="languageContextId">�X�R�[�v�g���q���֘A�t���錾��R���e�L�X�g��\�� <see cref="ContextId"/> ���w�肵�܂��B</param>
		/// <param name="extension">�ݒ肷��X�R�[�v�g���q���w�肵�܂��B</param>
		/// <returns>�ȑO�ɓ�������R���e�L�X�g�ɃX�R�[�v�g���q���֘A�t�����Ă����ꍇ�͈ȑO�̒l�B����ȊO�̏ꍇ�͐V�����ݒ肳�ꂽ <see cref="ScopeExtension"/>�B</returns>
		public ScopeExtension SetExtension(ContextId languageContextId, ScopeExtension extension)
		{
			ContractUtils.RequiresNotNull(extension, "extension");
			lock (_extensions)
			{
				if (languageContextId.Id >= _extensions.Length)
					Array.Resize(ref _extensions, languageContextId.Id + 1);
				return _extensions[languageContextId.Id] ?? (_extensions[languageContextId.Id] = extension);
			}
		}

		/// <summary>���̃I�u�W�F�N�g�̊�ɂȂ��Ă���X�g���[�W���擾���܂��B</summary>
		public dynamic Storage { get { return _storage; } }

		sealed class MetaScope : DynamicMetaObject
		{
			public MetaScope(Expression parameter, Scope scope) : base(parameter, BindingRestrictions.Empty, scope) { }

			public override DynamicMetaObject BindGetMember(GetMemberBinder binder) { return Restrict(binder.Bind(StorageMetaObject, DynamicMetaObject.EmptyMetaObjects)); }

			public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args) { return Restrict(binder.Bind(StorageMetaObject, args)); }

			public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value) { return Restrict(binder.Bind(StorageMetaObject, new[] { value })); }

			public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder) { return Restrict(binder.Bind(StorageMetaObject, DynamicMetaObject.EmptyMetaObjects)); }

			DynamicMetaObject Restrict(DynamicMetaObject result)
			{
				if (Expression.Type == typeof(Scope))
					return result; // ideal binding, we add no new restrictions if we're binding against a strongly typed Scope
				// Un-ideal binding: we add restrictions.
				return new DynamicMetaObject(result.Expression, BindingRestrictions.GetTypeRestriction(Expression, typeof(Scope)).Merge(result.Restrictions));
			}

			DynamicMetaObject StorageMetaObject { get { return DynamicMetaObject.Create(((Scope)Value)._storage, Expression.Property(Expression.Convert(Expression, typeof(Scope)), typeof(Scope).GetProperty("Storage"))); } }

			public override IEnumerable<string> GetDynamicMemberNames() { return StorageMetaObject.GetDynamicMemberNames(); }
		}

		#region IDynamicMetaObjectProvider Members

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) { return new MetaScope(parameter, this); }

		#endregion

		sealed class AttributesAdapter : IDynamicMetaObjectProvider
		{
			static readonly object _getFailed = new object();
			IAttributesCollection _data;

			public AttributesAdapter(IAttributesCollection data) { _data = data; }

			static object TryGetMember(object adapter, SymbolId name)
			{
				object result;
				if (((AttributesAdapter)adapter)._data.TryGetValue(name, out result))
					return result;
				return _getFailed;
			}

			static void TrySetMember(object adapter, SymbolId name, object value) { ((AttributesAdapter)adapter)._data[name] = value; }

			static bool TryDeleteMember(object adapter, SymbolId name) { return ((AttributesAdapter)adapter)._data.Remove(name); }

			DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) { return new Meta(parameter, this); }

			sealed class Meta : DynamicMetaObject
			{
				public Meta(Expression parameter, AttributesAdapter storage) : base(parameter, BindingRestrictions.Empty, storage) { }

				public override DynamicMetaObject BindGetMember(GetMemberBinder binder) { return DynamicTryGetMember(binder.Name, binder.FallbackGetMember(this).Expression, _ => _); }

				public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
				{
					return DynamicTryGetMember(binder.Name,
						binder.FallbackInvokeMember(this, args).Expression,
						_ => binder.FallbackInvoke(new DynamicMetaObject(_, BindingRestrictions.Empty), args, null).Expression
					);
				}

				DynamicMetaObject DynamicTryGetMember(string name, Expression fallback, Func<Expression, Expression> resultOp)
				{
					var tmp = Expression.Parameter(typeof(object));
					return new DynamicMetaObject(
						Expression.Block(
							new[] { tmp },
							Expression.Condition(
								Expression.NotEqual(
									Expression.Assign(
										tmp,
										Expression.Invoke(
											Expression.Constant(new Func<object, SymbolId, object>(AttributesAdapter.TryGetMember)),
											Expression,
											Expression.Constant(SymbolTable.StringToId(name))
										)
									),
									Expression.Constant(_getFailed)
								),
								ExpressionUtils.Convert(resultOp(tmp), typeof(object)),
								ExpressionUtils.Convert(fallback, typeof(object))
							)
						),
						GetRestrictions()
					);
				}

				BindingRestrictions GetRestrictions() { return BindingRestrictions.GetTypeRestriction(Expression, typeof(AttributesAdapter)); }

				public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
				{
					return new DynamicMetaObject(
						Expression.Block(
							Expression.Invoke(
								Expression.Constant(new Action<object, SymbolId, object>(AttributesAdapter.TrySetMember)),
								Expression,
								Expression.Constant(SymbolTable.StringToId(binder.Name)),
								Expression.Convert(
									value.Expression,
									typeof(object)
								)
							),
							value.Expression
						),
						GetRestrictions()
					);
				}

				public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
				{
					return new DynamicMetaObject(
						Expression.Condition(
							Expression.Invoke(
								Expression.Constant(new Func<object, SymbolId, bool>(AttributesAdapter.TryDeleteMember)),
								Expression,
								Expression.Constant(SymbolTable.StringToId(binder.Name))
							),
							Expression.Default(binder.ReturnType),
							binder.FallbackDeleteMember(this).Expression
						),
						GetRestrictions()
					);
				}

				public override IEnumerable<string> GetDynamicMemberNames() { return ((AttributesAdapter)Value)._data.Keys.OfType<string>(); }
			}
		}
	}
}
