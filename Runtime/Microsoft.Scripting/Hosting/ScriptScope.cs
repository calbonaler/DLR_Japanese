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
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>�R�[�h�ɑ΂�����s�P�ʂ�\���܂��B<see cref="Microsoft.Scripting.Runtime.Scope"/> �ɑ΂���A���� 1 �̃z�X�e�B���O API �ł��B</summary>
	/// <remarks>
	/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> �ɂ́A���ׂẴR�[�h�����s�����O���[�o���� <see cref="Microsoft.Scripting.Runtime.Scope"/> ���܂܂�A
	/// �C�ӂ̃C�j�V�����C�U�⃊���[�_���܂߂邱�Ƃ��ł��܂��B
	/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> �̓X���b�h�Z�[�t�ł͂Ȃ����߁A
	/// �z�X�g�͕����X���b�h���������W���[���ɃA�N�Z�X����ۂɃ��b�N���邩�A�X���b�h���ƂɃR�s�[���Ƃ邩��I������K�v������܂��B
	/// </remarks>
	[DebuggerTypeProxy(typeof(ScriptScope.DebugView))]
	public sealed class ScriptScope : MarshalByRefObject, IDynamicMetaObjectProvider
	{
		/// <summary>��ɂȂ�X�R�[�v����уG���W�����g�p���āA<see cref="Microsoft.Scripting.Hosting.ScriptScope"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="engine">���̃X�R�[�v�Ɋ֘A�t������G���W�����w�肵�܂��B</param>
		/// <param name="scope">���̃X�R�[�v�̊�ɂȂ� <see cref="Microsoft.Scripting.Runtime.Scope"/> ���w�肵�܂��B</param>
		internal ScriptScope(ScriptEngine engine, Scope scope)
		{
			Assert.NotNull(engine, scope);
			Scope = scope;
			Engine = engine;
		}

		/// <summary>���̃X�R�[�v�̊�ɂȂ� <see cref="Microsoft.Scripting.Runtime.Scope"/> ���擾���܂��B</summary>
		internal Scope Scope { get; private set; }

		/// <summary>���̃X�R�[�v�Ɋ֘A�t�����Ă��錾��ɑ΂���G���W�����擾���܂��B�X�R�[�v������Ɋ֘A�t�����Ă��Ȃ��ꍇ�A�C���o���A���g�G���W����Ԃ��܂��B</summary>
		public ScriptEngine Engine { get; private set; }

		/// <summary>�w�肳�ꂽ���O�ŃX�R�[�v�Ɋi�[����Ă���l���擾���܂��B</summary>
		/// <param name="name">�擾����l�Ɋ֘A�t�����Ă��閼�O���w�肵�܂��B</param>
		/// <exception cref="MissingMemberException">�w�肳�ꂽ���O�̓X�R�[�v�ł͒�`����Ă��܂���B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public dynamic GetVariable(string name) { return Engine.LanguageContext.ScopeGetVariable(Scope, name); }

		/// <summary>
		/// �w�肳�ꂽ���O�ŃX�R�[�v�Ɋi�[����Ă���l���擾���܂��B
		/// ���ʂ̓X�R�[�v�Ɋ֘A�t�����Ă��錾�ꂪ��`����ϊ����g�p���Ďw�肳�ꂽ�^�ɕϊ�����܂��B
		/// �X�R�[�v�ɂǂ̌�����֘A�t�����Ă��Ȃ��ꍇ�A����̕ϊ������s����܂��B
		/// </summary>
		/// <param name="name">�擾����l�Ɋ֘A�t�����Ă��閼�O���w�肵�܂��B</param>
		/// <exception cref="MissingMemberException">�w�肳�ꂽ���O�̓X�R�[�v�ł͒�`����Ă��܂���B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public T GetVariable<T>(string name) { return Engine.LanguageContext.ScopeGetVariable<T>(Scope, name); }

		/// <summary>�w�肳�ꂽ���O�ŃX�R�[�v�Ɋi�[����Ă���l�̎擾�����݂܂��B�擾�����������ꍇ <c>true</c> ��Ԃ��܂��B</summary>
		/// <param name="name">�擾����l�Ɋ֘A�t�����Ă��閼�O���w�肵�܂��B</param>
		/// <param name="value">�擾�����l���i�[����ϐ����w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public bool TryGetVariable(string name, out dynamic value) { return Engine.LanguageContext.ScopeTryGetVariable(Scope, name, out value); }

		/// <summary>
		/// �w�肳�ꂽ���O�ŃX�R�[�v�Ɋi�[����Ă���l�̎擾�����݂܂��B
		/// ���ʂ̓X�R�[�v�Ɋ֘A�t�����Ă��錾�ꂪ��`����ϊ����g�p���Ďw�肳�ꂽ�^�ɕϊ�����܂��B
		/// �X�R�[�v�ɂǂ̌�����֘A�t�����Ă��Ȃ��ꍇ�A����̕ϊ������s����܂��B
		/// �擾�����������ꍇ <c>true</c> ��Ԃ��܂��B
		/// </summary>
		/// <param name="name">�擾����l�Ɋ֘A�t�����Ă��閼�O���w�肵�܂��B</param>
		/// <param name="value">�擾�����l���i�[����ϐ����w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public bool TryGetVariable<T>(string name, out T value)
		{
			object result;
			if (Engine.LanguageContext.ScopeTryGetVariable(Scope, name, out result))
			{
				value = Engine.Operations.ConvertTo<T>(result);
				return true;
			}
			value = default(T);
			return false;
		}

		/// <summary>�w�肳�ꂽ�l���w�肳�ꂽ���O�ł��̃X�R�[�v�Ɋi�[���܂��B</summary>
		/// <param name="name">�l���֘A�t�����閼�O���w�肵�܂��B</param>
		/// <param name="value">�X�R�[�v�Ɋi�[����l���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public void SetVariable(string name, object value) { Engine.LanguageContext.ScopeSetVariable(Scope, name, value); }

		/// <summary>�w�肳�ꂽ���O�ŃX�R�[�v�Ɋi�[����Ă���l�ɑ΂���n���h�����擾���܂��B</summary>
		/// <param name="name">�擾����l�Ɋ֘A�t�����Ă��閼�O���w�肵�܂��B</param>
		/// <exception cref="MissingMemberException">�w�肳�ꂽ���O�̓X�R�[�v�ł͒�`����Ă��܂���B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public ObjectHandle GetVariableHandle(string name) { return new ObjectHandle((object)GetVariable(name)); }

		/// <summary>�w�肳�ꂽ���O�ŃX�R�[�v�Ɋi�[����Ă���l�ɑ΂���n���h���̎擾�����݂܂��B�擾�����������ꍇ <c>true</c> ��Ԃ��܂��B</summary>
		/// <param name="name">�擾����l�Ɋ֘A�t�����Ă��閼�O���w�肵�܂��B</param>
		/// <param name="handle">�擾�����l�ɑ΂���n���h�����i�[����ϐ����w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public bool TryGetVariableHandle(string name, out ObjectHandle handle)
		{
			object value;
			if (TryGetVariable(name, out value))
			{
				handle = new ObjectHandle(value);
				return true;
			}
			else
			{
				handle = null;
				return false;
			}
		}

		/// <summary>�w�肳�ꂽ�l���w�肳�ꂽ���O�ł��̃X�R�[�v�Ɋi�[���܂��B</summary>
		/// <param name="name">�l���֘A�t�����閼�O���w�肵�܂��B</param>
		/// <param name="handle">�X�R�[�v�Ɋi�[����l�ɑ΂���n���h�����w�肵�܂��B</param>
		/// <exception cref="SerializationException">
		/// �n���h���ɂ���ĕێ�����Ă���l�̓X�R�[�v�̃A�v���P�[�V�����h���C���̂��̂ł͂Ȃ��A�܂��A�V���A���C�Y�\�ł� <see cref="MarshalByRefObject"/> �ł�����܂���B
		/// </exception>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> �܂��� <paramref name="handle"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public void SetVariable(string name, ObjectHandle handle)
		{
			ContractUtils.RequiresNotNull(handle, "handle");
			SetVariable(name, handle.Unwrap());
		}

		/// <summary>���̃R���e�L�X�g�܂��͊O���̃X�R�[�v�Ɏw�肳�ꂽ���O����`����Ă��邩�ǂ����𒲂ׂ܂��B</summary>
		/// <param name="name">��`����Ă��邩�ǂ����𒲂ׂ閼�O���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public bool ContainsVariable(string name)
		{
			object dummy;
			return TryGetVariable(name, out dummy);
		}

		/// <summary>���̃X�R�[�v����w�肳�ꂽ���O�̕ϐ����폜���܂��B</summary>
		/// <param name="name">�폜����ϐ��̖��O���w�肵�܂��B</param>
		/// <returns>�폜�����O�ɒl�����̃X�R�[�v�ɑ��݂����ꍇ�� <c>true</c> ��Ԃ��܂��B</returns>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> �� <c>null</c> �Q�Ƃł��B</exception>
		public bool RemoveVariable(string name)
		{
			if (Engine.Operations.ContainsMember(Scope, name))
			{
				Engine.Operations.RemoveMember(Scope, name);
				return true;
			}
			return false;
		}

		/// <summary>���̃X�R�[�v�Ɋi�[����Ă���ϐ����擾���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public IEnumerable<string> GetVariableNames() { return Engine.Operations.GetMemberNames((object)Scope.Storage); } // Remoting: we eagerly enumerate all variables to avoid cross domain calls for each item.

		/// <summary>���̃X�R�[�v�Ɋi�[����Ă��閼�O�ƒl�̑g���擾���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public IEnumerable<KeyValuePair<string, object>> GetItems() { return GetVariableNames().Select(name => new KeyValuePair<string, object>(name, (object)Engine.Operations.GetMember((object)Scope.Storage, name))); } // Remoting: we eagerly enumerate all variables to avoid cross domain calls for each item.

		#region DebugView
		sealed class DebugView
		{
			readonly ScriptScope _scope;

			public DebugView(ScriptScope scope)
			{
				Assert.NotNull(scope);
				_scope = scope;
			}

			public ScriptEngine Language { get { return _scope.Engine; } }

			public System.Collections.Hashtable Variables
			{
				get
				{
					System.Collections.Hashtable result = new System.Collections.Hashtable();
					foreach (var variable in _scope.GetItems())
						result[variable.Key] = variable.Value;
					return result;
				}
			}
		}
		#endregion

		#region IDynamicMetaObjectProvider implementation

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) { return new Meta(parameter, this); }

		sealed class Meta : DynamicMetaObject
		{
			internal Meta(Expression parameter, ScriptScope scope) : base(parameter, BindingRestrictions.Empty, scope) { }

			// TODO: support for IgnoreCase in underlying ScriptScope APIs
			public override DynamicMetaObject BindGetMember(GetMemberBinder action)
			{
				var result = Expression.Variable(typeof(object), "result");
				var fallback = action.FallbackGetMember(this);
				return new DynamicMetaObject(
					Expression.Block(
						new [] { result },
						Expression.Condition(
							Expression.Call(
								Expression.Convert(Expression, typeof(ScriptScope)),
								typeof(ScriptScope).GetMethod("TryGetVariable", new[] { typeof(string), typeof(object).MakeByRefType() }),
								Expression.Constant(action.Name),
								result
							),
							result,
							Expression.Convert(fallback.Expression, typeof(object))
						)
					),
					BindingRestrictions.GetTypeRestriction(Expression, typeof(ScriptScope)).Merge(fallback.Restrictions)
				);
			}

			// TODO: support for IgnoreCase in underlying ScriptScope APIs
			public override DynamicMetaObject BindSetMember(SetMemberBinder action, DynamicMetaObject value)
			{
				var objValue = Expression.Convert(value.Expression, typeof(object));
				return new DynamicMetaObject(
					Expression.Block(
						Expression.Call(
							Expression.Convert(Expression, typeof(ScriptScope)),
							typeof(ScriptScope).GetMethod("SetVariable", new[] { typeof(string), typeof(object) }),
							Expression.Constant(action.Name),
							objValue
						),
						objValue
					),
					Restrictions.Merge(value.Restrictions).Merge(BindingRestrictions.GetTypeRestriction(Expression, typeof(ScriptScope)))
				);
			}

			// TODO: support for IgnoreCase in underlying ScriptScope APIs
			public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder action)
			{
				var fallback = action.FallbackDeleteMember(this);
				return new DynamicMetaObject(
					Expression.IfThenElse(
						Expression.Call(
							Expression.Convert(Expression, typeof(ScriptScope)),
							typeof(ScriptScope).GetMethod("RemoveVariable"),
							Expression.Constant(action.Name)
						),
						Expression.Empty(),
						fallback.Expression
					),
					Restrictions.Merge(BindingRestrictions.GetTypeRestriction(Expression, typeof(ScriptScope))).Merge(fallback.Restrictions)
				);
			}

			// TODO: support for IgnoreCase in underlying ScriptScope APIs
			public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder action, DynamicMetaObject[] args)
			{
				var fallback = action.FallbackInvokeMember(this, args);
				var result = Expression.Variable(typeof(object), "result");
				return new DynamicMetaObject(
					Expression.Block(
						new [] { result },
						Expression.Condition(
							Expression.Call(
								Expression.Convert(Expression, typeof(ScriptScope)),
								typeof(ScriptScope).GetMethod("TryGetVariable", new[] { typeof(string), typeof(object).MakeByRefType() }),
								Expression.Constant(action.Name),
								result
							),
							Expression.Convert(action.FallbackInvoke(new DynamicMetaObject(result, BindingRestrictions.Empty), args, null).Expression, typeof(object)),
							Expression.Convert(fallback.Expression, typeof(object))
						)
					),
					BindingRestrictions.Combine(args).Merge(BindingRestrictions.GetTypeRestriction(Expression, typeof(ScriptScope))).Merge(fallback.Restrictions)
				);
			}

			public override IEnumerable<string> GetDynamicMemberNames() { return ((ScriptScope)Value).GetVariableNames(); }
		}

		#endregion

		// TODO: Figure out what is the right lifetime
		/// <summary>�Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��A�L�����ԃT�[�r�X �I�u�W�F�N�g���擾���܂��B</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
