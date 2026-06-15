using System.ComponentModel.Composition;
using System.Windows;

namespace NinaHA.Plugin.SequenceItems {

    [Export(typeof(ResourceDictionary))]
    public partial class SequenceItemTemplates : ResourceDictionary {

        public SequenceItemTemplates() {
            InitializeComponent();
        }
    }
}
