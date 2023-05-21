using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utill
{
    public class HStringWriter : StringWriter
    {
        public delegate string OnWriteStringEventHandler(string savedStr, string newSTr);
        public event OnWriteStringEventHandler OnWriteStringEvent;

        public override void WriteLine(char[] buffer)
        {
            string newStr = "";
            if (OnWriteStringEvent != null)
            {
                newStr = OnWriteStringEvent(this.ToString(), new string(buffer));
            }

            this.GetStringBuilder().Clear();
            this.GetStringBuilder().Append(newStr);
        }

        public override void WriteLine(string value)
        {
            string newStr = "";
            if (OnWriteStringEvent != null)
            {
                newStr = OnWriteStringEvent(this.ToString(), value);
            }

            this.GetStringBuilder().Clear();
            this.GetStringBuilder().Append(newStr);
        }
    }
}
