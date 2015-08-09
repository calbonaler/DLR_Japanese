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
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Interpreter;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Generation
{
	// TODO: keep this?
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
	delegate void ActionRef<T0, T1>(ref T0 arg0, ref T1 arg1);

	/// <summary>�R���p�C���ɕK�v�ȃw���p�[ ���\�b�h�����J���܂��B</summary>
	public static class CompilerHelpers
	{
		/// <summary>public static �ł��郁�\�b�h�̑�����\���܂��B</summary>
		public static readonly MethodAttributes PublicStatic = MethodAttributes.Public | MethodAttributes.Static;
		static readonly MethodInfo _CreateInstanceMethod = new Func<int>(ScriptingRuntimeHelpers.CreateInstance<int>).Method.GetGenericMethodDefinition();
		static int _Counter; // �����_���\�b�h�Ɉ�ӂȖ��O�𐶐����邽��

		/// <summary>�w�肳�ꂽ�^�̑��݂��Ȃ����Ƃ�\���l���擾���܂��B</summary>
		/// <param name="type">���݂��Ȃ����Ƃ�\���l�̌^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�̑��݂��Ȃ����Ƃ�\���l�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		public static object GetMissingValue(Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			if (type.IsByRef)
				type = type.GetElementType();
			if (type.IsEnum)
				return Activator.CreateInstance(type);
			switch (Type.GetTypeCode(type))
			{
				default:
				case TypeCode.Object:
					// struct
					if (type.IsSealed && type.IsValueType)
						return Activator.CreateInstance(type);
					else if (type == typeof(object))
						return Missing.Value; // object �^�̈����͖{���� Missing �l���󂯕t����
					else if (!type.IsValueType)
						return null;
					else
						throw Error.CantCreateDefaultTypeFor(type);
				case TypeCode.Empty:
				case TypeCode.DBNull:
				case TypeCode.String:
					return null;
				case TypeCode.Boolean: return false;
				case TypeCode.Char: return '\0';
				case TypeCode.SByte: return (sbyte)0;
				case TypeCode.Byte: return (byte)0;
				case TypeCode.Int16: return (short)0;
				case TypeCode.UInt16: return (ushort)0;
				case TypeCode.Int32: return (int)0;
				case TypeCode.UInt32: return (uint)0;
				case TypeCode.Int64: return 0L;
				case TypeCode.UInt64: return 0UL;
				case TypeCode.Single: return 0.0f;
				case TypeCode.Double: return 0.0D;
				case TypeCode.Decimal: return (decimal)0;
				case TypeCode.DateTime: return DateTime.MinValue;
			}
		}

		/// <summary>�w�肳�ꂽ���\�b�h���C���X�^���X�Q�Ƃ̕K�v�Ȃ��Ăяo�����Ƃ��ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="mi">���f���郁�\�b�h���w�肵�܂��B</param>
		/// <returns>���\�b�h�ɃC���X�^���X��^���邱�ƂȂ��Ăяo�����Ƃ��ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsStatic(MethodBase mi) { return mi.IsConstructor || mi.IsStatic; }

		/// <summary>�w�肳�ꂽ���\�b�h���I�u�W�F�N�g���\�z���郁�\�b�h�ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="mb">���f���郁�\�b�h���w�肵�܂��B</param>
		/// <returns>���\�b�h���I�u�W�F�N�g���\�z���郁�\�b�h�ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsConstructor(MethodBase mb) { return mb.IsConstructor || mb.IsGenericMethod && ((MethodInfo)mb).GetGenericMethodDefinition() == _CreateInstanceMethod; }

		/// <summary>�w�肳�ꂽ���c���[ �m�[�h�^����r���Z�q�ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="op">���f���鎮�c���[ �m�[�h�^���w�肵�܂��B</param>
		/// <returns>���c���[ �m�[�h�^����r���Z�q��\���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsComparisonOperator(ExpressionType op)
		{
			switch (op)
			{
				case ExpressionType.LessThan: return true;
				case ExpressionType.LessThanOrEqual: return true;
				case ExpressionType.GreaterThan: return true;
				case ExpressionType.GreaterThanOrEqual: return true;
				case ExpressionType.Equal: return true;
				case ExpressionType.NotEqual: return true;
			}
			return false;
		}

		/// <summary><c>null</c> ���܂ނ��ׂẴI�u�W�F�N�g�ɑ΂���^��Ԃ��܂��B</summary>
		/// <param name="obj">�^��Ԃ��I�u�W�F�N�g���w�肵�܂��B<c>null</c> ���w�肷�邱�Ƃ��ł��܂��B</param>
		/// <returns>�w�肳�ꂽ�I�u�W�F�N�g�̌^�B<c>null</c> �̏ꍇ�� <see cref="DynamicNull"/> �̌^���Ԃ���܂��B</returns>
		public static Type GetType(object obj) { return obj == null ? typeof(DynamicNull) : obj.GetType(); }

		/// <summary>�w�肳�ꂽ���X�g�̂��ꂼ��̗v�f�̌^���w�肳�ꂽ�^�Ɠ��������ǂ����𔻒f���܂��B��r�̓��X�g���̎w�肳�ꂽ�C���f�b�N�X����J�n����܂��B</summary>
		/// <param name="args">�^����r����郊�X�g���w�肵�܂��B</param>
		/// <param name="start">�^�̔�r���J�n����郊�X�g���̈ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="types">�v�f�̌^�Ɣ�r����^�̃��X�g���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���X�g���̂��ׂĂ̗v�f�̌^���w�肳�ꂽ�^�Ɠ�������� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool TypesEqual(IEnumerable args, int start, IEnumerable<Type> types) { return types.Zip(args.Cast<object>().Skip(start), (x, y) => Tuple.Create(x, y)).All(x => x.Item1 == (x.Item2 != null ? x.Item2.GetType() : null)); }

		/// <summary>�w�肳�ꂽ���\�b�h���œK���\���ǂ����𔻒f���܂��B</summary>
		/// <param name="method">���ׂ郁�\�b�h���w�肵�܂��B</param>
		/// <returns>���\�b�h���œK���\�ł���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool CanOptimizeMethod(MethodBase method) { return !method.ContainsGenericParameters && !method.IsProtected() && !method.IsPrivate && method.DeclaringType.IsVisible; }

		/// <summary>���̃��\�b�h�Ƀf�B�X�p�b�`����p�u���b�N�ł���^�Ő錾����Ă��郁�\�b�h���擾���܂��B</summary>
		/// <param name="method">�p�u���b�N�ł���^�Ő錾����Ă��郁�\�b�h���擾���郁�\�b�h���w�肵�܂��B</param>
		/// <returns>
		/// �w�肳�ꂽ���\�b�h�Ƀf�B�X�p�b�`����p�u���b�N�ł���^�Ő錾����Ă��郁�\�b�h�����������ꍇ�͂��̃��\�b�h�B
		/// ����ȊO�̏ꍇ�͌��̃��\�b�h�B
		/// </returns>
		public static MethodInfo TryGetCallableMethod(MethodInfo method) { return TryGetCallableMethod(method.ReflectedType, method); }

		/// <summary>���̃��\�b�h�Ƀf�B�X�p�b�`����p�u���b�N�ł���^�Ő錾����Ă��郁�\�b�h���擾���܂��B</summary>
		/// <param name="targetType">���\�b�h��錾�܂��͌p������^���w�肵�܂��B</param>
		/// <param name="method">�p�u���b�N�ł���^�Ő錾����Ă��郁�\�b�h���擾���郁�\�b�h���w�肵�܂��B</param>
		/// <returns>
		/// �w�肳�ꂽ���\�b�h�Ƀf�B�X�p�b�`����p�u���b�N�ł���^�Ő錾����Ă��郁�\�b�h�����������ꍇ�͂��̃��\�b�h�B
		/// ����ȊO�̏ꍇ�͌��̃��\�b�h�B
		/// </returns>
		public static MethodInfo TryGetCallableMethod(Type targetType, MethodInfo method)
		{
			if (method.DeclaringType == null || method.DeclaringType.IsVisible)
				return method;
			// �ŏ��ɃI�[�o�[���C�h���Ă��錳�̌^����擾���Ă݂�
			var baseMethod = method.GetBaseDefinition();
			if (baseMethod.DeclaringType.IsVisible || baseMethod.DeclaringType.IsInterface)
				return baseMethod;
			// �������̃��\�b�h�����Ă���^�̃C���^�[�t�F�C�X����擾�ł���...
			return targetType.GetInterfaces().Select(x => targetType.GetInterfaceMap(x))
				.SelectMany(x =>
					x.InterfaceMethods.Zip(x.TargetMethods, (a, b) => new { Interface = a, Target = b })
					.Where(y => y.Target != null && y.Target.MethodHandle == method.MethodHandle)
					.Take(1).Select(y => y.Interface)
				).FirstOrDefault() ?? baseMethod;
		}

		/// <summary>�w�肳�ꂽ�^�̃����o����K�؂ȉ��ł��郁���o���擾���邱�Ƃŕs���ȃ����o�����O���������o�̔z���Ԃ��܂��B</summary>
		/// <param name="type">�����o�����������^���w�肵�܂��B</param>
		/// <param name="foundMembers">�������������o���w�肵�܂��B</param>
		/// <returns>�s���ȃ����o�����O���ꂽ�����o�̔z��B</returns>
		public static MemberInfo[] FilterNonVisibleMembers(Type type, MemberInfo[] foundMembers)
		{
			if (!type.IsVisible && foundMembers.Length > 0)
				// ���̕��@�Ŏ擾�ł��Ȃ������郁���o���폜����K�v������
				foundMembers = foundMembers.Select(x => TryGetVisibleMember(x)).Where(x => x != null).ToArray();
			return foundMembers;
		}

		/// <summary>�w�肳�ꂽ�����o�Ɋ֘A�Â���ꂽ���\�b�h������ł��郁�\�b�h���������邱�Ƃɂ��A���ł��郁���o���擾���܂��B</summary>
		/// <param name="curMember">���ł��郁���o���擾���郁���o���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�����o�Ɋ֘A������ł��郁���o�B���ł��郁���o��������Ȃ������ꍇ�� <c>null</c> ��Ԃ��܂��B</returns>
		public static MemberInfo TryGetVisibleMember(MemberInfo curMember)
		{
			MethodInfo mi;
			MemberInfo visible = null;
			switch (curMember.MemberType)
			{
				case MemberTypes.Method:
					mi = TryGetCallableMethod((MethodInfo)curMember);
					if (IsVisible(mi))
						visible = mi;
					break;
				case MemberTypes.Property:
					var pi = (PropertyInfo)curMember;
					mi = TryGetCallableMethod(pi.GetGetMethod() ?? pi.GetSetMethod());
					if (IsVisible(mi))
						visible = mi.DeclaringType.GetProperty(pi.Name);
					break;
				case MemberTypes.Event:
					var ei = (EventInfo)curMember;
					mi = TryGetCallableMethod(ei.GetAddMethod() ?? ei.GetRemoveMethod() ?? ei.GetRaiseMethod());
					if (IsVisible(mi))
						visible = mi.DeclaringType.GetEvent(ei.Name);
					break;
				// ���̕��@�ł͂���ȊO�͌��J����Ȃ�
			}
			return visible;
		}

		/// <summary>
		/// �w�肳�ꂽ 2 �̃����o�� IL �œ����\���������Ă��邩�ǂ����𔻒f���܂��B
		/// ���̃��\�b�h�͓��������o�ł����Ă����ڔ�r�̌��ʂ� <c>false</c> �ɂ����� <see cref="MemberInfo.ReflectedType"/> �v���p�e�B�𖳎����܂��B
		/// </summary>
		/// <param name="self">��r���� 1 �Ԗڂ̃����o���w�肵�܂��B</param>
		/// <param name="other">��r���� 2 �Ԗڂ̃����o���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ 2 �̃����o�� IL �œ����\���������Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool MemberEquals(this MemberInfo self, MemberInfo other)
		{
			if ((self == null) != (other == null))
				return false; // 1 �� null �������͈Ⴄ
			if (self == null)
				return true; // �����Ƃ� null
			if (self.MemberType != other.MemberType)
				return false;
			switch (self.MemberType)
			{
				case MemberTypes.Field:
					return ((FieldInfo)self).FieldHandle.Equals(((FieldInfo)other).FieldHandle);
				case MemberTypes.Method:
					return ((MethodInfo)self).MethodHandle.Equals(((MethodInfo)other).MethodHandle);
				case MemberTypes.Constructor:
					return ((ConstructorInfo)self).MethodHandle.Equals(((ConstructorInfo)other).MethodHandle);
				case MemberTypes.NestedType:
				case MemberTypes.TypeInfo:
					return ((Type)self).TypeHandle.Equals(((Type)other).TypeHandle);
				case MemberTypes.Event:
				case MemberTypes.Property:
				default:
					return ((MemberInfo)self).Module == ((MemberInfo)other).Module && ((MemberInfo)self).MetadataToken == ((MemberInfo)other).MetadataToken;
			}
		}

		/// <summary>���̃��\�b�h�Ƀf�B�X�p�b�`����p�u���b�N�ł���^�Ő錾����Ă��郁�\�b�h���擾���܂��B</summary>
		/// <param name="method">�p�u���b�N�ł���^�Ő錾����Ă��郁�\�b�h���擾���郁�\�b�h���w�肵�܂��B</param>
		/// <param name="privateBinding">�p�u���b�N�łȂ����\�b�h���Ăяo�����Ƃ��ł��邩�ǂ����������l���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���\�b�h�Ƀf�B�X�p�b�`����p�u���b�N�ł���^�Ő錾����Ă��郁�\�b�h�B</returns>
		/// <exception cref="System.InvalidOperationException"><paramref name="privateBinding"/> �� <c>false</c> �ł����p�u���b�N�ł��郁�\�b�h��������܂���ł����B</exception>
		public static MethodInfo GetCallableMethod(MethodInfo method, bool privateBinding)
		{
			var callable = TryGetCallableMethod(method);
			if (privateBinding || IsVisible(callable))
				return callable;
			throw Error.NoCallableMethods(method.DeclaringType, method.Name);
		}

		/// <summary>�w�肳�ꂽ���\�b�h�����ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="info">���f���郁�\�b�h���w�肵�܂��B</param>
		/// <returns>���\�b�h�����ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsVisible(MethodBase info) { return info.IsPublic && (info.DeclaringType == null || info.DeclaringType.IsVisible); }

		/// <summary>�w�肳�ꂽ�t�B�[���h�����ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="info">���f����t�B�[���h���w�肵�܂��B</param>
		/// <returns>�t�B�[���h�����ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsVisible(FieldInfo info) { return info.IsPublic && (info.DeclaringType == null || info.DeclaringType.IsVisible); }

		/// <summary>�w�肳�ꂽ���\�b�h�� protected �ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="info">���f���郁�\�b�h���w�肵�܂��B</param>
		/// <returns>���\�b�h�� protected �ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsProtected(this MethodBase info) { return info.IsFamily || info.IsFamilyOrAssembly; }

		/// <summary>�w�肳�ꂽ�t�B�[���h�� protected �ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="info">���f����t�B�[���h���w�肵�܂��B</param>
		/// <returns>�t�B�[���h�� protected �ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsProtected(this FieldInfo info) { return info.IsFamily || info.IsFamilyOrAssembly; }

		/// <summary>�w�肳�ꂽ�^�� protected �ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="type">���f����^���w�肵�܂��B</param>
		/// <returns>�^�� protected �ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsProtected(this Type type) { return type.IsNestedFamily || type.IsNestedFamORAssem; }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�̌^�̌p���K�w�̒��ŉ��ł���^���擾���܂��B</summary>
		/// <param name="value">���ł���^���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�I�u�W�F�N�g�̌^�̌p���K�w�̒��ŉ��ł���^�B</returns>
		public static Type GetVisibleType(object value) { return GetVisibleType(GetType(value)); }

		/// <summary>�w�肳�ꂽ�^�̌p���K�w�̒��ŉ��ł���^���擾���܂��B</summary>
		/// <param name="t">���ł���^���擾����^���w�肵�܂��B</param>
		/// <returns>�^�̌p���K�w�̒��ŉ��ł���^�B</returns>
		public static Type GetVisibleType(Type t)
		{
			while (!t.IsVisible)
				t = t.BaseType;
			return t;
		}

		/// <summary>�w�肳�ꂽ�^�̃R���X�g���N�^���擾���܂��B</summary>
		/// <param name="t">�R���X�g���N�^���擾����^���w�肵�܂��B</param>
		/// <param name="privateBinding">�v���C�x�[�g�ȃR���X�g���N�^���Ăяo�����Ƃ��ł��邩�ǂ����������l���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�ɒ�`����Ă���R���X�g���N�^�̔z��B</returns>
		public static MethodBase[] GetConstructors(Type t, bool privateBinding) { return GetConstructors(t, privateBinding, false); }

		/// <summary>�w�肳�ꂽ�^�̃R���X�g���N�^���擾���܂��B</summary>
		/// <param name="t">�R���X�g���N�^���擾����^���w�肵�܂��B</param>
		/// <param name="privateBinding">�v���C�x�[�g�ȃR���X�g���N�^���Ăяo�����Ƃ��ł��邩�ǂ����������l���w�肵�܂��B</param>
		/// <param name="includeProtected">protected �ȃR���X�g���N�^���܂߂邩�ǂ����������l���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�ɒ�`����Ă���R���X�g���N�^�̔z��B</returns>
		public static MethodBase[] GetConstructors(Type t, bool privateBinding, bool includeProtected)
		{
			if (t.IsArray)
				// �R���X�g���N�^�̂悤�Ɍ����܂����AJIT ���؂� new int[](3) ���D�݂܂���B
				// �������ǂ��V�����z��̍쐬��Ԃ��܂��B
				return new[] { new Func<int, int[]>(ScriptingRuntimeHelpers.CreateArray<int>).Method.GetGenericMethodDefinition().MakeGenericMethod(t.GetElementType()) };
			var bf = BindingFlags.Instance | BindingFlags.Public;
			if (privateBinding || includeProtected)
				bf |= BindingFlags.NonPublic;
			var ci = t.GetConstructors(bf);
			// �v���C�x�[�g�o�C���f�B���O�łȂ��Ƃ� protected �R���X�g���N�^�͎c���܂��B
			if (!privateBinding && includeProtected)
				ci = FilterConstructorsToPublicAndProtected(ci);
			if (t.IsValueType && t != typeof(ArgIterator))
				// �\���͈̂����̂Ȃ��R���X�g���N�^�͒�`���Ȃ��̂ŁA�W�F�l���b�N���\�b�h��ǉ����܂��B
				return ArrayUtils.Insert<MethodBase>(_CreateInstanceMethod.MakeGenericMethod(t), ci);
			return ci;
		}

		/// <summary>�w�肳�ꂽ�R���X�g���N�^����p�u���b�N�܂��� protected �Ƃ��Ē�`����Ă���R���X�g���N�^�݂̂𒊏o���܂��B</summary>
		/// <param name="ci">���̃R���X�g���N�^���w�肵�܂��B</param>
		/// <returns>���o���ꂽ�R���X�g���N�^�̔z��B</returns>
		public static ConstructorInfo[] FilterConstructorsToPublicAndProtected(IEnumerable<ConstructorInfo> ci) { return ci.Where(x => x.IsPublic || x.IsProtected()).ToArray(); }

		#region Type Conversions

		/// <summary>�w�肳�ꂽ�^�̊Ԃ̃��[�U�[��`�̈ÖٓI�ȕϊ����\�b�h���擾���܂��B</summary>
		/// <param name="fromType">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="toType">�ϊ���̌^���w�肵�܂��B</param>
		/// <returns>�^�̊Ԃ̃��[�U�[��`�̈ÖٓI�ȕϊ����\�b�h�B</returns>
		public static MethodInfo GetImplicitConverter(Type fromType, Type toType) { return GetConverter(fromType, toType, "op_Implicit"); }

		/// <summary>�w�肳�ꂽ�^�̊Ԃ̃��[�U�[��`�̖����I�ȕϊ����\�b�h���擾���܂��B</summary>
		/// <param name="fromType">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="toType">�ϊ���̌^���w�肵�܂��B</param>
		/// <returns>�^�̊Ԃ̃��[�U�[��`�̖����I�ȕϊ����\�b�h�B</returns>
		public static MethodInfo GetExplicitConverter(Type fromType, Type toType) { return GetConverter(fromType, toType, "op_Explicit"); }

		static MethodInfo GetConverter(Type fromType, Type toType, string opMethodName)
		{
			return fromType.GetMember(opMethodName, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static)
			.Concat(toType.GetMember(opMethodName, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static))
			.Cast<MethodInfo>().FirstOrDefault(x => (x.DeclaringType == null || x.DeclaringType.IsVisible) && x.ReturnType == toType && x.GetParameters()[0].ParameterType.IsAssignableFrom(fromType));
		}

		/// <summary>�w�肳�ꂽ�l����w�肳�ꂽ�^�ւ̈ÖٓI�ȕϊ������݂܂��B</summary>
		/// <param name="value">�ϊ����̒l���w�肵�܂��B</param>
		/// <param name="to">�l���ϊ������^���w�肵�܂��B</param>
		/// <param name="result">�ÖٓI�ϊ��̌��ʂ��i�[����ϐ����w�肵�܂��B</param>
		/// <returns>�ÖٓI�ϊ������������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool TryImplicitConversion(object value, Type to, out object result)
		{
			if (TryImplicitConvert(value, to, to.GetMember("op_Implicit"), out result))
				return true;
			for (var curType = GetType(value); curType != null; curType = curType.BaseType)
			{
				if (TryImplicitConvert(value, to, curType.GetMember("op_Implicit"), out result))
					return true;
			}
			return false;
		}

		static bool TryImplicitConvert(object value, Type to, IEnumerable<MemberInfo> implicitConv, out object result)
		{
			var method = implicitConv.Cast<MethodInfo>().FirstOrDefault(x => to.IsValueType == x.ReturnType.IsValueType && to.IsAssignableFrom(x.ReturnType));
			if (method != null)
			{
				result = method.IsStatic ? method.Invoke(null, new object[] { value }) : method.Invoke(value, ArrayUtils.EmptyObjects);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�̌^�� <see cref="StrongBox&lt;T&gt;"/> �ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="target">���f����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�I�u�W�F�N�g�̌^�� <see cref="StrongBox&lt;T&gt;"/> �ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsStrongBox(object target) { return IsStrongBox(GetType(target)); }

		/// <summary>�w�肳�ꂽ�^�� <see cref="StrongBox&lt;T&gt;"/> �ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="t">���f����^���w�肵�܂��B</param>
		/// <returns>�^�� <see cref="StrongBox&lt;T&gt;"/> �ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsStrongBox(Type t) { return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(StrongBox<>); }

		/// <summary>�ϊ������s�����ꍇ�̒l��\�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <param name="type">�ϊ���̌^���w�肵�܂��B</param>
		/// <returns>�ϊ������s�����ꍇ�̒l��\�� <see cref="Expression"/>�B</returns>
		public static Expression GetTryConvertReturnValue(Type type) { return type.IsInterface || type.IsClass || TypeUtils.IsNullableType(type) ? AstUtils.Constant(null, type) : AstUtils.Constant(Activator.CreateInstance(type)); }

		/// <summary>�w�肳�ꂽ�^�̊Ԃŕϊ����s�����Ƃ��ł��� <see cref="TypeConverter"/> �����݂��邩�ǂ����𒲂ׂ܂��B</summary>
		/// <param name="fromType">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="toType">�ϊ���� <see cref="TypeConverterAttribute"/> ����`����Ă���^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�̊Ԃŕϊ����s�����Ƃ��ł��� <see cref="TypeConverter"/> �����݂���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool HasTypeConverter(Type fromType, Type toType)
		{
			TypeConverter _;
			return TryGetTypeConverter(fromType, toType, out _);
		}

		/// <summary>�w�肳�ꂽ�^�̊Ԃ� <see cref="TypeConverter"/> ��K�p���ĕϊ������݂܂��B</summary>
		/// <param name="value">�ϊ������l���w�肵�܂��B</param>
		/// <param name="toType">�ϊ���̌^���w�肵�܂��B</param>
		/// <param name="result">�ϊ��̌��ʂ��i�[�����ϐ����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�̊Ԃł̕ϊ������������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool TryApplyTypeConverter(object value, Type toType, out object result)
		{
			TypeConverter converter;
			if (value != null && TryGetTypeConverter(value.GetType(), toType, out converter))
			{
				result = converter.ConvertFrom(value);
				return true;
			}
			else
			{
				result = value;
				return false;
			}
		}

		/// <summary>�w�肳�ꂽ�^�̊Ԃŕϊ����s�����Ƃ��ł��� <see cref="TypeConverter"/> �̎擾�����݂܂��B</summary>
		/// <param name="fromType">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="toType">�ϊ���� <see cref="TypeConverterAttribute"/> ����`����Ă���^���w�肵�܂��B</param>
		/// <param name="converter">�ϊ����s�����Ƃ��ł��� <see cref="TypeConverter"/> ���i�[�����ϐ����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�̊Ԃŕϊ����s�����Ƃ��ł��� <see cref="TypeConverter"/> �����݂���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static bool TryGetTypeConverter(Type fromType, Type toType, out TypeConverter converter)
		{
			ContractUtils.RequiresNotNull(fromType, "fromType");
			ContractUtils.RequiresNotNull(toType, "toType");
			// ���p�\�Ȍ^�ϊ�������...
			foreach (var tca in toType.GetCustomAttributes<TypeConverterAttribute>(true))
			{
				try { converter = Activator.CreateInstance(Type.GetType(tca.ConverterTypeName)) as TypeConverter; }
				catch (Exception) { converter = null; }
				if (converter != null && converter.CanConvertFrom(fromType))
					return true;
			}
			converter = null;
			return false;
		}

		#endregion

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���Ăяo�����Ƃ��ł��郁�\�b�h���擾���܂��B</summary>
		/// <param name="obj">�Ăяo�����\�b�h���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�I�u�W�F�N�g���Ăяo�����\�b�h�̔z��B</returns>
		public static MethodBase[] GetMethodTargets(object obj)
		{
			var t = GetType(obj);
			if (typeof(Delegate).IsAssignableFrom(t))
				return new MethodBase[] { t.GetMethod("Invoke") };
			else if (typeof(BoundMemberTracker).IsAssignableFrom(t))
			{
				if (((BoundMemberTracker)obj).BoundTo.MemberType == TrackerTypes.Method) { }
			}
			else if (typeof(MethodGroup).IsAssignableFrom(t))
			{
			}
			else if (typeof(MemberGroup).IsAssignableFrom(t))
			{
			}
			else
				return t.GetMember("Call", MemberTypes.Method, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Cast<MethodInfo>().Where(x => x.IsSpecialName).ToArray();
			return null;
		}

		/// <summary>�w�肳�ꂽ�^�̈�������і߂�l�̃f���Q�[�g���^�����Ɏ��� <see cref="CallSite&lt;T&gt;"/> ���쐬���܂��B</summary>
		/// <param name="types">�f���Q�[�g�̈�������і߂�l�̌^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�̈�������і߂�l�̃f���Q�[�g���^�����Ɏ��� <see cref="CallSite&lt;T&gt;"/>�B</returns>
		public static Type MakeCallSiteType(params Type[] types) { return typeof(CallSite<>).MakeGenericType(Expression.GetDelegateType(types)); }

		/// <summary>�w�肳�ꂽ�����_���ɑ΂��Ė|�󂳂��f���Q�[�g���쐬���܂��B</summary>
		/// <param name="lambda">�R���p�C�����郉���_�����w�肵�܂��B</param>
		/// <returns>�����_����|��ł���f���Q�[�g�B</returns>
		public static Delegate LightCompile(this LambdaExpression lambda) { return LightCompile(lambda, -1); }

		/// <summary>�w�肳�ꂽ�����_���ɑ΂��Ė|�󂳂��f���Q�[�g���쐬���܂��B</summary>
		/// <param name="lambda">�R���p�C�����郉���_�����w�肵�܂��B</param>
		/// <param name="compilationThreshold">�C���^�v���^���R���p�C�����J�n����J��Ԃ������w�肵�܂��B</param>
		/// <returns>�����_����|��ł���f���Q�[�g�B</returns>
		public static Delegate LightCompile(this LambdaExpression lambda, int compilationThreshold) { return new LightCompiler(compilationThreshold).CompileTop(lambda).CreateDelegate(); }

		/// <summary>�w�肳�ꂽ�����_���ɑ΂��Ė|�󂳂��f���Q�[�g���쐬���܂��B</summary>
		/// <typeparam name="TDelegate">�����_���̃f���Q�[�g�^���w�肵�܂��B</typeparam>
		/// <param name="lambda">�R���p�C�����郉���_�����w�肵�܂��B</param>
		/// <returns>�����_����|��ł���f���Q�[�g�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static TDelegate LightCompile<TDelegate>(this Expression<TDelegate> lambda) where TDelegate : class { return (TDelegate)(object)LightCompile((LambdaExpression)lambda); }

		/// <summary>�w�肳�ꂽ�����_���ɑ΂��Ė|�󂳂��f���Q�[�g���쐬���܂��B</summary>
		/// <typeparam name="TDelegate">�����_���̃f���Q�[�g�^���w�肵�܂��B</typeparam>
		/// <param name="lambda">�R���p�C�����郉���_�����w�肵�܂��B</param>
		/// <param name="compilationThreshold">�C���^�v���^���R���p�C�����J�n����J��Ԃ������w�肵�܂��B</param>
		/// <returns>�����_����|��ł���f���Q�[�g�B</returns>
		public static TDelegate LightCompile<TDelegate>(this Expression<TDelegate> lambda, int compilationThreshold) where TDelegate : class { return (TDelegate)(object)LightCompile((LambdaExpression)lambda, compilationThreshold); }

		/// <summary>�����_�������\�b�h��`�ɃR���p�C�����܂��B</summary>
		/// <param name="lambda">�R���p�C�����郉���_�����w�肵�܂��B</param>
		/// <param name="method">�����_���� IL ��ێ����邽�߂Ɏg�p����� <see cref="MethodBuilder"/> ���w�肵�܂��B</param>
		/// <param name="emitDebugSymbols">PDB �V���{���X�g�A�Ƀf�o�b�O��񂪏o�͂���邩�ǂ����������l���w�肵�܂��B</param>
		public static void CompileToMethod(this LambdaExpression lambda, MethodBuilder method, bool emitDebugSymbols)
		{
			if (emitDebugSymbols)
			{
				ContractUtils.Requires(method.Module is ModuleBuilder, "method", "MethodBuilder �͗L���� ModuleBuilder ��ێ����Ă��܂���B");
				lambda.CompileToMethod(method, DebugInfoGenerator.CreatePdbGenerator());
			}
			else
				lambda.CompileToMethod(method);
		}

		/// <summary>
		/// �����_�����R���p�C�����܂��B
		/// <paramref name="emitDebugSymbols"/> �� <c>true</c> �̏ꍇ�A�����_���� <see cref="TypeBuilder"/> ���ɃR���p�C������܂��B
		/// ����ȊO�̏ꍇ�́A���̃��\�b�h�͒P�� <see cref="Expression&lt;TDelegate&gt;.Compile()"/> ���Ăяo�����ƂƓ����ł��B
		/// ���̉����͓��I���\�b�h���f�o�b�O���������Ƃ��ł��Ȃ��Ƃ��� CLR �̐����ɂ����̂ł��B
		/// </summary>
		/// <typeparam name="TDelegate">�����_���̃f���Q�[�g�^���w�肵�܂��B</typeparam>
		/// <param name="lambda">�R���p�C�����郉���_�����w�肵�܂��B</param>
		/// <param name="emitDebugSymbols">�f�o�b�O �V���{�� (PDB) �� <see cref="DebugInfoGenerator"/> �ɂ���ďo�͂���邩�ǂ����������l���w�肵�܂��B</param>
		/// <returns>�R���p�C�����ꂽ�f���Q�[�g�B</returns>
		public static TDelegate Compile<TDelegate>(this Expression<TDelegate> lambda, bool emitDebugSymbols) where TDelegate : class { return emitDebugSymbols ? CompileToMethod(lambda, DebugInfoGenerator.CreatePdbGenerator(), true) : lambda.Compile(); }

		/// <summary>
		/// �����_����V�����^�ɏo�͂��邱�ƂŃR���p�C�����܂��B�I�v�V�����Ńf�o�b�O�\�ł��邱�Ƃ��}�[�N�ł��܂��B
		/// ���̉����͓��I���\�b�h���f�o�b�O���������Ƃ��ł��Ȃ��Ƃ��� CLR �̐����ɂ����̂ł��B
		/// </summary>
		/// <typeparam name="TDelegate">�����_���̃f���Q�[�g�^���w�肵�܂��B</typeparam>
		/// <param name="lambda">�R���p�C�����郉���_�����w�肵�܂��B</param>
		/// <param name="debugInfoGenerator">�R���p�C���ɂ���ăV�[�P���X �|�C���g�̃}�[�N�⃍�[�J���ϐ��̒��߂Ɏg�p����� <see cref="DebugInfoGenerator"/> ���w�肵�܂��B</param>
		/// <param name="emitDebugSymbols">�f�o�b�O �V���{�� (PDB) �� <paramref name="debugInfoGenerator"/> �ɂ���ďo�͂���邩�ǂ����������l���w�肵�܂��B</param>
		/// <returns>�R���p�C�����ꂽ�f���Q�[�g�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static TDelegate CompileToMethod<TDelegate>(this Expression<TDelegate> lambda, DebugInfoGenerator debugInfoGenerator, bool emitDebugSymbols) where TDelegate : class { return (TDelegate)(object)CompileToMethod((LambdaExpression)lambda, debugInfoGenerator, emitDebugSymbols); }

		/// <summary>
		/// �����_����V�����^�ɏo�͂��邱�ƂŃR���p�C�����܂��B�I�v�V�����Ńf�o�b�O�\�ł��邱�Ƃ��}�[�N�ł��܂��B
		/// ���̉����͓��I���\�b�h���f�o�b�O���������Ƃ��ł��Ȃ��Ƃ��� CLR �̐����ɂ����̂ł��B
		/// </summary>
		/// <param name="lambda">�R���p�C�����郉���_�����w�肵�܂��B</param>
		/// <param name="debugInfoGenerator">�R���p�C���ɂ���ăV�[�P���X �|�C���g�̃}�[�N�⃍�[�J���ϐ��̒��߂Ɏg�p����� <see cref="DebugInfoGenerator"/> ���w�肵�܂��B</param>
		/// <param name="emitDebugSymbols">�f�o�b�O �V���{�� (PDB) �� <paramref name="debugInfoGenerator"/> �ɂ���ďo�͂���邩�ǂ����������l���w�肵�܂��B</param>
		/// <returns>�R���p�C�����ꂽ�f���Q�[�g�B</returns>
		public static Delegate CompileToMethod(this LambdaExpression lambda, DebugInfoGenerator debugInfoGenerator, bool emitDebugSymbols)
		{
			// �����_�������O�������Ă��Ȃ��ꍇ�A��ӂȃ��\�b�h�����쐬����B
			var methodName = string.IsNullOrEmpty(lambda.Name) ? "lambda_method$" + System.Threading.Interlocked.Increment(ref _Counter) : lambda.Name;
			var type = Snippets.DefinePublicType(methodName, typeof(object), false, emitDebugSymbols);
			var rewriter = new BoundConstantsRewriter(type);
			lambda = (LambdaExpression)rewriter.Visit(lambda);
			var method = type.DefineMethod(methodName, PublicStatic);
			lambda.CompileToMethod(method, debugInfoGenerator);
			var finished = type.CreateType();
			rewriter.InitializeFields(finished);
			return Delegate.CreateDelegate(lambda.Type, finished.GetMethod(method.Name));
		}

		// Matches ILGen.TryEmitConstant
		/// <summary>IL �Ɏw�肳�ꂽ�^�̒萔�l���o�͂ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="value">���ׂ�l���w�肵�܂��B</param>
		/// <param name="type">���ׂ�^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�̒萔�l�� IL �ɏo�͂ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool CanEmitConstant(object value, Type type)
		{
			if (value == null)
				return true;
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Char:
				case TypeCode.Byte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Decimal:
				case TypeCode.String:
					return true;
			}
			var t = value as Type;
			if (t != null && ILGeneratorExtensions.ShouldLdtoken(t))
				return true;
			var mb = value as MethodBase;
			if (mb != null && ILGeneratorExtensions.ShouldLdtoken(mb))
				return true;
			return false;
		}

		/// <summary><see cref="DynamicExpression"/> �� site.Target(site, *args) �̌`�ɏk�ނ��܂��B</summary>
		/// <param name="node">�k�ނ���m�[�h���w�肵�܂��B</param>
		/// <returns>�k�ނ��ꂽ�m�[�h�B</returns>
		public static Expression Reduce(DynamicExpression node)
		{
			// Store the callsite as a constant
			var siteConstant = AstUtils.Constant(CallSite.Create(node.DelegateType, node.Binder));
			// ($site = siteExpr).Target.Invoke($site, *args)
			var site = Expression.Variable(siteConstant.Type, "$site");
			return Expression.Block(new[] { site },
				Expression.Call(Expression.Field(Expression.Assign(site, siteConstant), siteConstant.Type.GetField("Target")),
					node.DelegateType.GetMethod("Invoke"),
					ArrayUtils.Insert(site, node.Arguments)
				)
			);
		}

		/// <summary>���ׂĂ̐������Ă���I�u�W�F�N�g����菜���A�^�̐ÓI�t�B�[���h�ɔz�u���郊���C�^�[��\���܂��B</summary>
		sealed class BoundConstantsRewriter : ExpressionVisitor
		{
			sealed class ReferenceEqualityComparer : EqualityComparer<object>
			{
				internal static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

				ReferenceEqualityComparer() { }

				public override bool Equals(object x, object y) { return ReferenceEquals(x, y); }

				public override int GetHashCode(object obj) { return RuntimeHelpers.GetHashCode(obj); }
			}

			readonly Dictionary<object, FieldBuilder> _fields = new Dictionary<object, FieldBuilder>(ReferenceEqualityComparer.Instance);
			readonly TypeBuilder _type;

			internal BoundConstantsRewriter(TypeBuilder type) { _type = type; }

			internal void InitializeFields(Type type)
			{
				foreach (var pair in _fields)
					type.GetField(pair.Value.Name).SetValue(null, pair.Key);
			}

			protected override Expression VisitConstant(ConstantExpression node)
			{
				if (CanEmitConstant(node.Value, node.Type))
					return node;
				FieldBuilder field;
				if (!_fields.TryGetValue(node.Value, out field))
				{
					field = _type.DefineField("$constant" + _fields.Count, GetVisibleType(node.Value.GetType()), FieldAttributes.Public | FieldAttributes.Static);
					_fields.Add(node.Value, field);
				}
				Expression result = Expression.Field(null, field);
				if (result.Type != node.Type)
					result = Expression.Convert(result, node.Type);
				return result;
			}

			protected override Expression VisitDynamic(DynamicExpression node) { return Visit(Reduce(node)); }
		}
	}
}
