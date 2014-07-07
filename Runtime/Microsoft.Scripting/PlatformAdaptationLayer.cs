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
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>
	/// DLR によって使用される潜在的にプラットフォーム固有となりうるシステム操作を抽象化します。
	/// ホストは DLR が動作するプラットフォームに合わせて <see cref="PlatformAdaptationLayer"/> を実装できます。
	/// </summary>
	[Serializable]
	public class PlatformAdaptationLayer
	{
		/// <summary><see cref="PlatformAdaptationLayer"/> オブジェクトの既定のインスタンスを取得します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly PlatformAdaptationLayer Default = new PlatformAdaptationLayer();

		/// <summary>このプラットフォームが Compact Framework であるかどうかを示す値を取得します。</summary>
		public static readonly bool IsCompactFramework = Environment.OSVersion.Platform == PlatformID.WinCE || Environment.OSVersion.Platform == PlatformID.Xbox;

		#region Assembly Loading

		/// <summary>長い形式の名前を指定してアセンブリを読み込みます。</summary>
		/// <param name="name">長い形式のアセンブリ名。</param>
		/// <returns>読み込み済みのアセンブリ。</returns>
		public virtual Assembly LoadAssembly(string name) { return Assembly.Load(name); }

		/// <summary>指定したパスのアセンブリ ファイルの内容を読み込みます。</summary>
		/// <param name="path">読み込むファイルのパス。</param>
		/// <returns>読み込み済みのアセンブリ。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFile")]
		public virtual Assembly LoadAssemblyFromPath(string path) { return Assembly.LoadFile(path); }

		/// <summary>スクリプトの実行を終了し、指定された終了コードを基になるプラットフォームに渡します。</summary>
		/// <param name="exitCode">プラットフォームに渡される終了コード。 処理が正常に完了したことを示す場合は 0 (ゼロ) を使用します。</param>
		public virtual void TerminateScriptExecution(int exitCode) { Environment.Exit(exitCode); }

		#endregion

		#region Virtual File System

		static bool IsSingleRootFileSystem { get { return Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX; } }

		/// <summary>現在のプラットフォームにおいてパスを比較する <see cref="System.StringComparer"/> オブジェクトを取得します。</summary>
		public virtual StringComparer PathComparer { get { return Environment.OSVersion.Platform == PlatformID.Unix ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase; } }

		/// <summary>指定されたファイルが存在するかどうかを示す値を返します。</summary>
		/// <param name="path">存在を調べるファイルのパスを指定します。</param>
		/// <returns>ファイルが存在すれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public virtual bool FileExists(string path) { return File.Exists(path); }

		/// <summary>指定されたディレクトリが存在するかどうかを示す値を返します。</summary>
		/// <param name="path">存在を調べるディレクトリのパスを指定します。</param>
		/// <returns>ディレクトリが存在すれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public virtual bool DirectoryExists(string path) { return Directory.Exists(path); }

		// TODO: better APIs
		/// <summary>指定されたファイルを読み書きするストリームを作成します。</summary>
		/// <param name="path">ファイルを示すパスを指定します。</param>
		/// <param name="mode">ファイルを開く方法を指定します。</param>
		/// <param name="access">ファイルに対するアクセス許可を指定します。</param>
		/// <param name="share">ファイルに対する共有アクセス許可を指定します。</param>
		/// <returns>開かれたストリーム。</returns>
		public virtual Stream OpenFileStream(string path, FileMode mode, FileAccess access, FileShare share) { return new FileStream(path, mode, access, share); }

		// TODO: better APIs
		/// <summary>指定されたファイルを読み書きするストリームを作成します。</summary>
		/// <param name="path">ファイルを示すパスを指定します。</param>
		/// <param name="mode">ファイルを開く方法を指定します。</param>
		/// <param name="access">ファイルに対するアクセス許可を指定します。</param>
		/// <param name="share">ファイルに対する共有アクセス許可を指定します。</param>
		/// <param name="bufferSize">ファイルの読み書きに使用されるバッファの大きさを指定します。</param>
		/// <returns>開かれたストリーム。</returns>
		public virtual Stream OpenFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize) { return new FileStream(path, mode, access, share, bufferSize); }

		// TODO: better APIs
		/// <summary>指定されたファイルを読み取るストリームを作成します。</summary>
		/// <param name="path">ファイルを示すパスを指定します。</param>
		/// <returns>開かれた読み取り専用のストリーム。</returns>
		public virtual Stream OpenInputFileStream(string path) { return new FileStream(path, FileMode.Open, FileAccess.Read); }

		// TODO: better APIs
		/// <summary>指定されたファイルに書き込むストリームを作成します。</summary>
		/// <param name="path">ファイルを示すパスを指定します。</param>
		/// <returns>開かれた書き込み専用のストリーム。</returns>
		public virtual Stream OpenOutputFileStream(string path) { return new FileStream(path, FileMode.Create, FileAccess.Write); }

		/// <summary>指定されたファイルを削除します。</summary>
		/// <param name="path">削除するファイルを示すパスを指定します。</param>
		/// <param name="deleteReadOnly">ファイルが読み取り専用でも削除するかどうかを示す値を指定します。</param>
		public virtual void DeleteFile(string path, bool deleteReadOnly)
		{
			FileInfo info = new FileInfo(path);
			if (deleteReadOnly && info.IsReadOnly)
				info.IsReadOnly = false;
			info.Delete();
		}

		/// <summary>指定されたパスにある検索条件に一致するファイルおよびディレクトリを返します。</summary>
		/// <param name="path">検索するパスを指定します。</param>
		/// <param name="searchPattern">検索条件を指定します。</param>
		/// <returns>見つかったファイルおよびディレクトリのパスを含む配列。</returns>
		public string[] GetFileSystemEntries(string path, string searchPattern) { return GetFileSystemEntries(path, searchPattern, true, true); }

		/// <summary>指定されたパスにある検索条件に一致するファイルやディレクトリを返します。</summary>
		/// <param name="path">検索するパスを指定します。</param>
		/// <param name="searchPattern">検索条件を指定します。</param>
		/// <param name="includeFiles">検索結果にファイルを含むかどうかを示す値を指定します。</param>
		/// <param name="includeDirectories">検索結果にディレクトリを含むかどうかを示す値を指定します。</param>
		/// <returns>見つかったファイルやディレクトリのパスを含む配列。</returns>
		public virtual string[] GetFileSystemEntries(string path, string searchPattern, bool includeFiles, bool includeDirectories)
		{
			if (includeFiles && includeDirectories)
				return Directory.GetFileSystemEntries(path, searchPattern);
			if (includeFiles)
				return Directory.GetFiles(path, searchPattern);
			if (includeDirectories)
				return Directory.GetDirectories(path, searchPattern);
			return ArrayUtils.EmptyStrings;
		}

		/// <summary>指定されたパスに対するフルパスを取得します。</summary>
		/// <param name="path">フルパスを取得するパスを指定します。</param>
		/// <returns>フルパス。</returns>
		/// <exception cref="ArgumentException">正しくないパスを指定しました。</exception>
		public virtual string GetFullPath(string path)
		{
			try { return Path.GetFullPath(path); }
			catch { throw Error.InvalidPath(); }
		}

		/// <summary>2 つの文字列を 1 つのパスに結合します。</summary>
		/// <param name="path1">結合する 1 番目のパス。</param>
		/// <param name="path2">結合する 2 番目のパス。</param>
		/// <returns>結合されたパス。 </returns>
		public virtual string CombinePaths(string path1, string path2) { return Path.Combine(path1, path2); }

		/// <summary>指定したパス文字列のファイル名と拡張子を返します。</summary>
		/// <param name="path">ファイル名と拡張子の取得元のパス文字列。</param>
		/// <returns><paramref name="path"/> の最後のディレクトリ文字の後ろの文字。</returns>
		public virtual string GetFileName(string path) { return Path.GetFileName(path); }

		/// <summary>指定したパス文字列のディレクトリ情報を返します。</summary>
		/// <param name="path">ファイルまたはディレクトリのパス。</param>
		/// <returns><paramref name="path"/> のディレクトリ情報。</returns>
		public virtual string GetDirectoryName(string path) { return Path.GetDirectoryName(path); }

		/// <summary>指定したパス文字列の拡張子を返します。</summary>
		/// <param name="path">拡張子の取得元のパス文字列。</param>
		/// <returns>指定したパスの拡張子 (ピリオド "." を含む)、<c>null</c>、または <see cref="System.String.Empty"/>。</returns>
		public virtual string GetExtension(string path) { return Path.GetExtension(path); }

		/// <summary>指定したパス文字列のファイル名を拡張子を付けずに返します。</summary>
		/// <param name="path">ファイルのパス。</param>
		/// <returns>拡張子およびドットを除いたファイル名。</returns>
		public virtual string GetFileNameWithoutExtension(string path) { return Path.GetFileNameWithoutExtension(path); }

		/// <summary>指定されたパスが絶対パスであるかどうかを調べます。</summary>
		/// <param name="path">絶対パスかどうかを調べるパスを指定します。</param>
		/// <returns>パスが絶対パスならば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		/// <exception cref="ArgumentException">正しくないパスを指定しました。</exception>
		public virtual bool IsAbsolutePath(string path)
		{
			// GetPathRoot は次のいずれかを返します:
			// "" -> 現在のディレクトリに相対的
			// "\" -> 現在のディレクトリのドライブに相対的
			// "X:" -> 現在のディレクトリに相対的、異なるドライブの可能性あり
			// "X:\" -> 絶対的
			if (IsSingleRootFileSystem)
				return Path.IsPathRooted(path);
			var root = Path.GetPathRoot(path);
			return root.EndsWith(@":\") || root.EndsWith(@":/");
		}

		/// <summary>アプリケーションの現在の作業ディレクトリを取得または設定します。</summary>
		public virtual string CurrentDirectory
		{
			get { return Directory.GetCurrentDirectory(); }
			set { Directory.SetCurrentDirectory(value); }
		}

		/// <summary>指定したパスにすべてのディレクトリとサブディレクトリを作成します。</summary>
		/// <param name="path">作成するディレクトリ パス。</param>
		public virtual void CreateDirectory(string path) { Directory.CreateDirectory(path); }

		/// <summary>指定したディレクトリと、特に指定されている場合はディレクトリ内の任意のサブディレクトリおよびファイルを削除します。</summary>
		/// <param name="path">削除するディレクトリの名前。</param>
		/// <param name="recursive"><paramref name="path"/> のディレクトリ、サブディレクトリ、およびファイルを削除する場合は <c>true</c>。それ以外の場合は <c>false。</c></param>
		public virtual void DeleteDirectory(string path, bool recursive) { Directory.Delete(path, recursive); }

		/// <summary>ファイルまたはディレクトリ、およびその内容を新しい場所に移動します。</summary>
		/// <param name="sourcePath">移動するファイルまたはディレクトリのパス。</param>
		/// <param name="destinationPath"><paramref name="sourcePath"/> の新しい位置へのパス。 <paramref name="sourcePath"/> がファイルの場合は、<paramref name="destinationPath"/> もファイル名にする必要があります。</param>
		public virtual void MoveFileSystemEntry(string sourcePath, string destinationPath) { Directory.Move(sourcePath, destinationPath); }

		#endregion

		#region Environmental Variables

		/// <summary>現在のプロセスから環境変数の値を取得します。</summary>
		/// <param name="key">環境変数の名前。</param>
		/// <returns><paramref name="key"/> で指定された環境変数の値。環境変数が見つからなかった場合は <c>null</c>。</returns>
		public virtual string GetEnvironmentVariable(string key) { return Environment.GetEnvironmentVariable(key); }

		/// <summary>現在のプロセスに格納されている環境変数を作成、変更、または削除します。</summary>
		/// <param name="key">環境変数の名前。</param>
		/// <param name="value"><paramref name="key"/> に割り当てる値。</param>
		public virtual void SetEnvironmentVariable(string key, string value)
		{
			if (value != null && value.Length == 0 && !NativeMethods.SetEnvironmentVariable(key, value))
				// System.Environment.SetEnvironmentVariable interprets an empty value string as 
				// deleting the environment variable. So we use the native SetEnvironmentVariable 
				// function here which allows setting of the value to an empty string.
				// This will require high trust and will fail in sandboxed environments
				throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "SetEnvironmentVariable failed");
			Environment.SetEnvironmentVariable(key, value);
		}

		/// <summary>すべての環境変数の名前と値を現在のプロセスから取得します。</summary>
		public virtual System.Collections.IDictionary EnvironmentVariables { get { return Environment.GetEnvironmentVariables(); } }

		#endregion
	}
}
