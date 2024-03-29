﻿namespace Elmish.Uno.Samples.EventBindingsAndBehaviors;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable

using Microsoft.Xaml.Interactivity;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public sealed partial class FocusAction : DependencyObject, IAction
{
    public static readonly DependencyProperty TargetObjectProperty = DependencyProperty.Register("TargetObject", typeof(Control), typeof(FocusAction), new PropertyMetadata((object?)null));

    public Control? TargetObject
    {
        get => (Control)this.GetValue(TargetObjectProperty);
        set => this.SetValue(TargetObjectProperty, (object?)value);
    }

    public object? Execute(object sender, object parameter)
    {
        Control? val = ((object?)TargetObject == null) ? (sender as Control) : TargetObject;
        val?.Focus(FocusState.Programmatic);
        return null;
    }
}