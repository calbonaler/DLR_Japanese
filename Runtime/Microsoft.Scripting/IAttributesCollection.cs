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

namespace Microsoft.Scripting
{
	/// <summary>�V���{������єC�ӂ̃I�u�W�F�N�g���g�p���邱�ƂŃA�N�Z�X�\�ȃf�B�N�V���i����\���܂��B</summary>
	/// <remarks>
	/// ���̃C���^�[�t�F�C�X�͊T�O�I�� <see cref="IDictionary&lt;Object, Object&gt;"/> ���p�����܂����A
	/// �I�u�W�F�N�g�ł͂Ȃ� <see cref="SymbolId"/> �ɃC���f�b�N�X�����悤�ɂ������̂ł��̂悤�ɂ��܂���B
	/// </remarks>
	public interface IAttributesCollection : IEnumerable<KeyValuePair<object, object>>
	{
		/// <summary>�w�肵���L�[����ђl�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> �ɒǉ����܂��B</summary>
		/// <param name="name">�ǉ�����v�f�̃L�[�Ƃ��Ďg�p���� <see cref="SymbolId"/>�B</param>
		/// <param name="value">�ǉ�����v�f�̒l�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		void Add(SymbolId name, object value);

		/// <summary>�w�肵���L�[�Ɋ֘A�t�����Ă���l���擾���܂��B</summary>
		/// <param name="name">�l���擾����Ώۂ̃L�[�B</param>
		/// <param name="value">
		/// ���̃��\�b�h���Ԃ����Ƃ��ɁA�L�[�����������ꍇ�́A�w�肵���L�[�Ɋ֘A�t�����Ă���l�B����ȊO�̏ꍇ�� <c>null</c>�B
		/// ���̃p�����[�^�[�͏����������ɓn����܂��B
		/// </param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> ����������I�u�W�F�N�g�Ɋi�[����Ă���ꍇ��
		/// <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// </returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
		bool TryGetValue(SymbolId name, out object value);

		/// <summary>�w�肵���L�[�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> ����폜���܂��B</summary>
		/// <param name="name">�폜����v�f�̃L�[�B</param>
		/// <returns>
		/// �v�f������ɍ폜���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// ���̃��\�b�h�́A<paramref name="name"/> ������ <see cref="Microsoft.Scripting.IAttributesCollection"/> �Ɍ�����Ȃ������ꍇ�ɂ� <c>false</c> ��Ԃ��܂��B
		/// </returns>
		bool Remove(SymbolId name);

		/// <summary>�w�肵���L�[�̗v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> �Ɋi�[����Ă��邩�ǂ������m�F���܂��B</summary>
		/// <param name="name"><see cref="Microsoft.Scripting.IAttributesCollection"/> ���Ō��������L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> ���ێ����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		bool ContainsKey(SymbolId name);

		/// <summary>�w�肵���L�[�����v�f���擾�܂��͐ݒ肵�܂��B</summary>
		/// <param name="name">�擾�܂��͐ݒ肷��v�f�̃L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1043:UseIntegralOrStringArgumentForIndexers")]
		object this[SymbolId name] { get; set; }

		/// <summary><see cref="SymbolId"/> ���L�[�ł��鑮���̃f�B�N�V���i�����擾���܂��B</summary>
		IDictionary<SymbolId, object> SymbolAttributes { get; }

		/// <summary>�w�肵���L�[����ђl�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> �ɒǉ����܂��B</summary>
		/// <param name="name">�ǉ�����v�f�̃L�[�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		/// <param name="value">�ǉ�����v�f�̒l�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		void Add(object name, object value);

		/// <summary>�w�肵���L�[�Ɋ֘A�t�����Ă���l���擾���܂��B</summary>
		/// <param name="name">�l���擾����Ώۂ̃L�[�B</param>
		/// <param name="value">
		/// ���̃��\�b�h���Ԃ����Ƃ��ɁA�L�[�����������ꍇ�́A�w�肵���L�[�Ɋ֘A�t�����Ă���l�B����ȊO�̏ꍇ�� <c>null</c>�B
		/// ���̃p�����[�^�[�͏����������ɓn����܂��B
		/// </param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> ����������I�u�W�F�N�g�Ɋi�[����Ă���ꍇ��
		/// <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// </returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
		bool TryGetValue(object name, out object value);

		/// <summary>�w�肵���L�[�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> ����폜���܂��B</summary>
		/// <param name="name">�폜����v�f�̃L�[�B</param>
		/// <returns>
		/// �v�f������ɍ폜���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// ���̃��\�b�h�́A<paramref name="name"/> ������ <see cref="Microsoft.Scripting.IAttributesCollection"/> �Ɍ�����Ȃ������ꍇ�ɂ� <c>false</c> ��Ԃ��܂��B
		/// </returns>
		bool Remove(object name);

		/// <summary>�w�肵���L�[�̗v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> �Ɋi�[����Ă��邩�ǂ������m�F���܂��B</summary>
		/// <param name="name"><see cref="Microsoft.Scripting.IAttributesCollection"/> ���Ō��������L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> ���ێ����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		bool ContainsKey(object name);

		/// <summary>���̃I�u�W�F�N�g�� <see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> �Ƃ��Ď擾���܂��B</summary>
		/// <returns><see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> �Ƃ����`���Ŏ擾���ꂽ���݂̃I�u�W�F�N�g�B</returns>
		IDictionary<object, object> AsObjectKeyedDictionary();

		/// <summary><see cref="Microsoft.Scripting.IAttributesCollection"/> �Ɋi�[����Ă���v�f�̐����擾���܂��B</summary>
		int Count { get; }

		/// <summary><see cref="Microsoft.Scripting.IAttributesCollection"/> �̃L�[��ێ����Ă��� <see cref="System.Collections.Generic.ICollection&lt;Object&gt;"/> ���擾���܂��B</summary>
		ICollection<object> Keys { get; }
	}
}
