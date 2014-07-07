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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>����̃Z�b�g�A�b�v�ɕK�v�ȏ����i�[���܂��B</summary>
	[Serializable]
	public sealed class LanguageSetup
	{
		string _typeName;
		string _displayName;
		bool _frozen;
		bool? _exceptionDetail;

		/// <summary>����v���o�C�_�̃A�Z���u���C���^�����g�p���āA<see cref="Microsoft.Scripting.Hosting.LanguageSetup"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="typeName">����v���o�C�_��\���A�Z���u���C���^�����w�肵�܂��B</param>
		public LanguageSetup(string typeName) : this(typeName, "", ArrayUtils.EmptyStrings, ArrayUtils.EmptyStrings) { }

		/// <summary>
		/// ����v���o�C�_�̃A�Z���u���C���^������ь���̕\�������g�p���āA<see cref="Microsoft.Scripting.Hosting.LanguageSetup"/> �N���X�̐V�����C���X�^���X�����������܂��B
		/// </summary>
		/// <param name="typeName">����v���o�C�_��\���A�Z���u���C���^�����w�肵�܂��B</param>
		/// <param name="displayName">���̌���̕\�������w�肵�܂��B</param>
		public LanguageSetup(string typeName, string displayName) : this(typeName, displayName, ArrayUtils.EmptyStrings, ArrayUtils.EmptyStrings) { }

		/// <summary>
		/// ����v���o�C�_�̃A�Z���u���C���^���A����̕\�����A�啶������������������錾��̖��O�A�t�@�C���g���q���g�p���āA
		/// <see cref="Microsoft.Scripting.Hosting.LanguageSetup"/> �N���X�̐V�����C���X�^���X�����������܂��B
		/// </summary>
		/// <param name="typeName">����v���o�C�_��\���A�Z���u���C���^�����w�肵�܂��B</param>
		/// <param name="displayName">���̌���̕\�������w�肵�܂��B</param>
		/// <param name="names">���̌���̖��O�̃��X�g���w�肵�܂��B</param>
		/// <param name="fileExtensions">���̌���̃\�[�X�t�@�C���Ɏg�p����g���q�̃��X�g���w�肵�܂��B</param>
		public LanguageSetup(string typeName, string displayName, IEnumerable<string> names, IEnumerable<string> fileExtensions)
		{
			ContractUtils.RequiresNotEmpty(typeName, "typeName");
			ContractUtils.RequiresNotNull(displayName, "displayName");
			ContractUtils.RequiresNotNull(names, "names");
			ContractUtils.RequiresNotNull(fileExtensions, "fileExtensions");
			_typeName = typeName;
			_displayName = displayName;
			Names = new List<string>(names);
			FileExtensions = new List<string>(fileExtensions);
			Options = new Dictionary<string, object>();
		}

		/// <summary>�����Ɍ^�w�肳�ꂽ�l�Ƃ��ăI�v�V�������擾���܂��B</summary>
		/// <param name="name">�擾����I�v�V�����̖��O���w�肵�܂��B</param>
		/// <param name="defaultValue">�I�v�V���������݂��Ȃ��ꍇ�̊���l���w�肵�܂��B</param>
		public T GetOption<T>(string name, T defaultValue)
		{
			object value;
			if (Options != null && Options.TryGetValue(name, out value))
			{
				if (value is T)
					return (T)value;
				return (T)Convert.ChangeType(value, typeof(T), Thread.CurrentThread.CurrentCulture);
			}
			return defaultValue;
		}

		/// <summary>����v���o�C�_�̃A�Z���u���C���^�����擾�܂��͐ݒ肵�܂��B</summary>
		public string TypeName
		{
			get { return _typeName; }
			set
			{
				ContractUtils.RequiresNotEmpty(value, "value");
				CheckFrozen();
				_typeName = value;
			}
		}

		/// <summary>����̕\�������擾�܂��͐ݒ肵�܂��B��ł���ꍇ�A<see cref="Names"/> �̍ŏ��̗v�f���g�p����܂��B</summary>
		public string DisplayName
		{
			get { return _displayName; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				CheckFrozen();
				_displayName = value;
			}
		}

		/// <summary>�啶���Ə���������ʂ��Ȃ�����̖��O�̃��X�g���擾���܂��B</summary>
		public IList<string> Names { get; private set; }

		/// <summary>�啶���Ə���������ʂ��Ȃ��t�@�C���̊g���q�̃��X�g���擾���܂��B�h�b�g�Ŏn�߂邱�Ƃ��ł��܂��B</summary>
		public IList<string> FileExtensions { get; private set; }

		/// <summary>�I�v�V�����̃��X�g���擾���܂��B</summary>
		public IDictionary<string, object> Options { get; private set; }

		/// <summary>��O���ڍׂɐ������邩�ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool ExceptionDetail
		{
			get { return GetCachedOption("ExceptionDetail", ref _exceptionDetail); }
			set
			{
				CheckFrozen();
				Options["ExceptionDetail"] = value;
			}
		}

		bool GetCachedOption(string name, ref bool? storage)
		{
			if (storage.HasValue)
				return storage.Value;
			if (_frozen)
			{
				storage = GetOption<bool>(name, false);
				return storage.Value;
			}
			return GetOption<bool>(name, false);
		}

		/// <summary>���̃I�u�W�F�N�g��ύX�s�\�ɂ��܂��B</summary>
		internal void Freeze()
		{
			_frozen = true;
			Names = new ReadOnlyCollection<string>(Names.ToArray());
			FileExtensions = new ReadOnlyCollection<string>(FileExtensions.ToArray());
			Options = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(Options));
		}

		void CheckFrozen()
		{
			if (_frozen)
				throw new InvalidOperationException("ScriptRuntime �̍쐬�Ɏg�p���ꂽ��͂��̃I�u�W�F�N�g��ύX���邱�Ƃ͂ł��܂���B");
		}
	}
}
