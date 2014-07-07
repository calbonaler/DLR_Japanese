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

using System.Configuration;

namespace Microsoft.Scripting.Hosting.Configuration
{
	/// <summary>����I�v�V�����Ɋւ���\���v�f�̃R���N�V�������i�[����v�f��\���܂��B</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
	public class OptionElementCollection : ConfigurationElementCollection
	{
		/// <summary><see cref="Microsoft.Scripting.Hosting.Configuration.OptionElementCollection"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public OptionElementCollection() { AddElementName = "set"; }

		/// <summary><see cref="System.Configuration.ConfigurationElementCollection"/> �̌^���擾���܂��B</summary>
		public override ConfigurationElementCollectionType CollectionType { get { return ConfigurationElementCollectionType.AddRemoveClearMap; } }

		/// <summary>�V���� <see cref="System.Configuration.ConfigurationElement"/> ���쐬���܂��B</summary>
		/// <returns>�V�����쐬���� <see cref="System.Configuration.ConfigurationElement"/>�B</returns>
		protected override ConfigurationElement CreateNewElement() { return new OptionElement(); }

		/// <summary>
		/// �d������ <see cref="System.Configuration.ConfigurationElement"/> �� <see cref="System.Configuration.ConfigurationElementCollection"/>
		/// �ɒǉ����悤�Ƃ����Ƃ��ɁA��O���X���[���邩�ǂ����������l���擾���܂��B
		/// </summary>
		protected override bool ThrowOnDuplicate { get { return false; } }

		/// <summary>�w�肵���\���v�f�̗v�f�L�[���擾���܂��B</summary>
		/// <param name="element">�L�[��Ԃ� <see cref="System.Configuration.ConfigurationElement"/>�B</param>
		/// <returns>�w�肵�� <see cref="System.Configuration.ConfigurationElement"/> �̃L�[�Ƃ��ċ@�\���� <see cref="System.Object"/>�B</returns>
		protected override object GetElementKey(ConfigurationElement element) { return ((OptionElement)element).GetKey(); }
	}
}