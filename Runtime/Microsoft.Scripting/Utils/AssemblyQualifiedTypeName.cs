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
	/// <summary>アセンブリ名によって修飾された型名を表します。</summary>
	[Serializable]
	struct AssemblyQualifiedTypeName : IEquatable<AssemblyQualifiedTypeName>
	{
		/// <summary>型名を取得します。</summary>
		public readonly string TypeName;
		/// <summary>アセンブリ名を表す <see cref="System.Reflection.AssemblyName"/> を取得します。</summary>
		public readonly AssemblyName AssemblyName;

		/// <summary>型名とそれを修飾するアセンブリ名を使用して、<see cref="Microsoft.Scripting.Utils.AssemblyQualifiedTypeName"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="typeName">型名を指定します。</param>
		/// <param name="assemblyName">アセンブリ名を表す <see cref="System.Reflection.AssemblyName"/> を指定します。</param>
		public AssemblyQualifiedTypeName(string typeName, AssemblyName assemblyName)
		{
			ContractUtils.RequiresNotNull(typeName, "typeName");
			ContractUtils.RequiresNotNull(assemblyName, "assemblyName");
			TypeName = typeName;
			AssemblyName = assemblyName;
		}

		/// <summary>型を使用して、<see cref="Microsoft.Scripting.Utils.AssemblyQualifiedTypeName"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="type">型を指定します。</param>
		public AssemblyQualifiedTypeName(Type type)
		{
			TypeName = type.FullName;
			AssemblyName = type.Assembly.GetName();
		}

		/// <summary>文字列で表されたアセンブリ修飾型名から <see cref="Microsoft.Scripting.Utils.AssemblyQualifiedTypeName"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="assemblyQualifiedTypeName">アセンブリ修飾型名を指定します。</param>
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

		/// <summary>指定された引数を解析して、<see cref="Microsoft.Scripting.Utils.AssemblyQualifiedTypeName"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="str">引数として渡された文字列を指定します。</param>
		/// <param name="argumentName">引数の名前を指定します。</param>
		internal static AssemblyQualifiedTypeName ParseArgument(string str, string argumentName)
		{
			Assert.NotEmpty(argumentName);
			try { return new AssemblyQualifiedTypeName(str); }
			catch (ArgumentException e) { throw new ArgumentException(e.Message, argumentName, e.InnerException); }
		}

		/// <summary>指定されたアセンブリ修飾型名がこのアセンブリ修飾型名と等しいかどうかを判断します。</summary>
		/// <param name="other">等価比較をするアセンブリ修飾型名を指定します。</param>
		/// <returns>等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool Equals(AssemblyQualifiedTypeName other) { return TypeName == other.TypeName && AssemblyName.FullName == other.AssemblyName.FullName; }

		/// <summary>指定されたオブジェクトがこのオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">等価比較をするオブジェクトを指定します。</param>
		/// <returns>等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public override bool Equals(object obj) { return obj is AssemblyQualifiedTypeName && Equals((AssemblyQualifiedTypeName)obj); }

		/// <summary>このオブジェクトに対するハッシュ値を返します。</summary>
		/// <returns>ハッシュ値。</returns>
		public override int GetHashCode() { return TypeName.GetHashCode() ^ AssemblyName.FullName.GetHashCode(); }

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>文字列表現。</returns>
		public override string ToString() { return TypeName + ", " + AssemblyName.FullName; }

		/// <summary>指定された 2 つのアセンブリ修飾型名が等しいかどうかを判断します。</summary>
		/// <param name="name">1 つ目のアセンブリ修飾型名を指定します。</param>
		/// <param name="other">2 つ目のアセンブリ修飾型名を指定します。</param>
		/// <returns>等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator ==(AssemblyQualifiedTypeName name, AssemblyQualifiedTypeName other) { return name.Equals(other); }

		/// <summary>指定された 2 つのアセンブリ修飾型名が等しくないかどうかを判断します。</summary>
		/// <param name="name">1 つ目のアセンブリ修飾型名を指定します。</param>
		/// <param name="other">2 つ目のアセンブリ修飾型名を指定します。</param>
		/// <returns>等しくない場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator !=(AssemblyQualifiedTypeName name, AssemblyQualifiedTypeName other) { return !name.Equals(other); }
	}
}
