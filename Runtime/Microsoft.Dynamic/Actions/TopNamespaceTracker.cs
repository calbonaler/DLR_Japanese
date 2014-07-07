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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>���[�h���ꂽ���ׂẴA�Z���u����g�ݍ��݃��W���[���Ȃǂ̒ǉ��̏����܂ލŏ�ʖ��O��Ԃ�\���܂��B</summary>
	public class TopNamespaceTracker : NamespaceTracker
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")] // TODO: fix
		int _lastDiscovery = 0;
		internal readonly object HierarchyLock = new object();
		static Dictionary<Guid, Type> _comTypeCache = new Dictionary<Guid, Type>();

		/// <summary>�w�肳�ꂽ <see cref="ScriptDomainManager"/> ���g�p���āA<see cref="Microsoft.Scripting.Actions.TopNamespaceTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="manager">���̍ŏ�ʖ��O��ԂɊ֘A�t������ <see cref="ScriptDomainManager"/> ���w�肵�܂��B</param>
		public TopNamespaceTracker(ScriptDomainManager manager) : base(null)
		{
			ContractUtils.RequiresNotNull(manager, "manager");
			SetTopPackage(this);
			DomainManager = manager;
		}

		/// <summary>�w�肳�ꂽ���O��ԂɊ֘A�t����ꂽ�p�b�P�[�W���擾���A�֘A�t����ꂽ���W���[�����p�b�P�[�W���C���|�[�g���ꂽ�Ƃ��ă}�[�N���܂��B</summary>
		/// <param name="name">�C���|�[�g���閼�O��Ԃ̖��O���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���O��Ԃ�\�� <see cref="NamespaceTracker"/>�B</returns>
		public NamespaceTracker TryGetPackage(string name) { return TryGetPackage(SymbolTable.StringToId(name)); }

		/// <summary>�w�肳�ꂽ���O��ԂɊ֘A�t����ꂽ�p�b�P�[�W���擾���A�֘A�t����ꂽ���W���[�����p�b�P�[�W���C���|�[�g���ꂽ�Ƃ��ă}�[�N���܂��B</summary>
		/// <param name="name">�C���|�[�g���閼�O��Ԃ̖��O�� <see cref="SymbolId"/> �Ƃ��Ďw�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���O��Ԃ�\�� <see cref="NamespaceTracker"/>�B</returns>
		public NamespaceTracker TryGetPackage(SymbolId name) { return TryGetPackageAny(name) as NamespaceTracker; }

		/// <summary>
		/// �w�肳�ꂽ���O�̃p�b�P�[�W���擾���A�֘A�t����ꂽ���W���[�����p�b�P�[�W���C���|�[�g���ꂽ�Ƃ��ă}�[�N���܂��B
		/// �擾�����p�b�P�[�W�͌^�ł���\��������܂��B
		/// </summary>
		/// <param name="name">�C���|�[�g����p�b�P�[�W�̖��O���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�p�b�P�[�W�B</returns>
		public MemberTracker TryGetPackageAny(string name) { return TryGetPackageAny(SymbolTable.StringToId(name)); }

		/// <summary>
		/// �w�肳�ꂽ���O�̃p�b�P�[�W���擾���A�֘A�t����ꂽ���W���[�����p�b�P�[�W���C���|�[�g���ꂽ�Ƃ��ă}�[�N���܂��B
		/// �擾�����p�b�P�[�W�͌^�ł���\��������܂��B
		/// </summary>
		/// <param name="name">�C���|�[�g����p�b�P�[�W�̖��O�� <see cref="SymbolId"/> �Ƃ��Ďw�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�p�b�P�[�W�B</returns>
		public MemberTracker TryGetPackageAny(SymbolId name)
		{
			MemberTracker ret;
			if (TryGetValue(name, out ret))
				return ret;
			return null;
		}

		/// <summary>�w�肳�ꂽ���O�̃p�b�P�[�W���擾���܂��B�^�̃��[�h�Ȃǂ͍s���܂���B</summary>
		/// <param name="name">�擾����p�b�P�[�W�̖��O�� <see cref="SymbolId"/> �Ƃ��Ďw�肵�܂��B</param>
		/// <returns>�擾���ꂽ�p�b�P�[�W�B</returns>
		public MemberTracker TryGetPackageLazy(SymbolId name)
		{
			lock (HierarchyLock)
			{
				MemberTracker ret;
				if (_dict.TryGetValue(SymbolTable.IdToString(name), out ret))
					return ret;
				return null;
			}
		}

		/// <summary>�w�肳�ꂽ�A�Z���u�������[�h���܂��B</summary>
		/// <param name="assem">���[�h����A�Z���u�����w�肵�܂��B</param>
		/// <returns>���߂ăA�Z���u�������[�h���ꂽ�ꍇ�� <c>true</c>�B�ȑO�Ƀ��[�h����Ă����ꍇ�� <c>false</c>�B</returns>
		public bool LoadAssembly(Assembly assem)
		{
			ContractUtils.RequiresNotNull(assem, "assem");
			lock (HierarchyLock)
			{
				if (_packageAssemblies.Contains(assem))
					return false; // �A�Z���u���͊��Ƀ��[�h����Ă����̂ł����������邱�Ƃ͂Ȃ��B
				_packageAssemblies.Add(assem);
				UpdateSubtreeIds();
				PublishComTypes(assem);
			}
			return true;
		}

		/// <summary>�A�Z���u�����X�L�������āACOM �I�u�W�F�N�g�̌^��`�� COM �C���^�[�t�F�C�X�� GUID �Ɗ֘A�t���܂��B</summary>
		/// <param name="interopAssembly">�X�L�������� (���݉^�p) �A�Z���u�����w�肵�܂��B</param>
		/// <remarks>�ǂݍ��񂾂��ׂẴA�Z���u���̃X�L�����͕s�o�ςȂ��߁A�����ACOM �^�̃X�L�������g���K�����薾���I�ȃ��[�U�[�o�C���_�[���l�Ă���\��������܂��B</remarks>
		public static void PublishComTypes(Assembly interopAssembly)
		{
			lock (_comTypeCache)
			{
				// �����̂Ȃ��r���[�𔭍s�ł���悤�ɑ���S�̂����b�N���܂��B
				foreach (var type in LoadTypesFromAssembly(interopAssembly, false).Where(x => x.IsImport && x.IsInterface))
				{
					Type existing;
					if (!_comTypeCache.TryGetValue(type.GUID, out existing))
						_comTypeCache[type.GUID] = type;
					else if (!existing.IsDefined(typeof(CoClassAttribute), false))
						// CoClassAttribute�̂���^��D�悵�܂��B��:
						// MS.Office.Interop.Excel.Worksheet vs MS.Office.Interop.Excel._Worksheet
						// Worksheet �͌^���T�|�[�g���邷�ׂẴC���^�[�t�F�C�X���`���Ă��āACoClassAttribute ������܂��B
						// _Worksheet �̓��[�N�V�[�g�̃C���^�[�t�F�C�X��������܂���B
						// �����������Ƃ����� GUID �������Ă��܂��B
						_comTypeCache[type.GUID] = type;
				}
			}
		}

		/// <summary>�ŏ�ʖ��O��ԂɊ֘A�t����ꂽ���ׂẴA�Z���u�����̌^��K�؂Ȗ��O��Ԃɔz�u���܂��B</summary>
		protected override void LoadNamespaces()
		{
			lock (HierarchyLock)
			{
				for (int i = _lastDiscovery; i < _packageAssemblies.Count; i++)
					DiscoverAllTypes(_packageAssemblies[i]);
				_lastDiscovery = _packageAssemblies.Count;
			}
		}

		/// <summary>�ŏ�ʖ��O��ԂɊ֘A�t�����Ă���h���C���Ǘ����s�� <see cref="ScriptDomainManager"/> ���擾���܂��B</summary>
		public ScriptDomainManager DomainManager { get; private set; }
	}
}
