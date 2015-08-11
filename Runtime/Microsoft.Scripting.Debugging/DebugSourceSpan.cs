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
using System.Linq;

namespace Microsoft.Scripting.Debugging
{
	/// <summary>ソースのファイルと範囲を組み合わせます。これはまた Contains および Intersects 機能を提供します。</summary>
	sealed class DebugSourceSpan
	{
		internal DebugSourceSpan(DebugSourceFile sourceFile, int lineStart, int columnStart, int lineEnd, int columnEnd)
		{
			SourceFile = sourceFile;
			LineStart = lineStart;
			ColumnStart = columnStart;
			LineEnd = lineEnd;
			ColumnEnd = columnEnd;
		}

		internal DebugSourceSpan(DebugSourceFile sourceFile, SourceSpan dlrSpan) : this(sourceFile, dlrSpan.Start.Line, dlrSpan.Start.Column, dlrSpan.End.Line, dlrSpan.End.Column) { }

		internal DebugSourceFile SourceFile { get; private set; }

		internal int LineStart { get; private set; }

		internal int ColumnStart { get; private set; }

		internal int LineEnd { get; private set; }

		internal int ColumnEnd { get; private set; }

		internal SourceSpan ToDlrSpan()
		{
			return new SourceSpan(
				new SourceLocation(0, LineStart, ColumnStart),
				new SourceLocation(0, LineEnd, ColumnEnd == -1 ? Int32.MaxValue : ColumnEnd)
			);
		}

		internal bool Contains(DebugSourceSpan candidateSpan)
		{
			if (candidateSpan.SourceFile != SourceFile)
				return false;
			if (candidateSpan.LineStart < LineStart || candidateSpan.LineEnd > LineEnd)
				return false;
			if (candidateSpan.LineStart == LineStart && candidateSpan.ColumnStart < ColumnStart)
				return false;
			if (candidateSpan.LineEnd == LineEnd && candidateSpan.ColumnEnd > ColumnEnd)
				return false;
			return true;
		}

		internal bool Intersects(DebugSourceSpan candidateSpan)
		{
			if (candidateSpan.SourceFile != SourceFile)
				return false;
			if (candidateSpan.LineEnd < LineStart || candidateSpan.LineStart > LineEnd)
				return false;
			if (candidateSpan.LineStart == LineEnd && candidateSpan.ColumnStart > ColumnEnd)
				return false;
			if (candidateSpan.LineEnd == LineStart && ColumnStart > candidateSpan.ColumnEnd)
				return false;
			return true;
		}

		internal int GetSequencePointIndex(FunctionInfo funcInfo)
		{
			var res = funcInfo.SequencePoints.Select((x, i) => Tuple.Create(x, i)).Where(x => Intersects(x.Item1)).FirstOrDefault();
			return res == null ? Int32.MaxValue : res.Item2;
		}
	}
}
