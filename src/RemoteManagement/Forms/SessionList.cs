using System;
using System.Windows.Forms;

namespace RemoteManagement.Forms
{
    public partial class SessionList : RefreshableDataDialog
    {
        public SessionList(Action onRefresh = null) : base(onRefresh)
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
