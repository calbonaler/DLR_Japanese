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
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>����� DLR �o�C���_�[����Ăяo�����������ꂽ�w���p�[���\�b�h���i�[���܂��B</summary>
	public static class BinderOps
	{
		/// <summary>�w�肳�ꂽ���O�ƒl�̔z��̓����ʒu�̗v�f���� <see cref="SymbolDictionary"/> ���쐬���܂��B</summary>
		/// <param name="names"><see cref="SymbolDictionary"/> �Ɋi�[���閼�O���܂�ł���z����w�肵�܂��B</param>
		/// <param name="values"><see cref="SymbolDictionary"/> �Ɋi�[����l���܂�ł���z����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���O�ƒl�̔z��̓����ʒu�̗v�f����쐬���ꂽ <see cref="SymbolDictionary"/>�B</returns>
		public static SymbolDictionary MakeSymbolDictionary(string[] names, object[] values)
		{
			SymbolDictionary res = new SymbolDictionary();
			for (int i = 0; i < names.Length; i++)
				res[names[i]] = values[i];
			return res;
		}

		/// <summary>�w�肳�ꂽ���O�ƒl�̔z��̓����ʒu�̗v�f���� <see cref="Dictionary&lt;TKey, TValue&gt;"/> ���쐬���܂��B</summary>
		/// <typeparam name="TKey">�쐬����f�B�N�V���i���̃L�[�̌^���w�肵�܂��B<see cref="String"/> �܂��� <see cref="Object"/> �ł���K�v������܂��B</typeparam>
		/// <typeparam name="TValue">�쐬����f�B�N�V���i���̒l�̌^���w�肵�܂��B</typeparam>
		/// <param name="names"><see cref="Dictionary&lt;TKey, TValue&gt;"/> �Ɋi�[���閼�O���܂�ł���z����w�肵�܂��B</param>
		/// <param name="values"><see cref="Dictionary&lt;TKey, TValue&gt;"/> �Ɋi�[����l���܂�ł���z����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���O�ƒl�̔z��̓����ʒu�̗v�f����쐬���ꂽ <see cref="Dictionary&lt;TKey, TValue&gt;"/>�B</returns>
		public static Dictionary<TKey, TValue> MakeDictionary<TKey, TValue>(string[] names, object[] values)
		{
			Debug.Assert(typeof(TKey) == typeof(string) || typeof(TKey) == typeof(object));
			return names.Zip(values, (x, y) => new KeyValuePair<TKey, TValue>((TKey)(object)x, (TValue)y)).ToDictionary(x => x.Key, x => x.Value);
		}

		/// <summary>�w�肳�ꂽ���삪�I�y�����h�̌^���s���ł��邽�߂Ɏ��s�������Ƃ�������O��Ԃ��܂��B</summary>
		/// <param name="op">���s����������w�肵�܂��B</param>
		/// <param name="args">����̈������w�肵�܂��B</param>
		/// <returns>���삪�I�y�����h�̌^���s���ł��邽�߂Ɏ��s�������Ƃ����� <see cref="ArgumentTypeException"/>�B</returns>
		public static ArgumentTypeException BadArgumentsForOperation(ExpressionType op, params object[] args) { throw new ArgumentTypeException("���Z " + op.ToString() + " �ŃT�|�[�g����Ă��Ȃ��I�y�����h�^: " + string.Join(", ", args.Select(x => CompilerHelpers.GetType(x)))); }

		/// <summary>�����̐����������Ȃ����߂Ɏ��s�������Ƃ�������O��Ԃ��܂��B</summary>
		/// <param name="methodName">�s���Ȑ��̈������n���ꂽ���\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="formalNormalArgumentCount">�z���������уL�[���[�h�������܂܂Ȃ������̐����w�肵�܂��B</param>
		/// <param name="defaultArgumentCount">���\�b�h�錾�̒��Ŋ���l�̂�������̐����w�肵�܂��B</param>
		/// <param name="providedArgumentCount">�Ăяo���T�C�g�œn���ꂽ�����̐����w�肵�܂��B</param>
		/// <param name="hasArgList">���\�b�h�錾�ɔz��������܂ނ��ǂ����������l���w�肵�܂��B</param>
		/// <param name="keywordArgumentsProvided">�Ăяo���T�C�g�ŃL�[���[�h�������n���ꂽ���ǂ����������l���w�肵�܂��B</param>
		/// <returns>�����̐����������Ȃ����߂Ɏ��s�������Ƃ����� <see cref="ArgumentTypeException"/>�B</returns>
		public static ArgumentTypeException TypeErrorForIncorrectArgumentCount(string methodName, int formalNormalArgumentCount, int defaultArgumentCount, int providedArgumentCount, bool hasArgList, bool keywordArgumentsProvided) { return TypeErrorForIncorrectArgumentCount(methodName, formalNormalArgumentCount, formalNormalArgumentCount, defaultArgumentCount, providedArgumentCount, hasArgList, keywordArgumentsProvided); }

		/// <summary>�����̐����������Ȃ����߂Ɏ��s�������Ƃ�������O��Ԃ��܂��B</summary>
		/// <param name="methodName">�s���Ȑ��̈������n���ꂽ���\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="minFormalNormalArgumentCount">�z���������уL�[���[�h�������܂܂Ȃ����̃��\�b�h�����e��������̐��̍ŏ��l���w�肵�܂��B</param>
		/// <param name="maxFormalNormalArgumentCount">�z���������уL�[���[�h�������܂܂Ȃ����̃��\�b�h�����e��������̐��̍ő�l���w�肵�܂��B</param>
		/// <param name="defaultArgumentCount">���\�b�h�錾�̒��Ŋ���l�̂�������̐����w�肵�܂��B</param>
		/// <param name="providedArgumentCount">�Ăяo���T�C�g�œn���ꂽ�����̐����w�肵�܂��B</param>
		/// <param name="hasArgList">���\�b�h�錾�ɔz��������܂ނ��ǂ����������l���w�肵�܂��B</param>
		/// <param name="keywordArgumentsProvided">�Ăяo���T�C�g�ŃL�[���[�h�������n���ꂽ���ǂ����������l���w�肵�܂��B</param>
		/// <returns>�����̐����������Ȃ����߂Ɏ��s�������Ƃ����� <see cref="ArgumentTypeException"/>�B</returns>
		public static ArgumentTypeException TypeErrorForIncorrectArgumentCount(string methodName, int minFormalNormalArgumentCount, int maxFormalNormalArgumentCount, int defaultArgumentCount, int providedArgumentCount, bool hasArgList, bool keywordArgumentsProvided)
		{
			int formalCount;
			string formalCountQualifier;
			var nonKeyword = keywordArgumentsProvided ? "��L�[���[�h " : "";
			if (defaultArgumentCount > 0 || hasArgList || minFormalNormalArgumentCount != maxFormalNormalArgumentCount)
			{
				if (providedArgumentCount < minFormalNormalArgumentCount || maxFormalNormalArgumentCount == int.MaxValue)
				{
					formalCountQualifier = "�ŏ�";
					formalCount = minFormalNormalArgumentCount - defaultArgumentCount;
				}
				else
				{
					formalCountQualifier = "�ő�";
					formalCount = maxFormalNormalArgumentCount;
				}
			}
			else if (minFormalNormalArgumentCount == 0)
				return ScriptingRuntimeHelpers.SimpleTypeError(string.Format("{0}() �͈������Ƃ�܂��� ({1} ���w�肳��܂���)", methodName, providedArgumentCount));
			else
			{
				formalCountQualifier = "";
				formalCount = minFormalNormalArgumentCount;
			}
			return new ArgumentTypeException(string.Format("{0}() ��{1} {2} ��{3}�������Ƃ�܂� ({4} ���w�肳��܂���)", methodName, formalCountQualifier, formalCount, nonKeyword, providedArgumentCount));
		}

		/// <summary>�����̐����������Ȃ����߂Ɏ��s�������Ƃ�������O��Ԃ��܂��B</summary>
		/// <param name="methodName">�s���Ȑ��̈������n���ꂽ���\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="formalNormalArgumentCount">�z���������уL�[���[�h�������܂܂Ȃ������̐����w�肵�܂��B</param>
		/// <param name="defaultArgumentCount">���\�b�h�錾�̒��Ŋ���l�̂�������̐����w�肵�܂��B</param>
		/// <param name="providedArgumentCount">�Ăяo���T�C�g�œn���ꂽ�����̐����w�肵�܂��B</param>
		/// <returns>�����̐����������Ȃ����߂Ɏ��s�������Ƃ����� <see cref="ArgumentTypeException"/>�B</returns>
		public static ArgumentTypeException TypeErrorForIncorrectArgumentCount(string methodName, int formalNormalArgumentCount, int defaultArgumentCount, int providedArgumentCount) { return TypeErrorForIncorrectArgumentCount(methodName, formalNormalArgumentCount, defaultArgumentCount, providedArgumentCount, false, false); }

		/// <summary>�����̐����������Ȃ����߂Ɏ��s�������Ƃ�������O��Ԃ��܂��B</summary>
		/// <param name="methodName">�s���Ȑ��̈������n���ꂽ���\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="expectedArgumentCount">���̃��\�b�h�ŗ\������Ă�������̐����w�肵�܂��B</param>
		/// <param name="providedArgumentCount">�Ăяo���T�C�g�œn���ꂽ�����̐����w�肵�܂��B</param>
		/// <returns>�����̐����������Ȃ����߂Ɏ��s�������Ƃ����� <see cref="ArgumentTypeException"/>�B</returns>
		public static ArgumentTypeException TypeErrorForIncorrectArgumentCount(string methodName, int expectedArgumentCount, int providedArgumentCount) { return TypeErrorForIncorrectArgumentCount(methodName, expectedArgumentCount, 0, providedArgumentCount); }

		/// <summary>�\�����Ȃ��L�[���[�h�������n���ꂽ���߂Ɏ��s�������Ƃ�������O��Ԃ��܂��B</summary>
		/// <param name="methodName">�\�����Ȃ��L�[���[�h�������n���ꂽ���\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="argumentName">�n���ꂽ�L�[���[�h�����̖��O���w�肵�܂��B</param>
		/// <returns>�\�����Ȃ��L�[���[�h�������n���ꂽ���߂Ɏ��s�������Ƃ����� <see cref="ArgumentTypeException"/>�B</returns>
		public static ArgumentTypeException TypeErrorForExtraKeywordArgument(string methodName, string argumentName) { return new ArgumentTypeException(string.Format("{0}() �ɂ͗\�����Ȃ��L�[���[�h���� '{1}' ���w�肳��܂����B", methodName, argumentName)); }

		/// <summary>�d�������L�[���[�h�������n���ꂽ���߂Ɏ��s�������Ƃ�������O��Ԃ��܂��B</summary>
		/// <param name="methodName">�d�������L�[���[�h�������n���ꂽ���\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="argumentName">�d���̂���L�[���[�h�����̖��O���w�肵�܂��B</param>
		/// <returns>�d�������L�[���[�h�������n���ꂽ���߂Ɏ��s�������Ƃ����� <see cref="ArgumentTypeException"/>�B</returns>
		public static ArgumentTypeException TypeErrorForDuplicateKeywordArgument(string methodName, string argumentName) { return new ArgumentTypeException(string.Format("{0}() �ɂ̓L�[���[�h���� '{1}' �ɕ����̒l���w�肳��܂����B", methodName, argumentName)); }

		/// <summary>���\�b�h�̌^�����𐄘_�ł��Ȃ����ߎ��s�������Ƃ�������O��Ԃ��܂��B</summary>
		/// <param name="methodName">���_�ł��Ȃ��^�����������\�b�h�̖��O���w�肵�܂��B</param>
		/// <returns>���\�b�h�̌^�����𐄘_�ł��Ȃ����ߎ��s�������Ƃ����� <see cref="ArgumentTypeException"/>�B</returns>
		public static ArgumentTypeException TypeErrorForNonInferrableMethod(string methodName) { return new ArgumentTypeException(string.Format("���\�b�h '{0}' �ɑ΂���^�������g�p�@���琄�_�ł��܂���B�����I�Ȍ^�����̎w������݂Ă��������B", methodName)); }

		/// <summary>�w�肳�ꂽ���b�Z�[�W���g�p���āA�V���� <see cref="ArgumentTypeException"/> ���쐬���܂��B</summary>
		/// <param name="message">���b�Z�[�W���w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="ArgumentTypeException"/>�B</returns>
		public static ArgumentTypeException SimpleTypeError(string message) { return new ArgumentTypeException(message); }

		/// <summary>�U�J���������ɓn�����������̌^���V�[�P���X�łȂ����ߎ��s�������Ƃ�������O��Ԃ��܂��B</summary>
		/// <param name="methodName">�V�[�P���X�ȊO�̌^�̃I�u�W�F�N�g���U�J�����ɓn���ꂽ���\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="typeName">�U�J�����ɓn���ꂽ�I�u�W�F�N�g�̌^�̖��O���w�肵�܂��B</param>
		/// <returns>�U�J���������ɓn�����������̌^���V�[�P���X�łȂ����ߎ��s�������Ƃ����� <see cref="ArgumentTypeException"/>�B</returns>
		public static ArgumentTypeException InvalidSplatteeError(string methodName, string typeName) { return new ArgumentTypeException(string.Format("* �ȍ~�� {0}() �̈����̓V�[�P���X�ł���K�v������܂����A{1} ���w�肳��܂����B", methodName, typeName)); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂��郁�\�b�h�����t���N�V�������g�p���ČĂяo���܂��B</summary>
		/// <param name="mb">�Ăяo�����\�b�h���w�肵�܂��B</param>
		/// <param name="obj">���\�b�h���Ăяo���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="args">���\�b�h�ɓn�����������w�肵�܂��B</param>
		/// <returns>���\�b�h�̖߂�l�B</returns>
		public static object InvokeMethod(MethodBase mb, object obj, object[] args)
		{
			try
			{
				return mb.Invoke(obj, args);
			}
			catch (TargetInvocationException tie)
			{
				throw tie.InnerException;
			}
		}

		/// <summary>�w�肳�ꂽ�R���X�g���N�^�����t���N�V�������g�p���ČĂяo���܂��B</summary>
		/// <param name="ci">�Ăяo���R���X�g���N�^���w�肵�܂��B</param>
		/// <param name="args">�R���X�g���N�^�ɓn�����������w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�R���X�g���N�^�ɂ���č쐬���ꂽ�I�u�W�F�N�g�B</returns>
		public static object InvokeConstructor(ConstructorInfo ci, object[] args)
		{
			try
			{
				return ci.Invoke(args);
			}
			catch (TargetInvocationException tie)
			{
				throw tie.InnerException;
			}
		}

		// TODO: just emit this in the generated code
		/// <summary>�w�肳�ꂽ�f�B�N�V���i���Ɏw�肳�ꂽ���O�����݂��āA���O�ɑ΂���l���w�肳�ꂽ�^�ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="dict">���ׂ�f�B�N�V���i�����w�肵�܂��B</param>
		/// <param name="names">�f�B�N�V���i���Ɋ܂܂�Ă��鍀�ڂ̖��O���i�[���ꂽ�z����w�肵�܂��B</param>
		/// <param name="types">�f�B�N�V���i���� <paramref name="names"/> �z��ɑΉ�����v�f�̌^���i�[���ꂽ�z����w�肵�܂��B���̈����͏ȗ��\�ł��B</param>
		/// <returns>�w�肳�ꂽ�f�B�N�V���i���Ɏw�肳�ꂽ���O�����݂��āA���O�ɑ΂���l���w�肳�ꂽ�^�ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool CheckDictionaryMembers(IDictionary dict, string[] names, Type[] types) { return dict.Count == names.Length && names.Select((x, i) => dict.Contains(x) && (types == null || CompilerHelpers.GetType(dict[x]) == types[i])).All(x => x); }

		/// <summary>�w�肳�ꂽ <see cref="EventTracker"/> �Ɏw�肳�ꂽ�l���֘A�t�����Ă��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="eventTracker">�l���֘A�t�����Ă��� <see cref="EventTracker"/> ���w�肵�܂��B</param>
		/// <param name="value">�֘A�t�����Ă���l���w�肵�܂��B</param>
		/// <exception cref="ArgumentException">�C�x���g���w�肳�ꂽ�l�Ɋ֘A�t�����Ă��܂���B</exception>
		/// <exception cref="ArgumentTypeException">�֘A�t�����Ă���l�����҂��ꂽ�^�ł͂���܂���B</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")] // TODO: fix
		public static void SetEvent(EventTracker eventTracker, object value)
		{
			var et = value as EventTracker;
			if (et != null)
			{
				if (et != eventTracker)
					throw new ArgumentException(string.Format("{0}.{1} ����̃C�x���g���\������܂������A{2}.{3} ����̃C�x���g���w�肳��܂����B", eventTracker.DeclaringType.Name, eventTracker.Name, et.DeclaringType.Name, et.Name));
				return;
			}
			var bmt = value as BoundMemberTracker;
			if (bmt == null)
				throw new ArgumentTypeException(string.Format("�������ꂽ�C�x���g���\������܂������A{0} ���w�肳��܂����B", CompilerHelpers.GetType(value).Name));
			if (bmt.BoundTo.MemberType != TrackerTypes.Event)
				throw new ArgumentTypeException(string.Format("�������ꂽ�C�x���g���\������܂������A{0} ���w�肳��܂����B", bmt.BoundTo.MemberType.ToString()));
			if (bmt.BoundTo != eventTracker)
				throw new ArgumentException(string.Format("{0}.{1} ����̃C�x���g���\������܂������A{2}.{3} ����̃C�x���g���w�肳��܂����B", eventTracker.DeclaringType.Name, eventTracker.Name, bmt.BoundTo.DeclaringType.Name, bmt.BoundTo.Name));
		}
	}
}
