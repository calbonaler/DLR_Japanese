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
	/// DLR �ɂ���Ďg�p�������ݓI�Ƀv���b�g�t�H�[���ŗL�ƂȂ肤��V�X�e������𒊏ۉ����܂��B
	/// �z�X�g�� DLR �����삷��v���b�g�t�H�[���ɍ��킹�� <see cref="PlatformAdaptationLayer"/> �������ł��܂��B
	/// </summary>
	[Serializable]
	public class PlatformAdaptationLayer
	{
		/// <summary><see cref="PlatformAdaptationLayer"/> �I�u�W�F�N�g�̊���̃C���X�^���X���擾���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly PlatformAdaptationLayer Default = new PlatformAdaptationLayer();

		/// <summary>���̃v���b�g�t�H�[���� Compact Framework �ł��邩�ǂ����������l���擾���܂��B</summary>
		public static readonly bool IsCompactFramework = Environment.OSVersion.Platform == PlatformID.WinCE || Environment.OSVersion.Platform == PlatformID.Xbox;

		#region Assembly Loading

		/// <summary>�����`���̖��O���w�肵�ăA�Z���u����ǂݍ��݂܂��B</summary>
		/// <param name="name">�����`���̃A�Z���u�����B</param>
		/// <returns>�ǂݍ��ݍς݂̃A�Z���u���B</returns>
		public virtual Assembly LoadAssembly(string name) { return Assembly.Load(name); }

		/// <summary>�w�肵���p�X�̃A�Z���u�� �t�@�C���̓��e��ǂݍ��݂܂��B</summary>
		/// <param name="path">�ǂݍ��ރt�@�C���̃p�X�B</param>
		/// <returns>�ǂݍ��ݍς݂̃A�Z���u���B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFile")]
		public virtual Assembly LoadAssemblyFromPath(string path) { return Assembly.LoadFile(path); }

		/// <summary>�X�N���v�g�̎��s���I�����A�w�肳�ꂽ�I���R�[�h����ɂȂ�v���b�g�t�H�[���ɓn���܂��B</summary>
		/// <param name="exitCode">�v���b�g�t�H�[���ɓn�����I���R�[�h�B ����������Ɋ����������Ƃ������ꍇ�� 0 (�[��) ���g�p���܂��B</param>
		public virtual void TerminateScriptExecution(int exitCode) { Environment.Exit(exitCode); }

		#endregion

		#region Virtual File System

		static bool IsSingleRootFileSystem { get { return Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX; } }

		/// <summary>���݂̃v���b�g�t�H�[���ɂ����ăp�X���r���� <see cref="System.StringComparer"/> �I�u�W�F�N�g���擾���܂��B</summary>
		public virtual StringComparer PathComparer { get { return Environment.OSVersion.Platform == PlatformID.Unix ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase; } }

		/// <summary>�w�肳�ꂽ�t�@�C�������݂��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="path">���݂𒲂ׂ�t�@�C���̃p�X���w�肵�܂��B</param>
		/// <returns>�t�@�C�������݂���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public virtual bool FileExists(string path) { return File.Exists(path); }

		/// <summary>�w�肳�ꂽ�f�B���N�g�������݂��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="path">���݂𒲂ׂ�f�B���N�g���̃p�X���w�肵�܂��B</param>
		/// <returns>�f�B���N�g�������݂���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public virtual bool DirectoryExists(string path) { return Directory.Exists(path); }

		// TODO: better APIs
		/// <summary>�w�肳�ꂽ�t�@�C����ǂݏ�������X�g���[�����쐬���܂��B</summary>
		/// <param name="path">�t�@�C���������p�X���w�肵�܂��B</param>
		/// <param name="mode">�t�@�C�����J�����@���w�肵�܂��B</param>
		/// <param name="access">�t�@�C���ɑ΂���A�N�Z�X�����w�肵�܂��B</param>
		/// <param name="share">�t�@�C���ɑ΂��鋤�L�A�N�Z�X�����w�肵�܂��B</param>
		/// <returns>�J���ꂽ�X�g���[���B</returns>
		public virtual Stream OpenFileStream(string path, FileMode mode, FileAccess access, FileShare share) { return new FileStream(path, mode, access, share); }

		// TODO: better APIs
		/// <summary>�w�肳�ꂽ�t�@�C����ǂݏ�������X�g���[�����쐬���܂��B</summary>
		/// <param name="path">�t�@�C���������p�X���w�肵�܂��B</param>
		/// <param name="mode">�t�@�C�����J�����@���w�肵�܂��B</param>
		/// <param name="access">�t�@�C���ɑ΂���A�N�Z�X�����w�肵�܂��B</param>
		/// <param name="share">�t�@�C���ɑ΂��鋤�L�A�N�Z�X�����w�肵�܂��B</param>
		/// <param name="bufferSize">�t�@�C���̓ǂݏ����Ɏg�p�����o�b�t�@�̑傫�����w�肵�܂��B</param>
		/// <returns>�J���ꂽ�X�g���[���B</returns>
		public virtual Stream OpenFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize) { return new FileStream(path, mode, access, share, bufferSize); }

		// TODO: better APIs
		/// <summary>�w�肳�ꂽ�t�@�C����ǂݎ��X�g���[�����쐬���܂��B</summary>
		/// <param name="path">�t�@�C���������p�X���w�肵�܂��B</param>
		/// <returns>�J���ꂽ�ǂݎ���p�̃X�g���[���B</returns>
		public virtual Stream OpenInputFileStream(string path) { return new FileStream(path, FileMode.Open, FileAccess.Read); }

		// TODO: better APIs
		/// <summary>�w�肳�ꂽ�t�@�C���ɏ������ރX�g���[�����쐬���܂��B</summary>
		/// <param name="path">�t�@�C���������p�X���w�肵�܂��B</param>
		/// <returns>�J���ꂽ�������ݐ�p�̃X�g���[���B</returns>
		public virtual Stream OpenOutputFileStream(string path) { return new FileStream(path, FileMode.Create, FileAccess.Write); }

		/// <summary>�w�肳�ꂽ�t�@�C�����폜���܂��B</summary>
		/// <param name="path">�폜����t�@�C���������p�X���w�肵�܂��B</param>
		/// <param name="deleteReadOnly">�t�@�C�����ǂݎ���p�ł��폜���邩�ǂ����������l���w�肵�܂��B</param>
		public virtual void DeleteFile(string path, bool deleteReadOnly)
		{
			FileInfo info = new FileInfo(path);
			if (deleteReadOnly && info.IsReadOnly)
				info.IsReadOnly = false;
			info.Delete();
		}

		/// <summary>�w�肳�ꂽ�p�X�ɂ��錟�������Ɉ�v����t�@�C������уf�B���N�g����Ԃ��܂��B</summary>
		/// <param name="path">��������p�X���w�肵�܂��B</param>
		/// <param name="searchPattern">�����������w�肵�܂��B</param>
		/// <returns>���������t�@�C������уf�B���N�g���̃p�X���܂ޔz��B</returns>
		public string[] GetFileSystemEntries(string path, string searchPattern) { return GetFileSystemEntries(path, searchPattern, true, true); }

		/// <summary>�w�肳�ꂽ�p�X�ɂ��錟�������Ɉ�v����t�@�C����f�B���N�g����Ԃ��܂��B</summary>
		/// <param name="path">��������p�X���w�肵�܂��B</param>
		/// <param name="searchPattern">�����������w�肵�܂��B</param>
		/// <param name="includeFiles">�������ʂɃt�@�C�����܂ނ��ǂ����������l���w�肵�܂��B</param>
		/// <param name="includeDirectories">�������ʂɃf�B���N�g�����܂ނ��ǂ����������l���w�肵�܂��B</param>
		/// <returns>���������t�@�C����f�B���N�g���̃p�X���܂ޔz��B</returns>
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

		/// <summary>�w�肳�ꂽ�p�X�ɑ΂���t���p�X���擾���܂��B</summary>
		/// <param name="path">�t���p�X���擾����p�X���w�肵�܂��B</param>
		/// <returns>�t���p�X�B</returns>
		/// <exception cref="ArgumentException">�������Ȃ��p�X���w�肵�܂����B</exception>
		public virtual string GetFullPath(string path)
		{
			try { return Path.GetFullPath(path); }
			catch { throw Error.InvalidPath(); }
		}

		/// <summary>2 �̕������ 1 �̃p�X�Ɍ������܂��B</summary>
		/// <param name="path1">�������� 1 �Ԗڂ̃p�X�B</param>
		/// <param name="path2">�������� 2 �Ԗڂ̃p�X�B</param>
		/// <returns>�������ꂽ�p�X�B </returns>
		public virtual string CombinePaths(string path1, string path2) { return Path.Combine(path1, path2); }

		/// <summary>�w�肵���p�X������̃t�@�C�����Ɗg���q��Ԃ��܂��B</summary>
		/// <param name="path">�t�@�C�����Ɗg���q�̎擾���̃p�X������B</param>
		/// <returns><paramref name="path"/> �̍Ō�̃f�B���N�g�������̌��̕����B</returns>
		public virtual string GetFileName(string path) { return Path.GetFileName(path); }

		/// <summary>�w�肵���p�X������̃f�B���N�g������Ԃ��܂��B</summary>
		/// <param name="path">�t�@�C���܂��̓f�B���N�g���̃p�X�B</param>
		/// <returns><paramref name="path"/> �̃f�B���N�g�����B</returns>
		public virtual string GetDirectoryName(string path) { return Path.GetDirectoryName(path); }

		/// <summary>�w�肵���p�X������̊g���q��Ԃ��܂��B</summary>
		/// <param name="path">�g���q�̎擾���̃p�X������B</param>
		/// <returns>�w�肵���p�X�̊g���q (�s���I�h "." ���܂�)�A<c>null</c>�A�܂��� <see cref="System.String.Empty"/>�B</returns>
		public virtual string GetExtension(string path) { return Path.GetExtension(path); }

		/// <summary>�w�肵���p�X������̃t�@�C�������g���q��t�����ɕԂ��܂��B</summary>
		/// <param name="path">�t�@�C���̃p�X�B</param>
		/// <returns>�g���q����уh�b�g���������t�@�C�����B</returns>
		public virtual string GetFileNameWithoutExtension(string path) { return Path.GetFileNameWithoutExtension(path); }

		/// <summary>�w�肳�ꂽ�p�X����΃p�X�ł��邩�ǂ����𒲂ׂ܂��B</summary>
		/// <param name="path">��΃p�X���ǂ����𒲂ׂ�p�X���w�肵�܂��B</param>
		/// <returns>�p�X����΃p�X�Ȃ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		/// <exception cref="ArgumentException">�������Ȃ��p�X���w�肵�܂����B</exception>
		public virtual bool IsAbsolutePath(string path)
		{
			// GetPathRoot �͎��̂����ꂩ��Ԃ��܂�:
			// "" -> ���݂̃f�B���N�g���ɑ��ΓI
			// "\" -> ���݂̃f�B���N�g���̃h���C�u�ɑ��ΓI
			// "X:" -> ���݂̃f�B���N�g���ɑ��ΓI�A�قȂ�h���C�u�̉\������
			// "X:\" -> ��ΓI
			if (IsSingleRootFileSystem)
				return Path.IsPathRooted(path);
			var root = Path.GetPathRoot(path);
			return root.EndsWith(@":\") || root.EndsWith(@":/");
		}

		/// <summary>�A�v���P�[�V�����̌��݂̍�ƃf�B���N�g�����擾�܂��͐ݒ肵�܂��B</summary>
		public virtual string CurrentDirectory
		{
			get { return Directory.GetCurrentDirectory(); }
			set { Directory.SetCurrentDirectory(value); }
		}

		/// <summary>�w�肵���p�X�ɂ��ׂẴf�B���N�g���ƃT�u�f�B���N�g�����쐬���܂��B</summary>
		/// <param name="path">�쐬����f�B���N�g�� �p�X�B</param>
		public virtual void CreateDirectory(string path) { Directory.CreateDirectory(path); }

		/// <summary>�w�肵���f�B���N�g���ƁA���Ɏw�肳��Ă���ꍇ�̓f�B���N�g�����̔C�ӂ̃T�u�f�B���N�g������уt�@�C�����폜���܂��B</summary>
		/// <param name="path">�폜����f�B���N�g���̖��O�B</param>
		/// <param name="recursive"><paramref name="path"/> �̃f�B���N�g���A�T�u�f�B���N�g���A����уt�@�C�����폜����ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false�B</c></param>
		public virtual void DeleteDirectory(string path, bool recursive) { Directory.Delete(path, recursive); }

		/// <summary>�t�@�C���܂��̓f�B���N�g���A����т��̓��e��V�����ꏊ�Ɉړ����܂��B</summary>
		/// <param name="sourcePath">�ړ�����t�@�C���܂��̓f�B���N�g���̃p�X�B</param>
		/// <param name="destinationPath"><paramref name="sourcePath"/> �̐V�����ʒu�ւ̃p�X�B <paramref name="sourcePath"/> ���t�@�C���̏ꍇ�́A<paramref name="destinationPath"/> ���t�@�C�����ɂ���K�v������܂��B</param>
		public virtual void MoveFileSystemEntry(string sourcePath, string destinationPath) { Directory.Move(sourcePath, destinationPath); }

		#endregion

		#region Environmental Variables

		/// <summary>���݂̃v���Z�X������ϐ��̒l���擾���܂��B</summary>
		/// <param name="key">���ϐ��̖��O�B</param>
		/// <returns><paramref name="key"/> �Ŏw�肳�ꂽ���ϐ��̒l�B���ϐ���������Ȃ������ꍇ�� <c>null</c>�B</returns>
		public virtual string GetEnvironmentVariable(string key) { return Environment.GetEnvironmentVariable(key); }

		/// <summary>���݂̃v���Z�X�Ɋi�[����Ă�����ϐ����쐬�A�ύX�A�܂��͍폜���܂��B</summary>
		/// <param name="key">���ϐ��̖��O�B</param>
		/// <param name="value"><paramref name="key"/> �Ɋ��蓖�Ă�l�B</param>
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

		/// <summary>���ׂĂ̊��ϐ��̖��O�ƒl�����݂̃v���Z�X����擾���܂��B</summary>
		public virtual System.Collections.IDictionary EnvironmentVariables { get { return Environment.GetEnvironmentVariables(); } }

		#endregion
	}
}
