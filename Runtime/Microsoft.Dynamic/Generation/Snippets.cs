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
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation
{
	// TODO: simplify initialization logic & state
	/// <summary>���I���ꃉ���^�C���S�̂Ŏg�p����^���`���A�A�Z���u�����쐬�ł���悤�ɂ��܂��B</summary>
	public static class Snippets
	{
		static AssemblyGen _assembly;
		static AssemblyGen _debugAssembly;

		/// <summary><see cref="SaveSnippets"/> ���ݒ肳�ꂽ����Ƃ��ɁA�A�Z���u�����ۑ������f�B���N�g�����擾���܂��B</summary>
		public static string SnippetsDirectory { get; private set; }

		/// <summary>�A�Z���u����ۑ����邩�ǂ����������l���擾���܂��B</summary>
		public static bool SaveSnippets { get; private set; }

		static AssemblyGen GetAssembly(bool emitSymbols)
		{
			var outDirectory = SaveSnippets ? SnippetsDirectory ?? Directory.GetCurrentDirectory() : null;
			if (emitSymbols)
			{
				if (_debugAssembly == null)
					Interlocked.CompareExchange(ref _debugAssembly, new AssemblyGen(new AssemblyName("Snippets.debug.scripting"), outDirectory, ".dll", true), null);
				return _debugAssembly;
			}
			else
			{
				if (_assembly == null)
					Interlocked.CompareExchange(ref _assembly, new AssemblyGen(new AssemblyName("Snippets.scripting"), outDirectory, ".dll", false), null);
				return _assembly;
			}
		}

		/// <summary>�A�Z���u���̕ۑ��Ɋւ������ݒ肵�܂��B</summary>
		/// <param name="enable">�A�Z���u���̕ۑ����\�ɂ��邩�ǂ����������l���w�肵�܂��B</param>
		/// <param name="directory">�A�Z���u�����ۑ������f�B���N�g�����w�肵�܂��B</param>
		public static void SetSaveAssemblies(bool enable, string directory)
		{
			// ���t���N�V������ʂ��� SetSaveAssemblies ���Ăяo�����Ƃɂ���ē��������O�ɑ΂��� SaveAssemblies ��ݒ肷��B
			var assemblyGen = typeof(Expression).Assembly.GetType(typeof(Expression).Namespace + ".Compiler.AssemblyGen");
			// �^�����݂��Ȃ���������Ȃ�
			if (assemblyGen != null)
			{
				var configSaveAssemblies = assemblyGen.GetMethod("SetSaveAssemblies", BindingFlags.NonPublic | BindingFlags.Static);
				// ���\�b�h�����݂��Ȃ���������Ȃ�
				if (configSaveAssemblies != null)
					configSaveAssemblies.Invoke(null, new object[] { enable, directory });
			}
			SaveSnippets = enable;
			SnippetsDirectory = directory;
		}

		/// <summary>�A�Z���u����ۑ����Č��؂��s���܂��B</summary>
		public static void SaveAndVerifyAssemblies()
		{
			if (!SaveSnippets)
				return;
			// ���؂����A�Z���u���̏ꏊ���擾���邽�߂ɁA���t���N�V�������g�p���ăR�A�� AssemblyGen.SaveAssembliesToDisk ���Ăяo��
			// �A�Z���u���� PEVerify.exe ���g�p���Č��؂���B
			// �O���̃����O�A�Z���u���̓R�A�̃����O�A�Z���u���Ɉˑ����Ă���̂ŁA�����͊O���̃����O�A�Z���u�������؂���O�ɍs���K�v������B
			// ���Ȃ킿���Ԃ͎��̂悤�ł���K�v������B
			// 1) ���������O�̃A�Z���u����ۑ�����B
			// 2) �O�������O�̃A�Z���u����ۑ�����B���������O�̃A�Z���u���͐�������� IL ��ʂ��ĊO�������O�̃A�Z���u���Ɉˑ����Ă���̂ŁA
			//    ����͓��������O�̃A�Z���u�������؂���O�Ɏ��s�����K�v������B
			// 3) ���������O�̃A�Z���u�������؂���B
			// 4) �O�������O�̃A�Z���u�������؂���B
			var assemblyGen = typeof(Expression).Assembly.GetType(typeof(Expression).Namespace + ".Compiler.AssemblyGen");
			// �^�͑��݂��Ȃ���������Ȃ�
			string[] coreAssemblyLocations = null;
			if (assemblyGen != null)
			{
				var saveAssemblies = assemblyGen.GetMethod("SaveAssembliesToDisk", BindingFlags.NonPublic | BindingFlags.Static);
				// ���\�b�h�͑��݂��Ȃ���������Ȃ�
				if (saveAssemblies != null)
					coreAssemblyLocations = (string[])saveAssemblies.Invoke(null, null);
			}
			var outerAssemblyLocations = SaveAssemblies();
			if (coreAssemblyLocations != null)
			{
				foreach (var file in coreAssemblyLocations)
					AssemblyGen.PeVerifyAssemblyFile(file);
			}
			// �O�������O�̃A�Z���u��������
			foreach (var file in outerAssemblyLocations)
				AssemblyGen.PeVerifyAssemblyFile(file);
		}

		/// <summary>�A�Z���u����ۑ����āA���؂����K�v������A�Z���u���̏ꏊ��Ԃ��܂��B</summary>
		/// <returns>���؂����K�v������A�Z���u���̏ꏊ�B</returns>
		static string[] SaveAssemblies()
		{
			if (!SaveSnippets)
				return ArrayUtils.EmptyStrings;
			List<string> assemlyLocations = new List<string>();
			// �ŏ��ɂ��ׂẴA�Z���u�����f�B�X�N�ɕۑ�����
			if (_assembly != null)
			{
				var assemblyLocation = _assembly.SaveAssembly();
				if (assemblyLocation != null)
					assemlyLocations.Add(assemblyLocation);
				_assembly = null;
			}
			if (_debugAssembly != null)
			{
				var debugAssemblyLocation = _debugAssembly.SaveAssembly();
				if (debugAssemblyLocation != null)
					assemlyLocations.Add(debugAssemblyLocation);
				_debugAssembly = null;
			}
			return assemlyLocations.ToArray();
		}

		/// <summary>�V�������I���\�b�h���쐬���āA���\�b�h�{�̂��\�z���� <see cref="DynamicILGen"/> ��Ԃ��܂��B</summary>
		/// <param name="methodName">���I���\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="returnType">���I���\�b�h�̖߂�l�̌^���w�肵�܂��B</param>
		/// <param name="parameterTypes">���I���\�b�h�̉������̌^���w�肵�܂��B</param>
		/// <param name="isDebuggable">���I���\�b�h���f�o�b�O�\���ǂ����������l���w�肵�܂��B</param>
		/// <returns>���I���\�b�h�̖{�̂��\�z���� <see cref="DynamicILGen"/>�B</returns>
		public static DynamicILGen CreateDynamicMethod(string methodName, Type returnType, Type[] parameterTypes, bool isDebuggable)
		{
			ContractUtils.RequiresNotEmpty(methodName, "methodName");
			ContractUtils.RequiresNotNull(returnType, "returnType");
			ContractUtils.RequiresNotNullItems(parameterTypes, "parameterTypes");
			if (SaveSnippets)
			{
				var tb = GetAssembly(isDebuggable).DefinePublicType(methodName, typeof(object), false);
				return new DynamicILGenType(tb, tb.DefineMethod(methodName, CompilerHelpers.PublicStatic, returnType, parameterTypes));
			}
			else
				return new DynamicILGenMethod(new DynamicMethod(methodName, returnType, parameterTypes, true));
		}

		/// <summary>�w�肳�ꂽ���O�����V�����p�u���b�N�^���`���āA�^���\�z���� <see cref="TypeBuilder"/> ��Ԃ��܂��B</summary>
		/// <param name="name">��`����^�̖��O���w�肵�܂��B</param>
		/// <param name="parent">��`����^���g������^���w�肵�܂��B</param>
		/// <returns>��`���ꂽ�^���\�z���� <see cref="TypeBuilder"/>�B</returns>
		public static TypeBuilder DefinePublicType(string name, Type parent) { return GetAssembly(false).DefinePublicType(name, parent, false); }

		/// <summary>�w�肳�ꂽ���O�����V�����p�u���b�N�^���`���āA�^���\�z���� <see cref="TypeBuilder"/> ��Ԃ��܂��B</summary>
		/// <param name="name">��`����^�̖��O���w�肵�܂��B</param>
		/// <param name="parent">��`����^���g������^���w�肵�܂��B</param>
		/// <param name="preserveName">�w�肳�ꂽ���O��ێ����邩�ǂ����������l���w�肵�܂��B</param>
		/// <param name="emitDebugSymbols">�f�o�b�O�V���{�����o�͂��邩�ǂ����������l���w�肵�܂��B</param>
		/// <returns>��`���ꂽ�^���\�z���� <see cref="TypeBuilder"/>�B</returns>
		public static TypeBuilder DefinePublicType(string name, Type parent, bool preserveName, bool emitDebugSymbols) { return GetAssembly(emitDebugSymbols).DefinePublicType(name, parent, preserveName); }

		/// <summary>�w�肳�ꂽ���O�A�p�����[�^�^�A�߂�l�̌^�����V�����f���Q�[�g�^���`���āA�^���\�z������ <see cref="TypeBuilder"/> ��Ԃ��܂��B</summary>
		/// <param name="name">�A�Z���u���ɒ�`����^�̖��O���w�肵�܂��B</param>
		/// <param name="parameters">�쐬����f���Q�[�g�^�̃p�����[�^�^���w�肵�܂��B</param>
		/// <param name="returnType">�쐬����f���Q�[�g�^�̖߂�l�̌^���w�肵�܂��B</param>
		/// <returns>��`���ꂽ�^���\�z���� <see cref="Type"/>�B</returns>
		public static TypeBuilder DefineDelegateType(string name, Type[] parameters, Type returnType) { return GetAssembly(false).DefineDelegateType(name, parameters, returnType); }

		/// <summary>�w�肳�ꂽ�A�Z���u�������̃X�j�y�b�g�ō\�z���Ă���A�Z���u�����ǂ����𔻒f���܂��B</summary>
		/// <param name="asm">���ׂ�A�Z���u�����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�A�Z���u�������̃X�j�y�b�g�ō\�z���Ă���A�Z���u���ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsSnippetsAssembly(Assembly asm) { return _assembly != null && asm == _assembly.AssemblyBuilder || _debugAssembly != null && asm == _debugAssembly.AssemblyBuilder; }
	}
}
