
using System;
using System.Windows.Forms;

namespace CustomControls
{
    public class DataGridViewCheckedComboBoxCell : DataGridViewTextBoxCell
    {
        public override Type EditType => typeof(CheckedComboBox);
        public override Type ValueType => typeof(string);
        public override object DefaultNewRowValue => "";
    }

    public class DataGridViewCheckedComboBoxColumn : DataGridViewColumn
    {
        public DataGridViewCheckedComboBoxColumn()
            : base(new DataGridViewCheckedComboBoxCell())
        {
        }
    }
}
