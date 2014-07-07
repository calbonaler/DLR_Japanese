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
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>����Ɋւ���I�v�V�������i�[���܂��B</summary>
	[Serializable]
	public class LanguageOptions
	{
		/// <summary>�����^�C�������Ɋ�Â����œK�����s��Ȃ����ǂ����������l���擾���܂��B</summary>
		public bool NoAdaptiveCompilation { get; private set; }

		/// <summary>�C���^�v���^���R���p�C�����n�߂�O�̔����񐔂��擾���܂��B</summary>
		public int CompilationThreshold { get; private set; }

		/// <summary>��O���⑫���ꂽ�ۂɗ�O�̏ڍ� (�R�[���X�^�b�N) ��\�����邩�ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool ExceptionDetail { get; set; }

		/// <summary>CLR �̗�O��\�����邩�ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool ShowClrExceptions { get; set; }

		/// <summary>�p�t�H�[�}���X���v�������W���邩�ǂ����������l���擾���܂��B</summary>
		public bool PerfStats { get; private set; }

		/// <summary>�z�X�g�ɂ���Ē񋟂��ꂽ�����̃t�@�C�������p�X���擾���܂��B</summary>
		public ReadOnlyCollection<string> SearchPaths { get; private set; }

		/// <summary><see cref="Microsoft.Scripting.LanguageOptions"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public LanguageOptions() : this(null) { }

		/// <summary>�I�v�V�������i�[����f�B�N�V���i�����g�p���āA<see cref="Microsoft.Scripting.LanguageOptions"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="options">���̃I�u�W�F�N�g�ɃI�v�V������ݒ肷�邽�߂Ɏg�p�����f�B�N�V���i�����w�肵�܂��B</param>
		public LanguageOptions(IDictionary<string, object> options)
		{
			ExceptionDetail = GetOption(options, "ExceptionDetail", false);
			ShowClrExceptions = GetOption(options, "ShowClrExceptions", false);
			PerfStats = GetOption(options, "PerfStats", false);
			NoAdaptiveCompilation = GetOption(options, "NoAdaptiveCompilation", false);
			CompilationThreshold = GetOption(options, "CompilationThreshold", -1);
			SearchPaths = GetSearchPathsOption(options) ?? EmptyStringCollection;
		}

		/// <summary>�I�v�V�������i�[����f�B�N�V���i������w�肵�����O�̃I�v�V�����ɑ΂���l���擾���܂��B</summary>
		/// <param name="options">�I�v�V�������i�[����f�B�N�V���i�����w�肵�܂��B</param>
		/// <param name="name">�擾����I�v�V�����̖��O���w�肵�܂��B</param>
		/// <param name="defaultValue">�擾����I�v�V�����̒l�̊���l���w�肵�܂��B</param>
		/// <returns>�擾���ꂽ�I�v�V�����̒l�B�I�v�V���������݂��Ȃ��ꍇ�͊���l�B</returns>
		public static T GetOption<T>(IDictionary<string, object> options, string name, T defaultValue)
		{
			object value;
			if (options != null && options.TryGetValue(name, out value))
			{
				if (value is T)
					return (T)value;
				return (T)Convert.ChangeType(value, typeof(T), Thread.CurrentThread.CurrentCulture);
			}
			return defaultValue;
		}

		/// <summary>�l�� <c>null</c> �łȂ�������̃R���N�V�����ł���Ɨ\�������I�v�V�������擾���܂��B�I�v�V�����̃R�s�[�̓ǂݎ���p�̒l���擾���܂��B</summary>
		/// <param name="options">�I�v�V�������i�[����f�B�N�V���i�����w�肵�܂��B</param>
		/// <param name="name">�擾����I�v�V�����̖��O���w�肵�܂��B</param>
		/// <param name="separators">�擾���ꂽ������𕪊����� Unicode �����̔z����w�肵�܂��B</param>
		/// <returns>�擾���ꂽ�I�v�V�����̒l�B</returns>
		public static ReadOnlyCollection<string> GetStringCollectionOption(IDictionary<string, object> options, string name, params char[] separators)
		{
			object value;
			if (options == null || !options.TryGetValue(name, out value))
				return null;
			// a collection:
			var collection = value as ICollection<string>;
			if (collection != null)
			{
				if (collection.Any(x => x == null))
					throw new ArgumentException(string.Format("Invalid value for option {0}: collection shouldn't containt null items", name));
				return new ReadOnlyCollection<string>(collection.ToArray());
			}
			// a string:
			var strValue = value as string;
			if (strValue != null && separators != null && separators.Length > 0)
				return new ReadOnlyCollection<string>(strValue.Split(separators, int.MaxValue, StringSplitOptions.RemoveEmptyEntries));
			throw new ArgumentException(string.Format("Invalid value for option {0}", name));
		}

		/// <summary>�I�v�V�������i�[����f�B�N�V���i�����猟���p�X���擾���܂��B</summary>
		/// <param name="options">�I�v�V�������i�[����f�B�N�V���i�����w�肵�܂��B</param>
		/// <returns>�擾���ꂽ�I�v�V�����̒l�B</returns>
		public static ReadOnlyCollection<string> GetSearchPathsOption(IDictionary<string, object> options) { return GetStringCollectionOption(options, "SearchPaths", Path.PathSeparator); }

		/// <summary>�ǂݎ���p�̕�����̋�̃R���N�V�������擾���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		protected static readonly ReadOnlyCollection<string> EmptyStringCollection = new ReadOnlyCollection<string>(ArrayUtils.EmptyStrings);
	}

}