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
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>言語に関するオプションを格納します。</summary>
	[Serializable]
	public class LanguageOptions
	{
		/// <summary>ランタイム履歴に基づいた最適化を行わないかどうかを示す値を取得します。</summary>
		public bool NoAdaptiveCompilation { get; private set; }

		/// <summary>インタプリタがコンパイルを始める前の反復回数を取得します。</summary>
		public int CompilationThreshold { get; private set; }

		/// <summary>例外が補足された際に例外の詳細 (コールスタック) を表示するかどうかを示す値を取得または設定します。</summary>
		public bool ExceptionDetail { get; set; }

		/// <summary>CLR の例外を表示するかどうかを示す値を取得または設定します。</summary>
		public bool ShowClrExceptions { get; set; }

		/// <summary>パフォーマンス統計情報を収集するかどうかを示す値を取得します。</summary>
		public bool PerfStats { get; private set; }

		/// <summary>ホストによって提供された初期のファイル検索パスを取得します。</summary>
		public ReadOnlyCollection<string> SearchPaths { get; private set; }

		/// <summary><see cref="Microsoft.Scripting.LanguageOptions"/> クラスの新しいインスタンスを初期化します。</summary>
		public LanguageOptions() : this(null) { }

		/// <summary>オプションを格納するディクショナリを使用して、<see cref="Microsoft.Scripting.LanguageOptions"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="options">このオブジェクトにオプションを設定するために使用されるディクショナリを指定します。</param>
		public LanguageOptions(IDictionary<string, object> options)
		{
			ExceptionDetail = GetOption(options, "ExceptionDetail", false);
			ShowClrExceptions = GetOption(options, "ShowClrExceptions", false);
			PerfStats = GetOption(options, "PerfStats", false);
			NoAdaptiveCompilation = GetOption(options, "NoAdaptiveCompilation", false);
			CompilationThreshold = GetOption(options, "CompilationThreshold", -1);
			SearchPaths = GetSearchPathsOption(options) ?? EmptyStringCollection;
		}

		/// <summary>オプションを格納するディクショナリから指定した名前のオプションに対する値を取得します。</summary>
		/// <param name="options">オプションを格納するディクショナリを指定します。</param>
		/// <param name="name">取得するオプションの名前を指定します。</param>
		/// <param name="defaultValue">取得するオプションの値の既定値を指定します。</param>
		/// <returns>取得されたオプションの値。オプションが存在しない場合は既定値。</returns>
		public static T GetOption<T>(IDictionary<string, object> options, string name, T defaultValue)
		{
			object value;
			if (options != null && options.TryGetValue(name, out value))
			{
				if (value is T)
					return (T)value;
				return (T)Convert.ChangeType(value, typeof(T), Thread.CurrentThread.CurrentCulture);
			}
			return defaultValue;
		}

		/// <summary>値が <c>null</c> でない文字列のコレクションであると予測されるオプションを取得します。オプションのコピーの読み取り専用の値を取得します。</summary>
		/// <param name="options">オプションを格納するディクショナリを指定します。</param>
		/// <param name="name">取得するオプションの名前を指定します。</param>
		/// <param name="separators">取得された文字列を分割する Unicode 文字の配列を指定します。</param>
		/// <returns>取得されたオプションの値。</returns>
		public static ReadOnlyCollection<string> GetStringCollectionOption(IDictionary<string, object> options, string name, params char[] separators)
		{
			object value;
			if (options == null || !options.TryGetValue(name, out value))
				return null;
			// a collection:
			var collection = value as ICollection<string>;
			if (collection != null)
			{
				if (collection.Any(x => x == null))
					throw new ArgumentException(string.Format("Invalid value for option {0}: collection shouldn't containt null items", name));
				return new ReadOnlyCollection<string>(collection.ToArray());
			}
			// a string:
			var strValue = value as string;
			if (strValue != null && separators != null && separators.Length > 0)
				return new ReadOnlyCollection<string>(strValue.Split(separators, int.MaxValue, StringSplitOptions.RemoveEmptyEntries));
			throw new ArgumentException(string.Format("Invalid value for option {0}", name));
		}

		/// <summary>オプションを格納するディクショナリから検索パスを取得します。</summary>
		/// <param name="options">オプションを格納するディクショナリを指定します。</param>
		/// <returns>取得されたオプションの値。</returns>
		public static ReadOnlyCollection<string> GetSearchPathsOption(IDictionary<string, object> options) { return GetStringCollectionOption(options, "SearchPaths", Path.PathSeparator); }

		/// <summary>読み取り専用の文字列の空のコレクションを取得します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		protected static readonly ReadOnlyCollection<string> EmptyStringCollection = new ReadOnlyCollection<string>(ArrayUtils.EmptyStrings);
	}

}