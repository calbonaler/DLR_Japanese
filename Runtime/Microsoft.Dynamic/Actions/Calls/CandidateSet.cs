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
using System.Diagnostics;
using Microsoft.Contracts;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>���ׂē������̘_���������󂯓���� <see cref="MethodCandidate"/> �̃R���N�V������\���܂��B</summary>
	sealed class CandidateSet : IList<MethodCandidate>
	{
		List<MethodCandidate> candidates = new List<MethodCandidate>();

		/// <summary>�󂯓����_�������̐����g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.CandidateSet"/> �N���X�̐V�����C���X�^���X���쐬���܂��B</summary>
		/// <param name="count">�󂯓����_�������̐����w�肵�܂��B</param>
		internal CandidateSet(int count)
		{
			Arity = count;
			candidates = new List<MethodCandidate>();
		}

		/// <summary>�󂯓����_�������̐����擾���܂��B</summary>
		internal int Arity { get; private set; }

		/// <summary>�i�[����Ă��邷�ׂĂ� <see cref="MethodCandidate"/> �̈������X�g�Ɏ������������݂��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <returns>���ׂĂ� <see cref="MethodCandidate"/> �̈������X�g�Ɏ������������݂���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		internal bool IsParamsDictionaryOnly() { return candidates.TrueForAll(x => x.HasParamsDictionary); }

		/// <summary>���̃R���N�V�������Ŏw�肳�ꂽ <see cref="MethodCandidate"/> �����݂���ʒu��Ԃ��܂��B</summary>
		/// <param name="item">���̃R���N�V���������猟������ <see cref="MethodCandidate"/> ���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ <see cref="MethodCandidate"/> �����݂���C���f�b�N�X�B������Ȃ��ꍇ�� -1�B</returns>
		public int IndexOf(MethodCandidate item) { return candidates.IndexOf(item); }

		/// <summary>���̃R���N�V�����̎w�肳�ꂽ�ʒu�ɐV���� <see cref="MethodCandidate"/> ��ǉ����܂��B</summary>
		/// <param name="index"><see cref="MethodCandidate"/> ��ǉ�����ʒu���w�肵�܂��B</param>
		/// <param name="item">�ǉ����� <see cref="MethodCandidate"/> ���w�肵�܂��B</param>
		public void Insert(int index, MethodCandidate item)
		{
			Debug.Assert(item.Parameters.Count == Arity);
			candidates.Insert(index, item);
		}

		/// <summary>���̃R���N�V��������w�肳�ꂽ�ʒu�ɂ��� <see cref="MethodCandidate"/> ���폜���܂��B</summary>
		/// <param name="index">�폜���� <see cref="MethodCandidate"/> �̈ʒu���w�肵�܂��B</param>
		public void RemoveAt(int index) { candidates.RemoveAt(index); }

		/// <summary>���̃R���N�V�������̎w�肳�ꂽ�ʒu�ɂ��� <see cref="MethodCandidate"/> ���擾�܂��͐ݒ肵�܂��B</summary>
		/// <param name="index"><see cref="MethodCandidate"/> ���擾�܂��͐ݒ肷��ʒu���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�ʒu�ɂ��� <see cref="MethodCandidate"/>�B</returns>
		public MethodCandidate this[int index]
		{
			get { return candidates[index]; }
			set
			{
				Debug.Assert(value.Parameters.Count == Arity);
				candidates[index] = value;
			}
		}

		/// <summary>���̃R���N�V�����ɐV���� <see cref="MethodCandidate"/> ��ǉ����܂��B</summary>
		/// <param name="item">�ǉ����� <see cref="MethodCandidate"/> ���w�肵�܂��B</param>
		public void Add(MethodCandidate item)
		{
			Debug.Assert(item.Parameters.Count == Arity);
			candidates.Add(item);
		}

		/// <summary>���̃R���N�V�������̂��ׂĂ� <see cref="MethodCandidate"/> ���폜���܂��B</summary>
		public void Clear() { candidates.Clear(); }

		/// <summary>���̃R���N�V�����Ɏw�肳�ꂽ <see cref="MethodCandidate"/> �����݂��邩�ǂ�����Ԃ��܂��B</summary>
		/// <param name="item">���݂��邩�ǂ����𒲂ׂ� <see cref="MethodCandidate"/> ���w�肵�܂��B</param>
		/// <returns>�R���N�V�������� <see cref="MethodCandidate"/> �����݂���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool Contains(MethodCandidate item) { return candidates.Contains(item); }

		/// <summary>���̃R���N�V�������� <see cref="MethodCandidate"/> ���w�肳�ꂽ�z��ɃR�s�[���܂��B</summary>
		/// <param name="array"><see cref="MethodCandidate"/> ���R�s�[�����z����w�肵�܂��B</param>
		/// <param name="arrayIndex"><paramref name="array"/> ���̃R�s�[���J�n����� 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		public void CopyTo(MethodCandidate[] array, int arrayIndex) { candidates.CopyTo(array, arrayIndex); }

		/// <summary>���̃R���N�V�����Ɋi�[����Ă��� <see cref="MethodCandidate"/> �̐����擾���܂��B</summary>
		public int Count { get { return candidates.Count; } }

		/// <summary>���̃R���N�V�������ǂݎ���p���ǂ����������l���擾���܂��B</summary>
		public bool IsReadOnly { get { return false; } }

		/// <summary>���̃R���N�V����������w�肳�ꂽ <see cref="MethodCandidate"/> ���폜���܂��B</summary>
		/// <param name="item">�폜���� <see cref="MethodCandidate"/> ���w�肵�܂��B</param>
		/// <returns>�폜������Ɏ��s���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool Remove(MethodCandidate item) { return candidates.Remove(item); }

		/// <summary>���̃R���N�V�����𔽕���������񋓎q��Ԃ��܂��B</summary>
		/// <returns>�R���N�V�����̔��������Ɏg�p����񋓎q�B</returns>
		public IEnumerator<MethodCandidate> GetEnumerator() { return candidates.GetEnumerator(); }

		/// <summary>���̃R���N�V�����𔽕���������񋓎q��Ԃ��܂��B</summary>
		/// <returns>�R���N�V�����̔��������Ɏg�p����񋓎q�B</returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		
		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return string.Format("{0}: ({1} on {2})", Arity, candidates[0].Overload.Name, candidates[0].Overload.DeclaringType.FullName); }
	}
}
