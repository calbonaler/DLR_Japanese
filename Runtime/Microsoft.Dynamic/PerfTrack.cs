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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;

namespace Microsoft.Scripting
{
	/// <summary>
	/// ���Ԃ̂����鑀��ɑ΂���p�t�H�[�}���X�J�E���g�������Ɏ��W����ۂɎg�p����郁�\�b�h�Q��񋟂��܂��B
	/// �ʏ킱�̂悤�ȑ���̓��t���N�V�����܂��̓R�[�h�����Ɋ֌W���鑀����Ӗ����܂��B
	/// �����ɂ킽���Ă��̏������ʏ�̃p�t�H�[�}���X�J�E���^�[�A�[�L�e�N�`���Ɋ܂܂�邩���m���߂�K�v������܂��B
	/// </summary>
	public static class PerfTrack
	{
		/// <summary>�p�t�H�[�}���X�C�x���g�̃J�e�S����\���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")] // TODO: fix
		public enum Category
		{
			/// <summary>�v���Ȓ����̂��߂̈ꎞ�I�ȃJ�e�S����\���܂��B</summary>
			Temporary,
			/// <summary>�^�I�u�W�F�N�g�ɑ΂��鏈����\���܂��B</summary>
			ReflectedTypes,
			/// <summary>��O�̃X���[��\���܂��B</summary>
			Exceptions,
			/// <summary>�v���p�e�B�̎擾�܂��͐ݒ��\���܂��B</summary>
			Properties,
			/// <summary>�t�B�[���h�̎擾�܂��͐ݒ��\���܂��B</summary>
			Fields,
			/// <summary>MethodBase.Invoke ��ʂ������\�b�h�Ăяo����\���܂��B</summary>
			Methods,
			/// <summary>ReflectOptimizer ��ʂ��ăR���p�C�����ꂽ���\�b�h��\���܂��B</summary>
			Compiler,
			/// <summary>�f���Q�[�g�ɑ΂���V�������\�b�h���쐬�������Ƃ�\���܂��B</summary>
			DelegateCreate,
			/// <summary>�����ɂ��A�N�Z�X��\���܂��B</summary>
			DictInvoke,
			/// <summary>�^�ɑ΂��鉉�Z�q�Ăяo����\���܂��B</summary>
			OperatorInvoke,
			/// <summary>�K�v�ȏ�Ɋ��蓖�Ă����Ȃ���΂Ȃ�Ȃ����z�I�ł͂Ȃ��A���S���Y���̑��݂��Ă���ꏊ��\���܂��B</summary>
			OverAllocate,
			/// <summary>�K���܂��̓A�N�V�����Ɋ֘A���鑀���\���܂��B</summary>
			Rules,
			/// <summary>�K�����]�����ꂽ���Ƃ�\���܂��B</summary>
			RuleEvaluation,
			/// <summary>�K�����o�C���h���ꂽ���Ƃ�\���܂��B</summary>
			Binding,
			/// <summary>�ᑬ�ȃo�C���f�B���O��\���܂��B</summary>
			BindingSlow,
			/// <summary>�����ȃo�C���f�B���O��\���܂��B</summary>
			BindingFast,
			/// <summary>�K��������̌^�̃^�[�Q�b�g�ɑ΂��ăo�C���h���ꂽ���Ƃ�\���܂��B</summary>
			BindingTarget,
			/// <summary>�C�ӂ̃p�t�H�[�}���X�J�E���g�C�x���g��\���܂��B</summary>
			Count
		}

		[MultiRuntimeAware]
		static int totalEvents;
		static readonly Dictionary<Category, Dictionary<string, int>> _events = MakeEventsDictionary();
		static readonly Dictionary<Category, int> summaryStats = new Dictionary<Category, int>();

		static Dictionary<Category, Dictionary<string, int>> MakeEventsDictionary()
		{
			var result = new Dictionary<Category, Dictionary<string, int>>();
			for (int i = 0; i <= (int)Category.Count; i++)
				result[(Category)i] = new Dictionary<string, int>();
			return result;
		}

		/// <summary>�w�肳�ꂽ�q�X�g�O�����̓��e��W���o�͂ɕ\�����܂��B�q�X�g�O�����͒l�ɂ���ď����ɕ��ёւ����܂��B</summary>
		/// <typeparam name="TKey">�q�X�g�O�����̃L�[�̌^���w�肵�܂��B</typeparam>
		/// <param name="histogram">�o�͂���q�X�g�O�������w�肵�܂��B</param>
		public static void DumpHistogram<TKey>(IDictionary<TKey, int> histogram) { DumpHistogram(histogram, Console.Out); }

		/// <summary>�w�肳�ꂽ�q�X�g�O�����̓��e���w�肳�ꂽ <see cref="TextWriter"/> �ɏ������݂܂��B�q�X�g�O�����͒l�ɂ���ď����ɕ��ёւ����܂��B</summary>
		/// <typeparam name="TKey">�q�X�g�O�����̃L�[�̌^���w�肵�܂��B</typeparam>
		/// <param name="histogram">�o�͂���q�X�g�O�������w�肵�܂��B</param>
		/// <param name="output">�q�X�g�O�������o�͂��� <see cref="TextWriter"/> ���w�肵�܂��B</param>
		public static void DumpHistogram<TKey>(IDictionary<TKey, int> histogram, TextWriter output)
		{
			foreach (var kvp in histogram.OrderBy(x => x.Value))
				output.WriteLine("{0} {1}", kvp.Key, kvp.Value);
		}

		/// <summary>2 �̃q�X�g�O�����̓��e�� 1 �ɓ������܂��B</summary>
		/// <typeparam name="TKey">�q�X�g�O�����̃L�[�̌^���w�肵�܂��B</typeparam>
		/// <param name="result">�������ꂽ���ʂ��������܂��q�X�g�O�������w�肵�܂��B</param>
		/// <param name="addend"><paramref name="result"/> �ɓ�������q�X�g�O�������w�肵�܂��B</param>
		public static void AddHistograms<TKey>(IDictionary<TKey, int> result, IDictionary<TKey, int> addend)
		{
			int value;
			foreach (var entry in addend)
				result[entry.Key] = entry.Value + (result.TryGetValue(entry.Key, out value) ? value : 0);
		}

		/// <summary>�q�X�g�O�����̎w�肳�ꂽ�L�[�ɑ΂���l�� 1 ���������܂��B</summary>
		/// <typeparam name="TKey">�q�X�g�O�����̃L�[�̌^���w�肵�܂��B</typeparam>
		/// <param name="histogram">�l�𑝉�������q�X�g�O�������w�肵�܂��B</param>
		/// <param name="key">�l�𑝉�������G���g���������L�[���w�肵�܂��B</param>
		public static void IncrementEntry<TKey>(IDictionary<TKey, int> histogram, TKey key)
		{
			int value;
			histogram[key] = histogram.TryGetValue(key, out value) ? value + 1 : 1;
		}

		/// <summary>���̃N���X�Ŏ��W�����p�t�H�[�}���X���v����W���o�͂ɕ\�����܂��B</summary>
		public static void DumpStats() { DumpStats(Console.Out); }

		/// <summary>���̃N���X�Ŏ��W�����p�t�H�[�}���X���v�����w�肳�ꂽ <see cref="TextWriter"/> �ɏ������݂܂��B</summary>
		/// <param name="output">���v������������ <see cref="TextWriter"/> ���w�肵�܂��B</param>
		public static void DumpStats(TextWriter output)
		{
			if (totalEvents == 0)
				return;
			// numbers from AMD Opteron 244 1.8 Ghz, 2.00GB of ram, running on IronPython 1.0 Beta 4 against Whidbey RTM.
			const double CALL_TIME = 0.0000051442355;
			const double THROW_TIME = 0.000025365656;
			const double FIELD_TIME = 0.0000018080093;
			output.WriteLine();
			output.WriteLine("---- ���\�̏ڍ� ----");
			output.WriteLine();
			foreach (var kvp in _events)
			{
				if (kvp.Value.Count > 0)
				{
					output.WriteLine("�J�e�S�� : " + kvp.Key);
					DumpHistogram(kvp.Value, output);
					output.WriteLine();
				}
			}
			output.WriteLine();
			output.WriteLine("---- ���\�̊T�v ----");
			output.WriteLine();
			double knownTimes = 0;
			foreach (var kvp in summaryStats)
			{
				switch (kvp.Key)
				{
					case Category.Exceptions:
						output.WriteLine("�S��O ({0}) = {1}  (�X���[���� = ~{2} secs)", kvp.Key, kvp.Value, kvp.Value * THROW_TIME);
						knownTimes += kvp.Value * THROW_TIME;
						break;
					case Category.Fields:
						output.WriteLine("�S�t�B�[���h = {0} (���� = ~{1} secs)", kvp.Value, kvp.Value * FIELD_TIME);
						knownTimes += kvp.Value * FIELD_TIME;
						break;
					case Category.Methods:
						output.WriteLine("�S�Ăяo�� = {0} (�Ăяo������ = ~{1} secs)", kvp.Value, kvp.Value * CALL_TIME);
						knownTimes += kvp.Value * CALL_TIME;
						break;
					//case Categories.Properties:
					default:
						output.WriteLine("�S�� {1} = {0}", kvp.Value, kvp.Key);
						break;
				}
			}
			output.WriteLine();
			output.WriteLine("�S�̂̊��m�̎���: {0}", knownTimes);
		}

		/// <summary>�w�肳�ꂽ�J�e�S���̃C�x���g�̔������L�^���܂��B</summary>
		/// <param name="category">���������C�x���g�̃J�e�S�����w�肵�܂��B</param>
		/// <param name="key">���������C�x���g�̏ڍׂ������L�[���w�肵�܂��B</param>
		[Conditional("DEBUG")]
		public static void NoteEvent(Category category, object key)
		{
			if (!DebugOptions.TrackPerformance)
				return;
			var categoryEvents = _events[category];
			totalEvents++;
			lock (categoryEvents)
			{
				var ex = key as Exception;
				var name = ex != null ? ex.GetType().ToString() : key.ToString();
				int v;
				categoryEvents[name] = !categoryEvents.TryGetValue(name, out v) ? 1 : v + 1;
				summaryStats[category] = !summaryStats.TryGetValue(category, out v) ? 1 : v + 1;
			}
		}
	}

	/// <summary>
	/// ���̃N���X�͂��̃A�Z���u�����Ŏg�p���������I�ȃf�o�b�O�I�v�V������ێ����܂��B
	/// �����̃I�v�V�����͊��ϐ� DLR_{option-name} ��ʂ��Đݒ肷�邱�Ƃ��ł��܂��B
	/// �u�[���l�̃I�v�V������ "true" �� true�A����ȊO�� false �Ƃ݂Ȃ���܂��B
	/// 
	/// �����̃I�v�V�����͓����I�ȃf�o�b�O�̂��߂ɂ̂ݑ��݂��A�ǂ̂悤�ȃp�u���b�N API ��ʂ��Ă����J����ׂ��ł͂���܂���B
	/// </summary>
	static class DebugOptions
	{
		static bool ReadOption(string name) { return "true".Equals(ReadString(name), StringComparison.OrdinalIgnoreCase); }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name")]
		static bool ReadDebugOption(string name)
		{
#if DEBUG
			return ReadOption(name);
#else
            return false;
#endif
		}

		static string ReadString(string name)
		{
			try { return Environment.GetEnvironmentVariable("DLR_" + name); }
			catch (SecurityException) { return null; }
		}

		readonly static bool _trackPerformance = ReadDebugOption("TrackPerformance");

		internal static bool TrackPerformance { get { return _trackPerformance; } }
	}
}
