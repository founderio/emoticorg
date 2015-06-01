using System.Windows.Forms;

namespace Emoticorg
{
    public class DoubleBufferedListView : ListView
    {
        public DoubleBufferedListView()
            : base()
        {
            this.DoubleBuffered = true;
        }
    }
}
