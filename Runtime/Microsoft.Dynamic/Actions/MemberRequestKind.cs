namespace Microsoft.Scripting.Actions
{
	/// <summary>����̃o�C���_�[�������o��v������ۂ̑����\���܂��B</summary>
	public enum MemberRequestKind
	{
		/// <summary>�Ȃ�</summary>
		None,
		/// <summary>�����o�̎擾</summary>
		Get,
		/// <summary>�����o�̐ݒ�</summary>
		Set,
		/// <summary>�����o�̍폜</summary>
		Delete,
		/// <summary>�Ăяo��</summary>
		Invoke,
		/// <summary>�����o�Ăяo��</summary>
		InvokeMember,
		/// <summary>�ϊ�</summary>
		Convert,
		/// <summary>���Z�̎��s</summary>
		Operation
	}
}
