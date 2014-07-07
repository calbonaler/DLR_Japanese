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
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// �����̃o�C���_�[��P��̓��I�T�C�g�Ɍ����ł���o�C���_�[��\���܂��B
	/// ���̃N���X�̍쐬�҂͈����A�萔�A�T�u�T�C�g���̃}�b�s���O���s���A���̃f�[�^��\�� <see cref="BinderMappingInfo"/> �̃��X�g��񋟂���K�v������܂��B
	/// ��������A<see cref="ComboBinder"/> �͌��ʂ̃R�[�h�𐶐����邽�߂ɁA���X�g���������邾���ł悢���ƂɂȂ�܂��B
	/// </summary>
	public class ComboBinder : DynamicMetaObjectBinder, IEquatable<ComboBinder>
	{
		readonly BinderMappingInfo[] _metaBinders;

		/// <summary>�w�肳�ꂽ�}�b�s���O�����g�p���āA<see cref="Microsoft.Scripting.Actions.ComboBinder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binders">���� <see cref="ComboBinder"/> ���g�p����}�b�s���O�����w�肵�܂��B</param>
		public ComboBinder(params BinderMappingInfo[] binders) : this((ICollection<BinderMappingInfo>)binders) { }

		/// <summary>�w�肳�ꂽ�}�b�s���O�����g�p���āA<see cref="Microsoft.Scripting.Actions.ComboBinder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binders">���� <see cref="ComboBinder"/> ���g�p����}�b�s���O�����w�肵�܂��B</param>
		public ComboBinder(ICollection<BinderMappingInfo> binders)
		{
			Assert.NotNullItems(binders);
			_metaBinders = ArrayUtils.ToArray(binders);
		}

		/// <summary>���I����̃o�C���f�B���O�����s���܂��B</summary>
		/// <param name="target">���I����̃^�[�Q�b�g�B</param>
		/// <param name="args">���I����̈����̔z��B</param>
		/// <returns>�o�C���f�B���O�̌��ʂ�\�� <see cref="System.Dynamic.DynamicMetaObject"/>�B</returns>
		public override DynamicMetaObject Bind(DynamicMetaObject target, params DynamicMetaObject[] args)
		{
			List<DynamicMetaObject> results = new List<DynamicMetaObject>(_metaBinders.Length);
			List<Expression> steps = new List<Expression>();
			List<ParameterExpression> temps = new List<ParameterExpression>();
			var restrictions = BindingRestrictions.Empty;
			for (int i = 0; i < _metaBinders.Length; i++)
			{
				var targ = GetArguments(x => x == 0 ? target : args[x - 1], results, _metaBinders[i].MappingInfo);
				var next = _metaBinders[i].Binder.Bind(targ.First(), targ.Skip(1).ToArray());
				if (i != 0) // If the rule contains an embedded "update", replace it with a defer
					next = new DynamicMetaObject(new ReplaceUpdateVisitor { Binder = _metaBinders[i].Binder, Arguments = targ.ToArray() }.Visit(next.Expression), next.Restrictions);
				restrictions = restrictions.Merge(next.Restrictions);
				if (next.Expression.NodeType == ExpressionType.Throw)
				{
					// end of the line... the expression is throwing, none of the other binders will have an opportunity to run.
					steps.Add(next.Expression);
					break;
				}
				var tmp = Expression.Variable(next.Expression.Type, "comboTemp" + i.ToString());
				temps.Add(tmp);
				steps.Add(Expression.Assign(tmp, next.Expression));
				results.Add(new DynamicMetaObject(tmp, next.Restrictions));
			}
			return new DynamicMetaObject(Expression.Block(temps, steps), restrictions);
		}

		/// <summary>����̌��ʌ^�B</summary>
		public override Type ReturnType { get { return _metaBinders[_metaBinders.Length - 1].Binder.ReturnType; } }

		sealed class ReplaceUpdateVisitor : ExpressionVisitor
		{
			internal DynamicMetaObjectBinder Binder;
			internal DynamicMetaObject[] Arguments;
			protected override Expression VisitGoto(GotoExpression node) { return node.Target == CallSiteBinder.UpdateLabel ? Binder.Defer(Arguments).Expression : base.Visit(node); }
		}

		static IEnumerable<DynamicMetaObject> GetArguments(Func<int, DynamicMetaObject> args, IList<DynamicMetaObject> results, IEnumerable<ParameterMappingInfo> info)
		{
			return info.Select(x => x.IsAction ? results[x.ActionIndex] : (x.IsParameter ? args(x.ParameterIndex) : new DynamicMetaObject(x.Constant, BindingRestrictions.Empty, x.Constant.Value)));
		}

		/// <summary>���̃I�u�W�F�N�g�̃n�b�V���l���v�Z���܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g�̃n�b�V���l�B</returns>
		public override int GetHashCode() { return _metaBinders.Aggregate(6551, (res, binder) => binder.MappingInfo.Aggregate(res ^ binder.Binder.GetHashCode(), (x, y) => x ^ y.GetHashCode())); }

		/// <summary>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">��r����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>���̃I�u�W�F�N�g�Ǝw�肳�ꂽ�I�u�W�F�N�g���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public override bool Equals(object obj) { return Equals(obj as ComboBinder); }

		/// <summary>���� <see cref="ComboBinder"/> ���w�肳�ꂽ <see cref="ComboBinder"/> �Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="other">��r���� <see cref="ComboBinder"/> ���w�肵�܂��B</param>
		/// <returns>����<see cref="ComboBinder"/> �Ǝw�肳�ꂽ <see cref="ComboBinder"/> ���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool Equals(ComboBinder other)
		{
			return other != null && _metaBinders.Length == other._metaBinders.Length && Enumerable.Range(0, _metaBinders.Length).All(i =>
				_metaBinders[i].Binder.Equals(other._metaBinders[i].Binder) && _metaBinders[i].MappingInfo.Count == other._metaBinders[i].MappingInfo.Count &&
				Enumerable.Range(0, _metaBinders[i].MappingInfo.Count).All(x => _metaBinders[i].MappingInfo[x].Equals(other._metaBinders[i].MappingInfo[x]))
			);
		}
	}

	/// <summary>
	/// �R���{�A�N�V�������̓��͂ɑ΂���}�b�s���O��񋟂��܂��B
	/// ���͂͐V�������I�T�C�g�̓��́A�ȑO�� <see cref="DynamicExpression"/> �̓��́A���邢�͓��I�T�C�g�̈���������o���ꂽ <see cref="ConstantExpression"/> �Ƀ}�b�s���O�ł��܂��B
	/// </summary>
	public class ParameterMappingInfo : IEquatable<ParameterMappingInfo>
	{
		ParameterMappingInfo(int param, int action, ConstantExpression fixedInput)
		{
			ParameterIndex = param;
			ActionIndex = action;
			Constant = fixedInput;
		}

		/// <summary>���̈����Ƀ}�b�s���O���ꂽ���͂�Ԃ��܂��B</summary>
		/// <param name="index">�}�b�s���O���錳�̈����̈ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���̈����Ƀ}�b�s���O���ꂽ���́B</returns>
		public static ParameterMappingInfo Parameter(int index) { return new ParameterMappingInfo(index, -1, null); }

		/// <summary>�ȑO�̃o�C���f�B���O���ʂɃ}�b�s���O���ꂽ���͂�Ԃ��܂��B</summary>
		/// <param name="index">�}�b�s���O����ȑO�̃o�C���f�B���O���ʂ̈ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�ȑO�̃o�C���f�B���O���ʂɃ}�b�s���O���ꂽ���́B</returns>
		public static ParameterMappingInfo Action(int index) { return new ParameterMappingInfo(-1, index, null); }

		/// <summary>�萔�Ƀ}�b�s���O���ꂽ���͂�Ԃ��܂��B</summary>
		/// <param name="e">�}�b�s���O����萔��\�� <see cref="ConstantExpression"/> ���w�肵�܂��B</param>
		/// <returns>�萔�Ƀ}�b�s���O���ꂽ���́B</returns>
		public static ParameterMappingInfo Fixed(ConstantExpression e) { return new ParameterMappingInfo(-1, -1, e); }

		/// <summary>���̓��͂��}�b�s���O����Ă��錳�̈������X�g���̈����̈ʒu���擾���܂��B</summary>
		public int ParameterIndex { get; private set; }

		/// <summary>���̓��͂��}�b�s���O����Ă���ȑO�̃o�C���f�B���O���ʂ̈ʒu���擾���܂��B</summary>
		public int ActionIndex { get; private set; }

		/// <summary>���̓��͂��}�b�s���O����Ă���萔��\�� <see cref="ConstantExpression"/> ���擾���܂��B</summary>
		public ConstantExpression Constant { get; private set; }

		/// <summary>���̓��͂����̈������X�g���̈����Ƀ}�b�s���O����Ă��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsParameter { get { return ParameterIndex != -1; } }

		/// <summary>���̓��͂��ȑO�̃o�C���f�B���O���ʂɃ}�b�s���O����Ă��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsAction { get { return ActionIndex != -1; } }

		/// <summary>���̓��͂��萔�Ƀ}�b�s���O����Ă��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsConstant { get { return Constant != null; } }
		
		/// <summary>���̓��͂��w�肳�ꂽ���͂Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="other">��r������͂��w�肵�܂��B</param>
		/// <returns>���̓��͂��w�肳�ꂽ���͂Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool Equals(ParameterMappingInfo other)
		{
			return other != null && other.ParameterIndex == ParameterIndex && other.ActionIndex == ActionIndex &&
				(Constant != null ? other.Constant != null && Constant.Value == other.Constant.Value : other.Constant == null);
		}

		/// <summary>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">��r����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public override bool Equals(object obj) { return Equals(obj as ParameterMappingInfo); }

		/// <summary>���̃I�u�W�F�N�g�̃n�b�V���l���v�Z���܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g�̃n�b�V���l�B</returns>
		public override int GetHashCode() { return ParameterIndex.GetHashCode() ^ ActionIndex.GetHashCode() ^ (Constant != null && Constant.Value != null ? Constant.Value.GetHashCode() : 0); }

		/// <summary>���̃I�u�W�F�N�g�̕�����\����Ԃ��܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString()
		{
			if (IsAction)
				return "Action" + ActionIndex.ToString();
			else if (IsParameter)
				return "Parameter" + ParameterIndex.ToString();
			else
				return Constant.Value == null ? "(null)" : Constant.Value.ToString();
		}
	}

	/// <summary>
	/// �P��̃R���{�o�C���_�[�ɑ΂���}�b�s���O�����i�[���܂��B
	/// ���̃N���X�͌��� <see cref="DynamicMetaObjectBinder"/> �ƈ����A�T�u�T�C�g����ђ萔����o�C���f�B���O�փ}�b�s���O���܂�ł��܂��B
	/// </summary>
	public class BinderMappingInfo
	{
		/// <summary>���� <see cref="DynamicMetaObjectBinder"/> ���擾���܂��B</summary>
		public DynamicMetaObjectBinder Binder { get; private set; }
		
		/// <summary>�����A�T�u�T�C�g����ђ萔����o�C���f�B���O�ւ̃}�b�s���O�����擾���܂��B</summary>
		public IList<ParameterMappingInfo> MappingInfo { get; private set; }

		/// <summary>�w�肳�ꂽ�o�C���_�[�ƃ}�b�s���O�����g�p���āA<see cref="Microsoft.Scripting.Actions.BinderMappingInfo"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binder">���̃o�C���_�[���w�肵�܂��B</param>
		/// <param name="mappingInfo">�����A�T�u�T�C�g����ђ萔����o�C���f�B���O�ւ̃}�b�s���O�����w�肵�܂��B</param>
		public BinderMappingInfo(DynamicMetaObjectBinder binder, IList<ParameterMappingInfo> mappingInfo)
		{
			Binder = binder;
			MappingInfo = mappingInfo;
		}

		/// <summary>�w�肳�ꂽ�o�C���_�[�ƃ}�b�s���O�����g�p���āA<see cref="Microsoft.Scripting.Actions.BinderMappingInfo"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binder">���̃o�C���_�[���w�肵�܂��B</param>
		/// <param name="mappingInfo">�����A�T�u�T�C�g����ђ萔����o�C���f�B���O�ւ̃}�b�s���O�����w�肵�܂��B</param>
		public BinderMappingInfo(DynamicMetaObjectBinder binder, params ParameterMappingInfo[] mappingInfo) : this(binder, (IList<ParameterMappingInfo>)mappingInfo) { }

		/// <summary>���̃I�u�W�F�N�g�̕�����\����Ԃ��܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return Binder.ToString() + " " + string.Join(", ", MappingInfo.Select(x => x.ToString())); }
	}
}
