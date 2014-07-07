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
using System.Collections;
using System.Collections.Generic;
using Microsoft.Scripting.Ast;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>デバッグ可能なジェネレータを表します。</summary>
	public interface IDebuggableGenerator
	{
		/// <summary>現在の状態に対する yield マーカーの位置を取得します。</summary>
		int YieldMarkerLocation { get; set; }
	}

	delegate void GeneratorNext<T>(ref int state, ref T current);

	class GeneratorEnumerable<T> : IEnumerable<T>
	{
		protected readonly Func<GeneratorNext<T>> _next;

		internal GeneratorEnumerable(Func<GeneratorNext<T>> next) { _next = next; }

		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return new GeneratorEnumerator<T>(_next()); }

		IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable<T>)this).GetEnumerator(); }
	}

	class GeneratorEnumerator<T> : IEnumerator<T>
	{
		readonly GeneratorNext<T> _next;
		T _current;
		protected int _state;

		internal GeneratorEnumerator(GeneratorNext<T> next)
		{
			_next = next;
			_state = GeneratorRewriter.NotStarted;
		}

		T IEnumerator<T>.Current { get { return _current; } }

		bool IEnumerator.MoveNext()
		{
			_next(ref _state, ref _current);
			return _state != GeneratorRewriter.Finished;
		}

		object IEnumerator.Current { get { return ((IEnumerator<T>)this).Current; } }

		void IEnumerator.Reset() { throw new NotSupportedException(); }

		void IDisposable.Dispose() { GC.SuppressFinalize(this); } // TODO: call back into MoveNext, running any finally blocks that have not been run. This is needed for a complete generator implementation but is not needed yet.
	}

	sealed class DebugGeneratorEnumerable<T> : GeneratorEnumerable<T>, IEnumerable<T>
	{
		readonly int[] _yieldMarkers;
		internal DebugGeneratorEnumerable(Func<GeneratorNext<T>> next, int[] yieldMarkers) : base(next) { _yieldMarkers = yieldMarkers; }

		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return new DebugGeneratorEnumerator<T>(_next(), _yieldMarkers); }
	}

	sealed class DebugGeneratorEnumerator<T> : GeneratorEnumerator<T>, IDebuggableGenerator
	{
		readonly int[] _yieldMarkers;
		internal DebugGeneratorEnumerator(GeneratorNext<T> next, int[] yieldMarkers) : base(next) { _yieldMarkers = yieldMarkers; }

		int IDebuggableGenerator.YieldMarkerLocation
		{
			get
			{
				if (_state < _yieldMarkers.Length)
					return _yieldMarkers[_state];
				throw new InvalidOperationException("unknown yield marker");
			}
			set
			{
				for (int i = 0; i < _yieldMarkers.Length; i++)
				{
					if (_yieldMarkers[i] == value)
					{
						_state = i;
						return;
					}
				}
				throw new InvalidOperationException("unknown yield marker");
			}
		}
	}

	public partial class ScriptingRuntimeHelpers
	{
		[Obsolete("do not call this method", true)]
		internal static IEnumerable<T> MakeGenerator<T>(Func<GeneratorNext<T>> next) { return new GeneratorEnumerable<T>(next); }

		[Obsolete("do not call this method", true)]
		internal static IEnumerator<T> MakeGenerator<T>(GeneratorNext<T> next) { return new GeneratorEnumerator<T>(next); }

		[Obsolete("do not call this method", true)]
		internal static IEnumerable<T> MakeGenerator<T>(Func<GeneratorNext<T>> next, int[] yieldMarkers) { return new DebugGeneratorEnumerable<T>(next, yieldMarkers); }

		[Obsolete("do not call this method", true)]
		internal static IEnumerator<T> MakeGenerator<T>(GeneratorNext<T> next, int[] yieldMarkers) { return new DebugGeneratorEnumerator<T>(next, yieldMarkers); }
	}
}
