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

namespace Microsoft.Scripting.Utils
{
	/// <summary>�񋓑̃����o���m�̉��Z�Ɋւ��郁�\�b�h�����J���܂��B</summary>
	public static class EnumUtils
	{
		/// <summary>�w�肳�ꂽ 2 �̗񋓑̃����o�ɑ΂��ăr�b�g���Ƃ� OR ���Z�����s�����ʂ� 1 �Ԗڂ̃I�y�����h�̌^�ɕϊ����܂��B</summary>
		/// <param name="self">�r�b�g���Ƃ� OR ���Z�����s���� 1 �Ԗڂ̃I�y�����h���w�肵�܂��B</param>
		/// <param name="other">�r�b�g���Ƃ� OR ���Z�����s���� 2 �Ԗڂ̃I�y�����h���w�肵�܂��B</param>
		/// <returns>2 �̗񋓑̃����o�̃r�b�g���Ƃ� OR ���Z�̌��ʂ� 1 �Ԗڂ̃I�y�����h�̌^�ɕϊ������l�B</returns>
		public static object BitwiseOr(object self, object other)
		{
			if (self is Enum && other is Enum)
			{
				var selfType = self.GetType();
				var otherType = other.GetType();
				if (selfType == otherType)
				{
					var underType = Enum.GetUnderlyingType(selfType);
					if (underType == typeof(int))
						return Enum.ToObject(selfType, (int)self | (int)other);
					else if (underType == typeof(long))
						return Enum.ToObject(selfType, (long)self | (long)other);
					else if (underType == typeof(short))
						return Enum.ToObject(selfType, (short)self | (short)other);
					else if (underType == typeof(byte))
						return Enum.ToObject(selfType, (byte)self | (byte)other);
					else if (underType == typeof(sbyte))
						return Enum.ToObject(selfType, (sbyte)self | (sbyte)other);
					else if (underType == typeof(uint))
						return Enum.ToObject(selfType, (uint)self | (uint)other);
					else if (underType == typeof(ulong))
						return Enum.ToObject(selfType, (ulong)self | (ulong)other);
					else if (underType == typeof(ushort))
						return Enum.ToObject(selfType, (ushort)self | (ushort)other);
					else
						throw Assert.Unreachable;
				}
			}
			return null;
		}

		/// <summary>�w�肳�ꂽ 2 �̗񋓑̃����o�ɑ΂��ăr�b�g���Ƃ� AND ���Z�����s�����ʂ� 1 �Ԗڂ̃I�y�����h�̌^�ɕϊ����܂��B</summary>
		/// <param name="self">�r�b�g���Ƃ� AND ���Z�����s���� 1 �Ԗڂ̃I�y�����h���w�肵�܂��B</param>
		/// <param name="other">�r�b�g���Ƃ� AND ���Z�����s���� 2 �Ԗڂ̃I�y�����h���w�肵�܂��B</param>
		/// <returns>2 �̗񋓑̃����o�̃r�b�g���Ƃ� AND ���Z�̌��ʂ� 1 �Ԗڂ̃I�y�����h�̌^�ɕϊ������l�B</returns>
		public static object BitwiseAnd(object self, object other)
		{
			if (self is Enum && other is Enum)
			{
				var selfType = self.GetType();
				var otherType = other.GetType();
				if (selfType == otherType)
				{
					var underType = Enum.GetUnderlyingType(selfType);
					if (underType == typeof(int))
						return Enum.ToObject(selfType, (int)self & (int)other);
					else if (underType == typeof(long))
						return Enum.ToObject(selfType, (long)self & (long)other);
					else if (underType == typeof(short))
						return Enum.ToObject(selfType, (short)self & (short)other);
					else if (underType == typeof(byte))
						return Enum.ToObject(selfType, (byte)self & (byte)other);
					else if (underType == typeof(sbyte))
						return Enum.ToObject(selfType, (sbyte)self & (sbyte)other);
					else if (underType == typeof(uint))
						return Enum.ToObject(selfType, (uint)self & (uint)other);
					else if (underType == typeof(ulong))
						return Enum.ToObject(selfType, (ulong)self & (ulong)other);
					else if (underType == typeof(ushort))
						return Enum.ToObject(selfType, (ushort)self & (ushort)other);
					else
						throw Assert.Unreachable;
				}
			}
			return null;
		}

		/// <summary>�w�肳�ꂽ 2 �̗񋓑̃����o�ɑ΂��ăr�b�g���Ƃ� XOR ���Z�����s�����ʂ� 1 �Ԗڂ̃I�y�����h�̌^�ɕϊ����܂��B</summary>
		/// <param name="self">�r�b�g���Ƃ� XOR ���Z�����s���� 1 �Ԗڂ̃I�y�����h���w�肵�܂��B</param>
		/// <param name="other">�r�b�g���Ƃ� XOR ���Z�����s���� 2 �Ԗڂ̃I�y�����h���w�肵�܂��B</param>
		/// <returns>2 �̗񋓑̃����o�̃r�b�g���Ƃ� XOR ���Z�̌��ʂ� 1 �Ԗڂ̃I�y�����h�̌^�ɕϊ������l�B</returns>
		public static object ExclusiveOr(object self, object other)
		{
			if (self is Enum && other is Enum)
			{
				var selfType = self.GetType();
				var otherType = other.GetType();
				if (selfType == otherType)
				{
					var underType = Enum.GetUnderlyingType(selfType);
					if (underType == typeof(int))
						return Enum.ToObject(selfType, (int)self ^ (int)other);
					else if (underType == typeof(long))
						return Enum.ToObject(selfType, (long)self ^ (long)other);
					else if (underType == typeof(short))
						return Enum.ToObject(selfType, (short)self ^ (short)other);
					else if (underType == typeof(byte))
						return Enum.ToObject(selfType, (byte)self ^ (byte)other);
					else if (underType == typeof(sbyte))
						return Enum.ToObject(selfType, (sbyte)self ^ (sbyte)other);
					else if (underType == typeof(uint))
						return Enum.ToObject(selfType, (uint)self ^ (uint)other);
					else if (underType == typeof(ulong))
						return Enum.ToObject(selfType, (ulong)self ^ (ulong)other);
					else if (underType == typeof(ushort))
						return Enum.ToObject(selfType, (ushort)self ^ (ushort)other);
					else
						throw Assert.Unreachable;
				}
			}
			return null;
		}

		/// <summary>�w�肳�ꂽ�񋓑̃����o�ɑ΂��� 1 �̕␔�����ߌ��ʂ����̗񋓌^�ɕϊ����܂��B</summary>
		/// <param name="self">1 �̕␔�����߂�񋓑̃����o���w�肵�܂��B</param>
		/// <returns>1 �̕␔�̒l��񋓌^�ɕϊ������l�B</returns>
		public static object OnesComplement(object self)
		{
			if (self is Enum)
			{
				var selfType = self.GetType();
				var underType = Enum.GetUnderlyingType(selfType);
				if (underType == typeof(int))
					return Enum.ToObject(selfType, ~(int)self);
				else if (underType == typeof(long))
					return Enum.ToObject(selfType, ~(long)self);
				else if (underType == typeof(short))
					return Enum.ToObject(selfType, ~(short)self);
				else if (underType == typeof(byte))
					return Enum.ToObject(selfType, ~(byte)self);
				else if (underType == typeof(sbyte))
					return Enum.ToObject(selfType, ~(sbyte)self);
				else if (underType == typeof(uint))
					return Enum.ToObject(selfType, ~(uint)self);
				else if (underType == typeof(ulong))
					return Enum.ToObject(selfType, ~(ulong)self);
				else if (underType == typeof(ushort))
					return Enum.ToObject(selfType, ~(ushort)self);
				else
					throw Assert.Unreachable;
			}
			return null;
		}
	}
}
