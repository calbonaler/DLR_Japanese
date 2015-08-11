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
	/// <summary>アセンブリの構築を支援します。</summary>
	public sealed class AssemblyGen
	{
		readonly PortableExecutableKinds _peKind;
		readonly ImageFileMachine _machine;
		readonly bool _debuggable;
		readonly string _outFileName;       // can be null iff !SaveAndReloadAssemblies
		readonly string _outDir;            // null means the current directory
		const string peverify_exe = "peverify.exe";
		int _index;

		/// <summary>作成するアセンブリがデバッグ可能かどうかを示す値を取得します。</summary>
		internal bool IsDebuggable
		{
			get
			{
				Debug.Assert(_debuggable == (ModuleBuilder.GetSymWriter() != null));
				return _debuggable;
			}
		}

		/// <summary>名前、出力ディレクトリ、ファイル拡張子、デバッグ可能かどうかを示す値を使用して、<see cref="Microsoft.Scripting.Generation.AssemblyGen"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">アセンブリの一意識別子を表す <see cref="AssemblyName"/> クラスのインスタンスを指定します。</param>
		/// <param name="outDir">作成されるアセンブリが出力されるディレクトリを指定します。<c>null</c> を指定するとアセンブリをファイルに出力しません。</param>
		/// <param name="outFileExtension">出力されるアセンブリ ファイルの拡張子を指定します。既定の拡張子は dll です。</param>
		/// <param name="debuggable">作成されるアセンブリがデバッグ可能かどうかを示す値を指定します。</param>
		public AssemblyGen(AssemblyName name, string outDir, string outFileExtension, bool debuggable) : this(name, outDir, outFileExtension, debuggable, PortableExecutableKinds.ILOnly, ImageFileMachine.I386) { }

		/// <summary>名前、出力ディレクトリ、ファイル拡張子、デバッグ可能かどうか、出力されるコードの性質および対象のプラットフォームを使用して、<see cref="Microsoft.Scripting.Generation.AssemblyGen"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">アセンブリの一意識別子を表す <see cref="AssemblyName"/> クラスのインスタンスを指定します。</param>
		/// <param name="outDir">作成されるアセンブリが出力されるディレクトリを指定します。<c>null</c> を指定するとアセンブリをファイルに出力しません。</param>
		/// <param name="outFileExtension">出力されるアセンブリ ファイルの拡張子を指定します。既定の拡張子は dll です。</param>
		/// <param name="debuggable">作成されるアセンブリがデバッグ可能かどうかを示す値を指定します。</param>
		/// <param name="peKind">出力されるアセンブリ ファイルに含まれるコードの性質を示す値を指定します。</param>
		/// <param name="machine">出力されるアセンブリ ファイルが対象とするプラットフォームを指定します。</param>
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

		/// <summary>作成されるアセンブリに <see cref="DebuggableAttribute"/> 属性を付与します。</summary>
		internal void SetDebuggableAttributes()
		{
			var ctor = typeof(DebuggableAttribute).GetConstructor(new[] { typeof(DebuggableAttribute.DebuggingModes) });
			var argValues = new object[] { DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.DisableOptimizations };
			AssemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(ctor, argValues));
			ModuleBuilder.SetCustomAttribute(new CustomAttributeBuilder(ctor, argValues));
		}

		/// <summary>作成されるアセンブリにリソースを追加します。</summary>
		/// <param name="name">リソースの名前を指定します。</param>
		/// <param name="file">リソースとして追加するファイルの名前を指定します。</param>
		/// <param name="attribute">追加するリソースの属性を指定します。</param>
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

		/// <summary>アセンブリをファイルに出力します。</summary>
		/// <returns>出力されたアセンブリファイルのフルパス。</returns>
		public string SaveAssembly()
		{
			AssemblyBuilder.Save(_outFileName, _peKind, _machine);
			return Path.Combine(_outDir, _outFileName);
		}

		/// <summary>アセンブリを PEVerify ツールを使用して検証します。</summary>
		internal void Verify() { PeVerifyAssemblyFile(Path.Combine(_outDir, _outFileName)); }

		/// <summary>指定されたパスにあるアセンブリを PEVerify ツールを使用して検証します。</summary>
		/// <param name="fileLocation"></param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static void PeVerifyAssemblyFile(string fileLocation)
		{
			Console.WriteLine("生成された IL を検証しています: " + fileLocation);
			var outDir = Path.GetDirectoryName(fileLocation);
			var outFileName = Path.GetFileName(fileLocation);
			var peverifyPath = Environment.GetEnvironmentVariable("PATH").Split(';').Select(x => Path.Combine(x, peverify_exe)).FirstOrDefault(x => File.Exists(x));
			if (peverifyPath == null)
			{
				Console.WriteLine("PEVerify は利用できません。");
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
				strOut = "予期しない例外: " + ex.ToString();
				exitCode = 1;
			}
			if (exitCode != 0)
			{
				Console.WriteLine("検証は終了コード {0} で失敗しました: {1}", exitCode, strOut);
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
					catch (Exception ex) { Console.WriteLine("{0} のコピー中にエラーが発生しました: {1}", filename, ex.Message); }
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
					catch (Exception e) { Console.WriteLine("{0} のコピー中にエラーが発生しました: {1}", filename, e.Message); }
				}
			}
		}

		#endregion

		/// <summary>アセンブリに指定された名前をもつ新しいパブリック型を定義して、型を構築する <see cref="TypeBuilder"/> を返します。</summary>
		/// <param name="name">アセンブリに定義する型の名前を指定します。</param>
		/// <param name="parent">これから定義される型によって拡張される型を指定します。</param>
		/// <param name="preserveName">指定された名前を保持するかどうかを示す値を指定します。</param>
		/// <returns>定義された型を構築する <see cref="TypeBuilder"/>。</returns>
		public TypeBuilder DefinePublicType(string name, Type parent, bool preserveName) { return DefineType(name, parent, TypeAttributes.Public, preserveName); }

		/// <summary>アセンブリに指定された名前をもつ新しい型を定義して、型を構築する <see cref="TypeBuilder"/> を返します。</summary>
		/// <param name="name">アセンブリに定義する型の名前を指定します。</param>
		/// <param name="parent">これから定義される型によって拡張される型を指定します。</param>
		/// <param name="attr">定義される型の属性を指定します。</param>
		/// <param name="preserveName">指定された名前を保持するかどうかを示す値を指定します。</param>
		/// <returns>定義された型を構築する <see cref="TypeBuilder"/>。</returns>
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

		/// <summary>アセンブリのエントリポイントを設定し、構築するポータブル実行可能 (PE) ファイルの型を定義します。</summary>
		/// <param name="mi">アセンブリのエントリポイントとなるメソッドを指定します。</param>
		/// <param name="kind">構築するアセンブリ実行ファイルの型を指定します。</param>
		internal void SetEntryPoint(MethodInfo mi, PEFileKinds kind) { AssemblyBuilder.SetEntryPoint(mi, kind); }

		/// <summary>アセンブリを詳細に定義できる <see cref="AssemblyBuilder"/> を取得します。</summary>
		public AssemblyBuilder AssemblyBuilder { get; private set; }

		/// <summary>動的アセンブリ内のモジュールを定義する <see cref="ModuleBuilder"/> を取得します。</summary>
		public ModuleBuilder ModuleBuilder { get; private set; }

		const MethodImplAttributes ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;

		/// <summary>指定された名前、パラメータ型、戻り値の型をもつ新しいデリゲート型を定義して、型を構築をする <see cref="TypeBuilder"/> を返します。</summary>
		/// <param name="name">アセンブリに定義する型の名前を指定します。</param>
		/// <param name="parameters">作成するデリゲート型のパラメータ型を指定します。</param>
		/// <param name="returnType">作成するデリゲート型の戻り値の型を指定します。</param>
		/// <returns>定義された型を構築する <see cref="Type"/>。</returns>
		public TypeBuilder DefineDelegateType(string name, Type[] parameters, Type returnType)
		{
			var builder = DefineType(name, typeof(MulticastDelegate), TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass, false);
			builder.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(object), typeof(IntPtr) }).SetImplementationFlags(ImplAttributes);
			builder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, returnType, parameters).SetImplementationFlags(ImplAttributes);
			return builder;
		}
	}

	static class SymbolGuids
	{
		internal static readonly Guid LanguageType_ILAssembly = new Guid(-1358664493, -12063, 0x11d2, 0x97, 0x7c, 0, 160, 0xc9, 180, 0xd5, 12);

		internal static readonly Guid DocumentType_Text = new Guid(0x5a869d0b, 0x6611, 0x11d3, 0xbd, 0x2a, 0, 0, 0xf8, 8, 0x49, 0xbd);

		internal static readonly Guid LanguageVendor_Microsoft = new Guid(-1723120188, -6423, 0x11d2, 0x90, 0x3f, 0, 0xc0, 0x4f, 0xa3, 2, 0xa1);
	}
}

