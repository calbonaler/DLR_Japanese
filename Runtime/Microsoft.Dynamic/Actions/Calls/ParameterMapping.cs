/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * ironruby@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
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
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>メソッドに渡される仮引数と実引数の関連付けを行う方法を提供します。</summary>
	public sealed class ParameterMapping
	{
		readonly OverloadResolver _resolver;
		readonly IList<string> _argNames;

		readonly List<ParameterWrapper> _parameters = new List<ParameterWrapper>();
		readonly List<ArgBuilder> _arguments;

		List<int> _returnArgs;
		InstanceBuilder _instanceBuilder;
		ReturnBuilder _returnBuilder;

		readonly List<ArgBuilder> _defaultArguments = new List<ArgBuilder>();
		bool _hasByRef;
		bool _hasDefaults;
		ParameterWrapper _paramsDict;

		/// <summary>この <see cref="ParameterMapping"/> が作成されたオーバーロードを表す <see cref="OverloadInfo"/> を取得します。</summary>
		public OverloadInfo Overload { get; private set; }

		/// <summary>消費する次の実引数の位置を示す 0 から始まるインデックスを取得します。</summary>
		public int ArgIndex { get; private set; }

		/// <summary>オーバーロードを解決する <see cref="OverloadResolver"/>、対象のオーバーロード、名前付き引数の名前を使用して、<see cref="Microsoft.Scripting.Actions.Calls.ParameterMapping"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="resolver">オーバーロードの解決に使用する <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="method">対象となるオーバーロードを表す <see cref="OverloadInfo"/> を指定します。</param>
		/// <param name="argNames">名前付き引数の名前のリストを指定します。</param>
		internal ParameterMapping(OverloadResolver resolver, OverloadInfo method, IList<string> argNames)
		{
			Assert.NotNull(resolver, method);
			_resolver = resolver;
			Overload = method;
			_argNames = argNames;
			_arguments = new List<ArgBuilder>(method.ParameterCount);
		}

		/// <summary>参照渡し引数を縮退するかどうかを指定して、オーバーロードの特定の仮引数を実引数にマッピングします。</summary>
		/// <param name="reduceByRef">参照渡し引数を縮退するかどうかを示す値を指定します。</param>
		internal void MapParameters(bool reduceByRef)
		{
			if (reduceByRef)
			{
				_returnArgs = new List<int>();
				if (Overload.ReturnType != typeof(void))
					_returnArgs.Add(-1);
			}
			var specialParameters = _resolver.MapSpecialParameters(this);
			_instanceBuilder = _instanceBuilder ?? new InstanceBuilder(-1);
			foreach (var parameter in Overload.Parameters.Where(x => !IsSpecialParameter(specialParameters, x.Position)))
			{
				if (reduceByRef)
					MapParameterReduceByRef(parameter);
				else
					MapParameter(parameter);
			}
			_returnBuilder = MakeReturnBuilder(specialParameters);
		}

		bool IsSpecialParameter(BitArray specialParameters, int infoIndex) { return specialParameters != null && infoIndex < specialParameters.Length && specialParameters[infoIndex]; }

		/// <summary>指定された <see cref="InstanceBuilder"/> を使用して、次の実引数を消費します。</summary>
		/// <param name="builder">実引数を消費してインスタンスを提供する <see cref="InstanceBuilder"/> を指定します。</param>
		public void AddInstanceBuilder(InstanceBuilder builder)
		{
			ContractUtils.Requires(_instanceBuilder == null);
			ContractUtils.Requires(builder.HasValue, "builder");
			_instanceBuilder = builder;
			ArgIndex += builder.ConsumedArgumentCount;
		}

		// TODO: We might want to add bitmap of all consumed arguments and allow to consume an arbitrary argument, not just the next one.
		/// <summary>指定された <see cref="ArgBuilder"/> を使用して、次の実引数を消費します。</summary>
		/// <param name="builder">実引数を消費して実引数の値を提供する <see cref="ArgBuilder"/> を指定します。</param>
		public void AddBuilder(ArgBuilder builder)
		{
			ContractUtils.Requires(builder.ConsumedArgumentCount != ArgBuilder.AllArguments, "builder");
			_arguments.Add(builder);
			ArgIndex += builder.ConsumedArgumentCount;
		}

		/// <summary>このマッピングに新しい仮引数を追加します。</summary>
		/// <param name="parameter">追加する仮引数を指定します。</param>
		public void AddParameter(ParameterWrapper parameter) { _parameters.Add(parameter); }

		/// <summary>指定された仮引数に関する情報と一致する仮引数を実引数にマッピングします。</summary>
		/// <param name="pi">仮引数を探索するための情報を格納している <see cref="ParameterInfo"/> を指定します。</param>
		public void MapParameter(ParameterInfo pi)
		{
			int nameIndex = _argNames.IndexOf(pi.Name);
			// positional argument, we simply consume the next argument
			// keyword argument, we just tell the simple arg builder to consume arg 0.
			// KeywordArgBuilder will then pass in the correct single argument based upon the actual argument number provided by the user.
			int indexForArgBuilder = nameIndex == -1 ? ArgIndex++ : 0;
			// if the parameter is default we need to build a default arg builder and then
			// build a reduced method at the end.  
			if (!pi.IsMandatory())
			{
				// We need to build the default builder even if we have a parameter for it already to
				// get good consistency of our error messages.  But consider a method like 
				// def foo(a=1, b=2) and the user calls it as foo(b=3). Then adding the default
				// value breaks an otherwise valid call.  This is because we only generate MethodCandidates
				// filling in the defaults from right to left (so the method - 1 arg requires a,
				// and the method minus 2 args requires b).  So we only add the default if it's 
				// a positional arg or we don't already have a default value.
				if (nameIndex == -1 || !_hasDefaults)
				{
					_defaultArguments.Add(new DefaultArgBuilder(pi));
					_hasDefaults = true;
				}
				else
					_defaultArguments.Add(null);
			}
			else if (_defaultArguments.Count > 0) // non-contigious default parameter
				_defaultArguments.Add(null);
			ArgBuilder ab;
			if (pi.ParameterType.IsByRef)
			{
				_hasByRef = true;
				var elementType = pi.ParameterType.GetElementType();
				ab = new ReferenceArgBuilder(pi, elementType, indexForArgBuilder);
				_parameters.Add(new ParameterWrapper(pi, ab.Type, pi.Name, ParameterBindingFlags.ProhibitNull));
			}
			else if (pi.Position == 0 && Overload.IsExtension)
			{
				_parameters.Add(new ParameterWrapper(pi, pi.ParameterType, pi.Name, ParameterBindingFlags.IsHidden));
				ab = new SimpleArgBuilder(pi, pi.ParameterType, indexForArgBuilder, false, false);
			}
			else
				ab = AddSimpleParameterMapping(pi, indexForArgBuilder);
			if (nameIndex == -1)
				_arguments.Add(ab);
			else
			{
				Debug.Assert(KeywordArgBuilder.BuilderExpectsSingleParameter(ab));
				_arguments.Add(new KeywordArgBuilder(ab, _argNames.Count, nameIndex));
			}
		}

		/// <summary>out 引数を戻り値に、ref 引数を <see cref="StrongBox&lt;T&gt;"/> を受け付けない引数にマッピングします。</summary>
		void MapParameterReduceByRef(ParameterInfo pi)
		{
			Debug.Assert(_returnArgs != null);
			// TODO:
			// Is this reduction necessary? What if 
			// 1) we had an implicit conversion StrongBox<T> -> T& and 
			// 2) all out parameters were treated as optional StrongBox<T> parameters? (if not present we return the result in a return value)
			int indexForArgBuilder = 0;
			int nameIndex = -1;
			if (!pi.IsOutParameter())
			{
				nameIndex = _argNames.IndexOf(pi.Name);
				if (nameIndex == -1)
					indexForArgBuilder = ArgIndex++;
			}
			ArgBuilder ab;
			if (pi.IsOutParameter())
			{
				_returnArgs.Add(_arguments.Count);
				ab = new OutArgBuilder(pi);
			}
			else if (pi.ParameterType.IsByRef)
			{
				// if the parameter is marked as [In] it is not returned.
				if ((pi.Attributes & (ParameterAttributes.In | ParameterAttributes.Out)) != ParameterAttributes.In)
					_returnArgs.Add(_arguments.Count);
				_parameters.Add(new ParameterWrapper(pi, pi.ParameterType.GetElementType(), pi.Name, ParameterBindingFlags.None));
				ab = new ReturnReferenceArgBuilder(pi, indexForArgBuilder);
			}
			else
				ab = AddSimpleParameterMapping(pi, indexForArgBuilder);
			if (nameIndex == -1)
				_arguments.Add(ab);
			else
			{
				Debug.Assert(KeywordArgBuilder.BuilderExpectsSingleParameter(ab));
				_arguments.Add(new KeywordArgBuilder(ab, _argNames.Count, nameIndex));
			}
		}

		ParameterWrapper CreateParameterWrapper(ParameterInfo info)
		{
			var paramArray = Overload.IsParamArray(info.Position);
			var paramDict = !paramArray && Overload.IsParamDictionary(info.Position);
			return new ParameterWrapper(
				info,
				info.ParameterType,
				info.Name,
				(Overload.ProhibitsNull(info.Position) ? ParameterBindingFlags.ProhibitNull : 0) |
				((paramArray || paramDict) && Overload.ProhibitsNullItems(info.Position) ? ParameterBindingFlags.ProhibitNullItems : 0) |
				(paramArray ? ParameterBindingFlags.IsParamArray : 0) | (paramDict ? ParameterBindingFlags.IsParamDictionary : 0)
			);
		}

		SimpleArgBuilder AddSimpleParameterMapping(ParameterInfo info, int index)
		{
			var param = CreateParameterWrapper(info);
			if (param.IsParamDict)
				_paramsDict = param;
			else
				_parameters.Add(param);
			return new SimpleArgBuilder(info, info.ParameterType, index, param.IsParamArray, param.IsParamDict);
		}

		/// <summary>現在の <see cref="ParameterMapping"/> の状態に対応する <see cref="MethodCandidate"/> を作成します。</summary>
		/// <returns>現在の <see cref="ParameterMapping"/> の状態に対応する <see cref="MethodCandidate"/>。</returns>
		internal MethodCandidate CreateCandidate() { return new MethodCandidate(_resolver, Overload, _parameters, _paramsDict, _returnBuilder, _instanceBuilder, _arguments, null); }

		/// <summary>現在の <see cref="ParameterMapping"/> の状態に対応し、参照渡しを縮退した <see cref="MethodCandidate"/> を作成します。</summary>
		/// <returns>現在の <see cref="ParameterMapping"/> の状態に対応し、参照渡しを縮退した <see cref="MethodCandidate"/>。</returns>
		internal MethodCandidate CreateByRefReducedCandidate()
		{
			if (!_hasByRef)
				return null;
			var reducedMapping = new ParameterMapping(_resolver, Overload, _argNames);
			reducedMapping.MapParameters(true);
			return reducedMapping.CreateCandidate();
		}

		#region Candidates with Default Parameters

		/// <summary>あらゆるすべての数の既定値を使用した <see cref="MethodCandidate"/> を返します。</summary>
		/// <returns>あらゆるすべての数の既定値を使用した <see cref="MethodCandidate"/>。</returns>
		internal IEnumerable<MethodCandidate> CreateDefaultCandidates()
		{
			// if the left most default we'll use is not present then don't add a default.  This happens in cases such as:
			// a(a=1, b=2, c=3) and then call with a(a=5, c=3).  We'll come through once for c (no default, skip),
			// once for b (default present, emit) and then a (no default, skip again).  W/o skipping we'd generate the same
			// method multiple times.  This also happens w/ non-contigious default values, e.g. foo(a, b=3, c) where we don't want
			// to generate a default candidate for just c which matches the normal method.
			if (_hasDefaults)
				return Enumerable.Range(1, _defaultArguments.Count).Where(x => _defaultArguments[_defaultArguments.Count - x] != null).Select(x => CreateDefaultCandidate(x));
			return Enumerable.Empty<MethodCandidate>();
		}

		MethodCandidate CreateDefaultCandidate(int defaultsUsed)
		{
			List<ArgBuilder> defaultArgBuilders = new List<ArgBuilder>(_arguments);
			var necessaryParams = _parameters.GetRange(0, _parameters.Count - defaultsUsed);
			for (int curDefault = 0; curDefault < defaultsUsed; curDefault++)
			{
				int readIndex = _defaultArguments.Count - defaultsUsed + curDefault;
				if (_defaultArguments[readIndex] != null)
					defaultArgBuilders[defaultArgBuilders.Count - defaultsUsed + curDefault] = _defaultArguments[readIndex];
				else
					necessaryParams.Add(_parameters[_parameters.Count - defaultsUsed + curDefault]);
			}
			// shift any arguments forward that need to be...
			for (int i = 0, curArg = Overload.IsStatic ? 0 : 1; i < defaultArgBuilders.Count; i++)
			{
				var sab = defaultArgBuilders[i] as SimpleArgBuilder;
				if (sab != null)
					defaultArgBuilders[i] = sab.MakeCopy(curArg++);
			}
			return new MethodCandidate(_resolver, Overload, necessaryParams, _paramsDict, _returnBuilder, _instanceBuilder, defaultArgBuilders, null);
		}

		#endregion

		#region ReturnBuilder, Member Assigned Arguments

		ReturnBuilder MakeReturnBuilder(BitArray specialParameters)
		{
			var returnBuilder = _returnArgs != null ? new ByRefReturnBuilder(_returnArgs) : new ReturnBuilder(Overload.ReturnType);
			if (_argNames.Count > 0 && _resolver.AllowMemberInitialization(Overload))
			{
				var unusedNames = _argNames.Where(x => !Overload.Parameters.Any(y => !IsSpecialParameter(specialParameters, y.Position) && y.Name == x)).ToArray();
				var bindableMembers = GetBindableMembers(returnBuilder.ReturnType, unusedNames);
				if (unusedNames.Length == bindableMembers.Count)
				{
					List<int> nameIndices = new List<int>();
					foreach (var mi in bindableMembers)
					{
						_parameters.Add(new ParameterWrapper(null, mi.MemberType == MemberTypes.Property ? ((PropertyInfo)mi).PropertyType : ((FieldInfo)mi).FieldType, mi.Name, ParameterBindingFlags.None));
						nameIndices.Add(_argNames.IndexOf(mi.Name));
					}
					return new KeywordConstructorReturnBuilder(
						returnBuilder,
						_argNames.Count,
						nameIndices.ToArray(),
						bindableMembers.ToArray(),
						_resolver.Binder.PrivateBinding
					);
				}
			}
			return returnBuilder;
		}

		static List<MemberInfo> GetBindableMembers(Type returnType, IEnumerable<string> unusedNames)
		{
			List<MemberInfo> bindableMembers = new List<MemberInfo>();
			foreach (var name in unusedNames)
			{
				var members = returnType.GetMember(name);
				for (var curType = returnType; members.Length != 1 && curType != null; curType = curType.BaseType)
				{
					// see if we have a single member defined as the closest level
					members = curType.GetMember(name, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.SetField | BindingFlags.SetProperty | BindingFlags.Instance);
					if (members.Length > 1)
						break;
				}
				if (members.Length == 1 && (members[0].MemberType == MemberTypes.Property || members[0].MemberType == MemberTypes.Field))
					bindableMembers.Add(members[0]);
			}
			return bindableMembers;
		}

		#endregion
	}
}
