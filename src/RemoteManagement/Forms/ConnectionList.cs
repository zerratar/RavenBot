using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteManagement.Forms
{
    public class ConnectionList : RefreshableDataDialog
    {
        public ConnectionList(Action onRefresh = null) : base(onRefresh)
        {
        }

        public override void AddData(string data)
        {
            var columns = data.Split('\t');
            BeginInvoke(() =>
            {
                var lvi = new ListViewItem(columns);
                listItems.Items.Add(lvi);
                listItems.Refresh();
                this.Invalidate();
            });
        }
    }
}
