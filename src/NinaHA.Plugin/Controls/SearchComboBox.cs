using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace NinaHA.Plugin.Controls {

    /// <summary>
    /// An editable combo box with case-insensitive "contains" filtering: as the user types, the drop-down
    /// shows only matching items. Items are strings; the chosen value is the editable <see cref="ComboBox.Text"/>,
    /// so bind <c>Text</c> to the target property and <see cref="SourceItems"/> to the candidate list.
    /// Inherits the standard ComboBox theme.
    /// </summary>
    public class SearchComboBox : ComboBox {

        static SearchComboBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchComboBox), new FrameworkPropertyMetadata(typeof(ComboBox)));
        }

        public static readonly DependencyProperty SourceItemsProperty = DependencyProperty.Register(
            nameof(SourceItems), typeof(IEnumerable), typeof(SearchComboBox),
            new PropertyMetadata(null, OnSourceItemsChanged));

        public IEnumerable SourceItems {
            get => (IEnumerable)GetValue(SourceItemsProperty);
            set => SetValue(SourceItemsProperty, value);
        }

        // A per-control view so multiple boxes can filter the same underlying collection independently.
        private readonly CollectionViewSource viewSource = new CollectionViewSource();
        private string filterText = string.Empty;
        private bool suppressFilter;

        public SearchComboBox() {
            IsEditable = true;
            IsTextSearchEnabled = false;
            StaysOpenOnEdit = true;
            AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnTextChanged));
        }

        private static void OnSourceItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var box = (SearchComboBox)d;
            box.viewSource.Source = e.NewValue;
            if (box.viewSource.View != null) {
                box.viewSource.View.Filter = box.Matches;
            }
            box.ItemsSource = box.viewSource.View;
        }

        private bool Matches(object item) {
            if (string.IsNullOrEmpty(filterText)) {
                return true;
            }
            return item?.ToString()?.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e) {
            if (suppressFilter) {
                return;
            }
            filterText = Text ?? string.Empty;
            viewSource.View?.Refresh();
            if (!string.IsNullOrEmpty(filterText) && !IsDropDownOpen) {
                IsDropDownOpen = true;
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e) {
            base.OnSelectionChanged(e);
            // When an item is picked, clear the filter so the full list is available next time.
            if (SelectedItem != null) {
                suppressFilter = true;
                filterText = string.Empty;
                viewSource.View?.Refresh();
                suppressFilter = false;
            }
        }
    }
}
