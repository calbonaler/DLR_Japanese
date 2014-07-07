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
using System.Reflection;

namespace Microsoft.Scripting.Utils
{
	/// <summary>�A�Z���u�����ɂ���ďC�����ꂽ�^����\���܂��B</summary>
	[Serializable]
	struct AssemblyQualifiedTypeName : IEquatable<AssemblyQualifiedTypeName>
	{
		/// <summary>�^�����擾���܂��B</summary>
		public readonly string TypeName;
		/// <summary>�A�Z���u������\�� <see cref="System.Reflection.AssemblyName"/> ���擾���܂��B</summary>
		public readonly AssemblyName AssemblyName;

		/// <summary>�^���Ƃ�����C������A�Z���u�������g�p���āA<see cref="Microsoft.Scripting.Utils.AssemblyQualifiedTypeName"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="typeName">�^�����w�肵�܂��B</param>
		/// <param name="assemblyName">�A�Z���u������\�� <see cref="System.Reflection.AssemblyName"/> ���w�肵�܂��B</param>
		public AssemblyQualifiedTypeName(string typeName, AssemblyName assemblyName)
		{
			ContractUtils.RequiresNotNull(typeName, "typeName");
			ContractUtils.RequiresNotNull(assemblyName, "assemblyName");
			TypeName = typeName;
			AssemblyName = assemblyName;
		}

		/// <summary>�^���g�p���āA<see cref="Microsoft.Scripting.Utils.AssemblyQualifiedTypeName"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="type">�^���w�肵�܂��B</param>
		public AssemblyQualifiedTypeName(Type type)
		{
			TypeName = type.FullName;
			AssemblyName = type.Assembly.GetName();
		}

		/// <summary>������ŕ\���ꂽ�A�Z���u���C���^������ <see cref="Microsoft.Scripting.Utils.AssemblyQualifiedTypeName"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="assemblyQualifiedTypeName">�A�Z���u���C���^�����w�肵�܂��B</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public AssemblyQualifiedTypeName(string assemblyQualifiedTypeName)
		{
			ContractUtils.RequiresNotNull(assemblyQualifiedTypeName, "assemblyQualifiedTypeName");
			int firstColon = assemblyQualifiedTypeName.IndexOf(",");
			if (firstColon != -1)
			{
				TypeName = assemblyQualifiedTypeName.Substring(0, firstColon).Trim();
				var assemblyNameStr = assemblyQualifiedTypeName.Substring(firstColon + 1).Trim();
				if (TypeName.Length > 0 && assemblyNameStr.Length > 0)
				{
					try
					{
						AssemblyName = new AssemblyName(assemblyNameStr);
						return;
					}
					catch (Exception e) { throw new ArgumentException(string.Format("Invalid assembly qualified name '{0}': {1}", assemblyQualifiedTypeName, e.Message), e); }
				}
			}
			throw new ArgumentException(string.Format("Invalid assembly qualified name '{0}'", assemblyQualifiedTypeName));
		}

		/// <summary>�w�肳�ꂽ��������͂��āA<see cref="Microsoft.Scripting.Utils.AssemblyQualifiedTypeName"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="str">�����Ƃ��ēn���ꂽ��������w�肵�܂��B</param>
		/// <param name="argumentName">�����̖��O���w�肵�܂��B</param>
		internal static AssemblyQualifiedTypeName ParseArgument(string str, string argumentName)
		{
			Assert.NotEmpty(argumentName);
			try { return new AssemblyQualifiedTypeName(str); }
			catch (ArgumentException e) { throw new ArgumentException(e.Message, argumentName, e.InnerException); }
		}

		/// <summary>�w�肳�ꂽ�A�Z���u���C���^�������̃A�Z���u���C���^���Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="other">������r������A�Z���u���C���^�����w�肵�܂��B</param>
		/// <returns>�������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool Equals(AssemblyQualifiedTypeName other) { return TypeName == other.TypeName && AssemblyName.FullName == other.AssemblyName.FullName; }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�����̃I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">������r������I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public override bool Equals(object obj) { return obj is AssemblyQualifiedTypeName && Equals((AssemblyQualifiedTypeName)obj); }

		/// <summary>���̃I�u�W�F�N�g�ɑ΂���n�b�V���l��Ԃ��܂��B</summary>
		/// <returns>�n�b�V���l�B</returns>
		public override int GetHashCode() { return TypeName.GetHashCode() ^ AssemblyName.FullName.GetHashCode(); }

		/// <summary>���̃I�u�W�F�N�g�̕�����\����Ԃ��܂��B</summary>
		/// <returns>������\���B</returns>
		public override string ToString() { return TypeName + ", " + AssemblyName.FullName; }

		/// <summary>�w�肳�ꂽ 2 �̃A�Z���u���C���^�������������ǂ����𔻒f���܂��B</summary>
		/// <param name="name">1 �ڂ̃A�Z���u���C���^�����w�肵�܂��B</param>
		/// <param name="other">2 �ڂ̃A�Z���u���C���^�����w�肵�܂��B</param>
		/// <returns>�������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator ==(AssemblyQualifiedTypeName name, AssemblyQualifiedTypeName other) { return name.Equals(other); }

		/// <summary>�w�肳�ꂽ 2 �̃A�Z���u���C���^�����������Ȃ����ǂ����𔻒f���܂��B</summary>
		/// <param name="name">1 �ڂ̃A�Z���u���C���^�����w�肵�܂��B</param>
		/// <param name="other">2 �ڂ̃A�Z���u���C���^�����w�肵�܂��B</param>
		/// <returns>�������Ȃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator !=(AssemblyQualifiedTypeName name, AssemblyQualifiedTypeName other) { return !name.Equals(other); }
	}
}
