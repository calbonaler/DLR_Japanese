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

using System.Linq;
using System.Reflection;
using Microsoft.Contracts;

namespace Microsoft.Scripting.Generation
{
	/// <summary>
	/// �����ȃV�O�l�`���������\�b�h����菜���w���p�[ �N���X�ł��B
	/// �p���K�w���̂��ׂĂ̌^���烁���o��Ԃ� GetDefaultMembers �ɂ���Ďg�p����܂��B
	/// </summary>
	public class MethodSignatureInfo
	{
		readonly ParameterInfo[] _pis;
		readonly bool _isStatic;
		readonly int _genericArity;

		/// <summary>���\�b�h�̏���ێ����� <see cref="MethodInfo"/> ���g�p���āA<see cref="Microsoft.Scripting.Generation.MethodSignatureInfo"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">�V�O�l�`���������o�����\�b�h��\�� <see cref="MethodInfo"/> ���w�肵�܂��B</param>
		public MethodSignatureInfo(MethodInfo info) : this(info.IsStatic, info.GetParameters(), info.IsGenericMethodDefinition ? info.GetGenericArguments().Length : 0) { }

		/// <summary>���\�b�h�̏��𒼐ڎw�肷�邱�ƂŁA<see cref="Microsoft.Scripting.Generation.MethodSignatureInfo"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="isStatic">���\�b�h���ÓI�ł��邩�ǂ����������l���w�肵�܂��B</param>
		/// <param name="pis">���\�b�h�̉������Ɋւ�������i�[���� <see cref="ParameterInfo"/> �̔z����w�肵�܂��B</param>
		/// <param name="genericArity">���\�b�h�̃W�F�l���b�N�^�p�����[�^�̐����w�肵�܂��B</param>
		public MethodSignatureInfo(bool isStatic, ParameterInfo[] pis, int genericArity)
		{
			_isStatic = isStatic;
			_pis = pis;
			_genericArity = genericArity;
		}

		/// <summary>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">���f����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[Confined]
		public override bool Equals(object obj)
		{
			MethodSignatureInfo args = obj as MethodSignatureInfo;
			return args != null && _isStatic == args._isStatic && _genericArity == args._genericArity && _pis.Select(x => x.ParameterType).SequenceEqual(args._pis.Select(x => x.ParameterType));
		}

		/// <summary>���̃I�u�W�F�N�g�̃n�b�V���l���v�Z���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̃n�b�V���l�B</returns>
		[Confined]
		public override int GetHashCode() { return _pis.Aggregate(6551 ^ (_isStatic ? 79234 : 3123) ^ _genericArity, (x, y) => x ^ y.ParameterType.GetHashCode()); }
	}
}
