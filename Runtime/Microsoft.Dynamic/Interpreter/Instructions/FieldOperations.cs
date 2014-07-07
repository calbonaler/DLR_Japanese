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

using System.Diagnostics;
using System.Reflection;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>静的フィールドの値をスタックに読み込む命令を表します。</summary>
	sealed class LoadStaticFieldInstruction : Instruction
	{
		readonly FieldInfo _field;

		/// <summary>値を読み込む静的フィールドを使用して、<see cref="Microsoft.Scripting.Interpreter.LoadStaticFieldInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="field">値を読み込む静的フィールドを表す <see cref="FieldInfo"/> を指定します。</param>
		public LoadStaticFieldInstruction(FieldInfo field)
		{
			Debug.Assert(field.IsStatic);
			_field = field;
		}

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(_field.GetValue(null));
			return +1;
		}
	}

	/// <summary>インスタンスフィールドの値をスタックに読み込む命令を表します。</summary>
	sealed class LoadFieldInstruction : Instruction
	{
		readonly FieldInfo _field;

		/// <summary>値を読み込むインスタンスフィールドを使用して、<see cref="Microsoft.Scripting.Interpreter.LoadFieldInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="field">値を読み込むインスタンスフィールドを表す <see cref="FieldInfo"/> を指定します。</param>
		public LoadFieldInstruction(FieldInfo field)
		{
			Assert.NotNull(field);
			_field = field;
		}

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(_field.GetValue(frame.Pop()));
			return +1;
		}
	}

	/// <summary>インスタンスフィールドの値を設定する命令を表します。</summary>
	sealed class StoreFieldInstruction : Instruction
	{
		readonly FieldInfo _field;

		/// <summary>値を設定するインスタンスフィールドを使用して、<see cref="Microsoft.Scripting.Interpreter.StoreFieldInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="field">値を設定するインスタンスフィールドを表す <see cref="FieldInfo"/> を指定します。</param>
		public StoreFieldInstruction(FieldInfo field)
		{
			Assert.NotNull(field);
			_field = field;
		}

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 2; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 0; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			var value = frame.Pop();
			var self = frame.Pop();
			_field.SetValue(self, value);
			return +1;
		}
	}

	/// <summary>静的フィールドの値を設定する命令を表します。</summary>
	sealed class StoreStaticFieldInstruction : Instruction
	{
		readonly FieldInfo _field;

		/// <summary>値を設定する静的フィールドを使用して、<see cref="Microsoft.Scripting.Interpreter.StoreStaticFieldInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="field">値を設定する静的フィールドを表す <see cref="FieldInfo"/> を指定します。</param>
		public StoreStaticFieldInstruction(FieldInfo field)
		{
			Assert.NotNull(field);
			_field = field;
		}

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 0; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			_field.SetValue(null, frame.Pop());
			return +1;
		}
	}
}
