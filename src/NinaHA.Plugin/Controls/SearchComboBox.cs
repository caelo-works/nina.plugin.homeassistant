using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

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
        private TextBox? editableTextBox;

        public SearchComboBox() {
            IsEditable = true;
            IsTextSearchEnabled = false;
            // Don't let the result list's current-item drive the selection, otherwise refiltering
            // clears SelectedItem and wipes the editable text the user is working on.
            IsSynchronizedWithCurrentItem = false;
            StaysOpenOnEdit = true;
            ItemsSource = results;
            AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, __) => Rebuild()));
            DropDownOpened += (_, __) => CollapseAutoSelection();
            // NINA themes controls via implicit styles keyed by the exact type, which don't reach a derived
            // control. Explicitly adopt the ambient ComboBox style so we match the user's chosen theme.
            SetResourceReference(StyleProperty, typeof(ComboBox));
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            editableTextBox = GetTemplateChild("PART_EditableTextBox") as TextBox;
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
            // The ComboBox auto-selects all the text on focus / drop-down open. Collapse that before this
            // keystroke is applied, otherwise the key would replace the whole value instead of inserting.
            CollapseFullSelection();
            base.OnPreviewTextInput(e);
            if (!IsDropDownOpen) {
                // Defer opening: opening synchronously here re-selects all the text before the keystroke is
                // applied, which would again make the key replace the entire value.
                Dispatcher.BeginInvoke(new Action(() => {
                    if (!IsDropDownOpen) {
                        IsDropDownOpen = true;
                    }
                }), DispatcherPriority.Background);
            }
        }

        // The editable ComboBox selects all its text on focus and when the drop-down opens; left in place,
        // the next keystroke replaces the whole value. Collapse only that automatic full-selection (caret to
        // the end); a partial selection or a caret the user placed (e.g. by clicking) is left untouched.
        private void CollapseFullSelection() {
            var tb = editableTextBox;
            if (tb != null && tb.Text.Length > 0 && tb.SelectionLength == tb.Text.Length) {
                tb.CaretIndex = tb.Text.Length;
            }
        }

        private void CollapseAutoSelection() =>
            Dispatcher.BeginInvoke(new Action(CollapseFullSelection), DispatcherPriority.Input);

        /// <summary>Recomputes the (capped) list of matches for the current text.</summary>
        private void Rebuild() {
            // Read the live text from the editable box (ComboBox.Text can lag a keystroke behind during
            // editing). We only ever read it here — never write it — so editing is never disturbed.
            var text = editableTextBox?.Text ?? Text ?? string.Empty;

            // Compute the (capped) matches for the current text.
            var matches = new List<object>();
            if (SourceItems != null) {
                var total = 0;
                foreach (var item in SourceItems) {
                    var s = item?.ToString();
                    if (s == null) {
                        continue;
                    }
                    if (text.Length == 0 || s.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) {
                        total++;
                        if (matches.Count < MaxResults) {
                            matches.Add(item!);
                        }
                    }
                }
                if (total > MaxResults) {
                    matches.Add(new TruncationNotice(MaxResults, total));
                }
            }

            // Reconcile the bound list in place. Clear()+Add would raise a Reset, which makes the
            // editable ComboBox drop its selection and wipe the text being edited; granular
            // Replace/Add/Remove edits avoid that.
            while (results.Count > matches.Count) {
                results.RemoveAt(results.Count - 1);
            }
            for (var i = 0; i < matches.Count; i++) {
                if (i < results.Count) {
                    if (!Equals(results[i], matches[i])) {
                        results[i] = matches[i];
                    }
                } else {
                    results.Add(matches[i]);
                }
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
