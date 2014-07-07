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
using System.Diagnostics;
using System.Reflection;
using Microsoft.Scripting.Generation;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// �����^�C���ň�ʂɎg�p����郁�\�b�h��񋟂��܂��B
	/// ���̃N���X�ɂ͈�ʂɎg�p�����v���~�e�B�u�^�̃L���b�V�����ꂽ�{�b�N�X���\�������L�ł���悤�ɒ񋟂��郁�\�b�h���܂܂�܂��B
	/// ����� <see cref="System.Object"/> �𕁕ՓI�Ȍ^�Ƃ��Ďg�p����قƂ�ǂ̓��I����ŗL�p�ł��B
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
	public static partial class ScriptingRuntimeHelpers
	{
		const int MIN_CACHE = -100;
		const int MAX_CACHE = 1000;
		static readonly object[] cache = MakeCache();

		/// <summary>�{�b�N�X�����ꂽ�u�[���l <c>true</c> ��\���܂��B</summary>
		public static readonly object True = true;
		/// <summary>�{�b�N�X�����ꂽ�u�[���l <c>false</c> ��\���܂��B</summary>
		public static readonly object False = false;

		/// <summary><see cref="BooleanToObject"/> ���\�b�h�������܂��B</summary>
		internal static readonly MethodInfo BooleanToObjectMethod = typeof(ScriptingRuntimeHelpers).GetMethod("BooleanToObject");
		/// <summary><see cref="Int32ToObject"/> ���\�b�h�������܂��B</summary>
		internal static readonly MethodInfo Int32ToObjectMethod = typeof(ScriptingRuntimeHelpers).GetMethod("Int32ToObject");

		static object[] MakeCache()
		{
			var result = new object[MAX_CACHE - MIN_CACHE];
			for (int i = 0; i < result.Length; i++)
				result[i] = i + MIN_CACHE;
			return result;
		}

		/// <summary>�w�肳�ꂽ 32 �r�b�g�����t�������̃L���b�V�����ꂽ�{�b�N�X���\���𗘗p�ł���ꍇ�͂����Ԃ��܂��B����ȊO�̏ꍇ�͈������{�b�N�X�����܂��B</summary>
		/// <param name="value">�{�b�N�X������ 32 �r�b�g�����t���������w�肵�܂��B</param>
		/// <returns>�{�b�N�X�����ꂽ�l�B</returns>
		public static object Int32ToObject(int value)
		{
			// �L���b�V���� pystone �X�R�A�𐮐��𑽗p����A�v���P�[�V�����̏ꍇ�AMS .NET 1.1 �� 5-10% ���コ���܂��B
			// TODO: ���ꂪ�܂��p�t�H�[�}���X�ɍv�����Ă��邩�����؂��邱�ƁB.NET 3.5 ����� 4.0 �ŗL�Q�ł���Ƃ����؋�������܂��B
			if (value < MAX_CACHE && value >= MIN_CACHE)
				return cache[value - MIN_CACHE];
			return (object)value;
		}

		static readonly string[] chars = MakeSingleCharStrings();

		static string[] MakeSingleCharStrings()
		{
			string[] result = new string[255];
			for (char ch = (char)0; ch < result.Length; ch++)
				result[ch] = new string(ch, 1);
			return result;
		}

		/// <summary>�w�肳�ꂽ�u�[���l�ɑΉ�����{�b�N�X���\����Ԃ��܂��B</summary>
		/// <param name="value">�{�b�N�X������u�[���l���w�肵�܂��B</param>
		/// <returns>�{�b�N�X�����ꂽ�l�B</returns>
		public static object BooleanToObject(bool value) { return value ? True : False; }

		/// <summary>�w�肳�ꂽ������ 1 �����݂̂��܂ޕ�����ɕϊ����܂��B�L���b�V�����g�p�ł���ꍇ�̓L���b�V����Ԃ��܂��B</summary>
		/// <param name="ch">������ɕϊ����镶�����w�肵�܂��B</param>
		/// <returns>�ϊ����ꂽ������B</returns>
		public static string CharToString(char ch)
		{
			if (ch < chars.Length)
				return chars[ch];
			return new string(ch, 1);
		}

		/// <summary>�w�肳�ꂽ�v���~�e�B�u�^�̊���l��Ԃ��܂��B</summary>
		/// <param name="type">����l��Ԃ��v���~�e�B�u�^���w�肵�܂��B</param>
		/// <returns>�v���~�e�B�u�^�̊���l�B�v���~�e�B�u�^�ȊO���w�肳�ꂽ�ꍇ�� <c>null</c> ��Ԃ��܂��B</returns>
		internal static object GetPrimitiveDefaultValue(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean: return ScriptingRuntimeHelpers.False;
				case TypeCode.SByte: return default(SByte);
				case TypeCode.Byte: return default(Byte);
				case TypeCode.Char: return default(Char);
				case TypeCode.Int16: return default(Int16);
				case TypeCode.Int32: return ScriptingRuntimeHelpers.Int32ToObject(0);
				case TypeCode.Int64: return default(Int64);
				case TypeCode.UInt16: return default(UInt16);
				case TypeCode.UInt32: return default(UInt32);
				case TypeCode.UInt64: return default(UInt64);
				case TypeCode.Single: return default(Single);
				case TypeCode.Double: return default(Double);
				case TypeCode.DBNull: return default(DBNull);
				case TypeCode.DateTime: return default(DateTime);
				case TypeCode.Decimal: return default(Decimal);
				default: return null;
			}
		}

		/// <summary>�w�肳�ꂽ���b�Z�[�W���g�p���āA�V���� <see cref="ArgumentTypeException"/> ���쐬���܂��B</summary>
		/// <param name="message">��O��������郁�b�Z�[�W���w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="ArgumentTypeException"/>�B</returns>
		public static ArgumentTypeException SimpleTypeError(string message) { return new ArgumentTypeException(message); }

		/// <summary>�w�肳�ꂽ�^�ւ̕ϊ������s�������Ƃ�������O��Ԃ��܂��B</summary>
		/// <param name="toType">�ϊ���̌^���w�肵�܂��B</param>
		/// <param name="value">�ϊ������݂��l���w�肵�܂��B</param>
		/// <returns>�ϊ������s�������Ƃ����� <see cref="ArgumentTypeException"/>�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")] // TODO: fix
		public static ArgumentTypeException CannotConvertError(Type toType, object value) { return SimpleTypeError(string.Format("Cannot convert {0}({1}) to {2}", CompilerHelpers.GetType(value).Name, value, toType.Name)); }

		/// <summary>�w�肳�ꂽ������������Ȃ����Ƃ�������O��Ԃ��܂��B</summary>
		/// <param name="message">��O��������郁�b�Z�[�W���w�肵�܂��B</param>
		/// <returns>������������Ȃ����Ƃ����� <see cref="MissingMemberException"/>�B</returns>
		public static MissingMemberException SimpleAttributeError(string message) { return new MissingMemberException(message); } //TODO: localize

		/// <summary>�w�肳�ꂽ�t�B�[���h�܂��̓v���p�e�B���ǂݎ���p�ł���̂ɑ�������݂��ꍇ�ɃG���[�𔭐������܂��B</summary>
		/// <param name="field">�t�B�[���h�ւ̑���ł��邩�ǂ����������l���w�肵�܂��B</param>
		/// <param name="name">��������݂������o�̖��O���w�肵�܂��B</param>
		/// <returns>�Ȃ��B</returns>
		/// <exception cref="MissingMemberException">�t�B�[���h�܂��̓v���p�e�B�͓ǂݎ���p�ł��B</exception>
		public static object ReadOnlyAssignError(bool field, string name)
		{
			if (field)
				throw Error.FieldReadonly(name);
			else
				throw Error.PropertyReadonly(name);
		}

		/// <summary>�w�肳�ꂽ�^�̃C���X�^���X���쐬���܂��B</summary>
		/// <typeparam name="T">�C���X�^���X���쐬����^���w�肵�܂��B</typeparam>
		/// <returns>�w�肳�ꂽ�^�̃C���X�^���X�B</returns>
		public static T CreateInstance<T>() { return default(T); }

		/// <summary>�w�肳�ꂽ�^�̔z����쐬���܂��B</summary>
		/// <typeparam name="T">�z��̗v�f�̌^���w�肵�܂��B</typeparam>
		/// <param name="args">�z��̗v�f�����w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ�w�肳�ꂽ�^�̔z��B</returns>
		public static T[] CreateArray<T>(int args) { return new T[args]; }

		/// <summary><see cref="System.Runtime.CompilerServices.StrongBox&lt;T&gt;"/> ���\�����ꂽ�ɂ�������炸�ʂ̌^���󂯎�������Ƃ�������O��Ԃ��܂��B</summary>
		/// <param name="type">�\�����Ă��� <see cref="System.Runtime.CompilerServices.StrongBox&lt;T&gt;"/> �̌^�������w�肵�܂��B</param>
		/// <param name="received">���ۂɎ󂯎�����l���w�肵�܂��B</param>
		/// <returns><see cref="System.Runtime.CompilerServices.StrongBox&lt;T&gt;"/> ���\�����ꂽ�ɂ�������炸�ʂ̌^���󂯎�������Ƃ����� <see cref="ArgumentTypeException"/>�B</returns>
		public static Exception MakeIncorrectBoxTypeError(Type type, object received) { return Error.UnexpectedType("StrongBox<" + type.Name + ">", CompilerHelpers.GetType(received).Name); }

		/// <summary>�w�肳�ꂽ�^�� <see cref="SymbolId"/> �^�̐ÓI�t�B�[���h�ɂ��̃t�B�[���h�̖��O��\�� <see cref="SymbolId"/> ��ݒ肵�܂��B</summary>
		/// <param name="t"><see cref="SymbolId"/> �^�̐ÓI�t�B�[���h�𖼑O�ŏ���������^���w�肵�܂��B</param>
		public static void InitializeSymbols(Type t)
		{
			foreach (var fi in t.GetFields())
			{
				if (fi.FieldType == typeof(SymbolId))
				{
					Debug.Assert((SymbolId)fi.GetValue(null) == SymbolId.Empty);
					fi.SetValue(null, SymbolTable.StringToId(fi.Name));
				}
			}
		}
	}
}
