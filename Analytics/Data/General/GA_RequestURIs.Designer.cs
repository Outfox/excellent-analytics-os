﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.3082
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Analytics.Data.General {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "9.0.0.0")]
    internal sealed partial class GA_RequestURIs : global::System.Configuration.ApplicationSettingsBase {
        
        private static GA_RequestURIs defaultInstance = ((GA_RequestURIs)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new GA_RequestURIs())));
        
        public static GA_RequestURIs Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://www.google.com/analytics/feeds/data")]
        public string ReportFeed {
            get {
                return ((string)(this["ReportFeed"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://www.google.com/analytics/feeds/accounts/")]
        public string AccountFeed {
            get {
                return ((string)(this["AccountFeed"]));
            }
        }
    }
}