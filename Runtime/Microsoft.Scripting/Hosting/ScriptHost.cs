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
using System.Security.Permissions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>DLR ��ł̃z�X�g��\���܂��B</summary>
	/// <remarks>
	/// <see cref="Microsoft.Scripting.Hosting.ScriptHost"/> �� <see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> �ƂƂ��ɁA����̃A�v���P�[�V�����h���C���ɔz�u����܂��B
	/// �z�X�g�͂������̒ʒm���擾������ATryGetSourceUnit �� ResolveSourceUnit �Ȃǂ̂悤�ɑ�����J�X�^�}�C�Y���邽�߂ɔh���N���X�������ł��܂��B
	///
	/// �h���N���X�̃R���X�g���N�^������ <see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> �̏������Ɋ֗^����
	/// <see cref="Microsoft.Scripting.Hosting.ScriptRuntimeSetup"/> �C���X���^�X�ɂ���Ďw�肳��܂��B
	/// 
	/// �z�X�g�������[�g�ł���΁ADLR (���Ȃ킿�A<see cref="Microsoft.Scripting.Hosting.ScriptHost"/> ��)
	/// ����т��̃A�v���P�[�V�����h���C���ɐ������Ă���I�u�W�F�N�g�ւ̃A�N�Z�X�̕K�v���Ɋւ��ẮA<see cref="Microsoft.Scripting.Hosting.ScriptHost"/>
	/// �̔h���N���X�̃R���X�g���N�^�Ɉ����Ƃ��� <see cref="System.MarshalByRefObject"/> ��n�����Ƃ��ł��܂��B
	/// </remarks>
	public class ScriptHost : MarshalByRefObject
	{
		ScriptRuntime _runtime;

		/// <summary>�z�X�g���A�^�b�`����郉���^�C�����擾���܂��B</summary>
		public ScriptRuntime Runtime
		{
			get
			{
				if (_runtime == null)
					throw new InvalidOperationException("Host not initialized");
				return _runtime;
			}
			internal set
			{
				// ScriptRuntime �ɂ���Ċ��S�ɏ��������ꂽ�Ƃ��ɌĂ΂�܂��B
				Assert.NotNull(value);
				_runtime = value;
				RuntimeAttached(); // �z�X�g�����Ƀ����^�C�������p�\�ɂȂ������Ƃ�ʒm���܂��B
			}
		}

		/// <summary>�z�X�g�Ɋ֘A�t����ꂽ <see cref="Microsoft.Scripting.PlatformAdaptationLayer"/> ���擾���܂��B</summary>
		public virtual PlatformAdaptationLayer PlatformAdaptationLayer { get { return PlatformAdaptationLayer.Default; } }

		#region Notifications

		/// <summary>
		/// �֘A�t����ꂽ�����^�C���̏�����������������ɌĂяo����܂��B
		/// �z�X�g�̓A�Z���u���̃��[�h�Ȃǂ̃����^�C���̒ǉ��̏����������s���邽�߂ɂ��̃��\�b�h���I�[�o�[���C�h�ł��܂��B
		/// </summary>
		protected virtual void RuntimeAttached() { }

		/// <summary>
		/// �����^�C���ɐV�������ꂪ���[�h���ꂽ��ɌĂяo����܂��B
		/// �z�X�g�͌���G���W���̒ǉ��̏����������s���邽�߂ɂ��̃��\�b�h���I�[�o�[���C�h�ł��܂��B
		/// </summary>
		/// <param name="engine">���[�h���ꂽ������w�肵�܂��B</param>
		protected internal virtual void EngineCreated(ScriptEngine engine) { }

		#endregion

		// TODO: Figure out what is the right lifetime
		/// <summary>�Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��A�L�����ԃT�[�r�X �I�u�W�F�N�g���擾���܂��B</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}

}
