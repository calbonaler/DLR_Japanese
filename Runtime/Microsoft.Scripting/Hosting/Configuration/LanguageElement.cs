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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Configuration
{
	// <language names="IronPython;Python;py" extensions=".py" type="AQTN" displayName="IronPython v2">
	//    <option name="foo" value="bar" />
	// </language>
	/// <summary>�\���t�@�C�����̌���Ɋւ���\���v�f��\���܂��B</summary>
	public class LanguageElement : ConfigurationElement
	{
		const string _Names = "names";
		const string _Extensions = "extensions";
		const string _Type = "type";
		const string _DisplayName = "displayName";

		static ConfigurationPropertyCollection _Properties = new ConfigurationPropertyCollection() {
            new ConfigurationProperty(_Names, typeof(string), null),
            new ConfigurationProperty(_Extensions, typeof(string), null),
            new ConfigurationProperty(_Type, typeof(string), null, ConfigurationPropertyOptions.IsRequired),
            new ConfigurationProperty(_DisplayName, typeof(string), null)
        };

		/// <summary>�v���p�e�B�̃R���N�V�������擾���܂��B</summary>
		protected override ConfigurationPropertyCollection Properties { get { return _Properties; } }

		/// <summary>���̌���̖��O���擾�܂��͐ݒ肵�܂��B</summary>
		public string Names
		{
			get { return (string)this[_Names]; }
			set { this[_Names] = value; }
		}

		/// <summary>���̌���̃\�[�X�t�@�C���̊g���q���擾�܂��͐ݒ肵�܂��B</summary>
		public string Extensions
		{
			get { return (string)this[_Extensions]; }
			set { this[_Extensions] = value; }
		}

		/// <summary>���̌���̌���v���o�C�_�̌^���擾�܂��͐ݒ肵�܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
		public string Type
		{
			get { return (string)this[_Type]; }
			set { this[_Type] = value; }
		}

		/// <summary>���̌���̕\�������擾�܂��͐ݒ肵�܂��B</summary>
		public string DisplayName
		{
			get { return (string)this[_DisplayName]; }
			set { this[_DisplayName] = value; }
		}

		/// <summary>���̌���̖��O��z��Ƃ��Ď擾���܂��B</summary>
		public string[] GetNamesArray() { return Split(Names); }

		/// <summary>���̌���̃\�[�X�t�@�C���̊g���q��z��Ƃ��Ď擾���܂��B</summary>
		public string[] GetExtensionsArray() { return Split(Extensions); }

		static string[] Split(string str) { return str != null ? str.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries) : ArrayUtils.EmptyStrings; }
	}
}