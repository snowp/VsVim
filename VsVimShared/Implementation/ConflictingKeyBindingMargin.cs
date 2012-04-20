﻿using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using VsVim.UI;

namespace VsVim.Implementation
{
    internal sealed class ConflictingKeyBindingMargin : IWpfTextViewMargin
    {
        internal const string Name = "Vim Conflicting KeyBinding Margin";

        private readonly IKeyBindingService _keyBindingService;
        private readonly ConflictingKeyBindingMarginControl _control;
        private readonly ILegacySettings _legacySettings;
        private bool _enabled = true;

        internal ConflictingKeyBindingMargin(IKeyBindingService service, IEditorFormatMap formatMap, ILegacySettings legacySettings)
        {
            _keyBindingService = service;
            _legacySettings = legacySettings;
            _control = new ConflictingKeyBindingMarginControl();
            _control.Background = formatMap.GetBackgroundBrush(EditorFormatDefinitionNames.Margin, MarginFormatDefinition.DefaultColor);

            _control.ConfigureClick += OnConfigureClick;
            _control.IgnoreClick += OnIgnoreClick;
            _keyBindingService.ConflictingKeyBindingStateChanged += OnStateChanged;

            OnStateChanged(this, EventArgs.Empty);
        }

        private void Unsubscribe()
        {
            _control.ConfigureClick -= OnConfigureClick;
            _control.IgnoreClick -= OnIgnoreClick;
            _keyBindingService.ConflictingKeyBindingStateChanged -= OnStateChanged;
        }

        private void OnConfigureClick(object sender, EventArgs e)
        {
            _keyBindingService.ResolveAnyConflicts();
            _legacySettings.IgnoredConflictingKeyBinding = true;
            _legacySettings.Save();
        }

        private void OnIgnoreClick(object sender, EventArgs e)
        {
            _keyBindingService.IgnoreAnyConflicts();
            _legacySettings.IgnoredConflictingKeyBinding = true;
            _legacySettings.Save();
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            switch (_keyBindingService.ConflictingKeyBindingState)
            {
                case ConflictingKeyBindingState.HasNotChecked:
                case ConflictingKeyBindingState.ConflictsIgnoredOrResolved:
                    _control.Visibility = Visibility.Collapsed;
                    _enabled = false;
                    break;
                case ConflictingKeyBindingState.FoundConflicts:
                    _control.Visibility = Visibility.Visible;
                    _enabled = true;
                    break;
                default:
                    throw new Exception("Enum value unknown");
            }
        }

        #region IWpfTextViewMargin

        public System.Windows.FrameworkElement VisualElement
        {
            get { return _control; }
        }

        public bool Enabled
        {
            get { return _enabled; }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return marginName == Name ? this : null;
        }

        public double MarginSize
        {
            get { return 25d; }
        }

        public void Dispose()
        {
            Unsubscribe();
        }

        #endregion
    }
}
