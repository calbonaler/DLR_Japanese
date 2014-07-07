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

using System.IO;
using System.Text;
using Microsoft.Scripting;
using Microsoft.Scripting.Utils;

namespace IronPython.Runtime {
    internal sealed class NoLineFeedSourceContentProvider : TextContentProvider {
        private readonly string/*!*/ _code;

        internal sealed class Reader : SourceCodeReader {

            internal Reader(string/*!*/ s)
                : base(new StringReader(s), null) {
            }

            public override string/*!*/ ReadLine() {
				StringBuilder result = new StringBuilder();
				for (int ch; (ch = Read()) != -1; )
				{
					if (ch == '\n')
						return result.ToString();
					result.Append((char)ch);
				}
				return result.Length > 0 ? result.ToString() : null;
            }

            public override bool SeekLine(int line) {
                int currentLine = 1;
                for (;;) {
                    if (currentLine == line)
						return true;
					for (; ; )
					{
						int c = Read();
						if (c == -1)
							return false;
						if (c == '\n')
							break;
					}
                    currentLine++;
                }
            }
        }        
        
        public NoLineFeedSourceContentProvider(string/*!*/ code) {
            Assert.NotNull(code);
            _code = code;
        }

        public override SourceCodeReader/*!*/ GetReader() {
            return new Reader(_code);
        }
    }
}
