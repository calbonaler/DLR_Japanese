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
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Contracts;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>�R�[���T�C�g�̃V�O�l�`����L�x�ɕ\���܂��B</summary>
	public struct CallSignature : IEquatable<CallSignature>
	{
		// TODO: invariant _infos != null ==> _argumentCount == _infos.Length

		/// <summary>
		/// ���O�t�������̂悤�Ȉ����Ɋւ���ǉ��̏����i�[���܂��B
		/// �P���ȃV�O�l�`���A�܂莮�̃��X�g�̏ꍇ�� null �ɂȂ�܂��B
		/// </summary>
		readonly Argument[] _infos;
		/// <summary>�V�O�l�`�����Ɋ܂܂������̌��ł��B</summary>
		readonly int _argumentCount;

		/// <summary>���ׂĂ̈��������O�t���łȂ��A�ʒu�����Ɍ��肳��Ă��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsSimple { get { return _infos == null; } }

		/// <summary>�V�O�l�`�����Ɋ܂܂������̌����擾���܂��B</summary>
		public int ArgumentCount
		{
			get
			{
				Debug.Assert(_infos == null || _infos.Length == _argumentCount);
				return _argumentCount;
			}
		}

		/// <summary>�w�肳�ꂽ���̒P���Ȉ��������� <see cref="Microsoft.Scripting.Actions.CallSignature"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="argumentCount">�쐬����� <see cref="CallSignature"/> ���ێ�����P���Ȉ����̐����w�肵�܂��B</param>
		public CallSignature(int argumentCount)
		{
			ContractUtils.Requires(argumentCount >= 0, "argumentCount");
			_argumentCount = argumentCount;
			_infos = null;
		}

		/// <summary>�w�肳�ꂽ�����̃��X�g���g�p���āA<see cref="Microsoft.Scripting.Actions.CallSignature"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="infos">�����̃��X�g���w�肵�܂��B</param>
		public CallSignature(params Argument[] infos)
		{
			if (infos != null)
			{
				_argumentCount = infos.Length;
				_infos = Array.Exists(infos, x => x.Kind != ArgumentType.Simple) ? infos : null;
			}
			else
			{
				_argumentCount = 0;
				_infos = null;
			}
		}

		/// <summary>�w�肳�ꂽ�����̎�ނ̃��X�g���g�p���āA<see cref="Microsoft.Scripting.Actions.CallSignature"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="kinds">�����̎�ނ̃��X�g���w�肵�܂��B</param>
		public CallSignature(params ArgumentType[] kinds)
		{
			if (kinds != null)
			{
				_argumentCount = kinds.Length;
				_infos = Array.Exists(kinds, x => x != ArgumentType.Simple) ? Array.ConvertAll(kinds, x => new Argument(x)) : null;
			}
			else
			{
				_argumentCount = 0;
				_infos = null;
			}
		}

		/// <summary>���� <see cref="CallSignature"/> ���w�肳�ꂽ <see cref="CallSignature"/> �Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="other">��r���� <see cref="CallSignature"/> ���w�肵�܂��B</param>
		/// <returns>���� <see cref="CallSignature"/> ���w�肳�ꂽ <see cref="CallSignature"/> �Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[StateIndependent]
		public bool Equals(CallSignature other) { return _infos == null ? other._infos == null && other._argumentCount == _argumentCount : other._infos != null && _infos.SequenceEqual(other._infos); }

		/// <summary>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">��r����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public override bool Equals(object obj) { return obj is CallSignature && Equals((CallSignature)obj); }

		/// <summary>2 �� <see cref="CallSignature"/> �����������ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �Ԗڂ� <see cref="CallSignature"/>�B</param>
		/// <param name="right">��r���� 2 �Ԗڂ� <see cref="CallSignature"/>�B</param>
		/// <returns>2 �� <see cref="CallSignature"/> ���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator ==(CallSignature left, CallSignature right) { return left.Equals(right); }

		/// <summary>2 �� <see cref="CallSignature"/> ���������Ȃ����ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �Ԗڂ� <see cref="CallSignature"/>�B</param>
		/// <param name="right">��r���� 2 �Ԗڂ� <see cref="CallSignature"/>�B</param>
		/// <returns>2 �� <see cref="CallSignature"/> ���������Ȃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator !=(CallSignature left, CallSignature right) { return !left.Equals(right); }

		/// <summary>���̃I�u�W�F�N�g�̕�����\����Ԃ��܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return _infos == null ? "Simple" : "(" + string.Join(", ", _infos) + ")"; }

		/// <summary>���̃I�u�W�F�N�g�̃n�b�V���l���v�Z���܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g�̃n�b�V���l�B</returns>
		public override int GetHashCode() { return _infos == null ? 6551 : _infos.Aggregate(6551, (x, y) => x ^ (x << 5) ^ y.GetHashCode()); }

		/// <summary>���� <see cref="CallSignature"/> �Ɋi�[����Ă�������� <see cref="Argument"/> �I�u�W�F�N�g�̔z��Ƃ��ĕԂ��܂��B</summary>
		/// <returns><see cref="CallSignature"/> �Ɋi�[����Ă��邷�ׂĂ̈������i�[���ꂽ <see cref="Argument"/> �I�u�W�F�N�g�̔z��B</returns>
		public Argument[] GetArgumentInfos() { return _infos != null ? ArrayUtils.Copy(_infos) : Enumerable.Repeat(Argument.Simple, _argumentCount).ToArray(); }

		/// <summary>���� <see cref="CallSignature"/> �̐擪�Ɏw�肳�ꂽ�������������V���� <see cref="CallSignature"/> ��Ԃ��܂��B</summary>
		/// <param name="info">�擪�ɒǉ�����������w�肵�܂��B</param>
		/// <returns>���� <see cref="CallSignature"/> �̐擪�Ɉ������ǉ����ꂽ�V���� <see cref="CallSignature"/>�B</returns>
		public CallSignature InsertArgument(Argument info) { return InsertArgumentAt(0, info); }

		/// <summary>���� <see cref="CallSignature"/> �̎w�肳�ꂽ�ʒu�Ɏw�肳�ꂽ�������������V���� <see cref="CallSignature"/> ��Ԃ��܂��B</summary>
		/// <param name="index">������ǉ�����ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="info">�ǉ�����������w�肵�܂��B</param>
		/// <returns>���� <see cref="CallSignature"/> �̎w�肳�ꂽ�ʒu�Ɉ������ǉ����ꂽ�V���� <see cref="CallSignature"/>�B</returns>
		public CallSignature InsertArgumentAt(int index, Argument info)
		{
			if (IsSimple)
			{
				if (info.IsSimple)
					return new CallSignature(_argumentCount + 1);
				return new CallSignature(ArrayUtils.InsertAt(GetArgumentInfos(), index, info));
			}
			return new CallSignature(ArrayUtils.InsertAt(_infos, index, info));
		}

		/// <summary>���� <see cref="CallSignature"/> �̐擪�����������菜�����V���� <see cref="CallSignature"/> ��Ԃ��܂��B</summary>
		/// <returns>���� <see cref="CallSignature"/> �̐擪����������폜���ꂽ�V���� <see cref="CallSignature"/>�B</returns>
		public CallSignature RemoveFirstArgument() { return RemoveArgumentAt(0); }

		/// <summary>���� <see cref="CallSignature"/> �̎w�肳�ꂽ�ʒu�����������菜�����V���� <see cref="CallSignature"/> ��Ԃ��܂��B</summary>
		/// <param name="index">�������폜����ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���� <see cref="CallSignature"/> �̎w�肳�ꂽ�ʒu����������폜���ꂽ�V���� <see cref="CallSignature"/>�B</returns>
		public CallSignature RemoveArgumentAt(int index)
		{
			if (_argumentCount == 0)
				throw new InvalidOperationException();
			if (IsSimple)
				return new CallSignature(_argumentCount - 1);
			return new CallSignature(ArrayUtils.RemoveAt(_infos, index));
		}

		/// <summary>���� <see cref="CallSignature"/> ���Ŏw�肳�ꂽ��ނ̈������ŏ��Ɍ��������ʒu������ 0 ����n�܂�C���f�b�N�X��Ԃ��܂��B</summary>
		/// <param name="kind"><see cref="CallSignature"/> ����������������̎�ނ��w�肵�܂��B</param>
		/// <returns><see cref="CallSignature"/> ���Ŏw�肳�ꂽ��ނ̈������ŏ��Ɍ��������ʒu������ 0 ����n�܂�C���f�b�N�X�B������Ȃ������ꍇ�� -1 ��Ԃ��܂��B</returns>
		public int IndexOf(ArgumentType kind) { return _infos == null ? (kind == ArgumentType.Simple && _argumentCount > 0 ? 0 : -1) : Array.FindIndex(_infos, x => x.Kind == kind); }

		/// <summary>���� <see cref="CallSignature"/> ���Ɏ����������܂܂�Ă��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <returns><see cref="CallSignature"/> ���Ɏ����������܂܂�Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool HasDictionaryArgument() { return IndexOf(ArgumentType.Dictionary) > -1; }

		/// <summary>���� <see cref="CallSignature"/> ���ɃC���X�^���X�������܂܂�Ă��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <returns><see cref="CallSignature"/> ���ɃC���X�^���X�������܂܂�Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool HasInstanceArgument() { return IndexOf(ArgumentType.Instance) > -1; }

		/// <summary>���� <see cref="CallSignature"/> ���ɔz��������܂܂�Ă��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <returns><see cref="CallSignature"/> ���ɔz��������܂܂�Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool HasListArgument() { return IndexOf(ArgumentType.List) > -1; }

		/// <summary>���� <see cref="CallSignature"/> ���ɖ��O�t���������܂܂�Ă��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <returns><see cref="CallSignature"/> ���ɖ��O�t���������܂܂�Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		internal bool HasNamedArgument() { return IndexOf(ArgumentType.Named) > -1; }

		/// <summary>���� <see cref="CallSignature"/> ���Ɏ��������܂��͖��O�t���������܂܂�Ă��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <returns><see cref="CallSignature"/> ���Ɏ��������܂��͖��O�t���������܂܂�Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool HasKeywordArgument() { return _infos != null && Array.Exists(_infos, x => x.Kind == ArgumentType.Dictionary || x.Kind == ArgumentType.Named); }

		/// <summary>���� <see cref="CallSignature"/> ���̎w�肳�ꂽ�ʒu�ɑ��݂�������̎�ނ�Ԃ��܂��B</summary>
		/// <param name="index">��ނ��擾��������̈ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�ʒu�ɑ��݂�������̎�ށB</returns>
		public ArgumentType GetArgumentKind(int index) { return _infos != null ? _infos[index].Kind : ArgumentType.Simple; } // TODO: Contract.Requires(index >= 0 && index < _argumentCount, "index");

		/// <summary>���� <see cref="CallSignature"/> ���̎w�肳�ꂽ�ʒu�ɑ��݂�������̖��O��Ԃ��܂��B</summary>
		/// <param name="index">���O���擾��������̈ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�ʒu�ɑ��݂�������̖��O�B���O�����݂��Ȃ��ꍇ�� <c>null</c> ��Ԃ��܂��B</returns>
		public string GetArgumentName(int index)
		{
			ContractUtils.Requires(index >= 0 && index < _argumentCount);
			return _infos != null ? _infos[index].Name : null;
		}

		/// <summary>���[�U�[���R�[���T�C�g�Œ񋟂����ʒu����ς݈����̐���Ԃ��܂��B</summary>
		/// <returns>�ʒu����ς݂̈����̌��B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public int GetProvidedPositionalArgumentCount() { return _argumentCount - (_infos != null ? _infos.Count(x => x.Kind == ArgumentType.Dictionary || x.Kind == ArgumentType.List || x.Kind == ArgumentType.Named) : 0); }

		/// <summary>���� <see cref="CallSignature"/> �Ɋi�[����Ă��邷�ׂĂ̈����̖��O��Ԃ��܂��B</summary>
		/// <returns>���� <see cref="CallSignature"/> ���̂��ׂĂ̈����̖��O���܂�ł���z��B</returns>
		public string[] GetArgumentNames() { return _infos == null ? ArrayUtils.EmptyStrings : _infos.Where(x => x.Name != null).Select(x => x.Name).ToArray(); }

		/// <summary>���̃I�u�W�F�N�g��\�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g��\�� <see cref="Expression"/>�B</returns>
		public Expression CreateExpression()
		{
			if (_infos == null)
				return Expression.New(typeof(CallSignature).GetConstructor(new[] { typeof(int) }), AstUtils.Constant(ArgumentCount));
			else
				return Expression.New(
					typeof(CallSignature).GetConstructor(new[] { typeof(Argument[]) }),
					Expression.NewArrayInit(typeof(Argument), _infos.Select(x => x.CreateExpression()))
				);
		}
	}
}
