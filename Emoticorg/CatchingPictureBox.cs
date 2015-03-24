using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Emoticorg
{
    public class CatchingPictureBox : PictureBox
    {
        protected override void OnPaint(PaintEventArgs pe)
        {
            try
            {
                base.OnPaint(pe);
            }
            catch
            {
                Console.WriteLine("ERR");
            }
        }
    }
}
