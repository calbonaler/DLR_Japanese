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
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Scripting
{
	/// <summary>
	/// �񋓂̏�Ԃ��Ď����Ė����ȗ񋓑���ŗ�O�𔭐�������񋓎q�̒��ۊ�{�N���X��\���܂��B
	/// ���̃N���X�͎�ɔ�W�F�l���b�N �f�B�N�V���i���̗񋓂Ɏg�p����܂��B
	/// </summary>
	public abstract class CheckedDictionaryEnumerator : IDictionaryEnumerator, IEnumerator<KeyValuePair<object, object>>
	{
		EnumeratorState _enumeratorState = EnumeratorState.NotStarted;

		void CheckEnumeratorState()
		{
			if (_enumeratorState == EnumeratorState.NotStarted)
				throw Error.EnumerationNotStarted();
			else if (_enumeratorState == EnumeratorState.Ended)
				throw Error.EnumerationFinished();
		}

		/// <summary>���݂̃f�B�N�V���i�� �G���g���̃L�[�ƒl�̗������擾���܂��B</summary>
		public DictionaryEntry Entry
		{
			get
			{
				CheckEnumeratorState();
				return new DictionaryEntry(Key, Value);
			}
		}

		/// <summary>���݂̃f�B�N�V���i�� �G���g���̃L�[���擾���܂��B</summary>
		public object Key
		{
			get
			{
				CheckEnumeratorState();
				return KeyCore;
			}
		}

		/// <summary>���݂̃f�B�N�V���i�� �G���g���̒l���擾���܂��B</summary>
		public object Value
		{
			get
			{
				CheckEnumeratorState();
				return ValueCore;
			}
		}

		/// <summary>�񋓎q���R���N�V�����̎��̗v�f�ɐi�߂܂��B</summary>
		/// <returns>�񋓎q�����̗v�f�ɐ���ɐi�񂾏ꍇ�� <c>true</c>�B�񋓎q���R���N�V�����̖������z�����ꍇ�� <c>false</c>�B</returns>
		/// <exception cref="InvalidOperationException">�񋓎q���쐬���ꂽ��ɁA�R���N�V�������ύX����܂����B</exception>
		public bool MoveNext()
		{
			if (_enumeratorState == EnumeratorState.Ended)
				throw Error.EnumerationFinished();
			var result = MoveNextCore();
			if (result)
				_enumeratorState = EnumeratorState.Started;
			else
				_enumeratorState = EnumeratorState.Ended;
			return result;
		}

		/// <summary>�R���N�V�������̌��݂̗v�f���擾���܂��B</summary>
		public object Current { get { return Entry; } }

		/// <summary>�񋓎q�������ʒu�A�܂�R���N�V�����̍ŏ��̗v�f�̑O�ɐݒ肵�܂��B</summary>
		/// <exception cref="InvalidOperationException">�񋓎q���쐬���ꂽ��ɁA�R���N�V�������ύX����܂����B</exception>
		public void Reset()
		{
			ResetCore();
			_enumeratorState = EnumeratorState.NotStarted;
		}

		/// <summary>�񋓎q�̌��݈ʒu�ɂ���R���N�V�������̗v�f���擾���܂��B</summary>
		KeyValuePair<object, object> IEnumerator<KeyValuePair<object, object>>.Current { get { return new KeyValuePair<object, object>(Key, Value); } }

		/// <summary>�A���}�l�[�W ���\�[�X�̉������у��Z�b�g�Ɋ֘A�t�����Ă���A�v���P�[�V������`�̃^�X�N�����s���܂��B</summary>
		public void Dispose() { GC.SuppressFinalize(this); }

		/// <summary>���݂̃f�B�N�V���i�� �G���g���̃L�[���擾���܂��B</summary>
		protected abstract object KeyCore { get; }

		/// <summary>���݂̃f�B�N�V���i�� �G���g���̒l���擾���܂��B</summary>
		protected abstract object ValueCore { get; }

		/// <summary>�񋓎q���R���N�V�����̎��̗v�f�ɐi�߂܂��B</summary>
		/// <returns>�񋓎q�����̗v�f�ɐ���ɐi�񂾏ꍇ�� <c>true</c>�B�񋓎q���R���N�V�����̖������z�����ꍇ�� <c>false</c>�B</returns>
		protected abstract bool MoveNextCore();

		/// <summary>�񋓎q�������ʒu�A�܂�R���N�V�����̍ŏ��̗v�f�̑O�ɐݒ肵�܂��B</summary>
		protected abstract void ResetCore();

		enum EnumeratorState
		{
			NotStarted,
			Started,
			Ended
		}
	}
}
