using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace NinaHA.Plugin.Controls {

    /// <summary>
    /// An editable combo box with case-insensitive "contains" filtering and autocomplete. As the user
    /// types, the drop-down shows only matching items, capped at <see cref="MaxResults"/> so it stays fast
    /// even with thousands of candidates. Items are strings; the chosen value is the editable
    /// <see cref="ComboBox.Text"/>, so bind <c>Text</c> to the target property and <see cref="SourceItems"/>
    /// to the candidate list. Inherits the standard ComboBox theme.
    /// </summary>
    public class SearchComboBox : ComboBox {

        private const int MaxResults = 100;

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

        private readonly ObservableCollection<object> results = new ObservableCollection<object>();

        public SearchComboBox() {
            IsEditable = true;
            IsTextSearchEnabled = false;
            StaysOpenOnEdit = true;
            ItemsSource = results;
            AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, __) => Rebuild()));
        }

        private static void OnSourceItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var box = (SearchComboBox)d;
            if (e.OldValue is INotifyCollectionChanged oldObs) {
                oldObs.CollectionChanged -= box.OnSourceCollectionChanged;
            }
            if (e.NewValue is INotifyCollectionChanged newObs) {
                newObs.CollectionChanged += box.OnSourceCollectionChanged;
            }
            box.Rebuild();
        }

        private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

        // Open the drop-down only when the user actually types, so picking an item (mouse/Enter) doesn't reopen it.
        protected override void OnPreviewTextInput(TextCompositionEventArgs e) {
            base.OnPreviewTextInput(e);
            if (!IsDropDownOpen) {
                IsDropDownOpen = true;
            }
        }

        /// <summary>Recomputes the (capped) list of matches for the current text.</summary>
        private void Rebuild() {
            var text = Text ?? string.Empty;
            results.Clear();
            if (SourceItems == null) {
                return;
            }
            var total = 0;
            foreach (var item in SourceItems) {
                var s = item?.ToString();
                if (s == null) {
                    continue;
                }
                if (text.Length == 0 || s.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) {
                    total++;
                    if (results.Count < MaxResults) {
                        results.Add(item!);
                    }
                }
            }
            if (total > MaxResults) {
                results.Add(new TruncationNotice(MaxResults, total));
            }
        }

        // Render the truncation notice as a disabled, italic, non-selectable hint.
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item) {
            base.PrepareContainerForItemOverride(element, item);
            if (element is ComboBoxItem container) {
                var notice = item is TruncationNotice;
                container.IsEnabled = !notice;
                container.FontStyle = notice ? FontStyles.Italic : FontStyles.Normal;
                container.Opacity = notice ? 0.7 : 1.0;
            }
        }
    }

    /// <summary>Non-selectable footer shown when the result list is capped.</summary>
    internal sealed class TruncationNotice {
        private readonly int shown;
        private readonly int total;

        public TruncationNotice(int shown, int total) {
            this.shown = shown;
            this.total = total;
        }

        public override string ToString() => $"… showing first {shown} of {total} — refine your search";
    }
}
