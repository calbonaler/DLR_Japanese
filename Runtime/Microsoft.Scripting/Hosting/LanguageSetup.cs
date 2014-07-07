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
using System.Linq;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>言語のセットアップに必要な情報を格納します。</summary>
	[Serializable]
	public sealed class LanguageSetup
	{
		string _typeName;
		string _displayName;
		bool _frozen;
		bool? _exceptionDetail;

		/// <summary>言語プロバイダのアセンブリ修飾型名を使用して、<see cref="Microsoft.Scripting.Hosting.LanguageSetup"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="typeName">言語プロバイダを表すアセンブリ修飾型名を指定します。</param>
		public LanguageSetup(string typeName) : this(typeName, "", ArrayUtils.EmptyStrings, ArrayUtils.EmptyStrings) { }

		/// <summary>
		/// 言語プロバイダのアセンブリ修飾型名および言語の表示名を使用して、<see cref="Microsoft.Scripting.Hosting.LanguageSetup"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="typeName">言語プロバイダを表すアセンブリ修飾型名を指定します。</param>
		/// <param name="displayName">この言語の表示名を指定します。</param>
		public LanguageSetup(string typeName, string displayName) : this(typeName, displayName, ArrayUtils.EmptyStrings, ArrayUtils.EmptyStrings) { }

		/// <summary>
		/// 言語プロバイダのアセンブリ修飾型名、言語の表示名、大文字小文字が無視される言語の名前、ファイル拡張子を使用して、
		/// <see cref="Microsoft.Scripting.Hosting.LanguageSetup"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="typeName">言語プロバイダを表すアセンブリ修飾型名を指定します。</param>
		/// <param name="displayName">この言語の表示名を指定します。</param>
		/// <param name="names">この言語の名前のリストを指定します。</param>
		/// <param name="fileExtensions">この言語のソースファイルに使用する拡張子のリストを指定します。</param>
		public LanguageSetup(string typeName, string displayName, IEnumerable<string> names, IEnumerable<string> fileExtensions)
		{
			ContractUtils.RequiresNotEmpty(typeName, "typeName");
			ContractUtils.RequiresNotNull(displayName, "displayName");
			ContractUtils.RequiresNotNull(names, "names");
			ContractUtils.RequiresNotNull(fileExtensions, "fileExtensions");
			_typeName = typeName;
			_displayName = displayName;
			Names = new List<string>(names);
			FileExtensions = new List<string>(fileExtensions);
			Options = new Dictionary<string, object>();
		}

		/// <summary>厳密に型指定された値としてオプションを取得します。</summary>
		/// <param name="name">取得するオプションの名前を指定します。</param>
		/// <param name="defaultValue">オプションが存在しない場合の既定値を指定します。</param>
		public T GetOption<T>(string name, T defaultValue)
		{
			object value;
			if (Options != null && Options.TryGetValue(name, out value))
			{
				if (value is T)
					return (T)value;
				return (T)Convert.ChangeType(value, typeof(T), Thread.CurrentThread.CurrentCulture);
			}
			return defaultValue;
		}

		/// <summary>言語プロバイダのアセンブリ修飾型名を取得または設定します。</summary>
		public string TypeName
		{
			get { return _typeName; }
			set
			{
				ContractUtils.RequiresNotEmpty(value, "value");
				CheckFrozen();
				_typeName = value;
			}
		}

		/// <summary>言語の表示名を取得または設定します。空である場合、<see cref="Names"/> の最初の要素が使用されます。</summary>
		public string DisplayName
		{
			get { return _displayName; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				CheckFrozen();
				_displayName = value;
			}
		}

		/// <summary>大文字と小文字を区別しない言語の名前のリストを取得します。</summary>
		public IList<string> Names { get; private set; }

		/// <summary>大文字と小文字を区別しないファイルの拡張子のリストを取得します。ドットで始めることもできます。</summary>
		public IList<string> FileExtensions { get; private set; }

		/// <summary>オプションのリストを取得します。</summary>
		public IDictionary<string, object> Options { get; private set; }

		/// <summary>例外を詳細に説明するかどうかを示す値を取得または設定します。</summary>
		public bool ExceptionDetail
		{
			get { return GetCachedOption("ExceptionDetail", ref _exceptionDetail); }
			set
			{
				CheckFrozen();
				Options["ExceptionDetail"] = value;
			}
		}

		bool GetCachedOption(string name, ref bool? storage)
		{
			if (storage.HasValue)
				return storage.Value;
			if (_frozen)
			{
				storage = GetOption<bool>(name, false);
				return storage.Value;
			}
			return GetOption<bool>(name, false);
		}

		/// <summary>このオブジェクトを変更不可能にします。</summary>
		internal void Freeze()
		{
			_frozen = true;
			Names = new ReadOnlyCollection<string>(Names.ToArray());
			FileExtensions = new ReadOnlyCollection<string>(FileExtensions.ToArray());
			Options = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(Options));
		}

		void CheckFrozen()
		{
			if (_frozen)
				throw new InvalidOperationException("ScriptRuntime の作成に使用された後はこのオブジェクトを変更することはできません。");
		}
	}
}
