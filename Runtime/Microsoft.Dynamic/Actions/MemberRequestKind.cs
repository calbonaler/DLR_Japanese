namespace Microsoft.Scripting.Actions
{
	/// <summary>既定のバインダーがメンバを要求する際の操作を表します。</summary>
	public enum MemberRequestKind
	{
		/// <summary>なし</summary>
		None,
		/// <summary>メンバの取得</summary>
		Get,
		/// <summary>メンバの設定</summary>
		Set,
		/// <summary>メンバの削除</summary>
		Delete,
		/// <summary>呼び出し</summary>
		Invoke,
		/// <summary>メンバ呼び出し</summary>
		InvokeMember,
		/// <summary>変換</summary>
		Convert,
		/// <summary>演算の実行</summary>
		Operation
	}
}
