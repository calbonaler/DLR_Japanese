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
using System.Configuration;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Hosting.Configuration
{
	/// <summary>�\���t�@�C�����̌���I�v�V�����Ɋւ���\���v�f��\���܂��B</summary>
	public class OptionElement : ConfigurationElement
	{
		const string _Option = "option";
		const string _Value = "value";
		const string _Language = "language";

		static ConfigurationPropertyCollection _Properties = new ConfigurationPropertyCollection() {
            new ConfigurationProperty(_Option, typeof(string), string.Empty, ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey),
            new ConfigurationProperty(_Value, typeof(string), string.Empty, ConfigurationPropertyOptions.IsRequired),
            new ConfigurationProperty(_Language, typeof(string), string.Empty, ConfigurationPropertyOptions.IsKey),
        };

		/// <summary>�v���p�e�B�̃R���N�V�������擾���܂��B</summary>
		protected override ConfigurationPropertyCollection Properties { get { return _Properties; } }

		/// <summary>���̌���I�v�V�����̖��O���擾�܂��͐ݒ肵�܂��B</summary>
		public string Name
		{
			get { return (string)this[_Option]; }
			set { this[_Option] = value; }
		}

		/// <summary>���̌���I�v�V�����̒l���擾�܂��͐ݒ肵�܂��B</summary>
		public string Value
		{
			get { return (string)this[_Value]; }
			set { this[_Value] = value; }
		}

		/// <summary>���̌���I�v�V�������ΏۂƂ��錾��̖��O���擾�܂��͐ݒ肵�܂��B</summary>
		public string Language
		{
			get { return (string)this[_Language]; }
			set { this[_Language] = value; }
		}

		/// <summary>���̗v�f�ɑ΂���L�[��\���I�u�W�F�N�g���擾���܂��B</summary>
		/// <returns>���̗v�f�̃L�[�ƂȂ�I�u�W�F�N�g</returns>
		internal object GetKey() { return new Key(Name, Language); }

		sealed class Key : IEquatable<Key>
		{
			public string Option { get; private set; }
			public string Language { get; private set; }

			public Key(string option, string language)
			{
				Option = option;
				Language = language;
			}

			public override bool Equals(object obj) { return Equals(obj as Key); }

			public bool Equals(Key other) { return other != null && DlrConfiguration.OptionNameComparer.Equals(Option, other.Option) && DlrConfiguration.LanguageNameComparer.Equals(Language, other.Language); }

			public override int GetHashCode() { return Option.GetHashCode() ^ (Language ?? string.Empty).GetHashCode(); }

			public override string ToString() { return (string.IsNullOrEmpty(Language) ? string.Empty : Language + ":") + Option; }
		}
	}
}
