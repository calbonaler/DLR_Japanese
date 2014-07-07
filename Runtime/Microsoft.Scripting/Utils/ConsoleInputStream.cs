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
using System.Diagnostics;
using System.IO;

namespace Microsoft.Scripting.Utils
{
	/// <summary>
	/// コンソール入力ストリーム (Console.OpenStandardInput) には少量のデータを読み取った際に発生するバグがあります。
	/// このクラスは標準入力ストリームを十分な量のデータが読み取られることを保証するバッファでラップします。
	/// </summary>
	public sealed class ConsoleInputStream : Stream
	{
		/// <summary>標準入力ストリームの唯一のインスタンスを取得します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly ConsoleInputStream Instance = new ConsoleInputStream();

		// 安全のため 0x1000 を使用します。 (MSVCRT は標準入力ストリームバッファのためにこの値を使用します)
		const int MinimalBufferSize = 0x1000;

		Stream _input;
		object _lock = new object();
		byte[] _buffer = new byte[MinimalBufferSize];
		int _bufferPos;
		int _bufferSize;

		ConsoleInputStream() { _input = Console.OpenStandardInput(); }

		/// <summary>現在のストリームが読み取りをサポートするかどうかを示す値を取得します。</summary>
		public override bool CanRead { get { return true; } }

		/// <summary>現在のストリームからバイト シーケンスを読み取り、読み取ったバイト数の分だけストリームの位置を進めます。</summary>
		/// <param name="buffer">バイト配列。 このメソッドが戻るとき、指定したバイト配列の <paramref name="offset"/> から (<paramref name="offset"/> + <paramref name="count"/> -1) までの値が、現在のソースから読み取られたバイトに置き換えられます。</param>
		/// <param name="offset">現在のストリームから読み取ったデータの格納を開始する位置を示す <paramref name="buffer"/> 内のバイト オフセット。インデックス番号は 0 から始まります。</param>
		/// <param name="count">現在のストリームから読み取る最大バイト数。</param>
		/// <returns>バッファーに読み取られた合計バイト数。 要求しただけのバイト数を読み取ることができなかった場合、この値は要求したバイト数より小さくなります。ストリームの末尾に到達した場合は 0 (ゼロ) になることがあります。</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			int result;
			lock (_lock)
			{
				if (_bufferSize > 0)
				{
					result = Math.Min(count, _bufferSize);
					Buffer.BlockCopy(_buffer, _bufferPos, buffer, offset, result);
					_bufferPos += result;
					_bufferSize -= result;
					offset += result;
					count -= result;
				}
				else
					result = 0;
				if (count > 0)
				{
					Debug.Assert(_bufferSize == 0);
					if (count < MinimalBufferSize)
					{
						int bytesRead = _input.Read(_buffer, 0, MinimalBufferSize);
						int bytesToReturn = Math.Min(bytesRead, count);
						Buffer.BlockCopy(_buffer, 0, buffer, offset, bytesToReturn);
						_bufferSize = bytesRead - bytesToReturn;
						_bufferPos = bytesToReturn;
						result += bytesToReturn;
					}
					else
						result += _input.Read(buffer, offset, count);
				}
			}
			return result;
		}

		/// <summary>現在のストリームがシークをサポートするかどうかを示す値を取得します。</summary>
		public override bool CanSeek { get { return false; } }

		/// <summary>現在のストリームが書き込みをサポートするかどうかを示す値を取得します。</summary>
		public override bool CanWrite { get { return false; } }

		/// <summary>ストリームに対応するすべてのバッファーをクリアし、バッファー内のデータを基になるデバイスに書き込みます。</summary>
		public override void Flush() { throw new NotSupportedException(); }

		/// <summary>ストリームの長さをバイト単位で取得します。</summary>
		public override long Length { get { throw new NotSupportedException(); } }

		/// <summary>現在のストリーム内の位置を取得または設定します。</summary>
		public override long Position
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		/// <summary>現在のストリーム内の位置を設定します。</summary>
		/// <param name="offset"><paramref name="origin"/> パラメーターからのバイト オフセット。</param>
		/// <param name="origin">新しい位置を取得するために使用する参照ポイントを示す <see cref="System.IO.SeekOrigin"/> 型の値。</param>
		/// <returns>現在のストリーム内の新しい位置。</returns>
		public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }

		/// <summary>現在のストリームの長さを設定します。</summary>
		/// <param name="value">現在のストリームの希望の長さ (バイト数)。</param>
		public override void SetLength(long value) { throw new NotSupportedException(); }

		/// <summary>現在のストリームにバイト シーケンスを書き込み、書き込んだバイト数の分だけストリームの現在位置を進めます。</summary>
		/// <param name="buffer">バイト配列。 このメソッドは、<paramref name="buffer"/> から現在のストリームに、<paramref name="count"/> で指定されたバイト数だけコピーします。</param>
		/// <param name="offset">現在のストリームへのバイトのコピーを開始する位置を示す <paramref name="buffer"/> 内のバイト オフセット。インデックス番号は 0 から始まります。</param>
		/// <param name="count">現在のストリームに書き込むバイト数。</param>
		public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
	}
}