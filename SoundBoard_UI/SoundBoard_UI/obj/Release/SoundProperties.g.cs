﻿#pragma checksum "..\..\SoundProperties.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "1D6A1335A36862716579D8F2BC86F45D5830F1BC73389E040FE3282DC01E3418"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using NHotkey.Wpf;
using SoundBoard_UI;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace SoundBoard_UI {
    
    
    /// <summary>
    /// SoundProperties
    /// </summary>
    public partial class SoundProperties : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 20 "..\..\SoundProperties.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.DockPanel RootWindow;
        
        #line default
        #line hidden
        
        
        #line 21 "..\..\SoundProperties.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.DockPanel TitleBar;
        
        #line default
        #line hidden
        
        
        #line 22 "..\..\SoundProperties.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button CloseButton;
        
        #line default
        #line hidden
        
        
        #line 27 "..\..\SoundProperties.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Ellipse eClose;
        
        #line default
        #line hidden
        
        
        #line 39 "..\..\SoundProperties.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbSoundName;
        
        #line default
        #line hidden
        
        
        #line 46 "..\..\SoundProperties.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label lblShortcut;
        
        #line default
        #line hidden
        
        
        #line 47 "..\..\SoundProperties.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button btnShortcut;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/SoundBoard_UI;component/soundproperties.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\SoundProperties.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 15 "..\..\SoundProperties.xaml"
            ((SoundBoard_UI.SoundProperties)(target)).Closed += new System.EventHandler(this.Window_Closed);
            
            #line default
            #line hidden
            
            #line 16 "..\..\SoundProperties.xaml"
            ((SoundBoard_UI.SoundProperties)(target)).KeyDown += new System.Windows.Input.KeyEventHandler(this.Window_KeyDown);
            
            #line default
            #line hidden
            return;
            case 2:
            this.RootWindow = ((System.Windows.Controls.DockPanel)(target));
            return;
            case 3:
            this.TitleBar = ((System.Windows.Controls.DockPanel)(target));
            
            #line 21 "..\..\SoundProperties.xaml"
            this.TitleBar.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.TitleBar_MouseDown);
            
            #line default
            #line hidden
            return;
            case 4:
            this.CloseButton = ((System.Windows.Controls.Button)(target));
            
            #line 24 "..\..\SoundProperties.xaml"
            this.CloseButton.Click += new System.Windows.RoutedEventHandler(this.CloseButton_Click);
            
            #line default
            #line hidden
            return;
            case 5:
            this.eClose = ((System.Windows.Shapes.Ellipse)(target));
            return;
            case 6:
            this.tbSoundName = ((System.Windows.Controls.TextBox)(target));
            
            #line 39 "..\..\SoundProperties.xaml"
            this.tbSoundName.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.tbSoundName_TextChanged);
            
            #line default
            #line hidden
            
            #line 39 "..\..\SoundProperties.xaml"
            this.tbSoundName.GotFocus += new System.Windows.RoutedEventHandler(this.tbSoundName_GotFocus);
            
            #line default
            #line hidden
            return;
            case 7:
            this.lblShortcut = ((System.Windows.Controls.Label)(target));
            return;
            case 8:
            this.btnShortcut = ((System.Windows.Controls.Button)(target));
            
            #line 47 "..\..\SoundProperties.xaml"
            this.btnShortcut.Click += new System.Windows.RoutedEventHandler(this.btnShortcut_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

