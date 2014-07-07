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
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// 複数のバインダーを単一の動的サイトに結合できるバインダーを表します。
	/// このクラスの作成者は引数、定数、サブサイト式のマッピングを行い、このデータを表す <see cref="BinderMappingInfo"/> のリストを提供する必要があります。
	/// そこから、<see cref="ComboBinder"/> は結果のコードを生成するために、リストを処理するだけでよいことになります。
	/// </summary>
	public class ComboBinder : DynamicMetaObjectBinder, IEquatable<ComboBinder>
	{
		readonly BinderMappingInfo[] _metaBinders;

		/// <summary>指定されたマッピング情報を使用して、<see cref="Microsoft.Scripting.Actions.ComboBinder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binders">この <see cref="ComboBinder"/> が使用するマッピング情報を指定します。</param>
		public ComboBinder(params BinderMappingInfo[] binders) : this((ICollection<BinderMappingInfo>)binders) { }

		/// <summary>指定されたマッピング情報を使用して、<see cref="Microsoft.Scripting.Actions.ComboBinder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binders">この <see cref="ComboBinder"/> が使用するマッピング情報を指定します。</param>
		public ComboBinder(ICollection<BinderMappingInfo> binders)
		{
			Assert.NotNullItems(binders);
			_metaBinders = ArrayUtils.ToArray(binders);
		}

		/// <summary>動的操作のバインディングを実行します。</summary>
		/// <param name="target">動的操作のターゲット。</param>
		/// <param name="args">動的操作の引数の配列。</param>
		/// <returns>バインディングの結果を表す <see cref="System.Dynamic.DynamicMetaObject"/>。</returns>
		public override DynamicMetaObject Bind(DynamicMetaObject target, params DynamicMetaObject[] args)
		{
			List<DynamicMetaObject> results = new List<DynamicMetaObject>(_metaBinders.Length);
			List<Expression> steps = new List<Expression>();
			List<ParameterExpression> temps = new List<ParameterExpression>();
			var restrictions = BindingRestrictions.Empty;
			for (int i = 0; i < _metaBinders.Length; i++)
			{
				var targ = GetArguments(x => x == 0 ? target : args[x - 1], results, _metaBinders[i].MappingInfo);
				var next = _metaBinders[i].Binder.Bind(targ.First(), targ.Skip(1).ToArray());
				if (i != 0) // If the rule contains an embedded "update", replace it with a defer
					next = new DynamicMetaObject(new ReplaceUpdateVisitor { Binder = _metaBinders[i].Binder, Arguments = targ.ToArray() }.Visit(next.Expression), next.Restrictions);
				restrictions = restrictions.Merge(next.Restrictions);
				if (next.Expression.NodeType == ExpressionType.Throw)
				{
					// end of the line... the expression is throwing, none of the other binders will have an opportunity to run.
					steps.Add(next.Expression);
					break;
				}
				var tmp = Expression.Variable(next.Expression.Type, "comboTemp" + i.ToString());
				temps.Add(tmp);
				steps.Add(Expression.Assign(tmp, next.Expression));
				results.Add(new DynamicMetaObject(tmp, next.Restrictions));
			}
			return new DynamicMetaObject(Expression.Block(temps, steps), restrictions);
		}

		/// <summary>操作の結果型。</summary>
		public override Type ReturnType { get { return _metaBinders[_metaBinders.Length - 1].Binder.ReturnType; } }

		sealed class ReplaceUpdateVisitor : ExpressionVisitor
		{
			internal DynamicMetaObjectBinder Binder;
			internal DynamicMetaObject[] Arguments;
			protected override Expression VisitGoto(GotoExpression node) { return node.Target == CallSiteBinder.UpdateLabel ? Binder.Defer(Arguments).Expression : base.Visit(node); }
		}

		static IEnumerable<DynamicMetaObject> GetArguments(Func<int, DynamicMetaObject> args, IList<DynamicMetaObject> results, IEnumerable<ParameterMappingInfo> info)
		{
			return info.Select(x => x.IsAction ? results[x.ActionIndex] : (x.IsParameter ? args(x.ParameterIndex) : new DynamicMetaObject(x.Constant, BindingRestrictions.Empty, x.Constant.Value)));
		}

		/// <summary>このオブジェクトのハッシュ値を計算します。</summary>
		/// <returns>このオブジェクトのハッシュ値。</returns>
		public override int GetHashCode() { return _metaBinders.Aggregate(6551, (res, binder) => binder.MappingInfo.Aggregate(res ^ binder.Binder.GetHashCode(), (x, y) => x ^ y.GetHashCode())); }

		/// <summary>このオブジェクトが指定されたオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">比較するオブジェクトを指定します。</param>
		/// <returns>このオブジェクトと指定されたオブジェクトが等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public override bool Equals(object obj) { return Equals(obj as ComboBinder); }

		/// <summary>この <see cref="ComboBinder"/> が指定された <see cref="ComboBinder"/> と等しいかどうかを判断します。</summary>
		/// <param name="other">比較する <see cref="ComboBinder"/> を指定します。</param>
		/// <returns>この<see cref="ComboBinder"/> と指定された <see cref="ComboBinder"/> が等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool Equals(ComboBinder other)
		{
			return other != null && _metaBinders.Length == other._metaBinders.Length && Enumerable.Range(0, _metaBinders.Length).All(i =>
				_metaBinders[i].Binder.Equals(other._metaBinders[i].Binder) && _metaBinders[i].MappingInfo.Count == other._metaBinders[i].MappingInfo.Count &&
				Enumerable.Range(0, _metaBinders[i].MappingInfo.Count).All(x => _metaBinders[i].MappingInfo[x].Equals(other._metaBinders[i].MappingInfo[x]))
			);
		}
	}

	/// <summary>
	/// コンボアクション式の入力に対するマッピングを提供します。
	/// 入力は新しい動的サイトの入力、以前の <see cref="DynamicExpression"/> の入力、あるいは動的サイトの引数から取り出された <see cref="ConstantExpression"/> にマッピングできます。
	/// </summary>
	public class ParameterMappingInfo : IEquatable<ParameterMappingInfo>
	{
		ParameterMappingInfo(int param, int action, ConstantExpression fixedInput)
		{
			ParameterIndex = param;
			ActionIndex = action;
			Constant = fixedInput;
		}

		/// <summary>元の引数にマッピングされた入力を返します。</summary>
		/// <param name="index">マッピングする元の引数の位置を示す 0 から始まるインデックスを指定します。</param>
		/// <returns>元の引数にマッピングされた入力。</returns>
		public static ParameterMappingInfo Parameter(int index) { return new ParameterMappingInfo(index, -1, null); }

		/// <summary>以前のバインディング結果にマッピングされた入力を返します。</summary>
		/// <param name="index">マッピングする以前のバインディング結果の位置を示す 0 から始まるインデックスを指定します。</param>
		/// <returns>以前のバインディング結果にマッピングされた入力。</returns>
		public static ParameterMappingInfo Action(int index) { return new ParameterMappingInfo(-1, index, null); }

		/// <summary>定数にマッピングされた入力を返します。</summary>
		/// <param name="e">マッピングする定数を表す <see cref="ConstantExpression"/> を指定します。</param>
		/// <returns>定数にマッピングされた入力。</returns>
		public static ParameterMappingInfo Fixed(ConstantExpression e) { return new ParameterMappingInfo(-1, -1, e); }

		/// <summary>この入力がマッピングされている元の引数リスト内の引数の位置を取得します。</summary>
		public int ParameterIndex { get; private set; }

		/// <summary>この入力がマッピングされている以前のバインディング結果の位置を取得します。</summary>
		public int ActionIndex { get; private set; }

		/// <summary>この入力がマッピングされている定数を表す <see cref="ConstantExpression"/> を取得します。</summary>
		public ConstantExpression Constant { get; private set; }

		/// <summary>この入力が元の引数リスト内の引数にマッピングされているかどうかを示す値を取得します。</summary>
		public bool IsParameter { get { return ParameterIndex != -1; } }

		/// <summary>この入力が以前のバインディング結果にマッピングされているかどうかを示す値を取得します。</summary>
		public bool IsAction { get { return ActionIndex != -1; } }

		/// <summary>この入力が定数にマッピングされているかどうかを示す値を取得します。</summary>
		public bool IsConstant { get { return Constant != null; } }
		
		/// <summary>この入力が指定された入力と等しいかどうかを判断します。</summary>
		/// <param name="other">比較する入力を指定します。</param>
		/// <returns>この入力が指定された入力と等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool Equals(ParameterMappingInfo other)
		{
			return other != null && other.ParameterIndex == ParameterIndex && other.ActionIndex == ActionIndex &&
				(Constant != null ? other.Constant != null && Constant.Value == other.Constant.Value : other.Constant == null);
		}

		/// <summary>このオブジェクトが指定されたオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">比較するオブジェクトを指定します。</param>
		/// <returns>このオブジェクトが指定されたオブジェクトと等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public override bool Equals(object obj) { return Equals(obj as ParameterMappingInfo); }

		/// <summary>このオブジェクトのハッシュ値を計算します。</summary>
		/// <returns>このオブジェクトのハッシュ値。</returns>
		public override int GetHashCode() { return ParameterIndex.GetHashCode() ^ ActionIndex.GetHashCode() ^ (Constant != null && Constant.Value != null ? Constant.Value.GetHashCode() : 0); }

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>このオブジェクトの文字列表現。</returns>
		public override string ToString()
		{
			if (IsAction)
				return "Action" + ActionIndex.ToString();
			else if (IsParameter)
				return "Parameter" + ParameterIndex.ToString();
			else
				return Constant.Value == null ? "(null)" : Constant.Value.ToString();
		}
	}

	/// <summary>
	/// 単一のコンボバインダーに対するマッピング情報を格納します。
	/// このクラスは元の <see cref="DynamicMetaObjectBinder"/> と引数、サブサイトおよび定数からバインディングへマッピングを含んでいます。
	/// </summary>
	public class BinderMappingInfo
	{
		/// <summary>元の <see cref="DynamicMetaObjectBinder"/> を取得します。</summary>
		public DynamicMetaObjectBinder Binder { get; private set; }
		
		/// <summary>引数、サブサイトおよび定数からバインディングへのマッピング情報を取得します。</summary>
		public IList<ParameterMappingInfo> MappingInfo { get; private set; }

		/// <summary>指定されたバインダーとマッピング情報を使用して、<see cref="Microsoft.Scripting.Actions.BinderMappingInfo"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binder">元のバインダーを指定します。</param>
		/// <param name="mappingInfo">引数、サブサイトおよび定数からバインディングへのマッピング情報を指定します。</param>
		public BinderMappingInfo(DynamicMetaObjectBinder binder, IList<ParameterMappingInfo> mappingInfo)
		{
			Binder = binder;
			MappingInfo = mappingInfo;
		}

		/// <summary>指定されたバインダーとマッピング情報を使用して、<see cref="Microsoft.Scripting.Actions.BinderMappingInfo"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binder">元のバインダーを指定します。</param>
		/// <param name="mappingInfo">引数、サブサイトおよび定数からバインディングへのマッピング情報を指定します。</param>
		public BinderMappingInfo(DynamicMetaObjectBinder binder, params ParameterMappingInfo[] mappingInfo) : this(binder, (IList<ParameterMappingInfo>)mappingInfo) { }

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>このオブジェクトの文字列表現。</returns>
		public override string ToString() { return Binder.ToString() + " " + string.Join(", ", MappingInfo.Select(x => x.ToString())); }
	}
}
