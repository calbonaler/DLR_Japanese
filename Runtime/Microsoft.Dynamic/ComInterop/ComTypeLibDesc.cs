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
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>
	/// �^�C�v���C�u�����̃L���b�V�����ꂽ����\���܂��B
	/// �v�����ꂽ���݂̂��ۑ�����܂��B
	/// �R�N���X�̓C�x���g �t�b�N�A�b�v�Ɏg�p����܂��B
	/// �񋓑̂̓X�N���v�g����V���{�����ŃA�N�Z�X���邽�߂Ɋi�[����܂��B
	/// </summary>
	public sealed class ComTypeLibDesc : IDynamicMetaObjectProvider
	{
		// �ʏ�^�C�v���C�u�����͔��ɏ����̃R�N���X�����܂܂Ȃ����߁A�v�f�������Ȃ��ꍇ�Ƀp�t�H�[�}���X���悢�����N���X�g���g�p���܂��B
		LinkedList<ComTypeClassDesc> _classes = new LinkedList<ComTypeClassDesc>();
		Dictionary<string, ComTypeEnumDesc> _enums = new Dictionary<string, ComTypeEnumDesc>();
		ComTypes.TYPELIBATTR _typeLibAttributes;

		readonly static Dictionary<Guid, ComTypeLibDesc> _CachedTypeLibDesc = new Dictionary<Guid, ComTypeLibDesc>();

		ComTypeLibDesc() { }

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return string.Format(CultureInfo.CurrentCulture, "<type library {0}>", Name); }

		/// <summary>���̃I�u�W�F�N�g�̃h�L�������g���擾���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		public string Documentation { get { return string.Empty; } }

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) { return new TypeLibMetaObject(parameter, this); }

		/// <summary>�w�肳�ꂽ GUID �ɑΉ�����o�^����Ă���ŐV�̃^�C�v���C�u�����Ƃ���Ɋ܂܂�Ă���R�N���X����ї񋓑̂�ǂݎ��A�R�N���X�̃C���X�^���X���Ɨ񋓑̂̎��ۂ̒l���擾�ł���悤�ɂ��� <see cref="IDynamicMetaObjectProvider"/> ���쐬���܂��B</summary>
		/// <param name="typeLibGuid">�^�C�v���C�u���������ʂ��� GUID (�O���[�o����ӎ��ʎq) ���w�肵�܂��B</param>
		/// <returns>�^�C�v���C�u�����Ɋւ����񂪊i�[���ꂽ <see cref="ComTypeLibInfo"/> �I�u�W�F�N�g�B</returns>
		[System.Runtime.Versioning.ResourceExposure(System.Runtime.Versioning.ResourceScope.Machine)]
		[System.Runtime.Versioning.ResourceConsumption(System.Runtime.Versioning.ResourceScope.Machine, System.Runtime.Versioning.ResourceScope.Machine)]
		public static ComTypeLibInfo CreateFromGuid(Guid typeLibGuid) { return new ComTypeLibInfo(GetFromTypeLib(UnsafeMethods.LoadRegTypeLib(ref typeLibGuid, -1, -1, 0))); } // majorVersion = -1, minorVersion = -1 �͏�ɍŐV�̃^�C�v���C�u���������[�h����

		/// <summary>OLE �I�[�g���[�V�����݊��� RCW ���� ITypeLib �I�u�W�F�N�g�Ƃ���Ɋ܂܂�Ă���R�N���X����ї񋓑̂�ǂݎ��A�R�N���X�̃C���X�^���X���Ɨ񋓑̂̎��ۂ̒l���擾�ł���悤�ɂ��� <see cref="IDynamicMetaObjectProvider"/> ���쐬���܂��B</summary>
		/// <param name="rcw">�^�C�v���C�u�������擾���� OLE �I�[�g���[�V�����݊��� RCW ���w�肵�܂��B</param>
		/// <returns>�^�C�v���C�u�����Ɋւ����񂪊i�[���ꂽ <see cref="ComTypeLibInfo"/> �I�u�W�F�N�g�B</returns>
		public static ComTypeLibInfo CreateFromObject(object rcw)
		{
			if (!Marshal.IsComObject(rcw))
				throw new ArgumentException("COM object �ł���K�v������܂��B");
			ComTypes.ITypeLib typeLib;
			int typeInfoIndex;
			ComRuntimeHelpers.GetITypeInfoFromIDispatch(rcw as IDispatch, true).GetContainingTypeLib(out typeLib, out typeInfoIndex);
			return new ComTypeLibInfo(GetFromTypeLib(typeLib));
		}

		internal static ComTypeLibDesc GetFromTypeLib(ComTypes.ITypeLib typeLib)
		{
			// check whether we have already loaded this type library
			ComTypes.TYPELIBATTR typeLibAttr = ComRuntimeHelpers.GetTypeAttrForTypeLib(typeLib);
			ComTypeLibDesc typeLibDesc;
			lock (_CachedTypeLibDesc)
			{
				if (_CachedTypeLibDesc.TryGetValue(typeLibAttr.guid, out typeLibDesc))
					return typeLibDesc;
			}
			typeLibDesc = new ComTypeLibDesc();
			typeLibDesc.Name = ComRuntimeHelpers.GetNameOfLib(typeLib);
			typeLibDesc._typeLibAttributes = typeLibAttr;
			int countTypes = typeLib.GetTypeInfoCount();
			for (int i = 0; i < countTypes; i++)
			{
				ComTypes.TYPEKIND typeKind;
				typeLib.GetTypeInfoType(i, out typeKind);
				ComTypes.ITypeInfo typeInfo;
				typeLib.GetTypeInfo(i, out typeInfo);
				if (typeKind == ComTypes.TYPEKIND.TKIND_COCLASS)
					typeLibDesc._classes.AddLast(new ComTypeClassDesc(typeInfo, typeLibDesc));
				else if (typeKind == ComTypes.TYPEKIND.TKIND_ENUM)
				{
					ComTypeEnumDesc enumDesc = new ComTypeEnumDesc(typeInfo, typeLibDesc);
					typeLibDesc._enums.Add(enumDesc.TypeName, enumDesc);
				}
				else if (typeKind == ComTypes.TYPEKIND.TKIND_ALIAS)
				{
					var typeAttr = ComRuntimeHelpers.GetTypeAttrForTypeInfo(typeInfo);
					if (typeAttr.tdescAlias.vt == (short)VarEnum.VT_USERDEFINED)
					{
						string aliasName, documentation;
						ComRuntimeHelpers.GetInfoFromType(typeInfo, out aliasName, out documentation);
						ComTypes.ITypeInfo referencedTypeInfo;
						typeInfo.GetRefTypeInfo(typeAttr.tdescAlias.lpValue.ToInt32(), out referencedTypeInfo);
						if (ComRuntimeHelpers.GetTypeAttrForTypeInfo(referencedTypeInfo).typekind == ComTypes.TYPEKIND.TKIND_ENUM)
							typeLibDesc._enums.Add(aliasName, new ComTypeEnumDesc(referencedTypeInfo, typeLibDesc));
					}
				}
			}
			// cached the typelib using the guid as the dictionary key
			lock (_CachedTypeLibDesc)
				_CachedTypeLibDesc.Add(typeLibAttr.guid, typeLibDesc);
			return typeLibDesc;
		}

		/// <summary>�w�肳�ꂽ���O�����^�̋L�q���^�C�v���C�u�������猟�����܂��B</summary>
		/// <param name="member">�������閼�O���w�肵�܂��B</param>
		/// <returns>���������^�̋L�q�B�^��������Ȃ������ꍇ�� <c>null</c> ��Ԃ��܂��B</returns>
		public ComTypeDesc GetTypeLibObjectDesc(string member)
		{
			ComTypeEnumDesc enumDesc;
			if (_enums != null && _enums.TryGetValue(member, out enumDesc))
				return enumDesc;
			return _classes.FirstOrDefault(x => member == x.TypeName);
		}

		internal string[] GetMemberNames() { return _classes.Select(x => x.TypeName).Concat(_enums.Select(x => x.Key)).ToArray(); }

		internal bool HasMember(string member) { return _classes.Any(x => member == x.TypeName) || _enums.ContainsKey(member); }

		/// <summary>���̃^�C�v���C�u�����̃O���[�o����Ӄ��C�u�������ʎq���擾���܂��B</summary>
		public Guid Guid { get { return _typeLibAttributes.guid; } }

		/// <summary>���̃^�C�v���C�u�����̃��W���[�o�[�W�����ԍ����擾���܂��B</summary>
		public short VersionMajor { get { return _typeLibAttributes.wMajorVerNum; } }

		/// <summary>���̃^�C�v���C�u�����̃}�C�i�[�o�[�W�����ԍ����擾���܂��B</summary>
		public short VersionMinor { get { return _typeLibAttributes.wMinorVerNum; } }

		/// <summary>���̃^�C�v���C�u�����̖��O���擾���܂��B</summary>
		public string Name { get; private set; }

		internal ComTypeClassDesc GetCoClassForInterface(string itfName) { return _classes.FirstOrDefault(x => x.Implements(itfName, false)); }
	}
}