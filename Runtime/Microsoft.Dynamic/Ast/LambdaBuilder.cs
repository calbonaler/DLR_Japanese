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
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;
using RuntimeHelpers = Microsoft.Scripting.Runtime.ScriptingRuntimeHelpers;

namespace Microsoft.Scripting.Ast
{
	// TODO: ����͍폜����A���g�̃��[�J���X�R�[�v���n���h�����錾��ɒu���������� CodeContext �Ɋ֘A����@�\���܂�ł��܂��B
	/// <summary>
	/// <see cref="LambdaExpression"/> ���쐬���邽�߂̃r���_�[��\���܂��B
	/// ���c���[�͈�������ѕϐ������O�ɍ쐬���ꂽ��ŁA<see cref="LambdaExpression"/> ���쐬����t�@�N�g���ɓn����邱�Ƃ�v������̂ŁA
	/// ���̃r���_�[�̓����_���̍\���Ɋւ��邠�������ǐՂ��A<see cref="LambdaExpression"/> ���쐬���܂��B
	/// </summary>
	public class LambdaBuilder
	{
		readonly List<KeyValuePair<ParameterExpression, bool>> _visibleVars = new List<KeyValuePair<ParameterExpression, bool>>();
		string _name;
		Type _returnType;
		Expression _body;
		bool _completed;
		static int _lambdaId; // �����_�̈�ӂȖ��O�𐶐����邽��

		internal LambdaBuilder(string name, Type returnType)
		{
			Locals = new List<ParameterExpression>();
			Parameters = new List<ParameterExpression>();
			Visible = true;
			_name = name;
			_returnType = returnType;
		}

		/// <summary>�����_���̖��O���擾�܂��͐ݒ肵�܂��B���ݓ������邢�͖����̃����_���͋�����Ă��܂���B</summary>
		public string Name
		{
			get { return _name; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_name = value;
			}
		}

		/// <summary>�쐬����郉���_���̖߂�l�̌^���擾�܂��͐ݒ肵�܂��B</summary>
		public Type ReturnType
		{
			get { return _returnType; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_returnType = value;
			}
		}

		/// <summary>�����_���̃��[�J���ϐ��𒼐ڑ���ł��郊�X�g���擾���܂��B</summary>
		public List<ParameterExpression> Locals { get; private set; }

		/// <summary>�����_���̉������𒼐ڑ���ł��郊�X�g���擾���܂��B</summary>
		public List<ParameterExpression> Parameters { get; private set; }

		/// <summary>���݂���ꍇ�͔z��������擾���܂��B</summary>
		public ParameterExpression ParamsArray { get; private set; }

		/// <summary>�����_���̖{�̂��擾���܂��B����� <c>null</c> �łȂ��K�v������܂��B</summary>
		public Expression Body
		{
			get { return _body; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_body = value;
			}
		}

		/// <summary>��������郉���_�������[�J���ϐ��𒼐� CLR �X�^�b�N�Ɋm�ۂ������ɂ������i�[����f�B�N�V���i���������ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool Dictionary { get; set; }

		/// <summary>�X�R�[�v�������ǂ����������l���擾�܂��͐ݒ肵�܂��B����ł̓X�R�[�v�͉��ł��B</summary>
		public bool Visible { get; set; }

		/// <summary>���ł���ϐ��̃��X�g���擾���܂��B</summary>
		/// <returns>���ł���ϐ��̃��X�g�B</returns>
		public IEnumerable<ParameterExpression> GetVisibleVariables() { return _visibleVars.Where(x => Dictionary || x.Value).Select(x => x.Key); }

		/// <summary>
		/// �w�肳�ꂽ���O�ƌ^���g�p���āA�����_���̉��������쐬���܂��B
		/// <see cref="Parameters"/> �͍쐬���ꂽ������ێ����܂����A���� <see cref="Parameters"/> �ɃA�N�Z�X���邱�Ƃŏ�����ύX���邱�Ƃ��\�ł��B
		/// </summary>
		/// <param name="type">�쐬����鉼�����̌^���w�肵�܂��B</param>
		/// <param name="name">�쐬����鉼�����̖��O���w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ��������\�� <see cref="ParameterExpression"/>�B</returns>
		public ParameterExpression Parameter(Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			var result = Expression.Parameter(type, name);
			Parameters.Add(result);
			_visibleVars.Add(new KeyValuePair<ParameterExpression, bool>(result, false));
			return result;
		}

		/// <summary>
		/// �w�肳�ꂽ���O�ƌ^���g�p���āA�����_���̉��������쐬���܂��B
		/// <see cref="Parameters"/> �͍쐬���ꂽ������ێ����܂����A���� <see cref="Parameters"/> �ɃA�N�Z�X���邱�Ƃŏ�����ύX���邱�Ƃ��\�ł��B
		/// </summary>
		/// <param name="type">�쐬����鉼�����̌^���w�肵�܂��B</param>
		/// <param name="name">�쐬����鉼�����̖��O���w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ��������\�� <see cref="ParameterExpression"/>�B</returns>
		public ParameterExpression ClosedOverParameter(Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			var result = Expression.Parameter(type, name);
			Parameters.Add(result);
			_visibleVars.Add(new KeyValuePair<ParameterExpression, bool>(result, true));
			return result;
		}

		/// <summary>
		/// �w�肳�ꂽ���O�ƌ^���g�p���āA�����_���̉B�ꂽ���������쐬���܂��B
		/// <see cref="Parameters"/> �͍쐬���ꂽ������ێ����܂����A���� <see cref="Parameters"/> �ɃA�N�Z�X���邱�Ƃŏ�����ύX���邱�Ƃ��\�ł��B
		/// </summary>
		/// <param name="type">�쐬����鉼�����̌^���w�肵�܂��B</param>
		/// <param name="name">�쐬����鉼�����̖��O���w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ��������\�� <see cref="ParameterExpression"/>�B</returns>
		public ParameterExpression CreateHiddenParameter(Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			var result = Expression.Parameter(type, name);
			Parameters.Add(result);
			return result;
		}

		/// <summary>
		/// �w�肳�ꂽ���O�ƌ^���g�p���āA�����_���̔z��������쐬���܂��B
		/// �z������̓V�O�l�`���ɑ����ɒǉ�����܂��B
		/// �����_���쐬�����O�ɁA(�Ăяo�����͖����I�Ƀ��X�g�𑀍삷�邱�Ƃŏ�����ύX�ł��܂���) �r���_�[�͂��̈������Ō�ł��邩�ǂ������m�F���܂��B
		/// </summary>
		/// <param name="type">�쐬����鉼�����̌^���w�肵�܂��B</param>
		/// <param name="name">�쐬����鉼�����̖��O���w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ��������\�� <see cref="ParameterExpression"/>�B</returns>
		public ParameterExpression CreateParamsArray(Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.Requires(type.IsArray, "type");
			ContractUtils.Requires(type.GetArrayRank() == 1, "type");
			ContractUtils.Requires(ParamsArray == null, "type", "���łɔz����������݂��܂��B");
			return ParamsArray = Parameter(type, name);
		}

		/// <summary>�w�肳�ꂽ���O�ƌ^���g�p���āA���[�J���ϐ����쐬���܂��B</summary>
		/// <param name="type">�쐬����郍�[�J���ϐ��̌^���w�肵�܂��B</param>
		/// <param name="name">�쐬����郍�[�J���ϐ��̖��O���w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ���[�J���ϐ���\�� <see cref="ParameterExpression"/>�B</returns>
		public ParameterExpression ClosedOverVariable(Type type, string name)
		{
			var result = Expression.Variable(type, name);
			Locals.Add(result);
			_visibleVars.Add(new KeyValuePair<ParameterExpression, bool>(result, true));
			return result;
		}

		/// <summary>�w�肳�ꂽ���O�ƌ^���g�p���āA���[�J���ϐ����쐬���܂��B</summary>
		/// <param name="type">�쐬����郍�[�J���ϐ��̌^���w�肵�܂��B</param>
		/// <param name="name">�쐬����郍�[�J���ϐ��̖��O���w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ���[�J���ϐ���\�� <see cref="ParameterExpression"/>�B</returns>
		public ParameterExpression Variable(Type type, string name)
		{
			var result = Expression.Variable(type, name);
			Locals.Add(result);
			_visibleVars.Add(new KeyValuePair<ParameterExpression, bool>(result, false));
			return result;
		}

		/// <summary>�w�肳�ꂽ���O�ƌ^���g�p���āA�ꎞ�ϐ����쐬���܂��B</summary>
		/// <param name="type">�쐬����郍�[�J���ϐ��̌^���w�肵�܂��B</param>
		/// <param name="name">�쐬����郍�[�J���ϐ��̖��O���w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ���[�J���ϐ���\�� <see cref="ParameterExpression"/>�B</returns>
		public ParameterExpression HiddenVariable(Type type, string name)
		{
			var result = Expression.Variable(type, name);
			Locals.Add(result);
			return result;
		}

		/// <summary>
		/// �ꎞ�ϐ����r���_�[�ɂ���ĕێ������ϐ����X�g�ɒǉ����܂��B
		/// ����͕ϐ����r���_�[�̊O�ō쐬���ꂽ�ꍇ�ɕ֗��ł��B
		/// </summary>
		/// <param name="temp">�ǉ�����郍�[�J���ϐ���\�� <see cref="ParameterExpression"/> ���w�肵�܂��B</param>
		public void AddHiddenVariable(ParameterExpression temp)
		{
			ContractUtils.RequiresNotNull(temp, "temp");
			Locals.Add(temp);
		}

		/// <summary>
		/// ���̃r���_�[���� <see cref="LambdaExpression"/> ���쐬���܂��B
		/// ���̑���̌�́A���̃r���_�[�͑��̃C���X�^���X�̍쐬�Ɏg�p�ł��Ȃ��Ȃ�܂��B
		/// </summary>
		/// <param name="lambdaType">�쐬����郉���_���̌^���w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="LambdaExpression"/>�B</returns>
		public LambdaExpression MakeLambda(Type lambdaType)
		{
			Validate();
			EnsureSignature(lambdaType);
			var lambda = Expression.Lambda(lambdaType, AddDefaultReturn(MakeBody()), _name + "$" + Interlocked.Increment(ref _lambdaId), Parameters);
			// ���̃r���_�[�͍���������
			_completed = true;
			return lambda;
		}

		/// <summary>
		/// ���̃r���_�[���� <see cref="LambdaExpression"/> ���쐬���܂��B
		/// ���̑���̌�́A���̃r���_�[�͑��̃C���X�^���X�̍쐬�Ɏg�p�ł��Ȃ��Ȃ�܂��B
		/// </summary>
		/// <returns>�V�����쐬���ꂽ <see cref="LambdaExpression"/>�B</returns>
		public LambdaExpression MakeLambda()
		{
			ContractUtils.Requires(ParamsArray == null, "�z������̃����_�ɂ͖����I�ȃf���Q�[�g�^���K�v�ł��B");
			Validate();
			var lambda = Expression.Lambda(
				GetLambdaType(_returnType, Parameters),
				AddDefaultReturn(MakeBody()),
				_name + "$" + Interlocked.Increment(ref _lambdaId),
				Parameters
			);
			// ���̃r���_�[�͍���������
			_completed = true;
			return lambda;
		}

		/// <summary>
		/// ���̃r���_�[����W�F�l���[�^���܂� <see cref="LambdaExpression"/> ���쐬���܂��B
		/// ���̑���̌�́A���̃r���_�[�͑��̃C���X�^���X�̍쐬�Ɏg�p�ł��Ȃ��Ȃ�܂��B
		/// </summary>
		/// <param name="label">�����̃W�F�l���[�^���珈�������郉�x�����w�肵�܂��B</param>
		/// <param name="lambdaType">�Ԃ���郉���_���̌^���w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="LambdaExpression"/>�B</returns>
		public LambdaExpression MakeGenerator(LabelTarget label, Type lambdaType)
		{
			Validate();
			EnsureSignature(lambdaType);
			var lambda = Utils.GeneratorLambda(lambdaType, label, MakeBody(), _name + "$" + Interlocked.Increment(ref _lambdaId), Parameters);
			// ���̃r���_�[�͍���������
			_completed = true;
			return lambda;
		}

		/// <summary>�K�v�ł���΃����_���̖{�̂���юw�肳�ꂽ�f���Q�[�g�̃V�O�l�`���Ɉ�v���鉼�������\�z���܂��B</summary>
		void EnsureSignature(Type delegateType)
		{
			System.Diagnostics.Debug.Assert(Parameters != null, "�����ł͉��������X�g���K�v�ł��B");
			// paramMapping �̓L�[�������A�l�̓��_�C���N�g�����ׂ����ŁA�ǂ̂悤�Ɉ��������蓖�Ă邩���i�[����f�B�N�V���i���ł��B
			// ���݁A������ (�ǂ̂悤�ȕύX���K�v�Ȃ����Ƃ�����) ���ꎩ�g���A
			// ���̈������f���Q�[�g�V�O�l�`���ɑΉ����钼�ڈ����������Ȃ��ꍇ�ɁA�����_���ɒǉ�����鍇���ϐ��Ƀ��_�C���N�g����܂��B
			// ��:
			//     �f���Q�[�g�̃V�O�l�`��    del(x, params y[])
			//     �����_���̃V�O�l�`��      lambda(a, b, param n[])
			// ���̏󋵂ł͏�L�̊��蓖�Ă� <a, x>, <b, V1>, <n, V2> �̂悤�ɂȂ�܂��B
			// �����ŁAV1 ����� V2 �͍����ϐ��ŁA���̂悤�ɏ���������܂��B V1 = y[0], V2 = { y[1], y[2], ... y[n] }
			var delegateParams = delegateType.GetMethod("Invoke").GetParameters();
			var delegateHasParamarray = delegateParams.Any() && delegateParams.Last().IsDefined(typeof(ParamArrayAttribute), false);
			if (ParamsArray != null && !delegateHasParamarray)
				throw new ArgumentException("�z������̃����_�ɂ͔z������̃f���Q�[�g�^���K�v�ł��B");
			var copy = delegateHasParamarray ? delegateParams.Length - 1 : delegateParams.Length;
			var unwrap = Parameters.Count - copy - (ParamsArray != null ? 1 : 0);
			// �����_���ɂ͔z������������ď��Ȃ��Ƃ��f���Q�[�g�Ɠ����̉��������Ȃ��Ă͂Ȃ�܂���B
			if (unwrap < 0)
				throw new ArgumentException("�����_�ɏ\���ȉ�����������܂���B");
			// �����C�g���K�v�Ȃ���ΒZ������
			if (!delegateHasParamarray && Enumerable.Range(0, copy).All(x => Parameters[x].Type == delegateParams[x].ParameterType))
				return;
			List<ParameterExpression> newParams = new List<ParameterExpression>(delegateParams.Length);
			Dictionary<ParameterExpression, ParameterExpression> paramMapping = new Dictionary<ParameterExpression, ParameterExpression>();
			List<Tuple<ParameterExpression, Expression>> backings = new List<Tuple<ParameterExpression, Expression>>();
			for (int i = 0; i < copy; i++)
			{
				if (Parameters[i].Type != delegateParams[i].ParameterType)
				{
					// �ϊ����ꂽ�����Ɋ��蓖��
					var newParameter = Expression.Parameter(delegateParams[i].ParameterType, delegateParams[i].Name);
					var backingVariable = Expression.Variable(Parameters[i].Type, Parameters[i].Name);
					newParams.Add(newParameter);
					paramMapping.Add(Parameters[i], backingVariable);
					backings.Add(new Tuple<ParameterExpression, Expression>(backingVariable, newParameter));
				}
				else
				{
					// �������������g�p
					newParams.Add(Parameters[i]);
					paramMapping.Add(Parameters[i], Parameters[i]);
				}
			}
			if (delegateHasParamarray)
			{
				var delegateParamarray = Expression.Parameter(delegateParams.Last().ParameterType, delegateParams.Last().Name);
				newParams.Add(delegateParamarray);
				// �f���Q�[�g�̔z�������ϐ��փ��b�v�������āA��������ϐ��֊��蓖��
				for (int i = 0; i < unwrap; i++)
				{
					var backingVariable = Expression.Variable(Parameters[copy + i].Type, Parameters[copy + i].Name);
					paramMapping.Add(Parameters[copy + i], backingVariable);
					backings.Add(new Tuple<ParameterExpression, Expression>(backingVariable, Expression.ArrayAccess(delegateParamarray, AstUtils.Constant(i))));
				}
				// �����_���̔z������̓f���Q�[�g�̔z��������烉�b�v���������v�f���X�L�b�v���āA�c��̗v�f���擾����ׂ��B
				if (ParamsArray != null)
				{
					var backingVariable = Expression.Variable(ParamsArray.Type, ParamsArray.Name);
					paramMapping.Add(ParamsArray, backingVariable);
					// �w���p�[�Ăяo��
					backings.Add(new Tuple<ParameterExpression, Expression>(backingVariable, Expression.Call(
						new Func<int[], int, int[]>(RuntimeHelpers.ShiftParamsArray).Method.GetGenericMethodDefinition()
						.MakeGenericMethod(delegateParamarray.Type.GetElementType()),
						delegateParamarray,
						AstUtils.Constant(unwrap)
					)));
				}
			}
			_body = Expression.Block(
				backings.Select(x => Expression.Assign(x.Item1, AstUtils.Convert(x.Item2, x.Item1.Type)))
				.Concat(Enumerable.Repeat(new LambdaParameterRewriter(paramMapping).Visit(_body), 1))
			);
			ParamsArray = null;
			Locals.AddRange(backings.Select(x => x.Item1));
			Parameters = newParams;
			for (int i = 0; i < _visibleVars.Count; i++)
			{
				var p = _visibleVars[i].Key as ParameterExpression;
				ParameterExpression v;
				if (p != null && paramMapping.TryGetValue(p, out v))
					_visibleVars[i] = new KeyValuePair<ParameterExpression, bool>(v, _visibleVars[i].Value);
			}
		}

		/// <summary>�����_���쐬����̂ɏ\���ȏ����r���_�[���ێ����Ă��邩�ǂ��������؂��܂��B</summary>
		void Validate()
		{
			if (_completed)
				throw new InvalidOperationException("�r���_�[�̓N���[�Y����Ă��܂��B");
			if (_returnType == null)
				throw new InvalidOperationException("�߂�l�̌^���w�肳��Ă��܂���B");
			if (_name == null)
				throw new InvalidOperationException("���O���w�肳��Ă��܂���B");
			if (_body == null)
				throw new InvalidOperationException("�{�̂��w�肳��Ă��܂���B");
			if (ParamsArray != null && (Parameters.Count == 0 || Parameters[Parameters.Count - 1] != ParamsArray))
				throw new InvalidOperationException("�z������̉����������������X�g�̍Ō�ɂ���܂���B");
		}

		// �K�v�ł���΃X�R�[�v�����b�v���܂��B
		Expression MakeBody() { return Locals != null && Locals.Count > 0 ? Expression.Block(Locals, _body) : _body; }

		// �K�v�ł���Ί���̖߂�l��ǉ����܂��B
		Expression AddDefaultReturn(Expression body) { return body.Type == typeof(void) && _returnType != typeof(void) ? Expression.Block(body, Utils.Default(_returnType)) : body; }

		static Type GetLambdaType(Type returnType, IEnumerable<ParameterExpression> parameterList)
		{
			parameterList = parameterList ?? Enumerable.Empty<ParameterExpression>();
			ContractUtils.RequiresNotNull(returnType, "returnType");
			ContractUtils.RequiresNotNullItems(parameterList, "parameter");
			return Expression.GetDelegateType(ArrayUtils.Append(parameterList.Select(x => x.Type).ToArray(), returnType));
		}
	}

	public static partial class Utils
	{
		/// <summary>�w�肳�ꂽ���O�Ɩ߂�l�̌^���g�p���āA<see cref="LambdaBuilder"/> �̐V�����C���X�^���X���쐬���܂��B</summary>
		/// <param name="returnType">�\�z����郉���_���̖߂�l�̌^���w�肵�܂��B</param>
		/// <param name="name">�\�z����郉���_���̖��O���w�肵�܂��B</param>
		/// <returns>�V���� <see cref="LambdaBuilder"/> �̃C���X�^���X�B</returns>
		public static LambdaBuilder Lambda(Type returnType, string name) { return new LambdaBuilder(name, returnType); }
	}
}

namespace Microsoft.Scripting.Runtime
{
	public static partial class ScriptingRuntimeHelpers
	{
		/// <summary>�w�肳�ꂽ�z��������w�肳�ꂽ�����ɃV�t�g�����c���Ԃ��܂��B</summary>
		/// <param name="array">�V�t�g����z����w�肵�܂��B</param>
		/// <param name="count">�V�t�g��������w�肵�܂��B</param>
		/// <returns><paramref name="count"/> �����ɃV�t�g���ꂽ�z��B�V�t�g�ʂ��͈͂𒴂��Ă���ꍇ�͋�̔z���Ԃ��܂��B</returns>
		public static T[] ShiftParamsArray<T>(T[] array, int count) { return array != null && array.Length > count ? ArrayUtils.ShiftLeft(array, count) : new T[0]; }
	}
}
