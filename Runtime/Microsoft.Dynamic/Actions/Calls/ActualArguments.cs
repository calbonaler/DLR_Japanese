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

using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>���\�b�h�ɓn�����������̃Z�b�g��\���܂��B</summary>
	public sealed class ActualArguments
	{
		// Index into _args array indicating the first post-splat argument or -1 of there are no splatted arguments.
		// For call site f(a,b,*c,d) and preSplatLimit == 1 and postSplatLimit == 2
		// args would be (a,b,c[0],c[n-2],c[n-1],d) with splat index 3, where n = c.Count.
		/// <summary>�������Ɋւ�������g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.ActualArguments"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="args">���������w�肵�܂��B</param>
		/// <param name="namedArgs">���O�t�����������w�肵�܂��B</param>
		/// <param name="argNames">���O�t���������̖��O���w�肵�܂��B</param>
		/// <param name="hiddenCount">�G���[�񍐂Ɏg�p�����B���ꂽ�������̐����w�肵�܂��B</param>
		/// <param name="collapsedCount">�܂肽���܂ꂽ�������̐����w�肵�܂��B</param>
		/// <param name="firstSplattedArg">�W�J���ꂽ�������̐擪�̈������X�g���ł̈ʒu���w�肵�܂��B</param>
		/// <param name="splatIndex">�ȗ����ꂽ�W�J���ꂽ�������̐擪�̈������X�g���ł̈ʒu���w�肵�܂��B</param>
		public ActualArguments(IList<DynamicMetaObject> args, IList<DynamicMetaObject> namedArgs, IList<string> argNames, int hiddenCount, int collapsedCount, int firstSplattedArg, int splatIndex)
		{
			ContractUtils.RequiresNotNullItems(args, "args");
			ContractUtils.RequiresNotNullItems(namedArgs, "namedArgs");
			ContractUtils.RequiresNotNullItems(argNames, "argNames");
			ContractUtils.Requires(namedArgs.Count == argNames.Count);

			ContractUtils.Requires(splatIndex == -1 || firstSplattedArg == -1 || firstSplattedArg >= 0 && firstSplattedArg <= splatIndex);
			ContractUtils.Requires(splatIndex == -1 || splatIndex >= 0);
			ContractUtils.Requires(collapsedCount >= 0);
			ContractUtils.Requires(hiddenCount >= 0);

			Arguments = args;
			NamedArguments = namedArgs;
			ArgNames = argNames;
			CollapsedCount = collapsedCount;
			SplatIndex = collapsedCount > 0 ? splatIndex : -1;
			FirstSplattedArg = firstSplattedArg;
			HiddenCount = hiddenCount;
		}

		/// <summary>�܂肽���܂ꂽ�����̐����擾���܂��B</summary>
		public int CollapsedCount { get; private set; }

		/// <summary>�ȗ����ꂽ�W�J���ꂽ�������̐擪�̈������X�g���ł̈ʒu���擾���܂��B</summary>
		public int SplatIndex { get; private set; }

		/// <summary>�W�J���ꂽ�������̐擪�̈������X�g���ł̈ʒu���擾���܂��B</summary>
		public int FirstSplattedArg { get; private set; }

		/// <summary>���O�t���������̖��O���擾���܂��B</summary>
		public IList<string> ArgNames { get; private set; }

		/// <summary>���O�t�����������擾���܂��B</summary>
		public IList<DynamicMetaObject> NamedArguments { get; private set; }

		/// <summary>���������擾���܂��B</summary>
		public IList<DynamicMetaObject> Arguments { get; private set; }

		internal int ToSplattedItemIndex(int collapsedArgIndex) { return SplatIndex - FirstSplattedArg + collapsedArgIndex; }

		/// <summary>�܂肽���܂ꂽ���������܂܂Ȃ��������̐����擾���܂��B</summary>
		public int Count { get { return Arguments.Count + NamedArguments.Count; } }

		/// <summary>�G���[�񍐂Ɏg�p�����B���ꂽ�������̐����擾���܂��B</summary>
		public int HiddenCount { get; private set; }

		/// <summary>�R�[���T�C�g�ɓn���ꂽ�܂肽���܂ꂽ���������܂މ��ł���������̑������擾���܂��B</summary>
		public int VisibleCount { get { return Count + CollapsedCount - HiddenCount; } }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̎��������擾���܂��B</summary>
		/// <param name="index">�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�̎������B</returns>
		public DynamicMetaObject this[int index] { get { return index < Arguments.Count ? Arguments[index] : NamedArguments[index - Arguments.Count]; } }

		/// <summary>
		/// ���O�t�����������������Ɋ֘A�t���A���O�t���������ƑΉ����鉼�����̊Ԃ̊֌W�������C���f�b�N�X�̒u����Ԃ��܂��B
		/// ���̃��\�b�h�͏d������ъ֘A�t�����Ă��Ȃ����O�t���������m�F���܂��B
		/// </summary>
		/// <param name="method">�֘A�t���鉼�����������\�b�h���w�肵�܂��B</param>
		/// <param name="binding">�֘A�t���̌��ʂƂ��ē�����u�����i�[����ϐ����w�肵�܂��B</param>
		/// <param name="failure">�֘A�t�������s�����ۂ� <see cref="CallFailure"/> �I�u�W�F�N�g���i�[�����ϐ����w�肵�܂��B</param>
		/// <returns>�֘A�t�������������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		/// <remarks>���ׂĂ� i �ɑ΂��āAnamedArgs[i] �� parameters[args.Length + bindingPermutation[i]] �Ɋ֘A�t�����Ă��邱�Ƃ�ۏ؂��܂��B</remarks>
		internal bool TryBindNamedArguments(MethodCandidate method, out ArgumentBinding binding, out CallFailure failure)
		{
			if (NamedArguments.Count == 0)
			{
				binding = new ArgumentBinding(Arguments.Count);
				failure = null;
				return true;
			}
			var permutation = new int[NamedArguments.Count];
			var boundParameters = new BitArray(NamedArguments.Count);
			for (int i = 0; i < permutation.Length; i++)
				permutation[i] = -1;
			List<string> unboundNames = null;
			List<string> duppedNames = null;
			int positionalArgCount = Arguments.Count;
			for (int i = 0; i < ArgNames.Count; i++)
			{
				int paramIndex = method.IndexOfParameter(ArgNames[i]);
				if (paramIndex >= 0)
				{
					int nameIndex = paramIndex - positionalArgCount;
					// argument maps to already bound parameter:
					if (paramIndex < positionalArgCount || boundParameters[nameIndex])
					{
						if (duppedNames == null)
							duppedNames = new List<string>();
						duppedNames.Add(ArgNames[i]);
					}
					else
					{
						permutation[i] = nameIndex;
						boundParameters[nameIndex] = true;
					}
				}
				else
				{
					if (unboundNames == null)
						unboundNames = new List<string>();
					unboundNames.Add(ArgNames[i]);
				}
			}
			binding = new ArgumentBinding(positionalArgCount, permutation);
			if (unboundNames != null)
			{
				failure = new CallFailure(method, unboundNames.ToArray(), true);
				return false;
			}
			if (duppedNames != null)
			{
				failure = new CallFailure(method, duppedNames.ToArray(), false);
				return false;
			}
			failure = null;
			return true;
		}
	}
}
