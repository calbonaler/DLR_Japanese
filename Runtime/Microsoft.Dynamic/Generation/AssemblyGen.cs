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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Security;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation
{
	/// <summary>�A�Z���u���̍\�z���x�����܂��B</summary>
	public sealed class AssemblyGen
	{
		readonly PortableExecutableKinds _peKind;
		readonly ImageFileMachine _machine;
		readonly bool _debuggable;
		readonly string _outFileName;       // can be null iff !SaveAndReloadAssemblies
		readonly string _outDir;            // null means the current directory
		const string peverify_exe = "peverify.exe";
		int _index;

		/// <summary>�쐬����A�Z���u�����f�o�b�O�\���ǂ����������l���擾���܂��B</summary>
		internal bool IsDebuggable
		{
			get
			{
				Debug.Assert(_debuggable == (ModuleBuilder.GetSymWriter() != null));
				return _debuggable;
			}
		}

		/// <summary>���O�A�o�̓f�B���N�g���A�t�@�C���g���q�A�f�o�b�O�\���ǂ����������l���g�p���āA<see cref="Microsoft.Scripting.Generation.AssemblyGen"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">�A�Z���u���̈�ӎ��ʎq��\�� <see cref="AssemblyName"/> �N���X�̃C���X�^���X���w�肵�܂��B</param>
		/// <param name="outDir">�쐬�����A�Z���u�����o�͂����f�B���N�g�����w�肵�܂��B<c>null</c> ���w�肷��ƃA�Z���u�����t�@�C���ɏo�͂��܂���B</param>
		/// <param name="outFileExtension">�o�͂����A�Z���u�� �t�@�C���̊g���q���w�肵�܂��B����̊g���q�� dll �ł��B</param>
		/// <param name="debuggable">�쐬�����A�Z���u�����f�o�b�O�\���ǂ����������l���w�肵�܂��B</param>
		public AssemblyGen(AssemblyName name, string outDir, string outFileExtension, bool debuggable) : this(name, outDir, outFileExtension, debuggable, PortableExecutableKinds.ILOnly, ImageFileMachine.I386) { }

		/// <summary>���O�A�o�̓f�B���N�g���A�t�@�C���g���q�A�f�o�b�O�\���ǂ����A�o�͂����R�[�h�̐�������ёΏۂ̃v���b�g�t�H�[�����g�p���āA<see cref="Microsoft.Scripting.Generation.AssemblyGen"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">�A�Z���u���̈�ӎ��ʎq��\�� <see cref="AssemblyName"/> �N���X�̃C���X�^���X���w�肵�܂��B</param>
		/// <param name="outDir">�쐬�����A�Z���u�����o�͂����f�B���N�g�����w�肵�܂��B<c>null</c> ���w�肷��ƃA�Z���u�����t�@�C���ɏo�͂��܂���B</param>
		/// <param name="outFileExtension">�o�͂����A�Z���u�� �t�@�C���̊g���q���w�肵�܂��B����̊g���q�� dll �ł��B</param>
		/// <param name="debuggable">�쐬�����A�Z���u�����f�o�b�O�\���ǂ����������l���w�肵�܂��B</param>
		/// <param name="peKind">�o�͂����A�Z���u�� �t�@�C���Ɋ܂܂��R�[�h�̐����������l���w�肵�܂��B</param>
		/// <param name="machine">�o�͂����A�Z���u�� �t�@�C�����ΏۂƂ���v���b�g�t�H�[�����w�肵�܂��B</param>
		internal AssemblyGen(AssemblyName name, string outDir, string outFileExtension, bool debuggable, PortableExecutableKinds peKind, ImageFileMachine machine)
		{
			ContractUtils.RequiresNotNull(name, "name");
			if (outFileExtension == null)
				outFileExtension = ".dll";
			if (outDir != null)
			{
				try { outDir = Path.GetFullPath(outDir); }
				catch (Exception) { throw Error.InvalidOutputDir(); }
				try { Path.Combine(outDir, name.Name + outFileExtension); }
				catch (ArgumentException) { throw Error.InvalidAsmNameOrExtension(); }
				_outFileName = name.Name + outFileExtension;
				_outDir = outDir;
			}
			// mark the assembly transparent so that it works in partial trust:
			var attributes = new[] { 
                new CustomAttributeBuilder(typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes), new object[0]),
                new CustomAttributeBuilder(typeof(SecurityRulesAttribute).GetConstructor(new[] { typeof(SecurityRuleSet) }), new object[] { SecurityRuleSet.Level1 }),
            };
			if (outDir != null)
			{
				AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndSave, outDir, false, attributes);
				ModuleBuilder = AssemblyBuilder.DefineDynamicModule(name.Name, _outFileName, debuggable);
			}
			else
			{
				AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run, attributes);
				ModuleBuilder = AssemblyBuilder.DefineDynamicModule(name.Name, debuggable);
			}
			AssemblyBuilder.DefineVersionInfoResource();
			_machine = machine;
			_peKind = peKind;
			_debuggable = debuggable;
			if (debuggable)
				SetDebuggableAttributes();
		}

		/// <summary>�쐬�����A�Z���u���� <see cref="DebuggableAttribute"/> ������t�^���܂��B</summary>
		internal void SetDebuggableAttributes()
		{
			var ctor = typeof(DebuggableAttribute).GetConstructor(new[] { typeof(DebuggableAttribute.DebuggingModes) });
			var argValues = new object[] { DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.DisableOptimizations };
			AssemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(ctor, argValues));
			ModuleBuilder.SetCustomAttribute(new CustomAttributeBuilder(ctor, argValues));
		}

		/// <summary>�쐬�����A�Z���u���Ƀ��\�[�X��ǉ����܂��B</summary>
		/// <param name="name">���\�[�X�̖��O���w�肵�܂��B</param>
		/// <param name="file">���\�[�X�Ƃ��Ēǉ�����t�@�C���̖��O���w�肵�܂��B</param>
		/// <param name="attribute">�ǉ����郊�\�[�X�̑������w�肵�܂��B</param>
		internal void AddResourceFile(string name, string file, ResourceAttributes attribute)
		{
			var rw = ModuleBuilder.DefineResource(Path.GetFileName(file), name, attribute);
			if (string.Equals(Path.GetExtension(file), ".resources", StringComparison.OrdinalIgnoreCase))
			{
				using (ResourceReader rr = new ResourceReader(file))
				{
					foreach (System.Collections.DictionaryEntry entry in rr)
						rw.AddResource(entry.Key as string, entry.Value);
				}
			}
			else
				rw.AddResource(name, File.ReadAllBytes(file));
		}

		#region Dump and Verify

		/// <summary>�A�Z���u�����t�@�C���ɏo�͂��܂��B</summary>
		/// <returns>�o�͂��ꂽ�A�Z���u���t�@�C���̃t���p�X�B</returns>
		public string SaveAssembly()
		{
			AssemblyBuilder.Save(_outFileName, _peKind, _machine);
			return Path.Combine(_outDir, _outFileName);
		}

		/// <summary>�A�Z���u���� PEVerify �c�[�����g�p���Č��؂��܂��B</summary>
		internal void Verify() { PeVerifyAssemblyFile(Path.Combine(_outDir, _outFileName)); }

		/// <summary>�w�肳�ꂽ�p�X�ɂ���A�Z���u���� PEVerify �c�[�����g�p���Č��؂��܂��B</summary>
		/// <param name="fileLocation"></param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static void PeVerifyAssemblyFile(string fileLocation)
		{
			Console.WriteLine("�������ꂽ IL �����؂��Ă��܂�: " + fileLocation);
			var outDir = Path.GetDirectoryName(fileLocation);
			var outFileName = Path.GetFileName(fileLocation);
			var peverifyPath = Environment.GetEnvironmentVariable("PATH").Split(';').Select(x => Path.Combine(x, peverify_exe)).FirstOrDefault(x => File.Exists(x));
			if (peverifyPath == null)
			{
				Console.WriteLine("PEVerify �͗��p�ł��܂���B");
				return;
			}
			var exitCode = 0;
			string strOut = null;
			string verifyFile = null;
			try
			{
				var assemblyFile = Path.Combine(outDir, outFileName).ToLower(CultureInfo.InvariantCulture);
				var assemblyName = Path.GetFileNameWithoutExtension(outFileName);
				var assemblyExtension = Path.GetExtension(outFileName);
				var rnd = new Random();
				for (int i = 0; ; i++)
				{
					var verifyName = Path.Combine(Path.GetTempPath(), string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}{3}", assemblyName, i, rnd.Next(1, 100), assemblyExtension));
					try
					{
						File.Copy(assemblyFile, verifyName);
						verifyFile = verifyName;
						break;
					}
					catch (IOException) { }
				}
				// copy any DLLs or EXEs created by the process during the run...
				CopyFilesCreatedSinceStart(Path.GetTempPath(), Environment.CurrentDirectory, outFileName);
				CopyDirectory(Path.GetTempPath(), new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName);
				if (Snippets.SnippetsDirectory != null && Snippets.SnippetsDirectory != Path.GetTempPath())
					CopyFilesCreatedSinceStart(Path.GetTempPath(), Snippets.SnippetsDirectory, outFileName);
				// /IGNORE=80070002 ignores errors related to files we can't find, this happens when we generate assemblies
				// and then peverify the result.  Note if we can't resolve a token thats in an external file we still
				// generate an error.
				var proc = Process.Start(new ProcessStartInfo(peverifyPath, "/IGNORE=80070002 \"" + verifyFile + "\"") { UseShellExecute = false, RedirectStandardOutput = true });
				var thread = new Thread(() =>
				{
					using (StreamReader sr = proc.StandardOutput)
						strOut = sr.ReadToEnd();
				});
				thread.Start();
				proc.WaitForExit();
				thread.Join();
				exitCode = proc.ExitCode;
				proc.Close();
			}
			catch (Exception ex)
			{
				strOut = "�\�����Ȃ���O: " + ex.ToString();
				exitCode = 1;
			}
			if (exitCode != 0)
			{
				Console.WriteLine("���؂͏I���R�[�h {0} �Ŏ��s���܂���: {1}", exitCode, strOut);
				throw Error.VerificationException(outFileName, verifyFile, strOut ?? "");
			}
			if (verifyFile != null)
				File.Delete(verifyFile);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		static void CopyFilesCreatedSinceStart(string pythonPath, string dir, string outFileName)
		{
			var start = Process.GetCurrentProcess().StartTime;
			foreach (var filename in Directory.GetFiles(dir))
			{
				var fi = new FileInfo(filename);
				if (fi.Name != outFileName && fi.LastWriteTime - start >= TimeSpan.Zero)
				{
					try { File.Copy(filename, Path.Combine(pythonPath, fi.Name), true); }
					catch (Exception ex) { Console.WriteLine("{0} �̃R�s�[���ɃG���[���������܂���: {1}", filename, ex.Message); }
				}
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		static void CopyDirectory(string to, string from)
		{
			foreach (string filename in Directory.GetFiles(from))
			{
				FileInfo fi = new FileInfo(filename);
				FileInfo toInfo = new FileInfo(Path.Combine(to, fi.Name));
				if ((fi.Extension.ToLowerInvariant() == ".dll" || fi.Extension.ToLowerInvariant() == ".exe") && (!toInfo.Exists || toInfo.CreationTime != fi.CreationTime))
				{
					try { fi.CopyTo(toInfo.FullName, true); }
					catch (Exception e) { Console.WriteLine("{0} �̃R�s�[���ɃG���[���������܂���: {1}", filename, e.Message); }
				}
			}
		}

		#endregion

		/// <summary>�A�Z���u���Ɏw�肳�ꂽ���O�����V�����p�u���b�N�^���`���āA�^���\�z���� <see cref="TypeBuilder"/> ��Ԃ��܂��B</summary>
		/// <param name="name">�A�Z���u���ɒ�`����^�̖��O���w�肵�܂��B</param>
		/// <param name="parent">���ꂩ���`�����^�ɂ���Ċg�������^���w�肵�܂��B</param>
		/// <param name="preserveName">�w�肳�ꂽ���O��ێ����邩�ǂ����������l���w�肵�܂��B</param>
		/// <returns>��`���ꂽ�^���\�z���� <see cref="TypeBuilder"/>�B</returns>
		public TypeBuilder DefinePublicType(string name, Type parent, bool preserveName) { return DefineType(name, parent, TypeAttributes.Public, preserveName); }

		/// <summary>�A�Z���u���Ɏw�肳�ꂽ���O�����V�����^���`���āA�^���\�z���� <see cref="TypeBuilder"/> ��Ԃ��܂��B</summary>
		/// <param name="name">�A�Z���u���ɒ�`����^�̖��O���w�肵�܂��B</param>
		/// <param name="parent">���ꂩ���`�����^�ɂ���Ċg�������^���w�肵�܂��B</param>
		/// <param name="attr">��`�����^�̑������w�肵�܂��B</param>
		/// <param name="preserveName">�w�肳�ꂽ���O��ێ����邩�ǂ����������l���w�肵�܂��B</param>
		/// <returns>��`���ꂽ�^���\�z���� <see cref="TypeBuilder"/>�B</returns>
		internal TypeBuilder DefineType(string name, Type parent, TypeAttributes attr, bool preserveName)
		{
			ContractUtils.RequiresNotNull(name, "name");
			ContractUtils.RequiresNotNull(parent, "parent");
			if (!preserveName)
				name += "$" + Interlocked.Increment(ref _index);
			// There is a bug in Reflection.Emit that leads to 
			// Unhandled Exception: System.Runtime.InteropServices.COMException (0x80131130): Record not found on lookup.
			// if there is any of the characters []*&+,\ in the type name and a method defined on the type is called.
			return ModuleBuilder.DefineType(string.Concat(name.Select(x => @"+[]*&,\".IndexOf(x) >= 0 ? '_' : x)), attr, parent);
		}

		/// <summary>�A�Z���u���̃G���g���|�C���g��ݒ肵�A�\�z����|�[�^�u�����s�\ (PE) �t�@�C���̌^���`���܂��B</summary>
		/// <param name="mi">�A�Z���u���̃G���g���|�C���g�ƂȂ郁�\�b�h���w�肵�܂��B</param>
		/// <param name="kind">�\�z����A�Z���u�����s�t�@�C���̌^���w�肵�܂��B</param>
		internal void SetEntryPoint(MethodInfo mi, PEFileKinds kind) { AssemblyBuilder.SetEntryPoint(mi, kind); }

		/// <summary>�A�Z���u�����ڍׂɒ�`�ł��� <see cref="AssemblyBuilder"/> ���擾���܂��B</summary>
		public AssemblyBuilder AssemblyBuilder { get; private set; }

		/// <summary>���I�A�Z���u�����̃��W���[�����`���� <see cref="ModuleBuilder"/> ���擾���܂��B</summary>
		public ModuleBuilder ModuleBuilder { get; private set; }

		const MethodImplAttributes ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;

		/// <summary>�w�肳�ꂽ���O�A�p�����[�^�^�A�߂�l�̌^���g�p���āA�A�Z���u���Ƀf���Q�[�g���쐬���܂��B</summary>
		/// <param name="name">�쐬����f���Q�[�g�̖��O���w�肵�܂��B</param>
		/// <param name="parameters">�쐬����f���Q�[�g�̃p�����[�^�^���w�肵�܂��B</param>
		/// <param name="returnType">�쐬����f���Q�[�g�̖߂�l�̌^���w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ�f���Q�[�g��\�� <see cref="Type"/>�B</returns>
		public Type MakeDelegateType(string name, Type[] parameters, Type returnType)
		{
			var builder = DefineType(name, typeof(MulticastDelegate), TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass, false);
			builder.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(object), typeof(IntPtr) }).SetImplementationFlags(ImplAttributes);
			builder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, returnType, parameters).SetImplementationFlags(ImplAttributes);
			return builder.CreateType();
		}
	}

	static class SymbolGuids
	{
		internal static readonly Guid LanguageType_ILAssembly = new Guid(-1358664493, -12063, 0x11d2, 0x97, 0x7c, 0, 160, 0xc9, 180, 0xd5, 12);

		internal static readonly Guid DocumentType_Text = new Guid(0x5a869d0b, 0x6611, 0x11d3, 0xbd, 0x2a, 0, 0, 0xf8, 8, 0x49, 0xbd);

		internal static readonly Guid LanguageVendor_Microsoft = new Guid(-1723120188, -6423, 0x11d2, 0x90, 0x3f, 0, 0xc0, 0x4f, 0xa3, 2, 0xa1);
	}
}

