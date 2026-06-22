using System.Windows;
using System.Windows.Controls;

namespace NinaHA.Plugin.Controls {

    /// <summary>
    /// Enables two-way binding of <see cref="PasswordBox.Password"/> (which is not a dependency property,
    /// so it can't be bound directly) through an attached <c>BoundPassword</c> property. Set
    /// <c>BindPassword="True"</c> to activate the bridge.
    /// </summary>
    public static class PasswordBoxAssistant {

        public static readonly DependencyProperty BoundPasswordProperty = DependencyProperty.RegisterAttached(
            "BoundPassword", typeof(string), typeof(PasswordBoxAssistant),
            new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

        public static readonly DependencyProperty BindPasswordProperty = DependencyProperty.RegisterAttached(
            "BindPassword", typeof(bool), typeof(PasswordBoxAssistant),
            new PropertyMetadata(false, OnBindPasswordChanged));

        private static readonly DependencyProperty UpdatingProperty = DependencyProperty.RegisterAttached(
            "Updating", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false));

        public static string GetBoundPassword(DependencyObject d) => (string)d.GetValue(BoundPasswordProperty);
        public static void SetBoundPassword(DependencyObject d, string value) => d.SetValue(BoundPasswordProperty, value);

        public static bool GetBindPassword(DependencyObject d) => (bool)d.GetValue(BindPasswordProperty);
        public static void SetBindPassword(DependencyObject d, bool value) => d.SetValue(BindPasswordProperty, value);

        private static bool GetUpdating(DependencyObject d) => (bool)d.GetValue(UpdatingProperty);
        private static void SetUpdating(DependencyObject d, bool value) => d.SetValue(UpdatingProperty, value);

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            // Push the bound value into the box, unless this change originated from the box itself.
            if (d is PasswordBox box && GetBindPassword(box) && !GetUpdating(box)) {
                box.Password = e.NewValue as string ?? string.Empty;
            }
        }

        private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is not PasswordBox box) {
                return;
            }
            if ((bool)e.OldValue) {
                box.PasswordChanged -= HandlePasswordChanged;
            }
            if ((bool)e.NewValue) {
                box.Password = GetBoundPassword(box);
                box.PasswordChanged += HandlePasswordChanged;
            }
        }

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e) {
            var box = (PasswordBox)sender;
            SetUpdating(box, true);
            SetBoundPassword(box, box.Password);
            SetUpdating(box, false);
        }
    }
}
