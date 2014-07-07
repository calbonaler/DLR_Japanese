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
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter
{
	partial class DynamicInstructionN
	{
		/// <summary>指定されたジェネリック デリゲート型の動的呼び出しサイトを使用する動的呼び出しを行う命令の型を取得します。</summary>
		/// <param name="delegateType">動的呼び出しを行う呼び出しサイトのデリゲート型を指定します。</param>
		/// <returns>動的呼び出しを行う命令の型。</returns>
		internal static Type GetDynamicInstructionType(Type delegateType)
		{
			var argTypes = delegateType.GetGenericArguments();
			if (argTypes.Length == 0)
				return null;
			Type genericType;
			var newArgTypes = ArrayUtils.RemoveFirst(argTypes);
			switch (newArgTypes.Length)
			{
				case 1: genericType = typeof(DynamicInstruction<>); break;
				case 2: genericType = typeof(DynamicInstruction<,>); break;
				case 3: genericType = typeof(DynamicInstruction<,,>); break;
				case 4: genericType = typeof(DynamicInstruction<,,,>); break;
				case 5: genericType = typeof(DynamicInstruction<,,,,>); break;
				case 6: genericType = typeof(DynamicInstruction<,,,,,>); break;
				case 7: genericType = typeof(DynamicInstruction<,,,,,,>); break;
				case 8: genericType = typeof(DynamicInstruction<,,,,,,,>); break;
				case 9: genericType = typeof(DynamicInstruction<,,,,,,,,>); break;
				case 10: genericType = typeof(DynamicInstruction<,,,,,,,,,>); break;
				case 11: genericType = typeof(DynamicInstruction<,,,,,,,,,,>); break;
				case 12: genericType = typeof(DynamicInstruction<,,,,,,,,,,,>); break;
				case 13: genericType = typeof(DynamicInstruction<,,,,,,,,,,,,>); break;
				case 14: genericType = typeof(DynamicInstruction<,,,,,,,,,,,,,>); break;
				case 15: genericType = typeof(DynamicInstruction<,,,,,,,,,,,,,,>); break;
				case 16: genericType = typeof(DynamicInstruction<,,,,,,,,,,,,,,,>); break;
				default: throw Assert.Unreachable;
			}
			return genericType.MakeGenericType(newArgTypes);
		}

		/// <summary>指定された <see cref="CallSiteBinder"/> を使用して、object 型の引数および戻り値を受け入れる動的呼び出し命令を返します。</summary>
		/// <param name="binder">動的呼び出しに使用される <see cref="CallSiteBinder"/> を指定します。</param>
		/// <param name="argCount">動的呼び出しの引数の数を指定します。</param>
		/// <returns>object 型の引数および戻り値を受け入れる動的呼び出し命令のインスタンス。</returns>
		internal static Instruction CreateUntypedInstruction(CallSiteBinder binder, int argCount)
		{
			switch (argCount)
			{
				case 0: return DynamicInstruction<object>.Factory(binder);
				case 1: return DynamicInstruction<object, object>.Factory(binder);
				case 2: return DynamicInstruction<object, object, object>.Factory(binder);
				case 3: return DynamicInstruction<object, object, object, object>.Factory(binder);
				case 4: return DynamicInstruction<object, object, object, object, object>.Factory(binder);
				case 5: return DynamicInstruction<object, object, object, object, object, object>.Factory(binder);
				case 6: return DynamicInstruction<object, object, object, object, object, object, object>.Factory(binder);
				case 7: return DynamicInstruction<object, object, object, object, object, object, object, object>.Factory(binder);
				case 8: return DynamicInstruction<object, object, object, object, object, object, object, object, object>.Factory(binder);
				case 9: return DynamicInstruction<object, object, object, object, object, object, object, object, object, object>.Factory(binder);
				case 10: return DynamicInstruction<object, object, object, object, object, object, object, object, object, object, object>.Factory(binder);
				case 11: return DynamicInstruction<object, object, object, object, object, object, object, object, object, object, object, object>.Factory(binder);
				case 12: return DynamicInstruction<object, object, object, object, object, object, object, object, object, object, object, object, object>.Factory(binder);
				case 13: return DynamicInstruction<object, object, object, object, object, object, object, object, object, object, object, object, object, object>.Factory(binder);
				case 14: return DynamicInstruction<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>.Factory(binder);
				case 15: return DynamicInstruction<object, object, object, object, object, object, object, object, object, object, object, object, object, object, object, object>.Factory(binder);
				default: return null;
			}
		}
	}

	class DynamicInstruction<TRet> : Instruction
	{
		CallSite<Func<CallSite, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 0; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Push(_site.Target(_site));
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 1; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Push(_site.Target(_site, (T0)frame.Pop()));
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 2; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 2] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 2], (T1)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 1;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, T2, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, T2, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, T2, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, T2, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 3; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 3] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 3], (T1)frame.Data[frame.StackIndex - 2], (T2)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 2;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, T2, T3, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, T2, T3, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, T2, T3, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 4; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 4] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 4], (T1)frame.Data[frame.StackIndex - 3], (T2)frame.Data[frame.StackIndex - 2], (T3)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 3;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, T2, T3, T4, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, T2, T3, T4, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, T2, T3, T4, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 5; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 5] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 5], (T1)frame.Data[frame.StackIndex - 4], (T2)frame.Data[frame.StackIndex - 3], (T3)frame.Data[frame.StackIndex - 2], (T4)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 4;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, T2, T3, T4, T5, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, T2, T3, T4, T5, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 6; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 6] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 6], (T1)frame.Data[frame.StackIndex - 5], (T2)frame.Data[frame.StackIndex - 4], (T3)frame.Data[frame.StackIndex - 3], (T4)frame.Data[frame.StackIndex - 2], (T5)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 5;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 7; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 7] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 7], (T1)frame.Data[frame.StackIndex - 6], (T2)frame.Data[frame.StackIndex - 5], (T3)frame.Data[frame.StackIndex - 4], (T4)frame.Data[frame.StackIndex - 3], (T5)frame.Data[frame.StackIndex - 2], (T6)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 6;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 8; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 8] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 8], (T1)frame.Data[frame.StackIndex - 7], (T2)frame.Data[frame.StackIndex - 6], (T3)frame.Data[frame.StackIndex - 5], (T4)frame.Data[frame.StackIndex - 4], (T5)frame.Data[frame.StackIndex - 3], (T6)frame.Data[frame.StackIndex - 2], (T7)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 7;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 9; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 9] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 9], (T1)frame.Data[frame.StackIndex - 8], (T2)frame.Data[frame.StackIndex - 7], (T3)frame.Data[frame.StackIndex - 6], (T4)frame.Data[frame.StackIndex - 5], (T5)frame.Data[frame.StackIndex - 4], (T6)frame.Data[frame.StackIndex - 3], (T7)frame.Data[frame.StackIndex - 2], (T8)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 8;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 10; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 10] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 10], (T1)frame.Data[frame.StackIndex - 9], (T2)frame.Data[frame.StackIndex - 8], (T3)frame.Data[frame.StackIndex - 7], (T4)frame.Data[frame.StackIndex - 6], (T5)frame.Data[frame.StackIndex - 5], (T6)frame.Data[frame.StackIndex - 4], (T7)frame.Data[frame.StackIndex - 3], (T8)frame.Data[frame.StackIndex - 2], (T9)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 9;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 11; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 11] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 11], (T1)frame.Data[frame.StackIndex - 10], (T2)frame.Data[frame.StackIndex - 9], (T3)frame.Data[frame.StackIndex - 8], (T4)frame.Data[frame.StackIndex - 7], (T5)frame.Data[frame.StackIndex - 6], (T6)frame.Data[frame.StackIndex - 5], (T7)frame.Data[frame.StackIndex - 4], (T8)frame.Data[frame.StackIndex - 3], (T9)frame.Data[frame.StackIndex - 2], (T10)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 10;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 12; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 12] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 12], (T1)frame.Data[frame.StackIndex - 11], (T2)frame.Data[frame.StackIndex - 10], (T3)frame.Data[frame.StackIndex - 9], (T4)frame.Data[frame.StackIndex - 8], (T5)frame.Data[frame.StackIndex - 7], (T6)frame.Data[frame.StackIndex - 6], (T7)frame.Data[frame.StackIndex - 5], (T8)frame.Data[frame.StackIndex - 4], (T9)frame.Data[frame.StackIndex - 3], (T10)frame.Data[frame.StackIndex - 2], (T11)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 11;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 13; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 13] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 13], (T1)frame.Data[frame.StackIndex - 12], (T2)frame.Data[frame.StackIndex - 11], (T3)frame.Data[frame.StackIndex - 10], (T4)frame.Data[frame.StackIndex - 9], (T5)frame.Data[frame.StackIndex - 8], (T6)frame.Data[frame.StackIndex - 7], (T7)frame.Data[frame.StackIndex - 6], (T8)frame.Data[frame.StackIndex - 5], (T9)frame.Data[frame.StackIndex - 4], (T10)frame.Data[frame.StackIndex - 3], (T11)frame.Data[frame.StackIndex - 2], (T12)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 12;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 14; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 14] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 14], (T1)frame.Data[frame.StackIndex - 13], (T2)frame.Data[frame.StackIndex - 12], (T3)frame.Data[frame.StackIndex - 11], (T4)frame.Data[frame.StackIndex - 10], (T5)frame.Data[frame.StackIndex - 9], (T6)frame.Data[frame.StackIndex - 8], (T7)frame.Data[frame.StackIndex - 7], (T8)frame.Data[frame.StackIndex - 6], (T9)frame.Data[frame.StackIndex - 5], (T10)frame.Data[frame.StackIndex - 4], (T11)frame.Data[frame.StackIndex - 3], (T12)frame.Data[frame.StackIndex - 2], (T13)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 13;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}

	class DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TRet> : Instruction
	{
		CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TRet>> _site;

		public static Instruction Factory(CallSiteBinder binder) { return new DynamicInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TRet>(binder); }

		DynamicInstruction(CallSiteBinder binder) { _site = CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TRet>>.Create(binder); }

		public override int ProducedStack { get { return 1; } }

		public override int ConsumedStack { get { return 15; } }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 15] = _site.Target(_site, (T0)frame.Data[frame.StackIndex - 15], (T1)frame.Data[frame.StackIndex - 14], (T2)frame.Data[frame.StackIndex - 13], (T3)frame.Data[frame.StackIndex - 12], (T4)frame.Data[frame.StackIndex - 11], (T5)frame.Data[frame.StackIndex - 10], (T6)frame.Data[frame.StackIndex - 9], (T7)frame.Data[frame.StackIndex - 8], (T8)frame.Data[frame.StackIndex - 7], (T9)frame.Data[frame.StackIndex - 6], (T10)frame.Data[frame.StackIndex - 5], (T11)frame.Data[frame.StackIndex - 4], (T12)frame.Data[frame.StackIndex - 3], (T13)frame.Data[frame.StackIndex - 2], (T14)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 14;
			return 1;
		}

		public override string ToString() { return "Dynamic(" + _site.Binder.ToString() + ")"; }
	}
}
